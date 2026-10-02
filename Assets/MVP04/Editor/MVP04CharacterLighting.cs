using System;
using MVP03;
using UnityEditor;
using UnityEngine;

public static partial class MVP04Builder
{
    [MenuItem("MVP04/Apply Character Lighting")]
    public static void ApplyCharacterLighting()
    {
        var hero = UnityEngine.Object.FindFirstObjectByType<PixelPilgrim>();
        if (hero == null || hero.gameObject.scene.path != ScenePath)
            throw new InvalidOperationException("Open the MVP04 chapel first.");
        var portrait = hero.GetComponentInChildren<SpriteRenderer>();
        Material material = portrait.sharedMaterial;
        Undo.RecordObject(material, "Enable character scene lighting");
        ConfigureCharacterLighting(material);
        AssetDatabase.SaveAssetIfDirty(material);
        SceneView.RepaintAll();
    }

    private static void ConfigureCharacterLighting(Material material)
    {
        Shader shader = Shader.Find("MVP04/Lit Pixel Character");
        if (shader == null) throw new InvalidOperationException("Import the lit character shader first.");
        if (material.shader == shader) return;
        material.shader = shader;
        // The old blue tint simulated scene lighting; the new shader receives its colour from lights.
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Cutoff", .5f);
        material.SetFloat("_Smoothness", .12f);
        material.SetFloat("_NormalBend", 1.5f);
        material.SetFloat("_BackLight", .35f);
        material.SetFloat("_AmbientStrength", 1f);
        material.DisableKeyword("_ALPHATEST_ON"); // All four passes always alpha-clip.
        material.renderQueue = 2450;
        EditorUtility.SetDirty(material);
    }
}
