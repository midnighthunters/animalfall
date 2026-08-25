using UnityEngine;
using AnimalFall.Core.Arcade.Shared;

namespace AnimalFall.Core.Arcade.RhinoDemolition
{
    public class GroundPound : MonoBehaviour
    {
        [Header("Ground Pound Settings")]
        [SerializeField] private float downwardForce = 50f;
        [SerializeField] private float shockwaveRadius = 4f;
        [SerializeField] private float shockwaveForce = 30f;
        [SerializeField] private float shockwaveDamage = 600f;

        private Rigidbody2D rb;
        private bool used;
        private bool grounded;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        public void Configure(float force)
        {
            downwardForce = force;
        }

        private void Update()
        {
            if (used || grounded) return;

            if (Input.GetMouseButtonDown(0))
                Activate();
        }

        private void Activate()
        {
            if (used || rb == null) return;
            used = true;

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            rb.AddForce(Vector2.down * downwardForce, ForceMode2D.Impulse);
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            if (grounded) return;

            if (used && col.contacts.Length > 0)
            {
                float hitNormalY = col.contacts[0].normal.y;
                if (hitNormalY > 0.5f)
                {
                    grounded = true;
                    ShockwaveEffect.CreateAt(transform.position, shockwaveRadius, shockwaveForce, shockwaveDamage);
                }
            }
        }
    }
}
