using System.Collections;
using UnityEngine;
using AnimalFall.Core.Animals;

namespace AnimalFall.Core.Hindrances.EnvironmentMods
{
    /// <summary>Animated fan above the goal card that pushes animals along a 45-degree air cone.</summary>
    public sealed class FanHindrance : HindranceBase
    {
        [SerializeField] private Sprite[] _frames;
        [SerializeField] private Vector2 _viewportAnchor = new Vector2(0.16f, 0.2f);
        [SerializeField, Min(1f)] private float _duration = 9f;
        [SerializeField, Min(0.1f)] private float _frameRate = 12f;
        [SerializeField, Min(0.1f)] private float _coneLength = 6f;
        [SerializeField, Range(0.1f, 1f)] private float _coneHalfAngleTangent = 0.5f;
        [SerializeField, Min(0.1f)] private float _pushStrength = 6.5f;

        private readonly Vector2 _direction = new Vector2(1f, 1f).normalized;
        private LineRenderer[] _airLines;
        private Material _airMaterial;
        private float _frameClock;
        private int _frameIndex;

        public override HindranceType Type => HindranceType.Fan;

#if UNITY_EDITOR
        public void EditorConfigure(Sprite[] frames) { _frames = frames; UnityEditor.EditorUtility.SetDirty(this); }
#endif

        protected override void OnActivate()
        {
            if (_frames == null || _frames.Length == 0) _frames = LoadFrames("icons/hindrances/fan");
            if (_sr != null)
            {
                _sr.sprite = _frames != null && _frames.Length > 0 ? _frames[0] : null;
                _sr.sortingOrder = 36;
                _sr.enabled = true;
            }
            Normalize(Animal.TargetWorldSize * 1.45f);
            PositionAboveGoalPanel();
            EnsureAirLines();
            SetAirLinesEnabled(true);
            _frameClock = 0f;
            _frameIndex = 0;
            StartCoroutine(RetireAfter(_duration));
        }

        private void Update()
        {
            if (!_isActive) return;
            AnimateFan();
            PushAnimalsInCone();
            UpdateAirLines();
        }

        private void LateUpdate()
        {
            if (_isActive) PositionAboveGoalPanel();
        }

        protected override void OnDeactivate() => SetAirLinesEnabled(false);

        private void AnimateFan()
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

        private void PushAnimalsInCone()
        {
            Vector2 origin = transform.position;
            Vector2 perpendicular = new Vector2(-_direction.y, _direction.x);
            var animals = ActiveAnimalRegistry.All;
            for (int i = animals.Count - 1; i >= 0; i--)
            {
                Animal animal = animals[i];
                if (animal == null || animal.IsCollected) continue;
                Vector2 delta = (Vector2)animal.transform.position - origin;
                float along = Vector2.Dot(delta, _direction);
                if (along < 0f || along > _coneLength) continue;
                float across = Mathf.Abs(Vector2.Dot(delta, perpendicular));
                if (across > 0.35f + along * _coneHalfAngleTangent) continue;
                animal.GetComponent<AnimalMovement>()?.AddImpulse(_direction * (_pushStrength * Time.deltaTime));
            }
        }

        private void EnsureAirLines()
        {
            if (_airLines != null && _airLines.Length == 5) return;
            _airLines = new LineRenderer[5];
            for (int i = 0; i < _airLines.Length; i++)
            {
                Transform child = transform.Find("Air Stream " + i);
                if (child == null)
                {
                    child = new GameObject("Air Stream " + i).transform;
                    child.SetParent(transform, false);
                }
                LineRenderer line = child.GetComponent<LineRenderer>();
                if (!line) line = child.gameObject.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 4;
                line.widthMultiplier = 0.035f + i * 0.006f;
                line.numCapVertices = 3;
                line.sortingOrder = 35;
                line.material = GetAirMaterial();
                _airLines[i] = line;
            }
        }

        private void UpdateAirLines()
        {
            if (_airLines == null) return;
            Vector2 perpendicular = new Vector2(-_direction.y, _direction.x);
            for (int i = 0; i < _airLines.Length; i++)
            {
                LineRenderer line = _airLines[i];
                if (line == null) continue;
                float phase = Mathf.Repeat(Time.time * 2.2f + i * 0.19f, 1f);
                float side = (i - 2) * 0.18f;
                Vector2 start = (Vector2)transform.position + _direction * (0.45f + phase * 1.4f) + perpendicular * side;
                for (int p = 0; p < 4; p++)
                {
                    Vector2 point = start + _direction * (p * 0.55f) + perpendicular * Mathf.Sin(Time.time * 9f + i + p) * 0.04f;
                    line.SetPosition(p, point);
                }
                Color color = new Color(0.72f, 0.93f, 1f, 0.62f);
                line.startColor = color;
                line.endColor = new Color(color.r, color.g, color.b, 0f);
            }
        }

        private void PositionAboveGoalPanel()
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            float depth = Mathf.Abs(camera.transform.position.z - transform.position.z);
            Vector3 world = camera.ViewportToWorldPoint(new Vector3(_viewportAnchor.x, _viewportAnchor.y, depth));
            world.z = 0f;
            transform.position = world;
            transform.rotation = Quaternion.Euler(0f, 0f, -8f);
        }

        private void Normalize(float worldSize)
        {
            if (_sr == null || _sr.sprite == null) return;
            float largest = Mathf.Max(_sr.sprite.bounds.size.x, _sr.sprite.bounds.size.y);
            if (largest > 0.001f) transform.localScale = Vector3.one * (worldSize / largest);
        }

        private void SetAirLinesEnabled(bool enabled)
        {
            if (_airLines == null) return;
            for (int i = 0; i < _airLines.Length; i++) if (_airLines[i] != null) _airLines[i].enabled = enabled;
        }

        private Material GetAirMaterial()
        {
            if (_airMaterial != null) return _airMaterial;
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            _airMaterial = new Material(shader) { name = "Fan Air Runtime" };
            return _airMaterial;
        }

        private void OnDestroy() { if (_airMaterial != null) Destroy(_airMaterial); }

        private IEnumerator RetireAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (_isActive) Deactivate();
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
