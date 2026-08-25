using UnityEngine;

namespace AnimalFall.MegaShooter
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class MegaSafeArea : MonoBehaviour
    {
        private Rect _last;

        private void OnEnable() => Apply();

        private void Update()
        {
            if (Screen.safeArea != _last) Apply();
        }

        private void Apply()
        {
            _last = Screen.safeArea;
            RectTransform rect = (RectTransform)transform;
            Vector2 min = _last.position;
            Vector2 max = _last.position + _last.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
