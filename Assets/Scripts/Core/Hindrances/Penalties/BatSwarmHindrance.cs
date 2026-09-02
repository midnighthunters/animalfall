using System.Collections.Generic;
using UnityEngine;
using AnimalFall.Core.Animals;

namespace AnimalFall.Core.Hindrances.Penalties
{
    /// <summary>
    /// A flowing formation of animated bats that sweeps across the screen and
    /// carries away every eligible animal visible when the swarm arrives.
    /// </summary>
    public sealed class BatSwarmHindrance : HindranceBase, IAnimalTapGate
    {
        [SerializeField] private Sprite[] _frames;
        [SerializeField, Range(10, 12)] private int _minimumBats = 10;
        [SerializeField, Range(10, 12)] private int _maximumBats = 12;
        [SerializeField, Min(0.1f)] private float _frameRate = 14f;
        // Four seconds is 1.5x faster than the original six-second sweep.
        [SerializeField, Min(1f)] private float _travelDuration = 4f;
        [SerializeField, Min(0.1f)] private float _batWorldSize = 0.95f;
        [SerializeField, Range(0f, 0.5f)] private float _captureStart = 0.16f;
        [SerializeField, Min(0.1f)] private float _captureBlendSeconds = 0.8f;

        private sealed class Capture
        {
            public Animal Animal;
            public AnimalMovement Movement;
            public int BatIndex;
            public Vector3 StartPosition;
            public Vector2 CarryOffset;
        }

        private readonly List<SpriteRenderer> _bats = new List<SpriteRenderer>(12);
        private readonly List<Capture> _captured = new List<Capture>(16);
        private Vector3 _start;
        private Vector3 _end;
        private float _elapsed;
        private float _frameClock;
        private int _frameIndex;
        private int _direction;
        private int _activeBatCount;
        private bool _completed;

        public override HindranceType Type => HindranceType.BatSwarm;
        public int ActiveBatCount => _activeBatCount;
        public int CapturedAnimalCount => _captured.Count;

#if UNITY_EDITOR
        public void EditorConfigure(Sprite[] frames)
        {
            _frames = frames;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        protected override void OnActivate()
        {
            if (_frames == null || _frames.Length == 0)
                _frames = LoadFrames("icons/hindrances/bat");

            // The root renderer is only an editor/prefab preview. Runtime bats are
            // individual children so the formation can flap and flow independently.
            if (_sr != null) _sr.enabled = false;

            _minimumBats = Mathf.Clamp(_minimumBats, 10, 12);
            _maximumBats = Mathf.Clamp(_maximumBats, _minimumBats, 12);
            _activeBatCount = Random.Range(_minimumBats, _maximumBats + 1);
            SetTravelPath();
            EnsureBatRenderers();
            CaptureVisibleAnimals();

            _elapsed = 0f;
            _frameClock = 0f;
            _frameIndex = 0;
            _completed = false;
            UpdateBatFormation(0f);
        }

        private void Update()
        {
            if (!_isActive) return;

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _travelDuration);
            transform.position = Vector3.Lerp(_start, _end, t);
            AnimateBats();
            UpdateBatFormation(t);
            MoveCapturedAnimals(t);

            if (t >= 1f)
            {
                _completed = true;
                DespawnCapturedAnimals();
                Deactivate();
            }
        }

        public bool CanCollect(Animal animal) => !ContainsCapturedAnimal(animal);
        public void OnBlockedTap(Animal animal) { }

        protected override void OnDeactivate()
        {
            if (!_completed) ReleaseCapturedAnimals();
            HideBatRenderers();
        }

        private void EnsureBatRenderers()
        {
            while (_bats.Count < 12)
            {
                int index = _bats.Count;
                var batObject = new GameObject("Bat " + (index + 1));
                batObject.transform.SetParent(transform, false);
                SpriteRenderer renderer = batObject.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = 42 + index % 3;
                _bats.Add(renderer);
            }

            for (int i = 0; i < _bats.Count; i++)
            {
                SpriteRenderer renderer = _bats[i];
                bool active = i < _activeBatCount;
                renderer.gameObject.SetActive(active);
                if (!active) continue;
                renderer.sprite = _frames != null && _frames.Length > 0
                    ? _frames[i % _frames.Length]
                    : null;
                // The source bat frames face left by default. Flip only when
                // travelling right so the bats always face their movement.
                renderer.flipX = _direction > 0;
                renderer.color = Color.white;
                NormalizeBat(renderer);
            }
        }

        private void AnimateBats()
        {
            if (_frames == null || _frames.Length == 0) return;
            _frameClock += Time.deltaTime;
            float secondsPerFrame = 1f / _frameRate;
            while (_frameClock >= secondsPerFrame)
            {
                _frameClock -= secondsPerFrame;
                _frameIndex = (_frameIndex + 1) % _frames.Length;
            }

            for (int i = 0; i < _activeBatCount; i++)
                _bats[i].sprite = _frames[(_frameIndex + i * 2) % _frames.Length];
        }

