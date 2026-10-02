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
        glass.SetFloat("_Transmission",transmission);glass.SetFloat("_Cutoff",.5f);glass.SetFloat("_Cull",2);
        glass.renderQueue=2450;EditorUtility.SetDirty(glass);return glass;
    }

    private static void SharpenWindowBeams(Material volume)
    {
        MVP04LightingWindow.SetEdgePreset(volume,2);
        volume.SetFloat("_BeamIntensity",1);
    }

    [MenuItem("MVP04/Apply Frosted Glass and Sharper Beams")]
    public static void ApplyFrostedGlassAndBeams()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play mode before editing chapel materials.");
        var rose=FrostedGlass("LuminousRoseGlass",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/RoseTransmission.png"),2.05f,new Vector2(5.26f,5.26f));
        var side=FrostedGlass("LuminousSideGlass",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/SideGlassTransmission.png"),1.65f,new Vector2(2.44f,4.5f));
        var volume=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/RoseWindowVolume.mat");
        SharpenWindowBeams(volume);
        AssetDatabase.SaveAssetIfDirty(rose);AssetDatabase.SaveAssetIfDirty(side);AssetDatabase.SaveAssetIfDirty(volume);
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
