using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnimalFall.Core.Hindrances.EnvironmentMods
{
    /// <summary>
    /// Sends a bunch of cloud sprites in a staggered wave from the left edge
    /// of the playfield to the right edge.
    /// </summary>
    public sealed class CloudWaveHindrance : HindranceBase
    {
        [SerializeField] private Sprite _cloudSprite;
        [SerializeField, Min(0.5f)] private float _activeDuration = 8f;
        [SerializeField, Min(0.25f)] private float _cloudSpeed = 2.15f;
        [SerializeField, Min(0.05f)] private float _spawnInterval = 0.12f;
        [SerializeField, Min(1)] private int _cloudCount = 12;
        [SerializeField, Min(0.2f)] private float _cloudWorldSize = 1.65f;
        [SerializeField, Min(0f)] private float _verticalSpread = 3.6f;

        private readonly List<CloudEntry> _clouds = new List<CloudEntry>(12);
        private float _leftEdge;
        private float _rightEdge;
        private float _baseY;
        private float _waveStartTime;

        public override HindranceType Type => HindranceType.CloudWave;

#if UNITY_EDITOR
        public void EditorConfigure(Sprite cloudSprite)
        {
            _cloudSprite = cloudSprite;
            _spawnInterval = 0.12f;
            _cloudCount = 12;
            _cloudWorldSize = 1.65f;
            _verticalSpread = 3.6f;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        protected override void OnActivate()
        {
            CleanupClouds();
            _sr.enabled = false;
            ResolveBounds(out _leftEdge, out _rightEdge, out _baseY);
            _waveStartTime = Time.time;
            StartCoroutine(SpawnWaveRoutine());
        }

        protected override void OnDeactivate()
        {
            CleanupClouds();
        }

        private void Update()
        {
            if (!_isActive || _clouds.Count == 0) return;

            float deltaTime = Time.deltaTime;
            float now = Time.time;
            for (int i = _clouds.Count - 1; i >= 0; i--)
            {
                CloudEntry entry = _clouds[i];
                if (entry == null || entry.cloud == null)
                {
                    _clouds.RemoveAt(i);
                    continue;
                }

                Vector3 position = entry.cloud.transform.position;
                position.x += entry.speed * deltaTime;
                position.y = entry.baseY + Mathf.Sin((now - _waveStartTime) * 1.35f + entry.phase) * 0.07f;
                entry.cloud.transform.position = position;

                if (position.x > _rightEdge + 1.5f)
                {
                    Destroy(entry.cloud);
                    _clouds.RemoveAt(i);
                }
            }
        }

        private IEnumerator SpawnWaveRoutine()
        {
            float finishAt = Time.time + _activeDuration;
            Sprite sprite = _cloudSprite != null ? _cloudSprite : Resources.Load<Sprite>("icons/hindrances/cloud");
            if (sprite == null)
            {
                Debug.LogWarning("[CloudWave] The cloud sprite is missing from Resources.");
                Deactivate();
                yield break;
            }

            for (int i = 0; i < _cloudCount && _isActive; i++)
            {
                CreateCloud(sprite, i);
                yield return new WaitForSeconds(_spawnInterval);
            }

            while (_isActive && Time.time < finishAt)
                yield return null;

            if (_isActive) Deactivate();
        }

        private void CreateCloud(Sprite sprite, int index)
        {
            var cloud = new GameObject($"CloudWave_Cloud_{index + 1:00}");
            cloud.transform.SetParent(transform, true);
            // A golden-ratio lane sequence keeps every cloud on a distinct
            // vertical route instead of looking like a rigid diagonal row.
            float lane = Mathf.Repeat(0.16f + index * 0.618034f, 1f);
            float y = _baseY + ((lane - 0.5f) * _verticalSpread);
            float stagger = index * 0.26f;
            cloud.transform.position = new Vector3(_leftEdge - 0.8f - stagger, y, transform.position.z);

            var renderer = cloud.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 29 + (index % 3);
            float size = sprite.bounds.size.x > 0.01f ? _cloudWorldSize / sprite.bounds.size.x : 1f;
            cloud.transform.localScale = Vector3.one * size * (0.88f + (index % 3) * 0.08f);

            _clouds.Add(new CloudEntry
            {
                cloud = cloud,
                baseY = y,
                phase = index * 0.65f,
                speed = _cloudSpeed * (0.92f + (index % 4) * 0.045f)
            });
        }

        private void ResolveBounds(out float left, out float right, out float centerY)
        {
            Camera camera = Camera.main;
            if (camera != null)
            {
                float distance = Mathf.Abs(camera.transform.position.z - transform.position.z);
                Vector3 leftPoint = camera.ViewportToWorldPoint(new Vector3(0f, 0.5f, distance));
                Vector3 rightPoint = camera.ViewportToWorldPoint(new Vector3(1f, 0.5f, distance));
                left = leftPoint.x;
                right = rightPoint.x;
                centerY = Mathf.Lerp(leftPoint.y, rightPoint.y, 0.5f);
                return;
            }

            left = -6f;
            right = 6f;
            centerY = transform.position.y;
        }

        private void CleanupClouds()
        {
            for (int i = _clouds.Count - 1; i >= 0; i--)
            {
                if (_clouds[i] != null && _clouds[i].cloud != null)
                    Destroy(_clouds[i].cloud);
            }
            _clouds.Clear();
        }

        private sealed class CloudEntry
        {
            public GameObject cloud;
            public float baseY;
            public float phase;
            public float speed;
        }
    }
}
