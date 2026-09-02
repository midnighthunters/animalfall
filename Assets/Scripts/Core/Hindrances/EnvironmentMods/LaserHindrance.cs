using System.Collections;
using UnityEngine;
using AnimalFall.Core.Animals;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.EnvironmentMods
{
    /// <summary>Mirrored emitters form a horizontal laser that eliminates crossing animals.</summary>
    public sealed class LaserHindrance : HindranceBase
    {
        [SerializeField] private Sprite _emitterSprite;
        [SerializeField, Min(0f)] private float _warningDuration = 0.8f;
        [SerializeField, Min(1f)] private float _activeDuration = 6.5f;
        [SerializeField, Min(0.02f)] private float _collisionHalfWidth = 0.16f;

        private SpriteRenderer _leftEmitter;
        private SpriteRenderer _rightEmitter;
        private LineRenderer _glow;
        private LineRenderer _core;
        private Material _beamMaterial;
        private Vector3 _leftPoint;
        private Vector3 _rightPoint;
        private bool _armed;

        public override HindranceType Type => HindranceType.Laser;

#if UNITY_EDITOR
        public void EditorConfigure(Sprite sprite) { _emitterSprite = sprite; UnityEditor.EditorUtility.SetDirty(this); }
#endif

        protected override void OnActivate()
        {
            if (_sr != null) _sr.enabled = false;
            EnsureVisuals();
            PlaceAcrossPlayfield();
            _armed = false;
            SetBeamColors(new Color(1f, 0.72f, 0.15f, 0.45f), new Color(1f, 0.9f, 0.45f, 0.75f));
            StartCoroutine(LaserRoutine());
        }

        private void Update()
        {
            if (!_isActive || _core == null) return;
            float pulse = 0.8f + Mathf.Sin(Time.time * 18f) * 0.18f;
            _core.widthMultiplier = 0.07f * pulse;
            _glow.widthMultiplier = 0.24f * pulse;
            if (_armed) EliminateCrossingAnimals();
        }

        private IEnumerator LaserRoutine()
        {
            yield return new WaitForSeconds(_warningDuration);
            if (!_isActive) yield break;
            _armed = true;
            SetBeamColors(new Color(1f, 0.06f, 0.05f, 0.55f), Color.white);
            GameEvents.OnSfxRequested?.Invoke(SfxType.HindranceActivate);
            yield return new WaitForSeconds(_activeDuration);
            Deactivate();
        }

        private void EliminateCrossingAnimals()
        {
            var animals = ActiveAnimalRegistry.All;
            float minX = Mathf.Min(_leftPoint.x, _rightPoint.x);
            float maxX = Mathf.Max(_leftPoint.x, _rightPoint.x);
            for (int i = animals.Count - 1; i >= 0; i--)
            {
                Animal animal = animals[i];
                if (animal == null || animal.IsCollected) continue;
                Vector3 position = animal.transform.position;
                if (position.x < minX || position.x > maxX || Mathf.Abs(position.y - _leftPoint.y) > _collisionHalfWidth)
                    continue;
                animal.Despawn();
            }
        }

        protected override void OnDeactivate()
        {
            _armed = false;
            if (_leftEmitter != null) _leftEmitter.enabled = false;
            if (_rightEmitter != null) _rightEmitter.enabled = false;
            if (_glow != null) _glow.enabled = false;
            if (_core != null) _core.enabled = false;
        }

        private void EnsureVisuals()
        {
            Sprite sprite = _emitterSprite != null ? _emitterSprite : FirstSprite("icons/hindrances/Laser");
            _leftEmitter = EnsureRenderer("Left Emitter", sprite, false);
            _rightEmitter = EnsureRenderer("Right Emitter", sprite, true);
            _glow = EnsureLine("Laser Glow", 0.24f, 31);
            _core = EnsureLine("Laser Core", 0.07f, 32);
            _leftEmitter.enabled = _rightEmitter.enabled = true;
            _glow.enabled = _core.enabled = true;
        }

        private void PlaceAcrossPlayfield()
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            float depth = Mathf.Abs(camera.transform.position.z);
            float y = Random.Range(0.34f, 0.72f);
            _leftPoint = camera.ViewportToWorldPoint(new Vector3(0.04f, y, depth));
            _rightPoint = camera.ViewportToWorldPoint(new Vector3(0.96f, y, depth));
            _leftPoint.z = _rightPoint.z = 0f;
            transform.position = Vector3.zero;
            FitEmitter(_leftEmitter, _leftPoint, 0.92f);
            FitEmitter(_rightEmitter, _rightPoint, 0.92f);
            SetLine(_glow);
            SetLine(_core);
        }

        private void SetLine(LineRenderer line)
        {
            line.positionCount = 2;
            line.SetPosition(0, _leftPoint);
            line.SetPosition(1, _rightPoint);
        }

        private SpriteRenderer EnsureRenderer(string name, Sprite sprite, bool flip)
        {
            Transform child = transform.Find(name);
            if (child == null)
            {
                child = new GameObject(name).transform;
                child.SetParent(transform, false);
            }
            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (!renderer) renderer = child.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.flipX = flip;
            renderer.sortingOrder = 33;
            return renderer;
        }

        private LineRenderer EnsureLine(string name, float width, int sortingOrder)
        {
            Transform child = transform.Find(name);
            if (child == null)
            {
                child = new GameObject(name).transform;
                child.SetParent(transform, false);
            }
            LineRenderer line = child.GetComponent<LineRenderer>();
            if (!line) line = child.gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.widthMultiplier = width;
            line.numCapVertices = 5;
            line.sortingOrder = sortingOrder;
            line.material = GetBeamMaterial();
            return line;
        }

        private void FitEmitter(SpriteRenderer renderer, Vector3 position, float worldSize)
        {
            if (renderer == null) return;
            renderer.transform.position = position;
            float largest = renderer.sprite != null ? Mathf.Max(renderer.sprite.bounds.size.x, renderer.sprite.bounds.size.y) : 1f;
            renderer.transform.localScale = Vector3.one * (worldSize / Mathf.Max(0.001f, largest));
        }

        private void SetBeamColors(Color glow, Color core)
        {
            if (_glow != null) _glow.startColor = _glow.endColor = glow;
            if (_core != null) _core.startColor = _core.endColor = core;
        }

        private Material GetBeamMaterial()
        {
            if (_beamMaterial != null) return _beamMaterial;
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            _beamMaterial = new Material(shader) { name = "Laser Beam Runtime" };
            return _beamMaterial;
        }

        private void OnDestroy() { if (_beamMaterial != null) Destroy(_beamMaterial); }

        private static Sprite FirstSprite(string path)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>(path);
            return sprites.Length > 0 ? sprites[0] : null;
        }
    }
}
