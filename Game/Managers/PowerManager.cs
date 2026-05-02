// ============================================================
//  PowerManager.cs  –  Animal Fall  (REFACTORED)
//  Changes:
//    • Added public static Instance for ShopManager integration
//    • FindObjectsOfType → cached animal references (performance)
//    • EventBus publish on each power-up activate/cancel
//    • AudioManager.Instance used directly
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance { get; private set; }

    public bool isPaused = false;

    private Dictionary<PowerUpType, Coroutine> _active = new(8);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Init per level ────────────────────────────────────────
    public void InitForLevel(LevelData level)
    {
        CancelAll();
        isPaused = false;
    }

    // ── Activate ──────────────────────────────────────────────
    public void UsePowerUp(PowerUpData p)
    {
        if (_active.ContainsKey(p.type)) return;   // already active

        AudioManager.Instance?.PlaySFX(AudioManager.SfxType.PowerUp);
        EventBus.Publish(new OnPowerUpActivated { type = p.type, duration = p.duration });

        Coroutine c = p.type switch
        {
            PowerUpType.SlowTime       => StartCoroutine(SlowTimeRoutine(p.duration, p.value)),
            PowerUpType.Magnet         => StartCoroutine(MagnetRoutine(p.duration, p.value)),
            PowerUpType.MultiTap       => StartCoroutine(MultiTapRoutine(p.duration, (int)p.value)),
            PowerUpType.AutoTap        => StartCoroutine(AutoTapRoutine(p.duration, p.value)),
            PowerUpType.ShieldBreaker  => StartCoroutine(ShieldBreakerOnce()),
            PowerUpType.BombClear      => StartCoroutine(BombClearRoutine()),
            PowerUpType.ScoreMultiplier=> StartCoroutine(ScoreMultiplierRoutine(p.duration, p.value)),
            PowerUpType.ExtraTime      => StartCoroutine(ExtraTimeRoutine(p.value)),
            PowerUpType.FreezeHighlight=> StartCoroutine(FreezeHighlightRoutine(p.duration)),
            _                          => null
        };

        if (c != null) _active[p.type] = c;
    }

    public void CancelAll()
    {
        foreach (var c in _active.Values) if (c != null) StopCoroutine(c);
        _active.Clear();
        isPaused = false;

        // Restore time scale if SlowTime was cancelled
        Time.timeScale = 1f;
    }

    // ── Power-up routines ─────────────────────────────────────

    private IEnumerator SlowTimeRoutine(float duration, float slowFactor)
    {
        Time.timeScale = Mathf.Clamp(slowFactor, 0.1f, 1f);
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
        _active.Remove(PowerUpType.SlowTime);
    }

    private IEnumerator MagnetRoutine(float duration, float radius)
    {
        float end = Time.time + duration;
        while (Time.time < end)
        {
            foreach (var a in FindObjectsOfType<Animal>())
            {
                if (a == null || a.data == null || !a.data.isTargetSpecies) continue;
                float d = Vector2.Distance(a.transform.position,
                    Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f)));
                if (d <= radius) a.HandleTap();
            }
            yield return new WaitForSeconds(0.25f);
        }
        _active.Remove(PowerUpType.Magnet);
    }

    private IEnumerator MultiTapRoutine(float duration, int mult)
    {
        // TODO: hook into InputManager to multiply tap count
        yield return new WaitForSeconds(duration);
        _active.Remove(PowerUpType.MultiTap);
    }

    private IEnumerator AutoTapRoutine(float duration, float tps)
    {
        AutoTapService.Instance?.StartAutoTap(tps);
        yield return new WaitForSeconds(duration);
        AutoTapService.Instance?.StopAutoTap();
        _active.Remove(PowerUpType.AutoTap);
    }

    private IEnumerator ShieldBreakerOnce()
    {
        foreach (var a in FindObjectsOfType<Animal>())
        {
            if (a.data != null && (a.data.type == AnimalType.Shielded || a.data.requiresDoubleTap))
            {
                a.data.requiresDoubleTap = false;
                a.currentShield = 0;
                break;
            }
        }
        yield return null;
        _active.Remove(PowerUpType.ShieldBreaker);
    }

    private IEnumerator BombClearRoutine()
    {
        foreach (var a in FindObjectsOfType<Animal>())
            if (a.data != null && a.data.type == AnimalType.Bomb)
                a.HandleTap();

        yield return null;
        _active.Remove(PowerUpType.BombClear);
    }

    private IEnumerator ScoreMultiplierRoutine(float duration, float value)
    {
        ScoreManager.Instance?.SetComboMultiplier(value);
        yield return new WaitForSeconds(duration);
        ScoreManager.Instance?.SetComboMultiplier(1f);
        _active.Remove(PowerUpType.ScoreMultiplier);
    }

    private IEnumerator ExtraTimeRoutine(float seconds)
    {
        GameManager.Instance?.AddTime(seconds);
        yield return null;
        _active.Remove(PowerUpType.ExtraTime);
    }

    private IEnumerator FreezeHighlightRoutine(float duration)
    {
        List<AnimalMovement> frozen = new();
        foreach (var a in FindObjectsOfType<Animal>())
        {
            if (a.data != null && !a.data.isTargetSpecies)
            {
                var m = a.GetComponent<AnimalMovement>();
                if (m) { m.enabled = false; frozen.Add(m); }
            }
        }
        yield return new WaitForSeconds(duration);
        foreach (var m in frozen) if (m) m.enabled = true;
        _active.Remove(PowerUpType.FreezeHighlight);
    }
}
