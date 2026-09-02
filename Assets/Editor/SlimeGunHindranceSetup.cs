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
    public static class SlimeGunHindranceSetup
    {
        private const string PrefabPath = "Assets/Prefabs/Hindrances/Hindrance_67_SlimeGun.prefab";
        private const string DefinitionPath = "Assets/Resources/Hindrances/Definitions/Hindrance_67_SlimeGun.asset";
        private const string RegistryPath = "Assets/Resources/Hindrances/HindranceRegistry.asset";

        [MenuItem("Animal Fall/Hindrances/Setup Slime Gun")]
        public static void Setup()
        {
            Sprite slimeGun = Resources.Load<Sprite>("icons/hindrances/slime_gun");
            if (slimeGun == null)
            {
                Debug.LogError("[SlimeGun] The slime_gun sprite is missing from Resources.");
                return;
            }

            GameObject prefab = BuildPrefab(slimeGun);
            HindranceData definition = BuildDefinition(prefab, slimeGun);
            AddToRegistry(definition);
            AddToLevels();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SlimeGun] Ready on normal levels 86, 87, 88, and 89.");
        }

        private static GameObject BuildPrefab(Sprite slimeGun)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                var root = new GameObject("Hindrance_67_SlimeGun");
                root.AddComponent<SpriteRenderer>();
                root.AddComponent<SlimeGunHindrance>();
                prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Object.DestroyImmediate(root);
            }

            SlimeGunHindrance hindrance = prefab.GetComponent<SlimeGunHindrance>();
            if (hindrance == null) hindrance = prefab.AddComponent<SlimeGunHindrance>();
            hindrance.EditorConfigure(slimeGun);
            EditorUtility.SetDirty(prefab);
            return prefab;
        }

        private static HindranceData BuildDefinition(GameObject prefab, Sprite slimeGun)
        {
            HindranceData definition = AssetDatabase.LoadAssetAtPath<HindranceData>(DefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<HindranceData>();
                AssetDatabase.CreateAsset(definition, DefinitionPath);
            }

            definition.hindranceType = HindranceType.SlimeGun;
            definition.prefab = prefab;
            definition.icon = slimeGun;
            definition.unlockLevel = 86;
            definition.displayName = "Slime Gun";
            definition.effectDescription = "The slime gun shoots and holds animals on screen temporarily.";
            definition.category = HindranceCategory.EnvironmentModifier;
            definition.baseWeight = 1.25f;
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
            definition.tutorialInstruction = "The slime gun is firing — captured animals are held briefly!";
            definition.stateSprites = new[] { slimeGun };
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
            HindranceRegistry.Entry entry = entries.FirstOrDefault(item => item != null && item.type == HindranceType.SlimeGun);
            if (entry == null)
            {
                entry = new HindranceRegistry.Entry { type = HindranceType.SlimeGun };
                entries.Add(entry);
            }
            entry.data = definition;
            registry.EditorSetEntries(entries.OrderBy(item => (int)item.type).ToArray());
        }

        private static void AddToLevels()
        {
            foreach (int number in new[] { 86, 87, 88, 89 })
            {
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>($"Assets/Levels/LevelData/Level_{number:D2}.asset");
                if (level == null) throw new System.InvalidOperationException($"Level {number} is missing.");
                var configs = level.Hindrances == null
                    ? new List<HindranceConfig>()
                    : level.Hindrances.Where(config => config != null && config.type != HindranceType.SlimeGun).ToList();
                configs.Insert(0, new HindranceConfig { type = HindranceType.SlimeGun, weight = 1.25f, initialDelay = 0f });
                level.SetHindrancesArray(configs.ToArray());
                level.SetMaxHindrancesActive(Mathf.Max(3, level.MaxHindrancesActive));
                EditorUtility.SetDirty(level);
            }
        }
    }
}
#endif
