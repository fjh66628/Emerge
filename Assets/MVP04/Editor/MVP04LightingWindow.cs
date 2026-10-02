using System.Collections.Generic;
using MVP04;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class MVP04LightingWindow : EditorWindow
{
    public const string SettingsPath="Assets/MVP04/Rendering/ExteriorLight.asset";
    private const string MaterialPath="Assets/MVP04/Materials/RoseWindowVolume.mat";
    private Material volume;
    private ChapelLightSettings source;
    private readonly List<System.Action> refreshers=new List<System.Action>();
    private IVisualElementScheduledItem pendingSave;
    private Label saveStatus;
    private bool needsSave;

    [MenuItem("MVP04/体积光设置")]
    public static void Open()
    {
        var window=GetWindow<MVP04LightingWindow>();
        window.titleContent=new GUIContent("体积光设置");
        window.minSize=new Vector2(440,620);window.Show();
    }
    public void CreateGUI()
    {
        rootVisualElement.Clear();refreshers.Clear();
        volume=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        source=AssetDatabase.LoadAssetAtPath<ChapelLightSettings>(SettingsPath);
        rootVisualElement.style.backgroundColor=new Color(.13f,.14f,.16f);
        var scroll=new ScrollView();rootVisualElement.Add(scroll);
        scroll.style.paddingLeft=18;scroll.style.paddingRight=18;
        scroll.style.paddingTop=18;scroll.style.paddingBottom=18;
        var title=new Label("MVP04 · 光源与空气");title.style.fontSize=22;
        title.style.unityFontStyleAndWeight=FontStyle.Bold;scroll.Add(title);
        var subtitle=new Label("同一光源穿过窗洞，投射到空气与建筑表面");
        subtitle.style.color=new Color(.65f,.72f,.8f);subtitle.style.marginTop=4;scroll.Add(subtitle);
        if(volume==null || source==null)
        {
            scroll.Add(new HelpBox("请先执行 MVP04 > Apply Physical Window Lighting。",HelpBoxMessageType.Info));return;
        }
        var emitter=Section(scroll,"外部光源 · 世界坐标 / 米");
        var explanation=new Label("默认光源在教堂左前方上空。点光源产生几何硬边；光束方向和张角由光源、窗洞位置决定。背光侧不会出现直接光束。");
        explanation.style.whiteSpace=WhiteSpace.Normal;emitter.Add(explanation);
        var serialized=new SerializedObject(source);
        AddProperty("sourceEnabled","开启光源");
        AddProperty("position","光源位置");
        AddProperty("target","照射目标");
        AddProperty("intensity","光源强度（URP）");
        AddProperty("colour","光源颜色");
        AddProperty("coneAngle","光源照射角度");
        AddProperty("range","光源范围（米）");
        AddProperty("directTransmission","玻璃直透比例");
        emitter.Bind(serialized);
        var air=Section(scroll,"空气介质 · 全教堂共享");
        AddSlider(air,"消光系数 / 米","density","_Density",0,.2f,"吸收与散射之和；数值过高时，光在到达深处前就会衰减。");
        AddSlider(air,"散射反照率","albedo","_ScatteringAlbedo",0,1,"散射占消光的比例。0 表示纯吸收，1 表示不吸收。");
        AddSlider(air,"前向散射 g","anisotropy","_Anisotropy",-.8f,.8f,"0 为各向同性；正值使朝观察者传播的光更亮。");
        AddSlider(air,"密度起伏","noise-amount","_NoiseAmount",0,1,"噪声只改变空气密度，不修改光路。");
        AddSlider(air,"密度噪声频率","dust-scale","_NoiseScale",.05f,2,"单位空间内空气密度变化的频率。");
        var footer=Section(scroll,"实时预览与保存");
        var help=new Label("编辑后实时预览，自动保存，支持 Ctrl+Z。\n运行中调整也会保留。光源位置变化会重新计算玻璃投影。\n实时模型：单次散射 + 阴影贴图；环境补光近似反弹光。");
        help.style.whiteSpace=WhiteSpace.Normal;footer.Add(help);
        var actions=new VisualElement();actions.style.flexDirection=FlexDirection.Row;footer.Add(actions);
        var save=new Button(SaveSettings){text="立即保存",name="save-settings"};save.style.flexGrow=1;actions.Add(save);
        var select=new Button(()=>{var light=Object.FindFirstObjectByType<ChapelWindowLight>();if(light!=null)Selection.activeGameObject=light.gameObject;}){text="选中光源 / 查看光路"};
        select.style.flexGrow=1;actions.Add(select);
        saveStatus=new Label("设置已载入");saveStatus.style.marginTop=8;footer.Add(saveStatus);
        RefreshControls();

        void AddProperty(string property,string label)
        {
            var field=new PropertyField(serialized.FindProperty(property),label){name="source-"+property};
            field.style.marginTop=6;emitter.Add(field);
            field.RegisterCallback<SerializedPropertyChangeEvent>(_=>Changed());
        }
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
    private void AddSlider(VisualElement parent,string label,string name,string property,float low,float high,string tooltip)
    {
        var slider=new Slider(label,low,high){name=name,showInputField=true,tooltip=tooltip};
        slider.style.marginTop=9;slider.labelElement.style.minWidth=130;parent.Add(slider);
        refreshers.Add(()=>slider.SetValueWithoutNotify(volume.GetFloat(property)));
        slider.RegisterValueChangedCallback(evt=>
        {
            Undo.RecordObject(volume,"调整空气："+label);
            volume.SetFloat(property,Mathf.Clamp(evt.newValue,low,high));Changed();
        });
    }
    private void Changed()
    {
        if(volume!=null)EditorUtility.SetDirty(volume);
        if(source!=null)EditorUtility.SetDirty(source);
        needsSave=true;
        var light=Object.FindFirstObjectByType<ChapelWindowLight>();if(light!=null)light.Apply();
        if(saveStatus!=null)saveStatus.text="预览已更新 · 正在自动保存…";
        pendingSave?.Pause();pendingSave=rootVisualElement.schedule.Execute(SaveSettings).StartingIn(400);
        SceneView.RepaintAll();EditorApplication.QueuePlayerLoopUpdate();UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }
    public void SaveSettings()
    {
        pendingSave?.Pause();pendingSave=null;
        if(needsSave)
        {
            if(volume!=null)AssetDatabase.SaveAssetIfDirty(volume);
            if(source!=null)AssetDatabase.SaveAssetIfDirty(source);
            needsSave=false;
        }
        if(saveStatus!=null)saveStatus.text="已保存 · 停止运行后仍保留";
    }
    private void RefreshControls(){if(volume!=null)foreach(var refresh in refreshers)refresh();}
    private void OnEnable(){Undo.undoRedoPerformed+=OnUndoRedo;}
    private void OnDisable(){Undo.undoRedoPerformed-=OnUndoRedo;SaveSettings();}
    private void OnFocus(){RefreshControls();}
    private void OnUndoRedo(){RefreshControls();Changed();}
}
