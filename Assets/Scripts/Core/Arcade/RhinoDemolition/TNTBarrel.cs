using UnityEngine;
using AnimalFall.Core.Arcade.Shared;

namespace AnimalFall.Core.Arcade.RhinoDemolition
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer))]
    public class TNTBarrel : MonoBehaviour
    {
        [Header("TNT Settings")]
        [SerializeField] private float explosionRadius = 3f;
        [SerializeField] private float explosionForce = 25f;
        [SerializeField] private float explosionDamage = 1000f;
        [SerializeField] private float triggerVelocity = 2f;
        [SerializeField] private GameObject explosionFXPrefab;

        private bool exploded;
        private SpriteRenderer sr;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            sr.color = new Color(0.9f, 0.2f, 0.15f);
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            if (exploded) return;

            if (col.relativeVelocity.magnitude >= triggerVelocity)
                Explode();
        }

        public void Explode()
        {
            if (exploded) return;
            exploded = true;

            ShockwaveEffect.CreateAt(transform.position, explosionRadius, explosionForce, explosionDamage);

            if (explosionFXPrefab != null)
            {
                var fx = Instantiate(explosionFXPrefab, transform.position, Quaternion.identity);
                Destroy(fx, 2f);
            }

            Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, explosionRadius * 0.5f);
            foreach (var col in nearby)
            {
                if (col.gameObject == gameObject) continue;
                var otherTNT = col.GetComponent<TNTBarrel>();
                if (otherTNT != null && !otherTNT.exploded)
                    otherTNT.Explode();
            }

            Destroy(gameObject);
        }
    }
}
