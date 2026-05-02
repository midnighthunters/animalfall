using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AnimalFall.UI.Components;

namespace AnimalFall.UI
{
    public class GameUIManager : MonoBehaviour
    {
        [Header("HUD Texts")]
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text targetText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text comboText;

        [Header("HUD Elements")]
        [SerializeField] private Image progressBar;
        [SerializeField] private Image timerWarningOverlay;

        [Header("Panels")]
        [SerializeField] private GameObject levelCompletePanel;
        [SerializeField] private GameObject levelFailPanel;
        [SerializeField] private TMP_Text completionScoreText;
        [SerializeField] private TMP_Text completionCoinsText;
        [SerializeField] private TMP_Text failScoreText;

        [Header("Components")]
        [SerializeField] private FloatingTextSpawner floatingTextSpawner;
        [SerializeField] private ToastNotification toastNotification;

        [Header("Settings")]
        [SerializeField] private float timerWarningThreshold = 10f;
        [SerializeField] private Color timerNormalColor = Color.white;
        [SerializeField] private Color timerWarningColor = Color.red;

        private Coroutine timerPulseCoroutine;

        public void UpdateTimer(float seconds)
        {
            int displaySeconds = Mathf.CeilToInt(Mathf.Max(0, seconds));
            timerText.text = displaySeconds.ToString("00") + "s";

            if (seconds <= timerWarningThreshold && seconds > 0)
            {
                timerText.color = timerWarningColor;
                if (timerPulseCoroutine == null)
                    timerPulseCoroutine = StartCoroutine(PulseTimer());
            }
            else
            {
                timerText.color = timerNormalColor;
                if (timerPulseCoroutine != null)
                {
                    StopCoroutine(timerPulseCoroutine);
                    timerPulseCoroutine = null;
                    timerText.transform.localScale = Vector3.one;
                }
            }
        }

        public void UpdateTargetText(int current, int target)
        {
            targetText.text = $"{current} / {target}";
        }

        public void UpdateScoreText(int score)
        {
            scoreText.text = score.ToString("N0");
        }

        public void SetProgress(float t)
        {
            progressBar.fillAmount = t;
        }

        public void UpdateComboUI(float multiplier)
        {
            if (multiplier > 1f)
            {
                comboText.gameObject.SetActive(true);
                comboText.text = $"x{multiplier:0.0}";
            }
            else
            {
                comboText.gameObject.SetActive(false);
            }
        }

        public void ShowLevelComplete(int score, int coins)
        {
            if (levelCompletePanel == null) return;
            levelCompletePanel.SetActive(true);
            if (completionScoreText != null) completionScoreText.text = $"Score: {score:N0}";
            if (completionCoinsText != null) completionCoinsText.text = $"+{coins} Coins";
        }

        public void ShowLevelFailed(int score)
        {
            if (levelFailPanel == null) return;
            levelFailPanel.SetActive(true);
            if (failScoreText != null) failScoreText.text = $"Score: {score:N0}";
        }

        public void ShowFloatingText(string text, Vector3 screenPos)
        {
            if (floatingTextSpawner != null)
                floatingTextSpawner.Spawn(text, screenPos);
        }

        public void ShowToast(string message)
        {
            if (toastNotification != null)
                toastNotification.Show(message);
        }

        public void HideAllPanels()
        {
            if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
            if (levelFailPanel != null) levelFailPanel.SetActive(false);
        }

        private IEnumerator PulseTimer()
        {
            while (true)
            {
                float t = Mathf.PingPong(Time.time * 3f, 1f);
                float scale = Mathf.Lerp(1f, 1.15f, t);
                timerText.transform.localScale = Vector3.one * scale;
                yield return null;
            }
        }
    }
}
