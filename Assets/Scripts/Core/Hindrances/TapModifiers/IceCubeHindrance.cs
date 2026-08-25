// Task 4.4 — IceCubeHindrance: plain tap = SFX only; swipe ≥80px ≤0.4s melts ice
using UnityEngine;
using DG.Tweening;
using AnimalFall.Core.Animals;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.TapModifiers
{
    public class IceCubeHindrance : HindranceBase
    {
        public override HindranceType Type => HindranceType.IceCube;

        private Animal         _targetAnimal;
        private SpriteRenderer _iceOverlay;

        protected override void OnActivate()
        {
            _targetAnimal = _ctx.HindranceManager?.GetRandomActiveAnimal();
            if (_targetAnimal == null) { Deactivate(); return; }

            _targetAnimal.IsIceFrozen = true;

            // Ice overlay child
            var go = new GameObject("IceOverlay");
            go.transform.SetParent(_targetAnimal.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale    = Vector3.one * 1.15f;
            _iceOverlay = go.AddComponent<SpriteRenderer>();
            _iceOverlay.sprite       = Utils.ImageLibrary.GetHindranceSprite(HindranceType.IceCube);
            _iceOverlay.color        = new Color(0.7f, 0.9f, 1f, 0.75f);
            _iceOverlay.sortingOrder = 5;

            // Subscribe to swipe event
            GameEvents.OnSwipeDetailed += OnSwipe;

            if (_sr != null) _sr.enabled = false;
        }

        protected override void OnDeactivate()
        {
            GameEvents.OnSwipeDetailed -= OnSwipe;
            if (_iceOverlay != null) Destroy(_iceOverlay.gameObject);
            if (_targetAnimal != null) _targetAnimal.IsIceFrozen = false;
        }

        private void OnSwipe(Vector2 screenStart, Vector2 screenEnd)
        {
            if (!_isActive) return;
            if (_targetAnimal == null || Camera.main == null) return;
            Vector2 targetScreen = Camera.main.WorldToScreenPoint(_targetAnimal.transform.position);
            float distance = DistanceToSegment(targetScreen, screenStart, screenEnd);
            if ((screenEnd - screenStart).magnitude >= 80f && distance <= 80f)
            {
                // Melt ice — shake then deactivate
                if (_targetAnimal != null)
                    _targetAnimal.transform.DOShakePosition(0.3f, 0.2f, 10, 90f).SetId(_targetAnimal.gameObject);
                Deactivate();
            }
        }

        private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float sqr = ab.sqrMagnitude;
            if (sqr < 0.001f) return Vector2.Distance(point, a);
            float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / sqr);
            return Vector2.Distance(point, a + ab * t);
        }
    }
}
