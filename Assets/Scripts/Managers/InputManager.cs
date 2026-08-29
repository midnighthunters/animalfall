// Task 6.6 — InputManager: touch/mouse input, GestureDetector, magnet/mirror support
using UnityEngine;
using AnimalFall.Core.Animals;
using AnimalFall.Core.Hindrances;
using AnimalFall.Utils;
using System.Collections.Generic;

namespace AnimalFall.Managers
{
    public class InputManager : MonoBehaviour
    {
        public Vector2 MagnetOffset { get; private set; }
        private bool   _mirrorMode;
        private readonly Dictionary<object, Vector2> _magnetOwners = new Dictionary<object, Vector2>();
        private readonly HashSet<object> _mirrorOwners = new HashSet<object>();

        // Touch state
        private Vector2 _touchStart;
        private float   _touchStartTime;
        private Animal  _pendingAnimal;
        private IPointerGestureTarget _gestureTarget;
        private bool    _inputBlocked;
        private readonly Collider2D[] _hits = new Collider2D[16];
        private ContactFilter2D _contactFilter;
        private bool _contactFilterConfigured;

        private void Awake()
        {
            EnsureContactFilter();
        }

        private void EnsureContactFilter()
        {
            if (_contactFilterConfigured) return;
            _contactFilter = new ContactFilter2D { useTriggers = true };
            _contactFilter.SetLayerMask(Physics2D.AllLayers);
            _contactFilterConfigured = true;
        }

        public void SetMagnetOffset(Vector2 offset) => MagnetOffset = offset;
        public void SetMirrorMode(bool on)          => _mirrorMode = on;
        public HindranceEffectToken AddMagnetOffset(object owner, Vector2 offset)
        {
            _magnetOwners[owner] = offset; RecalculateMagnet();
            return new HindranceEffectToken(() => { _magnetOwners.Remove(owner); RecalculateMagnet(); });
        }

        public HindranceEffectToken AddMirror(object owner)
        {
            _mirrorOwners.Add(owner); _mirrorMode = true;
            return new HindranceEffectToken(() => { _mirrorOwners.Remove(owner); _mirrorMode = _mirrorOwners.Count > 0; });
        }

        private void RecalculateMagnet()
        {
            Vector2 sum = Vector2.zero;
            foreach (Vector2 offset in _magnetOwners.Values) sum += offset;
            MagnetOffset = Vector2.ClampMagnitude(sum, 1f);
        }
        public void BlockInput(bool block)          => _inputBlocked = block;

