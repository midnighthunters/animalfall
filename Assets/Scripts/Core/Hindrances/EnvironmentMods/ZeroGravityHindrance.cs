// Task 4.6 — ZeroGravityHindrance: 4s of zero gravity
using System.Collections;
using UnityEngine;

namespace AnimalFall.Core.Hindrances.EnvironmentMods
{
    public class ZeroGravityHindrance : HindranceBase
    {
        private HindranceEffectToken _token;
        public override HindranceType Type => HindranceType.ZeroGravity;

        protected override void OnActivate()
        {
            if (_sr != null) _sr.enabled = false;
            _token = _ctx.EnvironmentEffects?.AddZeroGravity(this);
            StartCoroutine(ZeroGravCoroutine());
        }

        protected override void OnDeactivate()
        {
            _token?.Dispose(); _token = null;
        }

        private IEnumerator ZeroGravCoroutine()
        {
            yield return new WaitForSeconds(4f);
            Deactivate();
        }
    }
}
