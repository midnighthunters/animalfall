using UnityEngine;

namespace AnimalFall.Core.Arcade.GorillaArtillery
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class MudBallProjectile : MonoBehaviour
    {
        [Header("Mud Settings")]
        [SerializeField] private float addedMass = 5f;
        [SerializeField] private float stickDuration = 4f;
        [SerializeField] private float lifetime = 8f;

        private Rigidbody2D rb;
        private WindVector wind;
        private bool stuck;
        private float spawnTime;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.mass = 2f;
            rb.gravityScale = 1f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            spawnTime = Time.time;
        }

        public void Launch(Vector2 velocity, WindVector windVector)
        {
            wind = windVector;
            rb.velocity = velocity;
        }

        private void FixedUpdate()
        {
            if (!stuck && wind != null)
                rb.AddForce(wind.CurrentWind, ForceMode2D.Force);
        }

        private void Update()
        {
            if (Time.time - spawnTime > lifetime)
                Destroy(gameObject);
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            if (stuck) return;

            var drone = col.gameObject.GetComponent<DroneTarget>();
            if (drone != null)
            {
                StickToDrone(drone);
            }
        }

        private void StickToDrone(DroneTarget drone)
        {
            stuck = true;

            rb.velocity = Vector2.zero;
            rb.gravityScale = 0f;
            rb.isKinematic = true;

            transform.SetParent(drone.transform);
            transform.localPosition = Vector3.zero;

            var droneRb = drone.GetComponent<Rigidbody2D>();
            if (droneRb != null)
            {
                droneRb.mass += addedMass;
                droneRb.gravityScale = 1f;
            }

            Destroy(gameObject, stickDuration);
        }
    }
}
