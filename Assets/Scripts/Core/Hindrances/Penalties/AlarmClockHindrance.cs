// Task 4.3 — AlarmClockHindrance: multiplies spawn rate for 5s; re-activate resets timer
using System.Collections;
using UnityEngine;

namespace AnimalFall.Core.Hindrances.Penalties
{
    public class AlarmClockHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.AlarmClock;

        private const float MULTIPLIER   = 0.6f;
        private const float DURATION     = 5f;
        private Coroutine   _timerCoroutine;
        private HindranceEffectToken _intervalToken;

        protected override void OnActivate()
        {
            if (_sr != null)
                _sr.sprite = Utils.ImageLibrary.GetHindranceSprite(HindranceType.AlarmClock);

            // Apply multiplier (no stacking — always sets to MULTIPLIER)
            _intervalToken?.Dispose();
            _intervalToken = _ctx.HindranceManager?.AddSpawnIntervalMultiplier(this, MULTIPLIER);

            // Re-activation resets timer
            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
            _timerCoroutine = StartCoroutine(TimerCoroutine());
        }

        protected override void OnDeactivate()
        {
            if (_timerCoroutine != null) { StopCoroutine(_timerCoroutine); _timerCoroutine = null; }
            _intervalToken?.Dispose();
            _intervalToken = null;
        }

        private IEnumerator TimerCoroutine()
        {
            yield return new WaitForSeconds(DURATION);
            Deactivate();
        }
    }
}
