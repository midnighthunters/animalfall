// Task 4.7 — MagnetTrapHindrance: offsets tap position by random vector
using UnityEngine;

namespace AnimalFall.Core.Hindrances.Advanced
{
    public class MagnetTrapHindrance : HindranceBase
    {
        private HindranceEffectToken _token;
        public override HindranceType Type => HindranceType.MagnetTrap;

        protected override void OnActivate()
        {
            if (_sr != null) _sr.enabled = false;
            float mag    = Random.Range(0.3f, 0.8f);
            float angle  = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            var offset   = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * mag;
            _token = _ctx.InputManager?.AddMagnetOffset(this, offset);
            Invoke(nameof(Deactivate), 6f);
        }

        protected override void OnDeactivate()
        {
            CancelInvoke(); _token?.Dispose(); _token = null;
        }
    }
}
