using System;
using UnityEngine;

namespace AnimalFall.Core.Arcade.Shared
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(SpriteRenderer))]
    public class DestructibleBlock : MonoBehaviour
    {
        [Header("Block Properties")]
        public BlockMaterial material = BlockMaterial.Wood;
        public float maxHP;
        public float currentHP;

        [Header("FX Prefabs")]
        [SerializeField] private GameObject shatterFXPrefab;

        public event Action<DestructibleBlock> OnShattered;

        private SpriteRenderer sr;
        private bool shattered;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            if (maxHP <= 0) maxHP = DamageCalculator.GetMaterialHP(material);
            currentHP = maxHP;

            ApplyMaterialColor();
        }

        private void ApplyMaterialColor()
        {
            switch (material)
            {
                case BlockMaterial.Glass: sr.color = new Color(0.7f, 0.85f, 1f, 0.7f); break;
                case BlockMaterial.Wood:  sr.color = new Color(0.72f, 0.53f, 0.35f); break;
                case BlockMaterial.Stone: sr.color = new Color(0.6f, 0.6f, 0.6f); break;
                case BlockMaterial.Metal: sr.color = new Color(0.75f, 0.75f, 0.8f); break;
                case BlockMaterial.TNT:   sr.color = new Color(0.9f, 0.2f, 0.15f); break;
            }
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            if (shattered) return;

            float impactorMass = col.rigidbody != null ? col.rigidbody.mass : 1f;
            float damage = DamageCalculator.Calculate(
                col.relativeVelocity.magnitude,
                impactorMass,
                material
            );

            currentHP -= damage;

            float hpRatio = Mathf.Clamp01(currentHP / maxHP);
            sr.color = Color.Lerp(Color.red, sr.color, hpRatio);

            if (currentHP <= 0f)
                Shatter();
        }

        public void TakeDamage(float damage)
        {
            if (shattered) return;

            currentHP -= damage;
            if (currentHP <= 0f)
                Shatter();
        }

        private void Shatter()
        {
            shattered = true;

            if (material == BlockMaterial.TNT)
            {
                ShockwaveEffect.CreateAt(transform.position, 3f, 20f, 800f);
            }

            if (shatterFXPrefab != null)
            {
                var fx = Instantiate(shatterFXPrefab, transform.position, Quaternion.identity);
                Destroy(fx, 2f);
            }

            OnShattered?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
