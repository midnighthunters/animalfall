using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnimalFall.Utils
{
    /// <summary>
    /// Keeps the presentation at the iPhone portrait aspect ratio on wider devices.
    /// Cameras and camera-space canvases render into the centred viewport while an
    /// overlay covers the unused display area with black bars.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class FixedPortraitAspect : MonoBehaviour
    {
        private const float TargetWidth = 1284f;
        private const float TargetHeight = 2778f;
        private const float RefreshInterval = 0.25f;

        private static FixedPortraitAspect _instance;

        private Canvas _barsCanvas;
        private RectTransform _leftBar;
        private RectTransform _rightBar;
        private RectTransform _topBar;
        private RectTransform _bottomBar;
        private int _lastScreenWidth = -1;
        private int _lastScreenHeight = -1;
        private int _lastCameraCount = -1;
        private int _lastCanvasCount = -1;
        private float _nextRefresh;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (_instance != null) return;

            GameObject root = new GameObject("[Display] Fixed iPhone Aspect");
            DontDestroyOnLoad(root);
            _instance = root.AddComponent<FixedPortraitAspect>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            CreateBars();
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            ApplyLayout();
        }

        private void OnDestroy()
        {
            if (_instance != this) return;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            _instance = null;
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + RefreshInterval;

            int cameraCount = Camera.allCamerasCount;
            int canvasCount = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight ||
                cameraCount != _lastCameraCount || canvasCount != _lastCanvasCount)
            {
                ApplyLayout();
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyLayout();
        }

        private void ApplyLayout()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;

            Rect viewport = CalculateViewport(Screen.width, Screen.height);
            Camera primaryCamera = Camera.main;
            Camera[] cameras = Camera.allCameras;
            if (primaryCamera == null && cameras.Length > 0) primaryCamera = cameras[0];

            foreach (Camera camera in cameras)
            {
                if (camera == null || camera.cameraType != CameraType.Game || camera.targetTexture != null) continue;
                camera.rect = viewport;
            }

            if (primaryCamera != null)
            {
                Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (Canvas canvas in canvases)
                {
                    if (canvas == null || canvas == _barsCanvas || !canvas.isRootCanvas) continue;
                    if (canvas.renderMode != RenderMode.ScreenSpaceOverlay &&
                        canvas.renderMode != RenderMode.ScreenSpaceCamera) continue;

                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = primaryCamera;
                    canvas.planeDistance = Mathf.Max(primaryCamera.nearClipPlane + 0.1f, 10f);
                }

                _lastCanvasCount = canvases.Length;
            }

            LayoutBars(viewport);
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            _lastCameraCount = Camera.allCamerasCount;
        }

        public static Rect CalculateViewport(int screenWidth, int screenHeight)
        {
            if (screenWidth <= 0 || screenHeight <= 0) return new Rect(0f, 0f, 1f, 1f);

            float targetAspect = TargetWidth / TargetHeight;
            float screenAspect = (float)screenWidth / screenHeight;
            if (screenAspect > targetAspect)
            {
                float width = targetAspect / screenAspect;
                return new Rect((1f - width) * 0.5f, 0f, width, 1f);
            }

            float height = screenAspect / targetAspect;
            return new Rect(0f, (1f - height) * 0.5f, 1f, height);
        }

        private void CreateBars()
        {
            GameObject canvasObject = new GameObject("Letterbox Bars", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            _barsCanvas = canvasObject.GetComponent<Canvas>();
            _barsCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _barsCanvas.sortingOrder = short.MaxValue;

            _leftBar = CreateBar("Left");
            _rightBar = CreateBar("Right");
            _topBar = CreateBar("Top");
            _bottomBar = CreateBar("Bottom");
        }

        private RectTransform CreateBar(string barName)
        {
            GameObject bar = new GameObject(barName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bar.transform.SetParent(_barsCanvas.transform, false);
            Image image = bar.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = true;
            return bar.GetComponent<RectTransform>();
        }

        private void LayoutBars(Rect viewport)
        {
            SetBar(_leftBar, new Vector2(0f, 0f), new Vector2(viewport.xMin, 1f));
            SetBar(_rightBar, new Vector2(viewport.xMax, 0f), new Vector2(1f, 1f));
            SetBar(_bottomBar, new Vector2(viewport.xMin, 0f), new Vector2(viewport.xMax, viewport.yMin));
            SetBar(_topBar, new Vector2(viewport.xMin, viewport.yMax), new Vector2(viewport.xMax, 1f));
        }

        private static void SetBar(RectTransform bar, Vector2 anchorMin, Vector2 anchorMax)
        {
            bar.anchorMin = anchorMin;
            bar.anchorMax = anchorMax;
            bar.offsetMin = Vector2.zero;
            bar.offsetMax = Vector2.zero;
            bar.gameObject.SetActive(anchorMax.x - anchorMin.x > 0.0001f && anchorMax.y - anchorMin.y > 0.0001f);
        }
    }
}
