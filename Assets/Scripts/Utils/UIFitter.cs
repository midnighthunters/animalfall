using UnityEngine;
using UnityEngine.UI;

namespace AnimalFall.Utils
{
    [RequireComponent(typeof(RectTransform))]
    public class UIFitter : MonoBehaviour
    {
        [SerializeField] private CanvasScaler canvasScaler;
        [SerializeField] private RectTransform rectTransform;

        private void Start()
        {
            if (canvasScaler == null || rectTransform == null) return;
            if (Mathf.Approximately(canvasScaler.referenceResolution.x, Screen.width)) return;

            float imageResRatio = rectTransform.sizeDelta.x / rectTransform.sizeDelta.y;
            float oldRatio = canvasScaler.referenceResolution.x / canvasScaler.referenceResolution.y;
            float newRatio = (float)Screen.width / Screen.height;

            if (oldRatio > newRatio) return;

            float changePercent = ((newRatio - oldRatio) * 100f) / oldRatio;
            float newX = rectTransform.sizeDelta.x + (rectTransform.sizeDelta.x * (changePercent / 100f));
            float newY = newX / imageResRatio;

            rectTransform.sizeDelta = new Vector2(newX, newY);
        }
    }
}
