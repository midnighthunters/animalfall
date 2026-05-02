using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace AnimalFall.Utils
{
    [RequireComponent(typeof(Canvas))]
    public class CanvasHelper : MonoBehaviour
    {
        public static UnityEvent OnResolutionOrOrientationChanged = new UnityEvent();

        private static readonly List<CanvasHelper> helpers = new List<CanvasHelper>();
        private static bool initialized;
        private static ScreenOrientation lastOrientation = ScreenOrientation.LandscapeLeft;
        private static Vector2 lastResolution = Vector2.zero;
        private static Rect lastSafeArea = Rect.zero;

        private Canvas canvas;
        private RectTransform safeAreaTransform;

        private void Awake()
        {
            if (!helpers.Contains(this))
                helpers.Add(this);

            canvas = GetComponent<Canvas>();
            safeAreaTransform = transform.Find("SafeArea") as RectTransform;

            if (!initialized)
            {
                lastOrientation = Screen.orientation;
                lastResolution = new Vector2(Screen.width, Screen.height);
                lastSafeArea = Screen.safeArea;
                initialized = true;
            }

            ApplySafeArea();
        }

        private void Update()
        {
            if (helpers.Count == 0 || helpers[0] != this) return;

            if (Application.isMobilePlatform && Screen.orientation != lastOrientation)
            {
                lastOrientation = Screen.orientation;
                lastResolution = new Vector2(Screen.width, Screen.height);
                OnResolutionOrOrientationChanged.Invoke();
            }

            if (Screen.safeArea != lastSafeArea)
            {
                lastSafeArea = Screen.safeArea;
                foreach (var helper in helpers)
                    helper.ApplySafeArea();
            }

            if (Screen.width != lastResolution.x || Screen.height != lastResolution.y)
            {
                lastResolution = new Vector2(Screen.width, Screen.height);
                OnResolutionOrOrientationChanged.Invoke();
            }
        }

        private void OnDestroy()
        {
            helpers.Remove(this);
        }

        private void ApplySafeArea()
        {
            if (safeAreaTransform == null) return;

            Rect safeArea = Screen.safeArea;
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= canvas.pixelRect.width;
            anchorMin.y /= canvas.pixelRect.height;
            anchorMax.x /= canvas.pixelRect.width;
            anchorMax.y /= canvas.pixelRect.height;

            safeAreaTransform.anchorMin = anchorMin;
            safeAreaTransform.anchorMax = anchorMax;
        }
    }
}
