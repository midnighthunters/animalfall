using System.Collections;
using UnityEngine;
using AnimalFall.Core.Animals;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.New
{
    public sealed class DynamicRiskRewardHindrance : HindranceBase, IPointerTapTarget, IAnimalTapGate
    {
        [SerializeField] private HindranceType _type;
        [SerializeField] private float _duration = 7f;
        [SerializeField] private int _requiredInteractions = 3;
        private Animal _target;
        private AnimalMovement _movement;
        private int _progress;
        private int _lastSide;
        private bool _safeWindow;

        public override HindranceType Type => _type;
        public int InteractionPriority => 240;

#if UNITY_EDITOR
        public void EditorConfigure(HindranceType type, float duration, int required)
        { _type = type; _duration = duration; _requiredInteractions = required; }
#endif

        protected override void OnActivate()
        {
            _progress = 0; _lastSide = 0;
            if (NeedsTarget(Type))
            {
                _target = _ctx.HindranceManager?.GetRandomActiveAnimal();
                if (_target == null || !_target.TryClaimExclusive(this)) { Deactivate(); return; }
                _movement = _target.GetComponent<AnimalMovement>();
                if (Type == HindranceType.VenusFlytrapRescue) _movement?.TryAttach(this);
                transform.position = _target.transform.position + Vector3.up * 0.7f;
            }
            if (Type == HindranceType.GoalSwapMonkey) TrySwapGoal();
            if (Type == HindranceType.TimerMoth) StartCoroutine(DrainTimer());
            StartCoroutine(Lifetime());
        }

        protected override void OnDeactivate()
        {
            _movement?.ReleaseAttachment(this, Vector2.down);
            _target?.ReleaseExclusive(this);
            _movement = null; _target = null;
        }

        private void Update()
        {
            if (!_isActive) return;
            _safeWindow = Mathf.Repeat(Time.time, 1.6f) > 0.75f;
            if (_target != null)
            {
                if (_target.IsCollected || !_target.gameObject.activeInHierarchy) { Deactivate(); return; }
                transform.position = _target.transform.position + Vector3.up * 0.7f;
            }
            if (_sr != null && (Type == HindranceType.BeeSwarmGuard || Type == HindranceType.PorcupinePulse))
                _sr.color = _safeWindow ? new Color(0.5f, 1f, 0.55f) : Color.white;
        }

        public bool TryHandleTap(WorldPointerEvent e)
        {
            if (!_isActive) return false;
            if (Type == HindranceType.VenusFlytrapRescue)
            {
                int side = e.WorldPosition.x < transform.position.x ? -1 : 1;
                if (_lastSide != 0 && side == _lastSide) return true;
                _lastSide = side;
            }
            _progress++;
            if (_progress >= Mathf.Max(1, _requiredInteractions)) Deactivate();
            return true;
        }

        public bool CanCollect(Animal animal)
        {
            if (animal != _target) return true;
            if (Type == HindranceType.BeeSwarmGuard || Type == HindranceType.PorcupinePulse) return _safeWindow;
            return Type != HindranceType.VenusFlytrapRescue || _progress >= Mathf.Max(1, _requiredInteractions);
        }

        public void OnBlockedTap(Animal animal) => _movement?.AddImpulse(Vector2.up * 0.3f);

        private IEnumerator DrainTimer()
        {
            yield return new WaitForSeconds(1f);
            int stolen = 0;
            while (_isActive && stolen < 4)
            {
                _ctx.GameManager?.AddTime(-1f); stolen++;
                yield return new WaitForSeconds(1.25f);
            }
        }

        private void TrySwapGoal()
        {
            GoalTracker tracker = GoalTracker.Instance;
            Spawner spawner = Spawner.Instance;
            if (tracker == null || spawner == null) return;
            tracker.TrySwapFirstUnfinishedGoal(spawner);
        }

        private IEnumerator Lifetime()
        { yield return new WaitForSeconds(Mathf.Max(2f, _duration)); Deactivate(); }

        private static bool NeedsTarget(HindranceType type) =>
            type == HindranceType.BeeSwarmGuard || type == HindranceType.PorcupinePulse ||
            type == HindranceType.VenusFlytrapRescue;
    }
}
