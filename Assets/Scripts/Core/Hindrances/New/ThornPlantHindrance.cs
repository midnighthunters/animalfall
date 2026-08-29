using System.Collections;
using UnityEngine;
using AnimalFall.Core.Animals;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.New
{
    /// <summary>
    /// A frog-style periodic snatcher assembled from base, stem, and flower sprites.
    /// The stem extends to a target, the open flower catches it, then retracts it.
    /// </summary>
    public sealed class ThornPlantHindrance : HindranceBase, IAnimalTapGate
    {
        [Header("Sprite pieces")]
        [SerializeField] private Sprite _baseSprite;
        [SerializeField] private Sprite _closedMouthSprite;
        [SerializeField] private Sprite _openMouthSprite;
        [SerializeField] private Sprite _stemSprite;

        [Header("Placement")]
        [SerializeField] private Vector2 _viewportAnchor = new Vector2(0.82f, 0.18f);
        [SerializeField] private float _baseScale = 0.19f;
        [SerializeField] private float _stemWidthScale = 0.13f;
        [SerializeField] private float _flowerScale = 0.17f;
        [SerializeField] private float _idleStemLength = 0.72f;

        [Header("Attack timing")]
        [SerializeField, Min(0f)] private float _firstAttackDelay = 2.2f;
        [SerializeField, Min(0.5f)] private float _minAttackInterval = 3.2f;
        [SerializeField, Min(0.5f)] private float _maxAttackInterval = 4.8f;
        [SerializeField, Min(0.05f)] private float _telegraphDuration = 0.4f;
        [SerializeField, Min(0.05f)] private float _extendDuration = 0.28f;
        [SerializeField, Min(0f)] private float _biteDuration = 0.14f;
        [SerializeField, Min(0.05f)] private float _retractDuration = 0.4f;

        private SpriteRenderer _baseRenderer;
        private SpriteRenderer _stemRenderer;
        private SpriteRenderer _flowerRenderer;
        private Animal _target;
        private AnimalMovement _targetMovement;
        private bool _attacking;
        private int _capturedCount;

        public override HindranceType Type => HindranceType.VenusFlytrapRescue;
        public int CapturedCount => _capturedCount;

#if UNITY_EDITOR
        public void EditorConfigure(Sprite baseSprite, Sprite closedMouthSprite,
            Sprite openMouthSprite, Sprite stemSprite)
        {
            _baseSprite = baseSprite;
            _closedMouthSprite = closedMouthSprite;
            _openMouthSprite = openMouthSprite;
            _stemSprite = stemSprite;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        protected override void OnActivate()
        {
            EnsureVisuals();
            PositionAtAnchor();
            _attacking = false;
            _capturedCount = 0;
            _target = null;
            _targetMovement = null;
            ResetPlant();
            StartCoroutine(AttackLoop());
        }

        protected override void OnDeactivate()
        {
            ReleaseTarget();
            ResetPlant();
        }

        private void LateUpdate()
        {
            if (!_isActive) return;
            PositionAtAnchor();
            if (!_attacking && _flowerRenderer != null)
            {
                float bob = Mathf.Sin(Time.time * 2.2f) * 0.025f;
                Vector3 rest = GetStemOrigin() + Vector3.up * (_idleStemLength + bob);
                SetStem(GetStemOrigin(), rest);
                _flowerRenderer.transform.position = rest;
            }
        }

        private IEnumerator AttackLoop()
        {
            yield return new WaitForSeconds(Mathf.Max(0f, _firstAttackDelay));
            while (_isActive)
            {
                Animal candidate = ActiveAnimalRegistry.GetEligible();
                if (candidate == null)
                {
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }

                yield return Attack(candidate);
                if (!_isActive) yield break;

                float low = Mathf.Min(_minAttackInterval, _maxAttackInterval);
                float high = Mathf.Max(_minAttackInterval, _maxAttackInterval);
                yield return new WaitForSeconds(Random.Range(low, high));
            }
        }

        private IEnumerator Attack(Animal candidate)
        {
            if (!IsAvailable(candidate) || !candidate.TryClaimExclusive(this)) yield break;

            _target = candidate;
            _targetMovement = candidate.GetComponent<AnimalMovement>();
            _attacking = true;

            if (_flowerRenderer != null)
            {
                _flowerRenderer.sprite = _openMouthSprite != null ? _openMouthSprite : _closedMouthSprite;
                _flowerRenderer.transform.localScale =
                    new Vector3(_flowerScale * 1.12f, _flowerScale * 0.9f, 1f);
            }
            yield return new WaitForSeconds(_telegraphDuration);

            if (!IsAvailable(_target))
            {
                ReleaseTarget();
                yield break;
            }

            Vector3 origin = GetStemOrigin();
            Vector3 restTip = origin + Vector3.up * _idleStemLength;
            float elapsed = 0f;
            while (elapsed < _extendDuration && IsAvailable(_target))
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / _extendDuration));
                Vector3 tip = Vector3.Lerp(restTip, _target.transform.position, t);
                SetStem(origin, tip);
                if (_flowerRenderer != null) _flowerRenderer.transform.position = tip;
                yield return null;
            }

            if (!IsAvailable(_target) || _targetMovement == null || !_targetMovement.TryAttach(this))
            {
                ReleaseTarget();
                yield break;
            }

            GameEvents.OnSfxRequested?.Invoke(SfxType.HindranceActivate);
            if (_flowerRenderer != null)
            {
                _flowerRenderer.sprite = _closedMouthSprite != null ? _closedMouthSprite : _openMouthSprite;
                _flowerRenderer.transform.localScale = Vector3.one * _flowerScale;
            }
            if (_biteDuration > 0f) yield return new WaitForSeconds(_biteDuration);

            Vector3 caughtPosition = _target.transform.position;
            elapsed = 0f;
            while (elapsed < _retractDuration && IsAvailable(_target))
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / _retractDuration));
                Vector3 tip = Vector3.Lerp(caughtPosition, restTip, t);
                _target.transform.position = tip;
                SetStem(origin, tip);
                if (_flowerRenderer != null) _flowerRenderer.transform.position = tip;
                yield return null;
            }

            if (IsAvailable(_target))
            {
                _target.Despawn();
                _capturedCount++;
            }

            ReleaseTarget();
        }

        public bool CanCollect(Animal animal) => !_attacking || animal != _target;

        public void OnBlockedTap(Animal animal)
        {
            if (animal != _target || _flowerRenderer == null) return;
            _flowerRenderer.transform.localScale =
                new Vector3(_flowerScale * 1.08f, _flowerScale * 0.92f, 1f);
        }

        private void ReleaseTarget()
        {
            if (_targetMovement != null)
                _targetMovement.ReleaseAttachment(this, Vector2.down * 0.35f);
            if (_target != null)
                _target.ReleaseExclusive(this);

            _targetMovement = null;
            _target = null;
            _attacking = false;
            ResetPlant();
        }

        private void EnsureVisuals()
        {
            _baseRenderer = EnsureRenderer("Base", 24);
            _stemRenderer = EnsureRenderer("Stem", 25);
            _flowerRenderer = EnsureRenderer("Flower", 26);

            if (_baseRenderer != null) _baseRenderer.sprite = _baseSprite;
            if (_stemRenderer != null) _stemRenderer.sprite = _stemSprite;
            if (_flowerRenderer != null) _flowerRenderer.sprite = _closedMouthSprite;
        }

        private SpriteRenderer EnsureRenderer(string childName, int sortingOrder)
        {
            Transform child = transform.Find(childName);
            if (child == null)
            {
                GameObject created = new GameObject(childName);
                child = created.transform;
                child.SetParent(transform, false);
            }

            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = child.gameObject.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private void ResetPlant()
        {
            if (_baseRenderer != null)
            {
                _baseRenderer.sprite = _baseSprite;
                _baseRenderer.transform.localScale = Vector3.one * _baseScale;
                _baseRenderer.transform.localPosition = Vector3.zero;
                _baseRenderer.enabled = true;
            }

            Vector3 origin = GetStemOrigin();
            Vector3 restTip = origin + Vector3.up * _idleStemLength;
            if (_stemRenderer != null)
            {
                _stemRenderer.sprite = _stemSprite;
                _stemRenderer.enabled = true;
            }
            SetStem(origin, restTip);

            if (_flowerRenderer != null)
            {
                _flowerRenderer.sprite = _closedMouthSprite != null ? _closedMouthSprite : _openMouthSprite;
                _flowerRenderer.transform.position = restTip;
                _flowerRenderer.transform.localScale = Vector3.one * _flowerScale;
                _flowerRenderer.transform.rotation = Quaternion.identity;
                _flowerRenderer.enabled = true;
            }
        }

        private void SetStem(Vector3 from, Vector3 to)
        {
            if (_stemRenderer == null || _stemRenderer.sprite == null) return;
            Vector3 delta = to - from;
            float length = Mathf.Max(0.05f, delta.magnitude);
            _stemRenderer.transform.position = (from + to) * 0.5f;
            _stemRenderer.transform.rotation = Quaternion.Euler(
                0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f);
            float spriteHeight = Mathf.Max(0.01f, _stemRenderer.sprite.bounds.size.y);
            _stemRenderer.transform.localScale =
                new Vector3(_stemWidthScale, length / spriteHeight, 1f);
        }

        private Vector3 GetStemOrigin()
        {
            return transform.position + Vector3.up * 0.18f;
        }

        private void PositionAtAnchor()
        {
            if (Camera.main == null) return;
            float z = Mathf.Abs(Camera.main.transform.position.z);
            transform.position = Camera.main.ViewportToWorldPoint(
                new Vector3(_viewportAnchor.x, _viewportAnchor.y, z));
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        private static bool IsAvailable(Animal animal)
        {
            return animal != null && animal.gameObject.activeInHierarchy && !animal.IsCollected;
        }
    }
}