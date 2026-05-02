using UnityEngine;
using AnimalFall.UI;

namespace AnimalFall.Managers
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [SerializeField] private GameUIManager ui;

        private int score;
        private float comboMultiplier = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void AddPoints(int pts)
        {
            int applied = Mathf.RoundToInt(pts * comboMultiplier);
            score += applied;
            ui?.UpdateScoreText(score);
        }

        public void SetComboMultiplier(float multiplier)
        {
            comboMultiplier = multiplier;
            ui?.UpdateComboUI(comboMultiplier);
        }

        public void ResetScore()
        {
            score = 0;
            comboMultiplier = 1f;
            ui?.UpdateScoreText(score);
        }

        public int GetScore() => score;
    }
}
