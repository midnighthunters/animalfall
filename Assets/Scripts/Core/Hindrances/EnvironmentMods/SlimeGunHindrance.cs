using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnimalFall.Core.Animals;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.EnvironmentMods
{
    /// <summary>
    /// A slime gun positioned over the goal-card area periodically shoots a
    /// visible slime blob at an active animal. A hit animal is held in place
    /// with a slime overlay for a short time, then released to fall again.
    /// </summary>
    public sealed class SlimeGunHindrance : HindranceBase
    {
        [SerializeField] private Sprite _gunSprite;
        [SerializeField, Min(0.5f)] private float _activeDuration = 8f;
        [SerializeField, Min(0.25f)] private float _shotInterval = 1.45f;
        [SerializeField, Min(0.1f)] private float _projectileSpeed = 5.2f;
        [SerializeField, Min(0.25f)] private float _captureDuration = 2.4f;
        [SerializeField, Min(0.1f)] private float _gunWorldSize = 0.9f;
        [SerializeField] private Vector2 _goalPanelViewportPosition = new Vector2(0.18f, 0.24f);

        private readonly HashSet<Animal> _capturedAnimals = new HashSet<Animal>();
        private readonly List<SlimeCapture> _liveCaptures = new List<SlimeCapture>(4);
        private static Sprite _slimeSprite;

        public override HindranceType Type => HindranceType.SlimeGun;

#if UNITY_EDITOR
        public void EditorConfigure(Sprite gunSprite)
        {
            _gunSprite = gunSprite;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        protected override void OnActivate()
        {
            _capturedAnimals.Clear();
            _liveCaptures.Clear();
            _sr.sprite = _gunSprite != null ? _gunSprite : Resources.Load<Sprite>("icons/hindrances/slime_gun");
            _sr.enabled = true;
            FitGunToWorldSize();
            PlaceAboveGoalPanel();
            StartCoroutine(ShootRoutine());
        }

        protected override void OnDeactivate()
        {
            for (int i = _liveCaptures.Count - 1; i >= 0; i--)
            {
                SlimeCapture capture = _liveCaptures[i];
                if (capture != null) capture.Release();
            }
            _liveCaptures.Clear();
            _capturedAnimals.Clear();
        }

        private IEnumerator ShootRoutine()
        {
            float finishAt = Time.time + _activeDuration;
            while (_isActive && Time.time < finishAt)
            {
                Animal target = FindTargetAnimal();
                if (target != null) StartCoroutine(ShootAt(target));
                yield return new WaitForSeconds(_shotInterval);
            }
            if (_isActive) Deactivate();
        }

        private IEnumerator ShootAt(Animal target)
        {
            GameObject blob = new GameObject("SlimeGun_Projectile");
            var renderer = blob.AddComponent<SpriteRenderer>();
            renderer.sprite = GetSlimeSprite();
            renderer.sortingOrder = 37;
            blob.transform.position = transform.position + Vector3.up * 0.42f;
            blob.transform.localScale = Vector3.one * 0.62f;

            while (_isActive && target != null && target.gameObject.activeInHierarchy)
            {
                Vector3 destination = target.transform.position;
                blob.transform.position = Vector3.MoveTowards(blob.transform.position, destination,
                    _projectileSpeed * Time.deltaTime);
                if (Vector2.Distance(blob.transform.position, destination) < 0.3f)
                {
                    Capture(target);
                    break;
                }
                yield return null;
            }

            if (blob != null) Destroy(blob);
        }

        private void Capture(Animal animal)
        {
            if (!_isActive || animal == null || animal.IsCollected || !_capturedAnimals.Add(animal)) return;
            AnimalMovement movement = animal.GetComponent<AnimalMovement>();
            if (movement == null || !movement.TryAttach(this))
            {
                _capturedAnimals.Remove(animal);
                return;
            }

            var slime = new GameObject("SlimeGun_SlimeCapture");
            var renderer = slime.AddComponent<SpriteRenderer>();
            renderer.sprite = GetSlimeSprite();
            renderer.color = new Color(0.38f, 1f, 0.18f, 0.78f);
            renderer.sortingOrder = 38;
            slime.transform.SetParent(animal.transform, false);
            slime.transform.localPosition = Vector3.zero;
            slime.transform.localScale = Vector3.one * 0.9f;

            var capture = slime.AddComponent<SlimeCapture>();
            capture.Configure(this, animal, movement, _captureDuration);
            _liveCaptures.Add(capture);
            GameEvents.OnSfxRequested?.Invoke(SfxType.HindranceActivate);
        }

        internal void RemoveCapture(SlimeCapture capture, Animal animal)
        {
            _liveCaptures.Remove(capture);
            if (animal != null) _capturedAnimals.Remove(animal);
        }

        private Animal FindTargetAnimal()
        {
            Animal best = null;
            float bestY = float.MinValue;
            var animals = ActiveAnimalRegistry.All;
            for (int i = 0; i < animals.Count; i++)
            {
                Animal animal = animals[i];
                if (animal == null || animal.IsCollected || _capturedAnimals.Contains(animal)) continue;
                if (animal.transform.position.y > bestY)
                {
                    bestY = animal.transform.position.y;
                    best = animal;
                }
            }
            return best;
        }

        private void FitGunToWorldSize()
        {
            if (_sr.sprite == null) return;
            float largest = Mathf.Max(_sr.sprite.bounds.size.x, _sr.sprite.bounds.size.y);
            transform.localScale = Vector3.one * (_gunWorldSize / Mathf.Max(0.001f, largest));
        }

        private void PlaceAboveGoalPanel()
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            float depth = Mathf.Abs(camera.transform.position.z - transform.position.z);
            Vector3 position = camera.ViewportToWorldPoint(new Vector3(
                _goalPanelViewportPosition.x, _goalPanelViewportPosition.y, depth));
            position.z = transform.position.z;
            transform.position = position;
        }

        private static Sprite GetSlimeSprite()
        {
            if (_slimeSprite != null) return _slimeSprite;
            const int width = 48;
            const int height = 32;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float dx = (x - 23.5f) / 21f;
                    float dy = (y - 15.5f) / 14f;
                    float edge = dx * dx + dy * dy;
                    pixels[y * width + x] = edge <= 1f
                        ? new Color(0.25f, 1f, 0.08f, 0.9f)
                        : Color.clear;
                }
            texture.SetPixels(pixels);
            texture.Apply();
            _slimeSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height),
                Vector2.one * 0.5f, 48f);
            return _slimeSprite;
        }
    }

    public sealed class SlimeCapture : MonoBehaviour
    {
        private SlimeGunHindrance _owner;
        private Animal _animal;
        private AnimalMovement _movement;
        private float _duration;
        private float _startedAt;
        private bool _released;

        public void Configure(SlimeGunHindrance owner, Animal animal, AnimalMovement movement, float duration)
        {
            _owner = owner;
            _animal = animal;
            _movement = movement;
            _duration = duration;
            _startedAt = Time.time;
        }

        private void Update()
        {
            if (_released) return;
            if (_animal == null || !_animal.gameObject.activeInHierarchy || Time.time >= _startedAt + _duration)
                Release();
        }

        public void Release()
        {
            if (_released) return;
            _released = true;
            if (_movement != null) _movement.ReleaseAttachment(_owner, Vector2.zero, 0.15f);
            if (_owner != null) _owner.RemoveCapture(this, _animal);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (!_released && _movement != null)
                _movement.ReleaseAttachment(_owner, Vector2.zero, 0.15f);
        }
    }
}
