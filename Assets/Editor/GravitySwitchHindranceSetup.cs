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
    public static class GravitySwitchHindranceSetup
    {
        private const string PrefabPath = "Assets/Prefabs/Hindrances/Hindrance_65_GravitySwitch.prefab";
        private const string DefinitionPath = "Assets/Resources/Hindrances/Definitions/Hindrance_65_GravitySwitch.asset";
        private const string RegistryPath = "Assets/Resources/Hindrances/HindranceRegistry.asset";

        [MenuItem("Animal Fall/Hindrances/Setup Gravity Switch")]
        public static void Setup()
        {
            Sprite[] switchSprites = Resources.LoadAll<Sprite>("icons/hindrances/switch");
            if (switchSprites.Length < 2)
            {
                Debug.LogError("[GravitySwitch] switch.png must provide closed and open sprites.");
                return;
            }

            Sprite closed = switchSprites[0];
            Sprite open = switchSprites[1];
            GameObject prefab = BuildPrefab(closed, open);
            HindranceData definition = BuildDefinition(prefab, closed, open);
            AddToRegistry(definition);
            AddToLevels();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GravitySwitch] Ready on normal levels 73, 74, 76, and 77.");
        }

        private static GameObject BuildPrefab(Sprite closed, Sprite open)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                var root = new GameObject("Hindrance_65_GravitySwitch");
                root.AddComponent<SpriteRenderer>();
                root.AddComponent<BoxCollider2D>();
                root.AddComponent<GravitySwitchHindrance>();
                prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Object.DestroyImmediate(root);
            }

            GravitySwitchHindrance hindrance = prefab.GetComponent<GravitySwitchHindrance>();
            if (hindrance == null) hindrance = prefab.AddComponent<GravitySwitchHindrance>();
            hindrance.EditorConfigure(closed, open);
            EditorUtility.SetDirty(prefab);
            return prefab;
        }

        private static HindranceData BuildDefinition(GameObject prefab, Sprite closed, Sprite open)
        {
            HindranceData definition = AssetDatabase.LoadAssetAtPath<HindranceData>(DefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<HindranceData>();
                AssetDatabase.CreateAsset(definition, DefinitionPath);
            }

            definition.hindranceType = HindranceType.GravitySwitch;
            definition.prefab = prefab;
            definition.icon = closed;
            definition.unlockLevel = 73;
            definition.displayName = "Gravity Switch";
            definition.effectDescription = "Tap the closed switch to open it and reverse gravity.";
            definition.category = HindranceCategory.EnvironmentModifier;
            definition.baseWeight = 1.35f;
            definition.difficultyTier = 3;
            definition.minDuration = 6f;
            definition.maxDuration = 8f;
            definition.maxSimultaneous = 1;
            definition.cooldown = 8f;
            definition.compatibilityTags = HindranceCompatibilityTag.GlobalMotion | HindranceCompatibilityTag.ExclusiveGesture;
            definition.exclusionTags = HindranceCompatibilityTag.GlobalMotion | HindranceCompatibilityTag.ExclusiveGesture;
            definition.inputMode = HindranceInputMode.Tap;
            definition.targetScope = HindranceTargetScope.World;
            definition.normalLevelEligible = true;
            definition.megaLevelEligible = false;
            definition.debugShowcaseOnly = false;
            definition.tutorialInstruction = "Tap the switch to open it and send animals upward!";
            definition.stateSprites = new[] { closed, open };
            definition.requiredInteractions = 1;
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
            HindranceRegistry.Entry entry = entries.FirstOrDefault(item => item != null && item.type == HindranceType.GravitySwitch);
            if (entry == null)
            {
                entry = new HindranceRegistry.Entry { type = HindranceType.GravitySwitch };
                entries.Add(entry);
            }
            entry.data = definition;
            registry.EditorSetEntries(entries.OrderBy(item => (int)item.type).ToArray());
        }

        private static void AddToLevels()
        {
            foreach (int number in new[] { 73, 74, 76, 77 })
            {
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>($"Assets/Levels/LevelData/Level_{number:D2}.asset");
                if (level == null) throw new System.InvalidOperationException($"Level {number} is missing.");
                var configs = level.Hindrances == null
                    ? new List<HindranceConfig>()
                    : level.Hindrances.Where(config => config != null && config.type != HindranceType.GravitySwitch).ToList();
                configs.Insert(0, new HindranceConfig { type = HindranceType.GravitySwitch, weight = 1.35f, initialDelay = 0f });
                level.SetHindrancesArray(configs.ToArray());
                level.SetMaxHindrancesActive(Mathf.Max(3, level.MaxHindrancesActive));
                EditorUtility.SetDirty(level);
            }
        }
    }
}
#endif
