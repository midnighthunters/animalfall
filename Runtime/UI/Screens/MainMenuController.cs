using UnityEngine;
using UnityEngine.UI;
using TMPro;
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

        [Header("Navigation")]
        [SerializeField] private Button shopButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button leaderboardButton;
        [SerializeField] private Button logoutButton;

        [Header("Panels")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject leaderboardPanel;

        private void Start()
        {
            SetupButtons();
            UpdateUI();
        }

        private void SetupButtons()
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayClicked);

            if (shopButton != null) shopButton.onClick.AddListener(() => TogglePanel(shopPanel));
            if (settingsButton != null) settingsButton.onClick.AddListener(() => TogglePanel(settingsPanel));
            if (leaderboardButton != null) leaderboardButton.onClick.AddListener(() => TogglePanel(leaderboardPanel));
            if (logoutButton != null) logoutButton.onClick.AddListener(OnLogoutClicked);
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

            int levelToLoad = LevelManager.Instance.GetHighestUnlockedLevel();
            if (levelToLoad >= LevelManager.Instance.TotalLevels)
                levelToLoad = 0;

            LevelManager.Instance.LoadGameSceneForLevel(levelToLoad);
        }

        private void OnLogoutClicked()
        {
            if (FirebaseAuthService.Instance != null)
            {
                FirebaseAuthService.Instance.Logout();
                UnityEngine.SceneManagement.SceneManager.LoadScene("AuthScene");
            }
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
        }
    }
}
