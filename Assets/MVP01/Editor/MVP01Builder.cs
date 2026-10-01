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

    [MenuItem("MVP01/Build Metal Field")]
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
        ConfigureRadialFog();
        Material floorMetal = MakeSurface("Metal Floor", new Color(0.35f, 0.39f, 0.41f), "steel", 0.94f, 0.62f);
        floorMetal.SetTextureScale("_BaseMap", new Vector2(25f, 25f));
        EditorUtility.SetDirty(floorMetal);
        Material columnMetal = MakeSurface("Brushed Steel", new Color(0.58f, 0.63f, 0.65f), "steel", 0.94f, 0.55f);

        Scene previous = SceneManager.GetActiveScene();
        bool previousDirty = previous.IsValid() && previous.isDirty;
        bool replaceActive = !previous.IsValid() || previous.path == ScenePath || string.IsNullOrEmpty(previous.path);
        if (replaceActive && previousDirty)
            throw new InvalidOperationException("Save or discard manual edits to the active scene before regenerating MVP01.");
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
            replaceActive ? NewSceneMode.Single : NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(scene);

        GameObject world = new GameObject("01  |  METAL COLUMN FIELD");
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Single metal floor plane";
        floor.transform.SetParent(world.transform);
        floor.transform.localScale = V(10f, 1f, 10f);
        floor.GetComponent<MeshRenderer>().sharedMaterial = floorMetal;
        floor.isStatic = true;

        GameObject columns = ChildGroup("Perimeter metal columns", world.transform);
        float[] rowX = { -15f, -5f, 5f, 15f };
        for (int i = 0; i < rowX.Length; i++)
        {
            float northHeight = 8f + (i % 3) * 2.2f;
            float southHeight = 9f + ((i + 1) % 3) * 1.8f;
            Box("North metal column " + i, V(rowX[i], northHeight * 0.5f, 20f),
                V(1.8f, northHeight, 1.8f), columnMetal, columns.transform);
            Box("South metal column " + i, V(rowX[i], southHeight * 0.5f, -20f),
                V(1.8f, southHeight, 1.8f), columnMetal, columns.transform);
        }
        float[] sideZ = { -10f, 0f, 10f };
        for (int i = 0; i < sideZ.Length; i++)
        {
            float westHeight = 10f + i * 1.4f;
            float eastHeight = 12f - i * 1.3f;
            Box("West metal column " + i, V(-21f, westHeight * 0.5f, sideZ[i]),
                V(2.2f, westHeight, 2.2f), columnMetal, columns.transform);
            Box("East metal column " + i, V(21f, eastHeight * 0.5f, sideZ[i]),
                V(2.2f, eastHeight, 2.2f), columnMetal, columns.transform);
        }

        MVP01FloraBuilder.Build(world.transform);
        MVP01BoardBuilder.Build(world.transform);

        GameObject lighting = new GameObject("02  |  LIGHT AND ATMOSPHERE");
        GameObject sunObject = new GameObject("Cold directional sun");
        sunObject.transform.SetParent(lighting.transform);
        sunObject.transform.rotation = Quaternion.Euler(35, -42, 0);
        Light sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.12f;
        sun.color = new Color(0.86f, 0.92f, 1f);
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.82f;

        RenderSettings.skybox = sky;
        RenderSettings.sun = sun;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.69f, 0.75f, 0.78f);
        RenderSettings.ambientEquatorColor = new Color(0.49f, 0.56f, 0.59f);
        RenderSettings.ambientGroundColor = new Color(0.25f, 0.31f, 0.34f);
        RenderSettings.ambientIntensity = 0.85f;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        RenderSettings.reflectionIntensity = 0.8f;
        RenderSettings.fog = false;

        AddReflectionProbe("Metal field reflection", V(0, 5, 0), V(90, 24, 90), lighting.transform);

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
        grading.contrast.Override(9f);
        grading.saturation.Override(-13f);
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
        player.transform.position = V(0, 0.08f, -17f);
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

        // Let the freshly opened scene and URP finish a frame before reading the render target.
        EditorApplication.delayCall += () =>
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath) return;
            Camera previewCamera = GameObject.Find("Main Camera")?.GetComponent<Camera>();
            if (previewCamera != null) CapturePreviews(previewCamera);
        };
        Debug.Log($"MVP01 built: one metal floor plane, {boxCount} metal columns, central flora, first-person player. Scene: {ScenePath}");
    }

    [MenuItem("MVP01/Capture Preview")]
    public static void CaptureCurrentScene()
    {
        Camera camera = GameObject.Find("Main Camera")?.GetComponent<Camera>();
        if (camera == null) throw new InvalidOperationException("Open the MVP01 scene first.");
        CapturePreviews(camera);
    }

    [MenuItem("MVP01/Update Flower Circle and Board")]
    public static void UpdateFlowerCircleAndBoard()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            throw new InvalidOperationException("Open the MVP01 scene before updating its flower circle.");
        GameObject world = GameObject.Find("01  |  METAL COLUMN FIELD");
        if (world == null) throw new InvalidOperationException("The metal field root is missing.");

        MVP01FloraBuilder.Build(world.transform);
        MVP01BoardBuilder.Build(world.transform);
        if (!EditorSceneManager.SaveScene(scene))
            throw new IOException("Unity could not save the updated MVP01 scene.");
        AssetDatabase.SaveAssets();
        CaptureCurrentScene();
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
        material.SetColor("_ZenithColor", new Color(0.71f, 0.78f, 0.81f));
        material.SetColor("_UpperColor", new Color(0.91f, 0.94f, 0.94f));
        material.SetColor("_HorizonColor", new Color(0.99f, 1.0f, 0.98f));
        material.SetColor("_LowerColor", new Color(0.80f, 0.86f, 0.87f));
        material.SetFloat("_BandStrength", 0.012f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ConfigureRadialFog()
    {
        string materialPath = Root + "/Materials/Camera_Radial_Fog.mat";
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "/Shaders/CameraRadialFog.shader");
        if (shader == null) throw new InvalidOperationException("CameraRadialFog shader has not imported.");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "Camera Radial Fog" };
            AssetDatabase.CreateAsset(material, materialPath);
        }
        material.shader = shader;
        material.SetColor("_FogColor", new Color(0.83f, 0.88f, 0.89f));
        material.SetFloat("_FogStart", 7f);
        material.SetFloat("_FogEnd", 48f);
        material.SetFloat("_NoiseScale", 0.11f);
        material.SetFloat("_NoiseStrength", 14f);
        EditorUtility.SetDirty(material);

        const string rendererPath = "Assets/Settings/PC_Renderer.asset";
        UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
        if (rendererData == null) throw new InvalidOperationException("PC URP renderer asset is missing.");
        const string featureName = "MVP01 Camera Radial Fog";
        FullScreenPassRendererFeature feature = rendererData.rendererFeatures
            .OfType<FullScreenPassRendererFeature>()
            .FirstOrDefault(item => item.name == featureName);
        if (feature == null)
        {
            feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
            feature.name = featureName;
            AssetDatabase.AddObjectToAsset(feature, rendererData);
            rendererData.rendererFeatures.Add(feature);
        }
        feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;
        feature.fetchColorBuffer = true;
        feature.requirements = ScriptableRenderPassInput.Depth;
        feature.passMaterial = material;
        feature.passIndex = 0;
        feature.bindDepthStencilAttachment = false;
        feature.SetActive(true);
        feature.Create();

        // Keep URP's feature ID map in sync so the subasset survives an Editor reload.
        SerializedObject serializedRenderer = new SerializedObject(rendererData);
        SerializedProperty featureMap = serializedRenderer.FindProperty("m_RendererFeatureMap");
        featureMap.arraySize = rendererData.rendererFeatures.Count;
        for (int i = 0; i < rendererData.rendererFeatures.Count; i++)
        {
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(rendererData.rendererFeatures[i],
                    out string _, out long localId))
                featureMap.GetArrayElementAtIndex(i).longValue = localId;
        }
        serializedRenderer.ApplyModifiedPropertiesWithoutUndo();
        rendererData.SetDirty();
        EditorUtility.SetDirty(rendererData);
        EditorUtility.SetDirty(feature);
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
        bool concreteSurface = textureName == "concrete";
        material.SetFloat("_WorkflowMode", concreteSurface ? 0f : 1f);
        material.SetTexture("_MetallicGlossMap", concreteSurface ? null : Texture(textureName, "mask", true));
        material.SetTexture("_SpecGlossMap", concreteSurface ? Texture(textureName, "spec", true) : null);
        material.SetTexture("_OcclusionMap", Texture(textureName, "mask", true));
        material.SetTexture("_BumpMap", Texture(textureName, "normal", true));
        material.SetTexture("_ParallaxMap", concreteSurface ? Texture(textureName, "height", true) : null);
        material.SetFloat("_Parallax", concreteSurface ? 0.035f : 0.005f);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", concreteSurface ? 0.68f : smoothness);
        material.SetFloat("_BumpScale", concreteSurface ? 0.46f : 0.20f);
        material.SetFloat("_OcclusionStrength", 0.65f);
        if (concreteSurface) material.EnableKeyword("_SPECULAR_SETUP");
        else material.DisableKeyword("_SPECULAR_SETUP");
        if (concreteSurface) material.EnableKeyword("_PARALLAXMAP");
        else material.DisableKeyword("_PARALLAXMAP");
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
        float[,] largeScale = new float[resolution, resolution];
        float[,] directional = new float[resolution, resolution];
        int seed = surface == "concrete" ? 13 : surface == "steel" ? 47 : 71;
        for (int y = 0; y < resolution; y++)
        for (int x = 0; x < resolution; x++)
        {
            if (surface == "concrete")
            {
                float u = x / (resolution - 1f);
                float v = y / (resolution - 1f);
                float warpU = (SeamlessPerlin(u, v, 2f, 2f, seed + 9) - 0.5f) * 0.055f;
                float warpV = (SeamlessPerlin(u, v, 2f, 2f, seed + 23) - 0.5f) * 0.055f;
                u = Mathf.Repeat(u + warpU, 1f);
                v = Mathf.Repeat(v + warpV, 1f);

                // Four tileable Perlin octaves: large cement clouds, aggregate, grain, and pores.
                float macro = SeamlessPerlin(u, v, 2f, 2f, seed + 31);
                float aggregate = SeamlessPerlin(u, v, 5f, 6f, seed + 61);
                float grain = SeamlessPerlin(u, v, 16f, 19f, seed + 97);
                float pores = SeamlessPerlin(u, v, 42f, 43f, seed + 131);
                float stria = 0.68f * SeamlessPerlin(u, v, 2f, 26f, seed + 157)
                    + 0.32f * SeamlessPerlin(u, v, 5f, 53f, seed + 181);
                largeScale[x, y] = macro;
                directional[x, y] = stria;
                float poreCavity = Mathf.SmoothStep(0.56f, 0.73f, pores) * 0.11f;
                height[x, y] = Mathf.Clamp01(0.43f * macro + 0.27f * aggregate
                    + 0.17f * grain + 0.06f * pores + 0.07f * stria - poreCavity);
                continue;
            }
            if (surface == "steel")
            {
                float u = x / (resolution - 1f);
                float v = y / (resolution - 1f);
                float steelCloud = SeamlessPerlin(u, v, 5f, 5f, seed + 7);
                float grain = SeamlessPerlin(u, v, 38f, 38f, seed + 13);
                float brushing = SeamlessPerlin(u, v, 2f, 49f, seed + 19);
                height[x, y] = Mathf.Clamp01(0.54f * steelCloud + 0.20f * grain + 0.18f * brushing);
                continue;
            }
            float broad = Mathf.PerlinNoise((x + seed) * 0.025f, (y + seed) * 0.025f);
            float fine = Mathf.PerlinNoise((x + seed * 3) * 0.19f, (y + seed * 3) * 0.19f);
            height[x, y] = Mathf.Clamp01(broad * 0.67f + fine * 0.25f);
        }

        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, true, kind != "base");
        var pixels = new Color[resolution * resolution];
        for (int y = 0; y < resolution; y++)
        for (int x = 0; x < resolution; x++)
        {
            float h = height[x, y];
            float macro = largeScale[x, y];
            float stria = directional[x, y];
            float fleck = Mathf.PerlinNoise((x + seed) * 0.53f, (y - seed) * 0.53f);
            Color color;
            if (surface == "concrete" && kind == "base")
            {
                float value = Mathf.Clamp01(0.81f + (h - 0.48f) * 0.72f + (macro - 0.5f) * 0.13f);
                float warmth = (macro - 0.5f) * 0.025f;
                color = new Color(value + warmth, value + warmth * 0.2f, value - warmth, 1f);
            }
            else if (surface == "concrete" && kind == "spec")
            {
                // Directional Perlin changes specular F0 and smoothness along formwork grain.
                float specular = Mathf.Clamp(0.048f + (stria - 0.5f) * 0.055f, 0.025f, 0.075f);
                float gloss = Mathf.Clamp01(0.62f + (stria - 0.5f) * 0.30f + (macro - 0.5f) * 0.10f);
                color = new Color(specular, specular * 0.99f, specular * 0.96f, gloss);
            }
            else if (surface == "concrete" && kind == "height")
            {
                // Center the relief around 0.5; URP Lit reads green for parallax.
                float relief = Mathf.Clamp01(h + 0.10f);
                color = new Color(relief, relief, relief, 1f);
            }
            else if (kind == "base")
            {
                float value = 0.90f + (h - 0.5f) * 0.10f;
                if (surface == "paint") value *= fleck > 0.79f ? 0.69f : 1f;
                color = new Color(value, value, value, 1);
            }
            else if (kind == "mask")
            {
                float metal = surface == "concrete" ? 0.0f : surface == "paint" ? (fleck > 0.79f ? 0.9f : 0.08f) : 0.90f;
                float ao = surface == "concrete" ? Mathf.Lerp(0.77f, 1f, h) : Mathf.Lerp(0.84f, 1f, h);
                float gloss = 0.76f + h * 0.16f;
                color = new Color(metal, ao, surface == "concrete" ? stria : 0f, gloss);
            }
            else
            {
                float dx = height[(x + 1) % resolution, y] - height[(x + resolution - 1) % resolution, y];
                float dy = height[x, (y + 1) % resolution] - height[x, (y + resolution - 1) % resolution];
                float striaDy = surface == "concrete"
                    ? directional[x, (y + 1) % resolution] - directional[x, (y + resolution - 1) % resolution]
                    : 0f;
                Vector3 n = new Vector3(-dx * 1.3f, -(dy + striaDy * 0.22f) * 1.3f, 1f).normalized;
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

    private static float SeamlessPerlin(float u, float v, float scaleU, float scaleV, int seed)
    {
        float offsetU = seed * 0.173f;
        float offsetV = seed * 0.311f;
        float a = Mathf.PerlinNoise(offsetU + u * scaleU, offsetV + v * scaleV);
        float b = Mathf.PerlinNoise(offsetU + (u - 1f) * scaleU, offsetV + v * scaleV);
        float c = Mathf.PerlinNoise(offsetU + u * scaleU, offsetV + (v - 1f) * scaleV);
        float d = Mathf.PerlinNoise(offsetU + (u - 1f) * scaleU, offsetV + (v - 1f) * scaleV);
        return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
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
        Capture(camera, "MVP01_MetalField.png", V(0, 1.75f, -17), V(0, 1.4f, 5));
        Capture(camera, "MVP01_MetalFloor.png", V(-3, 1.75f, -2), V(4, 0.9f, 8));
        Capture(camera, "MVP01_MetalColumns.png", V(8, 1.75f, 4), V(19, 6, 8));
        Capture(camera, "MVP01_FloraDetail.png", V(1.5f, 1.55f, -2.5f), V(0, 1.3f, 3));
        Capture(camera, "MVP01_PaintingBoard.png", V(0.3f, 1.7f, -4.2f), V(0, 1.9f, 0));
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
