using UnityEngine;

namespace AnimalFall.Core.Hindrances.EnvironmentMods
{
    public class LaserBeamHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.LaserBeam;

        [SerializeField] private float duration = 4f;
        [SerializeField] private float warningTime = 1f;

        public override void Activate(HindranceContext ctx)
        {
            base.Activate(ctx);

            Camera cam = Camera.main;
            float y = 0f;
            if (cam != null)
                y = cam.ViewportToWorldPoint(new Vector3(0, Random.Range(0.2f, 0.7f), 10f)).y;

            ctx?.EnvironmentEffects?.SpawnLaserBeam(y, duration, warningTime);

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            Invoke(nameof(FinishHindrance), duration + warningTime);
        }

        private void FinishHindrance()
        {
            Deactivate();
        }
    }
}
