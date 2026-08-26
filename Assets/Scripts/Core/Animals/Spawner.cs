// Spawner — continuous wave of animals for the duration of the level
using System.Collections;
using UnityEngine;
using DG.Tweening;
using AnimalFall.Core;
using AnimalFall.Data;

namespace AnimalFall.Core.Animals
{
    public class Spawner : MonoBehaviour
    {
        public static Spawner Instance { get; private set; }

        [SerializeField, Tooltip("Animal prefab used for all spawns.")]
        private GameObject _animalPrefab;

        [SerializeField, Tooltip("Container transform for pooled animals.")]
        private Transform _animalContainer;

        [SerializeField, Tooltip("Spawn point positions (6 recommended).")]
        private Transform[] _spawnPoints;

        [Header("Spawn Spacing")]
        [SerializeField, Range(3, 8), Tooltip("Fallback horizontal lanes used when no spawn points are assigned.")]
        private int _spawnLaneCount = 6;

        [SerializeField, Min(0.5f), Tooltip("Minimum world-space distance between newly spawned animals.")]
        private float _spawnClearance = 1.65f;

        private LevelData    _level;
        private AnimalData[] _cachedPool;
        private int          _cachedPoolLen;
        private bool         _spawning;
        private int          _activeCount;
        private int          _nextSpawnLane;
        private Coroutine    _loop;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // Prefer the scene-local spawner
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Called by LevelManager/GameManager before spawning begins.</summary>
        public void Setup(LevelData level)
        {
            ActiveAnimalRegistry.Clear();
            _level = level;
            _cachedPoolLen = (level.SpawnPool != null) ? level.SpawnPool.Length : 0;

            if (_cachedPoolLen == 0)
            {
                Debug.LogError("[Spawner] Setup: SpawnPool is empty or null. Spawning halted.");
                return;
            }

            _cachedPool = new AnimalData[_cachedPoolLen];
            for (int i = 0; i < _cachedPoolLen; i++)
                _cachedPool[i] = level.SpawnPool[i];

            _activeCount = 0;
            _nextSpawnLane = 0;
        }

        public void StartSpawning()
        {
            if (_level == null) { Debug.LogError("[Spawner] StartSpawning called before Setup."); return; }
            if (_cachedPoolLen == 0) return;

            StopSpawning();
            _spawning = true;
            _loop = StartCoroutine(SpawnLoop());
            Debug.Log($"[Spawner] Continuous wave started. interval={_level.SpawnInterval}s maxOnScreen={_level.MaxOnScreen}");
        }

        public void StopSpawning()
        {
            _spawning = false;
            if (_loop != null)
            {
                StopCoroutine(_loop);
                _loop = null;
            }
            StopAllCoroutines();
        }

        /// <summary>Called by Animal when it returns to the pool.</summary>
        public void OnAnimalReturned()
        {
            _activeCount = Mathf.Max(0, _activeCount - 1);
        }

        public int ActiveCount => _activeCount;

