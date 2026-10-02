using System;
using System.IO;
using MVP03;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class MVP03MagicBuilder
{
    private const string Root = "Assets/MVP03";

    [MenuItem("MVP03/Add or Rebuild Magic Attack")]
    public static void ConfigureScene()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode before rebuilding magic assets.");
        var player = UnityEngine.Object.FindFirstObjectByType<PixelPilgrim>();
        if (player == null) throw new InvalidOperationException("Open the MVP03 courtyard first.");
        Directory.CreateDirectory(Root + "/Prefabs");
        AssetDatabase.Refresh();

        Material core = Material("MagicCore", "Universal Render Pipeline/Lit");
        core.SetColor("_BaseColor", new Color(.12f, .48f, .65f));
        core.SetFloat("_Smoothness", .82f);
        core.SetFloat("_Metallic", .25f);
        core.SetColor("_EmissionColor", new Color(1.6f, 3.8f, 4.8f));
        core.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        core.EnableKeyword("_EMISSION");
        EditorUtility.SetDirty(core);
        Material aura = Glow("MagicAura", new Color(.12f, 1.6f, 2.8f, .75f), 1);
        Material ribbon = Glow("MagicRibbon", new Color(.25f, 2.3f, 3.2f, .85f), 0);
        Material gold = Glow("MagicOrbit", new Color(3.5f, 2.5f, .8f, .9f), 0);
        Mesh ring = RingMesh();

        var impactRoot = new GameObject("Magic Impact 3D");
        Transform flash = Sphere("Expanding shell", impactRoot.transform, aura, .28f);
        Transform halo = Ring("Surface ripple", impactRoot.transform, ring, gold);
        Light impactLight = PointLight(impactRoot.transform, 9f, 3.3f);
        Particles(impactRoot.transform, ribbon, true);
        var impact = impactRoot.AddComponent<MagicImpact>();
        impact.Configure(flash, halo, impactLight);
        var impactAsset = PrefabUtility.SaveAsPrefabAsset(impactRoot, Root + "/Prefabs/MagicImpact.prefab").GetComponent<MagicImpact>();
        UnityEngine.Object.DestroyImmediate(impactRoot);

        var boltRoot = new GameObject("Lumen / 3D Magic Bolt");
        Sphere("Solid luminous core", boltRoot.transform, core, .29f);
        Transform shell = Sphere("Fresnel atmosphere", boltRoot.transform, aura, .60f);
        Transform orbits = new GameObject("Orbit rotation").transform;
        orbits.SetParent(boltRoot.transform, false);
        Ring("Gold orbit", orbits, ring, gold).localRotation = Quaternion.Euler(26, 12, 0);
        Ring("Blue orbit", orbits, ring, ribbon).localRotation = Quaternion.Euler(105, 35, 0);
        var trailObject = new GameObject("Fading light ribbon");
        trailObject.transform.SetParent(boltRoot.transform, false);
        var trail = trailObject.AddComponent<TrailRenderer>();
        trail.sharedMaterial = ribbon;
        trail.time = .24f;
        trail.minVertexDistance = .035f;
        trail.widthCurve = new AnimationCurve(new Keyframe(0, .23f), new Keyframe(.35f, .12f), new Keyframe(1, 0));
        trail.colorGradient = FadeGradient();
        trail.numCapVertices = 4;
        trail.numCornerVertices = 3;
        trail.alignment = LineAlignment.View;
        trail.shadowCastingMode = ShadowCastingMode.Off;
        trail.receiveShadows = false;
        var motes = Particles(boltRoot.transform, ribbon, false);
        var light = PointLight(boltRoot.transform, 5.5f, 3f);
        var bolt = boltRoot.AddComponent<MagicBolt>();
        bolt.Configure(impactAsset, shell, orbits, trail, motes, light);
        var boltAsset = PrefabUtility.SaveAsPrefabAsset(boltRoot, Root + "/Prefabs/MagicBolt.prefab").GetComponent<MagicBolt>();
        UnityEngine.Object.DestroyImmediate(boltRoot);

        var caster = player.GetComponent<MagicBoltCaster>();
        if (caster == null) caster = player.gameObject.AddComponent<MagicBoltCaster>();
        caster.Configure(boltAsset);
        // Query the visible stone/wood surface so impacts do not disappear inside the
        // courtyard's coarse walking colliders. Exclude contact pairs to retain its movement setup.
        foreach (string name in new[] { "RebuiltArchitecture", "RebuiltStairs", "RebuiltCarvedEdges", "RebuiltWoodwork", "RebuiltTreeBranches" })
        {
            GameObject surface = GameObject.Find(name);
            if (surface == null) continue;
            var collider = surface.GetComponent<MeshCollider>();
            if (collider == null) collider = surface.AddComponent<MeshCollider>();
            collider.sharedMesh = surface.GetComponent<MeshFilter>().sharedMesh;
            collider.excludeLayers = ~0;
            EditorUtility.SetDirty(collider);
        }
        EditorUtility.SetDirty(caster);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        AssetDatabase.SaveAssets();
    }

    private static Material Material(string name, string shader)
    {
        string path = Root + "/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find(shader)) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        return material;
    }

    private static Material Glow(string name, Color color, float rim)
    {
        Material material = Material(name, "MVP03/Magic Glow");
        material.SetColor("_BaseColor", color);
        material.SetFloat("_RimWeight", rim);
        material.SetFloat("_RimPower", 2.2f);
        material.SetFloat("_Opacity", 1);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Transform Sphere(string name, Transform parent, Material material, float diameter)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.one * diameter;
        Surface(go.GetComponent<MeshRenderer>(), material);
        return go.transform;
    }

    private static Transform Ring(string name, Transform parent, Mesh mesh, Material material)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        Surface(go.AddComponent<MeshRenderer>(), material);
        return go.transform;
    }

    private static void Surface(Renderer renderer, Material material)
    {
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    private static Light PointLight(Transform parent, float intensity, float range)
    {
        var go = new GameObject("Local cyan light");
        go.transform.SetParent(parent, false);
        Light light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(.30f, .78f, 1f);
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
        return light;
    }

    private static Gradient FadeGradient()
    {
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(.08f, .48f, 1f), 1) },
            new[] { new GradientAlphaKey(.9f, 0), new GradientAlphaKey(.6f, .3f), new GradientAlphaKey(0, 1) });
        return gradient;
    }

    private static ParticleSystem Particles(Transform parent, Material material, bool burst)
    {
        var go = new GameObject(burst ? "Impact fragments / mesh particles" : "Trailing motes / mesh particles");
        go.transform.SetParent(parent, false);
        var particles = go.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.loop = !burst;
        main.duration = 1;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(.22f, burst ? .55f : .4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(burst ? 1.5f : .05f, burst ? 3.5f : .35f);
        main.startSize = new ParticleSystem.MinMaxCurve(.025f, .06f);
        main.maxParticles = 64;
        main.gravityModifier = burst ? .2f : -.02f;
        var emission = particles.emission;
        emission.rateOverTime = burst ? 0 : 38;
        if (burst) emission.SetBursts(new[] { new ParticleSystem.Burst(0, 22) });
        var shape = particles.shape;
        shape.shapeType = burst ? ParticleSystemShapeType.Hemisphere : ParticleSystemShapeType.Sphere;
        shape.radius = burst ? .12f : .17f;
        var colors = particles.colorOverLifetime;
        colors.enabled = true;
        colors.color = FadeGradient();
        var size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0));
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        var primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        renderer.mesh = primitive.GetComponent<MeshFilter>().sharedMesh;
        UnityEngine.Object.DestroyImmediate(primitive);
        Surface(renderer, material);
        return particles;
    }

    private static Mesh RingMesh()
    {
        const int segments = 64, sides = 6;
        var vertices = new Vector3[(segments + 1) * (sides + 1)];
        var normals = new Vector3[vertices.Length];
        var colors = new Color[vertices.Length];
        var indices = new int[segments * sides * 6];
        int index = 0;
        for (int i = 0; i <= segments; i++) for (int j = 0; j <= sides; j++)
        {
            float a = i * Mathf.PI * 2 / segments, b = j * Mathf.PI * 2 / sides;
            int k = i * (sides + 1) + j;
            Vector3 radial = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
            normals[k] = radial * Mathf.Cos(b) + Vector3.forward * Mathf.Sin(b);
            vertices[k] = radial * .27f + normals[k] * .009f;
            colors[k] = Color.white;
            if (i == segments || j == sides) continue;
            indices[index++] = k; indices[index++] = k + sides + 1; indices[index++] = k + 1;
            indices[index++] = k + 1; indices[index++] = k + sides + 1; indices[index++] = k + sides + 2;
        }
        var generated = new Mesh { name = "MagicOrbit", vertices = vertices, normals = normals, colors = colors, triangles = indices };
        generated.RecalculateBounds();
        string path = Root + "/Meshes/MagicOrbit.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null) { AssetDatabase.CreateAsset(generated, path); return generated; }
        EditorUtility.CopySerialized(generated, existing);
        UnityEngine.Object.DestroyImmediate(generated);
        EditorUtility.SetDirty(existing);
        return existing;
    }

    [MenuItem("MVP03/Capture Magic Preview")]
    public static void CapturePreview()
    {
        Camera camera = Camera.main;
        if (camera == null) throw new InvalidOperationException("Open the MVP03 courtyard first.");
        var output = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
        var pixels = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        RenderTexture oldTarget = camera.targetTexture, oldActive = RenderTexture.active;
        try
        {
            camera.targetTexture = output;
            camera.Render();
            RenderTexture.active = output;
            pixels.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            pixels.Apply();
            Directory.CreateDirectory("Previews");
            File.WriteAllBytes("Previews/MVP03_Magic.png", pixels.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = oldTarget;
            RenderTexture.active = oldActive;
            UnityEngine.Object.DestroyImmediate(pixels);
            UnityEngine.Object.DestroyImmediate(output);
        }
    }
}
