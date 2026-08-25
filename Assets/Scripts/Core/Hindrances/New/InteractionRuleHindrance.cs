using System.Collections;
using UnityEngine;
using AnimalFall.Core.Animals;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.New
{
    public sealed class InteractionRuleHindrance : HindranceBase, IPointerTapTarget,
        IPointerGestureTarget, IAnimalTapGate
    {
        [SerializeField] private HindranceType _type;
        [SerializeField] private float _duration = 7f;
        [SerializeField] private int _requiredInteractions = 1;

        private Animal _target;
        private AnimalMovement _movement;
        private int _progress;
        private float _gestureDistance;
        private Vector2 _gestureStart;
        private bool _gestureActive;
        private bool _greenState;
        private int _echoesRemaining;

        public override HindranceType Type => _type;
        public int InteractionPriority => 200;

#if UNITY_EDITOR
        public void EditorConfigure(HindranceType type, float duration, int required)
        { _type = type; _duration = duration; _requiredInteractions = required; }
#endif

        protected override void OnActivate()
        {
            _progress = 0;
            _gestureDistance = 0f;
            _echoesRemaining = Type == HindranceType.EchoTapRune ? 3 : 0;
            if (NeedsTarget(Type))
            {
                _target = _ctx.HindranceManager?.GetRandomActiveAnimal();
                if (_target == null || !_target.TryClaimExclusive(this)) { Deactivate(); return; }
                _movement = _target.GetComponent<AnimalMovement>();
                if (IsHolder(Type)) _movement?.TryAttach(this);
                transform.position = _target.transform.position + Vector3.up * 0.65f;
            }
            StartCoroutine(Lifetime());
        }

        protected override void OnDeactivate()
        {
            if (_movement != null) _movement.ReleaseAttachment(this, Vector2.down * 0.5f);
            if (_target != null) _target.ReleaseExclusive(this);
            _target = null;
            _movement = null;
        }

        private void Update()
        {
            if (!_isActive) return;
            float phase = Mathf.Repeat(Time.time, 1.35f) / 1.35f;
            _greenState = phase > 0.35f && phase < 0.72f;
            if (_target != null)
            {
                if (_target.IsCollected || !_target.gameObject.activeInHierarchy) { Deactivate(); return; }
                transform.position = _target.transform.position + Vector3.up * 0.65f;
            }
            if (_sr != null && UsesTimingGate(Type))
                _sr.color = _greenState ? new Color(0.45f, 1f, 0.55f) : new Color(1f, 0.45f, 0.35f);
        }

        public bool TryHandleTap(WorldPointerEvent pointerEvent)
        {
            if (!_isActive) return false;
            if (Type == HindranceType.EchoTapRune && !pointerEvent.IsSynthetic)
            {
                StartCoroutine(Echo(pointerEvent.WorldPosition));
                if (--_echoesRemaining <= 0) Complete();
                return true;
            }
            _progress++;
            if (_progress >= Mathf.Max(1, _requiredInteractions)) Complete();
            else Pulse();
            return true;
        }

        public bool CanCollect(Animal animal)
        {
            if (animal != _target) return true;
            if (Type == HindranceType.MovingSafeHalo)
                return Vector2.Distance(animal.transform.position, transform.position) < 1.25f;
            if (UsesTimingGate(Type)) return _greenState;
            return !IsHolder(Type) && _progress >= Mathf.Max(1, _requiredInteractions);
        }

        public void OnBlockedTap(Animal animal)
        {
            _movement?.AddImpulse(Vector2.up * 0.35f);
            Pulse();
        }

        public void OnPointerDown(WorldPointerEvent pointerEvent)
        { _gestureActive = true; _gestureStart = pointerEvent.ScreenPosition; _gestureDistance = 0f; }

        public void OnPointerMove(WorldPointerEvent pointerEvent)
        { if (_gestureActive) _gestureDistance = Mathf.Max(_gestureDistance, pointerEvent.ScreenDelta.magnitude); }

        public void OnPointerUp(WorldPointerEvent pointerEvent, bool canceled)
        {
            if (!_gestureActive) return;
            _gestureActive = false;
            if (canceled) return;
            float closure = Vector2.Distance(pointerEvent.ScreenPosition, _gestureStart);
            bool valid = Type == HindranceType.LassoRing
                ? _gestureDistance >= 120f && closure <= 90f
                : _gestureDistance >= 80f;
            if (valid) Complete(); else Pulse();
        }

        private IEnumerator Echo(Vector2 worldPosition)
        { yield return new WaitForSeconds(0.45f); _ctx.InputManager?.DispatchSyntheticWorldTap(worldPosition); }

        private IEnumerator Lifetime()
        { yield return new WaitForSeconds(Mathf.Max(2f, _duration)); Deactivate(); }

        private void Complete()
        {
            if (_target != null && Type != HindranceType.TrafficLightOwl &&
                Type != HindranceType.RhythmTotem && Type != HindranceType.MovingSafeHalo)
            {
                _movement?.ReleaseAttachment(this, Vector2.down * 0.6f);
                _target.ReleaseExclusive(this);
            }
            GameEvents.OnSfxRequested?.Invoke(SfxType.Collect);
            Deactivate();
        }

        private void Pulse()
        {
            if (_sr != null) StartCoroutine(PulseRoutine());
        }

        private IEnumerator PulseRoutine()
        {
            Vector3 start = transform.localScale;
            transform.localScale = start * 1.15f;
            yield return new WaitForSeconds(0.08f);
            transform.localScale = start;
        }

        private static bool NeedsTarget(HindranceType type) =>
            type != HindranceType.EchoTapRune && type != HindranceType.KeepersWhistle;

        private static bool IsHolder(HindranceType type) =>
            type == HindranceType.SpiderwebCurtain || type == HindranceType.TrackingRescueCage;

        private static bool UsesTimingGate(HindranceType type) =>
            type == HindranceType.RhythmTotem || type == HindranceType.TrafficLightOwl;
    }
}
