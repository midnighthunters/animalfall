using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AnimalFall.Managers;

namespace AnimalFall.UI
{
    /// <summary>
    /// Main-menu settings panel. It is intentionally runtime-built so the
    /// MainScene can reuse the supplied panel and icon spritesheets without
    /// touching the in-level settings UI.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MainSettingsPanel : MonoBehaviour
    {
        private const string PanelSheetPath = "panels/main_screen_panels";
        private const string IconsSheetPath = "icons/settings_icons";
        private const string ToggleSheetPath = "icons/settings_background";
        private const string HapticsKey = "AnimalFall.HapticsEnabled";

        [Header("Privacy")]
        [Tooltip("Optional privacy policy URL. Leave empty to keep the button as a safe placeholder.")]
        [SerializeField] private string _privacyPolicyUrl = "";

        private Button _settingsButton;
        private GameObject _overlay;
        private Button _musicButton;
        private Button _soundButton;
        private Button _hapticsButton;
        private TextMeshProUGUI _musicState;
        private TextMeshProUGUI _soundState;
        private TextMeshProUGUI _hapticsState;
        private Image _musicToggleBackground;
        private Image _soundToggleBackground;
        private Image _hapticsToggleBackground;
        private Sprite _greenToggle;
        private Sprite _redToggle;
        private bool _hapticsEnabled;

        private void Awake()
        {
            _hapticsEnabled = PlayerPrefs.GetInt(HapticsKey, 1) != 0;
            _settingsButton = FindSettingsButton();
            if (_settingsButton != null)
                _settingsButton.onClick.AddListener(TogglePanel);
        }

        private void OnDestroy()
        {
            if (_settingsButton != null)
                _settingsButton.onClick.RemoveListener(TogglePanel);
        }

        private Button FindSettingsButton()
        {
            Transform button = transform.Find("SettingsButton");
            return button != null ? button.GetComponent<Button>() : null;
        }

        private void TogglePanel()
        {
            AudioManager.PlayClick();
            if (_overlay == null) BuildPanel();
            if (_overlay == null) return;

            _overlay.SetActive(!_overlay.activeSelf);
            if (_overlay.activeSelf) RefreshToggles();
        }

        private void BuildPanel()
        {
            Texture2D panels = Resources.Load<Texture2D>(PanelSheetPath);
            Texture2D icons = Resources.Load<Texture2D>(IconsSheetPath);
            Texture2D toggles = Resources.Load<Texture2D>(ToggleSheetPath);
            if (panels == null || icons == null || toggles == null)
            {
                Debug.LogWarning("[MainSettingsPanel] Main settings spritesheets are missing.");
                return;
            }

            Sprite panelSprite = Slice(panels, new Rect(670f, 234f, 464f, 412f), "main_settings_panel");
            Sprite rowSprite = Slice(panels, new Rect(54f, 679f, 553f, 150f), "main_settings_row");
            Sprite privacySprite = Slice(panels, new Rect(1175f, 373f, 522f, 167f), "main_settings_privacy");
            Sprite closeSprite = Slice(panels, new Rect(59f, 538f, 94f, 97f), "main_settings_close");
            Sprite musicSprite = Slice(icons, new Rect(0f, 0f, 768f, 512f), "main_settings_music");
            Sprite soundSprite = Slice(icons, new Rect(768f, 512f, 768f, 512f), "main_settings_sound");
            Sprite hapticsSprite = Slice(icons, new Rect(768f, 0f, 768f, 512f), "main_settings_haptics");
            _greenToggle = Slice(toggles, new Rect(255f, 30f, 225f, 230f), "main_settings_toggle_on");
            _redToggle = Slice(toggles, new Rect(8f, 30f, 225f, 230f), "main_settings_toggle_off");

            _overlay = new GameObject("MainSettingsOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _overlay.transform.SetParent(transform, false);
            _overlay.transform.SetAsLastSibling();
            RectTransform overlayRect = _overlay.GetComponent<RectTransform>();
            Stretch(overlayRect);
            Image overlayImage = _overlay.GetComponent<Image>();
            overlayImage.color = new Color(0.02f, 0.08f, 0.20f, 0.68f);
            overlayImage.raycastTarget = true;

            GameObject panel = CreateImage("SettingsPanel", _overlay.transform, panelSprite,
                new Vector2(0f, 55f), new Vector2(820f, 1220f), new Color(1f, 1f, 1f, 1f));
            GameObject shadow = CreateImage("PanelShadow", _overlay.transform, panelSprite,
                new Vector2(18f, 28f), new Vector2(820f, 1220f), new Color(0.02f, 0.12f, 0.32f, 0.45f));
            shadow.transform.SetSiblingIndex(panel.transform.GetSiblingIndex());

            CreateLabel("Title", panel.transform, "SETTINGS", new Vector2(0f, 470f), new Vector2(680f, 88f), 52f,
                new Color(0.04f, 0.26f, 0.60f, 1f), FontStyles.Bold);
            CreateLabel("Subtitle", panel.transform, "Make your rescue feel just right", new Vector2(0f, 408f),
                new Vector2(650f, 46f), 22f, new Color(0.22f, 0.40f, 0.58f, 1f), FontStyles.Normal);

            Button close = CreateImageButton("CloseButton", panel.transform, closeSprite, new Vector2(330f, 485f), new Vector2(86f, 88f));
            close.onClick.AddListener(() => _overlay.SetActive(false));

            CreateToggleRow(panel.transform, rowSprite, musicSprite, "MUSIC", "Background music", 230f,
                out _musicButton, out _musicState, out _musicToggleBackground, ToggleMusic);
            CreateToggleRow(panel.transform, rowSprite, soundSprite, "SOUND", "Animal pops and effects", 65f,
                out _soundButton, out _soundState, out _soundToggleBackground, ToggleSound);
            CreateToggleRow(panel.transform, rowSprite, hapticsSprite, "HAPTICS", "Gentle vibration feedback", -100f,
                out _hapticsButton, out _hapticsState, out _hapticsToggleBackground, ToggleHaptics);

            Button privacy = CreateImageButton("PrivacyPolicyButton", panel.transform, privacySprite,
                new Vector2(0f, -285f), new Vector2(600f, 112f));
            CreateLabel("PrivacyLabel", privacy.transform, "PRIVACY POLICY", new Vector2(0f, 3f), new Vector2(560f, 70f),
                30f, Color.white, FontStyles.Bold);
            privacy.onClick.AddListener(OpenPrivacyPolicy);

            CreateLabel("Footer", panel.transform, "Your settings are saved on this device", new Vector2(0f, -385f),
                new Vector2(650f, 42f), 18f, new Color(0.30f, 0.46f, 0.62f, 1f), FontStyles.Normal);

            _overlay.SetActive(false);
            RefreshToggles();
        }

        private void CreateToggleRow(Transform parent, Sprite rowSprite, Sprite iconSprite, string title, string subtitle,
            float y, out Button button, out TextMeshProUGUI state, out Image background, UnityEngine.Events.UnityAction action)
        {
            GameObject row = CreateImage(title + "Row", parent, rowSprite, new Vector2(0f, y), new Vector2(680f, 138f), Color.white);
            CreateIcon("Icon", row.transform, iconSprite, new Vector2(-245f, 0f), new Vector2(86f, 76f));
            CreateLabel("Title", row.transform, title, new Vector2(-75f, 22f), new Vector2(280f, 42f), 28f,
                new Color(0.05f, 0.22f, 0.50f, 1f), FontStyles.Bold, TextAlignmentOptions.Left);
            CreateLabel("Subtitle", row.transform, subtitle, new Vector2(-75f, -20f), new Vector2(280f, 34f), 17f,
                new Color(0.20f, 0.38f, 0.56f, 1f), FontStyles.Normal, TextAlignmentOptions.Left);

            button = CreateImageButton(title + "Button", row.transform, _greenToggle,
                new Vector2(245f, 0f), new Vector2(112f, 84f));
            background = button.GetComponent<Image>();
            state = CreateLabel("State", button.transform, "ON", Vector2.zero, new Vector2(100f, 60f), 22f,
                Color.white, FontStyles.Bold);
            button.onClick.AddListener(action);
        }

        private void ToggleMusic()
        {
            AudioManager.PlayClick();
            AudioManager.Instance?.ToggleMusicMuted();
            RefreshToggles();
        }

        private void ToggleSound()
        {
            AudioManager.PlayClick();
            AudioManager.Instance?.ToggleSfxMuted();
            RefreshToggles();
        }

        private void ToggleHaptics()
        {
            AudioManager.PlayClick();
            _hapticsEnabled = !_hapticsEnabled;
            PlayerPrefs.SetInt(HapticsKey, _hapticsEnabled ? 1 : 0);
            PlayerPrefs.Save();
            if (_hapticsEnabled) Handheld.Vibrate();
            RefreshToggles();
        }

        private void RefreshToggles()
        {
            bool musicOn = AudioManager.Instance == null || !AudioManager.Instance.IsMusicMuted;
            bool soundOn = AudioManager.Instance == null || !AudioManager.Instance.IsSfxMuted;
            UpdateToggle(_musicButton, _musicState, _musicToggleBackground, musicOn);
            UpdateToggle(_soundButton, _soundState, _soundToggleBackground, soundOn);
            UpdateToggle(_hapticsButton, _hapticsState, _hapticsToggleBackground, _hapticsEnabled);
        }

        private void UpdateToggle(Button button, TextMeshProUGUI state, Image background, bool enabled)
        {
            if (button == null) return;
            if (state != null) state.text = enabled ? "ON" : "OFF";
            if (background != null) background.sprite = enabled ? _greenToggle : _redToggle;
            if (background != null) background.color = enabled ? Color.white : new Color(0.72f, 0.72f, 0.72f, 1f);
        }

        private void OpenPrivacyPolicy()
        {
            if (!string.IsNullOrWhiteSpace(_privacyPolicyUrl))
                Application.OpenURL(_privacyPolicyUrl);
            else
                Debug.Log("[MainSettingsPanel] Privacy Policy button pressed; no URL configured.");
        }

        private static GameObject CreateImage(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.color = color;
            image.raycastTarget = false;
            return go;
        }

        private static Button CreateImageButton(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = true;
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(1f, 1f, 1f, 1f),
                pressedColor = new Color(0.82f, 0.92f, 1f, 1f),
                selectedColor = Color.white,
                disabledColor = new Color(1f, 1f, 1f, 0.5f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
            return button;
        }

        private static Image CreateIcon(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI CreateLabel(string name, Transform parent, string value, Vector2 position,
            Vector2 size, float fontSize, Color color, FontStyles style,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.enableWordWrapping = false;
            text.raycastTarget = false;
            return text;
        }

        private static Sprite Slice(Texture2D texture, Rect rect, string name)
        {
            Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, Vector4.zero, false);
            sprite.name = name;
            return sprite;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
