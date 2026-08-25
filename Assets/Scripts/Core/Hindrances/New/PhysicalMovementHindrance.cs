using System.Collections;
using UnityEngine;
using AnimalFall.Core.Animals;

namespace AnimalFall.Core.Hindrances.New
{
    public sealed class PhysicalMovementHindrance : HindranceBase, IPointerTapTarget
    {
        [SerializeField] private HindranceType _type;
        [SerializeField] private float _duration = 7f;
        private Animal _target;
        private AnimalMovement _movement;
        private float _nextImpulse;

        public override HindranceType Type => _type;
        public int InteractionPriority => 180;

#if UNITY_EDITOR
        public void EditorConfigure(HindranceType type, float duration)
        { _type = type; _duration = duration; }
#endif

        protected override void OnActivate()
        {
            if (IsHolder(Type))
            {
                _target = _ctx.HindranceManager?.GetRandomActiveAnimal();
                if (_target == null || !_target.TryClaimExclusive(this)) { Deactivate(); return; }
                _movement = _target.GetComponent<AnimalMovement>();
                if (!_movement.TryAttach(this)) { _target.ReleaseExclusive(this); Deactivate(); return; }
                transform.position = _target.transform.position + Vector3.down * 0.55f;
            }
            _nextImpulse = Time.time + 0.7f;
            StartCoroutine(Lifetime());
        }

        protected override void OnDeactivate()
        {
            _movement?.ReleaseAttachment(this, SafeReleaseVelocity(Type));
            _target?.ReleaseExclusive(this);
            _movement = null; _target = null;
        }

        private void Update()
        {
            if (!_isActive) return;
            if (_target != null)
            {
                if (_target.IsCollected || !_target.gameObject.activeInHierarchy) { Deactivate(); return; }
                transform.position = _target.transform.position + Vector3.down * 0.55f;
            }
            if (Time.time < _nextImpulse || IsHolder(Type)) return;
            _nextImpulse = Time.time + 0.8f;
            var animals = ActiveAnimalRegistry.All;
            for (int i = 0; i < animals.Count; i++)
            {
                Animal animal = animals[i];
                if (animal == null || animal.IsCollected) continue;
                if (Vector2.Distance(animal.transform.position, transform.position) > 2f) continue;
                animal.GetComponent<AnimalMovement>()?.AddImpulse(ImpulseFor(Type, animal.transform.position));
            }
        }

        public bool TryHandleTap(WorldPointerEvent pointerEvent)
        {
            if (!_isActive) return false;
            if (_movement != null) _movement.ReleaseAttachment(this, SafeReleaseVelocity(Type));
            Deactivate();
            return true;
        }

        private IEnumerator Lifetime()
        { yield return new WaitForSeconds(Mathf.Max(2f, _duration)); Deactivate(); }

        private static bool IsHolder(HindranceType t) =>
            t == HindranceType.ConveyorClouds || t == HindranceType.CrumblingPerches ||
            t == HindranceType.PendulumVines || t == HindranceType.SeesawBranch ||
            t == HindranceType.CarouselNests || t == HindranceType.TrapdoorClouds;

        private static Vector2 SafeReleaseVelocity(HindranceType t) =>
            t == HindranceType.PendulumVines ? new Vector2(1.5f, -0.5f) : new Vector2(0f, -0.8f);

        private Vector2 ImpulseFor(HindranceType t, Vector2 animalPosition)
        {
            Vector2 away = (animalPosition - (Vector2)transform.position).normalized;
            if (t == HindranceType.SpringMushroomBumpers) return new Vector2(away.x * 1.2f, 2.2f);
            if (t == HindranceType.AcornHail) return away * 0.45f;
            if (t == HindranceType.RollingLog) return new Vector2(away.x * 1.4f, -1.1f);
            return away * 0.9f;
        }
    }
}
