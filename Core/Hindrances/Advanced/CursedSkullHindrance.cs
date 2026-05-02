using UnityEngine;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.Advanced
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class CursedSkullHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.CursedSkull;

        [SerializeField] private float fallSpeed = 1.6f;
        [SerializeField] private float timePenaltyOnMiss = 5f;

        private bool destroyed;

        public override void Activate(HindranceContext ctx)
        {
            base.Activate(ctx);
            destroyed = false;
        }

        private void Update()
        {
            if (!IsActive || destroyed) return;

            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

            float bob = Mathf.Sin(Time.time * 6f) * 0.05f;
            transform.position += new Vector3(bob, 0, 0);

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                float glow = Mathf.PingPong(Time.time * 2f, 0.3f);
                sr.color = new Color(0.8f + glow, 0.2f, 0.2f + glow);
            }

            if (transform.position.y < -6f)
            {
                context?.GameManager?.AddTime(-timePenaltyOnMiss);
                context?.AudioManager?.PlaySFX(AudioManager.SfxType.Explosion);
                Deactivate();
            }
        }

        private void OnMouseDown()
        {
            if (!IsActive) return;
            destroyed = true;
            context?.AudioManager?.PlaySFX(AudioManager.SfxType.ShieldBreak);
            Deactivate();
        }
    }
}
