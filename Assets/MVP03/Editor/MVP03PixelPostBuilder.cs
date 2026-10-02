using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class MVP03PixelPostBuilder
{
    private const string ShaderName = "MVP03/Subtle Pixels";
    private const string MaterialPath = "Assets/MVP03/Materials/SubtlePixels.mat";
    private const string RendererPath = "Assets/MVP03/Rendering/RebuiltRenderer.asset";
    private static readonly int Blend = Shader.PropertyToID("_Blend");

    [MenuItem("MVP03/Apply Subtle Pixel Post Process")]
    public static void Apply()
    {
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (renderer == null) throw new InvalidOperationException("Build the MVP03 courtyard first.");
        Configure(renderer);
        AssetDatabase.SaveAssets();
    }

    public static void Configure(UniversalRendererData renderer)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null) throw new InvalidOperationException("Import the Subtle Pixels shader first.");
            material = new Material(shader) { name = "SubtlePixels" };
            material.SetFloat("_PixelSize", 2);
            material.SetFloat(Blend, .55f);
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        FullScreenPassRendererFeature feature = null;
        foreach (var item in renderer.rendererFeatures)
            if (item is FullScreenPassRendererFeature fullScreen && fullScreen.passMaterial == material)
                feature = fullScreen;
        if (feature == null)
        {
            feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
            feature.name = "Subtle pixels / scene and magic";
            AssetDatabase.AddObjectToAsset(feature, renderer);
            renderer.rendererFeatures.Add(feature);
        }

        // Process the composited image, including transparent VFX, bloom and depth of field.
        feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;
        feature.fetchColorBuffer = true;
        feature.requirements = ScriptableRenderPassInput.None;
        feature.bindDepthStencilAttachment = false;
        feature.passMaterial = material;
        feature.passIndex = 0;
        feature.SetActive(true);
        feature.Create();
        EditorUtility.SetDirty(feature);
        renderer.SetDirty();
        EditorUtility.SetDirty(renderer);
    }

    [MenuItem("MVP03/Capture Subtle Pixel Comparison")]
    public static void CaptureComparison()
    {
        Camera camera = Camera.main;
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (camera == null || material == null) throw new InvalidOperationException("Open MVP03 and apply the pixel effect first.");
        var output = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
        var pixels = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        RenderTexture oldTarget = camera.targetTexture, oldActive = RenderTexture.active;
        float blend = material.GetFloat(Blend);
        try
        {
            camera.targetTexture = output;
            Directory.CreateDirectory("Previews");
            material.SetFloat(Blend, 0);
            Capture("Previews/MVP03_SubtlePixels_Original.png");
            material.SetFloat(Blend, blend);
            Capture("Previews/MVP03_SubtlePixels.png");
        }
        finally
        {
            material.SetFloat(Blend, blend);
            camera.targetTexture = oldTarget;
            RenderTexture.active = oldActive;
            UnityEngine.Object.DestroyImmediate(pixels);
            UnityEngine.Object.DestroyImmediate(output);
        }

        void Capture(string path)
        {
            camera.Render();
            RenderTexture.active = output;
            pixels.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(path, pixels.EncodeToPNG());
        }
    }
}
