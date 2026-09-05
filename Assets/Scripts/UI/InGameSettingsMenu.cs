using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using AnimalFall.Managers;

namespace AnimalFall.UI
{
    /// <summary>
    /// Builds the in-level settings affordance from the supplied Resources spritesheets.
    /// Keeping it runtime-built makes the control available in every GameScene variation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InGameSettingsMenu : MonoBehaviour
    {
        private const string BackgroundResource = "icons/settings_background";
        private const string IconsResource = "icons/settings_icons";

        private GameObject _root;
        private GameObject _actionsRoot;
        private Button _musicButton;
        private Button _soundButton;
        private Text _musicLabel;
        private Text _soundLabel;
        private Image _musicIcon;
        private Image _soundIcon;

        private Sprite _redButton;
        private Sprite _greenButton;
        private Sprite _exitIcon;
        private Sprite _musicIconSprite;
        private Sprite _soundIconSprite;
        private Sprite _settingsIcon;

        public void Build()
        {
            if (_root != null) return;

            Canvas canvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[InGameSettingsMenu] No Canvas was found for the settings control.");
                return;
            }

            Texture2D backgrounds = Resources.Load<Texture2D>(BackgroundResource);
            Texture2D icons = Resources.Load<Texture2D>(IconsResource);
            if (backgrounds == null || icons == null)
            {
                Debug.LogWarning("[InGameSettingsMenu] Settings spritesheets are missing from Resources/icons.");
                return;
            }

            // Button circles are laid out in the supplied 500 x 295 background sheet.
            _redButton = CreateSprite(backgrounds, new Rect(8f, 30f, 225f, 230f), "settings_red_button");
            _greenButton = CreateSprite(backgrounds, new Rect(255f, 30f, 225f, 230f), "settings_green_button");

            // The icon sheet is a 2 x 2 grid: exit, sound, music, settings.
            _exitIcon = CreateSprite(icons, new Rect(0f, 512f, 768f, 512f), "settings_exit_icon");
            _soundIconSprite = CreateSprite(icons, new Rect(768f, 512f, 768f, 512f), "settings_sound_icon");
            _musicIconSprite = CreateSprite(icons, new Rect(0f, 0f, 768f, 512f), "settings_music_icon");
            _settingsIcon = CreateSprite(icons, new Rect(768f, 0f, 768f, 512f), "settings_gear_icon");

            _root = new GameObject("InGameSettings", typeof(RectTransform));
            _root.transform.SetParent(canvas.transform, false);
            _root.transform.SetAsLastSibling();
            Stretch(_root.GetComponent<RectTransform>());

            Button trigger = CreateIconButton("SettingsButton", _root.transform, _settingsIcon,
                new Vector2(-76f, -82f), new Vector2(96f, 96f));
            trigger.onClick.AddListener(ToggleActions);

            _actionsRoot = new GameObject("SettingsActions", typeof(RectTransform));
            _actionsRoot.transform.SetParent(_root.transform, false);
            Stretch(_actionsRoot.GetComponent<RectTransform>());
            _actionsRoot.SetActive(false);

            CreateActionButton("ExitLevelButton", _redButton, _exitIcon, null,
                new Vector2(-220f, -80f), ExitLevel, out _, out _, out _);
            CreateActionButton("MusicButton", _greenButton, _musicIconSprite, null,
                new Vector2(-220f, -210f), ToggleMusic, out _musicButton, out _musicLabel, out _musicIcon);
            CreateActionButton("SoundButton", _greenButton, _soundIconSprite, null,
                new Vector2(-220f, -340f), ToggleSound, out _soundButton, out _soundLabel, out _soundIcon);

            RefreshAudioButtons();
        }

        private void OnEnable()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.OnMusicMutedChanged += HandleMuteChanged;
                AudioManager.Instance.OnSfxMutedChanged += HandleMuteChanged;
            }
            RefreshAudioButtons();
        }

        private void OnDisable()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.OnMusicMutedChanged -= HandleMuteChanged;
                AudioManager.Instance.OnSfxMutedChanged -= HandleMuteChanged;
            }
        }

        private void HandleMuteChanged(bool _) => RefreshAudioButtons();

        private void ToggleActions()
        {
            AudioManager.PlayClick();
            if (_actionsRoot == null) return;
            bool show = !_actionsRoot.activeSelf;
            _actionsRoot.SetActive(show);
            if (show) RefreshAudioButtons();
        }

        private void ExitLevel()
        {
            AudioManager.PlayClick();
            // Leaving a level in progress forfeits a life
            Time.timeScale = 1f;
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Running)
            {
                GameEvents.OnLevelFailed?.Invoke();
            }

            if (LevelManager.Instance != null)
                LevelManager.Instance.ReturnToMainScene();
            else
                SceneManager.LoadScene("MainScene");
        }

        private void ToggleMusic()
        {
            AudioManager.PlayClick();
            AudioManager.Instance?.ToggleMusicMuted();
            RefreshAudioButtons();
        }

        private void ToggleSound()
        {
            AudioManager.PlayClick();
            AudioManager.Instance?.ToggleSfxMuted();
            RefreshAudioButtons();
        }

        private void RefreshAudioButtons()
        {
            bool musicMuted = AudioManager.Instance != null && AudioManager.Instance.IsMusicMuted;
            bool soundMuted = AudioManager.Instance != null && AudioManager.Instance.IsSfxMuted;
            UpdateToggle(_musicButton, _musicLabel, _musicIcon, musicMuted, "MUSIC");
            UpdateToggle(_soundButton, _soundLabel, _soundIcon, soundMuted, "SOUND");
        }

        private void UpdateToggle(Button button, Text label, Image icon, bool muted, string title)
        {
            if (button == null) return;
            var buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.sprite = muted ? _redButton : _greenButton;
            }
            if (label != null) label.text = title + " " + (muted ? "OFF" : "ON");
            if (icon != null) icon.color = muted ? new Color(0.62f, 0.62f, 0.62f, 0.75f) : Color.white;
        }

        private void CreateActionButton(string name, Sprite background, Sprite icon, string label,
            Vector2 position, UnityEngine.Events.UnityAction action, out Button button, out Text text, out Image iconImage)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(_actionsRoot.transform, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(120f, 120f);
            rect.anchoredPosition = position;

            Image image = go.GetComponent<Image>();
            image.sprite = background;
            image.preserveAspect = true;
            button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            Vector2 iconPos = string.IsNullOrEmpty(label) ? Vector2.zero : new Vector2(0f, 12f);
            Vector2 iconSize = string.IsNullOrEmpty(label) ? new Vector2(76f, 76f) : new Vector2(70f, 70f);
            iconImage = CreateIcon("Icon", go.transform, icon, iconPos, iconSize);
            text = !string.IsNullOrEmpty(label) ? CreateLabel(go.transform, label) : null;
        }

        private static Button CreateIconButton(string name, Transform parent, Sprite icon, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Image image = go.GetComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
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

        private static Text CreateLabel(Transform parent, string value)
        {
            GameObject go = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(0f, 28f);
            rect.anchoredPosition = new Vector2(0f, 5f);

            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = 13;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static Sprite CreateSprite(Texture2D texture, Rect rect, string name)
        {
            Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, Vector4.zero, false);
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
