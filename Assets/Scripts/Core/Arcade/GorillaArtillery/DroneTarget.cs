using System;
using UnityEngine;

namespace AnimalFall.Core.Arcade.GorillaArtillery
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer))]
    public class DroneTarget : MonoBehaviour
    {
        [Header("Drone Settings")]
        [SerializeField] private float patrolSpeed = 1.5f;
        [SerializeField] private float patrolRange = 2f;
        [SerializeField] private float maxHP = 40f;

        public float CurrentHP { get; private set; }
        public bool IsDestroyed { get; private set; }
        public event Action<DroneTarget> OnDroneDestroyed;

        private Rigidbody2D rb;
        private SpriteRenderer sr;
        private Vector2 startPos;
        private float patrolDir = 1f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            sr = GetComponent<SpriteRenderer>();

            rb.gravityScale = 0f;
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            CurrentHP = maxHP;
            startPos = transform.position;
        }

        private void Update()
        {
            if (IsDestroyed) return;

            if (rb.isKinematic)
            {
                float x = transform.position.x + patrolDir * patrolSpeed * Time.deltaTime;
                if (Mathf.Abs(x - startPos.x) > patrolRange)
                    patrolDir *= -1f;
                transform.position = new Vector3(x, transform.position.y, 0);
            }
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            if (IsDestroyed) return;

            float damage = col.relativeVelocity.magnitude * (col.rigidbody != null ? col.rigidbody.mass : 1f);
            TakeDamage(damage);
        }

        public void TakeDamage(float damage)
        {
            if (IsDestroyed) return;

            CurrentHP -= damage;
            sr.color = Color.Lerp(Color.red, Color.white, Mathf.Clamp01(CurrentHP / maxHP));

            if (CurrentHP <= 0f)
                DestroyDrone();
        }

        public void MakePhysical()
        {
            rb.isKinematic = false;
            rb.gravityScale = 1f;
        }

        private void DestroyDrone()
        {
            IsDestroyed = true;
            OnDroneDestroyed?.Invoke(this);
            Destroy(gameObject, 0.5f);
        }
    }
}
