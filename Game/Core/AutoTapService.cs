using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoTapService : MonoBehaviour
{
    public static AutoTapService Instance;
    private Coroutine autoRoutine;

    private void Awake()
    {
        if (Instance == null) Instance = this; else Destroy(gameObject);
    }

    public void StartAutoTap(float tapsPerSecond)
    {
        if (autoRoutine != null) StopCoroutine(autoRoutine);
        autoRoutine = StartCoroutine(AutoTap(tapsPerSecond));
    }

    public void StopAutoTap()
    {
        if (autoRoutine != null) StopCoroutine(autoRoutine);
        autoRoutine = null;
    }

    IEnumerator AutoTap(float tps)
    {
        while (true)
        {
            float interval = 1f / tps;
            // find nearest target and call HandleTap()
            Animal nearest = FindNearestTarget();
            if (nearest != null) nearest.HandleTap();
            yield return new WaitForSeconds(interval);
        }
    }

    Animal FindNearestTarget()
    {
        Animal[] animals = FindObjectsOfType<Animal>();
        Animal best = null;
        float bestDist = float.MaxValue;
        foreach (var a in animals)
        {
            if (a == null || a.data == null) continue;
            if (!a.data.isTargetSpecies) continue;
            float d = Vector2.Distance(a.transform.position, Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, 0)));
            if (d < bestDist) { bestDist = d; best = a; }
        }
        return best;
    }
}