        private void UpdateBatFormation(float travelT)
        {
            for (int i = 0; i < _activeBatCount; i++)
            {
                int column = i % 4;
                int row = i / 4;
                float trail = column * 0.48f * -_direction;
                float vertical = (row - 1f) * 0.92f + (column % 2 == 0 ? 0.12f : -0.12f);
                float wave = Mathf.Sin(Time.time * 5.4f + i * 0.83f + travelT * 8f) * 0.18f;
                _bats[i].transform.localPosition = new Vector3(trail, vertical + wave, 0f);
                _bats[i].transform.localRotation = Quaternion.Euler(0f, 0f,
                    Mathf.Sin(Time.time * 4f + i) * 7f);
            }
        }

        private void CaptureVisibleAnimals()
        {
            _captured.Clear();
            Camera camera = Camera.main;
            var animals = ActiveAnimalRegistry.All;
            for (int i = animals.Count - 1; i >= 0; i--)
            {
                Animal animal = animals[i];
                if (animal == null || !animal.gameObject.activeInHierarchy || animal.IsCollected) continue;
                if (!IsVisible(camera, animal.transform.position)) continue;
                if (!animal.TryClaimExclusive(this)) continue;

                AnimalMovement movement = animal.GetComponent<AnimalMovement>();
                if (movement == null || !movement.TryAttach(this))
                {
                    animal.ReleaseExclusive(this);
                    continue;
                }

                int captureIndex = _captured.Count;
                int batIndex = captureIndex % _activeBatCount;
                int carrierLoad = captureIndex / _activeBatCount;
                _captured.Add(new Capture
                {
                    Animal = animal,
                    Movement = movement,
                    BatIndex = batIndex,
                    StartPosition = animal.transform.position,
                    CarryOffset = new Vector2(-0.08f * _direction + carrierLoad * 0.16f,
                        -0.5f - carrierLoad * 0.22f)
                });
            }
        }

        private void MoveCapturedAnimals(float travelT)
        {
            float blendDuration = Mathf.Max(0.01f, _captureBlendSeconds / _travelDuration);
            float captureT = Mathf.Clamp01((travelT - _captureStart) / blendDuration);
            captureT = captureT * captureT * (3f - 2f * captureT);

            for (int i = _captured.Count - 1; i >= 0; i--)
            {
                Capture capture = _captured[i];
                Animal animal = capture.Animal;
                if (animal == null || animal.IsCollected)
                {
                    _captured.RemoveAt(i);
                    continue;
                }

                Vector3 target = _bats[capture.BatIndex].transform.position +
                    (Vector3)capture.CarryOffset;
                animal.transform.position = Vector3.Lerp(capture.StartPosition, target, captureT);
                if (captureT > 0f)
                    animal.transform.Rotate(0f, 0f, _direction * 300f * Time.deltaTime);
            }
        }

        private void DespawnCapturedAnimals()
        {
            for (int i = _captured.Count - 1; i >= 0; i--)
            {
                Capture capture = _captured[i];
                if (capture.Animal == null) continue;
                capture.Movement?.ReleaseAttachment(this, Vector2.zero, 0f);
                capture.Animal.ReleaseExclusive(this);
                capture.Animal.Despawn();
            }
            _captured.Clear();
        }

        private void ReleaseCapturedAnimals()
        {
            for (int i = _captured.Count - 1; i >= 0; i--)
            {
                Capture capture = _captured[i];
                if (capture.Animal == null) continue;
                capture.Movement?.ReleaseAttachment(this, Vector2.down * 0.45f);
                capture.Animal.ReleaseExclusive(this);
            }
            _captured.Clear();
        }

        private void SetTravelPath()
        {
            Camera camera = Camera.main;
            _direction = Random.value < 0.5f ? 1 : -1;
            if (camera == null)
            {
                _start = new Vector3(-9f * _direction, 1f, 0f);
                _end = new Vector3(9f * _direction, 1f, 0f);
            }
            else
            {
                float depth = Mathf.Abs(camera.transform.position.z - transform.position.z);
                float startX = _direction > 0 ? -0.3f : 1.3f;
                float endX = _direction > 0 ? 1.3f : -0.3f;
                _start = camera.ViewportToWorldPoint(new Vector3(startX, 0.56f, depth));
                _end = camera.ViewportToWorldPoint(new Vector3(endX, 0.56f, depth));
                _start.z = _end.z = 0f;
            }
            transform.position = _start;
        }

        private void NormalizeBat(SpriteRenderer renderer)
        {
            if (renderer == null || renderer.sprite == null) return;
            float largest = Mathf.Max(renderer.sprite.bounds.size.x, renderer.sprite.bounds.size.y);
            renderer.transform.localScale = largest > 0.001f
                ? Vector3.one * (_batWorldSize / largest)
                : Vector3.one;
        }

        private bool ContainsCapturedAnimal(Animal animal)
        {
            for (int i = 0; i < _captured.Count; i++)
                if (_captured[i].Animal == animal) return true;
            return false;
        }

        private static bool IsVisible(Camera camera, Vector3 position)
        {
            if (camera == null) return true;
            Vector3 viewport = camera.WorldToViewportPoint(position);
            return viewport.z > 0f && viewport.x >= -0.03f && viewport.x <= 1.03f &&
                viewport.y >= -0.03f && viewport.y <= 1.03f;
        }

        private void HideBatRenderers()
        {
            for (int i = 0; i < _bats.Count; i++)
                if (_bats[i] != null) _bats[i].gameObject.SetActive(false);
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
