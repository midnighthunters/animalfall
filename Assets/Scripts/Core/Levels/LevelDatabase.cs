// LevelDatabase — ordered, dynamically-sized level container.
using UnityEngine;
using AnimalFall.Core.Hindrances;

namespace AnimalFall.Data
{
    [CreateAssetMenu(fileName = "LevelDatabase", menuName = "AnimalFall/Level Database")]
    public class LevelDatabase : ScriptableObject
    {
        [Tooltip("Ordered level slots. Missing normal levels are intentionally represented by null entries.")]
        [SerializeField] private LevelData[] _levels;

        public int TotalLevels => _levels != null ? _levels.Length : 0;
        public LevelData[] Levels => _levels;

        /// <summary>Returns the LevelData at the given zero-based index (0-49).</summary>
        public LevelData GetLevel(int zeroBasedIndex)
        {
            if (_levels == null || zeroBasedIndex < 0 || zeroBasedIndex >= _levels.Length)
            {
                Debug.LogError($"[LevelDatabase] Index {zeroBasedIndex} out of range (total={TotalLevels}).");
                return null;
            }
            return _levels[zeroBasedIndex];
        }

        public LevelData GetLevelOrNull(int zeroBasedIndex)
            => _levels != null && zeroBasedIndex >= 0 && zeroBasedIndex < _levels.Length
                ? _levels[zeroBasedIndex]
                : null;

#if UNITY_EDITOR
        public void SetLevelsPreservingExisting(LevelData[] levels)
        {
            _levels = levels;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [ContextMenu("Generate & Save 50 Levels")]
        public void GenerateAndSave50Levels()
        {
            const string FOLDER = "Assets/Levels/LevelData";
            if (!UnityEditor.AssetDatabase.IsValidFolder(FOLDER))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets/Levels", "LevelData");
            }

            _levels = new LevelData[50];

            for (int i = 0; i < 50; i++)
            {
                int n = i + 1; // 1-based level number

                // ── Difficulty band interpolation ──────────────────────────────
                float timeLimit;
                float spawnInterval;
                int maxOnScreen;

                if (i < 10) // Intro: L1-10
                {
                    float t = i / 9f;
                    timeLimit     = Mathf.Lerp(60f, 45f, t);
                    spawnInterval = Mathf.Lerp(0.9f, 0.7f, t);
                    maxOnScreen   = Mathf.RoundToInt(Mathf.Lerp(5, 8, t));
                }
                else if (i < 25) // Rising: L11-25
                {
                    float t = (i - 10) / 14f;
                    timeLimit     = Mathf.Lerp(45f, 35f, t);
                    spawnInterval = Mathf.Lerp(0.7f, 0.5f, t);
                    maxOnScreen   = Mathf.RoundToInt(Mathf.Lerp(8, 11, t));
                }
                else if (i < 40) // Challenge: L26-40
                {
                    float t = (i - 25) / 14f;
                    timeLimit     = Mathf.Lerp(35f, 28f, t);
                    spawnInterval = Mathf.Lerp(0.5f, 0.35f, t);
                    maxOnScreen   = Mathf.RoundToInt(Mathf.Lerp(11, 13, t));
                }
                else // Expert: L41-50
                {
                    float t = (i - 40) / 9f;
                    timeLimit     = Mathf.Lerp(28f, 20f, t);
                    spawnInterval = Mathf.Lerp(0.35f, 0.25f, t);
                    maxOnScreen   = Mathf.RoundToInt(Mathf.Lerp(13, 15, t));
                }

                bool isMega = (n % 5 == 0);
                if (isMega) timeLimit += 15f;

                // ── Chapter theme ──────────────────────────────────────────────
                string chapterTheme = GetChapterTheme(n);

                // ── Penalties ─────────────────────────────────────────────────
                float wrongTapPenalty = Mathf.Round((1.0f + (3.0f / 49f) * (n - 1)) * 100f) / 100f;
                float bombPenalty     = Mathf.Round((3.0f + (5.0f / 49f) * (n - 1)) * 100f) / 100f;

                // ── Hindrances (unlock schedule from Req 9.1) ─────────────────
                HindranceConfig[] hindrances = BuildHindranceConfigs(n);

                // ── Create/overwrite asset ─────────────────────────────────────
                string assetPath = $"{FOLDER}/Level_{n:D2}.asset";
                LevelData existing = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
                LevelData ld;
                if (existing != null)
                {
                    ld = existing;
                }
                else
                {
                    ld = CreateInstance<LevelData>();
                    UnityEditor.AssetDatabase.CreateAsset(ld, assetPath);
                }

                ld.SetLevelNumber(n);
                ld.SetChapterTheme(chapterTheme);
                ld.SetTimeLimit(Mathf.Round(timeLimit * 10f) / 10f);
                ld.SetSpawnInterval(Mathf.Round(spawnInterval * 100f) / 100f);
                ld.SetSpawnVariance(0.15f);
                ld.SetMaxOnScreen(maxOnScreen);
                ld.SetMaxHindrancesActive(GetMaxHindrances(n));
                ld.SetHindrancesArray(hindrances);
                ld.SetHindranceSpawnInterval(6f);
                ld.SetHindranceInitialDelay(n <= 5 ? 8f : 5f);
                ld.SetWrongTapTimePenalty(wrongTapPenalty);
                ld.SetBombTimePenalty(bombPenalty);
                ld.SetIsMegaLevel(isMega);
                ld.SetRewardCoins(10 + n * 8);

                UnityEditor.EditorUtility.SetDirty(ld);
                _levels[i] = ld;
            }

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log("[LevelDatabase] Generated and saved 50 levels.");
        }

