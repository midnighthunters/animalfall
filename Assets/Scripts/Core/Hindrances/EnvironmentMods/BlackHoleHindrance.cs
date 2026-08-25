// Task 4.6 — BlackHoleHindrance: pulls nearby animals toward a random center
using UnityEngine;
using System.Collections;

namespace AnimalFall.Core.Hindrances.EnvironmentMods
{
    public class BlackHoleHindrance : HindranceBase
    {
        private HindranceEffectToken _token;
        public override HindranceType Type => HindranceType.BlackHole;

        protected override void OnActivate()
        {
            Vector2 center = Vector2.zero;
            if (Camera.main != null)
            {
                float z = Mathf.Abs(Camera.main.transform.position.z);
                float rx = Random.Range(0.2f, 0.8f);
                float ry = Random.Range(0.2f, 0.8f);
                center   = Camera.main.ViewportToWorldPoint(new Vector3(rx, ry, z));
            }

            _token = _ctx.EnvironmentEffects?.AddBlackHole(this, center, 1.5f);

            transform.position = center;
            if (_sr != null)
                _sr.sprite = Utils.ImageLibrary.GetHindranceSprite(HindranceType.BlackHole);
            StartCoroutine(EndAfter(5f));
        }

        protected override void OnDeactivate()
        {
            _token?.Dispose(); _token = null;
        }

        private IEnumerator EndAfter(float seconds) { yield return new WaitForSeconds(seconds); Deactivate(); }
    }
}
