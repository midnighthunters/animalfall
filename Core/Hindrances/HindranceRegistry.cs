using System.Collections.Generic;
using UnityEngine;

namespace AnimalFall.Core.Hindrances
{
    [CreateAssetMenu(fileName = "HindranceRegistry", menuName = "AnimalFall/Hindrance Registry")]
    public class HindranceRegistry : ScriptableObject
    {
        [SerializeField] private HindranceData[] allHindrances;

        private Dictionary<HindranceType, HindranceData> lookup;

        public void Initialize()
        {
            lookup = new Dictionary<HindranceType, HindranceData>();
            if (allHindrances == null) return;

            foreach (var h in allHindrances)
            {
                if (h != null && !lookup.ContainsKey(h.type))
                    lookup[h.type] = h;
            }
        }

        public HindranceData GetData(HindranceType type)
        {
            if (lookup == null) Initialize();
            return lookup != null && lookup.TryGetValue(type, out var data) ? data : null;
        }

        public List<HindranceData> GetHindrancesForLevel(int levelNumber, HindranceType[] enabledTypes)
        {
            if (lookup == null) Initialize();
            var result = new List<HindranceData>();

            if (enabledTypes == null) return result;

            foreach (var type in enabledTypes)
            {
                var data = GetData(type);
                if (data != null && levelNumber >= data.minLevel)
                    result.Add(data);
            }

            return result;
        }

        public HindranceData PickWeightedRandom(List<HindranceData> pool)
        {
            if (pool == null || pool.Count == 0) return null;

            float totalWeight = 0f;
            foreach (var h in pool)
                totalWeight += h.spawnWeight;

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var h in pool)
            {
                cumulative += h.spawnWeight;
                if (roll <= cumulative)
                    return h;
            }

            return pool[pool.Count - 1];
        }
    }
}