        private void Update()
        {
            if (_inputBlocked) return;

#if UNITY_EDITOR
            // Mouse fallback for editor
            if (Input.GetMouseButtonDown(0))
            {
                _touchStart     = Input.mousePosition;
                _touchStartTime = Time.time;
                _pendingAnimal  = GetAnimalAtScreenPos(Input.mousePosition);
                _gestureTarget = GetBestGestureTarget(Input.mousePosition);
                _gestureTarget?.OnPointerDown(BuildEvent(Input.mousePosition, Vector2.zero, 0f));
            }
            if (Input.GetMouseButton(0) && _gestureTarget != null)
                _gestureTarget.OnPointerMove(BuildEvent(Input.mousePosition, (Vector2)Input.mousePosition - _touchStart, Time.time - _touchStartTime));
            if (Input.GetMouseButtonUp(0))
            {
                ProcessEnd(Input.mousePosition, Time.time - _touchStartTime);
            }
#else
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began)
                {
                    _touchStart     = t.position;
                    _touchStartTime = Time.time;
                    _pendingAnimal  = GetAnimalAtScreenPos(t.position);
                    _gestureTarget = GetBestGestureTarget(t.position);
                    _gestureTarget?.OnPointerDown(BuildEvent(t.position, Vector2.zero, 0f));
                }
                else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                    _gestureTarget?.OnPointerMove(BuildEvent(t.position, t.position - _touchStart, Time.time - _touchStartTime));
                else if (t.phase == TouchPhase.Ended)
                {
                    ProcessEnd(t.position, Time.time - _touchStartTime);
                }
                else if (t.phase == TouchPhase.Canceled)
                    CancelPointer(t.position);
            }
#endif
        }

        private void ProcessEnd(Vector2 screenEndPos, float duration)
        {
            WorldPointerEvent pointerEvent = BuildEvent(screenEndPos, screenEndPos - _touchStart, duration);
            if (GestureDetector.IsSwipe(_touchStart, screenEndPos, duration, out Vector2 swipeDelta))
            {
                _gestureTarget?.OnPointerUp(pointerEvent, false);
                GameEvents.OnSwipeDetected?.Invoke(swipeDelta);
                GameEvents.OnSwipeDetailed?.Invoke(_touchStart, screenEndPos);
                ClearPointer();
                return; // swipe — don't process as tap
            }

            // Tap
            Vector2 worldPos = GetWorldPos(screenEndPos);
            GameEvents.OnScreenTapped?.Invoke(worldPos);

            IPointerTapTarget worldTarget = GetBestTapTarget(screenEndPos);
            if (worldTarget != null && worldTarget.TryHandleTap(pointerEvent))
            {
                _gestureTarget?.OnPointerUp(pointerEvent, false);
                ClearPointer();
                return;
            }

            // Use the object that was under the initial press whenever possible.
            // Falling animals can move between pointer-down and pointer-up, so fall
            // back to the release position rather than silently discarding the tap.
            Animal animal = _pendingAnimal;
            if (animal == null || animal.Data == null || animal.IsCollected)
                animal = GetAnimalAtScreenPos(screenEndPos);

            if (animal != null && animal.Data != null && !animal.IsCollected)
            {
                var result = animal.HandleTap();
                HandleTapResult(result, animal);
            }
            _gestureTarget?.OnPointerUp(pointerEvent, false);
            ClearPointer();
        }

        public void DispatchSyntheticWorldTap(Vector2 worldPosition)
        {
            if (_inputBlocked || Camera.main == null) return;
            Vector2 screen = Camera.main.WorldToScreenPoint(worldPosition);
            IPointerTapTarget target = GetBestTapTarget(screen);
            if (target != null && target.TryHandleTap(new WorldPointerEvent(screen, worldPosition, Vector2.zero, 0f, true)))
                return;

            // Synthetic taps are used by interaction rules and automated checks.
            // They must follow the same animal-collection route as a real tap.
            Animal animal = GetAnimalAtScreenPos(screen);
            if (animal != null && animal.Data != null && !animal.IsCollected)
                HandleTapResult(animal.HandleTap(), animal);
        }

        private WorldPointerEvent BuildEvent(Vector2 screen, Vector2 delta, float duration)
            => new WorldPointerEvent(screen, GetWorldPos(screen), delta, duration);

        private void CancelPointer(Vector2 screen)
        {
            _gestureTarget?.OnPointerUp(BuildEvent(screen, screen - _touchStart, Time.time - _touchStartTime), true);
            ClearPointer();
        }

        private void ClearPointer()
        {
            _pendingAnimal = null;
            _gestureTarget = null;
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused && _gestureTarget != null) CancelPointer(_touchStart);
        }

        private IPointerTapTarget GetBestTapTarget(Vector2 screenPos)
        {
            int count = GetHits(screenPos);
            IPointerTapTarget best = null;
            int bestPriority = int.MinValue;
            int bestSorting = int.MinValue;
            for (int i = 0; i < count; i++)
            {
                Collider2D hit = _hits[i];
                IPointerTapTarget target = hit != null ? hit.GetComponent<IPointerTapTarget>() : null;
                if (target == null && hit != null) target = hit.GetComponentInParent<IPointerTapTarget>();
                if (target == null) continue;
                int sorting = hit.GetComponent<SpriteRenderer>()?.sortingOrder ?? 0;
                if (target.InteractionPriority > bestPriority ||
                    (target.InteractionPriority == bestPriority && sorting > bestSorting))
                { best = target; bestPriority = target.InteractionPriority; bestSorting = sorting; }
            }
            return best;
        }

        private IPointerGestureTarget GetBestGestureTarget(Vector2 screenPos)
        {
            int count = GetHits(screenPos);
            IPointerGestureTarget best = null;
            int priority = int.MinValue;
            for (int i = 0; i < count; i++)
            {
                Collider2D hit = _hits[i];
                IPointerGestureTarget target = hit != null ? hit.GetComponent<IPointerGestureTarget>() : null;
                if (target == null && hit != null) target = hit.GetComponentInParent<IPointerGestureTarget>();
                if (target != null && target.InteractionPriority > priority)
                { best = target; priority = target.InteractionPriority; }
            }
            return best;
        }

        private int GetHits(Vector2 screenPos)
        {
            if (Camera.main == null) return 0;
            EnsureContactFilter();
            return Physics2D.OverlapPoint(GetWorldPos(screenPos), _contactFilter, _hits);
        }

        private void HandleTapResult(TapResult result, Animal animal)
        {
            // Correct/Golden/Rainbow already call OnCollected inside HandleTap.
            switch (result)
            {
                case TapResult.Wrong:
                case TapResult.FakeCollected:
                    GameEvents.OnWrongTap?.Invoke();
                    break;
                case TapResult.BubblePopped:
                case TapResult.Correct:
                case TapResult.Golden:
                case TapResult.Rainbow:
                case TapResult.ShieldBroken:
                case TapResult.PairedWaiting:
                case TapResult.IceCubeFrozen:
                case TapResult.HindranceBlocked:
                    break;
            }
        }

        private Animal GetAnimalAtScreenPos(Vector2 screenPos)
        {
            if (Camera.main == null) { Debug.LogWarning("[InputManager] Camera.main is null."); return null; }
            Vector2 worldPos = GetWorldPos(screenPos);
            int count = Physics2D.OverlapPoint(worldPos, _contactFilter, _hits);
            Animal best = null;
            int bestOrder = int.MinValue;
            for (int i = 0; i < count; i++)
            {
                Animal animal = _hits[i] != null ? _hits[i].GetComponent<Animal>() : null;
                if (animal == null && _hits[i] != null) animal = _hits[i].GetComponentInParent<Animal>();
                if (animal == null || animal.Data == null || animal.IsCollected) continue;
                SpriteRenderer renderer = _hits[i].GetComponent<SpriteRenderer>()
                    ?? _hits[i].GetComponentInParent<SpriteRenderer>();
                int order = renderer != null ? renderer.sortingOrder : 0;
                if (order > bestOrder) { best = animal; bestOrder = order; }
            }
            return best;
        }

        private Vector2 GetWorldPos(Vector2 screenPos)
        {
            if (Camera.main == null) return screenPos;
            Vector3 wp = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Mathf.Abs(Camera.main.transform.position.z)));
            var result = new Vector2(wp.x, wp.y);
            if (_mirrorMode) result.x = -result.x;
            result += MagnetOffset;
            return result;
        }
    }
}
