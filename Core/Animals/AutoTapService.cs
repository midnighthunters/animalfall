using System.Collections;
using UnityEngine;

namespace AnimalFall.Core.Animals
{
    public class AutoTapService : MonoBehaviour
    {
        public static AutoTapService Instance { get; private set; }

        private Coroutine autoRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void StartAutoTap(float tapsPerSecond)
        {
            StopAutoTap();
            autoRoutine = StartCoroutine(AutoTapRoutine(tapsPerSecond));
        }

        public void StopAutoTap()
        {
            if (autoRoutine != null)
            {
                StopCoroutine(autoRoutine);
                autoRoutine = null;
            }
        }

        private IEnumerator AutoTapRoutine(float tps)
        {
            while (true)
            {
                float interval = 1f / tps;
                Animal nearest = FindNearestTarget();
                if (nearest != null) nearest.HandleTap();
                yield return new WaitForSeconds(interval);
            }
        }

        private Animal FindNearestTarget()
        {
            Animal[] animals = FindObjectsOfType<Animal>();
            Animal best = null;
            float bestDist = float.MaxValue;
            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);

            foreach (var a in animals)
            {
                if (a == null || a.data == null || !a.data.isTargetSpecies) continue;

                float d = Vector2.Distance(
                    a.transform.position,
                    Camera.main.ScreenToWorldPoint(screenCenter)
                );

                if (d < bestDist)
                {
                    bestDist = d;
                    best = a;
                }
            }

            return best;
        }
    }
}
