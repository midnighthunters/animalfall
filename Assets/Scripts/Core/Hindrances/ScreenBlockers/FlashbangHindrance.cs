// Task 4.5 — FlashbangHindrance: full-screen white flash
using System.Collections;
using UnityEngine;

namespace AnimalFall.Core.Hindrances.ScreenBlockers
{
    public class FlashbangHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.Flashbang;

        protected override void OnActivate()
        {
            _ctx.ScreenEffects?.FlashWhite();
            if (_sr != null) _sr.enabled = false;
            StartCoroutine(FinishAfter(0.8f));
        }

        protected override void OnDeactivate() { }

        private IEnumerator FinishAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            Deactivate();
        }
    }
}
