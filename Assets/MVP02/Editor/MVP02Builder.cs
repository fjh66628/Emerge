using System;
using System.IO;
using System.Linq;
using MVP01;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class MVP02Builder
{
    private const string Root = "Assets/MVP02";
    private const string ScenePath = Root + "/Scenes/MVP02_BinaryWalk.unity";
    private const string RendererPath = Root + "/Rendering/MVP02_Renderer.asset";

    [MenuItem("MVP02/Build Binary Walk")]
    public static void Build()
    {
        Directory.CreateDirectory(Root + "/Scenes");
        Directory.CreateDirectory(Root + "/Materials");
        Directory.CreateDirectory(Root + "/Rendering");
        Directory.CreateDirectory("Previews");

        ConfigureRenderer();
        Material white = MakeSurface("01_ChalkWhite", new Color(0.975f, 0.973f, 0.952f), 0.4f);
        Material warm = MakeSurface("02_WarmWhite", new Color(0.93f, 0.927f, 0.892f), 0.4f);
        Material yellow = MakeSurface("03_AcidYellow", new Color(0.68f, 1f, 0.025f), 0.4f);
        Material black = MakeSurface("04_DeepBlack", new Color(0.032f, 0.035f, 0.035f), 0.98f);
        Material sky = MakeSky();

        Scene previous = SceneManager.GetActiveScene();
        if (previous.IsValid() && previous.isDirty && !string.IsNullOrEmpty(previous.path))
            EditorSceneManager.SaveScene(previous);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SetActiveScene(scene);

        GameObject map = new GameObject("01  |  BINARY CUBE MAP");
        Box("White void / walkable foundation", V(0, -0.3f, 0), V(78, 0.6f, 78), white, map.transform);

        GameObject core = Group("Central floating composition", map.transform);
        Box("Dark inset beneath monolith", V(0, 0.035f, 5), V(9.7f, 0.07f, 9.7f), black, core.transform);
        Box("Monolith / main white cube", V(0, 3.0f, 5), V(7, 6, 7), white, core.transform);
        Box("Monolith / upper offset white cube", V(-0.7f, 6.35f, 5.1f), V(6.2f, 0.7f, 6.2f), warm, core.transform);
        Box("Floating acid yellow crown", V(0.6f, 8.4f, 5), V(8.4f, 0.28f, 7.1f), yellow, core.transform);
        Box("Floating white shadow sliver", V(2.5f, 7.2f, 3.4f), V(5.4f, 0.16f, 2.1f), white, core.transform);
        Box("Black suspended underside", V(-0.8f, 4.35f, -0.5f), V(3.2f, 0.16f, 2.8f), black, core.transform);

        GameObject route = Group("Walkable cubic route", map.transform);
        Box("Arrival plinth", V(11, 3.9f, -16), V(7.5f, 7.8f, 7.5f), white, route.transform);
        Box("Arrival yellow lip", V(11, 7.85f, -19.65f), V(7.5f, 0.10f, 0.24f), yellow, route.transform);
        for (int i = 0; i < 12; i++)
        {
            float height = (i + 1) * 0.65f;
            Box("Overlook steps " + i, V(11, height * 0.5f, -28.7f + i * 0.78f),
                V(4.0f, height, 0.84f), i == 5 ? yellow : white, route.transform);
        }
        Box("Low white causeway", V(11, 0.12f, -5), V(3.6f, 0.24f, 13), warm, route.transform);
        Box("Black slot in causeway", V(11, 0.251f, -6), V(2.6f, 0.025f, 0.32f), black, route.transform);

        GameObject field = Group("Asymmetric white cube field", map.transform);
        Box("West tower", V(-23, 4.2f, -5), V(5.5f, 8.4f, 5.5f), white, field.transform);
        Box("West lower mass", V(-12, 1.5f, 1), V(7, 3, 6), warm, field.transform);
        Box("West yellow reveal", V(-12, 3.02f, 1), V(7, 0.08f, 6), yellow, field.transform);
        Box("North tower", V(-11, 4.6f, 20), V(4.4f, 9.2f, 4.4f), white, field.transform);
        Box("North bridge mass", V(1.5f, 2.4f, 20), V(12, 4.8f, 4), white, field.transform);
        Box("East isolated cube", V(18, 3.1f, 8), V(6, 6.2f, 6), white, field.transform);
        Box("East cube yellow cap", V(18.3f, 6.35f, 7.8f), V(6.5f, 0.22f, 6.5f), yellow, field.transform);
        Box("South low cube", V(-8, 1.2f, -16), V(7, 2.4f, 7), white, field.transform);
        Box("South distant monolith", V(2, 5.4f, -28), V(3.5f, 10.8f, 3.5f), warm, field.transform);
        Box("East tall monolith", V(28, 6.8f, -4), V(4, 13.6f, 4), white, field.transform);
        Box("West far slab", V(-27, 1.2f, 13), V(8, 2.4f, 10), warm, field.transform);
        Box("Floating black marker", V(-16, 5.4f, 9), V(4.2f, 0.16f, 3.1f), black, field.transform);
        Box("Floating white marker", V(14, 9.1f, 20), V(7.4f, 0.25f, 3.2f), white, field.transform);

        GameObject sunObject = new GameObject("02  |  HARD CUT SUN");
        sunObject.transform.rotation = Quaternion.Euler(42, 35, 0);
        Light sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = Color.white;
        sun.intensity = 1.15f;
        sun.shadows = LightShadows.Hard;
        sun.shadowStrength = 1f;
        sun.shadowBias = 0.015f;
        sun.shadowNormalBias = 0.15f;
        RenderSettings.sun = sun;
        RenderSettings.skybox = sky;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.white;
        RenderSettings.fog = false;
        RenderSettings.reflectionIntensity = 0f;

        GameObject player = new GameObject("03  |  FIRST PERSON WALKER");
        player.transform.position = V(11, 7.88f, -16);
        player.transform.rotation = Quaternion.Euler(0, -28, 0);
        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.35f;
        controller.center = V(0, 0.9f, 0);
        controller.stepOffset = 0.7f;
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(player.transform);
        cameraObject.transform.localPosition = V(0, 1.65f, 0);
        cameraObject.transform.localRotation = Quaternion.Euler(18, 0, 0);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.fieldOfView = 64f;
        camera.nearClipPlane = 0.07f;
        camera.farClipPlane = 130f;
        camera.allowHDR = false;
        cameraObject.AddComponent<AudioListener>();
        UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.SetRenderer(1);
        cameraData.renderPostProcessing = false;
        player.AddComponent<FirstPersonWalk>();

        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new IOException("Could not save MVP02 scene.");
        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(s => s.path == ScenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
        AssetDatabase.SaveAssets();
        Debug.Log("MVP02 built: binary shader, discrete white sky, white cube map and hard black shadows.");
    }

    [MenuItem("MVP02/Capture Preview")]
    public static void CaptureCurrentScene()
    {
        if (SceneManager.GetActiveScene().path != ScenePath)
            throw new InvalidOperationException("Open the MVP02 scene before capturing.");
        Camera camera = GameObject.Find("Main Camera")?.GetComponent<Camera>();
        if (camera == null) throw new InvalidOperationException("MVP02 camera is missing.");
        Directory.CreateDirectory("Previews");
        Vector3 position = camera.transform.position;
        Quaternion rotation = camera.transform.rotation;
        Capture(camera, "MVP02_Walk.png", position, rotation);
        camera.transform.position = V(13, 11.5f, -13);
        camera.transform.LookAt(V(0, 4, 5));
        Capture(camera, "MVP02_Composition.png", camera.transform.position, camera.transform.rotation);
        camera.transform.SetPositionAndRotation(position, rotation);
    }

    private static void ConfigureRenderer()
    {
        if (AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(RendererPath) == null)
            if (!AssetDatabase.CopyAsset("Assets/Settings/Mobile_Renderer.asset", RendererPath))
                throw new IOException("Could not create fog-free MVP02 renderer.");
        ScriptableRendererData renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(RendererPath);
        renderer.name = "MVP02_Renderer";
        UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
        SerializedObject serialized = new SerializedObject(pipeline);
        SerializedProperty list = serialized.FindProperty("m_RendererDataList");
        if (list == null || list.arraySize < 1) throw new InvalidOperationException("PC URP renderer list is unavailable.");
        if (list.arraySize < 2) list.arraySize = 2;
        list.GetArrayElementAtIndex(1).objectReferenceValue = renderer;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(pipeline);
        AssetDatabase.SaveAssets();
    }

    private static Material MakeSurface(string name, Color baseColor, float cutoff)
    {
        string path = Root + "/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("MVP02/Binary Surface"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", baseColor);
        material.SetColor("_ShadowColor", new Color(0.025f, 0.027f, 0.028f));
        material.SetFloat("_Threshold", cutoff);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material MakeSky()
    {
        string path = Root + "/Materials/CutPaperSky.mat";
        Material sky = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (sky == null)
        {
            sky = new Material(Shader.Find("MVP02/Cut Paper Sky"));
            AssetDatabase.CreateAsset(sky, path);
        }
        EditorUtility.SetDirty(sky);
        return sky;
    }

    private static GameObject Group(string name, Transform parent)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent);
        return group;
    }

    private static GameObject Box(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent);
        box.transform.position = position;
        box.transform.localScale = scale;
        box.GetComponent<MeshRenderer>().sharedMaterial = material;
        box.isStatic = true;
        return box;
    }

    private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

    private static void Capture(Camera camera, string filename, Vector3 position, Quaternion rotation)
    {
        RenderTexture target = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
        RenderTexture oldTarget = camera.targetTexture;
        RenderTexture oldActive = RenderTexture.active;
        camera.transform.SetPositionAndRotation(position, rotation);
        camera.targetTexture = target;
        camera.Render();
        camera.Render();
        RenderTexture.active = target;
        Texture2D pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        pixels.Apply();
        File.WriteAllBytes("Previews/" + filename, pixels.EncodeToPNG());
        camera.targetTexture = oldTarget;
        RenderTexture.active = oldActive;
        UnityEngine.Object.DestroyImmediate(pixels);
        UnityEngine.Object.DestroyImmediate(target);
    }
}
