// Task 4.3 — PoisonVialHindrance: tapped → UseLife + WrongTap SFX
using UnityEngine;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.Penalties
{
    public class PoisonVialHindrance : HindranceBase, IPointerTapTarget
    {
        public override HindranceType Type => HindranceType.PoisonVial;
        public int InteractionPriority => 300;

        [SerializeField] private float _fallSpeed = 2f;
        private float _screenBottom;

        protected override void OnActivate()
        {
            if (Camera.main != null)
                _screenBottom = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, Mathf.Abs(Camera.main.transform.position.z))).y;

            if (_sr != null)
                _sr.sprite = Utils.ImageLibrary.GetHindranceSprite(HindranceType.PoisonVial);
        }

        protected override void OnDeactivate() { }

        private void Update()
        {
            if (!_isActive) return;
            transform.Translate(0f, -_fallSpeed * Time.deltaTime, 0f);
            if (transform.position.y < _screenBottom - 1f)
                Deactivate();
        }

        public void OnTapped()
        {
            if (!_isActive) return;
            _ctx.LivesManager?.UseLife();
            GameEvents.OnSfxRequested?.Invoke(SfxType.WrongTap);
            Deactivate();
        }

        public bool TryHandleTap(WorldPointerEvent pointerEvent) { OnTapped(); return true; }
    }
}
