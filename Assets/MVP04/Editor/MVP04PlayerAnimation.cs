using System;
using System.Linq;
using MVP03;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static partial class MVP04Builder
{
    private const string WalkAtlasPath = Root + "/Textures/PilgrimWalk.png";
    private const string WalkSetPath = Root + "/Textures/PilgrimWalk.asset";

    [MenuItem("MVP04/Apply Four Direction Walk Animation")]
    public static void ApplyPlayerAnimation()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != ScenePath)
            throw new InvalidOperationException("Open MVP04 outside Play mode first.");
        var hero = UnityEngine.Object.FindFirstObjectByType<PixelPilgrim>();
        if (hero == null) throw new InvalidOperationException("The chapel pilgrim must exist first.");
        Undo.RegisterFullObjectHierarchyUndo(hero.gameObject, "Apply four direction walk animation");
        ConfigurePlayerAnimation(hero.GetComponentInChildren<SpriteRenderer>());
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static void ConfigurePlayerAnimation(SpriteRenderer portrait)
    {
        PixelWalkSet set = ImportWalkSet();
        if (!portrait.TryGetComponent<PixelWalkAnimation>(out var animation))
            animation = portrait.gameObject.AddComponent<PixelWalkAnimation>();
        animation.Configure(set);
        // Keep the existing chapel tint, depth writing and shadow material settings.
        string materialPath = Root + "/Materials/NocturneTravelerWalk.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(portrait.sharedMaterial) { name = "NocturneTravelerWalk" };
            AssetDatabase.CreateAsset(material, materialPath);
        }
        material.SetTexture("_BaseMap", portrait.sprite.texture);
        ConfigureCharacterLighting(material);
        portrait.sharedMaterial = material;
        EditorUtility.SetDirty(material);
        EditorUtility.SetDirty(animation);
        EditorUtility.SetDirty(portrait);
    }

    private static PixelWalkSet ImportWalkSet()
    {
        AssetDatabase.ImportAsset(WalkAtlasPath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(WalkAtlasPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 112;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048;
        importer.wrapMode = TextureWrapMode.Clamp;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        // Keep a common baseline within each direction so feet can lift during a step.
        string[] directions = { "Front", "Left", "Right", "Back" };
        int[] footBaselines = { 250, 242, 242, 228 };
        var slices = new SpriteMetaData[24];
        for (int row = 0; row < 4; row++) for (int column = 0; column < 6; column++)
            slices[row * 6 + column] = new SpriteMetaData
            {
                name = "Pilgrim_" + directions[row] + "_" + column,
                rect = new Rect(column * 256, (3 - row) * 256, 256, 256),
                alignment = (int)SpriteAlignment.Custom,
                pivot = new Vector2(.5f, (256 - footBaselines[row]) / 256f)
            };
        // The project does not depend on the optional 2D Sprite editor package.
#pragma warning disable CS0618
        importer.spritesheet = slices;
#pragma warning restore CS0618
        importer.SaveAndReimport();
        var frames = AssetDatabase.LoadAllAssetsAtPath(WalkAtlasPath).OfType<Sprite>()
            .ToDictionary(sprite => sprite.name);
        var set = AssetDatabase.LoadAssetAtPath<PixelWalkSet>(WalkSetPath);
        if (set == null)
        {
            set = ScriptableObject.CreateInstance<PixelWalkSet>();
            AssetDatabase.CreateAsset(set, WalkSetPath);
        }
        Sprite[] Row(string direction) => Enumerable.Range(0, 6)
            .Select(index => frames["Pilgrim_" + direction + "_" + index]).ToArray();
        set.front = Row("Front"); set.left = Row("Left");
        set.right = Row("Right"); set.back = Row("Back");
        set.metresPerCycle = 2.25f;
        EditorUtility.SetDirty(set);
        return set;
    }
}
