using System;
using System.IO;
using MVP03;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class MVP03HDBuilder
{
    const string Root = "Assets/MVP03";
    const string ScenePath = Root + "/Scenes/MVP03_StainedGlassChapel.unity";
    static System.Random Random = new System.Random(2603);

    [MenuItem("MVP03/Build HD2D Church")]
    public static void Build()
    {
        Random = new System.Random(2603);
        foreach (string folder in new[] { "Scenes", "Textures", "Materials", "Rendering" })
            Directory.CreateDirectory(Root + "/" + folder);
        Directory.CreateDirectory("Previews");

        Texture2D blocks = Texture("HDStoneBlocks", 512, 512, StonePixel);
        Texture2D pavers = Texture("HDPavers", 512, 512, PaverPixel);
        Texture2D cutStone = Texture("HDCutStone", 512, 512, CutStonePixel);
        Texture2D blocksNormal = Normal("HDStoneBlocksNormal", 512, 512, StonePixel);
        Texture2D paversNormal = Normal("HDPaversNormal", 512, 512, PaverPixel);
        Texture2D cutNormal = Normal("HDCutStoneNormal", 512, 512, CutStonePixel);
        Texture2D bark = Texture("HDBark", 256, 256, BarkPixel);
        Texture2D glass = Texture("HDRoseGlass", 256, 256, GlassPixel);
        Sprite heroSprite = Sprite("HDHero", HeroPixel);
        Sprite companionSprite = Sprite("HDCompanion", CompanionPixel);
        Material limestone = Lit("HDLimestone", blocks, new Color(.84f, .84f, .78f), .12f, 0f, new Vector2(2, 2));
        Material pale = Lit("HDPaleStone", blocks, new Color(1f, .96f, .85f), .2f, 0f, Vector2.one);
        Material darkStone = Lit("HDDarkStone", blocks, new Color(.43f, .44f, .44f), .17f, 0f, Vector2.one);
        Material paving = Lit("HDPaving", pavers, new Color(.86f, .84f, .77f), .13f, 0f, new Vector2(5, 6));
        Material cut = Lit("HDCutLimestone", cutStone, new Color(.89f, .88f, .82f), .2f, 0f, new Vector2(1.3f, 1.3f));
        Bump(limestone, blocksNormal, .95f);
        Bump(pale, blocksNormal, .75f);
        Bump(darkStone, blocksNormal, .85f);
        Bump(paving, paversNormal, 1f);
        Bump(cut, cutNormal, .85f);
        Material trunk = Lit("HDBark", bark, new Color(.48f, .39f, .29f), .1f, 0f, Vector2.one);
        Material iron = Flat("HDIron", new Color(.075f, .08f, .08f), .65f, .75f);
        Material voidMat = Flat("HDPortalShadow", new Color(.025f, .032f, .041f), .03f, 0);
        Material[] leaves = {
            Flat("HDLeafPale", new Color(.68f, .75f, .36f), .19f, 0),
            Flat("HDLeafOlive", new Color(.37f, .46f, .23f), .14f, 0),
            Flat("HDLeafGold", new Color(.82f, .75f, .40f), .2f, 0)
        };
        Material[] flowers = {
            Flat("HDFlowerBlue", new Color(.19f, .28f, .82f), .2f, 0),
            Flat("HDFlowerViolet", new Color(.51f, .29f, .76f), .2f, 0),
            Flat("HDFlowerYellow", new Color(.97f, .77f, .22f), .2f, 0),
            Flat("HDFlowerWhite", new Color(.91f, .87f, .70f), .2f, 0)
        };
        Material glassMat = Lit("HDRoseGlass", glass, Color.white, .8f, .1f, Vector2.one);
        glassMat.EnableKeyword("_EMISSION");
        glassMat.SetColor("_EmissionColor", new Color(.38f, .24f, .13f));

        Scene previous = SceneManager.GetActiveScene();
        if (previous.IsValid() && previous.isDirty && !string.IsNullOrEmpty(previous.path))
            EditorSceneManager.SaveScene(previous);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SetActiveScene(scene);
        Transform architecture = Group("01  CHURCH / FULL RESOLUTION STONE");

        Box("Courtyard flagstone", new Vector3(0, -.24f, -14), new Vector3(30, .45f, 40), paving, architecture);
        // Physical slabs break up the texture near the two characters.
        for (int row = 0; row < 16; row++)
            for (int col = 0; col < 15; col++)
            {
                float x = -10.9f + col * 1.1f + ((row & 1) == 0 ? 0 : .52f);
                float z = -26.7f + row * 1.1f;
                if (x > 3.5f || (x > -4 && z > -10)) continue;
                float d = (float)(Random.NextDouble() - .5) * .06f;
                Box("Cut flagstone " + row + "/" + col, new Vector3(x, .015f + d * .1f, z),
                    new Vector3(1.02f, .045f, 1.02f), (row + col) % 7 == 0 ? cut : paving, architecture);
            }

        // Massive Gothic wall, deep portal and rose window.
        Box("Church left nave", new Vector3(-10.2f, 7.5f, 7), new Vector3(12.5f, 15, 2.2f), limestone, architecture);
        Box("Church right nave", new Vector3(10.2f, 7.5f, 7), new Vector3(12.5f, 15, 2.2f), limestone, architecture);
        Box("Portal high wall", new Vector3(0, 12.1f, 7), new Vector3(8.4f, 5.8f, 2.2f), limestone, architecture);
        Box("Church roof cornice", new Vector3(0, 15.1f, 7), new Vector3(34, 1.3f, 3.3f), pale, architecture);
        Box("Dark church door", new Vector3(0, 4.8f, 8.3f), new Vector3(7.8f, 9.3f, .2f), voidMat, architecture);
        Box("Door inner left", new Vector3(-1.8f, 3.8f, 8.12f), new Vector3(3.1f, 7.4f, .16f), iron, architecture);
        Box("Door inner right", new Vector3(1.8f, 3.8f, 8.12f), new Vector3(3.1f, 7.4f, .16f), iron, architecture);
        Box("Central door seam", new Vector3(0, 3.8f, 7.96f), new Vector3(.12f, 7.4f, .1f), pale, architecture);
        for (int side = -1; side <= 1; side += 2)
        {
            Box("Portal pier " + side, new Vector3(side * 4.55f, 5.25f, 5.85f), new Vector3(1.5f, 10.5f, 2.2f), pale, architecture);
            Box("Portal foot " + side, new Vector3(side * 4.55f, .5f, 5.5f), new Vector3(2.5f, 1, 3.2f), darkStone, architecture);
            Box("Portal capital " + side, new Vector3(side * 4.55f, 10.45f, 5.7f), new Vector3(2.1f, .56f, 2.8f), pale, architecture);
            Box("Pointed arch rib " + side, new Vector3(side * 2.18f, 11.25f, 5.7f),
                new Vector3(5.65f, .75f, 1.18f), pale, architecture, Quaternion.Euler(0, 0, side * 37));
            Box("Buttress " + side, new Vector3(side * 11.7f, 5.9f, 5.05f), new Vector3(2.3f, 11.8f, 3.7f), pale, architecture);
        }
        Cylinder("Rose glass", new Vector3(0, 13.05f, 5.72f), new Vector3(3.25f, .12f, 3.25f), glassMat,
            architecture, Quaternion.Euler(90, 0, 0));
        for (int i = 0; i < 12; i++)
        {
            float a = i * Mathf.PI / 6;
            Vector3 p = new Vector3(Mathf.Cos(a) * 3.25f, 13.05f + Mathf.Sin(a) * 3.25f, 5.55f);
            Box("Rose stone tracery " + i, p, new Vector3(.29f, .65f, .4f), pale, architecture,
                Quaternion.Euler(0, 0, -i * 30));
        }
        Box("Church cross vertical", new Vector3(0, 18.45f, 6.2f), new Vector3(.46f, 5f, .62f), pale, architecture);
        Box("Church cross horizontal", new Vector3(0, 19.15f, 6.2f), new Vector3(2.8f, .46f, .62f), pale, architecture);

        // Right stair flight rises behind the courtyard and crosses the near foreground.
        Transform stairs = Group("02  STAIRWAY AND PARAPET");
        for (int i = 0; i < 16; i++)
        {
            float z = -23 + i * 1.35f;
            float y = .17f + i * .24f;
            Box("Stair stone riser " + i, new Vector3(9.5f, y * .5f, z),
                new Vector3(10.1f, y, 1.38f), cut, stairs);
            Box("Broad limestone tread " + i, new Vector3(9.5f, y, z),
                new Vector3(10.1f, .25f, 1.38f), cut, stairs);
            Box("Tread dark nosing " + i, new Vector3(9.5f, y + .14f, z - .57f),
                new Vector3(10.2f, .045f, .1f), darkStone, stairs);
        }
        for (int side = -1; side <= 1; side += 2)
        {
            Box("Stair parapet " + side, new Vector3(9.5f + side * 5.48f, 3.3f, -12.2f),
                new Vector3(.85f, 3.15f, 23.8f), cut, stairs,
                Quaternion.Euler(-11, 0, 0));
            for (int i = 0; i < 4; i++)
                Box("Stair buttress " + side + "/" + i,
                    new Vector3(9.5f + side * 5.55f, 1.8f + i * .68f, -22 + i * 6),
                    new Vector3(1.35f, 3.2f, 1.3f), darkStone, stairs);
        }
        Box("Upper landing", new Vector3(9.5f, 4.05f, 1.15f), new Vector3(10.3f, .45f, 6), paving, stairs);

        // Near camera piers frame the scene and give the stone scale.
        Transform arcade = Group("03  BROKEN LEFT ARCADE");
        for (int i = 0; i < 3; i++)
        {
            float z = -29 + i * 9.8f;
            Box("Arcade pier " + i, new Vector3(-13.1f, 5.35f, z), new Vector3(2.25f, 10.7f, 2.7f), limestone, arcade);
            Box("Arcade base " + i, new Vector3(-13.1f, .5f, z), new Vector3(3.05f, 1, 3.35f), darkStone, arcade);
            Box("Arcade capital " + i, new Vector3(-13.1f, 10.45f, z), new Vector3(3.25f, .7f, 3.5f), pale, arcade);
            if (i < 2)
                Box("Arch upper lintel " + i, new Vector3(-13.1f, 11.3f, z + 4.9f),
                    new Vector3(2.65f, 1.7f, 8.5f), limestone, arcade);
        }
        Box("Left shadow wall", new Vector3(-17.3f, 5.8f, -16.5f), new Vector3(6.4f, 11.6f, 38), darkStone, arcade);

        // Tree canopy, branches and little blue/yellow plants mirror the reference's color accents.
        Transform garden = Group("04  TREE AND FLOWERS");
        Cylinder("Tree trunk", new Vector3(-5.5f, 4.35f, -1.7f), new Vector3(.52f, 4.4f, .52f), trunk, garden);
        for (int i = 0; i < 9; i++)
        {
            float angle = i * 2.399f;
            Vector3 start = new Vector3(-5.5f, 6.5f, -1.7f);
            Vector3 end = start + new Vector3(Mathf.Cos(angle) * (2.3f + i % 3), 1.3f + i % 4, Mathf.Sin(angle) * 2.5f);
            Branch("Tree branch " + i, start, end, .17f, trunk, garden);
        }
        for (int i = 0; i < 420; i++)
        {
            float a = (float)Random.NextDouble() * Mathf.PI * 2;
            float r = Mathf.Sqrt((float)Random.NextDouble()) * 5.9f;
            Vector3 p = new Vector3(-5.5f + Mathf.Cos(a) * r, 8.6f + ((float)Random.NextDouble() - .5f) * 4.6f,
                -1.7f + Mathf.Sin(a) * r * .75f);
            float size = .19f + (float)Random.NextDouble() * .47f;
            Sphere("Sunlit leaf cluster " + i, p, new Vector3(size, size * .75f, size * .86f),
                leaves[i % leaves.Length], garden);
        }
        for (int i = 0; i < 120; i++)
        {
            float x = -8.5f + (float)Random.NextDouble() * 11f;
            float z = -8.5f + (float)Random.NextDouble() * 10.5f;
            if (x > -4f && z > -5f) continue;
            float h = .16f + (float)Random.NextDouble() * .25f;
            Cylinder("Flower stem " + i, new Vector3(x, h * .5f, z), new Vector3(.023f, h * .5f, .023f), leaves[1], garden);
            Sphere("Flower head " + i, new Vector3(x, h, z), new Vector3(.13f, .1f, .13f),
                flowers[i % flowers.Length], garden);
        }
        // The courtyard lies under another tree just outside the camera view.
        // Shadow-only leaves put a real, moving-with-the-sun dapple on the paving and sprites.
        Transform canopy = Group("05  OFFSCREEN TREE SHADOW CASTERS");
        for (int i = 0; i < 170; i++)
        {
            float x = -8.5f + (float)Random.NextDouble() * 10.5f;
            float z = -29.5f + (float)Random.NextDouble() * 4.5f;
            float y = 6.2f + (float)Random.NextDouble() * 2.7f;
            float size = .25f + (float)Random.NextDouble() * .53f;
            GameObject leaf = Sphere("Out-of-frame canopy leaf " + i, new Vector3(x, y, z),
                new Vector3(size, size * .65f, size * .8f), leaves[i % leaves.Length], canopy);
            Renderer r = leaf.GetComponent<Renderer>();
            r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            r.receiveShadows = false;
        }

        Camera camera = MakeCamera();
        Actor("Pilgrim / playable pixel sprite", new Vector3(-2.4f, .04f, -18.1f), heroSprite, camera, true);
        Actor("Companion / pixel sprite", new Vector3(-5.1f, .04f, -17.6f), companionSprite, camera, false);
        Light sun = new GameObject("Late afternoon sun / hard stone shadows").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, .9f, .72f);
        sun.intensity = 2.25f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 1f;
        sun.transform.rotation = Quaternion.Euler(35, -20, 0);
        RenderSettings.sun = sun;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.37f, .46f, .53f);
        RenderSettings.ambientEquatorColor = new Color(.22f, .27f, .31f);
        RenderSettings.ambientGroundColor = new Color(.09f, .11f, .13f);
        RenderSettings.fog = false;
        Volume volume = new GameObject("Subtle full-resolution grading").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.profile = Atmosphere();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        CaptureCurrentScene();
        Debug.Log("MVP03 HD2D church built: PBR stone courtyard, full resolution light and shadows, two point-filtered sprite characters.");
    }

    [MenuItem("MVP03/Capture HD2D Preview")]
    public static void CaptureCurrentScene()
    {
        Camera camera = GameObject.Find("Main Camera")?.GetComponent<Camera>();
        if (camera == null) throw new InvalidOperationException("Open the MVP03 HD2D scene first.");
        RenderTexture output = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
        RenderTexture oldTarget = camera.targetTexture;
        RenderTexture oldActive = RenderTexture.active;
        try
        {
            camera.targetTexture = output;
            camera.Render();
            RenderTexture.active = output;
            Texture2D pixels = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            pixels.Apply();
            Directory.CreateDirectory("Previews");
            File.WriteAllBytes("Previews/MVP03_Chapel.png", pixels.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(pixels);
        }
        finally
        {
            camera.targetTexture = oldTarget;
            RenderTexture.active = oldActive;
            UnityEngine.Object.DestroyImmediate(output);
        }
    }

    static Camera MakeCamera()
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        Camera camera = go.AddComponent<Camera>();
        camera.transform.position = new Vector3(6.8f, 6.8f, -29f);
        camera.transform.LookAt(new Vector3(-2.5f, 2.5f, -13.4f));
        camera.fieldOfView = 48;
        camera.nearClipPlane = .15f;
        camera.farClipPlane = 135;
        camera.allowHDR = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.7f, .76f, .77f);
        UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
        data.SetRenderer(1);
        data.renderPostProcessing = true;
        go.AddComponent<AudioListener>();
        return camera;
    }

    static void Actor(string name, Vector3 position, Sprite sprite, Camera camera, bool playable)
    {
        GameObject root = new GameObject(name);
        root.transform.position = position;
        if (playable)
        {
            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 2.05f;
            cc.radius = .34f;
            cc.center = new Vector3(0, .8f, 0);
            cc.stepOffset = .35f;
        }
        GameObject child = new GameObject("Point-filtered 32 x 48 sprite");
        child.transform.SetParent(root.transform, false);
        child.transform.localPosition = new Vector3(0, .05f, 0);
        child.transform.localScale = Vector3.one * 1.3f;
        SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 10;
        renderer.shadowCastingMode = ShadowCastingMode.TwoSided;
        Vector3 facing = camera.transform.position - child.transform.position;
        facing.y = 0;
        child.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
        if (playable) root.AddComponent<PixelPilgrim>().Configure(camera, renderer);
        else root.AddComponent<PixelBillboard>().Configure(camera, renderer);
        Sphere(name + " / contact shadow", position + new Vector3(0, .014f, 0), new Vector3(.42f, .01f, .24f),
            Flat("HDContactShadow", new Color(.19f, .19f, .18f), .05f, 0), root.transform);
    }

    static VolumeProfile Atmosphere()
    {
        string path = Root + "/Rendering/HDAtmosphere.asset";
        VolumeProfile p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (p == null) { p = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(p, path); }
        if (!p.TryGet(out Bloom bloom)) { bloom = p.Add<Bloom>(true); AssetDatabase.AddObjectToAsset(bloom, p); }
        bloom.threshold.Override(1.25f);
        bloom.intensity.Override(.12f);
        if (!p.TryGet(out Vignette vignette)) { vignette = p.Add<Vignette>(true); AssetDatabase.AddObjectToAsset(vignette, p); }
        vignette.intensity.Override(.12f);
        if (!p.TryGet(out ColorAdjustments color)) { color = p.Add<ColorAdjustments>(true); AssetDatabase.AddObjectToAsset(color, p); }
        color.contrast.Override(8);
        color.saturation.Override(-7);
        if (!p.TryGet(out DepthOfField depth)) { depth = p.Add<DepthOfField>(true); AssetDatabase.AddObjectToAsset(depth, p); }
        depth.mode.Override(DepthOfFieldMode.Bokeh);
        depth.focusDistance.Override(16.3f);
        depth.focalLength.Override(38f);
        depth.aperture.Override(6.3f);
        EditorUtility.SetDirty(p);
        return p;
    }

    static Transform Group(string name) => new GameObject(name).transform;
    static GameObject Primitive(string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat,
        Transform parent, Quaternion? rotation = null)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.transform.rotation = rotation ?? Quaternion.identity;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (type == PrimitiveType.Sphere || type == PrimitiveType.Cylinder)
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }
    static GameObject Box(string n, Vector3 p, Vector3 s, Material m, Transform t, Quaternion? q = null) =>
        Primitive(n, PrimitiveType.Cube, p, s, m, t, q);
    static GameObject Sphere(string n, Vector3 p, Vector3 s, Material m, Transform t) =>
        Primitive(n, PrimitiveType.Sphere, p, s, m, t);
    static GameObject Cylinder(string n, Vector3 p, Vector3 s, Material m, Transform t, Quaternion? q = null) =>
        Primitive(n, PrimitiveType.Cylinder, p, s, m, t, q);
    static void Branch(string n, Vector3 from, Vector3 to, float radius, Material m, Transform t)
    {
        Vector3 delta = to - from;
        Cylinder(n, (from + to) * .5f, new Vector3(radius, delta.magnitude * .5f, radius), m, t,
            Quaternion.FromToRotation(Vector3.up, delta));
    }

    static Material GetMat(string name)
    {
        string path = Root + "/Materials/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        return m;
    }
    static Material Flat(string name, Color color, float smooth, float metal)
    {
        Material m = GetMat(name);
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", metal);
        EditorUtility.SetDirty(m);
        return m;
    }
    static Material Lit(string name, Texture2D tex, Color tint, float smooth, float metal, Vector2 tiling)
    {
        Material m = Flat(name, tint, smooth, metal);
        m.SetTexture("_BaseMap", tex);
        m.SetTextureScale("_BaseMap", tiling);
        EditorUtility.SetDirty(m);
        return m;
    }
    static void Bump(Material material, Texture2D normal, float strength)
    {
        material.SetTexture("_BumpMap", normal);
        material.SetFloat("_BumpScale", strength);
        material.EnableKeyword("_NORMALMAP");
        EditorUtility.SetDirty(material);
    }

    static Texture2D Texture(string name, int w, int h, Func<int, int, Color32> pixel)
    {
        string path = Root + "/Textures/" + name + ".png";
        Texture2D source = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color32[] colors = new Color32[w * h];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) colors[y * w + x] = pixel(x, y);
        source.SetPixels32(colors); source.Apply();
        File.WriteAllBytes(path, source.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(source);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.filterMode = FilterMode.Trilinear;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.mipmapEnabled = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Texture2D Normal(string name, int w, int h, Func<int, int, Color32> height)
    {
        string path = Root + "/Textures/" + name + ".png";
        Texture2D source = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color32[] colors = new Color32[w * h];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
        {
            float left = height((x + w - 1) % w, y).r;
            float right = height((x + 1) % w, y).r;
            float down = height(x, (y + h - 1) % h).r;
            float up = height(x, (y + 1) % h).r;
            Vector3 n = new Vector3((left - right) / 55f, (down - up) / 55f, 1).normalized;
            colors[y * w + x] = new Color32(B(128 + n.x * 127), B(128 + n.y * 127), B(128 + n.z * 127), 255);
        }
        source.SetPixels32(colors); source.Apply();
        File.WriteAllBytes(path, source.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(source);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.NormalMap;
        importer.filterMode = FilterMode.Trilinear;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.mipmapEnabled = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Sprite Sprite(string name, Func<int, int, Color32> pixel)
    {
        const int w = 32, h = 48;
        string path = Root + "/Textures/" + name + ".png";
        Texture2D source = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color32[] colors = new Color32[w * h];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) colors[y * w + x] = pixel(x, y);
        source.SetPixels32(colors); source.Apply();
        File.WriteAllBytes(path, source.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(source);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 30;
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(.5f, 0);
        importer.SetTextureSettings(settings);
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static float Grain(int x, int y) =>
        (Mathf.PerlinNoise(x * .0093f, y * .0093f) - .5f) * 30 +
        (Mathf.PerlinNoise(x * .051f + 4, y * .052f + 3) - .5f) * 17 +
        (Mathf.PerlinNoise(x * .19f + 7, y * .18f + 9) - .5f) * 7;
    static byte B(float x) => (byte)Mathf.Clamp(Mathf.RoundToInt(x), 0, 255);
    static Color32 StonePixel(int x, int y)
    {
        int row = y / 104;
        int xx = (x + ((row & 1) * 95)) % 192;
        int yy = y % 104;
        bool joint = xx < 5 || yy < 5;
        float n = Grain(x, y) + Mathf.Sin(x * .41f + y * .13f) * 2;
        float v = (joint ? 75 : 175) + n;
        return new Color32(B(v), B(v + 1), B(v - 6), 255);
    }
    static Color32 PaverPixel(int x, int y)
    {
        int row = y / 64;
        int xx = (x + ((row & 1) * 64)) % 128;
        int yy = y % 64;
        bool joint = xx < 4 || yy < 4;
        float irregular = Mathf.PerlinNoise(x * .027f, y * .027f) * 5;
        float v = (joint ? 65 : 165) + Grain(x, y) + irregular + (row % 3) * 3;
        return new Color32(B(v), B(v + 1), B(v - 4), 255);
    }
    static Color32 CutStonePixel(int x, int y)
    {
        float scratch = Mathf.Sin(x * .18f + Mathf.PerlinNoise(x * .015f, y * .013f) * 4) * 2;
        float v = 174 + Grain(x, y) * .66f + scratch;
        return new Color32(B(v + 3), B(v + 2), B(v - 3), 255);
    }
    static Color32 BarkPixel(int x, int y)
    {
        float stripe = Mathf.Sin(x * .53f + Mathf.PerlinNoise(x * .04f, y * .032f) * 9) * 16;
        float v = 75 + stripe + Grain(x, y) * .45f;
        return new Color32(B(v + 16), B(v + 6), B(v - 7), 255);
    }
    static Color32 GlassPixel(int x, int y)
    {
        float dx = x - 128, dy = y - 128;
        float r = Mathf.Sqrt(dx * dx + dy * dy);
        if (r > 121) return new Color32(29, 27, 30, 255);
        float a = Mathf.Atan2(dy, dx);
        int wedge = Mathf.FloorToInt((a + Mathf.PI) / (Mathf.PI / 6));
        bool lead = Mathf.Abs((a + Mathf.PI) % (Mathf.PI / 6)) < .035f || ((int)r % 40 < 4);
        if (lead) return new Color32(30, 29, 34, 255);
        float n = Grain(x, y) * .35f;
        if (wedge % 3 == 0) return new Color32(B(177 + n), B(76 + n), B(35 + n), 255);
        if (wedge % 3 == 1) return new Color32(B(40 + n), B(91 + n), B(145 + n), 255);
        return new Color32(B(187 + n), B(146 + n), B(51 + n), 255);
    }
    static Color32 HeroPixel(int x, int y) => CharacterPixel(x, y, false);
    static Color32 CompanionPixel(int x, int y) => CharacterPixel(x, y, true);
    static Color32 CharacterPixel(int x, int y, bool dark)
    {
        Color32 outline = new Color32(34, 29, 39, 255);
        Color32 skin = dark ? new Color32(188, 141, 109, 255) : new Color32(241, 199, 158, 255);
        Color32 hair = dark ? new Color32(25, 29, 34, 255) : new Color32(116, 73, 48, 255);
        Color32 coat = dark ? new Color32(45, 69, 66, 255) : new Color32(246, 246, 229, 255);
        Color32 accent = dark ? new Color32(141, 172, 107, 255) : new Color32(86, 106, 193, 255);
        Color32 boots = dark ? new Color32(40, 36, 40, 255) : new Color32(108, 54, 85, 255);
        int dx = Mathf.Abs(x - 16);
        if (y >= 38 && y <= 45 && dx <= (y > 42 ? 5 : 6)) return hair;
        if (y >= 31 && y < 41 && dx <= 5)
        {
            if (y >= 38 && (x < 12 || x > 19)) return hair;
            if (y == 35 && (x == 13 || x == 19)) return outline;
            return skin;
        }
        if (y >= 15 && y <= 31 && dx <= (y < 21 ? 8 : 7))
        {
            if (dx >= 7 || y == 15) return outline;
            if (y > 24 && dx <= 2) return accent;
            if (y < 20 && (x < 13 || x > 19)) return accent;
            return coat;
        }
        if (y >= 19 && y <= 29 && ((x >= 5 && x <= 8) || (x >= 24 && x <= 27)))
            return y < 22 ? skin : (dark ? coat : accent);
        if (y >= 2 && y <= 15 && ((x >= 10 && x <= 14) || (x >= 18 && x <= 22)))
            return y <= 5 ? boots : (dark ? new Color32(54, 58, 54, 255) : new Color32(154, 95, 136, 255));
        return new Color32(0, 0, 0, 0);
    }
}
