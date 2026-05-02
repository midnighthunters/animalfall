using System.Collections;
using UnityEngine;
using TMPro;

namespace AnimalFall.UI.Components
{
    public class FloatingTextSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject floatingTextPrefab;
        [SerializeField] private Canvas parentCanvas;
        [SerializeField] private float floatDuration = 1f;
        [SerializeField] private float floatDistance = 80f;
        [SerializeField] private float fadeDuration = 0.5f;

        public void Spawn(string text, Vector3 screenPosition)
        {
            if (floatingTextPrefab == null || parentCanvas == null) return;

            GameObject obj = Instantiate(floatingTextPrefab, parentCanvas.transform);
            RectTransform rt = obj.GetComponent<RectTransform>();
            TMP_Text tmp = obj.GetComponentInChildren<TMP_Text>();

            if (rt != null) rt.position = screenPosition;
            if (tmp != null) tmp.text = text;

            bool isPositive = !text.StartsWith("-");
            if (tmp != null)
                tmp.color = isPositive ? new Color(0.2f, 0.9f, 0.3f) : new Color(0.9f, 0.2f, 0.2f);

            StartCoroutine(AnimateFloatingText(obj, rt));
        }

        private IEnumerator AnimateFloatingText(GameObject obj, RectTransform rt)
        {
            float elapsed = 0f;
            Vector3 startPos = rt != null ? rt.localPosition : Vector3.zero;
            CanvasGroup cg = obj.GetComponent<CanvasGroup>();
            if (cg == null) cg = obj.AddComponent<CanvasGroup>();

            while (elapsed < floatDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / floatDuration;

                if (rt != null)
                    rt.localPosition = startPos + Vector3.up * (floatDistance * t);

                float fadeStart = 1f - (fadeDuration / floatDuration);
                if (t > fadeStart)
                    cg.alpha = Mathf.Lerp(1f, 0f, (t - fadeStart) / (1f - fadeStart));

                yield return null;
            }

            Destroy(obj);
        }
    }
}
