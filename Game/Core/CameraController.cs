// ============================================================
//  CameraController.cs  –  Animal Fall
//  • On scene load, smoothly pans to frame the player / spawn
//    area before the countdown begins
//  • Handles camera shake via EventBus
//  • Works with both 2D orthographic and perspective cameras
// ============================================================

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────
    [Header("Focus target")]
    [SerializeField] private Transform playerFocusTarget;  // drag the player / spawn-area center here
    [SerializeField] private float     focusDuration  = 0.6f;
    [SerializeField] private AnimationCurve focusCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Shake")]
    [SerializeField] private float defaultShakeDuration  = 0.25f;
    [SerializeField] private float defaultShakeMagnitude = 0.12f;

    // ── Private ───────────────────────────────────────────────
    private Camera    _cam;
    private Vector3   _originalPos;
    private Coroutine _shakeCoroutine;
    private Coroutine _focusCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance   = this;
        _cam       = GetComponent<Camera>();
        _originalPos = transform.localPosition;
    }

    private void Start()
    {
        if (playerFocusTarget != null)
            _focusCoroutine = StartCoroutine(FocusOnPlayer());
    }

    // ── Camera focus on player ────────────────────────────────
    private IEnumerator FocusOnPlayer()
    {
        Vector3 startPos = transform.position;
        Vector3 endPos   = new Vector3(
            playerFocusTarget.position.x,
            playerFocusTarget.position.y,
            transform.position.z);           // keep Z for 2D

        float elapsed = 0f;
        while (elapsed < focusDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t  = focusCurve.Evaluate(Mathf.Clamp01(elapsed / focusDuration));
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        transform.position = endPos;
        _originalPos       = transform.localPosition;
    }

    // ── Shake ─────────────────────────────────────────────────
    public void Shake(float duration = -1f, float magnitude = -1f)
    {
        if (duration  < 0) duration  = defaultShakeDuration;
        if (magnitude < 0) magnitude = defaultShakeMagnitude;

        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float damping = 1f - (elapsed / duration);          // decreasing shake
            float x = Random.Range(-1f, 1f) * magnitude * damping;
            float y = Random.Range(-1f, 1f) * magnitude * damping;
            transform.localPosition = _originalPos + new Vector3(x, y, 0f);
            yield return null;
        }
        transform.localPosition = _originalPos;
    }

    // ── Public API ────────────────────────────────────────────
    public void SetFocusTarget(Transform t)
    {
        playerFocusTarget = t;
        if (_focusCoroutine != null) StopCoroutine(_focusCoroutine);
        _focusCoroutine = StartCoroutine(FocusOnPlayer());
    }
}
