using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class MVP04LightingWindow : EditorWindow
{
    private const string MaterialPath="Assets/MVP04/Materials/RoseWindowVolume.mat";
    private Material volume;
    private readonly List<System.Action> refreshers=new List<System.Action>();
    private IVisualElementScheduledItem pendingSave;
    private Label saveStatus;
    private bool needsSave;

    [MenuItem("MVP04/体积光设置")]
    public static void Open()
    {
        var window=GetWindow<MVP04LightingWindow>();
        window.titleContent=new GUIContent("体积光设置");
        window.minSize=new Vector2(420,560);window.Show();
    }

    public void CreateGUI()
    {
        rootVisualElement.Clear();refreshers.Clear();
        volume=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        rootVisualElement.style.backgroundColor=new Color(.13f,.14f,.16f);
        var scroll=new ScrollView();rootVisualElement.Add(scroll);
        scroll.style.paddingLeft=18;scroll.style.paddingRight=18;
        scroll.style.paddingTop=18;scroll.style.paddingBottom=18;
        var title=new Label("MVP04 · 体积光");title.style.fontSize=22;
        title.style.unityFontStyleAndWeight=FontStyle.Bold;scroll.Add(title);
        var subtitle=new Label("边缘更清楚，亮度单独调整");
        subtitle.style.color=new Color(.65f,.72f,.8f);subtitle.style.marginTop=4;scroll.Add(subtitle);
        if(volume==null)
        {
            scroll.Add(new HelpBox("未找到 MVP04 的体积光材质，请先构建教堂场景。",HelpBoxMessageType.Warning));return;
        }
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/MVP04/Scenes/MVP04_NocturneChapel.unity")
            scroll.Add(new HelpBox("当前未打开 MVP04 场景。调整仍会保存到 MVP04 的体积光材质。",HelpBoxMessageType.Info));

        var edge=Section(scroll,"边缘与分束");
        var presets=new VisualElement();presets.style.flexDirection=FlexDirection.Row;edge.Add(presets);
        string[] names={"柔和","清晰","锐利"};
        for(int i=0;i<names.Length;i++)
        {
            int preset=i;
            var button=new Button(()=>
            {
                Undo.RecordObject(volume,"体积光预设："+names[preset]);
                SetEdgePreset(volume,preset);Changed();RefreshControls();
            }){text=names[i],name="preset-"+i,tooltip="只调整边缘、扩散、分束与阴影，保留当前亮度。"};
            button.style.flexGrow=1;button.style.height=30;presets.Add(button);
        }
        AddSlider(edge,"边缘锐利度","beam-edge","_BeamEdge",0,100,
            "越大越锐利，同时作用于圆窗与侧窗光束。",
            value=>Mathf.InverseLerp(Mathf.Log(.12f),Mathf.Log(.003f),Mathf.Log(Mathf.Max(value,.003f)))*100,
            value=>Mathf.Exp(Mathf.Lerp(Mathf.Log(.12f),Mathf.Log(.003f),value/100)));
        AddSlider(edge,"窗格分束对比","beam-contrast","_BeamContrast",1,3,"越大，暗窗棂与明亮光束之间的分界越明显。");
        AddSlider(edge,"阴影锐利度","beam-shadow","_ShadowSharpness",0,1,"收紧石柱和窗棂在光束中的阴影边缘。");
        AddSlider(edge,"圆窗光束扩散","beam-spread","_BeamSpread",0,.03f,"越小，圆窗光束沿传播方向越集中。侧窗方向由其灯光位置决定。");

        var light=Section(scroll,"亮度与空气");
        AddSlider(light,"光束亮度","beam-intensity","_BeamIntensity",0,2,"同时调整圆窗与侧窗的体积光亮度，不改变场景灯光强度。");
        AddSlider(light,"圆窗雾中散射","rose-density","_Density",0,.2f,"圆窗光束在空气中的浓度。");
        AddSlider(light,"侧窗雾中散射","side-density","_SideDensity",0,.2f,"全部侧窗光束在空气中的浓度。");
        AddSlider(light,"尘雾纹理频率","dust-scale","_NoiseScale",.1f,2,"越大，光束中明暗变化的颗粒越细。");

        var footer=Section(scroll,"实时预览与保存");
        var help=new Label("拖动滑块即可在 Game / Scene 窗口预览。\n修改自动保存，支持 Ctrl+Z 撤销。运行中调整也会保留。");
        help.style.whiteSpace=WhiteSpace.Normal;help.style.marginBottom=10;footer.Add(help);
        var actions=new VisualElement();actions.style.flexDirection=FlexDirection.Row;footer.Add(actions);
        var save=new Button(SaveSettings){text="立即保存",name="save-settings"};save.style.flexGrow=1;actions.Add(save);
        var inspect=new Button(()=>{Selection.activeObject=volume;EditorGUIUtility.PingObject(volume);}){text="在 Inspector 中查看"};
        inspect.style.flexGrow=1;actions.Add(inspect);
        saveStatus=new Label("设置已载入");saveStatus.style.marginTop=8;
        saveStatus.style.color=new Color(.65f,.78f,.67f);footer.Add(saveStatus);
        RefreshControls();
    }

    private VisualElement Section(VisualElement parent,string heading)
    {
        var section=new VisualElement();section.style.marginTop=16;
        section.style.paddingLeft=12;section.style.paddingRight=12;section.style.paddingTop=12;section.style.paddingBottom=12;
        section.style.backgroundColor=new Color(.19f,.2f,.23f);
        section.style.borderTopLeftRadius=6;section.style.borderTopRightRadius=6;
        section.style.borderBottomLeftRadius=6;section.style.borderBottomRightRadius=6;
        var label=new Label(heading);label.style.unityFontStyleAndWeight=FontStyle.Bold;
        label.style.marginBottom=10;section.Add(label);parent.Add(section);return section;
    }

    private void AddSlider(VisualElement parent,string label,string name,string property,float low,float high,string tooltip,
        System.Func<float,float> toDisplay=null,System.Func<float,float> fromDisplay=null)
    {
        var slider=new Slider(label,low,high){name=name,showInputField=true,tooltip=tooltip};
        slider.style.marginTop=9;slider.labelElement.style.minWidth=130;parent.Add(slider);
        refreshers.Add(()=>slider.SetValueWithoutNotify(toDisplay==null?volume.GetFloat(property):toDisplay(volume.GetFloat(property))));
        slider.RegisterValueChangedCallback(evt=>
        {
            Undo.RecordObject(volume,"调整体积光："+label);
            float value=Mathf.Clamp(evt.newValue,low,high);
            volume.SetFloat(property,fromDisplay==null?value:fromDisplay(value));Changed();
        });
    }

    private void Changed()
    {
        EditorUtility.SetDirty(volume);needsSave=true;
        if(saveStatus!=null)saveStatus.text="预览已更新 · 正在自动保存…";
        pendingSave?.Pause();pendingSave=rootVisualElement.schedule.Execute(SaveSettings).StartingIn(400);
        SceneView.RepaintAll();EditorApplication.QueuePlayerLoopUpdate();UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }

    public void SaveSettings()
    {
        pendingSave?.Pause();pendingSave=null;
        if(volume!=null && needsSave){AssetDatabase.SaveAssetIfDirty(volume);needsSave=false;}
        if(saveStatus!=null)saveStatus.text="已保存 · 停止运行后仍保留";
    }

    private void RefreshControls(){if(volume!=null)foreach(var refresh in refreshers)refresh();}
    private void OnEnable(){Undo.undoRedoPerformed+=OnUndoRedo;}
    private void OnDisable(){Undo.undoRedoPerformed-=OnUndoRedo;SaveSettings();}
    private void OnFocus(){RefreshControls();}
    private void OnUndoRedo(){if(volume!=null){RefreshControls();Changed();}}

    // Shared with the scene builder so a rebuild matches the settings-window presets.
    public static void SetEdgePreset(Material material,int preset)
    {
        Vector4 values=preset==0?new Vector4(.045f,.008f,1.25f,.45f):
            preset==1?new Vector4(.018f,.002f,1.65f,.75f):new Vector4(.008f,.001f,2.05f,.9f);
        material.SetFloat("_BeamEdge",values.x);material.SetFloat("_BeamSpread",values.y);
        material.SetFloat("_BeamContrast",values.z);material.SetFloat("_ShadowSharpness",values.w);
        EditorUtility.SetDirty(material);
    }

    [MenuItem("MVP04/Apply Sharper Beam Edges")]
    public static void ApplySharperDefaults()
    {
        var material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if(material==null)return;
        Undo.RecordObject(material,"应用锐利体积光");SetEdgePreset(material,2);
        // Older material assets acquire the shader's brightness default when this property is introduced.
        AssetDatabase.SaveAssetIfDirty(material);
        SceneView.RepaintAll();EditorApplication.QueuePlayerLoopUpdate();
    }
}
