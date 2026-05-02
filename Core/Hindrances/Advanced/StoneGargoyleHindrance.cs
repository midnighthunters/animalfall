using UnityEngine;
using AnimalFall.Utils;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.Advanced
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class StoneGargoyleHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.StoneGargoyle;

        [SerializeField] private float lifetime = 6f;

        private float spawnTime;
        private bool swipedAway;

        public override void Activate(HindranceContext ctx)
        {
            base.Activate(ctx);
            spawnTime = Time.time;
            swipedAway = false;

            Camera cam = Camera.main;
            if (cam != null)
            {
                float x = Random.Range(0.15f, 0.85f);
                float y = Random.Range(0.3f, 0.7f);
                Vector3 pos = cam.ViewportToWorldPoint(new Vector3(x, y, 10f));
                pos.z = 0f;
                transform.position = pos;
            }

            transform.localScale = Vector3.one * 1.5f;

            if (GestureDetector.Instance != null)
                GestureDetector.Instance.OnSwipe += OnSwipeDetected;
        }

        public override void Deactivate()
        {
            if (GestureDetector.Instance != null)
                GestureDetector.Instance.OnSwipe -= OnSwipeDetected;
            base.Deactivate();
        }

        private void Update()
        {
            if (!IsActive || swipedAway) return;

            if (Time.time - spawnTime > lifetime)
                Deactivate();
        }

        private void OnSwipeDetected(Vector2 start, Vector2 end, SwipeDirection dir)
        {
            if (!IsActive || swipedAway) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector2 worldStart = cam.ScreenToWorldPoint(start);
            float dist = Vector2.Distance(worldStart, transform.position);

            if (dist < 2f)
            {
                swipedAway = true;
                context?.AudioManager?.PlaySFX(AudioManager.SfxType.ShieldBreak);

                Vector3 flyDir = dir == SwipeDirection.Left ? Vector3.left :
                                 dir == SwipeDirection.Right ? Vector3.right :
                                 dir == SwipeDirection.Up ? Vector3.up : Vector3.down;

                StartCoroutine(FlyAway(flyDir));
            }
        }

        private System.Collections.IEnumerator FlyAway(Vector3 direction)
        {
            float elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                transform.position += direction * 8f * Time.deltaTime;
                transform.localScale *= 0.95f;
                yield return null;
            }
            Deactivate();
        }

        private void OnMouseDown()
        {
            if (!IsActive) return;
            context?.AudioManager?.PlaySFX(AudioManager.SfxType.WrongTap);
        }
    }
}
