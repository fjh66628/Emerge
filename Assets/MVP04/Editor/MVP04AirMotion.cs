using UnityEditor;
using UnityEngine;

public static partial class MVP04Builder
{
    [MenuItem("MVP04/Apply Air Motion")]
    public static void ApplyAirMotion()
    {
        ConfigureDensityNoise();
        var volume=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/RoseWindowVolume.mat");
        ConfigureAirMotionParameters(volume);
        EditorUtility.SetDirty(volume);
        AssetDatabase.SaveAssetIfDirty(volume);
        SceneView.RepaintAll();
    }

    private static void ConfigureAirMotionParameters(Material volume)
    {
        volume.SetFloat("_FlowSpeed",.28f);
        volume.SetVector("_FlowDirection",new Vector4(.8f,.2f,.35f,0));
        volume.SetFloat("_FlowWarp",1.1f);
        volume.SetFloat("_FlowDetail",.45f);
    }
}
