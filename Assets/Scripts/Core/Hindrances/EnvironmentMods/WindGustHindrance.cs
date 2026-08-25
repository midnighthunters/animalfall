// Task 4.6 — WindGustHindrance
using UnityEngine;
using System.Collections;

namespace AnimalFall.Core.Hindrances.EnvironmentMods
{
    public class WindGustHindrance : HindranceBase
    {
        private HindranceEffectToken _token;
        public override HindranceType Type => HindranceType.WindGust;

        protected override void OnActivate()
        {
            if (_sr != null) _sr.enabled = false;
            float mag   = Random.Range(1.5f, 3.0f);
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            var wind    = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.3f) * mag;
            _token = _ctx.EnvironmentEffects?.AddWind(this, wind);
            StartCoroutine(EndAfter(5f));
        }

        protected override void OnDeactivate()
        {
            _token?.Dispose(); _token = null;
        }

        private IEnumerator EndAfter(float seconds) { yield return new WaitForSeconds(seconds); Deactivate(); }
    }
}
