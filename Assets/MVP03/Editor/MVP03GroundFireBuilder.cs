using System;
using MVP03;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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
}