        public bool ContainsSpecies(AnimalSpecies species)
        {
            for (int i = 0; i < _cachedPoolLen; i++)
                if (_cachedPool[i] != null && _cachedPool[i].species == species) return true;
            return false;
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private IEnumerator SpawnLoop()
        {
            // Seed a few immediately so the screen isn't empty at GO
            int burst = Mathf.Min(3, Mathf.Max(1, _level.MaxOnScreen));
            for (int i = 0; i < burst && _spawning; i++)
            {
                if (_activeCount < _level.MaxOnScreen)
                    SpawnOne();
            }

            while (_spawning)
            {
                float interval = _level.SpawnInterval;
                if (_level.SpawnVariance > 0f)
                    interval += Random.Range(-_level.SpawnVariance, _level.SpawnVariance);
                interval = Mathf.Max(0.15f, interval);

                // Keep filling up to max — spawn every tick when under capacity
                if (_activeCount < _level.MaxOnScreen)
                {
                    SpawnOne();

                    // If still under capacity, spawn a second animal for a denser wave
                    if (_activeCount < _level.MaxOnScreen && Random.value < 0.35f)
                        SpawnOne();
                }

                yield return new WaitForSeconds(interval);
            }
        }

        private void SpawnOne()
        {
            if (_animalPrefab == null)
            {
                Debug.LogError("[Spawner] _animalPrefab is null.");
                return;
            }

            if (ObjectPooler.Instance == null)
            {
                Debug.LogError("[Spawner] ObjectPooler.Instance is null.");
                return;
            }

            AnimalData data = ChooseAnimalData();
            if (data == null) return;

            Vector3 spawnPos = FindSpacedSpawnPosition();

            GameObject obj = ObjectPooler.Instance.SpawnFromPool(
                _animalPrefab, spawnPos, Quaternion.identity, _animalContainer);

            if (obj == null) return;

            Animal animal = obj.GetComponent<Animal>();
            if (animal == null)
            {
                ObjectPooler.Instance.ReturnToPool(obj);
                return;
            }

            animal.SetupForPool(data, _level);
            _activeCount++;

            // SetupForPool computes a per-species normalised scale so every animal
            // renders at a consistent on-screen size. Animate a spawn pop up to it.
            float target = animal.CurrentScale;
            obj.transform.localScale = Vector3.one * (target * 0.15f);
            DOTween.Kill(obj);
            obj.transform.DOScale(target, 0.28f).SetEase(Ease.OutBack).SetId(obj);
        }

        private Vector3 FindSpacedSpawnPosition()
        {
            if (_spawnPoints != null && _spawnPoints.Length > 0)
            {
                for (int i = 0; i < _spawnPoints.Length; i++)
                {
                    int index = (_nextSpawnLane + i) % _spawnPoints.Length;
                    Transform point = _spawnPoints[index];
                    if (point == null) continue;
                    Vector3 candidate = point.position;
                    if (!IsSpawnAreaClear(candidate)) continue;
                    _nextSpawnLane = (index + 1) % _spawnPoints.Length;
                    return candidate;
                }
            }

            Camera cam = Camera.main;
            float left = transform.position.x - 3f;
            float right = transform.position.x + 3f;
            float top = transform.position.y + 5f;
            if (cam != null)
            {
                float z = Mathf.Abs(cam.transform.position.z);
                left = cam.ViewportToWorldPoint(new Vector3(0f, 1f, z)).x + 0.65f;
                right = cam.ViewportToWorldPoint(new Vector3(1f, 1f, z)).x - 0.65f;
                top = cam.ViewportToWorldPoint(new Vector3(0.5f, 1f, z)).y + 0.65f;
            }

            int lanes = Mathf.Max(3, _spawnLaneCount);
            Vector3 fallback = new Vector3((left + right) * 0.5f, top, 0f);
            for (int i = 0; i < lanes; i++)
            {
                int lane = (_nextSpawnLane + i) % lanes;
                float t = lanes == 1 ? 0.5f : lane / (float)(lanes - 1);
                Vector3 candidate = new Vector3(Mathf.Lerp(left, right, t), top, 0f);
                fallback = candidate;
                if (!IsSpawnAreaClear(candidate)) continue;
                _nextSpawnLane = (lane + 1) % lanes;
                return candidate;
            }

            // All lanes are occupied: place the next animal slightly above the row
            // instead of stacking it directly on another animal.
            _nextSpawnLane = (_nextSpawnLane + 1) % lanes;
            fallback.y += _spawnClearance;
            return fallback;
        }

        private bool IsSpawnAreaClear(Vector3 candidate)
        {
            float minDistanceSqr = _spawnClearance * _spawnClearance;
            var animals = ActiveAnimalRegistry.All;
            for (int i = 0; i < animals.Count; i++)
            {
                Animal animal = animals[i];
                if (animal == null || !animal.gameObject.activeInHierarchy || animal.IsCollected) continue;
                if ((animal.transform.position - candidate).sqrMagnitude < minDistanceSqr) return false;
            }
            return true;
        }

        private AnimalData ChooseAnimalData()
        {
            if (_cachedPool == null || _cachedPoolLen <= 0)
            {
                Debug.LogError("[Spawner] ChooseAnimalData: pool is empty.");
                return null;
            }

            int idx = Random.Range(0, _cachedPoolLen);
            return _cachedPool[idx];
        }
    }
}
