using UnityEngine;

namespace AnimalFall.Core.Hindrances.ScreenBlockers
{
    public class FallingLeavesHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.FallingLeaves;

        [SerializeField] private float duration = 8f;

        public override void Activate(HindranceContext ctx)
        {
            base.Activate(ctx);
            ctx?.ScreenEffects?.SpawnFallingLeaves(duration);

            GetComponent<SpriteRenderer>()?.gameObject.SetActive(false);
            Invoke(nameof(FinishHindrance), duration);
        }

        private void FinishHindrance()
        {
            Deactivate();
        }
    }
}
