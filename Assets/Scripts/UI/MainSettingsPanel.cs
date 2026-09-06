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
        private const string TermsOfUseUrl = "http://sites.google.com/view/aftou/home";
        private const string PrivacyPolicyUrl = "https://sites.google.com/view/afprivacypolicy/home";

        private Button _settingsButton;
        private GameObject _overlay;
        private Button _musicButton;
        private Button _soundButton;
        private Button _hapticsButton;
        private Image _musicIcon;
        private Image _soundIcon;
        private CanvasGroup _hapticsIcon;
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
            Sprite closeSprite = Slice(panels, new Rect(59f, 538f, 94f, 97f), "main_settings_close");
            Sprite musicSprite = Slice(icons, new Rect(220f, 15f, 460f, 470f), "main_settings_music");
            Sprite soundSprite = Slice(icons, new Rect(830f, 530f, 510f, 460f), "main_settings_sound");
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

            GameObject shadow = CreateImage("PanelShadow", _overlay.transform, panelSprite,
                new Vector2(16f, 52f), new Vector2(760f, 850f), new Color(0.02f, 0.12f, 0.32f, 0.45f));
            GameObject panel = CreateImage("SettingsPanel", _overlay.transform, panelSprite,
                new Vector2(0f, 70f), new Vector2(760f, 850f), Color.white);
            shadow.transform.SetSiblingIndex(panel.transform.GetSiblingIndex());

            CreateLabel("Title", panel.transform, "SETTINGS", new Vector2(0f, 330f), new Vector2(600f, 80f), 50f,
                new Color(0.04f, 0.26f, 0.60f, 1f), FontStyles.Bold);

            Button close = CreateImageButton("CloseButton", panel.transform, closeSprite, new Vector2(298f, 332f), new Vector2(76f, 78f));
            close.onClick.AddListener(() => _overlay.SetActive(false));

            CreateToggleButton(panel.transform, "MusicButton", musicSprite, new Vector2(-220f, 105f),
                ToggleMusic, out _musicButton, out _musicIcon);
            CreateToggleButton(panel.transform, "SoundButton", soundSprite, new Vector2(0f, 105f),
                ToggleSound, out _soundButton, out _soundIcon);
            CreateToggleButton(panel.transform, "HapticsButton", null, new Vector2(220f, 105f),
                ToggleHaptics, out _hapticsButton, out _);
            _hapticsIcon = CreateHapticsIcon(_hapticsButton.transform);

            CreateSolidRect("LinkDivider", panel.transform, new Vector2(0f, -48f), new Vector2(540f, 2f),
                new Color(0.20f, 0.42f, 0.66f, 0.28f));
            CreateTextLink("PrivacyPolicyButton", panel.transform, "PRIVACY POLICY", new Vector2(0f, -145f), OpenPrivacyPolicy);
            CreateTextLink("TermsOfUseButton", panel.transform, "TERMS OF USE", new Vector2(0f, -235f), OpenTermsOfUse);

            _overlay.SetActive(false);
            RefreshToggles();
        }

        private void CreateToggleButton(Transform parent, string name, Sprite iconSprite, Vector2 position,
            UnityEngine.Events.UnityAction action, out Button button, out Image icon)
        {
            button = CreateImageButton(name, parent, _greenToggle, position, new Vector2(164f, 164f));
            icon = iconSprite != null
                ? CreateIcon("Icon", button.transform, iconSprite, Vector2.zero, new Vector2(104f, 104f))
                : null;
            button.onClick.AddListener(action);
        }

        private static CanvasGroup CreateHapticsIcon(Transform parent)
        {
            GameObject root = new GameObject("Icon", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(104f, 104f);
            rect.anchoredPosition = Vector2.zero;

            CreateSolidRect("Phone", root.transform, Vector2.zero, new Vector2(38f, 72f), Color.white);
            CreateSolidRect("Screen", root.transform, new Vector2(0f, 2f), new Vector2(27f, 51f), new Color(0.07f, 0.35f, 0.62f, 1f));
            CreateSolidRect("LeftPulseTop", root.transform, new Vector2(-31f, 14f), new Vector2(7f, 24f), Color.white, -18f);
            CreateSolidRect("LeftPulseBottom", root.transform, new Vector2(-31f, -14f), new Vector2(7f, 24f), Color.white, 18f);
            CreateSolidRect("RightPulseTop", root.transform, new Vector2(31f, 14f), new Vector2(7f, 24f), Color.white, 18f);
            CreateSolidRect("RightPulseBottom", root.transform, new Vector2(31f, -14f), new Vector2(7f, 24f), Color.white, -18f);
            return root.GetComponent<CanvasGroup>();
        }

        private static void CreateTextLink(string name, Transform parent, string value, Vector2 position,
            UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(560f, 72f);
            rect.anchoredPosition = position;
            Image image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.001f);
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            CreateLabel("Label", go.transform, value, Vector2.zero, new Vector2(540f, 64f), 27f,
                new Color(0.04f, 0.32f, 0.70f, 1f), FontStyles.Bold | FontStyles.Underline);
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
            UpdateToggle(_musicButton, _musicIcon, musicOn);
            UpdateToggle(_soundButton, _soundIcon, soundOn);
            UpdateToggle(_hapticsButton, null, _hapticsEnabled);
            if (_hapticsIcon != null) _hapticsIcon.alpha = _hapticsEnabled ? 1f : 0.55f;
        }

        private void UpdateToggle(Button button, Image icon, bool enabled)
        {
            if (button == null) return;
            Image background = button.GetComponent<Image>();
            if (background != null) background.sprite = enabled ? _greenToggle : _redToggle;
            if (background != null) background.color = Color.white;
            if (icon != null) icon.color = enabled ? Color.white : new Color(1f, 1f, 1f, 0.55f);
        }

        private void OpenPrivacyPolicy()
        {
            Application.OpenURL(PrivacyPolicyUrl);
        }

        private void OpenTermsOfUse()
        {
            Application.OpenURL(TermsOfUseUrl);
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

        private static Image CreateSolidRect(string name, Transform parent, Vector2 position, Vector2 size, Color color,
            float rotation = 0f)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localEulerAngles = new Vector3(0f, 0f, rotation);
            Image image = go.GetComponent<Image>();
            image.color = color;
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
