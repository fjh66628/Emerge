using System;
using System.IO;
using System.Linq;
using MVP03;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class MVP03Builder
{
    private const string Root = "Assets/MVP03";
    private const string ScenePath = Root + "/Scenes/MVP03_PixelChapelArchive.unity";
    private const int DisplayLayer = 30;

    [MenuItem("MVP03/Build Pixel Chapel Archive")]
    public static void Build()
    {
        Directory.CreateDirectory(Root + "/Scenes");
        Directory.CreateDirectory(Root + "/Materials");
        Directory.CreateDirectory(Root + "/Textures");
        Directory.CreateDirectory(Root + "/Rendering");
        Directory.CreateDirectory("Previews");

        Texture2D stoneTexture = WriteTexture("ChapelStone", 32, 32, StonePixel);
        Texture2D floorTexture = WriteTexture("ChapelFloor", 32, 32, FloorPixel);
        Texture2D woodTexture = WriteTexture("DarkOak", 32, 32, WoodPixel);
        Texture2D runnerTexture = WriteTexture("AisleRunner", 32, 32, RunnerPixel);
        Texture2D roseTexture = WriteTexture("RoseGlass", 64, 64, RosePixel);
        Texture2D lancetTexture = WriteTexture("LancetGlass", 24, 48, LancetPixel);
        Texture2D projectionTexture = WriteTexture("GlassLightPatch", 32, 32, ProjectionPixel);
        WriteTexture("PixelPilgrim", 24, 40, PilgrimPixel, true);

        Material stone = Lit("Stone", stoneTexture, new Color(0.72f, 0.75f, 0.83f), 0.12f, V2(3, 2));
        Material paleStone = Lit("PaleStone", stoneTexture, new Color(0.95f, 0.89f, 0.78f), 0.18f, V2(1, 1));
        Material floor = Lit("Floor", floorTexture, new Color(0.82f, 0.82f, 0.86f), 0.18f, V2(6, 12));
        Material oak = Lit("DarkOak", woodTexture, new Color(0.55f, 0.38f, 0.27f), 0.26f, V2(2, 1));
        Material carpet = Lit("AisleRunner", runnerTexture, Color.white, 0.1f, V2(1, 10));
        Material brass = Solid("OldBrass", new Color(0.91f, 0.66f, 0.25f), 0.48f, 0.72f);
        Material dark = Solid("BlueBlackIron", new Color(0.055f, 0.07f, 0.12f), 0.28f, 0.52f);
        Material wax = Solid("IvoryWax", new Color(0.94f, 0.82f, 0.61f), 0.32f, 0f);
        Material flame = Emissive("CandleFlame", new Color(1.6f, 0.72f, 0.19f));
        Material rose = UnlitTexture("RoseGlass", roseTexture, 1.55f);
        Material lancet = UnlitTexture("LancetGlass", lancetTexture, 1.42f);
        Material projection = Projection(projectionTexture);
        Material screen = MakeScreenMaterial(MakeWorldTarget());

        Scene previous = SceneManager.GetActiveScene();
        if (previous.IsValid() && previous.isDirty && !string.IsNullOrEmpty(previous.path))
            EditorSceneManager.SaveScene(previous);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SetActiveScene(scene);

        GameObject architecture = Group("01  |  PIXEL STONE CHAPEL", null);
        Box("Checkered stone floor", V(0, -0.16f, 0), V(22, 0.3f, 42), floor, architecture.transform);
        Box("Raised chancel", V(0, 0.18f, 15), V(11, 0.36f, 8.5f), paleStone, architecture.transform);
        Box("Chancel step", V(0, 0.08f, 10.25f), V(12, 0.16f, 1.1f), paleStone, architecture.transform);
        Box("Aisle carpet", V(0, 0.011f, -4.4f), V(2.9f, 0.025f, 29), carpet, architecture.transform);
        for (int side = -1; side <= 1; side += 2)
        {
            Box("Aisle brass seam " + side, V(side * 1.51f, 0.037f, -4.4f),
                V(0.07f, 0.025f, 29), brass, architecture.transform);
            float wallHeight = side > 0 ? 1.5f : 8.4f;
            Box("Outer stone wall " + side, V(side * 11.1f, wallHeight * 0.5f, 0),
                V(0.8f, wallHeight, 42), stone, architecture.transform);
            if (side < 0)
                Box("Upper wall rail " + side, V(side * 10.5f, 8.55f, 0),
                    V(1.5f, 0.5f, 42), paleStone, architecture.transform);
            for (int i = 0; i < 5; i++)
            {
                float z = -15 + i * 8;
                Box("Pier " + side + " / " + i, V(side * 8.6f, 3.45f, z),
                    V(1.05f, 6.9f, 1.15f), paleStone, architecture.transform);
                Box("Pier foot " + side + " / " + i, V(side * 8.6f, 0.3f, z),
                    V(1.75f, 0.6f, 1.75f), stone, architecture.transform);
                Box("Pier capital " + side + " / " + i, V(side * 8.6f, 6.85f, z),
                    V(1.7f, 0.42f, 1.7f), brass, architecture.transform);
                if (i < 4 && side < 0)
                {
                    Box("Side lancet frame " + side + " / " + i,
                        V(side * 10.55f, 4.7f, z + 4), V(0.22f, 4.35f, 2.25f), dark, architecture.transform);
                    Box("Side lancet light " + side + " / " + i,
                        V(side * 10.40f, 4.7f, z + 4), V(0.16f, 3.85f, 1.78f),
                        i % 2 == 0 ? rose : lancet, architecture.transform);
                }
            }
        }

        Box("Rear sanctuary wall", V(0, 4.55f, 20.7f), V(22.8f, 9.1f, 0.85f), stone, architecture.transform);
        Box("Rose window dark inset", V(0, 6.7f, 20.18f), V(6.5f, 6.5f, 0.15f), dark, architecture.transform);
        Quad("Rose window mosaic", V(0, 6.7f, 20.07f), V(6.1f, 6.1f, 1), rose, architecture.transform);
        for (int side = -1; side <= 1; side += 2)
        {
            Box("Rear lancet inset " + side, V(side * 7.3f, 5.1f, 20.18f),
                V(2.8f, 5.5f, 0.15f), dark, architecture.transform);
            Quad("Rear lancet mosaic " + side, V(side * 7.3f, 5.1f, 20.07f),
                V(2.3f, 5.05f, 1), lancet, architecture.transform);
        }

        GameObject reflectedLight = Group("Pixel stained-glass light on floor", architecture.transform);
        for (int i = 0; i < 4; i++)
        {
            GameObject patch = Quad("Colored window projection " + i,
                V(-5.5f + i * 3.5f, 0.048f, 8.5f - i * 3.2f),
                V(3.8f, 5.0f, 1), projection, reflectedLight.transform);
            patch.transform.rotation = Quaternion.Euler(90, 0, -20);
        }

        GameObject arches = Group("Pointed nave arches", architecture.transform);
        for (int i = 0; i < 5; i++)
        {
            float z = -15 + i * 8;
            Beam("Left pointed rib " + i, V(-8.6f, 7.05f, z), V(0, 10.2f, z), paleStone, arches.transform, 0.48f);
            Beam("Right pointed rib " + i, V(8.6f, 7.05f, z), V(0, 10.2f, z), paleStone, arches.transform, 0.48f);
            Box("Hanging keystone " + i, V(0, 9.77f, z), V(0.76f, 0.85f, 0.92f), brass, arches.transform);
        }

        GameObject pews = Group("02  |  DARK OAK PEWS", null);
        for (int i = 0; i < 5; i++)
        {
            float z = -13 + i * 5;
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 5.15f;
                Box("Pew seat " + side + " / " + i, V(x, 0.72f, z),
                    V(5.25f, 0.22f, 1.12f), oak, pews.transform);
                Box("Pew back " + side + " / " + i, V(x, 1.17f, z + 0.48f),
                    V(5.25f, 1.0f, 0.16f), oak, pews.transform);
                for (int end = -1; end <= 1; end += 2)
                {
                    Box("Pew carved end " + side + " / " + i + " / " + end,
                        V(x + end * 2.55f, 0.6f, z), V(0.22f, 1.2f, 1.5f), oak, pews.transform);
                    Box("Pew brass stud " + side + " / " + i + " / " + end,
                        V(x + end * 2.55f, 1.18f, z - 0.5f),
                        V(0.28f, 0.09f, 0.2f), brass, pews.transform);
                }
            }
        }

        GameObject sanctuary = Group("03  |  ALTAR AND CANDLES", null);
        Box("Altar stone pedestal", V(0, 0.97f, 15.6f), V(4.9f, 1.55f, 2.15f), paleStone, sanctuary.transform);
        Box("Altar gold lip", V(0, 1.84f, 15.6f), V(5.4f, 0.18f, 2.45f), brass, sanctuary.transform);
        Box("Altar dark cloth", V(0, 1.865f, 15.6f), V(3.3f, 0.08f, 1.82f), dark, sanctuary.transform);
        Box("Standing cross vertical", V(0, 3.3f, 18.9f), V(0.34f, 3.8f, 0.35f), brass, sanctuary.transform);
        Box("Standing cross horizontal", V(0, 3.75f, 18.9f), V(2.25f, 0.35f, 0.35f), brass, sanctuary.transform);
        for (int side = -1; side <= 1; side += 2)
        {
            Box("Altar side pedestal " + side, V(side * 4.2f, 0.85f, 16.2f),
                V(0.85f, 1.45f, 0.85f), stone, sanctuary.transform);
            for (int i = 0; i < 3; i++)
            {
                float x = side * (2.05f + i * 0.62f);
                float height = 0.42f + i * 0.11f;
                Cylinder("Candle " + side + " / " + i, V(x, 2.05f + height * 0.5f, 15.6f),
                    V(0.10f, height * 0.5f, 0.10f), wax, sanctuary.transform);
                Sphere("Flame " + side + " / " + i, V(x, 2.14f + height, 15.6f),
                    V(0.13f, 0.24f, 0.13f), flame, sanctuary.transform);
            }
        }

        GameObject lights = Group("04  |  STAINED LIGHT AND SHADOW", null);
        GameObject sunObject = new GameObject("High warm sun");
        sunObject.transform.SetParent(lights.transform);
        sunObject.transform.rotation = Quaternion.Euler(54, -30, 0);
        Light sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.88f, 0.72f);
        sun.intensity = 0.82f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.82f;
        RenderSettings.sun = sun;
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.31f, 0.34f, 0.43f);
        RenderSettings.fog = false;
        AddSpot("Cyan stained light", V(-8.4f, 8.0f, 15.5f), V(-2.4f, 0, 5),
            new Color(0.32f, 0.72f, 1f), 8.2f, 37f, lights.transform);
        AddSpot("Magenta stained light", V(8.4f, 8.0f, 14.5f), V(2.3f, 0, 3),
            new Color(0.9f, 0.38f, 0.78f), 7.4f, 39f, lights.transform);
        AddSpot("Golden altar pool", V(0, 7.1f, 17), V(0, 0, 11),
            new Color(1f, 0.76f, 0.36f), 7f, 52f, lights.transform);
        for (int side = -1; side <= 1; side += 2)
        {
            Point("Candle glow " + side, V(side * 2.5f, 2.55f, 15.6f),
                new Color(1f, 0.52f, 0.2f), 2.8f, 6.5f, lights.transform);
        }
        VolumeProfile profile = MakeAtmosphere();
        GameObject atmosphere = Group("Window bloom and dark edge", lights.transform);
        Volume volume = atmosphere.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 20;
        volume.sharedProfile = profile;

        GameObject actor = new GameObject("05  |  PIXEL PILGRIM");
        actor.transform.position = V(0, 0.06f, -7.1f);
        CharacterController controller = actor.AddComponent<CharacterController>();
        controller.height = 1.65f;
        controller.center = V(0, 0.825f, 0);
        controller.radius = 0.32f;
        controller.stepOffset = 0.34f;
        Cylinder("Painted contact shadow", V(0, 0.018f, 0), V(0.43f, 0.006f, 0.28f), dark, actor.transform);
        GameObject portraitObject = new GameObject("Flat 24 x 40 sprite");
        portraitObject.transform.SetParent(actor.transform);
        portraitObject.transform.localPosition = V(0, 0.07f, 0);
        portraitObject.transform.localScale = V(1.7f, 1.7f, 1);
        SpriteRenderer portrait = portraitObject.AddComponent<SpriteRenderer>();
        portrait.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Textures/PixelPilgrim.png");
        portrait.sortingOrder = 10;
        PixelPilgrim walker = actor.AddComponent<PixelPilgrim>();

        RenderTexture worldTarget = AssetDatabase.LoadAssetAtPath<RenderTexture>(Root + "/Rendering/WorldPixels.renderTexture");
        GameObject worldCameraObject = new GameObject("Church Camera / 640 x 360 pixels");
        worldCameraObject.transform.position = V(14, 14, -23);
        worldCameraObject.transform.LookAt(V(0, 3.5f, 5.2f));
        Camera worldCamera = worldCameraObject.AddComponent<Camera>();
        worldCamera.orthographic = true;
        worldCamera.orthographicSize = 12f;
        worldCamera.nearClipPlane = 0.1f;
        worldCamera.farClipPlane = 120f;
        worldCamera.clearFlags = CameraClearFlags.SolidColor;
        worldCamera.backgroundColor = new Color(0.025f, 0.03f, 0.065f);
        worldCamera.allowHDR = true;
        worldCamera.cullingMask = ~(1 << DisplayLayer);
        worldCamera.targetTexture = worldTarget;
        UniversalAdditionalCameraData worldData = worldCameraObject.AddComponent<UniversalAdditionalCameraData>();
        worldData.SetRenderer(1);
        worldData.renderPostProcessing = true;

        GameObject screenObject = new GameObject("Main Camera");
        screenObject.tag = "MainCamera";
        screenObject.transform.position = V(0, 0, 10);
        screenObject.transform.rotation = Quaternion.Euler(0, 180, 0);
        Camera screenCamera = screenObject.AddComponent<Camera>();
        screenCamera.orthographic = true;
        screenCamera.orthographicSize = 4.5f;
        screenCamera.nearClipPlane = 0.1f;
        screenCamera.farClipPlane = 20f;
        screenCamera.clearFlags = CameraClearFlags.SolidColor;
        screenCamera.backgroundColor = Color.black;
        screenCamera.cullingMask = 1 << DisplayLayer;
        screenCamera.depth = 1;
        screenObject.AddComponent<AudioListener>();
        UniversalAdditionalCameraData screenData = screenObject.AddComponent<UniversalAdditionalCameraData>();
        screenData.SetRenderer(1);
        screenData.renderPostProcessing = false;
        GameObject display = Quad("Nearest-neighbor pixel display", Vector3.zero, V(16, 9, 1), screen, null);
        display.layer = DisplayLayer;
        display.transform.rotation = Quaternion.Euler(0, 180, 0);
        walker.Configure(worldCamera, portrait);

        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new IOException("Could not save MVP03 scene.");
        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(s => s.path == ScenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
        AssetDatabase.SaveAssets();
        Debug.Log("MVP03 built: pixel-textured church, stained-glass spotlights, flat playable sprite.");
    }

    [MenuItem("MVP03/Capture Pixel Chapel Archive")]
    public static void CaptureCurrentScene()
    {
        if (SceneManager.GetActiveScene().path != ScenePath)
            throw new InvalidOperationException("Open the MVP03 chapel before capturing.");
        Camera world = GameObject.Find("Church Camera / 640 x 360 pixels")?.GetComponent<Camera>();
        Camera display = GameObject.Find("Main Camera")?.GetComponent<Camera>();
        if (world == null || display == null) throw new InvalidOperationException("MVP03 cameras are missing.");
        world.Render();
        RenderTexture output = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
        RenderTexture oldTarget = display.targetTexture;
        RenderTexture oldActive = RenderTexture.active;
        display.targetTexture = output;
        display.Render();
        RenderTexture.active = output;
        Texture2D pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        pixels.Apply();
        Directory.CreateDirectory("Previews");
        File.WriteAllBytes("Previews/MVP03_PixelArchive.png", pixels.EncodeToPNG());
        display.targetTexture = oldTarget;
        RenderTexture.active = oldActive;
        UnityEngine.Object.DestroyImmediate(pixels);
        UnityEngine.Object.DestroyImmediate(output);
    }

    private static RenderTexture MakeWorldTarget()
    {
        string path = Root + "/Rendering/WorldPixels.renderTexture";
        RenderTexture target = AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
        if (target == null)
        {
            target = new RenderTexture(640, 360, 24, RenderTextureFormat.ARGB32);
            AssetDatabase.CreateAsset(target, path);
        }
        target.width = 640;
        target.height = 360;
        target.depth = 24;
        target.filterMode = FilterMode.Point;
        target.wrapMode = TextureWrapMode.Clamp;
        target.useMipMap = false;
        target.antiAliasing = 1;
        EditorUtility.SetDirty(target);
        return target;
    }

    private static VolumeProfile MakeAtmosphere()
    {
        string path = Root + "/Rendering/ChapelAtmosphere.asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }
        if (!profile.TryGet(out Bloom bloom))
        {
            bloom = profile.Add<Bloom>(true);
            AssetDatabase.AddObjectToAsset(bloom, profile);
        }
        bloom.threshold.Override(0.78f);
        bloom.intensity.Override(0.65f);
        bloom.scatter.Override(0.58f);
        if (!profile.TryGet(out Vignette vignette))
        {
            vignette = profile.Add<Vignette>(true);
            AssetDatabase.AddObjectToAsset(vignette, profile);
        }
        vignette.intensity.Override(0.16f);
        vignette.smoothness.Override(0.42f);
        EditorUtility.SetDirty(bloom);
        EditorUtility.SetDirty(vignette);
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static Material MakeScreenMaterial(RenderTexture target)
    {
        string path = Root + "/Materials/PixelDisplay.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetTexture("_BaseMap", target);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Cull", 2f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material Lit(string name, Texture2D texture, Color tint, float smoothness, Vector2 tiling)
    {
        Material material = GetMaterial(name, "Universal Render Pipeline/Lit");
        material.SetTexture("_BaseMap", texture);
        material.SetTextureScale("_BaseMap", tiling);
        material.SetColor("_BaseColor", tint);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material Solid(string name, Color tint, float smoothness, float metallic)
    {
        Material material = GetMaterial(name, "Universal Render Pipeline/Lit");
        material.SetColor("_BaseColor", tint);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material Emissive(string name, Color glow)
    {
        Material material = GetMaterial(name, "Universal Render Pipeline/Unlit");
        material.SetColor("_BaseColor", glow);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material UnlitTexture(string name, Texture2D texture, float intensity)
    {
        Material material = GetMaterial(name, "Universal Render Pipeline/Unlit");
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white * intensity);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material Projection(Texture2D texture)
    {
        Material material = GetMaterial("GlassLightPatch", "MVP03/Glass Projection");
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", new Color(0.65f, 0.75f, 1f, 0.56f));
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material GetMaterial(string name, string shaderName)
    {
        string path = Root + "/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null) throw new InvalidOperationException("Missing shader: " + shaderName);
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        return material;
    }

    private static Texture2D WriteTexture(string name, int width, int height,
        Func<int, int, Color32> pixel, bool sprite = false)
    {
        string path = Root + "/Textures/" + name + ".png";
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                pixels[y * width + x] = pixel(x, y);
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
        importer.spritePixelsPerUnit = 24;
        if (sprite)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            TextureImporterSettings spriteSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(spriteSettings);
            spriteSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            spriteSettings.spritePivot = new Vector2(0.5f, 0f);
            importer.SetTextureSettings(spriteSettings);
        }
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = sprite;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static int Noise(int x, int y) => unchecked((x * 73856093 ^ y * 19349663) & 7);

    private static Color32 StonePixel(int x, int y)
    {
        bool mortar = y % 8 == 0 || (x + (y / 8 % 2) * 4) % 8 == 0;
        if (mortar) return new Color32(58, 64, 79, 255);
        byte grain = (byte)(113 + Noise(x, y) * 4);
        return new Color32(grain, (byte)(grain + 3), (byte)(grain + 13), 255);
    }

    private static Color32 FloorPixel(int x, int y)
    {
        if (x % 8 == 0 || y % 8 == 0) return new Color32(69, 73, 85, 255);
        bool light = (x / 8 + y / 8) % 2 == 0;
        byte grain = (byte)(Noise(x, y) * 2);
        return light
            ? new Color32((byte)(164 + grain), (byte)(161 + grain), (byte)(153 + grain), 255)
            : new Color32((byte)(105 + grain), (byte)(111 + grain), (byte)(127 + grain), 255);
    }

    private static Color32 WoodPixel(int x, int y)
    {
        if (y % 8 == 0) return new Color32(36, 21, 27, 255);
        int grain = Noise(x / 3, y) * 3 + (y % 8 == 4 ? 14 : 0);
        return new Color32((byte)(87 + grain), (byte)(47 + grain / 2), (byte)(37 + grain / 3), 255);
    }

    private static Color32 RunnerPixel(int x, int y)
    {
        if (x < 3 || x > 28 || (x + y) % 16 == 0)
            return new Color32(181, 124, 53, 255);
        if ((x / 8 + y / 8) % 2 == 0)
            return new Color32(104, 31, 51, 255);
        return new Color32(69, 28, 49, 255);
    }

    private static Color32 RosePixel(int x, int y)
    {
        int dx = x - 32;
        int dy = y - 32;
        float radius = Mathf.Sqrt(dx * dx + dy * dy);
        if (radius > 29) return new Color32(24, 27, 43, 255);
        if (radius > 27 || radius < 3 || Mathf.Abs(radius - 12) < 1.1f || Mathf.Abs(radius - 21) < 1.1f)
            return new Color32(17, 20, 34, 255);
        float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 180;
        int sector = Mathf.FloorToInt(angle / 30);
        if (angle % 30 < 2.5f) return new Color32(17, 20, 34, 255);
        Color32[] glass = {
            new Color32(54, 178, 214, 255), new Color32(211, 61, 118, 255),
            new Color32(243, 173, 60, 255), new Color32(91, 104, 208, 255),
            new Color32(83, 197, 150, 255), new Color32(198, 91, 195, 255)
        };
        return glass[(sector + (int)(radius / 9)) % glass.Length];
    }

    private static Color32 LancetPixel(int x, int y)
    {
        int halfWidth = y < 34 ? 10 : Mathf.Max(1, (48 - y) * 10 / 14);
        if (Mathf.Abs(x - 12) > halfWidth) return new Color32(24, 27, 43, 255);
        if (x % 6 == 0 || y % 8 == 0) return new Color32(17, 20, 34, 255);
        Color32[] panes = {
            new Color32(58, 172, 212, 255), new Color32(191, 72, 143, 255),
            new Color32(237, 157, 54, 255), new Color32(107, 103, 205, 255)
        };
        return panes[(x / 6 + y / 8) % panes.Length];
    }

    private static Color32 ProjectionPixel(int x, int y)
    {
        float dx = (x - 15.5f) / 15.5f;
        float dy = (y - 15.5f) / 15.5f;
        float edge = Mathf.Max(0f, 1f - dx * dx - dy * dy);
        byte alpha = (byte)(Mathf.Pow(edge, 1.5f) * 180);
        if (x % 8 == 0 || y % 8 == 0) alpha = (byte)(alpha / 6);
        Color32[] colors = {
            new Color32(55, 180, 241, alpha), new Color32(232, 75, 173, alpha),
            new Color32(241, 186, 78, alpha), new Color32(105, 106, 244, alpha)
        };
        return colors[(x / 8 + y / 8) % colors.Length];
    }

    private static Color32 PilgrimPixel(int x, int y)
    {
        Color32 clear = new Color32(0, 0, 0, 0);
        if (y >= 2 && y <= 5 && (x >= 6 && x <= 10 || x >= 13 && x <= 17))
            return new Color32(30, 28, 42, 255);
        if (y >= 6 && y <= 25)
        {
            int halfWidth = y < 14 ? 9 : 7;
            if (Mathf.Abs(x - 12) <= halfWidth)
            {
                if (Mathf.Abs(x - 12) >= halfWidth - 1) return new Color32(31, 39, 65, 255);
                if (x >= 11 && x <= 13) return new Color32(183, 135, 67, 255);
                return y % 6 == 0 ? new Color32(81, 69, 103, 255) : new Color32(69, 56, 91, 255);
            }
        }
        if (y >= 25 && y <= 34 && x >= 7 && x <= 17)
        {
            if (y >= 32 || x <= 8 || x >= 16) return new Color32(34, 34, 48, 255);
            if (y == 29 && (x == 10 || x == 14)) return new Color32(29, 35, 51, 255);
            return new Color32(228, 185, 143, 255);
        }
        if (y == 35 && x >= 9 && x <= 15) return new Color32(31, 34, 48, 255);
        return clear;
    }

    private static GameObject Group(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name);
        if (parent != null) gameObject.transform.SetParent(parent);
        return gameObject;
    }

    private static GameObject Box(string name, Vector3 position, Vector3 size, Material material, Transform parent)
    {
        return Primitive(PrimitiveType.Cube, name, position, size, material, parent);
    }

    private static GameObject Quad(string name, Vector3 position, Vector3 size, Material material, Transform parent)
    {
        GameObject quad = Primitive(PrimitiveType.Quad, name, position, size, material, parent);
        UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
        quad.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        return quad;
    }

    private static GameObject Cylinder(string name, Vector3 position, Vector3 size, Material material, Transform parent)
    {
        GameObject cylinder = Primitive(PrimitiveType.Cylinder, name, position, size, material, parent);
        UnityEngine.Object.DestroyImmediate(cylinder.GetComponent<Collider>());
        return cylinder;
    }

    private static GameObject Sphere(string name, Vector3 position, Vector3 size, Material material, Transform parent)
    {
        GameObject sphere = Primitive(PrimitiveType.Sphere, name, position, size, material, parent);
        UnityEngine.Object.DestroyImmediate(sphere.GetComponent<Collider>());
        return sphere;
    }

    private static GameObject Primitive(PrimitiveType type, string name, Vector3 position,
        Vector3 scale, Material material, Transform parent)
    {
        GameObject gameObject = GameObject.CreatePrimitive(type);
        gameObject.name = name;
        if (parent != null) gameObject.transform.SetParent(parent);
        gameObject.transform.position = position;
        gameObject.transform.localScale = scale;
        gameObject.GetComponent<MeshRenderer>().sharedMaterial = material;
        return gameObject;
    }

    private static void Beam(string name, Vector3 a, Vector3 b, Material material, Transform parent, float width)
    {
        GameObject beam = Box(name, (a + b) * 0.5f,
            V(width, Vector3.Distance(a, b), 0.65f), material, parent);
        beam.transform.rotation = Quaternion.FromToRotation(Vector3.up, b - a);
    }

    private static void AddSpot(string name, Vector3 position, Vector3 target, Color color,
        float intensity, float angle, Transform parent)
    {
        GameObject lightObject = Group(name, parent);
        lightObject.transform.position = position;
        lightObject.transform.rotation = Quaternion.LookRotation(target - position);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = color;
        light.intensity = intensity;
        light.range = 24;
        light.spotAngle = angle;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.6f;
    }

    private static void Point(string name, Vector3 position, Color color,
        float intensity, float range, Transform parent)
    {
        GameObject lightObject = Group(name, parent);
        lightObject.transform.position = position;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
    }

    private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
    private static Vector2 V2(float x, float y) => new Vector2(x, y);
}
