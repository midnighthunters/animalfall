using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnimalFall.Core.Animals;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.EnvironmentMods
{
    /// <summary>
    /// Sends a staggered wave of colourful balloons upward from below the
    /// playfield. Each balloon searches for an eligible animal, attaches to it,
    /// and gives it upward lift without counting it as a missed animal.
    /// </summary>
    public sealed class BalloonWaveHindrance : HindranceBase
    {
        [SerializeField] private Sprite[] _balloonSprites;
        [SerializeField, Min(1f)] private float _activeDuration = 8f;
        [SerializeField, Min(0.1f)] private float _spawnInterval = 0.34f;
        [SerializeField, Min(0.1f)] private float _riseSpeed = 3.1f;
        [SerializeField, Min(0.1f)] private float _upwardLift = 3.4f;
        [SerializeField, Min(0.1f)] private float _balloonWorldSize = 0.78f;
        [SerializeField, Min(1)] private int _balloonsPerWave = 10;

        private readonly List<BalloonCarrier> _liveBalloons = new List<BalloonCarrier>(12);
        private readonly HashSet<Animal> _claimedAnimals = new HashSet<Animal>();
        private int _nextSprite;

        public override HindranceType Type => HindranceType.BalloonWave;

#if UNITY_EDITOR
        public void EditorConfigure(Sprite[] balloonSprites)
        {
            _balloonSprites = balloonSprites;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        protected override void OnActivate()
        {
            if (_sr != null) _sr.enabled = false;
            _nextSprite = 0;
            _claimedAnimals.Clear();
            _liveBalloons.Clear();
            StartCoroutine(WaveRoutine());
        }

        protected override void OnDeactivate()
        {
            for (int i = _liveBalloons.Count - 1; i >= 0; i--)
            {
                BalloonCarrier balloon = _liveBalloons[i];
                if (balloon == null) continue;
                balloon.Detach();
                Destroy(balloon.gameObject);
            }
            _liveBalloons.Clear();
            _claimedAnimals.Clear();
        }

        private IEnumerator WaveRoutine()
        {
            float finishAt = Time.time + _activeDuration;
            int spawned = 0;
            while (_isActive && Time.time < finishAt && spawned < _balloonsPerWave)
            {
                SpawnBalloon(spawned++);
                yield return new WaitForSeconds(_spawnInterval);
            }

            while (_isActive && Time.time < finishAt)
                yield return null;
            if (_isActive) Deactivate();
        }

        private void SpawnBalloon(int sequence)
        {
            Sprite sprite = GetNextBalloonSprite();
            GameObject balloonObject = new GameObject($"BalloonWave_Balloon_{sequence + 1}");
            balloonObject.transform.position = GetWaveStartPosition(sequence);
            var renderer = balloonObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 36;
            float largest = sprite != null ? Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y) : 1f;
            balloonObject.transform.localScale = Vector3.one * (_balloonWorldSize / Mathf.Max(0.001f, largest));

            var carrier = balloonObject.AddComponent<BalloonCarrier>();
            carrier.Configure(this, sprite, _riseSpeed, _upwardLift);
            _liveBalloons.Add(carrier);
        }

        internal Animal FindEligibleAnimal(Vector3 fromPosition)
        {
            Animal best = null;
            float bestDistance = float.MaxValue;
            var animals = ActiveAnimalRegistry.All;
            for (int i = 0; i < animals.Count; i++)
            {
                Animal animal = animals[i];
                if (animal == null || animal.IsCollected || _claimedAnimals.Contains(animal)) continue;
                float distance = Mathf.Abs(animal.transform.position.x - fromPosition.x);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = animal;
                }
            }
            return best;
        }

        internal bool ClaimAnimal(Animal animal)
        {
            return animal != null && !_claimedAnimals.Contains(animal) && _claimedAnimals.Add(animal);
        }

        internal void RemoveBalloon(BalloonCarrier balloon)
        {
            _liveBalloons.Remove(balloon);
        }

        private Sprite GetNextBalloonSprite()
        {
            if (_balloonSprites == null || _balloonSprites.Length == 0)
            {
                Sprite[] loaded = Resources.LoadAll<Sprite>("icons/hindrances/balloon");
                if (loaded.Length > 0) _balloonSprites = loaded;
            }
            if (_balloonSprites == null || _balloonSprites.Length == 0) return null;
            Sprite result = _balloonSprites[_nextSprite % _balloonSprites.Length];
            _nextSprite++;
            return result;
        }

        private Vector3 GetWaveStartPosition(int sequence)
        {
            Camera camera = Camera.main;
            if (camera == null) return new Vector3((sequence % 5 - 2) * 1.25f, -5.5f, 0f);
            float depth = Mathf.Abs(camera.transform.position.z);
            Vector3 start = camera.ViewportToWorldPoint(new Vector3(
                Mathf.Lerp(0.12f, 0.88f, (sequence % 7) / 6f), 0f, depth));
            start.y -= 1.0f;
            start.z = 0f;
            return start;
        }
    }

    public sealed class BalloonCarrier : MonoBehaviour
    {
        private BalloonWaveHindrance _owner;
        private Animal _target;
        private AnimalMovement _movement;
        private float _riseSpeed;
        private float _upwardLift;
        private bool _attached;
        private float _phase;

        public void Configure(BalloonWaveHindrance owner, Sprite sprite, float riseSpeed, float upwardLift)
        {
            _owner = owner;
            _riseSpeed = riseSpeed;
            _upwardLift = upwardLift;
            _phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            if (_owner == null || !_owner.isActiveAndEnabled)
            {
                Destroy(gameObject);
                return;
            }

            if (!_attached)
            {
                if (_target == null || _target.IsCollected || !_target.gameObject.activeInHierarchy)
                    _target = _owner.FindEligibleAnimal(transform.position);

                Vector3 destination = _target != null
                    ? _target.transform.position + Vector3.down * 0.72f
                    : transform.position + Vector3.up * 2f;
                float sway = Mathf.Sin(Time.time * 3f + _phase) * 0.18f;
                Vector3 next = Vector3.MoveTowards(transform.position, destination, _riseSpeed * Time.deltaTime);
                next.x += sway * Time.deltaTime;
                transform.position = next;

                if (_target != null && Vector2.Distance(transform.position, _target.transform.position) < 0.48f)
                    TryAttachToTarget();
            }
            else
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition,
                    new Vector3(0f, -0.72f, 0f), Time.deltaTime * 12f);
            }

            Camera camera = Camera.main;
            if (camera != null && transform.position.y > camera.ViewportToWorldPoint(new Vector3(0f, 1f, Mathf.Abs(camera.transform.position.z))).y + 1.5f)
            {
                _owner.RemoveBalloon(this);
                Destroy(gameObject);
            }
        }

        private void TryAttachToTarget()
        {
            if (!_owner.ClaimAnimal(_target)) return;
            _movement = _target.GetComponent<AnimalMovement>();
            if (_movement == null || !_movement.TryAttach(_owner))
            {
                return;
            }

            _attached = true;
            transform.SetParent(_target.transform, false);
            transform.localPosition = new Vector3(0f, -0.72f, 0f);
            _movement.ReleaseAttachment(_owner, Vector2.up * _upwardLift, 0.5f);
        }

        public void Detach()
        {
            if (_attached && _movement != null)
                _movement.ReleaseAttachment(_owner, Vector2.up * 0.6f, 0.15f);
            _attached = false;
            _movement = null;
        }

        private void OnDestroy()
        {
            if (_owner != null) _owner.RemoveBalloon(this);
        }
    }
}
