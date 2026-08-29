using UnityEngine;

namespace AnimalFall.MegaShooter
{
    /// <summary>
    /// Procedural, pooled combat flash used for every mega-shooter effect
    /// (muzzle fire, projectile impacts, explosions). It is built entirely in code
    /// from additive-looking sprite layers so it renders correctly under URP 2D:
    ///   • a bright core that pops and fades,
    ///   • a ring of sparks that burst outward,
    ///   • an expanding shock ring.
    /// The old version was a single sprite that simply scaled and faded.
    /// </summary>
    public sealed class MegaTimedPoolEffect : MonoBehaviour, IMegaPoolable
    {
        private const int SparkCount = 8;

        [SerializeField, Min(0.05f)] private float _duration = 0.5f;

        private SpriteRenderer _core;
        private SpriteRenderer _ring;
        private SpriteRenderer[] _sparks;
        private Vector2[] _sparkDirections;
        private Color _color = Color.white;
        private float _remaining;
        private float _scale = 1f;
        private bool _built;

        private void Awake() => Build();

        private void Build()
        {
            if (_built) return;
            _core = GetComponent<SpriteRenderer>();
            if (_core == null) _core = gameObject.AddComponent<SpriteRenderer>();

            Sprite sprite = _core.sprite;
            int order = _core.sortingOrder;
            int layer = _core.sortingLayerID;

            _ring = CreateLayer("ShockRing", sprite, layer, order - 1);
            _sparks = new SpriteRenderer[SparkCount];
            _sparkDirections = new Vector2[SparkCount];
            for (int i = 0; i < SparkCount; i++)
            {
                _sparks[i] = CreateLayer($"Spark{i}", sprite, layer, order);
                float angle = (360f / SparkCount) * i + (i % 2 == 0 ? 11f : -7f);
                _sparkDirections[i] = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            }
            _built = true;
        }

        private SpriteRenderer CreateLayer(string layerName, Sprite sprite, int sortingLayer, int order)
        {
            var go = new GameObject(layerName);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerID = sortingLayer;
            renderer.sortingOrder = order;
            return renderer;
        }

        public void Configure(Color color, float duration, float scale)
        {
            Build();
            _color = color;
            _duration = Mathf.Max(0.06f, duration);
            _remaining = _duration;
            _scale = Mathf.Max(0.05f, scale);
            // The root stays at unit scale; each layer is sized in world units so the
            // burst geometry is predictable regardless of the spawn scale request.
            transform.localScale = Vector3.one;
            ApplyFrame(0f);
        }

        public void OnMegaSpawned()
        {
            Build();
            _remaining = _duration;
            ApplyFrame(0f);
        }

        private void Update()
        {
            _remaining -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(_remaining / Mathf.Max(0.05f, _duration));
            ApplyFrame(t);
            if (_remaining <= 0f) MegaObjectPools.Instance?.Despawn(gameObject);
        }

        private void ApplyFrame(float t)
        {
            if (!_built) return;
            float ease = 1f - (1f - t) * (1f - t); // ease-out

            // Bright core: snaps to full size, then fades quickly.
            float coreScale = _scale * Mathf.Lerp(0.5f, 1.65f, Mathf.Clamp01(t * 2.4f));
            SetLayer(_core, Vector2.zero, coreScale,
                Color.Lerp(_color, Color.white, 0.55f), Mathf.Pow(1f - t, 1.7f));

            // Sparks fly outward and shrink as they fade.
            float distance = _scale * Mathf.Lerp(0.04f, 1.75f, ease);
            float sparkScale = _scale * 0.5f * (1f - t * 0.85f);
            float sparkAlpha = Mathf.Pow(1f - t, 1.25f);
            Color sparkColor = Color.Lerp(_color, Color.white, 0.3f);
            for (int i = 0; i < _sparks.Length; i++)
                SetLayer(_sparks[i], _sparkDirections[i] * distance, sparkScale, sparkColor, sparkAlpha);

            // Expanding shock ring trailing behind everything.
            float ringScale = _scale * Mathf.Lerp(0.35f, 2.6f, ease);
            SetLayer(_ring, Vector2.zero, ringScale, _color, (1f - t) * 0.45f);
        }

        private static void SetLayer(SpriteRenderer renderer, Vector2 localPosition, float scale, Color rgb, float alpha)
        {
            if (renderer == null) return;
            renderer.transform.localPosition = localPosition;
            renderer.transform.localScale = Vector3.one * Mathf.Max(0.001f, scale);
            renderer.color = new Color(rgb.r, rgb.g, rgb.b, Mathf.Clamp01(alpha));
        }

        public void OnMegaDespawned()
        {
            transform.localScale = Vector3.one;
            _scale = 1f;
        }
    }
}
