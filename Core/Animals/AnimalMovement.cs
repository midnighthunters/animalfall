using UnityEngine;
using AnimalFall.Effects;

namespace AnimalFall.Core.Animals
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class AnimalMovement : MonoBehaviour
    {
        [Header("Movement")]
        public MovementPattern pattern = MovementPattern.Drift;
        public float speed = 1f;
        public float zigzagAmplitude = 0.5f;
        public float zigzagFrequency = 2f;

        [Header("Bounds")]
        [SerializeField] private float screenMargin = 0.05f;
        [SerializeField] private bool destroyWhenBelowScreen = true;

        private Vector3 startPos;
        private float spawnTime;
        private Rigidbody2D rb;
        private float moveDirX;
        private Camera cam;
        private float minX, maxX, minY;
        private float halfWidth, halfHeight;
        private float zDistance;
        private bool hasBounced;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            startPos = transform.position;
            spawnTime = Time.time;
            moveDirX = Random.Range(-0.6f, 0.6f);

            cam = Camera.main;
            zDistance = Mathf.Abs(transform.position.z - (cam != null ? cam.transform.position.z : 0f));

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                halfWidth = sr.bounds.extents.x;
                halfHeight = sr.bounds.extents.y;
            }
            else
            {
                halfWidth = 0.5f;
                halfHeight = 0.5f;
            }

            RecalcBounds();
        }

        public void ConfigureRandomSpeed(float min, float max)
        {
            speed = Random.Range(min, max);
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (EnvironmentEffects.Instance != null && EnvironmentEffects.Instance.IsZeroGravityActive)
            {
                float floatX = Mathf.Sin(Time.time * 2f + startPos.x) * 0.3f;
                float floatY = Mathf.Cos(Time.time * 1.5f + startPos.y) * 0.2f;
                transform.Translate(new Vector3(floatX, floatY, 0) * dt);
                return;
            }

            Vector3 windOffset = Vector3.zero;
            if (EnvironmentEffects.Instance != null && EnvironmentEffects.Instance.IsWindActive)
            {
                Vector2 wind = EnvironmentEffects.Instance.WindForce;
                windOffset = new Vector3(wind.x, wind.y, 0) * dt;
            }

            Vector3 bhPull = Vector3.zero;
            if (EnvironmentEffects.Instance != null && EnvironmentEffects.Instance.IsBlackHoleActive)
            {
                Vector2 center = EnvironmentEffects.Instance.BlackHoleCenter;
                Vector2 dir = center - (Vector2)transform.position;
                float dist = dir.magnitude;
                if (dist > 0.5f)
                {
                    float pullForce = EnvironmentEffects.Instance.BlackHolePullStrength / (dist * dist) * dt;
                    bhPull = (Vector3)(dir.normalized * pullForce);
                }
            }

            switch (pattern)
            {
                case MovementPattern.Static:
                    transform.Translate(Vector3.down * speed * dt);
                    break;

                case MovementPattern.Drift:
                    transform.Translate((Vector3.down + Vector3.right * moveDirX * 0.1f) * speed * dt);
                    break;

                case MovementPattern.ZigZag:
                    float x = Mathf.Sin((Time.time - spawnTime) * zigzagFrequency) * zigzagAmplitude;
                    transform.position += Vector3.down * speed * dt;
                    transform.position = new Vector3(startPos.x + x, transform.position.y, transform.position.z);
                    break;

                case MovementPattern.Teleport:
                    transform.Translate(Vector3.down * speed * dt);
                    if (Random.value < 0.002f)
                    {
                        float down = Random.Range(0.5f, 1.2f);
                        transform.position += Vector3.down * down;
                    }
                    break;

                case MovementPattern.Bounce:
                    transform.Translate(Vector3.down * speed * dt);
                    if (!hasBounced && transform.position.y < minY + halfHeight + 0.1f)
                    {
                        hasBounced = true;
                        speed = -Mathf.Abs(speed) * 0.6f;
                    }
                    break;

                case MovementPattern.SineWave:
                    float sinX = Mathf.Sin((Time.time - spawnTime) * zigzagFrequency * 2f) * zigzagAmplitude;
                    transform.Translate((Vector3.down + Vector3.right * sinX) * speed * dt);
                    break;

                case MovementPattern.FloatUp:
                    transform.Translate(Vector3.up * speed * 0.5f * dt);
                    float driftX = Mathf.Sin(Time.time * 2f) * 0.3f;
                    transform.Translate(Vector3.right * driftX * dt);
                    break;

                case MovementPattern.HeavyFall:
                    transform.Translate(Vector3.down * speed * 3f * dt);
                    break;

                case MovementPattern.Erratic:
                    float erX = Mathf.PerlinNoise(Time.time * 3f, startPos.x) - 0.5f;
                    float erY = Mathf.PerlinNoise(Time.time * 2f, startPos.y) - 0.5f;
                    transform.Translate((Vector3.down * speed + new Vector3(erX, erY, 0) * 2f) * dt);
                    break;
            }

            transform.position += windOffset + bhPull;

            if (cam == null) cam = Camera.main;
            if (cam != null) RecalcBounds();

            float clampedX = Mathf.Clamp(transform.position.x, minX + halfWidth, maxX - halfWidth);
            transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);

            if (destroyWhenBelowScreen)
            {
                if (transform.position.y < (minY - halfHeight - 0.1f))
                    Destroy(gameObject);
            }
            else
            {
                transform.position = new Vector3(
                    transform.position.x,
                    Mathf.Max(transform.position.y, minY - 5f),
                    transform.position.z
                );
            }
        }

        private void RecalcBounds()
        {
            if (cam == null) return;
            Vector3 bl = cam.ViewportToWorldPoint(new Vector3(screenMargin, screenMargin, zDistance));
            Vector3 tr = cam.ViewportToWorldPoint(new Vector3(1f - screenMargin, 1f - screenMargin, zDistance));
            minX = bl.x;
            minY = bl.y;
            maxX = tr.x;
        }
    }
}
