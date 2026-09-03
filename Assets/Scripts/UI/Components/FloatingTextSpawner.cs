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

        private class PooledText
        {
            public GameObject gameObject;
            public RectTransform rectTransform;
            public TMP_Text textComponent;
            public CanvasGroup canvasGroup;
        }

        private readonly System.Collections.Generic.Stack<PooledText> _pool = new System.Collections.Generic.Stack<PooledText>(16);

        private PooledText GetFromPool()
        {
            while (_pool.Count > 0)
            {
                var item = _pool.Pop();
                if (item != null && item.gameObject != null) return item;
            }

            if (floatingTextPrefab == null || parentCanvas == null) return null;
            GameObject obj = Instantiate(floatingTextPrefab, parentCanvas.transform);
            var cg = obj.GetComponent<CanvasGroup>() ?? obj.AddComponent<CanvasGroup>();
            return new PooledText
            {
                gameObject = obj,
                rectTransform = obj.GetComponent<RectTransform>(),
                textComponent = obj.GetComponentInChildren<TMP_Text>(),
                canvasGroup = cg
            };
        }

        public void Spawn(string text, Vector3 screenPosition)
        {
            PooledText item = GetFromPool();
            if (item == null) return;

            item.gameObject.SetActive(true);
            if (item.rectTransform != null) item.rectTransform.position = screenPosition;
            if (item.textComponent != null) item.textComponent.text = text;

            bool isPositive = !text.StartsWith("-");
            if (item.textComponent != null)
                item.textComponent.color = isPositive ? new Color(0.2f, 0.9f, 0.3f) : new Color(0.9f, 0.2f, 0.2f);

            if (item.canvasGroup != null) item.canvasGroup.alpha = 1f;

            StartCoroutine(AnimateFloatingText(item));
        }

        private IEnumerator AnimateFloatingText(PooledText item)
        {
            float elapsed = 0f;
            Vector3 startPos = item.rectTransform != null ? item.rectTransform.localPosition : Vector3.zero;

            while (elapsed < floatDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / floatDuration;

                if (item.rectTransform != null)
                    item.rectTransform.localPosition = startPos + Vector3.up * (floatDistance * t);

                float fadeStart = 1f - (fadeDuration / floatDuration);
                if (t > fadeStart && item.canvasGroup != null)
                    item.canvasGroup.alpha = Mathf.Lerp(1f, 0f, (t - fadeStart) / (1f - fadeStart));

                yield return null;
            }

            if (item.gameObject != null)
            {
                item.gameObject.SetActive(false);
                _pool.Push(item);
            }
        }
    }
}
