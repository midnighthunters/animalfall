#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AnimalFall.Core.Animals;
using AnimalFall.Core.Hindrances;
using AnimalFall.Core.Hindrances.New;
using AnimalFall.Data;

namespace AnimalFall.EditorTools
{
    /// <summary>Creates the Level 21 frog set piece and keeps the operation repeatable.</summary>
    public static class FrogHindranceSetup
    {
        private const string FrogSheetPath = "Assets/Resources/icons/hindrances/frog.png";
        private const string PrefabPath = "Assets/Prefabs/Hindrances/Hindrance_55_FrogSnatcher.prefab";
        private const string DefinitionPath = "Assets/Resources/Hindrances/Definitions/Hindrance_55_FrogSnatcher.asset";
        private const string RegistryPath = "Assets/Resources/Hindrances/HindranceRegistry.asset";
        private const string RaccoonPath = "Assets/Data/Animals/Raccoon.asset";
        private const string LevelPath = "Assets/Levels/LevelData/Level_21.asset";
        private const string GoalPath = "Assets/Data/Goals/Goal_Level21.asset";

        [MenuItem("Animal Fall/Hindrances/Setup Level 21 Frog")]
        public static void Setup()
        {
            Dictionary<string, Sprite> sprites = LoadFrogSprites();
            if (!sprites.TryGetValue("frog_13", out Sprite baseSprite) ||
                !sprites.TryGetValue("frog_3", out Sprite frogSprite) ||
                !sprites.TryGetValue("frog_4", out Sprite tongueSprite))
            {
                Debug.LogError("[FrogSetup] frog.png must contain frog_13 (base), frog_3 (frog), and frog_4 (tongue).");
                return;
            }

            AnimalData raccoon = CreateOrUpdateRaccoon();
            GameObject prefab = CreateOrUpdatePrefab(baseSprite, frogSprite, tongueSprite);
            HindranceData definition = CreateOrUpdateDefinition(prefab, frogSprite,
                baseSprite, tongueSprite);
            AddToRegistry(definition);
            ConfigureLevel(raccoon);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<LevelData>(LevelPath);
            Debug.Log("[FrogSetup] Level 21 now uses Dog, Pig, Monkey, and Raccoon with the Frog Snatcher.");
        }

        private static Dictionary<string, Sprite> LoadFrogSprites()
        {
            var result = new Dictionary<string, Sprite>();
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(FrogSheetPath);
            for (int i = 0; i < assets.Length; i++)
                if (assets[i] is Sprite sprite) result[sprite.name] = sprite;
            return result;
        }

        private static AnimalData CreateOrUpdateRaccoon()
        {
            AnimalData data = AssetDatabase.LoadAssetAtPath<AnimalData>(RaccoonPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<AnimalData>();
                AssetDatabase.CreateAsset(data, RaccoonPath);
            }

            data.species = AnimalSpecies.Raccoon;
            data.type = AnimalType.Normal;
            data.movementPattern = MovementPattern.ZigZag;
            data.speedMin = 2f;
            data.speedMax = 3.2f;
            data.pointValue = 50;
            data.shieldHP = 0;
            data.isTargetSpecies = true;
            data.lifetime = 12f;
            data.zigzagAmplitude = 1.1f;
            data.zigzagFrequency = 1.7f;
            data.requiresDoubleTap = false;
            EditorUtility.SetDirty(data);
            return data;
        }

