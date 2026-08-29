using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnimalFall.Core.Animals;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.New
{
    /// <summary>
    /// Tapping the armed porcupine fires one visible spike at every active animal.
    /// Spike hits remove animals without awarding goal progress.
    /// </summary>
    public sealed class PorcupineHindrance : HindranceBase, IPointerTapTarget
    {
        [SerializeField] private Sprite _spikedSprite;
        [SerializeField] private Sprite _noSpikesSprite;
        [SerializeField] private Sprite _spikeSprite;
        [SerializeField, Min(0.1f)] private float _fallSpeed = 2f;
        [SerializeField, Min(0.05f)] private float _displayScale = 0.18f;
        [SerializeField, Min(0.05f)] private float _spikeScale = 0.07f;
        [SerializeField, Min(0.1f)] private float _spikeTravelDuration = 0.42f;
        [SerializeField, Min(1f)] private float _untappedLifetime = 7f;

        private readonly List<GameObject> _liveSpikes = new List<GameObject>(16);
        private bool _firing;
        private int _pendingSpikes;

        public override HindranceType Type => HindranceType.PorcupinePulse;
        public int InteractionPriority => 320;
        public int AnimalsEliminated { get; private set; }

#if UNITY_EDITOR
        public void EditorConfigure(Sprite spikedSprite, Sprite noSpikesSprite, Sprite spikeSprite)
        {
            _spikedSprite = spikedSprite;
            _noSpikesSprite = noSpikesSprite;
            _spikeSprite = spikeSprite;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        protected override void OnActivate()
        {
            transform.localScale = Vector3.one * _displayScale;
            _firing = false;
            _pendingSpikes = 0;
            AnimalsEliminated = 0;
            if (_sr != null)
            {
                _sr.sprite = _spikedSprite != null ? _spikedSprite : _noSpikesSprite;
                _sr.sortingOrder = 31;
                _sr.enabled = true;
            }

            StartCoroutine(FallUntilRetired());
        }

        protected override void OnDeactivate()
        {
            for (int i = _liveSpikes.Count - 1; i >= 0; i--)
                if (_liveSpikes[i] != null) Destroy(_liveSpikes[i]);
            _liveSpikes.Clear();
            _pendingSpikes = 0;
            _firing = false;
            if (_sr != null) _sr.sprite = _spikedSprite != null ? _spikedSprite : _noSpikesSprite;
        }

        public bool TryHandleTap(WorldPointerEvent pointerEvent)
        {
            if (!_isActive || _firing) return _isActive;
            StartCoroutine(FireAtAllAnimals());
            return true;
        }

        private IEnumerator FireAtAllAnimals()
        {
            _firing = true;
            if (_sr != null && _noSpikesSprite != null) _sr.sprite = _noSpikesSprite;
            GameEvents.OnSfxRequested?.Invoke(SfxType.HindranceActivate);

            var targets = new List<Animal>(ActiveAnimalRegistry.All.Count);
            for (int i = 0; i < ActiveAnimalRegistry.All.Count; i++)
            {
                Animal animal = ActiveAnimalRegistry.All[i];
                if (IsAvailable(animal)) targets.Add(animal);
            }

            _pendingSpikes = targets.Count;
            if (_pendingSpikes == 0)
            {
                yield return new WaitForSeconds(0.25f);
                Deactivate();
                yield break;
            }

            for (int i = 0; i < targets.Count; i++)
                StartCoroutine(FlySpike(targets[i], i * 0.025f));

            while (_isActive && _pendingSpikes > 0) yield return null;
            if (!_isActive) yield break;

            yield return new WaitForSeconds(0.35f);
            Deactivate();
        }

        private IEnumerator FlySpike(Animal target, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (!_isActive)
            {
                _pendingSpikes--;
                yield break;
            }

            GameObject spike = new GameObject("PorcupineSpike");
            _liveSpikes.Add(spike);
            SpriteRenderer renderer = spike.AddComponent<SpriteRenderer>();
            renderer.sprite = _spikeSprite;
            renderer.sortingOrder = 35;
            spike.transform.position = transform.position;
            spike.transform.localScale = Vector3.one * _spikeScale;

            float elapsed = 0f;
            Vector3 start = transform.position;
            while (_isActive && elapsed < _spikeTravelDuration && IsAvailable(target))
            {
                elapsed += Time.deltaTime;
                Vector3 destination = target.transform.position;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / _spikeTravelDuration));
                spike.transform.position = Vector3.Lerp(start, destination, t);
                Vector2 direction = destination - spike.transform.position;
                if (direction.sqrMagnitude > 0.0001f)
                    spike.transform.rotation = Quaternion.Euler(0f, 0f,
                        Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
                yield return null;
            }

            if (_isActive && IsAvailable(target))
            {
                target.Despawn();
                AnimalsEliminated++;
            }

            _liveSpikes.Remove(spike);
            if (spike != null) Destroy(spike);
            _pendingSpikes--;
        }

        private IEnumerator FallUntilRetired()
        {
            float elapsed = 0f;
            float lifetime = Mathf.Max(1f, _untappedLifetime);
            while (_isActive && !_firing && elapsed < lifetime)
            {
                float deltaTime = Time.deltaTime;
                elapsed += deltaTime;
                transform.position += Vector3.down * (_fallSpeed * deltaTime);

                Camera camera = Camera.main;
                if (camera != null && camera.WorldToViewportPoint(transform.position).y < -0.08f)
                {
                    Deactivate();
                    yield break;
                }

                yield return null;
            }

            if (_isActive && !_firing)
                Deactivate();
        }



        private static bool IsAvailable(Animal animal)
        {
            return animal != null && animal.gameObject.activeInHierarchy && !animal.IsCollected;
        }
    }
}