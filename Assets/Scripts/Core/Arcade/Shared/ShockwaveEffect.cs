using UnityEngine;

namespace AnimalFall.Core.Arcade.Shared
{
    public class ShockwaveEffect : MonoBehaviour
    {
        [SerializeField] private float radius = 3f;
        [SerializeField] private float force = 20f;
        [SerializeField] private float blockDamage = 800f;
        [SerializeField] private float duration = 0.3f;

        private float elapsed;
        private bool applied;

        public static void CreateAt(Vector3 position, float radius, float force, float blockDamage)
        {
            var go = new GameObject("Shockwave");
            go.transform.position = position;
            var sw = go.AddComponent<ShockwaveEffect>();
            sw.radius = radius;
            sw.force = force;
            sw.blockDamage = blockDamage;
        }

        private void Update()
        {
            if (!applied)
            {
                Apply();
                applied = true;
            }

            elapsed += Time.deltaTime;
            if (elapsed >= duration)
                Destroy(gameObject);
        }

        private void Apply()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                var rb = hit.attachedRigidbody;
                if (rb != null)
                {
                    Vector2 dir = ((Vector2)hit.transform.position - (Vector2)transform.position);
                    float dist = dir.magnitude;
                    if (dist < 0.1f) dist = 0.1f;

                    float forceFactor = 1f - Mathf.Clamp01(dist / radius);
                    rb.AddForce(dir.normalized * force * forceFactor, ForceMode2D.Impulse);
                }

                var block = hit.GetComponent<DestructibleBlock>();
                if (block != null)
                {
                    float dist = Vector2.Distance(transform.position, hit.transform.position);
                    float damageFactor = 1f - Mathf.Clamp01(dist / radius);
                    block.TakeDamage(blockDamage * damageFactor);
                }
            }
        }
    }
}
