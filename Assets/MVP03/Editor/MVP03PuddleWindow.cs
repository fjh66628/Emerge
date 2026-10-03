using System.Collections.Generic;
using MVP03;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class MVP03PuddleWindow : EditorWindow
{
    private Material material;
    private readonly List<System.Action> refreshers = new List<System.Action>();
    private IVisualElementScheduledItem pendingSave;
    private Label status;
    private bool needsSave;

    [MenuItem("MVP03/水渍设置")]
    public static void Open()
    {
        var window = GetWindow<MVP03PuddleWindow>();
        window.titleContent = new GUIContent("MVP03 水渍");
        window.minSize = new Vector2(420, 570);
        window.Show();
    }

    public void CreateGUI()
    {
        rootVisualElement.Clear(); refreshers.Clear();
        material = AssetDatabase.LoadAssetAtPath<Material>(MVP03PuddleBuilder.MaterialPath);
        var scroll = new ScrollView();
        scroll.style.paddingLeft = scroll.style.paddingRight = 18;
        scroll.style.paddingTop = scroll.style.paddingBottom = 16;
        rootVisualElement.Add(scroll);
        var title = new Label("MVP03 · 水渍与潮湿地面");
        title.style.fontSize = 21; title.style.unityFontStyleAndWeight = FontStyle.Bold;
        scroll.Add(title);
        scroll.Add(new HelpBox("左键点地面，后台生成积水；重叠区域合并。第一版镜面效果 + 水渍贴图。参数即时生效，停止 Play 后保留，支持 Ctrl+Z。", HelpBoxMessageType.Info));
        if (material == null) { scroll.Add(new Label("请先执行 MVP03 > Add Click-to-Place Puddles。")); return; }
        Heading(scroll, "水渍形状");
        var texture = new ObjectField("灰度水渍贴图") { name = "stain-texture", objectType = typeof(Texture2D), allowSceneObjects = false };
        scroll.Add(texture); refreshers.Add(() => texture.SetValueWithoutNotify(material.GetTexture("_StainMap")));
        texture.RegisterValueChangedCallback(evt =>
        {
            var value = evt.newValue as Texture2D;
            if (value == null) { texture.SetValueWithoutNotify(material.GetTexture("_StainMap")); return; }
            if (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(value)) is TextureImporter settings && settings.sRGBTexture)
            {
                if (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(value)) is TextureImporter importer)
                {
                    importer.sRGBTexture = false; importer.wrapMode = TextureWrapMode.Clamp;
                    importer.SaveAndReimport();
                }
            }
            Undo.RecordObject(material, "更换地面水渍贴图"); material.SetTexture("_StainMap", value);
            RebuildMasks(); Changed();
        });
        Slider(scroll, "积水核心阈值", "water-threshold", "_WaterThreshold", .15f, .9f, "越高，只有贴图更亮的区域产生水面倒影。");
        Slider(scroll, "边缘过渡", "edge-softness", "_EdgeSoftness", .02f, .3f, "调整湿痕到积水的过渡，轮廓来自水渍贴图。");
        Slider(scroll, "湿润深浅", "wet-darkening", "_WetDarkening", 0, .6f, "石板被水浸湿后的变暗程度；0 保留原亮度。");
        Heading(scroll, "反射与表面");
        Slider(scroll, "反射强度", "reflection-strength", "_ReflectionStrength", 0, 1, "第一版镜面反射，默认 0.48；掠射角更明显。0 关闭倒影与高光。");
        Slider(scroll, "倒影模糊", "reflection-blur", "_ReflectionBlur", 0, 6, "0 清晰；较高值让倒影更柔和。");
        Slider(scroll, "阳光高光", "sun-highlight", "_SunHighlight", 0, 1, "降低镜面亮点，避免潮湿地面像镀铬。");
        Slider(scroll, "光滑度", "smoothness", "_Smoothness", .7f, .99f, "控制太阳高光的集中程度。");
        Slider(scroll, "水面扰动", "ripple-strength", "_RippleStrength", 0, .02f, "轻微扭曲倒影；0 为静止平面。");
        var tint = new ColorField("湿润色调") { name = "wet-tint", showAlpha = false, hdr = false };
        scroll.Add(tint); refreshers.Add(() => tint.SetValueWithoutNotify(material.GetColor("_WetTint")));
        tint.RegisterValueChangedCallback(evt => { Undo.RecordObject(material, "调整水渍色调"); material.SetColor("_WetTint", evt.newValue); Changed(); });
        Heading(scroll, "保存");
        scroll.Add(new Button(() =>
        {
            Undo.RecordObject(material, "恢复第一版积水预设"); MVP03PuddleBuilder.ApplyOriginalDefaults(material);
            RefreshControls(); Changed();
        }) { text = "恢复第一版积水效果", name = "original-preset" });
        scroll.Add(new Button(SaveSettings) { text = "立即保存", name = "save-settings" });
        status = new Label("设置已载入") { name = "save-status" }; scroll.Add(status);
        RefreshControls();
    }

    private static void Heading(VisualElement parent, string title)
    {
        var label = new Label(title); label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.marginTop = 16; label.style.marginBottom = 8; parent.Add(label);
    }

    private void Slider(VisualElement parent, string label, string name, string property, float min, float max, string tooltip)
    {
        var field = new Slider(label, min, max) { name = name, showInputField = true, tooltip = tooltip };
        field.style.marginTop = 7; field.labelElement.style.minWidth = 115;
        parent.Add(field); refreshers.Add(() => field.SetValueWithoutNotify(material.GetFloat(property)));
        field.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(material, "调整水渍：" + label);
            material.SetFloat(property, Mathf.Clamp(evt.newValue, min, max)); Changed();
        });
    }

    private void Changed()
    {
        EditorUtility.SetDirty(material); needsSave = true;
        if (status != null) status.text = "预览已更新 · 正在保存…";
        pendingSave?.Pause(); pendingSave = rootVisualElement.schedule.Execute(SaveSettings).StartingIn(400);
        SceneView.RepaintAll(); EditorApplication.QueuePlayerLoopUpdate(); UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }

    public void SaveSettings()
    {
        pendingSave?.Pause(); pendingSave = null;
        if (needsSave && material != null) { AssetDatabase.SaveAssetIfDirty(material); needsSave = false; }
        if (status != null) status.text = "已保存 · 停止运行后仍保留";
    }

    private void RebuildMasks()
    {
        foreach (var puddles in Object.FindObjectsByType<ClickPuddles>(FindObjectsSortMode.None)) puddles.RebuildSurfaces();
    }
    private void RefreshControls() { if (material != null) foreach (var refresh in refreshers) refresh(); }
    private void OnEnable() => Undo.undoRedoPerformed += OnUndoRedo;
    private void OnDisable() { Undo.undoRedoPerformed -= OnUndoRedo; SaveSettings(); }
    private void OnFocus() => RefreshControls();
    private void OnUndoRedo() { RefreshControls(); RebuildMasks(); if (material != null) Changed(); }
}
