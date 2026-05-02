using UnityEngine;
using AnimalFall.Managers;
using AnimalFall.Utils;

namespace AnimalFall.Core.Hindrances.TapModifiers
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class IceCubeHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.IceCube;

        [SerializeField] private float fallSpeed = 1.3f;
        [SerializeField] private int pointValue = 90;
        [SerializeField] private float swipeMeltThreshold = 80f;

        private bool melted;
        private SpriteRenderer sr;

        public override void Activate(HindranceContext ctx)
        {
            base.Activate(ctx);
            melted = false;
            sr = GetComponent<SpriteRenderer>();

            if (sr != null)
                sr.color = new Color(0.6f, 0.85f, 1f, 0.9f);

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
            if (!IsActive) return;
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

            if (transform.position.y < -6f)
                Deactivate();
        }

        private void OnSwipeDetected(Vector2 start, Vector2 end, SwipeDirection dir)
        {
            if (!IsActive || melted) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector2 worldStart = cam.ScreenToWorldPoint(start);
            float dist = Vector2.Distance(worldStart, transform.position);

            if (dist < 1.5f)
            {
                melted = true;
                if (sr != null)
                    sr.color = Color.white;
            }
        }

        private void OnMouseDown()
        {
            if (!IsActive) return;

            if (!melted)
            {
                context?.AudioManager?.PlaySFX(AudioManager.SfxType.WrongTap);
                return;
            }

            context?.GameManager?.OnCorrectTap(1, pointValue);
            context?.AudioManager?.PlaySFX(AudioManager.SfxType.Collect);
            Deactivate();
        }
    }
}
