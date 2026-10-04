using System;
using MVP03;
using MVP04;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static partial class MVP04Builder
{
    [MenuItem("MVP04/Apply Player Controls")]
    public static void ApplyPlayerControls()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != ScenePath)
            throw new InvalidOperationException("Open MVP04 outside Play mode first.");

        Camera camera = Camera.main;
        var hero = UnityEngine.Object.FindFirstObjectByType<PixelPilgrim>();
        if (camera == null || hero == null)
            throw new InvalidOperationException("The chapel camera and pilgrim must exist before applying controls.");

        Undo.RegisterFullObjectHierarchyUndo(camera.gameObject, "Bind chapel camera and zoom");
        Undo.RegisterFullObjectHierarchyUndo(hero.gameObject, "Bind chapel movement");
        hero.enabled = true;
        hero.GetComponent<CharacterController>().enabled = true;
        hero.Configure(camera, hero.GetComponentInChildren<SpriteRenderer>());
        ConfigurePlayerCamera(camera, hero.transform);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("MVP04 controls: WASD / arrows move, Q / E and wheel zoom, Space casts.");
    }

    private static void ConfigurePlayerCamera(Camera camera, Transform hero)
    {
        var follow = camera.GetComponent<PixelFollowCamera>();
        if (follow == null) follow = camera.gameObject.AddComponent<PixelFollowCamera>();
        follow.enabled = true;
        follow.Configure(hero);
        follow.ConfigureZoom(5f, 15f);
        if (!camera.TryGetComponent<ChapelControlsHint>(out var hint))
            hint = camera.gameObject.AddComponent<ChapelControlsHint>();
        hint.enabled = true;
    }
}
