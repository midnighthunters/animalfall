using UnityEngine;

namespace AnimalFall.Core.Arcade.GorillaArtillery
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class BoulderProjectile : MonoBehaviour
    {
        [SerializeField] private float lifetime = 8f;

        private Rigidbody2D rb;
        private WindVector wind;
        private float spawnTime;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.mass = 3f;
            rb.gravityScale = 1f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            spawnTime = Time.time;
        }

        public void Launch(Vector2 velocity, WindVector windVector)
        {
            wind = windVector;
            rb.linearVelocity = velocity;
        }

        private void FixedUpdate()
        {
            if (wind != null)
                rb.AddForce(wind.CurrentWind, ForceMode2D.Force);
        }

        private void Update()
        {
            if (Time.time - spawnTime > lifetime)
                Destroy(gameObject);
        }
    }
}
