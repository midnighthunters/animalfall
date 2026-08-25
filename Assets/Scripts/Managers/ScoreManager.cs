// Task 6.2 — ScoreManager: combo multiplier, star calculation
using UnityEngine;

namespace AnimalFall.Managers
{
    public class ScoreManager : MonoBehaviour
    {
        private int   _score;
        private float _comboMultiplier = 1f;

        public int   GetScore()   => _score;
        public float GetMultiplier() => _comboMultiplier;

        public void ResetScore()
        {
            _score           = 0;
            _comboMultiplier = 1f;
            GameEvents.OnScoreChanged?.Invoke(_score);
        }

        public void AddPoints(int basePoints)
        {
            _score += Mathf.RoundToInt(basePoints * _comboMultiplier);
            GameEvents.OnScoreChanged?.Invoke(_score);
        }

        public void SetComboMultiplier(float m)
        {
            _comboMultiplier = m;
        }

        /// <summary>
        /// Sole authoritative star-calculation method.
        /// 3★: rescued >= 100% AND timeRemaining >= totalTime * 0.3
        /// 2★: rescued >= 100%
        /// 1★: rescued >= 75%
        /// 0★: rescued < 75%
        /// </summary>
        public int CalculateStars(int rescued, int target, float timeRemaining, float totalTime)
        {
            if (target <= 0) return 0;
            float ratio = (float)rescued / target;

            if (ratio >= 1.0f && timeRemaining >= totalTime * 0.3f) return 3;
            if (ratio >= 1.0f)  return 2;
            if (ratio >= 0.75f) return 1;
            return 0;
        }
    }
}
