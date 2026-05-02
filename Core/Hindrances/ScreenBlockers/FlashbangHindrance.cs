using UnityEngine;

namespace AnimalFall.Core.Hindrances.ScreenBlockers
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class FlashbangHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.Flashbang;

        [SerializeField] private float fallSpeed = 2f;
        [SerializeField] private float flashDuration = 1f;

        private SpriteRenderer sr;
        private float glowTimer;

        public override void Activate(HindranceContext ctx)
        {
            base.Activate(ctx);
            sr = GetComponent<SpriteRenderer>();
            glowTimer = 0f;
        }

        private void Update()
        {
            if (!IsActive) return;
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

            glowTimer += Time.deltaTime;
            if (sr != null)
            {
                float glow = Mathf.PingPong(glowTimer * 3f, 1f);
                sr.color = Color.Lerp(Color.white, Color.yellow, glow);
            }

            if (transform.position.y < -6f)
                Deactivate();
        }

        private void OnMouseDown()
        {
            if (!IsActive) return;
            context?.ScreenEffects?.ShowFlashbang(flashDuration);
            Deactivate();
        }
    }
}