        private static string GetChapterTheme(int n)
        {
            if (n <= 10) return "Sunny Meadow";
            if (n <= 20) return "Tropical Jungle";
            if (n <= 30) return "Snowy Arctic";
            if (n <= 40) return "Mystic Forest";
            return "Storm Peaks";
        }

private static int GetMaxHindrances(int n)
        {
            if (n <= 10) return 1;
            if (n <= 25) return 2;
            if (n <= 39) return 2;
            if (n == 40) return 3;
            return Mathf.Min(5, 3 + (n - 40) / 3);
        }

        private static HindranceConfig[] BuildHindranceConfigs(int n)
        {
            // Levels 26-39 introduce the three late-game set pieces while rotating
            // familiar hazards back in. Other post-21 levels remain unchanged.
            if (n >= 26 && n <= 39) return BuildLevel26To39Hindrances(n);
            if (n > 21) return System.Array.Empty<HindranceConfig>();

            // Unlock schedule (Req 9.1)
            System.Collections.Generic.List<HindranceConfig> list =
                new System.Collections.Generic.List<HindranceConfig>();

            void TryAdd(HindranceType t, int unlockLevel, float weight)
            {
                if (n >= unlockLevel) list.Add(new HindranceConfig { type = t, weight = weight, initialDelay = 0f });
            }

            TryAdd(HindranceType.Bomb,          3,  1.5f);
            TryAdd(HindranceType.FallingLeaves,  3,  1.0f);
            TryAdd(HindranceType.AlarmClock,     5,  1.0f);
            TryAdd(HindranceType.WindGust,       5,  1.0f);
            TryAdd(HindranceType.KnightHelmet,   7,  1.2f);
            TryAdd(HindranceType.InkSquid,       7,  0.8f);
            TryAdd(HindranceType.PoisonVial,    10,  0.8f);
            TryAdd(HindranceType.GhostAnimal,   10,  1.0f);
            TryAdd(HindranceType.BubbleShield,  12,  1.0f);
            TryAdd(HindranceType.StormCloud,    12,  0.8f);
            TryAdd(HindranceType.Flashbang,     15,  0.6f);
            TryAdd(HindranceType.ZeroGravity,   15,  0.7f);
            TryAdd(HindranceType.IceCube,       18,  1.0f);
            TryAdd(HindranceType.ThiefBird,     18,  0.9f);
            TryAdd(HindranceType.Tornado,       20,  0.8f);
            TryAdd(HindranceType.BlackHole,     20,  0.6f);
            TryAdd(HindranceType.PairedAnimal,  23,  1.0f);
            TryAdd(HindranceType.MirrorMode,    23,  0.7f);
            TryAdd(HindranceType.MagnetTrap,    26,  0.8f);
            TryAdd(HindranceType.CursedSkull,   26,  0.9f);

            return list.ToArray();
        }

private static HindranceConfig[] BuildLevel26To39Hindrances(int level)
        {
            HindranceConfig Config(HindranceType type, float weight) =>
                new HindranceConfig { type = type, weight = weight, initialDelay = 0f };

            // Mega-shooter levels keep their dedicated rules and no normal hindrances.
            if (level == 30 || level == 35) return System.Array.Empty<HindranceConfig>();

            switch (level)
            {
                case 26: return new[] { Config(HindranceType.SpringMushroomBumpers, 1.4f), Config(HindranceType.WindGust, 0.8f), Config(HindranceType.BubbleShield, 0.7f) };
                case 27: return new[] { Config(HindranceType.SpringMushroomBumpers, 1.3f), Config(HindranceType.Bomb, 0.8f), Config(HindranceType.FallingLeaves, 0.8f) };
                case 28: return new[] { Config(HindranceType.SpringMushroomBumpers, 1.3f), Config(HindranceType.IceCube, 0.8f), Config(HindranceType.ThiefBird, 0.7f) };
                case 29: return new[] { Config(HindranceType.SpringMushroomBumpers, 1.2f), Config(HindranceType.InkSquid, 0.8f), Config(HindranceType.AlarmClock, 0.8f) };

                case 31: return new[] { Config(HindranceType.PorcupinePulse, 1.4f), Config(HindranceType.WindGust, 0.8f), Config(HindranceType.BubbleShield, 0.7f) };
                case 32: return new[] { Config(HindranceType.PorcupinePulse, 1.3f), Config(HindranceType.Bomb, 0.8f), Config(HindranceType.FallingLeaves, 0.8f) };
                case 33: return new[] { Config(HindranceType.PorcupinePulse, 1.2f), Config(HindranceType.SpringMushroomBumpers, 0.9f), Config(HindranceType.KnightHelmet, 0.7f) };
                case 34: return new[] { Config(HindranceType.PorcupinePulse, 1.2f), Config(HindranceType.IceCube, 0.8f), Config(HindranceType.StormCloud, 0.7f) };

                case 36: return new[] { Config(HindranceType.VenusFlytrapRescue, 1.4f), Config(HindranceType.WindGust, 0.8f), Config(HindranceType.BubbleShield, 0.7f) };
                case 37: return new[] { Config(HindranceType.VenusFlytrapRescue, 1.3f), Config(HindranceType.SpringMushroomBumpers, 0.9f), Config(HindranceType.FallingLeaves, 0.8f) };
                case 38: return new[] { Config(HindranceType.VenusFlytrapRescue, 1.2f), Config(HindranceType.PorcupinePulse, 0.9f), Config(HindranceType.Bomb, 0.8f) };
                case 39: return new[] { Config(HindranceType.VenusFlytrapRescue, 1.2f), Config(HindranceType.SpringMushroomBumpers, 0.9f), Config(HindranceType.InkSquid, 0.8f) };
                default: return System.Array.Empty<HindranceConfig>();
            }
        }

#endif
    }
}
