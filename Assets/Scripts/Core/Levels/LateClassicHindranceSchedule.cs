using System;
using System.Collections.Generic;
using AnimalFall.Core.Hindrances;

namespace AnimalFall.Data
{
    /// <summary>Deterministic normal-level schedule for the classic hazards used from levels 40-100.</summary>
    public static class LateClassicHindranceSchedule
    {
        public const int FirstLevel = 40;
        public const int LastLevel = 100;

        public static readonly HindranceType[] Types =
        {
            HindranceType.Jellyfish,
            HindranceType.Laser,
            HindranceType.Eagle,
            HindranceType.WoodenPig,
            HindranceType.Tornado,
            HindranceType.Portal,
            HindranceType.Fan,
            HindranceType.BatSwarm
        };

        private static readonly int[] UnlockLevels = { 40, 43, 46, 48, 51, 53, 56, 59 };

        public static bool IsLateClassicType(HindranceType type)
            => Array.IndexOf(Types, type) >= 0;

        public static HindranceType[] BuildTypes(int levelNumber)
        {
            // Every fifth level is Mega Shooter and must never receive normal hindrances.
            if (levelNumber < FirstLevel || levelNumber > LastLevel || levelNumber % 5 == 0)
                return Array.Empty<HindranceType>();

            var unlocked = new List<HindranceType>(Types.Length);
            for (int i = 0; i < Types.Length; i++)
                if (levelNumber >= UnlockLevels[i]) unlocked.Add(Types[i]);

            if (unlocked.Count == 0) return Array.Empty<HindranceType>();

            int desired = levelNumber < 60 ? 2 : levelNumber < 80 ? 3 : 4;
            desired = Math.Min(desired, unlocked.Count);
            var result = new HindranceType[desired];
            int start = ((levelNumber - FirstLevel) * 3) % unlocked.Count;
            for (int i = 0; i < desired; i++)
                result[i] = unlocked[(start + i) % unlocked.Count];
            return result;
        }

        public static HindranceConfig[] BuildConfigs(int levelNumber)
        {
            HindranceType[] types = BuildTypes(levelNumber);
            var configs = new HindranceConfig[types.Length];
            for (int i = 0; i < types.Length; i++)
            {
                configs[i] = new HindranceConfig
                {
                    type = types[i],
                    weight = i == 0 ? 1.35f : 1f,
                    initialDelay = i * 0.35f
                };
            }
            return configs;
        }
    }
}
