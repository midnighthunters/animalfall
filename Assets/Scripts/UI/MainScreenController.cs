// MainScreenController — Main menu screen matching reference design
// Top bar: avatar circle + lives (Full/count) + coin count
// Center: background image + large Level button
// Bottom bar: 5 tab icons (Map, Trophy, Castle, Pets, Shop)
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AnimalFall.Managers;
using AnimalFall.Services;

namespace AnimalFall.UI
{
    public class MainScreenController : MonoBehaviour
    {
        [Header("Top Bar")]
        [SerializeField] private Image _avatarImage;
        [SerializeField] private Image _livesIcon;
        [SerializeField] private TextMeshProUGUI _livesText;
        [SerializeField] private Image _coinsIcon;
        [SerializeField] private TextMeshProUGUI _coinsText;
        [SerializeField] private Image _topBarBg;

        [Header("Center / Background")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Button _playButton;
        [SerializeField] private TextMeshProUGUI _levelButtonText;
        [SerializeField] private Image _levelButtonBg;

        [Header("Bottom Bar")]
        [SerializeField] private Image _bottomBarBg;
        [SerializeField] private Button _mapTabButton;
        [SerializeField] private Button _trophyTabButton;
        [SerializeField] private Button _castleTabButton; // center, elevated
        [SerializeField] private Button _petsTabButton;
        [SerializeField] private Button _shopTabButton;

        [Header("Tab Icons")]
        [SerializeField] private Image _mapTabIcon;
        [SerializeField] private Image _trophyTabIcon;
        [SerializeField] private Image _castleTabIcon;
        [SerializeField] private Image _petsTabIcon;
        [SerializeField] private Image _shopTabIcon;

        private SaveService _save;
        private int _currentLevel;
        private GameObject _noLivesPopup;

        private void Start()
        {
            // Grab SaveService from existing scene or bootstrap
            _save = FindFirstObjectByType<SaveService>();
            RefreshUI();

            if (_playButton != null)
                _playButton.onClick.AddListener(OnPlayPressed);
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (_save != null)
            {
                _currentLevel = _save.GetHighestUnlockedLevel();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                int debugLevel = PlayerPrefs.GetInt("AnimalFall.DebugLevel", -1);
                if (debugLevel >= 0 && LevelManager.Instance != null && LevelManager.Instance.GetLevelData(debugLevel) != null)
                    _currentLevel = debugLevel;
#endif

                // Coins
                if (_coinsText != null)
                    _coinsText.text = _save.GetCoins().ToString("N0");

                // Lives
                int lives = LivesManager.Instance != null
                    ? LivesManager.Instance.CurrentLives
                    : _save.GetLives();

                if (_livesText != null)
                    _livesText.text = lives >= 5 ? "Full" : lives.ToString();
            }

            // Level button label (1-indexed display)
            if (_levelButtonText != null)
                _levelButtonText.text = $"Level {_currentLevel + 1}";
        }

        public void OnPlayPressed()
        {
            if (LivesManager.Instance != null && !LivesManager.Instance.HasLives())
            {
                ShowNoLivesPopup();
                return;
            }

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.LoadGameSceneForLevel(_currentLevel);
            }
            else
            {
                // Fallback: load scene directly if LevelManager not yet initialised
                Debug.LogWarning("[MainScreen] LevelManager.Instance is null — loading GameScene directly.");
                UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
            }
        }

        private void ShowNoLivesPopup()
        {
            if (_noLivesPopup != null)
            {
                _noLivesPopup.SetActive(true);
                _noLivesPopup.transform.SetAsLastSibling();
                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[MainScreen] Cannot show the no-lives popup because no parent Canvas was found.");
                return;
            }

            _noLivesPopup = new GameObject("NoLivesPopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _noLivesPopup.transform.SetParent(canvas.transform, false);
            _noLivesPopup.transform.SetAsLastSibling();

            RectTransform overlayRect = _noLivesPopup.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            _noLivesPopup.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.62f);

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(_noLivesPopup.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(620f, 350f);
            panelRect.anchoredPosition = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.10f, 0.22f, 0.42f, 1f);

            TMP_FontAsset popupFont = _levelButtonText != null
                ? _levelButtonText.font
                : _livesText != null ? _livesText.font : null;
            CreatePopupText("Title", panel.transform, "OUT OF LIVES", popupFont, 46f, new Vector2(0f, 0.62f), new Vector2(1f, 0.92f));
            CreatePopupText("Message", panel.transform, "Your lives are regenerating.\nPlease try again soon!", popupFont, 29f, new Vector2(0.08f, 0.31f), new Vector2(0.92f, 0.58f));

            var closeButton = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            closeButton.transform.SetParent(panel.transform, false);
            RectTransform buttonRect = closeButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0.16f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(260f, 82f);
            closeButton.GetComponent<Image>().color = new Color(0.20f, 0.66f, 1f, 1f);

            Button button = closeButton.GetComponent<Button>();
            button.targetGraphic = closeButton.GetComponent<Image>();
            button.onClick.AddListener(CloseNoLivesPopup);
            CreatePopupText("Label", closeButton.transform, "OK", popupFont, 34f, Vector2.zero, Vector2.one);
        }

        private static TextMeshProUGUI CreatePopupText(
            string name,
            Transform parent,
            string value,
            TMP_FontAsset font,
            float fontSize,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            if (font != null) text.font = font;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private void CloseNoLivesPopup()
        {
            if (_noLivesPopup == null) return;
            _noLivesPopup.SetActive(false);
            RefreshUI();
        }
    }
}
