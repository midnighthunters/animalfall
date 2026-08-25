// Task 4.5 — InkSquidHindrance: shows ink overlay for 4s
using System.Collections;
using UnityEngine;

namespace AnimalFall.Core.Hindrances.ScreenBlockers
{
    public class InkSquidHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.InkSquid;

        protected override void OnActivate()
        {
            _ctx.ScreenEffects?.ShowInkOverlay(4f);
            if (_sr != null) _sr.enabled = false;
            StartCoroutine(FinishAfter(4f));
        }

        protected override void OnDeactivate() { }

        private IEnumerator FinishAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            Deactivate();
        }
    }
}
