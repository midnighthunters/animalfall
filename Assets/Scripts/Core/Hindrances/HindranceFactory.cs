// Task 4.2 — HindranceFactory: ObjectPooler-based spawning
using UnityEngine;
using AnimalFall.Core;

namespace AnimalFall.Core.Hindrances
{
    public static class HindranceFactory
    {
        /// <summary>Spawns a hindrance at a random position along the top of the screen.</summary>
        public static IHindrance CreateAtRandomScreenTop(HindranceData data, Transform parent)
        {
            if (data == null)
            {
                Debug.LogWarning("[HindranceFactory] HindranceData is null.");
                return null;
            }

            if (data.prefab == null)
            {
                Debug.LogWarning($"[HindranceFactory] Prefab is null for {data.hindranceType}.");
                return null;
            }

            // Random X across the top viewport
            float randomX = Random.Range(0.1f, 0.9f);
            Vector3 worldPos = Vector3.zero;
            if (Camera.main != null)
                worldPos = Camera.main.ViewportToWorldPoint(new Vector3(randomX, 1.05f, Mathf.Abs(Camera.main.transform.position.z)));

            GameObject obj = ObjectPooler.Instance.SpawnFromPool(data.prefab, worldPos, Quaternion.identity, parent);
            if (obj == null) return null;

            var hindrance = obj.GetComponent<IHindrance>();
            if (hindrance == null)
            {
                Debug.LogError($"[HindranceFactory] Spawned object '{obj.name}' has no IHindrance component.");
                ObjectPooler.Instance.ReturnToPool(obj);
                return null;
            }

            return hindrance;
        }
    }
}
