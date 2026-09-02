#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AnimalFall.Core.Hindrances;
using AnimalFall.Core.Hindrances.New;
using AnimalFall.Data;

namespace AnimalFall.EditorTools
{
    public static class PandaJailKeySetup
    {
        private const string PrefabPath = "Assets/Prefabs/Hindrances/Hindrance_63_PandaJailKey.prefab";
        private const string DefinitionPath = "Assets/Resources/Hindrances/Definitions/Hindrance_63_PandaJailKey.asset";
        private const string RegistryPath = "Assets/Resources/Hindrances/HindranceRegistry.asset";

        [MenuItem("Animal Fall/Hindrances/Setup Panda Jail and Key")]
        public static void Setup()
        {
            Sprite panda = Resources.Load<Sprite>("icons/animals/PANDA2");
            Sprite pandaJail = Resources.Load<Sprite>("icons/hindrances/panda_jail");
            if (panda == null || pandaJail == null)
            {
                Debug.LogError("[PandaJailKey] Panda or panda_jail sprite is missing from Resources.");
                return;
            }

            GameObject prefab = BuildPrefab(pandaJail, panda);
            HindranceData definition = BuildDefinition(prefab, pandaJail, panda);
            AddToRegistry(definition);
            AddToLevels();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PandaJailKey] Ready on normal levels 63, 64, 66, and 67.");
        }

        private static GameObject BuildPrefab(Sprite pandaJail, Sprite panda)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                var root = new GameObject("Hindrance_63_PandaJailKey");
                root.AddComponent<SpriteRenderer>();
                root.AddComponent<PandaJailKeyHindrance>();
                prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Object.DestroyImmediate(root);
            }

            PandaJailKeyHindrance hindrance = prefab.GetComponent<PandaJailKeyHindrance>();
            if (hindrance == null) hindrance = prefab.AddComponent<PandaJailKeyHindrance>();
            hindrance.EditorConfigure(pandaJail, panda, 1.1f);
            EditorUtility.SetDirty(prefab);
            return prefab;
        }

        private static HindranceData BuildDefinition(GameObject prefab, Sprite pandaJail, Sprite panda)
        {
            HindranceData definition = AssetDatabase.LoadAssetAtPath<HindranceData>(DefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<HindranceData>();
                AssetDatabase.CreateAsset(definition, DefinitionPath);
            }

            definition.hindranceType = HindranceType.PandaJailKey;
            definition.prefab = prefab;
            definition.icon = pandaJail;
            definition.unlockLevel = 63;
            definition.displayName = "Panda Jail & Key";
            definition.effectDescription = "Tap the key before the panda jail falls away.";
            definition.category = HindranceCategory.InteractionRule;
            definition.baseWeight = 1.6f;
            definition.difficultyTier = 2;
            definition.minDuration = 4f;
            definition.maxDuration = 8f;
            definition.maxSimultaneous = 1;
            definition.cooldown = 7f;
            definition.compatibilityTags = HindranceCompatibilityTag.ExclusiveTarget;
            definition.exclusionTags = HindranceCompatibilityTag.ExclusiveGesture;
            definition.inputMode = HindranceInputMode.Tap;
            definition.targetScope = HindranceTargetScope.World;
            definition.normalLevelEligible = true;
            definition.megaLevelEligible = false;
            definition.debugShowcaseOnly = false;
            definition.tutorialInstruction = "Tap the key to free Panda!";
            definition.stateSprites = new[] { pandaJail, panda };
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
            HindranceRegistry.Entry entry = entries.FirstOrDefault(item => item != null && item.type == HindranceType.PandaJailKey);
            if (entry == null)
            {
                entry = new HindranceRegistry.Entry { type = HindranceType.PandaJailKey };
                entries.Add(entry);
            }
            entry.data = definition;
            registry.EditorSetEntries(entries.OrderBy(item => (int)item.type).ToArray());
        }

        private static void AddToLevels()
        {
            foreach (int number in new[] { 63, 64, 66, 67 })
            {
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>($"Assets/Levels/LevelData/Level_{number:D2}.asset");
                if (level == null) throw new System.InvalidOperationException($"Level {number} is missing.");
                var configs = level.Hindrances == null
                    ? new List<HindranceConfig>()
                    : level.Hindrances.Where(config => config != null && config.type != HindranceType.PandaJailKey).ToList();
                configs.Insert(0, new HindranceConfig { type = HindranceType.PandaJailKey, weight = 1.6f, initialDelay = 0f });
                level.SetHindrancesArray(configs.ToArray());
                level.SetMaxHindrancesActive(Mathf.Max(3, level.MaxHindrancesActive));
                EditorUtility.SetDirty(level);
            }
        }
    }
}
#endif
