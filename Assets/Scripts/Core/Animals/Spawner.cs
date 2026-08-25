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

        private LevelData    _level;
        private AnimalData[] _cachedPool;
        private int          _cachedPoolLen;
        private bool         _spawning;
        private int          _activeCount;
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

            int spIdx = (_spawnPoints != null && _spawnPoints.Length > 0)
                ? Random.Range(0, _spawnPoints.Length)
                : 0;

            Vector3 spawnPos = (_spawnPoints != null && _spawnPoints.Length > spIdx)
                ? _spawnPoints[spIdx].position
                : transform.position + Vector3.up * 5f;

            // Slight X jitter so animals don't stack perfectly
            spawnPos.x += Random.Range(-0.15f, 0.15f);

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
