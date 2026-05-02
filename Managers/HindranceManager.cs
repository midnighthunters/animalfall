using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnimalFall.Core.Hindrances;
using AnimalFall.Core.Levels;

namespace AnimalFall.Managers
{
    public class HindranceManager : MonoBehaviour
    {
        public static HindranceManager Instance { get; private set; }

        [SerializeField] private HindranceRegistry registry;
        [SerializeField] private Transform hindranceContainer;

        private LevelData currentLevel;
        private List<HindranceData> availablePool;
        private readonly List<IHindrance> activeHindrances = new List<IHindrance>();
        private bool spawning;
        private HindranceContext cachedContext;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void InitForLevel(LevelData level)
        {
            StopAll();
            currentLevel = level;

            if (registry != null)
                registry.Initialize();

            if (level.enabledHindrances != null && level.enabledHindrances.Length > 0)
                availablePool = registry.GetHindrancesForLevel(level.levelNumber, level.enabledHindrances);
            else
                availablePool = new List<HindranceData>();

            cachedContext = HindranceContext.Create();
        }

        public void StartSpawning()
        {
            if (availablePool == null || availablePool.Count == 0) return;
            spawning = true;
            StartCoroutine(HindranceSpawnLoop());
        }

        public void StopAll()
        {
            spawning = false;
            StopAllCoroutines();

            foreach (var h in activeHindrances)
            {
                if (h != null) h.Deactivate();
            }
            activeHindrances.Clear();
        }

        public List<IHindrance> GetActiveHindrances() => activeHindrances;

        public bool IsHindranceActive(HindranceType type)
        {
            foreach (var h in activeHindrances)
            {
                if (h != null && h.Type == type && h.IsActive)
                    return true;
            }
            return false;
        }

        private IEnumerator HindranceSpawnLoop()
        {
            float initialDelay = currentLevel.hindranceInitialDelay > 0
                ? currentLevel.hindranceInitialDelay : 5f;
            yield return new WaitForSeconds(initialDelay);

            while (spawning)
            {
                activeHindrances.RemoveAll(h => h == null || !h.IsActive);

                int maxOnScreen = currentLevel.maxHindrancesOnScreen > 0
                    ? currentLevel.maxHindrancesOnScreen : 3;

                if (activeHindrances.Count < maxOnScreen)
                    SpawnRandomHindrance();

                float interval = currentLevel.hindranceSpawnInterval > 0
                    ? currentLevel.hindranceSpawnInterval : 4f;
                interval += Random.Range(-1f, 1f);
                yield return new WaitForSeconds(Mathf.Max(1f, interval));
            }
        }

        private void SpawnRandomHindrance()
        {
            if (availablePool == null || availablePool.Count == 0) return;

            HindranceData data = registry.PickWeightedRandom(availablePool);
            if (data == null) return;

            Transform parent = hindranceContainer != null ? hindranceContainer : transform;
            IHindrance hindrance = HindranceFactory.CreateAtRandomScreenTop(data, parent);

            if (hindrance != null)
            {
                hindrance.Activate(cachedContext);
                activeHindrances.Add(hindrance);
            }
        }
    }
}
