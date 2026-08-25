using System.Collections;
using UnityEngine;
using TMPro;

namespace AnimalFall.UI.Components
{
    public class ToastNotification : MonoBehaviour
    {
        [SerializeField] private GameObject toastPanel;
        [SerializeField] private TMP_Text toastText;
        [SerializeField] private float displayDuration = 2f;
        [SerializeField] private float fadeDuration = 0.3f;

        private Coroutine activeToast;
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            if (toastPanel != null)
            {
                canvasGroup = toastPanel.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = toastPanel.AddComponent<CanvasGroup>();
                toastPanel.SetActive(false);
            }
        }

        public void Show(string message)
        {
            if (activeToast != null)
                StopCoroutine(activeToast);
            activeToast = StartCoroutine(ShowToast(message));
        }

        private IEnumerator ShowToast(string message)
        {
            toastText.text = message;
            toastPanel.SetActive(true);

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;

            yield return new WaitForSecondsRealtime(displayDuration);

            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            toastPanel.SetActive(false);
            activeToast = null;
        }
    }
}
