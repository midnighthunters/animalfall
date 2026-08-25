#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AnimalFall.Core.Animals;
using AnimalFall.Core.Hindrances;
using AnimalFall.Data;
using AnimalFall.MegaShooter;

namespace AnimalFall.EditorTools
{
    public static class AnimalFallLevelRepairTool
    {
        private const string LevelFolder = "Assets/Levels/LevelData";
        private const string GoalFolder = "Assets/Data/Goals";
        private const string DatabasePath = "Assets/Levels/LevelDatabase.asset";

        [MenuItem("Tools/Animal Fall/Repair & Balance All 100 Levels")]
        public static void RepairAllLevels()
        {
            EnsureFolder("Assets/Levels", "LevelData");
            EnsureFolder("Assets/Data", "Goals");

            var database = AssetDatabase.LoadAssetAtPath<LevelDatabase>(DatabasePath);
            if (database == null)
            {
                Debug.LogError($"[LevelRepair] Missing {DatabasePath}.");
                return;
            }

            AnimalData[] animals = LoadAndTuneAnimals();
            if (animals.Length < 3)
            {
                Debug.LogError("[LevelRepair] Cat, Chicken and Dog AnimalData assets are required.");
                return;
            }

            Dictionary<int, MegaLevelData> megaByLevel = LoadMegaLevels();
            int[] unlockLevels = BuildHindranceUnlockLevels();
            var repaired = new LevelData[100];

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int levelNumber = 1; levelNumber <= 100; levelNumber++)
                {
                    bool isMega = levelNumber % 5 == 0;
                    LevelData level = LoadOrCreateLevel(levelNumber);
                    GoalData goal = isMega ? null : LoadOrCreateGoal(levelNumber);
                    if (!isMega) ConfigureGoal(goal, levelNumber, animals);
                    ConfigureLevel(level, levelNumber, isMega, goal, animals, megaByLevel, unlockLevels);
                    repaired[levelNumber - 1] = level;
                }

