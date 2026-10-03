using System;
using System.Linq;
using MVP03;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class MVP03PuddleBuilder
{
    public const string MaterialPath = "Assets/MVP03/Materials/WetPuddle.mat";
    public const string RendererPath = "Assets/MVP03/Rendering/PuddleReflectionRenderer.asset";

    [MenuItem("MVP03/Add Click-to-Place Puddles")]
    public static void Apply()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != MVP03Presentation.ScenePath)
            throw new InvalidOperationException("Open MVP03 outside Play mode first.");
        ConfigureScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    public static void ConfigureScene()
    {
        var camera = Camera.main;
        if (camera == null) throw new InvalidOperationException("The courtyard camera must exist first.");
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("MVP03/Wet Puddle")) { name = "WetPuddle" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (renderer == null)
        {
            renderer = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<UniversalRendererData>(MVP03WarmLighting.RendererPath));
            renderer.name = "PuddleReflectionRenderer";
            // Dedicated forward capture: no SSAO, volume integration, pixels or post processing.
            renderer.rendererFeatures.Clear();
            AssetDatabase.CreateAsset(renderer, RendererPath);
            renderer.SetDirty();
        }
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
        var serialized = new SerializedObject(pipeline);
        var list = serialized.FindProperty("m_RendererDataList");
        int index = -1;
        for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue == renderer) index = i;
        if (index < 0)
        {
            index = list.arraySize; list.InsertArrayElementAtIndex(index); list.GetArrayElementAtIndex(index).objectReferenceValue = renderer;
            serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(pipeline);
        }
        if (!camera.TryGetComponent<ClickPuddles>(out var puddles)) puddles = Undo.AddComponent<ClickPuddles>(camera.gameObject);
        Undo.RecordObject(puddles, "Configure ground puddles");
        var surfaces = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)
            .Where(c => c.gameObject.scene == camera.gameObject.scene &&
                (c.name == "Courtyard collision" || c.name == "RebuiltStairs" || c.name.StartsWith("Stair collision "))).ToArray();
        puddles.Configure(material, surfaces);
        if (!camera.TryGetComponent<PuddleReflections>(out var reflections)) reflections = Undo.AddComponent<PuddleReflections>(camera.gameObject);
        Undo.RecordObject(reflections, "Configure pooled water reflections");
        reflections.Configure(index);
        EditorUtility.SetDirty(puddles); EditorUtility.SetDirty(reflections);
    }
}
