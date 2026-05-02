using UnityEngine;
using AnimalFall.Core.Goals;
using AnimalFall.Core.Hindrances;

namespace AnimalFall.Core.Levels
{
    [CreateAssetMenu(fileName = "LevelDatabase", menuName = "AnimalFall/Level Database")]
    public class LevelDatabase : ScriptableObject
    {
        [SerializeField] private LevelData[] levels;

        public LevelData[] Levels => levels;
        public int TotalLevels => levels != null ? levels.Length : 0;

        public LevelData GetLevel(int index)
        {
            if (levels == null || index < 0 || index >= levels.Length) return null;
            return levels[index];
        }

#if UNITY_EDITOR
        [ContextMenu("Generate 50 Levels (Editor Only)")]
        public void Generate50Levels()
        {
            levels = new LevelData[50];

            for (int i = 0; i < 50; i++)
            {
                int levelNum = i + 1;
                LevelData ld = CreateInstance<LevelData>();
                ld.name = $"Level_{levelNum:D2}";
                ld.levelNumber = levelNum;

                float baseDifficulty = 1f + (i * 0.06f);
                ld.timeLimit = Mathf.Max(20f, 60f - i * 0.5f);
                ld.spawnInterval = Mathf.Max(0.25f, 0.8f - i * 0.01f);
                ld.spawnVariance = 0.1f + i * 0.003f;
                ld.maxOnScreen = Mathf.Min(15, 5 + i / 5);
                ld.rewardCoins = 20 + i * 5;

                ld.isMegaLevel = (levelNum % 5 == 0);
                ld.enableBombs = (levelNum >= 3);
                ld.enableShielded = (levelNum >= 6);
                ld.enableDecoys = (levelNum >= 8);

                ld.wrongTapTimePenalty = 1f + i * 0.05f;
                ld.wrongTapScorePenalty = 30 + i;
                ld.bombTimePenalty = 3f + i * 0.1f;
                ld.bombScorePenalty = 50 + i * 2;

                ld.enabledHindrances = GetHindrancesForLevel(levelNum);
                ld.hindranceSpawnInterval = Mathf.Max(2f, 6f - i * 0.08f);
                ld.hindranceInitialDelay = Mathf.Max(2f, 8f - i * 0.12f);
                ld.maxHindrancesOnScreen = Mathf.Min(5, 1 + i / 8);

                levels[i] = ld;
            }

            Debug.Log("[LevelDatabase] Generated 50 levels.");
        }

        private static HindranceType[] GetHindrancesForLevel(int level)
        {
            var list = new System.Collections.Generic.List<HindranceType>();

            if (level >= 5) { list.Add(HindranceType.Bomb); list.Add(HindranceType.FakeAnimal); }
            if (level >= 7) { list.Add(HindranceType.AlarmClock); list.Add(HindranceType.KnightHelmet); }
            if (level >= 10) { list.Add(HindranceType.PoisonVial); list.Add(HindranceType.ThiefBird); list.Add(HindranceType.GhostAnimal); }
            if (level >= 12) { list.Add(HindranceType.TitaniumArmor); list.Add(HindranceType.ZigZagFlyer); }
            if (level >= 15) { list.Add(HindranceType.BubbleShield); list.Add(HindranceType.Teleporter); list.Add(HindranceType.InkSquid); }
            if (level >= 18) { list.Add(HindranceType.HeavyWeight); list.Add(HindranceType.ShrinkingAnimal); list.Add(HindranceType.StormCloud); }
            if (level >= 20) { list.Add(HindranceType.IceCube); list.Add(HindranceType.Flashbang); list.Add(HindranceType.Tornado); }
            if (level >= 23) { list.Add(HindranceType.PairedAnimal); list.Add(HindranceType.FallingLeaves); list.Add(HindranceType.WindGust); }
            if (level >= 25) { list.Add(HindranceType.ZeroGravity); list.Add(HindranceType.BlackHole); list.Add(HindranceType.BouncingBorder); }
            if (level >= 28) { list.Add(HindranceType.LaserBeam); list.Add(HindranceType.DecoyChest); }
            if (level >= 30) { list.Add(HindranceType.MagnetTrap); list.Add(HindranceType.MirrorMode); }
            if (level >= 35) { list.Add(HindranceType.CursedSkull); list.Add(HindranceType.StoneGargoyle); }

            return list.ToArray();
        }
#endif
    }
}
