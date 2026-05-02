using UnityEngine;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.TapModifiers
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class TeleporterHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.Teleporter;

        [SerializeField] private float fallSpeed = 1.8f;
        [SerializeField] private int pointValue = 70;

        private bool hasTeleported;
        private float screenMidY;
        private float teleportXRange = 3f;

        public override void Activate(HindranceContext ctx)
        {
            base.Activate(ctx);
            hasTeleported = false;

            Camera cam = Camera.main;
            if (cam != null)
            {
                screenMidY = cam.ViewportToWorldPoint(new Vector3(0, 0.5f, 10f)).y;
                float left = cam.ViewportToWorldPoint(new Vector3(0.1f, 0, 10f)).x;
                float right = cam.ViewportToWorldPoint(new Vector3(0.9f, 0, 10f)).x;
                teleportXRange = right - left;
            }
        }

        private void Update()
        {
            if (!IsActive) return;

            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

            if (!hasTeleported && transform.position.y <= screenMidY)
            {
                hasTeleported = true;
                Camera cam = Camera.main;
                if (cam != null)
                {
                    float left = cam.ViewportToWorldPoint(new Vector3(0.1f, 0, 10f)).x;
                    float newX = left + Random.Range(0f, teleportXRange);
                    transform.position = new Vector3(newX, transform.position.y, transform.position.z);

                    SpriteRenderer sr = GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        Color c = sr.color;
                        c.a = 0.3f;
                        sr.color = c;
                        StartCoroutine(FadeIn(sr));
                    }
                }
            }

            if (transform.position.y < -6f)
                Deactivate();
        }

        private System.Collections.IEnumerator FadeIn(SpriteRenderer sr)
        {
            float elapsed = 0f;
            while (elapsed < 0.3f && sr != null)
            {
                elapsed += Time.deltaTime;
                Color c = sr.color;
                c.a = Mathf.Lerp(0.3f, 1f, elapsed / 0.3f);
                sr.color = c;
                yield return null;
            }
        }

        private void OnMouseDown()
        {
            if (!IsActive) return;
            context?.GameManager?.OnCorrectTap(1, pointValue);
            context?.AudioManager?.PlaySFX(AudioManager.SfxType.Collect);
            Deactivate();
        }
    }
}
