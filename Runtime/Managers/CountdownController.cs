using System;
using System.Collections;
using UnityEngine;
using TMPro;

namespace AnimalFall.Managers
{
    public class CountdownController : MonoBehaviour
    {
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private float stepDuration = 0.5f;

        public IEnumerator PlayCountdown(Action onComplete)
        {
            countdownText.gameObject.SetActive(true);
            Time.timeScale = 0f;

            yield return ShowStep("3");
            yield return ShowStep("2");
            yield return ShowStep("1");
            yield return ShowStep("GO!");

            countdownText.gameObject.SetActive(false);
            Time.timeScale = 1f;

            onComplete?.Invoke();
        }

        private IEnumerator ShowStep(string value)
        {
            countdownText.text = value;
            countdownText.transform.localScale = Vector3.zero;

            float elapsed = 0f;
            while (elapsed < stepDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float scale = Mathf.Sin((elapsed / stepDuration) * Mathf.PI);
                countdownText.transform.localScale = Vector3.one * scale;
                yield return null;
            }
        }
    }
}
