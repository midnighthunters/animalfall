// Task 4.7 — CursedSkullHindrance: tapped = +2s, missed = -5s
using UnityEngine;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.Advanced
{
    public class CursedSkullHindrance : HindranceBase, IPointerTapTarget
    {
        public override HindranceType Type => HindranceType.CursedSkull;
        public int InteractionPriority => 300;

        [SerializeField] private float _fallSpeed = 2f;
        private float _screenBottom;
        private bool  _tapped;

        protected override void OnActivate()
        {
            _tapped = false;
            if (_sr != null)
                _sr.sprite = Utils.ImageLibrary.GetHindranceSprite(HindranceType.CursedSkull);
            if (Camera.main != null)
                _screenBottom = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, Mathf.Abs(Camera.main.transform.position.z))).y;

        }

        protected override void OnDeactivate()
        {
        }

        private void Update()
        {
            if (!_isActive) return;
            transform.Translate(0f, -_fallSpeed * Time.deltaTime, 0f);
            if (transform.position.y < _screenBottom - 0.5f && !_tapped)
            {
                // Missed — -5s
                _ctx.GameManager?.AddTime(-5f);
                Deactivate();
            }
        }

        private void OnTapped()
        {
            if (!_isActive || _tapped) return;
            _tapped = true;
            _ctx.GameManager?.AddTime(2f);
            Deactivate();
        }

        public bool TryHandleTap(WorldPointerEvent pointerEvent) { OnTapped(); return true; }
    }
}
