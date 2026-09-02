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
    public static class CrusherHindranceSetup
    {
        private const string PrefabPath = "Assets/Prefabs/Hindrances/Hindrance_64_Crusher.prefab";
        private const string DefinitionPath = "Assets/Resources/Hindrances/Definitions/Hindrance_64_Crusher.asset";
        private const string RegistryPath = "Assets/Resources/Hindrances/HindranceRegistry.asset";

        [MenuItem("Animal Fall/Hindrances/Setup Crusher")]
        public static void Setup()
        {
            Sprite crusher = Resources.Load<Sprite>("icons/hindrances/crusher");
            if (crusher == null)
            {
                Debug.LogError("[Crusher] The crusher sprite is missing from Resources.");
                return;
            }

            GameObject prefab = BuildPrefab(crusher);
            HindranceData definition = BuildDefinition(prefab, crusher);
            AddToRegistry(definition);
            AddToLevels();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Crusher] Ready on normal levels 68, 69, 71, and 72.");
        }

        private static GameObject BuildPrefab(Sprite crusher)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                var root = new GameObject("Hindrance_64_Crusher");
                root.AddComponent<SpriteRenderer>();
                root.AddComponent<CrusherHindrance>();
                prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Object.DestroyImmediate(root);
            }

            CrusherHindrance hindrance = prefab.GetComponent<CrusherHindrance>();
            if (hindrance == null) hindrance = prefab.AddComponent<CrusherHindrance>();
            hindrance.EditorConfigure(crusher);
            EditorUtility.SetDirty(prefab);
            return prefab;
        }

        private static HindranceData BuildDefinition(GameObject prefab, Sprite crusher)
        {
            HindranceData definition = AssetDatabase.LoadAssetAtPath<HindranceData>(DefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<HindranceData>();
                AssetDatabase.CreateAsset(definition, DefinitionPath);
            }

            definition.hindranceType = HindranceType.Crusher;
            definition.prefab = prefab;
            definition.icon = crusher;
            definition.unlockLevel = 68;
            definition.displayName = "Crusher";
            definition.effectDescription = "Opposing crushers periodically close in and clear the animals between them.";
            definition.category = HindranceCategory.EnvironmentModifier;
            definition.baseWeight = 1.45f;
            definition.difficultyTier = 3;
            definition.minDuration = 6f;
            definition.maxDuration = 9f;
            definition.maxSimultaneous = 1;
            definition.cooldown = 8f;
            definition.compatibilityTags = HindranceCompatibilityTag.GlobalMotion | HindranceCompatibilityTag.ExclusiveTarget;
            definition.exclusionTags = HindranceCompatibilityTag.GlobalMotion | HindranceCompatibilityTag.ExclusiveTarget;
            definition.inputMode = HindranceInputMode.None;
            definition.targetScope = HindranceTargetScope.Global;
            definition.normalLevelEligible = true;
            definition.megaLevelEligible = false;
            definition.debugShowcaseOnly = false;
            definition.tutorialInstruction = "Watch the crusher jaws — animals caught between them are eliminated!";
            definition.stateSprites = new[] { crusher };
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
            HindranceRegistry.Entry entry = entries.FirstOrDefault(item => item != null && item.type == HindranceType.Crusher);
            if (entry == null)
            {
                entry = new HindranceRegistry.Entry { type = HindranceType.Crusher };
                entries.Add(entry);
            }
            entry.data = definition;
            registry.EditorSetEntries(entries.OrderBy(item => (int)item.type).ToArray());
        }

        private static void AddToLevels()
        {
            foreach (int number in new[] { 68, 69, 71, 72 })
            {
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>($"Assets/Levels/LevelData/Level_{number:D2}.asset");
                if (level == null) throw new System.InvalidOperationException($"Level {number} is missing.");
                var configs = level.Hindrances == null
                    ? new List<HindranceConfig>()
                    : level.Hindrances.Where(config => config != null && config.type != HindranceType.Crusher).ToList();
                configs.Insert(0, new HindranceConfig { type = HindranceType.Crusher, weight = 1.45f, initialDelay = 0f });
                level.SetHindrancesArray(configs.ToArray());
                level.SetMaxHindrancesActive(Mathf.Max(3, level.MaxHindrancesActive));
                EditorUtility.SetDirty(level);
            }
        }
    }
}
#endif
