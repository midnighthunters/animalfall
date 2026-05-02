using UnityEngine;
using AnimalFall.Utils;

namespace AnimalFall.Core.Hindrances.EnvironmentMods
{
    public class BlackHoleHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.BlackHole;

        [SerializeField] private float pullStrength = 2f;
        [SerializeField] private float duration = 5f;

        public override void Activate(HindranceContext ctx)
        {
            base.Activate(ctx);

            Camera cam = Camera.main;
            Vector2 center = MathUtils.ScreenCenter(cam);

            ctx?.EnvironmentEffects?.ActivateBlackHole(center, pullStrength, duration);

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            Invoke(nameof(FinishHindrance), duration);
        }

        private void FinishHindrance()
        {
            Deactivate();
        }
    }
}
