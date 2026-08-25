using UnityEngine;

namespace AnimalFall.MegaShooter
{
    public sealed class MegaTimedPoolEffect : MonoBehaviour, IMegaPoolable
    {
        [SerializeField, Min(0.05f)] private float _duration = 0.5f;
        private float _remaining;
        private float _configuredScale = 1f;
        private SpriteRenderer _renderer;

        private void Awake() => _renderer = GetComponent<SpriteRenderer>();

        public void Configure(Color color, float duration, float scale)
        {
            _duration = Mathf.Max(0.05f, duration);
            _remaining = _duration;
            _configuredScale = Mathf.Max(0.05f, scale);
            transform.localScale = Vector3.one * _configuredScale;
            if (_renderer != null) _renderer.color = color;
        }

        public void OnMegaSpawned() => _remaining = _duration;

        private void Update()
        {
            _remaining -= Time.deltaTime;
            float normalized = 1f - Mathf.Clamp01(_remaining / Mathf.Max(0.05f, _duration));
            transform.localScale = Vector3.one * (_configuredScale * Mathf.Lerp(0.75f, 1.9f, normalized));
            if (_renderer != null)
            {
                Color color = _renderer.color;
                color.a = Mathf.Pow(1f - normalized, 1.4f);
                _renderer.color = color;
            }
            if (_remaining <= 0f) MegaObjectPools.Instance?.Despawn(gameObject);
        }

        public void OnMegaDespawned()
        {
            transform.localScale = Vector3.one;
            _configuredScale = 1f;
        }
    }
}
