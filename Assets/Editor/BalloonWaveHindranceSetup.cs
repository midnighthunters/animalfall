#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AnimalFall.Core.Hindrances;
using AnimalFall.Core.Hindrances.EnvironmentMods;
using AnimalFall.Data;

namespace AnimalFall.EditorTools
{
    public static class BalloonWaveHindranceSetup
    {
        private const string PrefabPath = "Assets/Prefabs/Hindrances/Hindrance_66_BalloonWave.prefab";
        private const string DefinitionPath = "Assets/Resources/Hindrances/Definitions/Hindrance_66_BalloonWave.asset";
        private const string RegistryPath = "Assets/Resources/Hindrances/HindranceRegistry.asset";

        [MenuItem("Animal Fall/Hindrances/Setup Balloon Wave")]
        public static void Setup()
        {
            Sprite[] balloons = Resources.LoadAll<Sprite>("icons/hindrances/balloon");
            if (balloons.Length == 0)
            {
                Debug.LogError("[BalloonWave] The balloon spritesheet is missing from Resources.");
                return;
            }

            GameObject prefab = BuildPrefab(balloons);
            HindranceData definition = BuildDefinition(prefab, balloons);
            AddToRegistry(definition);
            AddToLevels();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BalloonWave] Ready on normal levels 81, 82, 83, and 84.");
        }

        private static GameObject BuildPrefab(Sprite[] balloons)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                var root = new GameObject("Hindrance_66_BalloonWave");
                root.AddComponent<SpriteRenderer>();
                root.AddComponent<BalloonWaveHindrance>();
                prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Object.DestroyImmediate(root);
            }

            BalloonWaveHindrance hindrance = prefab.GetComponent<BalloonWaveHindrance>();
            if (hindrance == null) hindrance = prefab.AddComponent<BalloonWaveHindrance>();
            hindrance.EditorConfigure(balloons);
            EditorUtility.SetDirty(prefab);
            return prefab;
        }

        private static HindranceData BuildDefinition(GameObject prefab, Sprite[] balloons)
        {
            HindranceData definition = AssetDatabase.LoadAssetAtPath<HindranceData>(DefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<HindranceData>();
                AssetDatabase.CreateAsset(definition, DefinitionPath);
            }

            definition.hindranceType = HindranceType.BalloonWave;
            definition.prefab = prefab;
            definition.icon = balloons[0];
            definition.unlockLevel = 81;
            definition.displayName = "Balloon Wave";
            definition.effectDescription = "A wave of balloons rises from below and attaches to animals.";
            definition.category = HindranceCategory.EnvironmentModifier;
            definition.baseWeight = 1.3f;
            definition.difficultyTier = 3;
            definition.minDuration = 7f;
            definition.maxDuration = 9f;
            definition.maxSimultaneous = 1;
            definition.cooldown = 8f;
            definition.compatibilityTags = HindranceCompatibilityTag.GlobalMotion | HindranceCompatibilityTag.ExclusiveTarget;
            definition.exclusionTags = HindranceCompatibilityTag.GlobalMotion | HindranceCompatibilityTag.ExclusiveTarget;
            definition.inputMode = HindranceInputMode.None;
            definition.targetScope = HindranceTargetScope.Animal;
            definition.normalLevelEligible = true;
            definition.megaLevelEligible = false;
            definition.debugShowcaseOnly = false;
            definition.tutorialInstruction = "A balloon wave is rising — balloons attach to animals and lift them!";
            definition.stateSprites = balloons;
            definition.requiredInteractions = 0;
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void AddToRegistry(HindranceData definition)
        {
            HindranceRegistry registry = AssetDatabase.LoadAssetAtPath<HindranceRegistry>(RegistryPath);
            if (registry == null) throw new System.InvalidOperationException("Hindrance registry is missing.");
            var entries = registry.Entries == null
                ? new List<HindranceRegistry.Entry>()
                : new List<HindranceRegistry.Entry>(registry.Entries);
            HindranceRegistry.Entry entry = entries.FirstOrDefault(item => item != null && item.type == HindranceType.BalloonWave);
            if (entry == null)
            {
                entry = new HindranceRegistry.Entry { type = HindranceType.BalloonWave };
                entries.Add(entry);
            }
            entry.data = definition;
            registry.EditorSetEntries(entries.OrderBy(item => (int)item.type).ToArray());
        }

        private static void AddToLevels()
        {
            foreach (int number in new[] { 81, 82, 83, 84 })
            {
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>($"Assets/Levels/Level_{number:D2}.asset");
                if (level == null) throw new System.InvalidOperationException($"Level {number} is missing.");
                var configs = level.Hindrances == null
                    ? new List<HindranceConfig>()
                    : level.Hindrances.Where(config => config != null && config.type != HindranceType.BalloonWave).ToList();
                configs.Insert(0, new HindranceConfig { type = HindranceType.BalloonWave, weight = 1.3f, initialDelay = 0f });
                level.SetHindrancesArray(configs.ToArray());
                level.SetMaxHindrancesActive(Mathf.Max(3, level.MaxHindrancesActive));
                EditorUtility.SetDirty(level);
            }
        }
    }
}
#endif
