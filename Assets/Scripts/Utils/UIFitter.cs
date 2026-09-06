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
            Canvas canvas = canvasScaler.GetComponent<Canvas>();
            float viewportWidth = canvas != null ? canvas.pixelRect.width : Screen.width;
            float viewportHeight = canvas != null ? canvas.pixelRect.height : Screen.height;
            if (viewportWidth <= 0f || viewportHeight <= 0f) return;
            if (Mathf.Approximately(canvasScaler.referenceResolution.x, viewportWidth)) return;

            float imageResRatio = rectTransform.sizeDelta.x / rectTransform.sizeDelta.y;
            float oldRatio = canvasScaler.referenceResolution.x / canvasScaler.referenceResolution.y;
            float newRatio = viewportWidth / viewportHeight;

            if (oldRatio > newRatio) return;

            float changePercent = ((newRatio - oldRatio) * 100f) / oldRatio;
            float newX = rectTransform.sizeDelta.x + (rectTransform.sizeDelta.x * (changePercent / 100f));
            float newY = newX / imageResRatio;

            rectTransform.sizeDelta = new Vector2(newX, newY);
        }
    }
}
