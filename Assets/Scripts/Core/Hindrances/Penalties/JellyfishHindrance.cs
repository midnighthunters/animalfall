using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnimalFall.Core.Animals;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances.Penalties
{
    /// <summary>A tappable jellyfish that electrically clears every animal currently on screen.</summary>
    public sealed class JellyfishHindrance : HindranceBase, IPointerTapTarget
    {
        [SerializeField] private Sprite _jellyfishSprite;
        [SerializeField, Min(0.1f)] private float _fallSpeed = 0.85f;
        [SerializeField, Min(1f)] private float _visibleLifetime = 7f;
        [SerializeField, Min(0.05f)] private float _shockDuration = 0.38f;

        private Collider2D _collider;
        private bool _shocking;
        private Material _lightningMaterial;
        private readonly List<GameObject> _bolts = new List<GameObject>(16);
        private float _screenBottom = -6f;

        public override HindranceType Type => HindranceType.Jellyfish;
        public int InteractionPriority => 250;

#if UNITY_EDITOR
        public void EditorConfigure(Sprite sprite) { _jellyfishSprite = sprite; UnityEditor.EditorUtility.SetDirty(this); }
#endif

        protected override void Awake()
        {
            base.Awake();
            _collider = GetComponent<Collider2D>();
        }

        protected override void OnActivate()
        {
            _shocking = false;
            if (_sr != null)
            {
                _sr.sprite = _jellyfishSprite != null ? _jellyfishSprite : FirstSprite("icons/hindrances/jellyfish");
                _sr.sortingOrder = 35;
                _sr.enabled = true;
            }
            FitVisualAndCollider(Animal.TargetWorldSize * 1.35f);
            MoveIntoPlayArea(0.86f);
            if (_collider != null) _collider.enabled = true;
            if (Camera.main != null)
            {
                float z = Mathf.Abs(Camera.main.transform.position.z);
                _screenBottom = Camera.main.ViewportToWorldPoint(new Vector3(0f, -0.08f, z)).y;
            }
            StartCoroutine(RetireAfter(_visibleLifetime));
        }

        private void Update()
        {
            if (!_isActive || _shocking) return;
            transform.Translate(0f, -_fallSpeed * Time.deltaTime, 0f, Space.World);
            if (transform.position.y < _screenBottom)
                Deactivate();
        }

        public bool TryHandleTap(WorldPointerEvent pointerEvent)
        {
            if (!_isActive || _shocking) return false;
            _shocking = true;
            if (_collider != null) _collider.enabled = false;
            StartCoroutine(ShockAllAnimals());
            return true;
        }

        private IEnumerator ShockAllAnimals()
        {
            GameEvents.OnSfxRequested?.Invoke(SfxType.HindranceActivate);
            var targets = new List<Animal>(ActiveAnimalRegistry.All.Count);
            for (int i = 0; i < ActiveAnimalRegistry.All.Count; i++)
            {
                Animal animal = ActiveAnimalRegistry.All[i];
                if (animal == null || animal.IsCollected || !animal.gameObject.activeInHierarchy) continue;
                targets.Add(animal);
                CreateBolt(animal.transform);
                SpriteRenderer renderer = animal.GetComponent<SpriteRenderer>();
                if (renderer != null) renderer.color = new Color(0.45f, 0.95f, 1f, 1f);
            }

            float elapsed = 0f;
            while (_isActive && elapsed < _shockDuration)
            {
                elapsed += Time.deltaTime;
                UpdateBolts(targets);
                if (_sr != null) _sr.color = Color.Lerp(Color.white, Color.cyan,
                    Mathf.PingPong(elapsed * 12f, 1f));
                yield return null;
            }

            for (int i = targets.Count - 1; i >= 0; i--)
            {
                Animal animal = targets[i];
                if (animal == null || animal.IsCollected) continue;
                SpriteRenderer renderer = animal.GetComponent<SpriteRenderer>();
                if (renderer != null) renderer.color = Color.white;
                animal.Despawn();
            }
            Deactivate();
        }

        protected override void OnDeactivate()
        {
            _shocking = false;
            if (_sr != null) _sr.color = Color.white;
            for (int i = _bolts.Count - 1; i >= 0; i--)
                if (_bolts[i] != null) Destroy(_bolts[i]);
            _bolts.Clear();
        }

        private void CreateBolt(Transform target)
        {
            var bolt = new GameObject("Electric Shock");
            bolt.transform.SetParent(transform, false);
            LineRenderer line = bolt.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 5;
            line.widthMultiplier = 0.055f;
            line.numCapVertices = 3;
            line.sortingOrder = 34;
            line.material = GetLightningMaterial();
            line.startColor = new Color(0.25f, 0.95f, 1f, 1f);
            line.endColor = new Color(0.85f, 0.55f, 1f, 0.85f);
            bolt.AddComponent<ShockBoltTarget>().Target = target;
            _bolts.Add(bolt);
        }

        private void UpdateBolts(List<Animal> targets)
        {
            for (int i = 0; i < _bolts.Count; i++)
            {
                if (_bolts[i] == null) continue;
                LineRenderer line = _bolts[i].GetComponent<LineRenderer>();
                Transform target = _bolts[i].GetComponent<ShockBoltTarget>()?.Target;
                if (line == null || target == null) continue;
                Vector3 start = transform.position;
                Vector3 end = target.position;
                for (int p = 0; p < 5; p++)
                {
                    float t = p / 4f;
                    Vector3 point = Vector3.Lerp(start, end, t);
                    if (p > 0 && p < 4) point += (Vector3)Random.insideUnitCircle * 0.12f;
                    line.SetPosition(p, point);
                }
            }
        }

        private Material GetLightningMaterial()
        {
            if (_lightningMaterial != null) return _lightningMaterial;
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            _lightningMaterial = new Material(shader) { name = "Jellyfish Lightning Runtime" };
            return _lightningMaterial;
        }

        private void OnDestroy()
        {
            if (_lightningMaterial != null) Destroy(_lightningMaterial);
        }

        private IEnumerator RetireAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (_isActive) Deactivate();
        }

        private void FitVisualAndCollider(float worldSize)
        {
            if (_sr == null || _sr.sprite == null) return;
            Vector2 size = _sr.sprite.bounds.size;
            float largest = Mathf.Max(size.x, size.y);
            if (largest > 0.001f) transform.localScale = Vector3.one * (worldSize / largest);
            if (_collider is CircleCollider2D circle)
            {
                circle.isTrigger = true;
                circle.radius = largest * 0.5f;
                circle.offset = _sr.sprite.bounds.center;
            }
        }

        private void MoveIntoPlayArea(float viewportY)
        {
            if (Camera.main == null) return;
            float depth = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
            Vector3 viewport = Camera.main.WorldToViewportPoint(transform.position);
            viewport.x = Mathf.Clamp(viewport.x, 0.15f, 0.85f);
            viewport.y = viewportY;
            viewport.z = depth;
            Vector3 world = Camera.main.ViewportToWorldPoint(viewport);
            world.z = 0f;
            transform.position = world;
        }

        private static Sprite FirstSprite(string path)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>(path);
            return sprites.Length > 0 ? sprites[0] : null;
        }

        private sealed class ShockBoltTarget : MonoBehaviour { public Transform Target; }
    }
}
