using System.Collections;
using UnityEngine;
using AnimalFall.Core.Animals;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.EnvironmentMods
{
    /// <summary>A tapped pufferfish instantly deflates and darts around very fast in random
    /// directions like an untied balloon, ejecting animals it hits.</summary>
    public sealed class PufferfishHindrance : HindranceBase, IPointerTapTarget
    {
        [SerializeField, Min(1f)] private float _visibleLifetime = 8.5f;
        [SerializeField, Range(0.1f, 1f)] private float _viewportHeight = 0.92f;
        [SerializeField, Min(0.1f)] private float _fallSpeed = 1.25f;
        [SerializeField, Min(0f)] private float _horizontalDrift = 0.18f;
        [SerializeField, Range(0.1f, 1f)] private float _deflatedScaleMultiplier = 0.55f;
        [SerializeField, Range(0.1f, 1f)] private float _deflatedSquish = 0.6f;
        [SerializeField, Min(0.02f)] private float _deflateDuration = 0.1f;
        [SerializeField, Min(0.25f)] private float _rushDuration = 1.65f;
        [SerializeField, Min(0.5f)] private float _rushSpeed = 13f;
        [SerializeField, Min(0.02f)] private float _directionChangeInterval = 0.12f;

        private bool _launched;
        private Vector3 _baseScale;
        private Vector2 _direction;
        private float _spawnX;
        private float _driftPhase;
        private CircleCollider2D _hitCollider;

        public override HindranceType Type => HindranceType.Pufferfish;
        public int InteractionPriority => 220;

        protected override void OnActivate()
        {
            _launched = false;
            NormalizeToAnimalSize();
            MoveIntoPlayArea();
            _spawnX = transform.position.x;
            _driftPhase = Random.Range(0f, Mathf.PI * 2f);
            StartCoroutine(FallUntilRetired());
        }

        public bool TryHandleTap(WorldPointerEvent pointerEvent)
        {
            if (!_isActive || _launched) return false;
            _launched = true;
            StartCoroutine(DeflateAndDart());
            return true;
        }

        protected override void OnDeactivate() { }

        private IEnumerator FallUntilRetired()
        {
            float elapsed = 0f;
            while (_isActive && !_launched && elapsed < _visibleLifetime)
            {
                elapsed += Time.deltaTime;
                Vector3 position = transform.position;
                position.y -= _fallSpeed * Time.deltaTime;
                position.x = _spawnX + Mathf.Sin(elapsed * 2.2f + _driftPhase) * _horizontalDrift;
                transform.position = position;

                Camera camera = Camera.main;
                if (camera != null && camera.WorldToViewportPoint(position).y < -0.08f)
                {
                    Deactivate();
                    yield break;
                }

                yield return null;
            }

            if (_isActive && !_launched) Deactivate();
        }

        private IEnumerator DeflateAndDart()
        {
            GameEvents.OnSfxRequested?.Invoke(SfxType.HindranceActivate);

            // Quick squash to a flattened, deflated silhouette.
            Vector3 deflatedScale = new Vector3(
                _baseScale.x * _deflatedScaleMultiplier,
                _baseScale.y * _deflatedScaleMultiplier * _deflatedSquish,
                _baseScale.z);

            float elapsed = 0f;
            while (_isActive && elapsed < _deflateDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / _deflateDuration));
                transform.localScale = Vector3.LerpUnclamped(_baseScale, deflatedScale, t);
                yield return null;
            }

            transform.localScale = deflatedScale;
            _direction = RandomDirection();
            float directionTimer = _directionChangeInterval;
            elapsed = 0f;
            while (_isActive && elapsed < _rushDuration)
            {
                elapsed += Time.deltaTime;
                directionTimer -= Time.deltaTime;
                if (directionTimer <= 0f)
                {
                    // Abrupt, fully random turns for erratic balloon-like darting.
                    _direction = RandomDirection();
                    directionTimer = _directionChangeInterval;
                }

                transform.position += (Vector3)(_direction * _rushSpeed * Time.deltaTime);

                // Spin fast and wobble the deflated body to sell escaping air.
                transform.Rotate(0f, 0f, _rushSpeed * 40f * Time.deltaTime);
                float wobble = 1f + Mathf.Sin(elapsed * 38f) * 0.12f;
                transform.localScale = new Vector3(
                    deflatedScale.x * wobble,
                    deflatedScale.y / wobble,
                    deflatedScale.z);

                HitAnimals();
                yield return null;
            }
            Deactivate();
        }

        private static Vector2 RandomDirection()
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            return direction.sqrMagnitude < 0.01f ? Vector2.right : direction;
        }

        private void HitAnimals()
        {
            var animals = ActiveAnimalRegistry.All;
            for (int i = animals.Count - 1; i >= 0; i--)
            {
                Animal animal = animals[i];
                if (animal == null || animal.IsCollected) continue;
                if (Vector2.Distance(animal.transform.position, transform.position) > 1.25f) continue;
                animal.GetComponent<AnimalMovement>()?.LaunchOutOfScreen(_direction);
            }
        }
        private void MoveIntoPlayArea()
        {
            Camera camera = Camera.main;
            if (camera == null) return;

            float depth = Mathf.Abs(camera.transform.position.z - transform.position.z);
            Vector3 viewportPosition = camera.WorldToViewportPoint(transform.position);
            viewportPosition.x = Mathf.Clamp(viewportPosition.x, 0.12f, 0.88f);
            viewportPosition.y = _viewportHeight;
            viewportPosition.z = depth;
            Vector3 worldPosition = camera.ViewportToWorldPoint(viewportPosition);
            worldPosition.z = 0f;
            transform.position = worldPosition;
        }

        private void NormalizeToAnimalSize()
        {
            if (_sr == null || _sr.sprite == null)
            {
                _baseScale = Vector3.one;
                return;
            }

            Vector2 size = _sr.sprite.bounds.size;
            float largest = Mathf.Max(size.x, size.y);
            _baseScale = largest > 0.001f
                ? Vector3.one * (Animal.TargetWorldSize / largest)
                : Vector3.one;
            transform.localScale = _baseScale;

            _hitCollider = GetComponent<CircleCollider2D>();
            if (_hitCollider == null) _hitCollider = gameObject.AddComponent<CircleCollider2D>();
            _hitCollider.isTrigger = true;
            _hitCollider.enabled = true;
            _hitCollider.offset = _sr.sprite.bounds.center;
            _hitCollider.radius = largest * 0.58f;
        }
    }
}
