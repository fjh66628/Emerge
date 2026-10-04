using System;
using System.Linq;
using MVP04;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static partial class MVP04Builder
{
    [MenuItem("MVP04/Apply Optimized Side Lighting")]
    public static void ApplyOptimizedSideLighting()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(Application.isPlaying || scene.path!=ScenePath)
            throw new InvalidOperationException("Open MVP04 outside Play mode first.");
        ConfigurePhysicalLighting();
        var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Root+"/Rendering/NocturneRenderer.asset");
        var feature=renderer.rendererFeatures.OfType<WindowVolumeFeature>().First();
        feature.resolutionDivisor=2;feature.viewSteps=40;feature.lightSteps=2;
        EditorUtility.SetDirty(feature);renderer.SetDirty();AssetDatabase.SaveAssetIfDirty(renderer);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }

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

        var rose=Readable("RoseTransmission");var sideGlass=Readable("SideGlassTransmission");
        for(int index=0;index<3;index++)
        {
            string path=index==0?MVP04LightingWindow.SettingsPath:Root+"/Rendering/"+(index==1?"ExteriorLeft":"ExteriorRight")+".asset";
            var settings=AssetDatabase.LoadAssetAtPath<ChapelLightSettings>(path);
            if(settings==null)
            {
                settings=ScriptableObject.CreateInstance<ChapelLightSettings>();
                if(index>0)
                {
                    settings.position=new Vector3(index==1?-24:24,14,12);settings.target=new Vector3(0,1,1);
                    settings.intensity=18000;settings.range=65;settings.coneAngle=82;
                    settings.colour=index==1?new Color(.76f,.84f,1):new Color(.84f,.88f,1);
                }
                AssetDatabase.CreateAsset(settings,path);
            }
            settings.roseTransmission=rose;settings.sideTransmission=sideGlass;
            EditorUtility.SetDirty(settings);AssetDatabase.SaveAssetIfDirty(settings);
            var source=UnityEngine.Object.FindObjectsByType<ChapelWindowLight>(FindObjectsSortMode.None).FirstOrDefault(s=>s.settings==settings);
            if(source==null)source=new GameObject().AddComponent<ChapelWindowLight>();
            source.gameObject.name="Exterior source / "+(index==0?"front rose":index==1?"left windows":"right windows");
            source.settings=settings;source.Apply();
            var serialized=new SerializedObject(source.Source.GetUniversalAdditionalLightData());
            serialized.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=2;
            serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(source);
        }
        ConfigureDensityNoise();
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

    private static void ConfigureDensityNoise()
    {
        string path=Root+"/Textures/AirFlowNoise.asset";
        var texture=AssetDatabase.LoadAssetAtPath<Texture3D>(path);
        if(texture==null)
        {
            const int size=32;
            texture=new Texture3D(size,size,size,UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm,
                UnityEngine.Experimental.Rendering.TextureCreationFlags.MipChain)
                {name="Air flow RGB and density A",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear};
            var bytes=new byte[size*size*size*4];uint state=20261003;
            for(int i=0;i<bytes.Length;i++){state^=state<<13;state^=state>>17;state^=state<<5;bytes[i]=(byte)(state>>24);}
            texture.SetPixelData(bytes,0);texture.Apply(true,true);AssetDatabase.CreateAsset(texture,path);
        }
        var volume=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/RoseWindowVolume.mat");
        volume.SetTexture("_DensityNoise",texture);EditorUtility.SetDirty(volume);AssetDatabase.SaveAssetIfDirty(volume);
    }
}