                database.SetLevelsPreservingExisting(repaired);
                EditorUtility.SetDirty(database);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LevelRepair] Repaired and balanced all 100 levels: 80 normal + 20 Mega Shooter.");
        }

        private static AnimalData[] LoadAndTuneAnimals()
        {
            AnimalData chicken = AssetDatabase.LoadAssetAtPath<AnimalData>("Assets/Data/Animals/Chicken.asset");
            AnimalData dog = AssetDatabase.LoadAssetAtPath<AnimalData>("Assets/Data/Animals/Dog.asset");
            AnimalData cat = AssetDatabase.LoadAssetAtPath<AnimalData>("Assets/Data/Animals/Cat.asset");
            AnimalData[] animals = { chicken, dog, cat };
            if (chicken == null || dog == null || cat == null) return new AnimalData[0];

            TuneAnimal(chicken, 0.48f, 0.74f, 1.35f, 0.35f);
            TuneAnimal(dog, 0.52f, 0.80f, 1.20f, 0.30f);
            TuneAnimal(cat, 0.45f, 0.70f, 1.45f, 0.38f);
            return animals;
        }

        private static void TuneAnimal(AnimalData animal, float speedMin, float speedMax, float frequency, float amplitude)
        {
            animal.speedMin = speedMin;
            animal.speedMax = speedMax;
            animal.lifetime = 24f;
            animal.zigzagFrequency = frequency;
            animal.zigzagAmplitude = amplitude;
            animal.isTargetSpecies = true;
            EditorUtility.SetDirty(animal);
        }

        private static Dictionary<int, MegaLevelData> LoadMegaLevels()
        {
            var result = new Dictionary<int, MegaLevelData>();
            string[] guids = AssetDatabase.FindAssets("t:MegaLevelData", new[] { "Assets/MegaShooter/Data/Levels" });
            foreach (string guid in guids)
            {
                MegaLevelData data = AssetDatabase.LoadAssetAtPath<MegaLevelData>(AssetDatabase.GUIDToAssetPath(guid));
                if (data != null && data.IsValidMegaNumber) result[data.gameLevelNumber] = data;
            }
            return result;
        }

        private static LevelData LoadOrCreateLevel(int number)
        {
            string path = $"{LevelFolder}/Level_{number:D2}.asset";
            LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (level != null) return level;
            level = ScriptableObject.CreateInstance<LevelData>();
            level.name = $"Level_{number:D2}";
            AssetDatabase.CreateAsset(level, path);
            return level;
        }

        private static GoalData LoadOrCreateGoal(int number)
        {
            string path = number == 1 ? $"{GoalFolder}/Goal_Level1.asset" : $"{GoalFolder}/Goal_Level{number}.asset";
            GoalData goal = AssetDatabase.LoadAssetAtPath<GoalData>(path);
            if (goal != null) return goal;
            goal = ScriptableObject.CreateInstance<GoalData>();
            goal.name = $"Goal_Level{number}";
            AssetDatabase.CreateAsset(goal, path);
            return goal;
        }

        private static void ConfigureGoal(GoalData goal, int levelNumber, AnimalData[] animals)
        {
            int poolCount = levelNumber <= 5 ? 1 : levelNumber <= 20 ? 2 : 3;
            int totalTarget = Mathf.RoundToInt(Mathf.Lerp(7f, 24f, (levelNumber - 1) / 99f));
            int start = (levelNumber - 1) % animals.Length;

            SerializedObject serialized = new SerializedObject(goal);
            SerializedProperty targets = serialized.FindProperty("_targets");
            targets.arraySize = poolCount;
            for (int i = 0; i < poolCount; i++)
            {
                AnimalData animal = animals[(start + i) % animals.Length];
                int count = totalTarget / poolCount + (i < totalTarget % poolCount ? 1 : 0);
                SerializedProperty target = targets.GetArrayElementAtIndex(i);
                target.FindPropertyRelative("species").enumValueIndex = (int)animal.species;
                target.FindPropertyRelative("count").intValue = count;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(goal);
        }

        private static void ConfigureLevel(LevelData level, int number, bool isMega, GoalData goal,
            AnimalData[] animals, Dictionary<int, MegaLevelData> megaByLevel, int[] unlockLevels)
        {
            float progress = (number - 1) / 99f;
            SerializedObject serialized = new SerializedObject(level);
            SetInt(serialized, "_levelNumber", number);
            SetString(serialized, "_chapterTheme", GetChapter(number));
            SetObject(serialized, "_chapterBackground", null);
            SetFloat(serialized, "_timeLimit", isMega ? 60f : Mathf.Lerp(72f, 52f, progress));
            SetObject(serialized, "_goal", goal);

            SerializedProperty spawnPool = serialized.FindProperty("_spawnPool");
            if (isMega)
            {
                spawnPool.arraySize = 0;
            }
            else
            {
                int poolCount = number <= 5 ? 1 : number <= 20 ? 2 : 3;
                int start = (number - 1) % animals.Length;
                spawnPool.arraySize = poolCount;
                for (int i = 0; i < poolCount; i++)
                    spawnPool.GetArrayElementAtIndex(i).objectReferenceValue = animals[(start + i) % animals.Length];
            }

            SetFloat(serialized, "_spawnInterval", Mathf.Lerp(1.08f, 0.58f, progress));
            SetFloat(serialized, "_spawnVariance", Mathf.Lerp(0.08f, 0.16f, progress));
            SetInt(serialized, "_maxOnScreen", Mathf.RoundToInt(Mathf.Lerp(5f, 10f, progress)));

            List<HindranceType> types = isMega ? new List<HindranceType>() : BuildLevelHindrances(number, unlockLevels);
            SerializedProperty hindrances = serialized.FindProperty("_hindrances");
            hindrances.arraySize = types.Count;
            for (int i = 0; i < types.Count; i++)
            {
                SerializedProperty config = hindrances.GetArrayElementAtIndex(i);
                config.FindPropertyRelative("type").enumValueIndex = (int)types[i];
                config.FindPropertyRelative("weight").floatValue = i == 0 ? 2f : 1f;
                config.FindPropertyRelative("initialDelay").floatValue = 0f;
            }
            SetFloat(serialized, "_hindranceSpawnInterval", Mathf.Lerp(10f, 6.5f, progress));
            SetFloat(serialized, "_hindranceInitialDelay", Mathf.Lerp(8f, 5f, progress));
            SetInt(serialized, "_maxHindrancesActive", types.Count == 0 ? 1 : Mathf.Min(types.Count, number < 30 ? 1 : number < 65 ? 2 : 3));

            SetFloat(serialized, "_wrongTapTimePenalty", Mathf.Lerp(0.6f, 2.0f, progress));
            SetInt(serialized, "_wrongTapScorePenalty", Mathf.RoundToInt(Mathf.Lerp(10f, 40f, progress)));
            SetFloat(serialized, "_bombTimePenalty", Mathf.Lerp(2f, 4f, progress));
            SetInt(serialized, "_bombScorePenalty", Mathf.RoundToInt(Mathf.Lerp(25f, 65f, progress)));
            SetInt(serialized, "_rewardCoins", Mathf.RoundToInt(Mathf.Lerp(15f, 180f, progress)));
            SetBool(serialized, "_isMegaLevel", isMega);
            SetBool(serialized, "_allowNormalHindrancesInMegaLevel", false);
            SetObject(serialized, "_villain", null);

            SerializedProperty mode = serialized.FindProperty("_levelMode");
            mode.enumValueIndex = isMega ? (int)LevelMode.MegaShooter : (int)LevelMode.Normal;
            MegaLevelData megaData = null;
            if (isMega && !megaByLevel.TryGetValue(number, out megaData))
                Debug.LogError($"[LevelRepair] Missing MegaLevelData for Level {number}.");
            SetObject(serialized, "_megaShooterData", megaData);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(level);
        }

        private static int[] BuildHindranceUnlockLevels()
        {
            var levels = new List<int>(50);
            for (int level = 3; level <= 100 && levels.Count < 50; level++)
                if (level % 5 != 0) levels.Add(level);
            return levels.ToArray();
        }

        private static List<HindranceType> BuildLevelHindrances(int levelNumber, int[] unlockLevels)
        {
            int unlocked = 0;
            while (unlocked < unlockLevels.Length && unlockLevels[unlocked] <= levelNumber) unlocked++;
            var result = new List<HindranceType>(3);
            if (unlocked == 0) return result;

            int desired = levelNumber < 30 ? 1 : levelNumber < 65 ? 2 : 3;
            int newest = unlocked - 1;
            bool introducing = newest < unlockLevels.Length && unlockLevels[newest] == levelNumber;
            if (introducing)
            {
                AddUnique(result, (HindranceType)(newest + 1));
                for (int offset = 1; result.Count < desired && newest - offset >= 0; offset++)
                    AddUnique(result, (HindranceType)(newest - offset + 1));
            }
            else
            {
                int anchor = levelNumber > unlockLevels[unlockLevels.Length - 1]
                    ? (levelNumber * 7) % unlocked
                    : newest;
                for (int offset = 0; result.Count < desired && offset < unlocked; offset++)
                    AddUnique(result, (HindranceType)(((anchor - offset * 9 + unlocked) % unlocked) + 1));
            }
            return result;
        }

        private static void AddUnique(List<HindranceType> list, HindranceType type)
        {
            if (type != HindranceType.None && !list.Contains(type)) list.Add(type);
        }

        private static string GetChapter(int level)
        {
            if (level <= 20) return "Meadow";
            if (level <= 40) return "Forest";
            if (level <= 60) return "Mountain";
            if (level <= 80) return "Night";
            return "Summit";
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }

        private static void SetInt(SerializedObject so, string name, int value) => so.FindProperty(name).intValue = value;
        private static void SetFloat(SerializedObject so, string name, float value) => so.FindProperty(name).floatValue = value;
        private static void SetString(SerializedObject so, string name, string value) => so.FindProperty(name).stringValue = value;
        private static void SetBool(SerializedObject so, string name, bool value) => so.FindProperty(name).boolValue = value;
        private static void SetObject(SerializedObject so, string name, Object value) => so.FindProperty(name).objectReferenceValue = value;
    }
}
#endif