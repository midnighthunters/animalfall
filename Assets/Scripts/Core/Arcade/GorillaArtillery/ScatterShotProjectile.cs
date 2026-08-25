using UnityEngine;

namespace AnimalFall.Core.Arcade.GorillaArtillery
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class ScatterShotProjectile : MonoBehaviour
    {
        [Header("Scatter Settings")]
        [SerializeField] private int fragmentCount = 5;
        [SerializeField] private float fragmentSpread = 3f;
        [SerializeField] private float fragmentMass = 0.5f;
        [SerializeField] private GameObject fragmentPrefab;
        [SerializeField] private float lifetime = 8f;

        private Rigidbody2D rb;
        private WindVector wind;
        private bool split;
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

            if (!split && Input.GetMouseButtonDown(0))
            {
                Split();
            }
        }

        private void Split()
        {
            split = true;
            Vector2 baseVel = rb.linearVelocity;

            for (int i = 0; i < fragmentCount; i++)
            {
                float angle = (i - fragmentCount / 2f) * (fragmentSpread / fragmentCount);
                Vector2 offset = Quaternion.Euler(0, 0, angle * 10f) * baseVel;

                GameObject frag;
                if (fragmentPrefab != null)
                {
                    frag = Instantiate(fragmentPrefab, transform.position, Quaternion.identity);
                }
                else
                {
                    frag = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    frag.transform.position = transform.position;
                    frag.transform.localScale = Vector3.one * 0.3f;
                    Destroy(frag.GetComponent<Collider>());
                    frag.AddComponent<CircleCollider2D>();
                }

                var fragRb = frag.GetComponent<Rigidbody2D>();
                if (fragRb == null) fragRb = frag.AddComponent<Rigidbody2D>();
                fragRb.mass = fragmentMass;
                fragRb.gravityScale = 1f;
                fragRb.linearVelocity = offset;
                fragRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                Destroy(frag, lifetime - (Time.time - spawnTime));
            }

            Destroy(gameObject);
        }
    }
}
