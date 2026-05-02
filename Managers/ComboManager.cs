using UnityEngine;

namespace AnimalFall.Managers
{
    public class ComboManager : MonoBehaviour
    {
        [SerializeField] private float comboWindow = 2f;

        private int consecutive;
        private float lastCorrectTime = -999f;

        public int Consecutive => consecutive;

        public void OnCorrect()
        {
            float now = Time.time;
            consecutive = (now - lastCorrectTime <= comboWindow) ? consecutive + 1 : 1;
            lastCorrectTime = now;
            ApplyComboBonus();
        }

        public void ResetCombo()
        {
            consecutive = 0;
        }

        private void ApplyComboBonus()
        {
            float multiplier = 1f + 0.1f * (consecutive - 1);
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.SetComboMultiplier(multiplier);
        }
    }
}
