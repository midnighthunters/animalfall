using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AnimalFall.Core.Arcade;

namespace AnimalFall.UI.Screens
{
    public class ArcadeHUD : MonoBehaviour
    {
        [Header("Common")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private Button exitButton;

        [Header("Gorilla Artillery")]
        [SerializeField] private TMP_Text ammoText;
        [SerializeField] private TMP_Text windText;
        [SerializeField] private Image windArrow;

        [Header("Rhino Demolition")]
        [SerializeField] private TMP_Text damageScoreText;
        [SerializeField] private TMP_Text requiredDamageText;

        [Header("Armadillo Ricochet")]
        [SerializeField] private TMP_Text slamChargesText;
        [SerializeField] private TMP_Text scarabsText;

        private void Start()
        {
            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitClicked);
        }

        private void Update()
        {
            var mgr = PhysicsMiniGameManager.Instance;
            if (mgr == null || !mgr.IsPlaying) return;

            if (scoreText != null)
                scoreText.text = "Score: " + mgr.ActiveGame.CurrentScore;

            if (timerText != null)
            {
                float t = mgr.RemainingTime;
                int mins = Mathf.FloorToInt(t / 60f);
                int secs = Mathf.FloorToInt(t % 60f);
                timerText.text = $"{mins:00}:{secs:00}";
            }
        }

        public void UpdateAmmo(int remaining, AmmoType type)
        {
            if (ammoText != null)
                ammoText.text = $"Ammo: {remaining} ({type})";
        }

        public void UpdateWind(float strength)
        {
            if (windText != null)
            {
                string dir = strength > 0 ? "→" : strength < 0 ? "←" : "-";
                windText.text = $"Wind: {dir} {Mathf.Abs(strength):F1}";
            }

            if (windArrow != null)
            {
                windArrow.transform.localScale = new Vector3(
                    Mathf.Sign(strength) * Mathf.Abs(strength) * 0.3f,
                    1f, 1f);
            }
        }

        public void UpdateDamageScore(float current, float required)
        {
            if (damageScoreText != null)
                damageScoreText.text = $"Damage: ${current:F0}";
            if (requiredDamageText != null)
                requiredDamageText.text = $"Target: ${required:F0}";
        }

        public void UpdateSlamCharges(int remaining)
        {
            if (slamChargesText != null)
                slamChargesText.text = $"Slams: {remaining}";
        }

        public void UpdateScarabs(int collected, int total)
        {
            if (scarabsText != null)
                scarabsText.text = $"Scarabs: {collected}/{total}";
        }

        private void OnExitClicked()
        {
            PhysicsMiniGameManager.Instance?.ForceEnd();
        }
    }
}
