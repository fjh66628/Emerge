using System;
using MVP03;
using MVP04;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class MVP03Presentation
{
    public const string SunSettingsPath = "Assets/MVP03/Rendering/CourtyardSun.asset";
    public const string ScenePath = "Assets/MVP03/Scenes/MVP03_StainedGlassChapel.unity";
    public const string WalkSetPath = "Assets/MVP04/Textures/PilgrimWalk.asset";
    public const string WalkMaterialPath = "Assets/MVP03/Materials/PilgrimWalk_Lit.mat";
    public const float CameraPitch = 18f;

    [MenuItem("MVP03/Apply Front Camera and Lit Characters")]
    public static void Apply()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != ScenePath)
            throw new InvalidOperationException("Open MVP03 outside Play mode first.");
        ConfigureScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("MVP03/Use MVP04 Animated Character")]
    public static void ApplyAnimatedCharacter()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != ScenePath)
            throw new InvalidOperationException("Open MVP03 outside Play mode first.");
        var hero = UnityEngine.Object.FindFirstObjectByType<PixelPilgrim>();
        var camera = Camera.main;
        if (hero == null || camera == null) throw new InvalidOperationException("The courtyard camera and pilgrim must exist first.");
        ConfigurePlayerAnimation(hero, camera);
        var portrait = hero.GetComponentInChildren<SpriteRenderer>();
        ConfigurePortrait(portrait);
        FaceCamera(portrait, camera);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    public static void ConfigureScene()
    {
        var camera = Camera.main;
        var hero = UnityEngine.Object.FindFirstObjectByType<PixelPilgrim>();
        if (camera == null || hero == null) throw new InvalidOperationException("The courtyard camera and pilgrim must exist first.");
        Undo.RegisterFullObjectHierarchyUndo(camera.gameObject, "Align courtyard camera");
        float distance = Mathf.Clamp(Vector3.Distance(camera.transform.position, hero.transform.position + Vector3.up), 5, 15);
        camera.transform.rotation = Quaternion.Euler(CameraPitch, 0, 0);
        camera.transform.position = hero.transform.position + Vector3.up - camera.transform.forward * distance;
        var follow = camera.GetComponent<PixelFollowCamera>();
        if (follow == null) follow = Undo.AddComponent<PixelFollowCamera>(camera.gameObject);
        follow.enabled = true;
        follow.Configure(hero.transform);
        follow.ConfigureZoom(5, 15);
        EditorUtility.SetDirty(follow);
        if (!camera.TryGetComponent<CameraPitchUI>(out _)) Undo.AddComponent<CameraPitchUI>(camera.gameObject);

        ConfigurePlayerAnimation(hero, camera);
        var scene = camera.gameObject.scene;
        foreach (var portrait in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (portrait.gameObject.scene != scene || portrait.sprite == null) continue;
            if (portrait.GetComponentInParent<PixelPilgrim>() == null && portrait.GetComponentInParent<PixelBillboard>() == null) continue;
            ConfigurePortrait(portrait);
            FaceCamera(portrait, camera);
        }
        ConfigureSun();
    }

    private static void ConfigurePlayerAnimation(PixelPilgrim hero, Camera camera)
    {
        var set = AssetDatabase.LoadAssetAtPath<PixelWalkSet>(WalkSetPath);
        if (set == null) throw new InvalidOperationException("Import the MVP04 PilgrimWalk animation set first.");
        foreach (PixelFacing facing in Enum.GetValues(typeof(PixelFacing)))
        {
            var frames = set.Frames(facing);
            if (frames == null || frames.Length != 6 || Array.Exists(frames, frame => frame == null))
                throw new InvalidOperationException("The MVP04 walk set needs six frames in each of its four directions.");
        }
        Undo.RegisterFullObjectHierarchyUndo(hero.gameObject, "Use MVP04 animated character");
        var portrait = hero.GetComponentInChildren<SpriteRenderer>();
        if (!portrait.TryGetComponent<PixelWalkAnimation>(out var animation))
            animation = Undo.AddComponent<PixelWalkAnimation>(portrait.gameObject);
        animation.enabled = true;
        animation.Configure(set);
        hero.Configure(camera, portrait);
        EditorUtility.SetDirty(animation);
        EditorUtility.SetDirty(hero);
    }

    private static void FaceCamera(SpriteRenderer portrait, Camera camera)
    {
        Vector3 facing = camera.transform.position - portrait.transform.position;
        facing.y = 0;
        bool directional = portrait.TryGetComponent<PixelWalkAnimation>(out var animation) && animation.IsConfigured;
        // Match PixelPilgrim.LateUpdate so left/right frames are not mirrored in Edit mode.
        if (facing.sqrMagnitude > .001f) portrait.transform.rotation = Quaternion.LookRotation(directional ? -facing : facing);
    }

    private static void ConfigurePortrait(SpriteRenderer portrait)
    {
        Undo.RegisterFullObjectHierarchyUndo(portrait.gameObject, "Enable courtyard character lighting");
        bool animated = portrait.TryGetComponent<PixelWalkAnimation>(out var animation) && animation.IsConfigured;
        // A stable material path covers all 24 frames, including subsequent scene rebuilds.
        string path = animated ? WalkMaterialPath : "Assets/MVP03/Materials/" + portrait.sprite.name + "_Lit.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("MVP04/Lit Pixel Character")) { name = animated ? "PilgrimWalk_Lit" : portrait.sprite.name + "_Lit" };
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Cutoff", .5f);
            material.SetFloat("_Smoothness", .12f);
            material.SetFloat("_NormalBend", 1.5f);
            material.SetFloat("_BackLight", .35f);
            material.SetFloat("_AmbientStrength", 1);
            material.SetFloat("_LightFacingShadow", 1);
            material.renderQueue = 2450;
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetTexture("_BaseMap", portrait.sprite.texture);
        portrait.sharedMaterial = material;
        portrait.receiveShadows = true;
        portrait.shadowCastingMode = ShadowCastingMode.TwoSided;
        if (!portrait.TryGetComponent<PixelCharacterShadow>(out var shadow)) shadow = Undo.AddComponent<PixelCharacterShadow>(portrait.gameObject);
        shadow.enabled = true;
        EditorUtility.SetDirty(material);
        EditorUtility.SetDirty(portrait);
    }

    private static void ConfigureSun()
    {
        var light = RenderSettings.sun;
        if (light == null) throw new InvalidOperationException("The courtyard needs its directional sun.");
        var settings = AssetDatabase.LoadAssetAtPath<CourtyardSunSettings>(SunSettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<CourtyardSunSettings>();
            settings.sourceEnabled = light.enabled;
            settings.rotation = light.transform.eulerAngles;
            settings.colour = light.color;
            settings.intensity = light.intensity;
            settings.shadowStrength = light.shadowStrength;
            AssetDatabase.CreateAsset(settings, SunSettingsPath);
        }
        if (!light.TryGetComponent<CourtyardSun>(out var driver)) driver = Undo.AddComponent<CourtyardSun>(light.gameObject);
        driver.settings = settings;
        driver.enabled = true;
        driver.Apply();
        EditorUtility.SetDirty(driver);
    }
}
