using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AnimalFall.Core.MegaLevel;

namespace AnimalFall.UI.Screens
{
    public class VillainHUD : MonoBehaviour
    {
        [Header("HP Bar")]
        [SerializeField] private Image hpBarFill;
        [SerializeField] private Image hpBarBackground;
        [SerializeField] private TMP_Text hpText;

        [Header("Info")]
        [SerializeField] private TMP_Text villainNameText;
        [SerializeField] private TMP_Text phaseText;
        [SerializeField] private Image villainPortrait;

        [Header("Status")]
        [SerializeField] private GameObject vulnerableIndicator;
        [SerializeField] private GameObject shieldedIndicator;

        [Header("Colors")]
        [SerializeField] private Color healthyColor = Color.green;
        [SerializeField] private Color damagedColor = Color.yellow;
        [SerializeField] private Color criticalColor = Color.red;

        private Villain trackedVillain;

        public void Setup(Villain villain)
        {
            trackedVillain = villain;
            gameObject.SetActive(true);

            if (villain.Data != null)
            {
                villainNameText.text = villain.Data.villainName;
                if (villainPortrait != null && villain.Data.sprite != null)
                    villainPortrait.sprite = villain.Data.sprite;
            }

            villain.OnHPChanged += UpdateHP;
            villain.OnPhaseChanged += UpdatePhase;
            villain.OnDefeated += OnDefeated;

            UpdateHP(villain.CurrentHP, villain.MaxHP);
            UpdatePhase();
        }

        private void Update()
        {
            if (trackedVillain == null) return;

            bool isVulnerable = trackedVillain.IsVulnerable;
            if (vulnerableIndicator != null)
                vulnerableIndicator.SetActive(isVulnerable);
            if (shieldedIndicator != null)
                shieldedIndicator.SetActive(!isVulnerable && !trackedVillain.IsDefeated);
        }

        private void UpdateHP(int current, int max)
        {
            float percent = max > 0 ? (float)current / max : 0f;
            hpBarFill.fillAmount = percent;

            Color barColor;
            if (percent > 0.5f)
                barColor = Color.Lerp(damagedColor, healthyColor, (percent - 0.5f) * 2f);
            else
                barColor = Color.Lerp(criticalColor, damagedColor, percent * 2f);

            hpBarFill.color = barColor;
            hpText.text = $"{current}/{max}";
        }

        private void UpdatePhase()
        {
            if (trackedVillain == null) return;
            phaseText.text = $"Phase {trackedVillain.CurrentPhase}";
        }

        private void OnDefeated()
        {
            hpBarFill.fillAmount = 0;
            hpText.text = "DEFEATED";
            if (vulnerableIndicator != null) vulnerableIndicator.SetActive(false);
            if (shieldedIndicator != null) shieldedIndicator.SetActive(false);
        }

        public void Hide()
        {
            if (trackedVillain != null)
            {
                trackedVillain.OnHPChanged -= UpdateHP;
                trackedVillain.OnPhaseChanged -= UpdatePhase;
                trackedVillain.OnDefeated -= OnDefeated;
            }
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (trackedVillain != null)
            {
                trackedVillain.OnHPChanged -= UpdateHP;
                trackedVillain.OnPhaseChanged -= UpdatePhase;
                trackedVillain.OnDefeated -= OnDefeated;
            }
        }
    }
}
