using UnityEngine;

namespace AnimalFall.Core.Hindrances
{
    public static class HindranceFactory
    {
        public static IHindrance Create(HindranceData data, Vector3 position, Transform parent = null)
        {
            if (data == null || data.prefab == null)
            {
                Debug.LogWarning($"[HindranceFactory] Missing prefab for {data?.type}");
                return null;
            }

            GameObject obj = Object.Instantiate(data.prefab, position, Quaternion.identity, parent);
            IHindrance hindrance = obj.GetComponent<IHindrance>();

            if (hindrance == null)
            {
                Debug.LogError($"[HindranceFactory] Prefab for {data.type} missing IHindrance component.");
                Object.Destroy(obj);
                return null;
            }

            return hindrance;
        }

        public static IHindrance CreateAtRandomScreenTop(HindranceData data, Transform parent = null)
        {
            Camera cam = Camera.main;
            if (cam == null)
                return Create(data, Vector3.zero, parent);

            float x = Random.Range(0.1f, 0.9f);
            Vector3 worldPos = cam.ViewportToWorldPoint(new Vector3(x, 1.05f, 10f));
            worldPos.z = 0f;
            return Create(data, worldPos, parent);
        }
    }
}
