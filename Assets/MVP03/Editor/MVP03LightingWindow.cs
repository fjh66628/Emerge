using System.Collections.Generic;
using System.Linq;
using MVP03;
using MVP04;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

public sealed class MVP03LightingWindow : EditorWindow
{
    private Material medium;
    private CourtyardSunSettings source;
    private UniversalRendererData rendererData;
    private WindowVolumeFeature feature;
    private readonly List<System.Action> refreshers = new List<System.Action>();
    private IVisualElementScheduledItem pendingSave;
    private Label status;
    private bool needsSave;

    [MenuItem("MVP03/体积光设置")]
    public static void Open()
    {
        var window = GetWindow<MVP03LightingWindow>();
        window.titleContent = new GUIContent("MVP03 体积光");
        window.minSize = new Vector2(440, 620);
        window.Show();
    }

    public void CreateGUI()
    {
        rootVisualElement.Clear();
        refreshers.Clear();
        medium = AssetDatabase.LoadAssetAtPath<Material>(MVP03WarmLighting.MaterialPath);
        source = AssetDatabase.LoadAssetAtPath<CourtyardSunSettings>(MVP03Presentation.SunSettingsPath);
        rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(MVP03WarmLighting.RendererPath);
        feature = rendererData != null ? rendererData.rendererFeatures.OfType<WindowVolumeFeature>().FirstOrDefault() : null;
        rootVisualElement.style.backgroundColor = new Color(.13f, .14f, .16f);
        var scroll = new ScrollView();
        scroll.style.paddingLeft = scroll.style.paddingRight = 18;
        scroll.style.paddingTop = scroll.style.paddingBottom = 18;
        rootVisualElement.Add(scroll);
        var title = new Label("MVP03 · 暖色阳光与空气");
        title.style.fontSize = 22;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        scroll.Add(title);
        var subtitle = new Label("阳光、树冠遮挡与人物受光 · 实时预览");
        subtitle.style.color = new Color(.82f, .75f, .61f);
        subtitle.style.marginTop = 5;
        scroll.Add(subtitle);
        if (medium == null || source == null || feature == null)
        {
            scroll.Add(new HelpBox("请先打开 MVP03，执行 Apply Warm Volumetric Light and Camera。", HelpBoxMessageType.Info));
            return;
        }

        var sunlight = Section(scroll, "太阳光源");
        var explanation = new Label("太阳使用平行光。朝向决定光束与地面树影的方向；颜色和强度同时影响场景与人物。");
        explanation.style.whiteSpace = WhiteSpace.Normal;
        sunlight.Add(explanation);
        var serialized = new SerializedObject(source);
        SourceField("sourceEnabled", "开启阳光");
        SourceField("rotation", "朝向 / 欧拉角 XYZ");
        SourceField("colour", "阳光颜色");
        SourceField("intensity", "阳光强度");
        SourceField("shadowStrength", "遮挡阴影强度");
        sunlight.Bind(serialized);

        var quality = Section(scroll, "性能与质量");
        var active = new Toggle("开启体积光") { name = "volume-enabled" };
        quality.Add(active);
        refreshers.Add(() => active.SetValueWithoutNotify(feature.isActive));
        active.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(feature, "切换 MVP03 体积光");
            feature.SetActive(evt.newValue);
            Changed();
        });
        var options = new List<string> { "性能 · 1/3 分辨率 / 32 步", "标准 · 1/2 分辨率 / 40 步", "精细 · 全分辨率 / 64 步" };
        var preset = new DropdownField("体积光质量", options, 0) { name = "volume-quality" };
        quality.Add(preset);
        refreshers.Add(() => preset.SetValueWithoutNotify(options[feature.resolutionDivisor == 3 ? 0 : feature.resolutionDivisor == 2 ? 1 : 2]));
        preset.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(feature, "调整 MVP03 体积光质量");
            int index = preset.index;
            feature.resolutionDivisor = index == 0 ? 3 : index == 1 ? 2 : 1;
            feature.viewSteps = index == 0 ? 32 : index == 1 ? 40 : 64;
            feature.lightSteps = index == 0 ? 1 : index == 1 ? 2 : 4;
            Changed();
        });
        var denoise = new Slider("体积光降噪", 0, 1) { name = "volume-denoise", showInputField = true };
        quality.Add(denoise);
        refreshers.Add(() => denoise.SetValueWithoutNotify(feature.denoiseStrength));
        denoise.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(feature, "调整 MVP03 降噪");
            feature.denoiseStrength = evt.newValue;
            Changed();
        });

        var air = Section(scroll, "空气介质");
        Slider(air, "消光系数 / 米", "density", "_Density", 0, .2f, "吸收与散射之和；越高空气越浓。0 关闭介质。");
        Slider(air, "散射反照率", "albedo", "_ScatteringAlbedo", 0, 1, "消光中重新散射的比例；0 只吸收光线。");
        Slider(air, "前向散射 g", "anisotropy", "_Anisotropy", -.8f, .8f, "控制观察方向与亮度的关系；0 为各向同性。");
        Slider(air, "密度起伏", "noise-amount", "_NoiseAmount", 0, 1, "0 为均匀空气；增加后形成疏密不同的雾丝。");
        Slider(air, "密度噪声频率", "noise-frequency", "_NoiseScale", .05f, 2, "越低雾团越大；降噪会保留平滑边缘。");
        var motion = Section(scroll, "空气流动 · 世界空间");
        Slider(motion, "流速 / 米每秒", "flow-speed", "_FlowSpeed", 0, 2, "0 冻结空气流动。");
        var direction = new Vector3Field("风向 XYZ") { name = "flow-direction" };
        motion.Add(direction);
        refreshers.Add(() => direction.SetValueWithoutNotify((Vector3)medium.GetVector("_FlowDirection")));
        direction.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(medium, "调整 MVP03 空气风向");
            Vector3 v = evt.newValue;
            medium.SetVector("_FlowDirection", new Vector4(v.x, v.y, v.z, 0));
            Changed();
        });
        Slider(motion, "扭曲幅度 / 米", "flow-warp", "_FlowWarp", 0, 3, "大尺度流场使空气轻微变形。");
        Slider(motion, "细层雾丝", "flow-detail", "_FlowDetail", 0, 1, "叠加较细的密度变化；0 跳过这一层采样。");

        var footer = Section(scroll, "预览与保存");
        var help = new Label("自动保存，支持 Ctrl+Z；Play 中调整也会保留。\nMVP03 参数独立保存；人物、环境与空气共享同一太阳光源。");
        help.style.whiteSpace = WhiteSpace.Normal;
        footer.Add(help);
        footer.Add(new Button(SaveSettings) { text = "立即保存", name = "save-settings" });
        footer.Add(new Button(() =>
        {
            var sun = Object.FindObjectsByType<CourtyardSun>(FindObjectsSortMode.None).FirstOrDefault(s => s.settings == source);
            if (sun != null) Selection.activeGameObject = sun.gameObject;
        }) { text = "选中场景太阳", name = "select-sun" });
        status = new Label("设置已载入") { name = "save-status" };
        footer.Add(status);
        RefreshControls();

        void SourceField(string property, string label)
        {
            var field = new PropertyField(serialized.FindProperty(property), label) { name = "source-" + property };
            field.style.marginTop = 6;
            sunlight.Add(field);
            field.RegisterCallback<SerializedPropertyChangeEvent>(_ => Changed());
        }
    }

    private static VisualElement Section(VisualElement parent, string heading)
    {
        var section = new VisualElement();
        section.style.marginTop = 16;
        section.style.paddingLeft = section.style.paddingRight = 12;
        section.style.paddingTop = section.style.paddingBottom = 12;
        section.style.backgroundColor = new Color(.19f, .20f, .23f);
        var title = new Label(heading);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.marginBottom = 10;
        section.Add(title);
        parent.Add(section);
        return section;
    }

    private void Slider(VisualElement parent, string label, string name, string property, float min, float max, string tooltip)
    {
        var field = new Slider(label, min, max) { name = name, showInputField = true, tooltip = tooltip };
        field.style.marginTop = 8;
        field.labelElement.style.minWidth = 130;
        parent.Add(field);
        refreshers.Add(() => field.SetValueWithoutNotify(medium.GetFloat(property)));
        field.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(medium, "调整 MVP03 空气：" + label);
            medium.SetFloat(property, Mathf.Clamp(evt.newValue, min, max));
            Changed();
        });
    }

    private void Changed()
    {
        if (medium != null) EditorUtility.SetDirty(medium);
        if (source != null) EditorUtility.SetDirty(source);
        if (feature != null) EditorUtility.SetDirty(feature);
        if (rendererData != null) EditorUtility.SetDirty(rendererData);
        foreach (var sun in Object.FindObjectsByType<CourtyardSun>(FindObjectsSortMode.None))
            if (sun.settings == source) sun.Apply();
        needsSave = true;
        if (status != null) status.text = "预览已更新 · 正在保存…";
        pendingSave?.Pause();
        pendingSave = rootVisualElement.schedule.Execute(SaveSettings).StartingIn(400);
        SceneView.RepaintAll();
        EditorApplication.QueuePlayerLoopUpdate();
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }

    public void SaveSettings()
    {
        pendingSave?.Pause();
        pendingSave = null;
        if (needsSave)
        {
            if (medium != null) AssetDatabase.SaveAssetIfDirty(medium);
            if (source != null) AssetDatabase.SaveAssetIfDirty(source);
            if (rendererData != null) AssetDatabase.SaveAssetIfDirty(rendererData);
            needsSave = false;
        }
        if (status != null) status.text = "已保存 · 停止运行后仍保留";
    }

    private void RefreshControls() { if (medium != null) foreach (var refresh in refreshers) refresh(); }
    private void OnEnable() => Undo.undoRedoPerformed += OnUndoRedo;
    private void OnDisable() { Undo.undoRedoPerformed -= OnUndoRedo; SaveSettings(); }
    private void OnFocus() => RefreshControls();
    private void OnUndoRedo() { RefreshControls(); Changed(); }
}
