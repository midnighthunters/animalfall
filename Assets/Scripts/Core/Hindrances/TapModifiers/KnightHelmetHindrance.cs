// Task 4.4 — KnightHelmetHindrance: wraps animal in 3-tap helmet
using UnityEngine;
using AnimalFall.Core.Animals;

namespace AnimalFall.Core.Hindrances.TapModifiers
{
    public class KnightHelmetHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.KnightHelmet;

        [SerializeField] private Sprite _helmetOverlaySprite;
        private Animal       _targetAnimal;
        private SpriteRenderer _overlayRenderer;

        protected override void OnActivate()
        {
            _targetAnimal = _ctx.HindranceManager?.GetRandomActiveAnimal();
            if (_targetAnimal == null) { Deactivate(); return; }

            _targetAnimal.HelmetLayers = 3;

            // Attach helmet overlay as child
            var overlayGO = new GameObject("HelmetOverlay");
            overlayGO.transform.SetParent(_targetAnimal.transform, false);
            overlayGO.transform.localPosition = Vector3.zero;
            overlayGO.transform.localScale    = Vector3.one * 1.2f;
            _overlayRenderer = overlayGO.AddComponent<SpriteRenderer>();
            _overlayRenderer.sprite       = _helmetOverlaySprite ?? Utils.ImageLibrary.GetHindranceSprite(HindranceType.KnightHelmet);
            _overlayRenderer.sortingOrder = 5;

            // Self-hide the hindrance gameobject — we're now attached to the animal
            if (_sr != null) _sr.enabled = false;
        }

        protected override void OnDeactivate()
        {
            if (_overlayRenderer != null)
                Destroy(_overlayRenderer.gameObject);
        }
    }
}
