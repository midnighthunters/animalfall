using System;
using UnityEngine;

namespace AnimalFall.Core.Arcade.ArmadilloRicochet
{
    [RequireComponent(typeof(CircleCollider2D), typeof(SpriteRenderer))]
    public class GoldenScarab : MonoBehaviour
    {
        public bool IsCollected { get; private set; }
        public event Action<GoldenScarab> OnCollected;

        private SpriteRenderer sr;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            sr.color = new Color(1f, 0.84f, 0f);

            var col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;

            var rb = GetComponent<Rigidbody2D>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsCollected) return;

            var ball = other.GetComponent<ArmadilloBall>();
            if (ball != null)
            {
                Collect();
            }
        }

        private void Collect()
        {
            IsCollected = true;
            sr.color = Color.green;
            OnCollected?.Invoke(this);
        }
    }
}
