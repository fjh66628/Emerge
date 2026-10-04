using System;
using System.Linq;
using MVP03;
using MVP04;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static partial class MVP04Builder
{
    [MenuItem("MVP04/Apply Candle Physics")]
    public static void ApplyCandlePhysics()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != ScenePath)
            throw new InvalidOperationException("Open MVP04 outside Play mode first.");
        var renderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
            .Where(r => r.gameObject.scene == scene).ToDictionary(r => r.name);
        var lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
            .Where(l => l.gameObject.scene == scene && l.name.StartsWith("Candle pool ")).ToArray();
        foreach (var lamp in lights)
        {
            string suffix = lamp.name.Substring("Candle pool ".Length);
            if (!renderers.TryGetValue("CandleHolder" + suffix, out var holder) ||
                !renderers.TryGetValue("Wax" + suffix, out var wax) ||
                !renderers.TryGetValue("Flames" + suffix, out var flames))
                throw new InvalidOperationException("Incomplete candle group: " + suffix);
            ConfigureCandlePhysics(holder.gameObject, wax.gameObject, flames.gameObject, lamp, suffix);
        }
        foreach (var hero in UnityEngine.Object.FindObjectsByType<PixelPilgrim>(FindObjectsSortMode.None)
                     .Where(h => h.gameObject.scene == scene)) ConfigurePropPusher(hero.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"Candle physics ready: {lights.Length} freestanding candle stands.");
    }

    private static void ConfigurePropPusher(GameObject hero)
    {
        if (!hero.TryGetComponent<ChapelPropPusher>(out _)) Undo.AddComponent<ChapelPropPusher>(hero);
    }

    private static void ConfigureCandlePhysics(GameObject holder, GameObject wax, GameObject flames, Light lamp, string suffix)
    {
        // Migration is idempotent and preserves all existing meshes and light settings.
        if (holder.GetComponentInParent<KnockableCandleStand>() != null) return;
        Bounds bounds = holder.GetComponent<Renderer>().bounds;
        var root = new GameObject("Candle stand " + suffix);
        Undo.RegisterCreatedObjectUndo(root, "Add candle physics");
        root.transform.position = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        foreach (var child in new[] { holder, wax, flames, lamp.gameObject })
        {
            Undo.RecordObject(child, "Make candle movable");
            GameObjectUtility.SetStaticEditorFlags(child, 0);
            Undo.SetTransformParent(child.transform, root.transform, "Group candle stand");
        }

        // The altar step is slightly above the original decorative base: avoid initial penetration.
        // Query before adding this stand's colliders so its own base cannot hide the floor.
        Physics.SyncTransforms();
        if (Physics.Raycast(root.transform.position + Vector3.up * .4f, Vector3.down, out var ground, .7f) &&
            ground.point.y > root.transform.position.y)
            root.transform.position += Vector3.up * (ground.point.y - root.transform.position.y + .006f);

        var friction = CandlePhysicsMaterial();
        var baseCollider = Undo.AddComponent<BoxCollider>(root);
        baseCollider.center = new Vector3(0, .10f, 0);
        baseCollider.size = new Vector3(.56f, .2f, .56f);
        baseCollider.sharedMaterial = friction;
        var shaft = Undo.AddComponent<CapsuleCollider>(root);
        shaft.center = new Vector3(0, .82f, 0);
        shaft.radius = .075f;
        shaft.height = 1.5f;
        shaft.sharedMaterial = friction;
        var arm = Undo.AddComponent<BoxCollider>(root);
        arm.center = new Vector3(0, 1.53f, 0);
        arm.size = new Vector3(bounds.size.x, .10f, .23f);
        arm.sharedMaterial = friction;
        Bounds waxBounds = wax.GetComponent<Renderer>().bounds;
        var candles = Undo.AddComponent<BoxCollider>(root);
        candles.center = root.transform.InverseTransformPoint(waxBounds.center);
        candles.size = waxBounds.size + new Vector3(.015f, .01f, .015f);
        candles.sharedMaterial = friction;

        var body = Undo.AddComponent<Rigidbody>(root);
        body.mass = 4;
        body.centerOfMass = new Vector3(0, .60f, 0);
        body.linearDamping = .25f;
        body.angularDamping = .6f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        var stand = Undo.AddComponent<KnockableCandleStand>(root);
        stand.Configure(lamp, flames.GetComponent<Renderer>(), flames.GetComponent<CandleSway>());
        EditorUtility.SetDirty(stand);
    }

    private static PhysicsMaterial CandlePhysicsMaterial()
    {
        string path = Root + "/Materials/CandleContact.physicMaterial";
        var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (material != null) return material;
        material = new PhysicsMaterial("CandleContact")
        {
            staticFriction = .65f, dynamicFriction = .5f, bounciness = 0,
            frictionCombine = PhysicsMaterialCombine.Average, bounceCombine = PhysicsMaterialCombine.Minimum
        };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
}
