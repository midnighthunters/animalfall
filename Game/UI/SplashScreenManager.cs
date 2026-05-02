// ============================================================
//  SplashScreenManager.cs  –  Animal Fall
//  Controls the Splash scene flow:
//    1. Waits for Firebase auth + save load
//    2. Animates the logo with DOTween (or plain coroutine fallback)
//    3. Transitions to MainScene
//  Place this on a SplashManager GameObject in a new "SplashScene".
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SplashScreenManager : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────
    [Header("Logo")]
    [SerializeField] private CanvasGroup   logoGroup;          // alpha fade
    [SerializeField] private RectTransform logoRect;           // scale punch
    [SerializeField] private Image         progressBar;        // optional loading bar

    [Header("Text")]
    [SerializeField] private TMP_Text      studioLabel;        // "ZemoLabs" etc.
    [SerializeField] private TMP_Text      loadingLabel;       // "Loading…"

    [Header("Timing (seconds)")]
    [SerializeField] private float fadeInDuration    = 0.8f;
    [SerializeField] private float holdDuration      = 1.2f;
    [SerializeField] private float fadeOutDuration   = 0.6f;
    [SerializeField] private float minimumSplashTime = 2.5f;

    [Header("Scene")]
    [SerializeField] private string nextSceneName = "MainScene";

    // ── State ─────────────────────────────────────────────────
    private float _startTime;
    private bool  _systemsReady = false;
    private float _progress     = 0f;

    // ── Lifecycle ─────────────────────────────────────────────
    private void Awake()
    {
        // Hide everything initially
        if (logoGroup)    logoGroup.alpha    = 0f;
        if (progressBar)  progressBar.fillAmount = 0f;
        if (loadingLabel) loadingLabel.text  = "Loading…";
        if (studioLabel)  studioLabel.alpha  = 0f;

        _startTime = Time.realtimeSinceStartup;
    }

    private void Start()
    {
        EventBus.Subscribe<OnSaveDataLoaded>(OnSaveDataLoaded);
        EventBus.Subscribe<OnFirebaseAuthReady>(OnFirebaseAuthReady);
        StartCoroutine(SplashRoutine());
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<OnSaveDataLoaded>(OnSaveDataLoaded);
        EventBus.Unsubscribe<OnFirebaseAuthReady>(OnFirebaseAuthReady);
    }

    // ── Event handlers ────────────────────────────────────────
    private void OnSaveDataLoaded(OnSaveDataLoaded _)   => MarkProgress(0.5f);
    private void OnFirebaseAuthReady(OnFirebaseAuthReady e) => MarkProgress(1.0f);

    private void MarkProgress(float value)
    {
        _progress = Mathf.Max(_progress, value);
        if (_progress >= 1f) _systemsReady = true;
    }

    // ── Main coroutine ────────────────────────────────────────
    private IEnumerator SplashRoutine()
    {
        // 1. Fade in logo + studio label
        yield return FadeGroup(logoGroup, 0f, 1f, fadeInDuration);
        yield return FadeText(studioLabel, 0f, 1f, 0.4f);

        // 2. Animated logo scale punch
        yield return ScalePunch(logoRect, 0.08f, 0.25f);

        // 3. Wait for systems OR minimum splash time (whichever is longer)
        float elapsed = 0f;
        while ((!_systemsReady || elapsed < minimumSplashTime - fadeInDuration - holdDuration)
               && elapsed < 8f)                 // hard cap 8 s
        {
            elapsed += Time.unscaledDeltaTime;
            if (progressBar) progressBar.fillAmount = Mathf.Lerp(0f, 1f, _progress);
            yield return null;
        }

        // Ensure bar reaches 100%
        if (progressBar) progressBar.fillAmount = 1f;

        // 4. Hold
        yield return new WaitForSecondsRealtime(holdDuration);

        // 5. Fade out
        yield return FadeGroup(logoGroup, 1f, 0f, fadeOutDuration);

        // 6. Load MainScene
        SceneManager.LoadScene(nextSceneName);
    }

    // ── Animation helpers ─────────────────────────────────────
    private IEnumerator FadeGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) { yield break; }
        cg.alpha = from;
        float t = 0f;
        while (t < duration)
        {
            t        += Time.unscaledDeltaTime;
            cg.alpha  = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        cg.alpha = to;
    }

    private IEnumerator FadeText(TMP_Text label, float from, float to, float duration)
    {
        if (label == null) yield break;
        float t = 0f;
        while (t < duration)
        {
            t         += Time.unscaledDeltaTime;
            label.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        label.alpha = to;
    }

    private IEnumerator ScalePunch(RectTransform rt, float magnitude, float duration)
    {
        if (rt == null) yield break;
        Vector3 origin = rt.localScale;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float punch  = Mathf.Sin((t / duration) * Mathf.PI) * magnitude;
            rt.localScale = origin * (1f + punch);
            yield return null;
        }
        rt.localScale = origin;
    }
}
