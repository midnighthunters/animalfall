using UnityEngine;
using AnimalFall.Core.Arcade.Shared;

namespace AnimalFall.Core.Arcade.GorillaArtillery
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer))]
    public class DroneBarrier : MonoBehaviour
    {
        [Header("Barrier")]
        [SerializeField] private BlockMaterial material = BlockMaterial.Glass;
        [SerializeField] private float maxHP;
        [SerializeField] private GameObject shatterFXPrefab;

        private float currentHP;
        private SpriteRenderer sr;
        private bool shattered;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();

            var rb = GetComponent<Rigidbody2D>();
            rb.isKinematic = true;

            if (maxHP <= 0) maxHP = DamageCalculator.GetMaterialHP(material);
            currentHP = maxHP;

            ApplyVisual();
        }

        private void ApplyVisual()
        {
            switch (material)
            {
                case BlockMaterial.Glass:
                    sr.color = new Color(0.7f, 0.85f, 1f, 0.6f);
                    break;
                case BlockMaterial.Metal:
                    sr.color = new Color(0.7f, 0.7f, 0.75f, 1f);
                    break;
                default:
                    sr.color = Color.gray;
                    break;
            }
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            if (shattered) return;

            float impactorMass = col.rigidbody != null ? col.rigidbody.mass : 1f;
            float damage = DamageCalculator.Calculate(col.relativeVelocity.magnitude, impactorMass, material);
            currentHP -= damage;

            float ratio = Mathf.Clamp01(currentHP / maxHP);
            sr.color = Color.Lerp(new Color(1f, 0.3f, 0.3f, 0.5f), sr.color, ratio);

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
