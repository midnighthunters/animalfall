using UnityEngine;

namespace AnimalFall.Core.Arcade.ArmadilloRicochet
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class ArmadilloBall : MonoBehaviour
    {
        [SerializeField] private float mass = 1.5f;

        private Rigidbody2D rb;
        private SlamCharge slamCharge;
        private bool dropped;

        public bool HasReachedExit { get; private set; }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.mass = mass;
            rb.gravityScale = 1f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var mat = new PhysicsMaterial2D("ArmadilloShell")
            {
                bounciness = 0.8f,
                friction = 0.15f
            };
            GetComponent<CircleCollider2D>().sharedMaterial = mat;

            rb.simulated = false;
        }

        public void Configure(int slamCharges, float slamForce)
        {
            slamCharge = GetComponent<SlamCharge>();
            if (slamCharge == null)
                slamCharge = gameObject.AddComponent<SlamCharge>();
            slamCharge.Configure(rb, slamCharges, slamForce);
        }

        public void Drop(float xPosition)
        {
            transform.position = new Vector3(xPosition, transform.position.y, 0);
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
            dropped = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("ExitPit") || other.gameObject.name.Contains("ExitPit"))
            {
                HasReachedExit = true;
            }
        }
    }
}
