using System;
using System.Linq;
using MVP04;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static partial class MVP04Builder
{
    [MenuItem("MVP04/Apply Candle Sway")]
    public static void ApplyCandleSway()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != ScenePath)
            throw new InvalidOperationException("Open MVP04 outside Play mode first.");
        var lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
            .Where(light => light.gameObject.scene == scene && light.name.StartsWith("Candle pool "))
            .ToDictionary(light => light.name);
        foreach (var flames in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                     .Where(renderer => renderer.gameObject.scene == scene && renderer.name.StartsWith("Flames")))
        {
            Undo.RegisterFullObjectHierarchyUndo(flames.gameObject, "Add candle sway");
            lights.TryGetValue("Candle pool " + flames.name.Substring("Flames".Length), out var lamp);
            ConfigureCandleSway(flames, lamp);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureCandleSway(Renderer flames, Light lamp)
    {
        if (!flames.TryGetComponent<CandleSway>(out var sway))
            sway = flames.gameObject.AddComponent<CandleSway>();
        Vector3 centre = flames.bounds.center;
        float phase = 11.7f + Mathf.PerlinNoise(centre.x * 1.71f + 100, centre.z * 2.39f + 100) * 700;
        sway.Configure(lamp, flames, phase);
        EditorUtility.SetDirty(sway);
    }
}
