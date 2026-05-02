// ============================================================
//  UIManager.cs  –  Animal Fall  (REFACTORED)
//  Changes:
//    • Subscribes to EventBus for score / coins / timer
//    • FloatingText spawner implemented with pooling
//    • Toast / banner system added
//    • Coin display wired to OnCoinsChanged
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────
    [Header("HUD Texts")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private TMP_Text coinsText;

    [Header("HUD Images")]
    [SerializeField] private Image progressBar;

    [Header("Panels")]
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private GameObject levelFailPanel;

    [Header("Floating Text")]
    [SerializeField] private GameObject floatingTextPrefab;  // TMP_Text that floats up
    [SerializeField] private Transform  floatingTextParent;  // Canvas overlay

    [Header("Toast")]
    [SerializeField] private TMP_Text   toastText;
    [SerializeField] private CanvasGroup toastGroup;
    [SerializeField] private float      toastDuration = 1.8f;

    // ── Pool ──────────────────────────────────────────────────
    private Queue<GameObject> _floatPool = new(8);
    private Coroutine         _toastCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────
    private void Start()
    {
        EventBus.Subscribe<OnScoreChanged>   (OnScoreChanged);
        EventBus.Subscribe<OnCoinsChanged>   (OnCoinsChanged);
        EventBus.Subscribe<OnTimerTick>      (OnTimerTick);
        EventBus.Subscribe<OnComboUpdated>   (OnComboUpdated);
        EventBus.Subscribe<OnSaveDataLoaded> (OnSaveLoaded);

        RefreshCoinsDisplay();
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<OnScoreChanged>  (OnScoreChanged);
        EventBus.Unsubscribe<OnCoinsChanged>  (OnCoinsChanged);
        EventBus.Unsubscribe<OnTimerTick>     (OnTimerTick);
        EventBus.Unsubscribe<OnComboUpdated>  (OnComboUpdated);
        EventBus.Unsubscribe<OnSaveDataLoaded>(OnSaveLoaded);
    }

    // ── EventBus handlers ─────────────────────────────────────
    private void OnScoreChanged(OnScoreChanged e)   => UpdateScoreText(e.newScore);
    private void OnCoinsChanged(OnCoinsChanged e)   => SetCoinsText(e.newTotal);
    private void OnTimerTick(OnTimerTick e)          => UpdateTimer(e.remaining);
    private void OnComboUpdated(OnComboUpdated e)   => UpdateComboUI(e.multiplier);
    private void OnSaveLoaded(OnSaveDataLoaded _)   => RefreshCoinsDisplay();

    private void RefreshCoinsDisplay()
    {
        if (SaveManager.Instance != null)
            SetCoinsText(SaveManager.Instance.GetCoins());
    }

    // ── Direct call API (backward compatible) ─────────────────
    public void UpdateTimer(float seconds)
    {
        if (timerText == null) return;
        timerText.text = Mathf.CeilToInt(seconds).ToString("00") + "s";

        // Red pulse under 5 seconds
        timerText.color = seconds <= 5f
            ? Color.Lerp(Color.red, Color.white, Mathf.PingPong(Time.time * 4f, 1f))
            : Color.white;
    }

    public void UpdateTargetText(int current, int target)
    {
        if (targetText) targetText.text = $"{current} / {target}";
    }

    public void UpdateScoreText(int score)
    {
        if (scoreText) scoreText.text = score.ToString("N0");
    }

    private void SetCoinsText(int coins)
    {
        if (coinsText) coinsText.text = coins.ToString("N0");
    }

    public void SetProgress(float t)
    {
        if (progressBar) progressBar.fillAmount = t;
    }

    public void UpdateComboUI(float multiplier)
    {
        if (comboText == null) return;
        comboText.text    = multiplier > 1.05f ? $"x{multiplier:0.0}" : "";
        comboText.enabled = multiplier > 1.05f;
    }

    // ── Panels ────────────────────────────────────────────────
    public void ShowLevelComplete() => levelCompletePanel?.SetActive(true);
    public void ShowLevelFailed()   => levelFailPanel?.SetActive(true);

    public void HideAllPanels()
    {
        levelCompletePanel?.SetActive(false);
        levelFailPanel?.SetActive(false);
    }

    // ── Floating text ─────────────────────────────────────────
    public void ShowFloating(string text, Vector3 screenPos)
    {
        if (floatingTextPrefab == null || floatingTextParent == null) return;

        GameObject go = GetFromPool();
        go.transform.SetParent(floatingTextParent, false);
        go.transform.position = screenPos;

        var tmp = go.GetComponent<TMP_Text>();
        if (tmp) tmp.text = text;

        go.SetActive(true);
        StartCoroutine(FloatAndReturn(go));
    }

    private IEnumerator FloatAndReturn(GameObject go)
    {
        float elapsed  = 0f;
        float duration = 0.9f;
        Vector3 start  = go.transform.position;
        var tmp        = go.GetComponent<TMP_Text>();

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / duration;
            go.transform.position = start + Vector3.up * (80f * t);
            if (tmp) tmp.alpha    = Mathf.Lerp(1f, 0f, t * t);
            yield return null;
        }

        go.SetActive(false);
        _floatPool.Enqueue(go);
    }

    private GameObject GetFromPool()
    {
        if (_floatPool.Count > 0)
        {
            var go = _floatPool.Dequeue();
            if (go != null) return go;
        }
        return Instantiate(floatingTextPrefab);
    }

    // ── Toast / banner ────────────────────────────────────────
    public void ShowMessage(string msg)
    {
        if (_toastCoroutine != null) StopCoroutine(_toastCoroutine);
        _toastCoroutine = StartCoroutine(ToastRoutine(msg));
    }

    private IEnumerator ToastRoutine(string msg)
    {
        if (toastText)  toastText.text = msg;
        if (toastGroup) toastGroup.alpha = 1f;

        yield return new WaitForSeconds(toastDuration);

        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            if (toastGroup) toastGroup.alpha = Mathf.Lerp(1f, 0f, t / 0.3f);
            yield return null;
        }

        if (toastGroup) toastGroup.alpha = 0f;
    }
}
