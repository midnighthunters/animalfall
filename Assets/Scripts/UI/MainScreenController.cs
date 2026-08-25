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
        [SerializeField] private Image     _avatarImage;
        [SerializeField] private Image     _livesIcon;
        [SerializeField] private TextMeshProUGUI _livesText;
        [SerializeField] private Image     _coinsIcon;
        [SerializeField] private TextMeshProUGUI _coinsText;
        [SerializeField] private Image     _topBarBg;

        [Header("Center / Background")]
        [SerializeField] private Image     _backgroundImage;
        [SerializeField] private Button    _playButton;
        [SerializeField] private TextMeshProUGUI _levelButtonText;
        [SerializeField] private Image     _levelButtonBg;

        [Header("Bottom Bar")]
        [SerializeField] private Image     _bottomBarBg;
        [SerializeField] private Button    _mapTabButton;
        [SerializeField] private Button    _trophyTabButton;
        [SerializeField] private Button    _castleTabButton;   // center, elevated
        [SerializeField] private Button    _petsTabButton;
        [SerializeField] private Button    _shopTabButton;

        [Header("Tab Icons")]
        [SerializeField] private Image     _mapTabIcon;
        [SerializeField] private Image     _trophyTabIcon;
        [SerializeField] private Image     _castleTabIcon;
        [SerializeField] private Image     _petsTabIcon;
        [SerializeField] private Image     _shopTabIcon;

        private SaveService _save;
        private int         _currentLevel;

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
                    _livesText.text = (lives >= 5) ? "Full" : lives.ToString();
            }

            // Level button label (1-indexed display)
            if (_levelButtonText != null)
                _levelButtonText.text = $"Level {_currentLevel + 1}";
        }

        public void OnPlayPressed()
        {
            if (LivesManager.Instance != null && !LivesManager.Instance.HasLives())
            {
                Debug.Log("[MainScreen] No lives — show refill popup");
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
    }
}
