using UnityEngine;

namespace AnimalFall.Core.Arcade.ArmadilloRicochet
{
    [RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
    public class BreakableRock : MonoBehaviour
    {
        [SerializeField] private float maxHP = 30f;
        [SerializeField] private GameObject shatterFXPrefab;

        private float currentHP;
        private SpriteRenderer sr;
        private bool shattered;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            sr.color = new Color(0.55f, 0.45f, 0.35f);
            currentHP = maxHP;

            var rb = GetComponent<Rigidbody2D>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
        }

        public void Configure(float hp)
        {
            maxHP = hp;
            currentHP = hp;
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            if (shattered) return;

            float damage = col.relativeVelocity.magnitude * (col.rigidbody != null ? col.rigidbody.mass : 1f);
            currentHP -= damage;

            float ratio = Mathf.Clamp01(currentHP / maxHP);
            sr.color = Color.Lerp(new Color(0.3f, 0.2f, 0.15f), new Color(0.55f, 0.45f, 0.35f), ratio);

            if (currentHP <= 0f)
                Shatter();
        }

        private void Shatter()
        {
            shattered = true;
            if (shatterFXPrefab != null)
            {
                var fx = Instantiate(shatterFXPrefab, transform.position, Quaternion.identity);
                Destroy(fx, 2f);
            }
            Destroy(gameObject);
        }
    }
}
