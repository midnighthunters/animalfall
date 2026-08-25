// Task 4.7 — MirrorModeHindrance: 8s of mirrored X axis
using System.Collections;
using UnityEngine;

namespace AnimalFall.Core.Hindrances.Advanced
{
    public class MirrorModeHindrance : HindranceBase
    {
        private HindranceEffectToken _inputToken;
        private HindranceEffectToken _environmentToken;
        public override HindranceType Type => HindranceType.MirrorMode;

        protected override void OnActivate()
        {
            if (_sr != null) _sr.enabled = false;
            _inputToken = _ctx.InputManager?.AddMirror(this);
            _environmentToken = _ctx.EnvironmentEffects?.AddMirror(this);
            StartCoroutine(MirrorCoroutine());
        }

        protected override void OnDeactivate()
        {
            _inputToken?.Dispose(); _inputToken = null;
            _environmentToken?.Dispose(); _environmentToken = null;
        }

        private IEnumerator MirrorCoroutine()
        {
            yield return new WaitForSeconds(8f);
            Deactivate();
        }
    }
}
