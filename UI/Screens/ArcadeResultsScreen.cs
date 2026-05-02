using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AnimalFall.Core.Arcade;

namespace AnimalFall.UI.Screens
{
    public class ArcadeResultsScreen : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text highScoreText;
        [SerializeField] private TMP_Text rewardText;
        [SerializeField] private TMP_Text newHighScoreLabel;

        [Header("Buttons")]
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button backButton;

        [Header("Root")]
        [SerializeField] private GameObject panelRoot;

        private void Awake()
        {
            if (panelRoot != null) panelRoot.SetActive(false);

            if (playAgainButton != null)
                playAgainButton.onClick.AddListener(OnPlayAgain);
            if (backButton != null)
                backButton.onClick.AddListener(OnBack);
        }

        private void OnEnable()
        {
            if (PhysicsMiniGameManager.Instance != null)
                PhysicsMiniGameManager.Instance.OnGameEnded += ShowResults;
        }

        private void OnDisable()
        {
            if (PhysicsMiniGameManager.Instance != null)
                PhysicsMiniGameManager.Instance.OnGameEnded -= ShowResults;
        }

        public void ShowResults(int score)
        {
            if (panelRoot != null) panelRoot.SetActive(true);

            var mgr = PhysicsMiniGameManager.Instance;
            if (mgr == null) return;

            if (titleText != null)
                titleText.text = mgr.ActiveConfig != null ? mgr.ActiveConfig.displayName + " Complete!" : "Game Complete!";

            if (scoreText != null)
                scoreText.text = "Score: " + score;

            int highScore = mgr.HighScore;
            if (highScoreText != null)
                highScoreText.text = "High Score: " + highScore;

            bool isNewHigh = score >= highScore && score > 0;
            if (newHighScoreLabel != null)
                newHighScoreLabel.gameObject.SetActive(isNewHigh);

            if (rewardText != null && mgr.ActiveConfig != null)
            {
                int coins = mgr.ActiveConfig.baseRewardCoins;
                if (score >= mgr.ActiveConfig.targetCount)
                    coins += mgr.ActiveConfig.perfectBonusCoins;
                rewardText.text = "Reward: " + coins + " coins";
            }
        }

        private void OnPlayAgain()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            // ArcadeRoomController handles replaying
        }

        private void OnBack()
        {
            if (panelRoot != null) panelRoot.SetActive(false);

            if (Managers.GameStateManager.Instance != null)
                Managers.GameStateManager.Instance.TransitionTo(Managers.GameState.MainMenu);
        }
    }
}
