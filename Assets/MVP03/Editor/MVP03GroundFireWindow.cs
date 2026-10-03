using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class MVP03GroundFireWindow : EditorWindow
{
    private Material material;
    private readonly List<System.Action> refreshers = new List<System.Action>();
    private IVisualElementScheduledItem save;

    [MenuItem("MVP03/地面火焰设置")]
    public static void Open()
    {
        var window = GetWindow<MVP03GroundFireWindow>();
        window.titleContent = new GUIContent("MVP03 地面火焰"); window.minSize = new Vector2(410, 410); window.Show();
    }
    public void CreateGUI()
    {
        rootVisualElement.Clear(); refreshers.Clear();
        material = AssetDatabase.LoadAssetAtPath<Material>(MVP03GroundFireBuilder.MaterialPath);
        var panel = new ScrollView(); panel.style.paddingLeft = panel.style.paddingRight = 16;
        panel.style.paddingTop = panel.style.paddingBottom = 16; rootVisualElement.Add(panel);
        panel.Add(new HelpBox("在 Game 面板选择“黄色火焰”，再点击地面。参数即时生效、自动保存，支持撤销。", HelpBoxMessageType.Info));
        if (material == null) { panel.Add(new Label("请先执行 MVP03 > Add Ground Fire。")); return; }
        Add(panel, "火焰高度 / 米", "_FlameHeight", .2f, 1.8f);
        Add(panel, "火焰宽度 / 米", "_FlameWidth", .2f, .9f);
        var pivot = new UnityEngine.UIElements.Vector3Field("粒子 Pivot")
        {
            name = "_ParticlePivot",
            tooltip = "粒子局部 XYZ 枢轴，相对粒子尺寸；0 为中心，Y=-0.5 为底边。默认 (0, -0.42, 0)。"
        };
        pivot.style.marginTop = 10;
        pivot.SetValueWithoutNotify(material.GetVector("_ParticlePivot"));
        refreshers.Add(() => pivot.SetValueWithoutNotify(material.GetVector("_ParticlePivot")));
        pivot.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(material, "调整火焰粒子 Pivot");
            material.SetVector("_ParticlePivot", evt.newValue); Changed();
        });
        panel.Add(pivot);
        Add(panel, "火焰密度", "_FlameDensity", .2f, 1);
        Add(panel, "上升速度", "_FlameSpeed", .2f, 3);
        Add(panel, "序列动画帧率", "_AnimationFPS", 4, 32);
        Add(panel, "湍流强度", "_Turbulence", 0, 4);
        Add(panel, "湍流频率", "_TurbulenceFrequency", .2f, 4);
        Add(panel, "火焰亮度", "_Emission", .3f, 4);
        Add(panel, "地面余火", "_GroundGlow", 0, 1);
        Add(panel, "灼烧地面深浅", "_ScorchStrength", 0, 1);
        Add(panel, "焦痕纹理密度", "_ScorchScale", 1, 12);
        Add(panel, "覆盖阈值", "_FireThreshold", .1f, .8f);
        Add(panel, "边缘过渡", "_EdgeSoftness", .02f, .3f);
        Add(panel, "局部暖光强度", "_LightIntensity", 0, 6);
        Add(panel, "局部暖光范围 / 米", "_LightRange", 1, 6);
        panel.Add(new Button(Save) { text = "立即保存" });
        panel.Add(new Button(() => AssetDatabase.OpenAsset(AssetDatabase.LoadMainAssetAtPath(MVP03GroundFireGraph.GraphPath))) { text = "打开 Visual Effect Graph" });
    }
    private void Add(VisualElement parent, string title, string property, float min, float max)
    {
        var slider = new Slider(title, min, max) { name = property, showInputField = true };
        slider.style.marginTop = 10; slider.labelElement.style.minWidth = 145;
        slider.SetValueWithoutNotify(material.GetFloat(property));
        refreshers.Add(() => slider.SetValueWithoutNotify(material.GetFloat(property)));
        slider.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(material, "调整地面火焰：" + title);
            material.SetFloat(property, Mathf.Clamp(evt.newValue, min, max)); Changed();
        });
        parent.Add(slider);
    }
    private void Changed()
    {
        EditorUtility.SetDirty(material); save?.Pause(); save = rootVisualElement.schedule.Execute(Save).StartingIn(400);
        SceneView.RepaintAll(); EditorApplication.QueuePlayerLoopUpdate();
    }
    private void Save() { save?.Pause(); if (material != null) AssetDatabase.SaveAssetIfDirty(material); }
    private void Refresh() { if (material != null) foreach (var refresh in refreshers) refresh(); }
    private void OnEnable() => Undo.undoRedoPerformed += OnUndo;
    private void OnDisable() { Undo.undoRedoPerformed -= OnUndo; Save(); }
    private void OnFocus() => Refresh();
    private void OnUndo() { Refresh(); if (material != null) Changed(); }
}
