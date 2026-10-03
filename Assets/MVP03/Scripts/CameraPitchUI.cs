using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MVP03
{
    [DisallowMultipleComponent, RequireComponent(typeof(PixelFollowCamera))]
    public sealed class CameraPitchUI : MonoBehaviour
    {
        public const string PreferenceKey = "MVP03.CameraPitch";
        public const float MinimumPitch = 5, MaximumPitch = 45, DefaultPitch = 18;
        private PixelFollowCamera follow;
        private GameObject canvasObject, ownedEventSystem;
        private Font font;
        private Slider slider;
        private Text readout;
        private bool savePending;
        private float saveAt;

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            follow = GetComponent<PixelFollowCamera>();
            if (EventSystem.current == null)
            {
                ownedEventSystem = new GameObject("MVP03 camera UI events", typeof(EventSystem), typeof(InputSystemUIInputModule));
                // Pointer-only controls leave WASD, arrows and Space with the player.
                ownedEventSystem.GetComponent<EventSystem>().sendNavigationEvents = false;
            }
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei UI", "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 18);
            canvasObject = new GameObject("MVP03 camera pitch UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;

            RectTransform panel = Rect("Camera pitch panel", canvasObject.transform, new Vector2(340, 110));
            panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.one;
            panel.anchoredPosition = new Vector2(-24, -24);
            Fill(panel, new Color(.055f, .07f, .095f, .94f));
            Label("Title", panel, "相机俯角", 19, new Vector2(18, -12), new Vector2(124, 28));
            readout = Label("Angle", panel, "", 20, new Vector2(158, -12), new Vector2(70, 28));
            readout.color = new Color(.91f, .80f, .58f);

            var resetRect = Rect("Reset pitch", panel, new Vector2(91, 28));
            resetRect.anchoredPosition = new Vector2(232, -12);
            var resetImage = Fill(resetRect, new Color(.23f, .25f, .28f, 1));
            var reset = resetRect.gameObject.AddComponent<Button>();
            reset.targetGraphic = resetImage; reset.navigation = new Navigation { mode = Navigation.Mode.None };
            var resetText = Label("Reset label", resetRect, "重置 18°", 14, Vector2.zero, resetRect.sizeDelta);
            resetText.alignment = TextAnchor.MiddleCenter;
            reset.onClick.AddListener(() => { slider.value = DefaultPitch; EventSystem.current?.SetSelectedGameObject(null); });

            var sliderRect = Rect("Camera pitch slider", panel, new Vector2(304, 30));
            sliderRect.anchoredPosition = new Vector2(18, -48);
            // A transparent graphic makes the full 30-pixel-high track clickable.
            Fill(sliderRect, Color.clear);
            var track = Rect("Track", sliderRect, Vector2.zero); Stretch(track, 0, 0, .4f, .6f);
            Fill(track, new Color(.30f, .33f, .38f, 1), false);
            var fillArea = Rect("Fill area", sliderRect, Vector2.zero); Stretch(fillArea, 0, 0, .4f, .6f);
            var fillRect = Rect("Fill", fillArea, Vector2.zero); Stretch(fillRect, 0, 0, 0, 1);
            Fill(fillRect, new Color(.74f, .62f, .40f, 1), false);
            var handleArea = Rect("Handle area", sliderRect, Vector2.zero); Stretch(handleArea, 0, 0, .1f, .9f);
            var handle = Rect("Handle", handleArea, new Vector2(14, 0));
            handle.anchorMin = handle.anchorMax = new Vector2(0, .5f); handle.pivot = new Vector2(.5f, .5f);
            var handleImage = Fill(handle, new Color(.98f, .88f, .66f, 1));
            slider = sliderRect.gameObject.AddComponent<Slider>();
            slider.fillRect = fillRect; slider.handleRect = handle; slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight; slider.minValue = MinimumPitch; slider.maxValue = MaximumPitch;
            slider.wholeNumbers = true; slider.navigation = new Navigation { mode = Navigation.Mode.None };
            slider.onValueChanged.AddListener(ApplyPitch);
            var low = Label("Minimum", panel, "5° · 低视角", 12, new Vector2(18, -80), new Vector2(140, 20));
            var high = Label("Maximum", panel, "高视角 · 45°", 12, new Vector2(182, -80), new Vector2(140, 20));
            low.color = high.color = new Color(.64f, .69f, .76f); high.alignment = TextAnchor.MiddleRight;

            float value = PlayerPrefs.GetFloat(PreferenceKey, follow.Pitch);
            if (float.IsNaN(value) || float.IsInfinity(value)) value = DefaultPitch;
            value = Mathf.Round(Mathf.Clamp(value, MinimumPitch, MaximumPitch));
            slider.SetValueWithoutNotify(value); follow.SetPitch(value); UpdateReadout(value);
        }

        private void ApplyPitch(float value)
        {
            follow.SetPitch(value); UpdateReadout(value);
            PlayerPrefs.SetFloat(PreferenceKey, value);
            savePending = true; saveAt = Time.unscaledTime + .4f;
        }

        private void UpdateReadout(float value) => readout.text = value.ToString("0") + "°";
        private void Update() { if (savePending && Time.unscaledTime >= saveAt) Save(); }
        private void OnApplicationFocus(bool focus) { if (!focus) Save(); }
        private void Save() { if (!savePending) return; PlayerPrefs.Save(); savePending = false; }
        private void OnDisable()
        {
            Save();
            if (canvasObject != null) { canvasObject.SetActive(false); Destroy(canvasObject); }
            if (ownedEventSystem != null) { ownedEventSystem.SetActive(false); Destroy(ownedEventSystem); }
            if (font != null) Destroy(font);
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = size; return rect;
        }

        private static void Stretch(RectTransform rect, float left, float right, float bottom, float top)
        {
            rect.anchorMin = new Vector2(0, bottom); rect.anchorMax = new Vector2(1, top);
            rect.pivot = new Vector2(.5f, .5f); rect.offsetMin = new Vector2(left, 0); rect.offsetMax = new Vector2(-right, 0);
        }

        private static Image Fill(RectTransform rect, Color color, bool raycast = true)
        {
            var graphic = rect.gameObject.AddComponent<Image>(); graphic.color = color; graphic.raycastTarget = raycast;
            return graphic;
        }

        private Text Label(string name, Transform parent, string text, int size, Vector2 position, Vector2 bounds)
        {
            var rect = Rect(name, parent, bounds); rect.anchoredPosition = position;
            var label = rect.gameObject.AddComponent<Text>(); label.font = font; label.fontSize = size;
            label.text = text; label.color = new Color(.9f, .92f, .95f); label.alignment = TextAnchor.MiddleLeft;
            label.raycastTarget = false; return label;
        }
    }
}