        private static GameObject CreateOrUpdatePrefab(Sprite baseSprite, Sprite frogSprite,
            Sprite tongueSprite)
        {
            var root = new GameObject("Hindrance_55_FrogSnatcher");
            FrogSnatcherHindrance frog = root.AddComponent<FrogSnatcherHindrance>();
            frog.EditorConfigure(baseSprite, frogSprite, tongueSprite);

            var serialized = new SerializedObject(frog);
            SerializedProperty lifetime = serialized.FindProperty("_maxLifetimeSeconds");
            if (lifetime != null) lifetime.floatValue = 90f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static HindranceData CreateOrUpdateDefinition(GameObject prefab, Sprite frogSprite,
            Sprite baseSprite, Sprite tongueSprite)
        {
            HindranceData data = AssetDatabase.LoadAssetAtPath<HindranceData>(DefinitionPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<HindranceData>();
                AssetDatabase.CreateAsset(data, DefinitionPath);
            }

            data.hindranceType = HindranceType.FrogSnatcher;
            data.prefab = prefab;
            data.icon = frogSprite;
            data.unlockLevel = 21;
            data.displayName = "Frog Snatcher";
            data.effectDescription = "The frog periodically catches and removes a falling animal.";
            data.category = HindranceCategory.DynamicRiskReward;
            data.baseWeight = 1f;
            data.difficultyTier = 2;
            data.minDuration = 60f;
            data.maxDuration = 75f;
            data.maxSimultaneous = 1;
            data.cooldown = 60f;
            data.compatibilityTags = HindranceCompatibilityTag.ExclusiveTarget;
            data.exclusionTags = HindranceCompatibilityTag.ExclusiveTarget;
            data.inputMode = HindranceInputMode.None;
            data.targetScope = HindranceTargetScope.Animal;
            data.normalLevelEligible = true;
            data.megaLevelEligible = false;
            data.debugShowcaseOnly = false;
            data.tutorialInstruction = "Watch out for the frog's tongue!";
            data.stateSprites = new[] { baseSprite, frogSprite, tongueSprite };
            data.telegraphDuration = 0.42f;
            data.interactionWindow = 0f;
            data.requiredInteractions = 1;
            data.primaryValue = 1f;
            data.secondaryValue = 0f;
            EditorUtility.SetDirty(data);
            return data;
        }

        private static void AddToRegistry(HindranceData definition)
        {
            HindranceRegistry registry = AssetDatabase.LoadAssetAtPath<HindranceRegistry>(RegistryPath);
            if (registry == null)
            {
                Debug.LogError("[FrogSetup] HindranceRegistry asset is missing.");
                return;
            }

            var entries = new List<HindranceRegistry.Entry>();
            if (registry.Entries != null)
            {
                for (int i = 0; i < registry.Entries.Count; i++)
                {
                    HindranceRegistry.Entry entry = registry.Entries[i];
                    if (entry != null && entry.type != HindranceType.FrogSnatcher)
                        entries.Add(entry);
                }
            }

            entries.Add(new HindranceRegistry.Entry
            {
                type = HindranceType.FrogSnatcher,
                data = definition
            });
            entries.Sort((a, b) => ((int)a.type).CompareTo((int)b.type));
            registry.EditorSetEntries(entries.ToArray());
        }

        private static void ConfigureLevel(AnimalData raccoon)
        {
            LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(LevelPath);
            GoalData goal = AssetDatabase.LoadAssetAtPath<GoalData>(GoalPath);
            AnimalData dog = AssetDatabase.LoadAssetAtPath<AnimalData>("Assets/Data/Animals/Dog.asset");
            AnimalData pig = AssetDatabase.LoadAssetAtPath<AnimalData>("Assets/Data/Animals/Pig.asset");
            AnimalData monkey = AssetDatabase.LoadAssetAtPath<AnimalData>("Assets/Data/Animals/Monkey.asset");
            if (level == null || goal == null || dog == null || pig == null || monkey == null || raccoon == null)
            {
                Debug.LogError("[FrogSetup] Level 21, its goal, or one of the four animal assets is missing.");
                return;
            }

            var levelObject = new SerializedObject(level);
            SerializedProperty pool = levelObject.FindProperty("_spawnPool");
            pool.arraySize = 4;
            pool.GetArrayElementAtIndex(0).objectReferenceValue = dog;
            pool.GetArrayElementAtIndex(1).objectReferenceValue = pig;
            pool.GetArrayElementAtIndex(2).objectReferenceValue = monkey;
            pool.GetArrayElementAtIndex(3).objectReferenceValue = raccoon;

            SerializedProperty hindrances = levelObject.FindProperty("_hindrances");
            hindrances.arraySize = 1;
            SerializedProperty frogEntry = hindrances.GetArrayElementAtIndex(0);
            frogEntry.FindPropertyRelative("type").enumValueIndex = (int)HindranceType.FrogSnatcher;
            frogEntry.FindPropertyRelative("weight").floatValue = 1f;
            frogEntry.FindPropertyRelative("initialDelay").floatValue = 0f;
            levelObject.FindProperty("_hindranceInitialDelay").floatValue = 2.5f;
            levelObject.FindProperty("_hindranceSpawnInterval").floatValue = 30f;
            levelObject.FindProperty("_maxHindrancesActive").intValue = 1;
            levelObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(level);

            var goalObject = new SerializedObject(goal);
            SerializedProperty targets = goalObject.FindProperty("_targets");
            targets.arraySize = 4;
            SetGoalTarget(targets.GetArrayElementAtIndex(0), AnimalSpecies.Dog, 3);
            SetGoalTarget(targets.GetArrayElementAtIndex(1), AnimalSpecies.Pig, 3);
            SetGoalTarget(targets.GetArrayElementAtIndex(2), AnimalSpecies.Monkey, 3);
            SetGoalTarget(targets.GetArrayElementAtIndex(3), AnimalSpecies.Raccoon, 3);
            goalObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(goal);
        }

        private static void SetGoalTarget(SerializedProperty target, AnimalSpecies species, int count)
        {
            target.FindPropertyRelative("species").enumValueIndex = (int)species;
            target.FindPropertyRelative("count").intValue = count;
        }
    }
}
#endif
