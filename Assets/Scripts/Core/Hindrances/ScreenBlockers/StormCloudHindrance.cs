// Task 4.5 — StormCloudHindrance: dark gradient covers lower 60% for 6s
using System.Collections;
using UnityEngine;

namespace AnimalFall.Core.Hindrances.ScreenBlockers
{
    public class StormCloudHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.StormCloud;

        protected override void OnActivate()
        {
            _ctx.ScreenEffects?.ShowStormGradient(6f);
            if (_sr != null) _sr.enabled = false;
            StartCoroutine(FinishAfter(6f));
        }

        protected override void OnDeactivate() { }

        private IEnumerator FinishAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            Deactivate();
        }
    }
}
