using System;
using System.IO;
using MVP03;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class MVP04Builder
{
    [MenuItem("MVP04/Apply Baroque Astral Magic")]
    public static void ApplyBaroqueMagic()
    {
        var hero = UnityEngine.Object.FindFirstObjectByType<PixelPilgrim>();
        if (Application.isPlaying || hero == null || hero.gameObject.scene.path != ScenePath)
            throw new InvalidOperationException("Open MVP04 outside Play mode first.");
        var caster = hero.GetComponent<MagicBoltCaster>();
        if (caster == null) caster = hero.gameObject.AddComponent<MagicBoltCaster>();
        Undo.RecordObject(caster, "Set baroque astral magic");
        ConfigureChapelMagic(caster);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(hero.gameObject.scene);
        EditorSceneManager.SaveScene(hero.gameObject.scene);
    }

    private static void ConfigureChapelMagic(MagicBoltCaster caster)
    {
        Directory.CreateDirectory(Root + "/Prefabs");
        AssetDatabase.Refresh();
        string texturePath = Root + "/Textures/BaroqueSigil.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Trilinear;
        importer.mipmapEnabled = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 1024;
        importer.SaveAndReimport();

        var pattern = Material("BaroqueSigil", "MVP04/Baroque Sigil");
        pattern.SetTexture("_Pattern", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
        pattern.SetColor("_Radiance", new Color(2, 1.65f, .9f, 1));
        pattern.SetFloat("_Reveal", 0); pattern.SetFloat("_Opacity", 0);
        EditorUtility.SetDirty(pattern);

        var core = Lit("AstralOrbCore", new Color(.68f, .76f, .88f), .25f);
        Emission(core, new Color(1.8f, 2.3f, 3.1f));
        var aura = MagicGlowMaterial("AstralOrbHalo", new Color(.38f, .8f, 1.6f, .32f), 1);
        var ripple = MagicGlowMaterial("AstralImpactHalo", new Color(1.4f, 1.7f, 2.4f, .65f), 0);
        var stars = Material("AstralStars", "MVP04/Star Mote");
        stars.SetColor("_BaseColor", new Color(2.1f, 2.4f, 3.1f, 1)); stars.SetFloat("_Opacity", 1);
        EditorUtility.SetDirty(stars);
        var mist = Material("AstralNebula", "MVP04/Astral Trail");
        mist.SetColor("_BaseColor", new Color(1.1f, 1.1f, 2.2f, .6f)); mist.SetFloat("_Opacity", .75f);
        EditorUtility.SetDirty(mist);

        var sigilObject = new GameObject("Baroque / casting ornament");
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Acanthus filigree"; quad.transform.SetParent(sigilObject.transform, false);
        UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
        var ornament = quad.GetComponent<MeshRenderer>(); MagicSurface(ornament, pattern);
        var sigil = sigilObject.AddComponent<MagicCastSigil>();
        sigil.Configure(ornament, MagicLight(sigilObject.transform, new Color(1, .72f, .38f), 0, 2.4f));
        var sigilAsset = SaveMagicPrefab<MagicCastSigil>(sigilObject, "BaroqueCast");

        var impactObject = new GameObject("Astral / small impact");
        Transform flash = MagicSphere("Brief light bloom", impactObject.transform, aura, .28f);
        var halo = new GameObject("Small impact ripple"); halo.transform.SetParent(impactObject.transform, false);
        halo.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/MVP03/Meshes/MagicOrbit.asset");
        MagicSurface(halo.AddComponent<MeshRenderer>(), ripple);
        StarParticles(impactObject.transform, stars, true);
        var impact = impactObject.AddComponent<MagicImpact>();
        impact.Configure(flash, halo.transform, MagicLight(impactObject.transform, new Color(.55f, .7f, 1), 3.5f, 2.5f));
        impact.ConfigureSmallBurst();
        var impactAsset = SaveMagicPrefab<MagicImpact>(impactObject, "AstralImpact");

        var orbObject = new GameObject("Astral / luminous orb");
        MagicSphere("Simple luminous sphere", orbObject.transform, core, .26f);
        Transform shell = MagicSphere("Soft spherical halo", orbObject.transform, aura, .42f);
        var trailObject = new GameObject("Astral mist wake"); trailObject.transform.SetParent(orbObject.transform, false);
        var trail = trailObject.AddComponent<TrailRenderer>(); MagicSurface(trail, mist);
        trail.time = .8f; trail.minVertexDistance = .045f;
        trail.widthCurve = new AnimationCurve(new Keyframe(0, .13f), new Keyframe(.3f, .32f), new Keyframe(1, 0));
        trail.colorGradient = AstralGradient(); trail.numCapVertices = 3; trail.numCornerVertices = 2;
        trail.alignment = LineAlignment.View; trail.textureMode = LineTextureMode.Stretch;
        var motes = StarParticles(orbObject.transform, stars, false);
        var bolt = orbObject.AddComponent<MagicBolt>();
        bolt.Configure(impactAsset, shell, null, trail, motes,
            MagicLight(orbObject.transform, new Color(.55f, .7f, 1), 3.6f, 3.2f));
        bolt.ConfigureOrb(.42f, 3.6f, 9f);
        var orbAsset = SaveMagicPrefab<MagicBolt>(orbObject, "AstralOrb");
        caster.Configure(orbAsset, sigilAsset, .6f, .48f, 1.6f);
        EditorUtility.SetDirty(caster);
    }

    private static T SaveMagicPrefab<T>(GameObject root, string name) where T : Component
    {
        try { return PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/" + name + ".prefab").GetComponent<T>(); }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static Material MagicGlowMaterial(string name, Color colour, float rim)
    {
        Material material = Material(name, "MVP03/Magic Glow");
        material.SetColor("_BaseColor", colour); material.SetFloat("_Opacity", 1);
        material.SetFloat("_RimWeight", rim); material.SetFloat("_RimPower", 2.4f);
        EditorUtility.SetDirty(material); return material;
    }

    private static Transform MagicSphere(string name, Transform parent, Material material, float diameter)
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere); sphere.name = name;
        UnityEngine.Object.DestroyImmediate(sphere.GetComponent<Collider>());
        sphere.transform.SetParent(parent, false); sphere.transform.localScale = Vector3.one * diameter;
        MagicSurface(sphere.GetComponent<MeshRenderer>(), material); return sphere.transform;
    }

    private static void MagicSurface(Renderer renderer, Material material)
    {
        renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false; renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    private static Light MagicLight(Transform parent, Color colour, float intensity, float range)
    {
        var source = new GameObject("Local spell light"); source.transform.SetParent(parent, false);
        var light = source.AddComponent<Light>(); light.type = LightType.Point;
        light.color = colour; light.intensity = intensity; light.range = range; light.shadows = LightShadows.None;
        return light;
    }

    private static Gradient AstralGradient()
    {
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(new Color(1, .93f, .73f), 0),
            new GradientColorKey(new Color(.48f, .7f, 1), .35f), new GradientColorKey(new Color(.6f, .3f, .94f), 1) },
            new[] { new GradientAlphaKey(.85f, 0), new GradientAlphaKey(.62f, .2f), new GradientAlphaKey(0, 1) });
        return gradient;
    }

    private static ParticleSystem StarParticles(Transform parent, Material material, bool burst)
    {
        var source = new GameObject(burst ? "Impact stardust" : "World space star wake"); source.transform.SetParent(parent, false);
        var particles = source.AddComponent<ParticleSystem>(); particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main; main.loop = !burst; main.duration = 1; main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(burst ? .2f : .5f, burst ? .5f : 1.05f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(burst ? .8f : .04f, burst ? 1.8f : .24f);
        main.startSize = new ParticleSystem.MinMaxCurve(.04f, burst ? .1f : .12f);
        main.maxParticles = burst ? 24 : 128; main.gravityModifier = 0;
        var emission = particles.emission; emission.rateOverTime = 0; emission.rateOverDistance = burst ? 0 : 13;
        if (burst) emission.SetBursts(new[] { new ParticleSystem.Burst(0, 16) });
        var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = burst ? .07f : .18f;
        var colours = particles.colorOverLifetime; colours.enabled = true; colours.color = AstralGradient();
        var size = particles.sizeOverLifetime; size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0, .5f), new Keyframe(.12f, 1), new Keyframe(1, 0)));
        var renderer = particles.GetComponent<ParticleSystemRenderer>(); renderer.renderMode = ParticleSystemRenderMode.Billboard;
        MagicSurface(renderer, material); return particles;
    }
}
