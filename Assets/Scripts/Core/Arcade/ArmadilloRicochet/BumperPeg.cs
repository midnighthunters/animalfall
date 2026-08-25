using UnityEngine;

namespace AnimalFall.Core.Arcade.ArmadilloRicochet
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class BumperPeg : MonoBehaviour
    {
        [SerializeField] private float bounciness = 1.2f;
        [SerializeField] private float bumperForce = 5f;

        private SpriteRenderer sr;
        private Color originalColor;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr != null) originalColor = sr.color;

            var collider = GetComponent<CircleCollider2D>();
            var mat = new PhysicsMaterial2D("BumperMat")
            {
                bounciness = bounciness,
                friction = 0.1f
            };
            collider.sharedMaterial = mat;

            var rb = GetComponent<Rigidbody2D>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
        }

        public void Configure(float radius, float bounce)
        {
            bounciness = bounce;
            var collider = GetComponent<CircleCollider2D>();
            collider.radius = radius;

            var mat = new PhysicsMaterial2D("BumperMat")
            {
                bounciness = bounciness,
                friction = 0.1f
            };
            collider.sharedMaterial = mat;
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            var rb = col.rigidbody;
            if (rb != null)
            {
                Vector2 dir = ((Vector2)col.transform.position - (Vector2)transform.position).normalized;
                rb.AddForce(dir * bumperForce, ForceMode2D.Impulse);
            }

            if (sr != null)
            {
                sr.color = Color.yellow;
                Invoke(nameof(ResetColor), 0.15f);
            }
        }

        private void ResetColor()
        {
            if (sr != null) sr.color = originalColor;
        }
    }
}
