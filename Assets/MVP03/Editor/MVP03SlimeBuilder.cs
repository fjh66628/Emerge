using System;
using System.IO;
using System.Linq;
using MVP03;
using MVP04;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class MVP03SlimeBuilder
{
    public const string TexturePath = "Assets/MVP03/Textures/BlueSlime.png";
    public const string MaterialPath = "Assets/MVP03/Materials/BlueSlime_Lit.mat";
    public const string PrefabPath = "Assets/MVP03/Prefabs/BlueSlime.prefab";

    [MenuItem("MVP03/Add or Rebuild Blue Slime")]
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
        Sprite sprite = ImportSprite();
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("MVP04/Lit Pixel Character")) { name = "BlueSlime_Lit" };
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Cutoff", .5f);
            material.SetFloat("_Smoothness", .12f);
            material.SetFloat("_NormalBend", 1.5f);
            material.SetFloat("_BackLight", .35f);
            material.SetFloat("_AmbientStrength", 1);
            material.SetFloat("_LightFacingShadow", 1);
            material.renderQueue = 2450;
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        material.SetTexture("_BaseMap", sprite.texture);
        EditorUtility.SetDirty(material);
        GameObject template = new GameObject("Blue slime / hopping enemy");
        GameObject prefab;
        try
        {
            var body = template.AddComponent<CharacterController>();
            body.height = 1.05f; body.radius = .46f; body.center = new Vector3(0, .535f, 0);
            body.skinWidth = .035f; body.minMoveDistance = 0; body.stepOffset = .18f; body.slopeLimit = 45;
            var card = new GameObject("Lit pixel slime");
            card.transform.SetParent(template.transform, false);
            var portrait = card.AddComponent<SpriteRenderer>();
            portrait.sprite = sprite; portrait.sharedMaterial = material; portrait.sortingOrder = 2;
            portrait.shadowCastingMode = ShadowCastingMode.TwoSided; portrait.receiveShadows = true;
            card.AddComponent<PixelCharacterShadow>();
            template.AddComponent<BouncingSlime>().Configure(portrait, null, null);
            prefab = PrefabUtility.SaveAsPrefabAsset(template, PrefabPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(template); }

        var slime = UnityEngine.Object.FindFirstObjectByType<BouncingSlime>();
        if (slime == null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Add blue slime enemy");
            instance.transform.position = new Vector3(-.2f, .09f, -15.2f);
            slime = instance.GetComponent<BouncingSlime>();
        }
        var hero = UnityEngine.Object.FindFirstObjectByType<PixelPilgrim>();
        Undo.RecordObject(slime, "Bind slime target and camera");
        slime.Configure(slime.GetComponentInChildren<SpriteRenderer>(), hero != null ? hero.transform : null, Camera.main);
        PrefabUtility.RecordPrefabInstancePropertyModifications(slime);
        EditorUtility.SetDirty(slime);
    }

    private static Sprite ImportSprite()
    {
        AssetDatabase.ImportAsset(TexturePath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
        var existing = AssetDatabase.LoadAllAssetsAtPath(TexturePath).OfType<Sprite>().FirstOrDefault();
        if (existing != null && importer.spriteImportMode == SpriteImportMode.Multiple) return existing;
        // Slice by alpha without altering the generated source pixels or alpha channel.
        var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Rect rect;
        try
        {
            ImageConversion.LoadImage(source, File.ReadAllBytes(TexturePath));
            Color32[] pixels = source.GetPixels32();
            int minX = source.width, minY = source.height, maxX = -1, maxY = -1;
            for (int y = 0; y < source.height; y++) for (int x = 0; x < source.width; x++)
            {
                if (pixels[y * source.width + x].a < 128) continue;
                minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
            }
            if (maxX < minX) throw new InvalidOperationException("BlueSlime.png has no opaque sprite pixels.");
            rect = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }
        finally { UnityEngine.Object.DestroyImmediate(source); }
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = rect.height / 1.05f;
        importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 2048;
        importer.wrapMode = TextureWrapMode.Clamp;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
#pragma warning disable CS0618
        importer.spritesheet = new[] { new SpriteMetaData { name = "BlueSlime_Body", rect = rect,
            alignment = (int)SpriteAlignment.Custom, pivot = new Vector2(.5f, 0) } };
#pragma warning restore CS0618
        importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(TexturePath).OfType<Sprite>().Single();
    }
}
