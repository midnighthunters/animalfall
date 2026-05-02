using UnityEngine;
using AnimalFall.Core.Animals;

namespace AnimalFall.Core.Hindrances.ScreenBlockers
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class TornadoHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.Tornado;

        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float duration = 5f;
        [SerializeField] private float tossForce = 3f;
        [SerializeField] private float tossRadius = 2f;

        private float spawnTime;

        public override void Activate(HindranceContext ctx)
        {
            base.Activate(ctx);
            spawnTime = Time.time;

            Camera cam = Camera.main;
            if (cam != null)
            {
                float x = cam.ViewportToWorldPoint(new Vector3(-0.1f, 0, 10f)).x;
                float y = cam.ViewportToWorldPoint(new Vector3(0, 0.5f, 10f)).y;
                transform.position = new Vector3(x, y, 0);
            }
        }

        private void Update()
        {
            if (!IsActive) return;

            float elapsed = Time.time - spawnTime;

            transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);
            transform.Rotate(0, 0, -Time.deltaTime * 360f);

            float yOff = Mathf.Sin(elapsed * 2f) * 0.5f;
            transform.position = new Vector3(
                transform.position.x,
                transform.position.y + yOff * Time.deltaTime,
                0);

            TossNearbyAnimals();

            if (elapsed > duration)
                Deactivate();
        }

        private void TossNearbyAnimals()
        {
            var animals = FindObjectsOfType<Animal>();
            foreach (var a in animals)
            {
                if (a == null) continue;
                float dist = Vector2.Distance(transform.position, a.transform.position);
                if (dist < tossRadius)
                {
                    Vector2 randomDir = new Vector2(
                        Random.Range(-1f, 1f), Random.Range(-0.5f, 1f)).normalized;
                    a.transform.position += (Vector3)(randomDir * tossForce * Time.deltaTime);
                }
            }
        }
    }
}
