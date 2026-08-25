// Task 6.3 — ComboManager: pitch steps, combo thresholds, multipliers
using UnityEngine;

namespace AnimalFall.Managers
{
    public class ComboManager : MonoBehaviour
    {
        [SerializeField] private ScoreManager _scoreManager;
        [SerializeField] private AudioManager _audioManager;

        private static readonly float[] PITCH_STEPS        = { 0.95f, 1.0f, 1.05f, 1.1f, 1.15f };
        private static readonly int[]   COMBO_THRESHOLDS   = { 3, 6, 10, 15 };
        private static readonly float[] COMBO_MULTIPLIERS  = { 1.5f, 2.0f, 3.0f, 5.0f };

        private int   _combo;
        private int   _pitchIndex;

        public int CurrentCombo => _combo;

        public void ResetCombo()
        {
            _combo      = 0;
            _pitchIndex = 0;
            GameEvents.OnComboChanged?.Invoke(0, 1.0f);
            _scoreManager?.SetComboMultiplier(1.0f);
        }

        public void OnCorrect()
        {
            _combo++;
            _pitchIndex = Mathf.Min(_combo - 1, PITCH_STEPS.Length - 1);
            float pitch = PITCH_STEPS[_pitchIndex];

            float multiplier = 1.0f;
            for (int i = COMBO_THRESHOLDS.Length - 1; i >= 0; i--)
            {
                if (_combo >= COMBO_THRESHOLDS[i]) { multiplier = COMBO_MULTIPLIERS[i]; break; }
            }

            _scoreManager?.SetComboMultiplier(multiplier);
            _audioManager?.PlaySFX(SfxType.Collect, pitch);
            GameEvents.OnComboChanged?.Invoke(_combo, multiplier);

            // Mega combo border flash at 10 consecutive
            if (_combo == 10)
            {
                Effects.ScreenEffects.Instance?.BorderFlashGold();
                GameEvents.OnSfxRequested?.Invoke(SfxType.MegaCombo);
            }
        }

        public void OnWrong()
        {
            ResetCombo();
        }
    }
}
