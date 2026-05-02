using UnityEngine;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.TapModifiers
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class BubbleShieldHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.BubbleShield;

        [SerializeField] private float floatSpeed = 1f;
        [SerializeField] private float horizontalDrift = 0.5f;
        [SerializeField] private int pointValue = 80;

        private bool bubblePopped;
        private float spawnTime;

        public override void Activate(HindranceContext ctx)
        {
            base.Activate(ctx);
            bubblePopped = false;
            spawnTime = Time.time;

            Camera cam = Camera.main;
            if (cam != null)
            {
                float x = Random.Range(0.15f, 0.85f);
                Vector3 pos = cam.ViewportToWorldPoint(new Vector3(x, -0.05f, 10f));
                pos.z = 0f;
                transform.position = pos;
            }
        }

        private void Update()
        {
            if (!IsActive) return;

            if (!bubblePopped)
            {
                float elapsed = Time.time - spawnTime;
                float xDrift = Mathf.Sin(elapsed * 2f) * horizontalDrift;
                transform.Translate(
                    (Vector3.up + Vector3.right * xDrift) * floatSpeed * Time.deltaTime);

                float scale = 1f + Mathf.Sin(elapsed * 3f) * 0.05f;
                transform.localScale = Vector3.one * scale;

                if (transform.position.y > 6f)
                    Deactivate();
            }
            else
            {
                transform.Translate(Vector3.down * 2f * Time.deltaTime);
                if (transform.position.y < -6f)
                    Deactivate();
            }
        }

        private void OnMouseDown()
        {
            if (!IsActive) return;

            if (!bubblePopped)
            {
                bubblePopped = true;
                transform.localScale = Vector3.one;
                context?.AudioManager?.PlaySFX(AudioManager.SfxType.ShieldBreak);
            }
            else
            {
                context?.GameManager?.OnCorrectTap(1, pointValue);
                context?.AudioManager?.PlaySFX(AudioManager.SfxType.Collect);
                Deactivate();
            }
        }
    }
}
