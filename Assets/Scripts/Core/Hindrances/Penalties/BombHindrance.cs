// Task 4.3 — BombHindrance: falls, tapped → bomb event, missed → deactivate
using UnityEngine;
using AnimalFall.Managers;
using AnimalFall.Core.Animals;

namespace AnimalFall.Core.Hindrances.Penalties
{
    public class BombHindrance : HindranceBase, IPointerTapTarget
    {
        public override HindranceType Type => HindranceType.Bomb;
        public int InteractionPriority => 300;

        [SerializeField] private float _fallSpeed = 2.5f;
        private float _screenBottom;

        protected override void OnActivate()
        {
            if (Camera.main != null)
                _screenBottom = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, Mathf.Abs(Camera.main.transform.position.z))).y;

            if (_sr != null)
                _sr.sprite = Utils.ImageLibrary.GetHindranceSprite(HindranceType.Bomb);
        }

        protected override void OnDeactivate() { }

        private void Update()
        {
            if (!_isActive) return;
            transform.Translate(0f, -_fallSpeed * Time.deltaTime, 0f);
            if (transform.position.y < _screenBottom - 1f)
                Deactivate();
        }

        /// <summary>Called by InputManager when tap hits this collider.</summary>
        public void OnTapped()
        {
            if (!_isActive) return;

            Vector3 explosionPosition = transform.position;
            var animalsOnScreen = new System.Collections.Generic.List<Animal>(ActiveAnimalRegistry.All);

            GameEvents.OnBombTapped?.Invoke(explosionPosition);

            for (int i = 0; i < animalsOnScreen.Count; i++)
            {
                Animal animal = animalsOnScreen[i];
                if (animal != null &&
                    animal.gameObject.activeInHierarchy &&
                    !animal.IsCollected &&
                    animal.Data != null &&
                    animal.Data.type != AnimalType.Bomb)
                {
                    animal.OnCollected();
                }
            }

            Deactivate();
        }

        public bool TryHandleTap(WorldPointerEvent pointerEvent) { OnTapped(); return true; }
    }
}
