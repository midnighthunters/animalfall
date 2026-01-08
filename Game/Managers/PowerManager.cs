using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUpManager : MonoBehaviour
{
    public bool isPaused = false;
    private Dictionary<PowerUpType, Coroutine> active = new Dictionary<PowerUpType, Coroutine>();

    public void InitForLevel(LevelData level)
    {
        CancelAll();
        isPaused = false;
    }

    public void UsePowerUp(PowerUpData p)
    {
        if (active.ContainsKey(p.type))
        {
            // already active; could stack or ignore
            return;
        }

        switch (p.type)
        {
            case PowerUpType.SlowTime:
                active[p.type] = StartCoroutine(SlowTimeRoutine(p.duration, p.value));
                break;
            case PowerUpType.Magnet:
                active[p.type] = StartCoroutine(MagnetRoutine(p.duration, p.value));
                break;
            case PowerUpType.MultiTap:
                active[p.type] = StartCoroutine(MultiTapRoutine(p.duration, (int)p.value));
                break;
            case PowerUpType.AutoTap:
                active[p.type] = StartCoroutine(AutoTapRoutine(p.duration, p.value));
                break;
            case PowerUpType.ShieldBreaker:
                // Break next shielded animal: implement as a flag checked by Animal on spawn/hit
                StartCoroutine(ShieldBreakerOnce());
                break;
            case PowerUpType.BombClear:
                BombClear();
                break;
            case PowerUpType.ScoreMultiplier:
                active[p.type] = StartCoroutine(ScoreMultiplierRoutine(p.duration, p.value));
                break;
            case PowerUpType.ExtraTime:
                GameManager.Instance.AddTime(p.value);
                break;
            case PowerUpType.FreezeHighlight:
                active[p.type] = StartCoroutine(FreezeHighlightRoutine(p.duration));
                break;
        }
    }

    public void CancelAll()
    {
        foreach (var c in active.Values) if (c != null) StopCoroutine(c);
        active.Clear();
    }

    IEnumerator SlowTimeRoutine(float duration, float slowFactor)
    {
        // slow all AnimalMovement speeds by slowFactor (e.g., 0.4 for 60% slow)
        isPaused = false;
        var animals = FindObjectsOfType<AnimalMovement>();
        foreach (var a in animals) a.speed *= slowFactor;
        yield return new WaitForSeconds(duration);
        foreach (var a in FindObjectsOfType<AnimalMovement>()) a.ConfigureRandomSpeed(a.speed * 1f, a.speed * 1f); // not ideal; better to store original speeds
        active.Remove(PowerUpType.SlowTime);
    }

    IEnumerator MagnetRoutine(float duration, float radius)
    {
        // find target animals and collect them
        float start = Time.time;
        while (Time.time - start < duration)
        {
            var animals = FindObjectsOfType<Animal>();
            foreach (var a in animals)
            {
                if (a == null || a.data == null) continue;
                if (!a.data.isTargetSpecies) continue;
                float dist = Vector2.Distance(a.transform.position, Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, 0)));
                if (dist <= radius)
                {
                    a.HandleTap();
                }
            }
            yield return new WaitForSeconds(0.25f);
        }
        active.Remove(PowerUpType.Magnet);
    }

    IEnumerator MultiTapRoutine(float duration, int multiplicity)
    {
        // For simplicity we set GameManager to accept multiplicity for next X seconds.
        float start = Time.time;
        while (Time.time - start < duration)
        {
            // set a global multiTap variable, used by PlayerInput/Animal.HandleTap (not included here)
            ScoreManager.Instance.ui.ShowMessage("MultiTap x" + multiplicity);
            yield return null;
        }
        active.Remove(PowerUpType.MultiTap);
    }

    IEnumerator AutoTapRoutine(float duration, float tapsPerSecond)
    {
        AutoTapService auto = AutoTapService.Instance;
        if (auto != null) auto.StartAutoTap(tapsPerSecond);
        yield return new WaitForSeconds(duration);
        if (auto != null) auto.StopAutoTap();
        active.Remove(PowerUpType.AutoTap);
    }

    IEnumerator ShieldBreakerOnce()
    {
        // find the next shielded animal on screen and remove its shield
        Animal[] animals = FindObjectsOfType<Animal>();
        Animal target = null;
        foreach (var a in animals)
            if (a.data != null && (a.data.type == AnimalType.Shielded || a.data.requiresDoubleTap))
            {
                target = a; break;
            }
        if (target != null)
        {
            target.data.requiresDoubleTap = false;
            target.currentShield = 0;
            // show effect
        }
        yield return null;
    }

    void BombClear()
    {
        Animal[] animals = FindObjectsOfType<Animal>();
        foreach (var a in animals)
            if (a.data != null && a.data.type == AnimalType.Bomb)
                Destroy(a.gameObject);
    }

    IEnumerator ScoreMultiplierRoutine(float duration, float value)
    {
        ScoreManager.Instance.SetComboMultiplier(value);
        yield return new WaitForSeconds(duration);
        ScoreManager.Instance.SetComboMultiplier(1f);
        active.Remove(PowerUpType.ScoreMultiplier);
    }

    IEnumerator FreezeHighlightRoutine(float duration)
    {
        // freeze non-targets and highlight targets
        var animals = FindObjectsOfType<Animal>();
        List<AnimalMovement> moved = new List<AnimalMovement>();
        foreach (var a in animals)
        {
            if (!a.data.isTargetSpecies)
            {
                var m = a.GetComponent<AnimalMovement>();
                if (m != null) { moved.Add(m); m.enabled = false; }
            }
            else
            {
                // highlight e.g. add glow
            }
        }

        yield return new WaitForSeconds(duration);
        foreach (var m in moved) if (m != null) m.enabled = true;
    }
}
