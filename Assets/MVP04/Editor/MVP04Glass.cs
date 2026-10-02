using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static partial class MVP04Builder
{
    private static Material FrostedGlass(string name,Texture2D texture,float transmission,Vector2 paneSize)
    {
        const string shader="MVP04/Frosted Stained Glass";
        var glass=Material(name,shader);glass.shader=Shader.Find(shader);
        glass.shaderKeywords=Array.Empty<string>();glass.EnableKeyword("_ALPHATEST_ON");
        glass.SetTexture("_BaseMap",texture);glass.SetColor("_BaseColor",Color.white);
        glass.SetColor("_FrostTint",new Color(.68f,.80f,.87f));
        glass.SetVector("_PaneSize",new Vector4(paneSize.x,paneSize.y,0,0));
        glass.SetFloat("_FrostAmount",.8f);glass.SetFloat("_FrostBlur",2.4f);
        glass.SetFloat("_GrainStrength",.38f);glass.SetFloat("_Smoothness",.24f);
        glass.SetFloat("_Transmission",1);glass.SetFloat("_Cutoff",.5f);glass.SetFloat("_Cull",2);
        glass.renderQueue=2450;EditorUtility.SetDirty(glass);return glass;
    }

    private static void ConfigurePhysicalMedium(Material volume)
    {
        // Remove the former artistic beam controls when migrating an existing material.
        var serialized=new SerializedObject(volume);
        foreach(string group in new[]{"m_TexEnvs","m_Floats","m_Colors","m_Ints"})
        {
            var properties=serialized.FindProperty("m_SavedProperties."+group);
            for(int i=properties.arraySize-1;i>=0;i--)
                if(!volume.HasProperty(properties.GetArrayElementAtIndex(i).FindPropertyRelative("first").stringValue))
                    properties.DeleteArrayElementAtIndex(i);
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        volume.SetFloat("_Density",.028f);volume.SetFloat("_ScatteringAlbedo",.9f);
        volume.SetFloat("_Anisotropy",.25f);volume.SetFloat("_NoiseAmount",.2f);volume.SetFloat("_NoiseScale",.28f);
        EditorUtility.SetDirty(volume);
    }

    [MenuItem("MVP04/Apply Frosted Glass")]
    public static void ApplyFrostedGlassAndBeams()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play mode before editing chapel materials.");
        var rose=FrostedGlass("LuminousRoseGlass",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/RoseTransmission.png"),2.05f,new Vector2(5.26f,5.26f));
        var side=FrostedGlass("LuminousSideGlass",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/SideGlassTransmission.png"),1.65f,new Vector2(2.44f,4.5f));
        AssetDatabase.SaveAssetIfDirty(rose);AssetDatabase.SaveAssetIfDirty(side);
    }

    [MenuItem("MVP04/Capture Frosted Glass Detail")]
    public static void CaptureFrostedGlassDetail()
    {
        var camera=Camera.main;if(camera==null)throw new InvalidOperationException("Open MVP04 first.");
        var position=camera.transform.position;var rotation=camera.transform.rotation;
        var atmosphere=UnityEngine.Object.FindFirstObjectByType<UnityEngine.Rendering.Volume>();
        atmosphere.profile.TryGet(out DepthOfField focus);float oldFocus=focus.focusDistance.value;
        try
        {
            camera.transform.position=V(5.8f,5.2f,-2.3f);camera.transform.LookAt(V(8.86f,5.3f,0));
            focus.focusDistance.value=3.85f;CaptureCamera(camera,"Previews/MVP04_FrostedGlass.png");
        }
        finally{focus.focusDistance.value=oldFocus;camera.transform.SetPositionAndRotation(position,rotation);}
    }
}
