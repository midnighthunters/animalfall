using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AnimalFall.Core.Arcade;
using AnimalFall.Managers;
using AnimalFall.Services.Auth;
using AnimalFall.Services.Save;

namespace AnimalFall.UI.Screens
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Play")]
        [SerializeField] private Button playButton;
        [SerializeField] private TMP_Text levelText;

        [Header("Player Info")]
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text highScoreText;
        [SerializeField] private TMP_Text livesText;

        [Header("Navigation")]
        [SerializeField] private Button shopButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button leaderboardButton;
        [SerializeField] private Button logoutButton;
        [SerializeField] private Button journeyMapButton;
        [SerializeField] private Button eventsButton;
        [SerializeField] private Button arcadeButton;

        [Header("Panels")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject leaderboardPanel;
        [SerializeField] private GameObject journeyMapPanel;
        [SerializeField] private GameObject eventsPanel;

        [Header("Easter Egg")]
        [SerializeField] private Button[] cornerButtons;
        [SerializeField] private RectTransform shopkeeperIcon;

        private void Start()
        {
            SetupButtons();
            UpdateUI();
            SetupCornerButtons();
            SetupShopkeeperDrag();
        }

        private void SetupButtons()
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayClicked);

            if (shopButton != null) shopButton.onClick.AddListener(() => TogglePanel(shopPanel));
            if (settingsButton != null) settingsButton.onClick.AddListener(() => TogglePanel(settingsPanel));
            if (leaderboardButton != null) leaderboardButton.onClick.AddListener(() => TogglePanel(leaderboardPanel));
            if (journeyMapButton != null) journeyMapButton.onClick.AddListener(() => TogglePanel(journeyMapPanel));
            if (eventsButton != null) eventsButton.onClick.AddListener(() => TogglePanel(eventsPanel));
            if (arcadeButton != null) arcadeButton.onClick.AddListener(OnArcadeClicked);
            if (logoutButton != null) logoutButton.onClick.AddListener(OnLogoutClicked);
        }

        private void Update()
        {
            if (LivesManager.Instance != null && livesText != null)
            {
                int lives = LivesManager.Instance.CurrentLives;
                if (LivesManager.Instance.IsRegenerating)
                {
                    float time = LivesManager.Instance.TimeUntilNextLife;
                    int minutes = Mathf.FloorToInt(time / 60f);
                    int seconds = Mathf.FloorToInt(time % 60f);
                    livesText.text = $"Lives: {lives}/{LivesManager.Instance.MaxLives} ({minutes:00}:{seconds:00})";
                }
                else
                {
                    livesText.text = $"Lives: {lives}/{LivesManager.Instance.MaxLives}";
                }
            }
        }

        private void UpdateUI()
        {
            if (LevelManager.Instance != null)
            {
                int nextLevel = LevelManager.Instance.GetHighestUnlockedLevel();
                if (nextLevel >= LevelManager.Instance.TotalLevels)
                    nextLevel = 0;
                levelText.text = "Level " + (nextLevel + 1);
            }

            if (FirebaseAuthService.Instance != null && FirebaseAuthService.Instance.CurrentUser != null)
            {
                if (playerNameText != null)
                    playerNameText.text = FirebaseAuthService.Instance.CurrentUser.displayName;
            }
            else
            {
                if (playerNameText != null)
                    playerNameText.text = "Guest";
            }

            if (SaveService.Instance != null)
            {
                if (coinsText != null) coinsText.text = SaveService.Instance.GetCoins().ToString("N0");
                if (highScoreText != null) highScoreText.text = "Best: " + SaveService.Instance.GetHighScore().ToString("N0");
            }
        }

        private void OnPlayClicked()
        {
            if (LevelManager.Instance == null) return;

            if (LivesManager.Instance != null && !LivesManager.Instance.HasLives())
            {
                Debug.Log("[MainMenu] No lives remaining.");
                return;
            }

            int levelToLoad = LevelManager.Instance.GetHighestUnlockedLevel();
            if (levelToLoad >= LevelManager.Instance.TotalLevels)
                levelToLoad = 0;

            LevelManager.Instance.LoadGameSceneForLevel(levelToLoad);
        }

        private void OnArcadeClicked()
        {
            GameStateManager.Instance?.TransitionTo(GameState.ArcadeRoom);
        }

        private void OnLogoutClicked()
        {
            if (FirebaseAuthService.Instance != null)
            {
                FirebaseAuthService.Instance.Logout();
                UnityEngine.SceneManagement.SceneManager.LoadScene("AuthScene");
            }
        }

        private void SetupCornerButtons()
        {
            if (cornerButtons == null) return;
            for (int i = 0; i < cornerButtons.Length; i++)
            {
                if (cornerButtons[i] == null) continue;
                int cornerIdx = i;
                cornerButtons[i].onClick.AddListener(() =>
                    EasterEggManager.Instance?.OnCornerTapped(cornerIdx));
            }
        }

        private void SetupShopkeeperDrag()
        {
            if (shopkeeperIcon == null) return;

            var drag = shopkeeperIcon.gameObject.AddComponent<ShopkeeperDragHandler>();
            drag.onDraggedOffScreen = () =>
                EasterEggManager.Instance?.OnShopkeeperDraggedOffScreen();
        }

        private void TogglePanel(GameObject panel)
        {
            if (panel == null) return;

            CloseAllPanels();
            panel.SetActive(!panel.activeSelf);
        }

        private void CloseAllPanels()
        {
            if (shopPanel != null) shopPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
            if (journeyMapPanel != null) journeyMapPanel.SetActive(false);
            if (eventsPanel != null) eventsPanel.SetActive(false);
        }
    }
}
