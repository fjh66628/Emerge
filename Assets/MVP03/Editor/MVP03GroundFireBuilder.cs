using System;
using MVP03;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class MVP03GroundFireBuilder
{
    public const string MaterialPath = "Assets/MVP03/Materials/GroundFire.mat";

    [MenuItem("MVP03/Add Ground Fire")]
    public static void Apply()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != MVP03Presentation.ScenePath)
            throw new InvalidOperationException("Open MVP03 outside Play first.");
        ConfigureScene();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
    }

    public static void ConfigureScene()
    {
        ConfigureLighting();
        var ground = Camera.main != null ? Camera.main.GetComponent<ClickPuddles>() : null;
        if (ground == null) throw new InvalidOperationException("Install click-to-place puddles first.");
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            var shader = Shader.Find("MVP03/Ground Fire");
            if (shader == null) throw new InvalidOperationException("Ground Fire shader has not imported.");
            material = new Material(shader) { name = "GroundFire" };
            material.SetTexture("_StainMap", AssetDatabase.LoadAssetAtPath<Texture2D>(MVP03PuddleBuilder.StainPath));
            AssetDatabase.CreateAsset(material, MaterialPath); AssetDatabase.SaveAssetIfDirty(material);
        }
        Undo.RecordObject(ground, "Configure yellow ground fire");
        ground.ConfigureFire(material, MVP03GroundFireGraph.Build()); EditorUtility.SetDirty(ground);
    }

    public static void ConfigureLighting()
    {
        // Both the Game camera and its water reflection need clustered lighting:
        // the courtyard's combined stone mesh must receive more than four fire lights.
        foreach (string path in new[] { MVP03WarmLighting.RendererPath, MVP03PuddleBuilder.RendererPath })
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            if (renderer == null || renderer.renderingMode == RenderingMode.ForwardPlus) continue;
            Undo.RecordObject(renderer, "Enable Forward+ ground fire lighting");
            renderer.renderingMode = RenderingMode.ForwardPlus;
            renderer.SetDirty(); EditorUtility.SetDirty(renderer); AssetDatabase.SaveAssetIfDirty(renderer);
        }
    }
}
