using UnityEngine;

namespace AnimalFall.Core.Hindrances.ScreenBlockers
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class StormCloudHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.StormCloud;

        [SerializeField] private float driftSpeed = 1f;
        [SerializeField] private float duration = 6f;
        [SerializeField] private float verticalWobble = 0.3f;

        private float spawnTime;
        private float startY;
        private int direction;

        public override void Activate(HindranceContext ctx)
        {
            base.Activate(ctx);
            spawnTime = Time.time;
            startY = transform.position.y;
            direction = Random.value > 0.5f ? 1 : -1;

            Camera cam = Camera.main;
            if (cam != null)
            {
                float x = direction > 0
                    ? cam.ViewportToWorldPoint(new Vector3(-0.15f, 0, 10f)).x
                    : cam.ViewportToWorldPoint(new Vector3(1.15f, 0, 10f)).x;
                float y = cam.ViewportToWorldPoint(
                    new Vector3(0, Random.Range(0.4f, 0.85f), 10f)).y;
                startY = y;
                transform.position = new Vector3(x, y, 0);
            }

            transform.localScale = Vector3.one * Random.Range(1.5f, 2.5f);
        }

        private void Update()
        {
            if (!IsActive) return;

            float elapsed = Time.time - spawnTime;

            float x = transform.position.x + direction * driftSpeed * Time.deltaTime;
            float y = startY + Mathf.Sin(elapsed * 1.5f) * verticalWobble;
            transform.position = new Vector3(x, y, 0);

            if (elapsed > duration)
            {
                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a -= Time.deltaTime;
                    sr.color = c;
                    if (c.a <= 0f) Deactivate();
                }
                else
                {
                    Deactivate();
                }
            }
        }
    }
}
