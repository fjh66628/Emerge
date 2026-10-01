using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MVP01;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class MVP01Builder
{
    private const string Root = "Assets/MVP01";
    private const string ScenePath = Root + "/Scenes/MVP01_BrutalistWalk.unity";
    private const string MeshPath = Root + "/Materials/BoxMeshes.asset";
    private static readonly Dictionary<string, Mesh> MeshCache = new Dictionary<string, Mesh>();
    private static readonly HashSet<string> GeneratedTextures = new HashSet<string>();
    private static int boxCount;

    [InitializeOnLoadMethod]
    private static void BuildOnFirstImport()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(ScenePath)) return;
            Build();
        };
    }

    [MenuItem("MVP01/Build Brutalist Walk")]
    public static void Build()
    {
        Directory.CreateDirectory(Root + "/Scenes");
        Directory.CreateDirectory(Root + "/Materials");
        Directory.CreateDirectory(Root + "/Textures");
        Directory.CreateDirectory("Previews");
        MeshCache.Clear();
        GeneratedTextures.Clear();
        boxCount = 0;

        Material sky = MakeSky();
        Material concrete = MakeSurface("Board Form Concrete", new Color(0.62f, 0.63f, 0.60f), "concrete", 0.03f, 0.31f);
        Material pale = MakeSurface("Pale Concrete", new Color(0.79f, 0.79f, 0.75f), "concrete", 0.03f, 0.29f);
        Material dark = MakeSurface("Basalt Floor", new Color(0.28f, 0.31f, 0.31f), "concrete", 0.02f, 0.26f);
        Material steel = MakeSurface("Brushed Steel", new Color(0.51f, 0.56f, 0.55f), "steel", 0.90f, 0.51f);
        Material accent = MakeSurface("Oxide Accent", new Color(0.57f, 0.27f, 0.15f), "paint", 0.25f, 0.42f);

        Scene previous = SceneManager.GetActiveScene();
        bool previousDirty = previous.IsValid() && previous.isDirty;
        bool replaceActive = !previous.IsValid() || previous.path == ScenePath || string.IsNullOrEmpty(previous.path);
        if (replaceActive && previousDirty)
            throw new InvalidOperationException("Save or discard manual edits to the active scene before regenerating MVP01.");
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
            replaceActive ? NewSceneMode.Single : NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(scene);

        GameObject world = new GameObject("01  |  MONOLITH FIELD");
        GameObject route = ChildGroup("Walkable slabs", world.transform);
        GameObject architecture = ChildGroup("Concrete architecture", world.transform);
        GameObject details = ChildGroup("Steel and oxide details", world.transform);
        GameObject backdrop = ChildGroup("Distant silhouettes", world.transform);

        Box("Continuous basalt plinth", V(0, -0.75f, 0), V(70, 1.5f, 76), dark, route.transform);
        for (int i = 0; i < 13; i++)
        {
            float z = -30 + i * 5.0f;
            Box("Inset concrete seam L " + i, V(-14, 0.025f, z), V(11, 0.05f, 0.10f), concrete, route.transform);
            Box("Inset concrete seam R " + i, V(14, 0.025f, z), V(11, 0.05f, 0.10f), concrete, route.transform);
        }
        Box("Entry landing", V(0, 0.08f, -27), V(10, 0.16f, 8), concrete, route.transform);
        Box("Courtyard landing", V(0, 0.08f, 13), V(30, 0.16f, 26), concrete, route.transform);
        Box("Return path", V(18, 0.08f, -8), V(8, 0.16f, 42), concrete, route.transform);

        Box("Entry wall left", V(-5.6f, 3.2f, -22), V(1.4f, 6.4f, 20), concrete, architecture.transform);
        Box("Entry wall right", V(5.6f, 3.2f, -22), V(1.4f, 6.4f, 20), concrete, architecture.transform);
        Box("Compression lintel", V(0, 5.8f, -14), V(12.5f, 1.6f, 3), pale, architecture.transform);
        Box("Threshold blade left", V(-4.1f, 3.7f, -10), V(1.5f, 7.4f, 2), steel, details.transform);
        Box("Threshold blade right", V(4.1f, 3.7f, -10), V(1.5f, 7.4f, 2), steel, details.transform);

        float[] columnZ = { -7, -1, 5 };
        for (int i = 0; i < columnZ.Length; i++)
        {
            float heightL = 5.8f + i * 1.3f;
            float heightR = 7.8f - i * 0.9f;
            Box("Column L " + i, V(-3.7f, heightL * 0.5f, columnZ[i]), V(1.3f, heightL, 1.3f), i == 1 ? pale : concrete, architecture.transform);
            Box("Column R " + i, V(3.7f, heightR * 0.5f, columnZ[i] + 1.5f), V(1.3f, heightR, 1.3f), i == 1 ? steel : concrete, architecture.transform);
        }
        Box("Sky slot bridge", V(0, 7.5f, 1), V(11, 1.2f, 2.4f), concrete, architecture.transform);

        Box("Central monolith", V(0, 6.5f, 19), V(9.5f, 13, 8.5f), concrete, architecture.transform);
        Box("Monolith upper offset", V(3.4f, 12.9f, 20), V(12.8f, 2.1f, 9.5f), pale, architecture.transform);
        Box("Cantilever counterweight", V(-5.8f, 9.5f, 17.5f), V(6.5f, 3.2f, 5.5f), dark, architecture.transform);
        Box("Monolith lower steel inset", V(0, 3.2f, 14.7f), V(5.7f, 0.8f, 0.11f), steel, details.transform);

        Box("West enclosure", V(-19, 6, 15), V(2.2f, 12, 35), concrete, architecture.transform);
        Box("East enclosure", V(26, 5, 13), V(2.0f, 10, 31), pale, architecture.transform);
        Box("Rear horizontal mass", V(3, 4.8f, 34), V(38, 9.6f, 2.2f), concrete, architecture.transform);
        Box("Rear raised crown", V(-9, 10.2f, 33.5f), V(12, 2.8f, 4.5f), dark, architecture.transform);

        Box("West vertical mass", V(-13, 5, 7), V(5, 10, 7), pale, architecture.transform);
        Box("East freestanding mass", V(17, 5.7f, 21), V(5, 11.4f, 8), concrete, architecture.transform);
        Box("East mass cap", V(18.7f, 11.9f, 22), V(8.4f, 1.5f, 9), steel, details.transform);
        Box("Return corridor edge", V(23, 1.8f, -8), V(1, 3.6f, 25), dark, architecture.transform);
        Box("Return corridor cut", V(12, 1.6f, -14), V(1, 3.2f, 14), concrete, architecture.transform);

        for (int i = 0; i < 7; i++)
        {
            float z = -25 + i * 8.2f;
            float h = 0.42f + (i % 3) * 0.24f;
            Box("Oxide navigation marker " + i, V(22.35f, h * 0.5f, z), V(0.10f, h, 1.5f), accent, details.transform);
        }
        Box("Courtyard oxide marker", V(-12, 0.22f, 22), V(4.2f, 0.44f, 0.22f), accent, details.transform);
        Box("Concrete study block A", V(-11, 1.2f, 19), V(2.4f, 2.4f, 2.4f), pale, architecture.transform);
        Box("Concrete study block B", V(-14, 0.75f, 24), V(1.5f, 1.5f, 1.5f), concrete, architecture.transform);
        Box("Metal study block", V(11, 1.1f, 27), V(2.2f, 2.2f, 2.2f), steel, details.transform);

        for (int i = 0; i < 8; i++)
        {
            float x = -37 + i * 10.3f;
            float h = 11 + (i * 7 % 12);
            Box("Distant tower " + i, V(x, h * 0.5f, 48 + (i % 3) * 4), V(5 + i % 3, h, 6), i % 2 == 0 ? pale : concrete, backdrop.transform, false);
        }

        GameObject lighting = new GameObject("02  |  LIGHT AND ATMOSPHERE");
        GameObject sunObject = new GameObject("Cold directional sun");
        sunObject.transform.SetParent(lighting.transform);
        sunObject.transform.rotation = Quaternion.Euler(35, -42, 0);
        Light sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.25f;
        sun.color = new Color(1f, 0.965f, 0.91f);
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.82f;

        RenderSettings.skybox = sky;
        RenderSettings.sun = sun;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.76f, 0.79f, 0.78f);
        RenderSettings.ambientEquatorColor = new Color(0.58f, 0.61f, 0.60f);
        RenderSettings.ambientGroundColor = new Color(0.32f, 0.35f, 0.35f);
        RenderSettings.ambientIntensity = 0.85f;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        RenderSettings.reflectionIntensity = 0.8f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.85f, 0.88f, 0.87f);
        RenderSettings.fogStartDistance = 20f;
        RenderSettings.fogEndDistance = 72f;

        AddReflectionProbe("Courtyard reflection", V(0, 5, 15), V(42, 20, 40), lighting.transform);
        AddReflectionProbe("Passage reflection", V(0, 3, -18), V(16, 12, 30), lighting.transform);

        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root + "/Materials/MVP01_Volume.asset");
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, Root + "/Materials/MVP01_Volume.asset");
        }
        if (!profile.TryGet(out Tonemapping tone))
        {
            tone = profile.Add<Tonemapping>(true);
            AssetDatabase.AddObjectToAsset(tone, profile);
        }
        tone.mode.Override(TonemappingMode.Neutral);
        if (!profile.TryGet(out ColorAdjustments grading))
        {
            grading = profile.Add<ColorAdjustments>(true);
            AssetDatabase.AddObjectToAsset(grading, profile);
        }
        grading.contrast.Override(5f);
        grading.saturation.Override(-7f);
        grading.postExposure.Override(0f);
        EditorUtility.SetDirty(profile);
        EditorUtility.SetDirty(tone);
        EditorUtility.SetDirty(grading);
        GameObject volumeObject = new GameObject("Global tonal balance");
        volumeObject.transform.SetParent(lighting.transform);
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10;
        volume.sharedProfile = profile;

        GameObject player = new GameObject("03  |  FIRST PERSON PLAYER");
        player.transform.position = V(0, 0.08f, -28.5f);
        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.35f;
        controller.center = V(0, 0.9f, 0);
        controller.stepOffset = 0.32f;
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(player.transform);
        cameraObject.transform.localPosition = V(0, 1.67f, 0);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.fieldOfView = 68f;
        camera.nearClipPlane = 0.08f;
        camera.farClipPlane = 130f;
        camera.allowHDR = true;
        cameraObject.AddComponent<AudioListener>();
        UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;
        player.AddComponent<FirstPersonWalk>();

        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new IOException("Unity could not save the generated MVP01 scene.");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        if (!previousDirty) EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Camera previewCamera = GameObject.Find("Main Camera")?.GetComponent<Camera>();
        if (previewCamera != null) CapturePreviews(previewCamera);
        Debug.Log($"MVP01 built: {boxCount} textured boxes, 2 reflection probes, first-person player. Scene: {ScenePath}");
    }

    [MenuItem("MVP01/Capture Preview")]
    public static void CaptureCurrentScene()
    {
        Camera camera = GameObject.Find("Main Camera")?.GetComponent<Camera>();
        if (camera == null) throw new InvalidOperationException("Open the MVP01 scene first.");
        CapturePreviews(camera);
    }

    private static GameObject ChildGroup(string name, Transform parent)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent);
        return group;
    }

    private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

    private static GameObject Box(string name, Vector3 position, Vector3 size, Material material, Transform parent, bool collider = true)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;
        MeshFilter filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = GetBoxMesh(size);
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        if (collider) go.AddComponent<BoxCollider>().size = size;
        go.isStatic = true;
        boxCount++;
        return go;
    }

    private static Mesh GetBoxMesh(Vector3 size)
    {
        string key = string.Format(CultureInfo.InvariantCulture, "Box_{0:0.###}_{1:0.###}_{2:0.###}", size.x, size.y, size.z);
        if (MeshCache.TryGetValue(key, out Mesh cached)) return cached;
        Mesh existing = AssetDatabase.LoadAllAssetsAtPath(MeshPath).OfType<Mesh>().FirstOrDefault(m => m.name == key);

        float x = size.x * 0.5f, y = size.y * 0.5f, z = size.z * 0.5f;
        var vertices = new List<Vector3>(24);
        var uvs = new List<Vector2>(24);
        var triangles = new List<int>(36);
        void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float u, float v)
        {
            int start = vertices.Count;
            vertices.AddRange(new[] { a, b, c, d });
            uvs.AddRange(new[] { new Vector2(0, 0), new Vector2(u, 0), new Vector2(u, v), new Vector2(0, v) });
            triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
        }
        Face(V(-x,-y,z), V(x,-y,z), V(x,y,z), V(-x,y,z), size.x / 4f, size.y / 4f);
        Face(V(x,-y,-z), V(-x,-y,-z), V(-x,y,-z), V(x,y,-z), size.x / 4f, size.y / 4f);
        Face(V(x,-y,z), V(x,-y,-z), V(x,y,-z), V(x,y,z), size.z / 4f, size.y / 4f);
        Face(V(-x,-y,-z), V(-x,-y,z), V(-x,y,z), V(-x,y,-z), size.z / 4f, size.y / 4f);
        Face(V(-x,y,z), V(x,y,z), V(x,y,-z), V(-x,y,-z), size.x / 4f, size.z / 4f);
        Face(V(-x,-y,-z), V(x,-y,-z), V(x,-y,z), V(-x,-y,z), size.x / 4f, size.z / 4f);
        Mesh mesh = existing ?? new Mesh { name = key };
        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        if (existing == null)
        {
            if (AssetDatabase.LoadMainAssetAtPath(MeshPath) == null) AssetDatabase.CreateAsset(mesh, MeshPath);
            else AssetDatabase.AddObjectToAsset(mesh, MeshPath);
        }
        else EditorUtility.SetDirty(mesh);
        MeshCache[key] = mesh;
        return mesh;
    }

    private static Material MakeSky()
    {
        string path = Root + "/Materials/WhiteStrataSky.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "/Shaders/WhiteStrataSky.shader");
        if (shader == null) throw new InvalidOperationException("WhiteStrataSky shader has not imported.");
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = shader;
        material.SetColor("_ZenithColor", new Color(0.79f, 0.83f, 0.84f));
        material.SetColor("_UpperColor", new Color(0.96f, 0.96f, 0.93f));
        material.SetColor("_HorizonColor", new Color(0.99f, 1.0f, 0.98f));
        material.SetColor("_LowerColor", new Color(0.86f, 0.89f, 0.88f));
        material.SetFloat("_BandStrength", 0.012f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material MakeSurface(string name, Color tint, string textureName, float metallic, float smoothness)
    {
        string path = Root + "/Materials/" + name.Replace(' ', '_') + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader is missing.");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", tint);
        material.SetTexture("_BaseMap", Texture(textureName, "base", false));
        material.SetTexture("_MetallicGlossMap", Texture(textureName, "mask", true));
        material.SetTexture("_OcclusionMap", Texture(textureName, "mask", true));
        material.SetTexture("_BumpMap", Texture(textureName, "normal", true));
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_BumpScale", textureName == "concrete" ? 0.33f : 0.20f);
        material.SetFloat("_OcclusionStrength", 0.65f);
        material.EnableKeyword("_METALLICSPECGLOSSMAP");
        material.EnableKeyword("_OCCLUSIONMAP");
        material.EnableKeyword("_NORMALMAP");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Texture2D Texture(string surface, string kind, bool linear)
    {
        string path = Root + "/Textures/" + surface + "_" + kind + ".png";
        if (GeneratedTextures.Add(path)) WriteTexture(surface, kind, path);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            importer = AssetImporter.GetAtPath(path) as TextureImporter;
        }
        if (importer != null)
        {
            TextureImporterType type = kind == "normal" ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (importer.textureType != type || importer.sRGBTexture == linear)
            {
                importer.textureType = type;
                importer.sRGBTexture = !linear;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.SaveAndReimport();
            }
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void WriteTexture(string surface, string kind, string path)
    {
        const int resolution = 256;
        float[,] height = new float[resolution, resolution];
        int seed = surface == "concrete" ? 13 : surface == "steel" ? 47 : 71;
        for (int y = 0; y < resolution; y++)
        for (int x = 0; x < resolution; x++)
        {
            float broad = Mathf.PerlinNoise((x + seed) * 0.025f, (y + seed) * 0.025f);
            float fine = Mathf.PerlinNoise((x + seed * 3) * 0.19f, (y + seed * 3) * 0.19f);
            float board = surface == "concrete" ? Mathf.Sin(y * 0.18f + broad * 3f) * 0.035f : 0f;
            height[x, y] = Mathf.Clamp01(broad * 0.67f + fine * 0.25f + board);
        }

        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, true, kind != "base");
        var pixels = new Color[resolution * resolution];
        for (int y = 0; y < resolution; y++)
        for (int x = 0; x < resolution; x++)
        {
            float h = height[x, y];
            float fleck = Mathf.PerlinNoise((x + seed) * 0.53f, (y - seed) * 0.53f);
            Color color;
            if (kind == "base")
            {
                float value = surface == "concrete" ? 0.88f + (h - 0.5f) * 0.10f : 0.90f + (h - 0.5f) * 0.10f;
                if (surface == "paint") value *= fleck > 0.79f ? 0.69f : 1f;
                color = new Color(value, value, value, 1);
            }
            else if (kind == "mask")
            {
                float metal = surface == "concrete" ? 0.0f : surface == "paint" ? (fleck > 0.79f ? 0.9f : 0.08f) : 0.90f;
                float ao = Mathf.Lerp(0.84f, 1f, h);
                float gloss = surface == "concrete" ? 0.72f + h * 0.12f : 0.76f + h * 0.16f;
                color = new Color(metal, ao, 0f, gloss);
            }
            else
            {
                float dx = height[(x + 1) % resolution, y] - height[(x + resolution - 1) % resolution, y];
                float dy = height[x, (y + 1) % resolution] - height[x, (y + resolution - 1) % resolution];
                Vector3 n = new Vector3(-dx * 1.3f, -dy * 1.3f, 1f).normalized;
                color = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
            }
            pixels[y * resolution + x] = color;
        }
        texture.SetPixels(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
    }

    private static void AddReflectionProbe(string name, Vector3 position, Vector3 size, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;
        ReflectionProbe probe = go.AddComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
        probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
        probe.size = size;
        probe.resolution = 128;
        probe.intensity = 0.65f;
        probe.boxProjection = true;
    }

    private static void CapturePreviews(Camera camera)
    {
        Directory.CreateDirectory("Previews");
        Transform cameraTransform = camera.transform;
        Vector3 originalPosition = cameraTransform.position;
        Quaternion originalRotation = cameraTransform.rotation;
        Capture(camera, "MVP01_Entrance.png", V(0, 1.75f, -25), V(0, 2.8f, 9));
        Capture(camera, "MVP01_Courtyard.png", V(-10, 1.75f, 6), V(1, 7, 20));
        Capture(camera, "MVP01_Monolith.png", V(14, 1.75f, 1), V(0, 8, 19));
        cameraTransform.position = originalPosition;
        cameraTransform.rotation = originalRotation;
    }

    private static void Capture(Camera camera, string filename, Vector3 position, Vector3 target)
    {
        RenderTexture output = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        camera.transform.position = position;
        camera.transform.rotation = Quaternion.LookRotation(target - position);
        camera.targetTexture = output;
        camera.Render();
        camera.Render();
        RenderTexture.active = output;
        Texture2D pixels = new Texture2D(output.width, output.height, TextureFormat.RGB24, false);
        pixels.ReadPixels(new Rect(0, 0, output.width, output.height), 0, 0);
        pixels.Apply();
        File.WriteAllBytes("Previews/" + filename, pixels.EncodeToPNG());
        camera.targetTexture = previousTarget;
        RenderTexture.active = previousActive;
        UnityEngine.Object.DestroyImmediate(pixels);
        UnityEngine.Object.DestroyImmediate(output);
    }
}
