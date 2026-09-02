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
    public static class CloudWaveHindranceSetup
    {
        private const string PrefabPath = "Assets/Prefabs/Hindrances/Hindrance_68_CloudWave.prefab";
        private const string DefinitionPath = "Assets/Resources/Hindrances/Definitions/Hindrance_68_CloudWave.asset";
        private const string RegistryPath = "Assets/Resources/Hindrances/HindranceRegistry.asset";

        [MenuItem("Animal Fall/Hindrances/Setup Cloud Wave")]
        public static void Setup()
        {
            Sprite cloud = Resources.Load<Sprite>("icons/hindrances/cloud");
            if (cloud == null)
            {
                Debug.LogError("[CloudWave] The cloud sprite is missing from Resources.");
                return;
            }

            GameObject prefab = BuildPrefab(cloud);
            HindranceData definition = BuildDefinition(prefab, cloud);
            AddToRegistry(definition);
            AddToLevels();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CloudWave] Ready on normal levels 91, 92, 93, and 94.");
        }

        private static GameObject BuildPrefab(Sprite cloud)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                var root = new GameObject("Hindrance_68_CloudWave");
                root.AddComponent<SpriteRenderer>();
                root.AddComponent<CloudWaveHindrance>();
                prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Object.DestroyImmediate(root);
            }

            CloudWaveHindrance hindrance = prefab.GetComponent<CloudWaveHindrance>();
            if (hindrance == null) hindrance = prefab.AddComponent<CloudWaveHindrance>();
            hindrance.EditorConfigure(cloud);
            EditorUtility.SetDirty(prefab);
            return prefab;
        }

        private static HindranceData BuildDefinition(GameObject prefab, Sprite cloud)
        {
            HindranceData definition = AssetDatabase.LoadAssetAtPath<HindranceData>(DefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<HindranceData>();
                AssetDatabase.CreateAsset(definition, DefinitionPath);
            }

            definition.hindranceType = HindranceType.CloudWave;
            definition.prefab = prefab;
            definition.icon = cloud;
            definition.unlockLevel = 91;
            definition.displayName = "Cloud Wave";
            definition.effectDescription = "A bunch of clouds sweeps across the screen from left to right.";
            definition.category = HindranceCategory.EnvironmentModifier;
            definition.baseWeight = 1.2f;
            definition.difficultyTier = 2;
            definition.minDuration = 6f;
            definition.maxDuration = 9f;
            definition.maxSimultaneous = 1;
            definition.cooldown = 7f;
            definition.compatibilityTags = HindranceCompatibilityTag.GlobalMotion;
            definition.exclusionTags = HindranceCompatibilityTag.GlobalMotion;
            definition.inputMode = HindranceInputMode.None;
            definition.targetScope = HindranceTargetScope.Global;
            definition.normalLevelEligible = true;
            definition.megaLevelEligible = false;
            definition.debugShowcaseOnly = false;
            definition.tutorialInstruction = "Watch the cloud wave cross the screen!";
            definition.stateSprites = new[] { cloud };
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
            HindranceRegistry.Entry entry = entries.FirstOrDefault(item => item != null && item.type == HindranceType.CloudWave);
            if (entry == null)
            {
                entry = new HindranceRegistry.Entry { type = HindranceType.CloudWave };
                entries.Add(entry);
            }
            entry.data = definition;
            registry.EditorSetEntries(entries.OrderBy(item => (int)item.type).ToArray());
        }

        private static void AddToLevels()
        {
            foreach (int number in new[] { 91, 92, 93, 94 })
            {
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>($"Assets/Levels/LevelData/Level_{number:D2}.asset");
                if (level == null) throw new System.InvalidOperationException($"Level {number} is missing.");
                var configs = level.Hindrances == null
                    ? new List<HindranceConfig>()
                    : level.Hindrances.Where(config => config != null && config.type != HindranceType.CloudWave).ToList();
                configs.Insert(0, new HindranceConfig { type = HindranceType.CloudWave, weight = 1.2f, initialDelay = 0f });
                level.SetHindrancesArray(configs.ToArray());
                level.SetMaxHindrancesActive(Mathf.Max(3, level.MaxHindrancesActive));
                EditorUtility.SetDirty(level);
            }
        }
    }
}
#endif
