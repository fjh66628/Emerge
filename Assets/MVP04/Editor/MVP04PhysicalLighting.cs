using System;
using MVP04;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static partial class MVP04Builder
{
    [MenuItem("MVP04/Apply Physical Window Lighting")]
    public static void ApplyPhysicalWindowLighting()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(Application.isPlaying || scene.path!=ScenePath)
            throw new InvalidOperationException("Open MVP04 outside Play mode first.");
        ConfigurePhysicalLighting();
        var volume=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/RoseWindowVolume.mat");
        ConfigurePhysicalMedium(volume);AssetDatabase.SaveAssetIfDirty(volume);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigurePhysicalLighting()
    {
        foreach(var obsolete in UnityEngine.Object.FindObjectsByType<SideWindowLight>(FindObjectsSortMode.None))
            UnityEngine.Object.DestroyImmediate(obsolete.gameObject);
        var oldSun=GameObject.Find("Moonlight / through the rose");
        if(oldSun!=null)UnityEngine.Object.DestroyImmediate(oldSun);
        RenderSettings.fog=false;
        var bounce=GameObject.Find("Soft nave bounce");
        RenderSettings.sun=bounce!=null?bounce.GetComponent<Light>():null;

        var settings=AssetDatabase.LoadAssetAtPath<ChapelLightSettings>(MVP04LightingWindow.SettingsPath);
        if(settings==null)
        {settings=ScriptableObject.CreateInstance<ChapelLightSettings>();AssetDatabase.CreateAsset(settings,MVP04LightingWindow.SettingsPath);}
        settings.roseTransmission=Readable("RoseTransmission");
        settings.sideTransmission=Readable("SideGlassTransmission");
        EditorUtility.SetDirty(settings);AssetDatabase.SaveAssetIfDirty(settings);
        var source=UnityEngine.Object.FindFirstObjectByType<ChapelWindowLight>();
        if(source==null)source=new GameObject("Exterior source / all window projections").AddComponent<ChapelWindowLight>();
        source.settings=settings;source.Apply();
        var serialized=new SerializedObject(source.Source.GetUniversalAdditionalLightData());
        serialized.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=2;
        serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(source);
        foreach(string name in new[]{"LuminousRoseGlass","LuminousSideGlass"})
        {
            var glass=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+name+".mat");
            if(glass!=null){glass.SetFloat("_Transmission",1);EditorUtility.SetDirty(glass);AssetDatabase.SaveAssetIfDirty(glass);}
        }

        Texture2D Readable(string name)
        {
            string path=Root+"/Textures/"+name+".png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            if(!importer.isReadable){importer.isReadable=true;importer.SaveAndReimport();}
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
