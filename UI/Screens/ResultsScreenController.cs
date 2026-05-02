using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using AnimalFall.Managers;

namespace AnimalFall.UI.Screens
{
    public class ResultsScreenController : MonoBehaviour
    {
        [Header("Common")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text coinsEarnedText;
        [SerializeField] private TMP_Text highScoreText;

        [Header("Star Rating")]
        [SerializeField] private Image[] starImages;
        [SerializeField] private Color starActiveColor = Color.yellow;
        [SerializeField] private Color starInactiveColor = new Color(0.3f, 0.3f, 0.3f);

        [Header("Buttons")]
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Mega Level")]
        [SerializeField] private GameObject villainDefeatedBanner;

        public void ShowWin(int score, int coins, bool isMegaLevel)
        {
            gameObject.SetActive(true);

            titleText.text = isMegaLevel ? "BOSS DEFEATED!" : "LEVEL COMPLETE!";
            scoreText.text = $"Score: {score:N0}";
            coinsEarnedText.text = $"+{coins} Coins";

            if (villainDefeatedBanner != null)
                villainDefeatedBanner.SetActive(isMegaLevel);

            int stars = CalculateStars(score);
            UpdateStars(stars);

            nextLevelButton.gameObject.SetActive(true);
            retryButton.gameObject.SetActive(true);
            mainMenuButton.gameObject.SetActive(true);

            SetupButtons();
        }

        public void ShowLose(int score)
        {
            gameObject.SetActive(true);

            titleText.text = "LEVEL FAILED";
            scoreText.text = $"Score: {score:N0}";
            coinsEarnedText.text = "";

            if (villainDefeatedBanner != null)
                villainDefeatedBanner.SetActive(false);

            UpdateStars(0);

            nextLevelButton.gameObject.SetActive(false);
            retryButton.gameObject.SetActive(true);
            mainMenuButton.gameObject.SetActive(true);

            SetupButtons();
        }

        private void SetupButtons()
        {
            nextLevelButton?.onClick.RemoveAllListeners();
            retryButton?.onClick.RemoveAllListeners();
            mainMenuButton?.onClick.RemoveAllListeners();

            nextLevelButton?.onClick.AddListener(OnNextLevel);
            retryButton?.onClick.AddListener(OnRetry);
            mainMenuButton?.onClick.AddListener(OnMainMenu);
        }

        private void OnNextLevel()
        {
            if (LevelManager.Instance != null)
            {
                int next = LevelManager.Instance.CurrentLevelIndex + 1;
                if (next < LevelManager.Instance.TotalLevels)
                    LevelManager.Instance.LoadGameSceneForLevel(next);
                else
                    SceneManager.LoadScene("MainScene");
            }
        }

        private void OnRetry()
        {
            if (LevelManager.Instance != null)
                LevelManager.Instance.LoadGameSceneForLevel(
                    LevelManager.Instance.CurrentLevelIndex);
        }

        private void OnMainMenu()
        {
            SceneManager.LoadScene("MainScene");
        }

        private int CalculateStars(int score)
        {
            if (score >= 5000) return 3;
            if (score >= 2500) return 2;
            if (score >= 1000) return 1;
            return 0;
        }

        private void UpdateStars(int count)
        {
            if (starImages == null) return;
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] != null)
                    starImages[i].color = i < count ? starActiveColor : starInactiveColor;
            }
        }
    }
}
