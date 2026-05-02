using UnityEngine;

namespace AnimalFall.Core.Hindrances.EnvironmentMods
{
    public class WindGustHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.WindGust;

        [SerializeField] private float windStrength = 3f;
        [SerializeField] private float duration = 4f;

        public override void Activate(HindranceContext ctx)
        {
            base.Activate(ctx);

            float direction = Random.value > 0.5f ? 1f : -1f;
            Vector2 force = new Vector2(direction * windStrength, 0f);

            ctx?.EnvironmentEffects?.ActivateWind(force, duration);

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
