using System.Collections;
using UnityEngine;
using DG.Tweening;
using AnimalFall.Core.Animals;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.New
{
    /// <summary>
    /// Level 21 set piece. The frog waits above the goal card, periodically lashes
    /// out at a live animal, and pulls it off the board without advancing the goal.
    /// </summary>
    public sealed class FrogSnatcherHindrance : HindranceBase, IAnimalTapGate
    {
        [Header("Spritesheet pieces")]
        [SerializeField] private Sprite _baseSprite;
        [SerializeField] private Sprite _frogSprite;
        [SerializeField] private Sprite _tongueSprite;

        [Header("Placement")]
        [SerializeField] private Vector2 _viewportAnchor = new Vector2(0.16f, 0.15f);
        [SerializeField] private Vector2 _mouthLocalPosition = new Vector2(0.35f, 0.57f);
        [SerializeField] private float _baseScale = 0.22f;
        [SerializeField] private float _frogScale = 0.17f;

        [Header("Capture timing")]
        [SerializeField, Min(0f)] private float _firstCaptureDelay = 2.4f;
        [SerializeField, Min(0.5f)] private float _minCaptureInterval = 3.5f;
        [SerializeField, Min(0.5f)] private float _maxCaptureInterval = 5.5f;
        [SerializeField, Min(0.05f)] private float _telegraphDuration = 0.42f;
        [SerializeField, Min(0.05f)] private float _extendDuration = 0.24f;
        [SerializeField, Min(0f)] private float _holdDuration = 0.16f;
        [SerializeField, Min(0.05f)] private float _retractDuration = 0.38f;

        private SpriteRenderer _baseRenderer;
        private SpriteRenderer _frogRenderer;
        private SpriteRenderer _tongueRenderer;
        private Animal _target;
        private AnimalMovement _targetMovement;
        private bool _capturing;
        private int _capturedCount;
        private Vector3 _frogIdleScale;

        private static Sprite _runtimeTongue;

        public override HindranceType Type => HindranceType.FrogSnatcher;
        public int CapturedCount => _capturedCount;

#if UNITY_EDITOR
        public void EditorConfigure(Sprite baseSprite, Sprite frogSprite, Sprite tongueSprite)
        {
            _baseSprite = baseSprite;
            _frogSprite = frogSprite;
            _tongueSprite = tongueSprite;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        protected override void OnActivate()
        {
            EnsureVisuals();
            PositionAboveGoalPanel();
            ResetTongue();
            _capturing = false;
            _capturedCount = 0;
            _target = null;
            _targetMovement = null;

            if (_baseRenderer != null) _baseRenderer.enabled = true;
            if (_frogRenderer != null)
            {
                _frogRenderer.enabled = true;
                _frogRenderer.transform.localScale = Vector3.zero;
                _frogRenderer.transform.DOScale(_frogIdleScale, 0.36f)
                    .SetEase(Ease.OutBack)
                    .SetId(gameObject);
            }

            StartCoroutine(CaptureLoop());
        }

        protected override void OnDeactivate()
        {
            ReleaseTarget();
            ResetTongue();
            DOTween.Kill(gameObject);
        }

        private void LateUpdate()
        {
            if (!_isActive) return;
            PositionAboveGoalPanel();

            if (!_capturing && _frogRenderer != null)
            {
                float bob = Mathf.Sin(Time.time * 2.4f) * 0.025f;
                _frogRenderer.transform.localPosition = new Vector3(0.25f, 0.55f + bob, 0f);
                _frogRenderer.transform.localRotation = Quaternion.Euler(0f, 0f,
                    Mathf.Sin(Time.time * 1.8f) * 1.5f);
            }
        }

        private IEnumerator CaptureLoop()
        {
            yield return new WaitForSeconds(Mathf.Max(0f, _firstCaptureDelay));

            while (_isActive)
            {
                Animal candidate = ActiveAnimalRegistry.GetEligible();
                if (candidate == null)
                {
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }

                yield return Capture(candidate);
                if (!_isActive) yield break;

                float low = Mathf.Min(_minCaptureInterval, _maxCaptureInterval);
                float high = Mathf.Max(_minCaptureInterval, _maxCaptureInterval);
                yield return new WaitForSeconds(Random.Range(low, high));
            }
        }

        private IEnumerator Capture(Animal candidate)
        {
            if (!IsAvailable(candidate) || !candidate.TryClaimExclusive(this)) yield break;

            _target = candidate;
            _targetMovement = candidate.GetComponent<AnimalMovement>();
            _capturing = true;

            // The frog visibly crouches before firing so the loss never feels random.
            if (_frogRenderer != null)
            {
                DOTween.Kill(_frogRenderer.transform);
                _frogRenderer.transform.DOScale(
                    new Vector3(_frogIdleScale.x * 1.08f, _frogIdleScale.y * 0.86f, 1f),
                    _telegraphDuration * 0.8f).SetEase(Ease.InOutSine);
            }
            yield return new WaitForSeconds(_telegraphDuration);

            if (!IsAvailable(_target))
            {
                ReleaseTarget();
                yield break;
            }

            Vector3 mouth = GetMouthWorldPosition();
            if (_tongueRenderer != null) _tongueRenderer.enabled = true;

            float elapsed = 0f;
            while (elapsed < _extendDuration && IsAvailable(_target))
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / _extendDuration));
                Vector3 tip = Vector3.Lerp(mouth, _target.transform.position, t);
                AimTongue(mouth, tip);
                yield return null;
            }

            if (!IsAvailable(_target) || (_targetMovement != null && !_targetMovement.TryAttach(this)))
            {
                ReleaseTarget();
                yield break;
            }

            GameEvents.OnSfxRequested?.Invoke(SfxType.HindranceActivate);
            if (_target != null)
                _target.transform.DOPunchScale(Vector3.one * 0.16f, 0.18f, 5, 0.55f).SetId(gameObject);

            if (_holdDuration > 0f) yield return new WaitForSeconds(_holdDuration);

            Vector3 caughtPosition = _target.transform.position;
            elapsed = 0f;
            while (elapsed < _retractDuration && IsAvailable(_target))
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / _retractDuration));
                Vector3 pulledPosition = Vector3.Lerp(caughtPosition, mouth, t);
                _target.transform.position = pulledPosition;
                AimTongue(mouth, pulledPosition);
                yield return null;
            }

            if (IsAvailable(_target))
            {
                _target.Despawn(); // Intentionally does not count as a rescued animal.
                _capturedCount++;
            }

            ReleaseTarget();
        }

        public bool CanCollect(Animal animal) => !_capturing || animal != _target;

        public void OnBlockedTap(Animal animal)
        {
            if (animal != _target || _frogRenderer == null) return;
            _frogRenderer.transform.DOPunchScale(Vector3.one * 0.08f, 0.16f, 4, 0.5f)
                .SetId(gameObject);
        }

        private void ReleaseTarget()
        {
            if (_targetMovement != null)
                _targetMovement.ReleaseAttachment(this, Vector2.down * 0.4f);
            if (_target != null)
                _target.ReleaseExclusive(this);

            _targetMovement = null;
            _target = null;
            _capturing = false;
            ResetTongue();

            if (_frogRenderer != null)
            {
                DOTween.Kill(_frogRenderer.transform);
                _frogRenderer.transform.localScale = _frogIdleScale;
            }
        }

        private void EnsureVisuals()
        {
            _baseRenderer = EnsureRenderer("Base", 24);
            _frogRenderer = EnsureRenderer("Frog", 27);
            _tongueRenderer = EnsureRenderer("Tongue", 26);

            if (_baseRenderer != null)
            {
                _baseRenderer.sprite = _baseSprite;
                _baseRenderer.transform.localPosition = Vector3.zero;
                _baseRenderer.transform.localScale = Vector3.one * _baseScale;
            }

            if (_frogRenderer != null)
            {
                _frogRenderer.sprite = _frogSprite;
                _frogRenderer.transform.localPosition = new Vector3(0.25f, 0.55f, 0f);
                _frogIdleScale = Vector3.one * _frogScale;
                _frogRenderer.transform.localScale = _frogIdleScale;
            }

            if (_tongueRenderer != null)
            {
                _tongueRenderer.sprite = GetRuntimeTongueSprite();
                _tongueRenderer.transform.localPosition = _mouthLocalPosition;
            }
        }

        private SpriteRenderer EnsureRenderer(string childName, int sortingOrder)
        {
            Transform child = transform.Find(childName);
            if (child == null)
            {
                var go = new GameObject(childName);
                child = go.transform;
                child.SetParent(transform, false);
            }

            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = child.gameObject.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private Sprite GetRuntimeTongueSprite()
        {
            if (_runtimeTongue != null) return _runtimeTongue;
            if (_tongueSprite == null || _tongueSprite.texture == null) return _tongueSprite;

            _runtimeTongue = Sprite.Create(_tongueSprite.texture, _tongueSprite.rect,
                new Vector2(0f, 0.45f), _tongueSprite.pixelsPerUnit, 0u, SpriteMeshType.FullRect);
            _runtimeTongue.name = "frog_tongue_runtime";
            return _runtimeTongue;
        }

        private void PositionAboveGoalPanel()
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            float depth = Mathf.Abs(camera.transform.position.z - transform.position.z);
            Vector3 position = camera.ViewportToWorldPoint(
                new Vector3(_viewportAnchor.x, _viewportAnchor.y, depth));
            position.z = 0f;
            transform.position = position;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        private Vector3 GetMouthWorldPosition() => transform.TransformPoint(_mouthLocalPosition);

        private void AimTongue(Vector3 mouth, Vector3 tip)
        {
            if (_tongueRenderer == null || _tongueRenderer.sprite == null) return;

            Vector2 direction = tip - mouth;
            float distance = Mathf.Max(0.01f, direction.magnitude);
            Transform tongue = _tongueRenderer.transform;
            tongue.position = mouth;
            tongue.rotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

            float width = Mathf.Max(0.01f, _tongueRenderer.sprite.bounds.size.x);
            tongue.localScale = new Vector3(distance / width, 0.18f, 1f);
        }

        private void ResetTongue()
        {
            if (_tongueRenderer == null) return;
            _tongueRenderer.enabled = false;
            _tongueRenderer.transform.localPosition = _mouthLocalPosition;
            _tongueRenderer.transform.localRotation = Quaternion.identity;
            _tongueRenderer.transform.localScale = Vector3.zero;
        }

        private static bool IsAvailable(Animal animal) =>
            animal != null && animal.gameObject.activeInHierarchy && !animal.IsCollected;
    }
}
