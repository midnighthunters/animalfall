using System.Collections.Generic;
using UnityEngine;
using AnimalFall.Core.Animals;

namespace AnimalFall.Core.Hindrances.EnvironmentMods
{
    /// <summary>Animated travelling tornado that catches nearby animals and carries them off-screen.</summary>
    public sealed class TornadoHindrance : HindranceBase, IAnimalTapGate
    {
        [SerializeField] private Sprite[] _frames;
        [SerializeField, Min(0.1f)] private float _frameRate = 13f;
        [SerializeField, Min(1f)] private float _travelDuration = 5f;
        [SerializeField, Min(0.1f)] private float _captureRadius = 1.75f;
        [SerializeField, Range(1, 8)] private int _maxCarried = 5;

        private readonly List<Animal> _carried = new List<Animal>(5);
        private Vector3 _start;
        private Vector3 _end;
        private float _elapsed;
        private float _frameClock;
        private int _frameIndex;

        public override HindranceType Type => HindranceType.Tornado;

#if UNITY_EDITOR
        public void EditorConfigure(Sprite[] frames) { _frames = frames; UnityEditor.EditorUtility.SetDirty(this); }
#endif

        protected override void OnActivate()
        {
            if (_frames == null || _frames.Length == 0) _frames = LoadFrames("icons/hindrances/tornado");
            if (_sr != null)
            {
                _sr.sprite = _frames != null && _frames.Length > 0 ? _frames[0] : null;
                _sr.sortingOrder = 38;
                _sr.enabled = true;
            }
            // Make the funnel prominent enough to read while it crosses the playfield.
            Normalize(Animal.TargetWorldSize * 2.35f);
            SetTravelPath();
            _carried.Clear();
            _elapsed = 0f;
            _frameClock = 0f;
            _frameIndex = 0;
        }

        private void Update()
        {
            if (!_isActive) return;
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _travelDuration);
            transform.position = Vector3.Lerp(_start, _end, t);
            Animate();
            CaptureNearbyAnimals();
            MoveCarriedAnimals();
            if (t >= 1f)
            {
                ReleaseCarried(true);
                Deactivate();
            }
        }

        public bool CanCollect(Animal animal) => !_carried.Contains(animal);
        public void OnBlockedTap(Animal animal) { }

        protected override void OnDeactivate() => ReleaseCarried(false);

        private void CaptureNearbyAnimals()
        {
            if (_carried.Count >= _maxCarried) return;
            var animals = ActiveAnimalRegistry.All;
            for (int i = 0; i < animals.Count && _carried.Count < _maxCarried; i++)
            {
                Animal animal = animals[i];
                if (animal == null || animal.IsCollected || _carried.Contains(animal)) continue;
                if (Vector2.Distance(animal.transform.position, transform.position) > _captureRadius) continue;
                if (!animal.TryClaimExclusive(this)) continue;
                AnimalMovement movement = animal.GetComponent<AnimalMovement>();
                if (movement == null || !movement.TryAttach(this))
                {
                    animal.ReleaseExclusive(this);
                    continue;
                }
                _carried.Add(animal);
            }
        }

        private void MoveCarriedAnimals()
        {
            for (int i = _carried.Count - 1; i >= 0; i--)
            {
                Animal animal = _carried[i];
                if (animal == null || animal.IsCollected) { _carried.RemoveAt(i); continue; }
                float angle = Time.time * (280f + i * 25f) + i * 96f;
                float radius = 0.28f + i * 0.12f;
                Vector3 orbit = Quaternion.Euler(0f, 0f, angle) * Vector3.right * radius;
                orbit.y += 0.18f + Mathf.Sin(Time.time * 8f + i) * 0.16f;
                animal.transform.position = transform.position + orbit;
                animal.transform.Rotate(0f, 0f, 540f * Time.deltaTime);
            }
        }

        private void ReleaseCarried(bool launchAway)
        {
            Vector2 travel = ((Vector2)_end - (Vector2)_start).normalized;
            for (int i = _carried.Count - 1; i >= 0; i--)
            {
                Animal animal = _carried[i];
                if (animal == null) continue;
                AnimalMovement movement = animal.GetComponent<AnimalMovement>();
                if (launchAway) movement?.LaunchOutOfScreen((travel + Vector2.up * 0.45f).normalized);
                else movement?.ReleaseAttachment(this, travel * 1.2f + Vector2.up * 0.4f);
                animal.ReleaseExclusive(this);
            }
            _carried.Clear();
        }

        private void Animate()
        {
            if (_sr == null || _frames == null || _frames.Length == 0) return;
            _frameClock += Time.deltaTime;
            float secondsPerFrame = 1f / _frameRate;
            while (_frameClock >= secondsPerFrame)
            {
                _frameClock -= secondsPerFrame;
                _frameIndex = (_frameIndex + 1) % _frames.Length;
                _sr.sprite = _frames[_frameIndex];
            }
        }

        private void SetTravelPath()
        {
            Camera camera = Camera.main;
            bool leftToRight = Random.value < 0.5f;
            if (camera == null)
            {
                _start = new Vector3(leftToRight ? -8f : 8f, 0f, 0f);
                _end = new Vector3(-_start.x, 0f, 0f);
            }
            else
            {
                float depth = Mathf.Abs(camera.transform.position.z);
                float y = Random.Range(0.34f, 0.68f);
                _start = camera.ViewportToWorldPoint(new Vector3(leftToRight ? -0.12f : 1.12f, y, depth));
                _end = camera.ViewportToWorldPoint(new Vector3(leftToRight ? 1.12f : -0.12f, y, depth));
                _start.z = _end.z = 0f;
            }
            transform.position = _start;
        }

        private void Normalize(float worldSize)
        {
            if (_sr == null || _sr.sprite == null) return;
            float largest = Mathf.Max(_sr.sprite.bounds.size.x, _sr.sprite.bounds.size.y);
            if (largest > 0.001f) transform.localScale = Vector3.one * (worldSize / largest);
        }

        private static Sprite[] LoadFrames(string path)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>(path);
            System.Array.Sort(sprites, (a, b) => ExtractIndex(a.name).CompareTo(ExtractIndex(b.name)));
            return sprites;
        }

        private static int ExtractIndex(string name)
        {
            int split = name.LastIndexOf('_');
            return split >= 0 && int.TryParse(name.Substring(split + 1), out int value) ? value : 0;
        }
    }
}
