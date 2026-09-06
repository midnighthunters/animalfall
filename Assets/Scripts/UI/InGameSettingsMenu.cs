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

            // Crop to the visible artwork so every icon is optically centred in its button.
            _exitIcon = CreateSprite(icons, new Rect(235f, 540f, 450f, 450f), "settings_exit_icon");
            _soundIconSprite = CreateSprite(icons, new Rect(830f, 530f, 510f, 460f), "settings_sound_icon");
            _musicIconSprite = CreateSprite(icons, new Rect(220f, 15f, 460f, 470f), "settings_music_icon");
            _settingsIcon = CreateSprite(icons, new Rect(845f, 20f, 480f, 470f), "settings_gear_icon");

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

            CreateActionButton("MusicButton", _greenButton, _musicIconSprite,
                new Vector2(-76f, -210f), ToggleMusic, out _musicButton, out _musicIcon);
            CreateActionButton("SoundButton", _greenButton, _soundIconSprite,
                new Vector2(-76f, -338f), ToggleSound, out _soundButton, out _soundIcon);
            CreateActionButton("ExitLevelButton", _redButton, _exitIcon,
                new Vector2(-76f, -466f), ExitLevel, out _, out _);

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
            UpdateToggle(_musicButton, _musicIcon, musicMuted);
            UpdateToggle(_soundButton, _soundIcon, soundMuted);
        }

        private void UpdateToggle(Button button, Image icon, bool muted)
        {
            if (button == null) return;
            var buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.sprite = muted ? _redButton : _greenButton;
            }
            if (icon != null) icon.color = muted ? new Color(0.62f, 0.62f, 0.62f, 0.75f) : Color.white;
        }

        private void CreateActionButton(string name, Sprite background, Sprite icon,
            Vector2 position, UnityEngine.Events.UnityAction action, out Button button, out Image iconImage)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(_actionsRoot.transform, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(108f, 108f);
            rect.anchoredPosition = position;

            Image image = go.GetComponent<Image>();
            image.sprite = background;
            image.preserveAspect = true;
            button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            iconImage = CreateIcon("Icon", go.transform, icon, Vector2.zero, new Vector2(72f, 72f));
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
