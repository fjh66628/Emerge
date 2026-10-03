using System;
using System.Linq;
using MVP03;
using MVP04;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class MVP03WarmLighting
{
    public const string RendererPath = "Assets/MVP03/Rendering/RebuiltRenderer.asset";
    public const string MaterialPath = "Assets/MVP03/Materials/WarmAirVolume.mat";
    private const string ScenePath = "Assets/MVP03/Scenes/MVP03_StainedGlassChapel.unity";

    [MenuItem("MVP03/Apply Warm Volumetric Light and Camera")]
    public static void Apply()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != ScenePath)
            throw new InvalidOperationException("Open the MVP03 courtyard outside Play mode first.");
        ConfigureScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("MVP03: warm shadowed sunlight, moving air, character follow and Q/E/wheel zoom.");
    }

    // Also used by the courtyard builder so rebuilding retains the migrated design.
    public static void ConfigureScene()
    {
        Camera camera = Camera.main;
        var hero = UnityEngine.Object.FindFirstObjectByType<PixelPilgrim>();
        var sun = RenderSettings.sun;
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (camera == null || hero == null || sun == null || renderer == null)
            throw new InvalidOperationException("Build the reference courtyard before applying its warm light preset.");

        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("MVP04/Window Volume")) { name = "WarmAirVolume" };
            material.SetFloat("_Density", .022f);
            material.SetFloat("_ScatteringAlbedo", .92f);
            material.SetFloat("_Anisotropy", .35f);
            material.SetFloat("_NoiseAmount", .7f);
            material.SetFloat("_NoiseScale", .38f);
            material.SetFloat("_FlowSpeed", .18f);
            material.SetVector("_FlowDirection", new Vector4(.8f, .2f, .35f, 0));
            material.SetFloat("_FlowWarp", 1.1f);
            material.SetFloat("_FlowDetail", .35f);
            material.SetTexture("_DensityNoise", AssetDatabase.LoadAssetAtPath<Texture3D>("Assets/MVP04/Textures/AirFlowNoise.asset"));
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        var feature = renderer.rendererFeatures.OfType<WindowVolumeFeature>().FirstOrDefault();
        if (feature == null)
        {
            feature = ScriptableObject.CreateInstance<WindowVolumeFeature>();
            feature.name = "Warm courtyard / MVP04 single scattering";
            feature.resolutionDivisor = 3;
            feature.viewSteps = 32;
            feature.lightSteps = 1;
            feature.denoiseStrength = .9f;
            AssetDatabase.AddObjectToAsset(feature, renderer);
            renderer.rendererFeatures.Add(feature);
        }
        feature.material = material;
        feature.mediumMin = new Vector3(-13.9f, 0, -30.4f);
        feature.mediumMax = new Vector3(12, 15, 3.4f);
        feature.includeMainDirectionalLight = true;
        feature.SetActive(true);
        feature.Create();
        EditorUtility.SetDirty(feature);
        renderer.SetDirty();
        EditorUtility.SetDirty(renderer);

        Undo.RecordObjects(new UnityEngine.Object[] { sun, sun.transform, camera, camera.transform, hero }, "MVP03 warm sunlight and follow");
        sun.type = LightType.Directional;
        sun.transform.rotation = Quaternion.LookRotation(new Vector3(.18f, -.64f, -.746f));
        sun.color = new Color(1, .90f, .72f);
        sun.intensity = 3.4f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 1;
        sun.shadowBias = .035f;
        sun.shadowNormalBias = .08f;
        sun.enabled = true;
        RenderSettings.fog = false; // Camera-path extinction is already part of the shared volume integrator.
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.40f, .45f, .49f);
        RenderSettings.ambientEquatorColor = new Color(.27f, .29f, .30f);
        RenderSettings.ambientGroundColor = new Color(.16f, .14f, .11f);
        ConfigureWallBacking();

        hero.enabled = true;
        hero.GetComponent<CharacterController>().enabled = true;
        hero.Configure(camera, hero.GetComponentInChildren<SpriteRenderer>());
        var follow = camera.GetComponent<PixelFollowCamera>();
        if (follow == null) follow = Undo.AddComponent<PixelFollowCamera>(camera.gameObject);
        follow.enabled = true;
        follow.Configure(hero.transform);
        follow.ConfigureZoom(5, 15);
        if (!camera.TryGetComponent<ChapelControlsHint>(out var hint)) hint = Undo.AddComponent<ChapelControlsHint>(camera.gameObject);
        hint.enabled = true;
        camera.GetUniversalAdditionalCameraData().requiresDepthTexture = true;
        var atmosphere = UnityEngine.Object.FindFirstObjectByType<Volume>();
        if (atmosphere != null && atmosphere.sharedProfile.TryGet(out DepthOfField depth))
        {
            // Keep the close zoom usable while retaining a restrained HD2D focus separation.
            depth.focalLength.Override(70);
            depth.aperture.Override(4);
            depth.focusDistance.Override(Vector3.Dot(hero.transform.position + Vector3.up - camera.transform.position, camera.transform.forward));
            EditorUtility.SetDirty(depth);
            EditorUtility.SetDirty(atmosphere.sharedProfile);
        }
        EditorUtility.SetDirty(sun);
        EditorUtility.SetDirty(follow);
        EditorUtility.SetDirty(hero);
    }

    private static void ConfigureWallBacking()
    {
        // The original masonry is made of separated stones. Fill its joints with opaque
        // mortar so they cannot expose the clear colour or transmit sunlight through a solid wall.
        const string name = "Warm light / opaque mortar backing";
        if (GameObject.Find(name) != null) return;
        var root = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(root, "Close courtyard mortar joints");
        Material mortar = AssetDatabase.LoadAssetAtPath<Material>("Assets/MVP03/Materials/RebuiltMortar.mat");
        Backing("Church wall core", new Vector3(0, 7, 2.5f), new Vector3(27, 14, 1.2f));
        Backing("West wall core", new Vector3(-10.2f, 5.5f, -14), new Vector3(1.2f, 11, 29));
        void Backing(string label, Vector3 position, Vector3 size)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = label;
            wall.transform.SetParent(root.transform, false);
            wall.transform.position = position;
            wall.transform.localScale = size;
            UnityEngine.Object.DestroyImmediate(wall.GetComponent<Collider>());
            wall.GetComponent<Renderer>().sharedMaterial = mortar;
        }
    }

    [MenuItem("MVP03/Select Warm Volume Settings")]
    public static void SelectSettings()
    {
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        EditorGUIUtility.PingObject(Selection.activeObject);
    }
}
