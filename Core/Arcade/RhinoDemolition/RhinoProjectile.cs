using UnityEngine;

namespace AnimalFall.Core.Arcade.RhinoDemolition
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class RhinoProjectile : MonoBehaviour
    {
        private Rigidbody2D rb;
        private GroundPound groundPound;
        private DamageScoreTracker scoreTracker;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            groundPound = GetComponent<GroundPound>();
            if (groundPound == null)
                groundPound = gameObject.AddComponent<GroundPound>();
        }

        public void Configure(float mass, float groundPoundForce)
        {
            rb.mass = mass;
            groundPound.Configure(groundPoundForce);
            scoreTracker = DamageScoreTracker.Instance;
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            if (scoreTracker == null) return;

            float velocity = col.relativeVelocity.magnitude;
            float mass = rb.mass;
            scoreTracker.RegisterBlockDestruction(velocity, mass);
        }
    }
}
