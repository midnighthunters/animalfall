#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AnimalFall.Core.Hindrances;
using AnimalFall.Core.Hindrances.New;
using AnimalFall.Data;

namespace AnimalFall.EditorTools
{
    public static class LateHindranceSetup
    {
        private const string DefinitionRoot = "Assets/Resources/Hindrances/Definitions/";
        private const string PrefabRoot = "Assets/Prefabs/Hindrances/";

        [MenuItem("Tools/Animal Fall/Configure Levels 26-39 Hindrances")]
        public static void Configure()
        {
            Sprite[] mushrooms = LoadSprites("Assets/Resources/icons/hindrances/mushroom.png");
            Sprite[] porcupines = LoadSprites("Assets/Resources/icons/hindrances/porcupine.png");
            Sprite[] thornPlant = LoadSprites("Assets/Resources/icons/hindrances/thorn_plant (1).png");

            Sprite pressed = Named(mushrooms, "pressed");
            Sprite stretched = Named(mushrooms, "stretched");
            Sprite spiked = Named(porcupines, "porcupine_0");
            Sprite noSpikes = Named(porcupines, "porcupine_1");
            Sprite spike = Named(porcupines, "porcupine_2");
            Sprite plantBase = Named(thornPlant, "base");
            Sprite closed = Named(thornPlant, "close");
            Sprite open = Named(thornPlant, "open");
            Sprite stem = Named(thornPlant, "stem");

            ConfigureMushroomPrefab(pressed, stretched);
            ConfigurePorcupinePrefab(spiked, noSpikes, spike);
            ConfigureThornPlantPrefab(plantBase, closed, open, stem);

            ConfigureDefinition(
                "Hindrance_31_SpringMushroomBumpers.asset",
                HindranceType.SpringMushroomBumpers,
                "Spring Mushroom",
                "Animals that touch it bounce back and fly off-screen.",
                "Watch the spring mushroom!",
                26,
                new[] { pressed, stretched });

            ConfigureDefinition(
                "Hindrance_48_PorcupinePulse.asset",
                HindranceType.PorcupinePulse,
                "Porcupine",
                "Tap it to fire spikes at every animal on screen.",
                "Tap the porcupine!",
                31,
                new[] { spiked, noSpikes, spike });

            ConfigureDefinition(
                "Hindrance_49_VenusFlytrapRescue.asset",
                HindranceType.VenusFlytrapRescue,
                "Thorn Plant",
                "It periodically stretches out and snatches an animal.",
                "The thorn plant is hunting!",
                36,
                new[] { plantBase, closed, open, stem });

            ConfigureLevels();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LateHindranceSetup] Configured mushroom, porcupine, thorn plant, and Levels 26-39.");
        }

        private static void ConfigureMushroomPrefab(Sprite pressed, Sprite stretched)
        {
            const string path = PrefabRoot + "Hindrance_31_SpringMushroomBumpers.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                RemoveOtherHindrances<SpringMushroomHindrance>(root);
                SpringMushroomHindrance component = root.GetComponent<SpringMushroomHindrance>()
                    ?? root.AddComponent<SpringMushroomHindrance>();
                component.EditorConfigure(pressed, stretched);

                SpriteRenderer renderer = root.GetComponent<SpriteRenderer>()
                    ?? root.AddComponent<SpriteRenderer>();
                renderer.sprite = stretched;
                renderer.sortingOrder = 28;

                CircleCollider2D collider = root.GetComponent<CircleCollider2D>()
                    ?? root.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
                collider.offset = new Vector2(0f, -0.15f);
                collider.radius = stretched != null
                    ? Mathf.Max(stretched.bounds.extents.x, stretched.bounds.extents.y) * 0.72f
                    : 2.2f;

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigurePorcupinePrefab(Sprite spiked, Sprite noSpikes, Sprite spike)
        {
            const string path = PrefabRoot + "Hindrance_48_PorcupinePulse.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                RemoveOtherHindrances<PorcupineHindrance>(root);
                PorcupineHindrance component = root.GetComponent<PorcupineHindrance>()
                    ?? root.AddComponent<PorcupineHindrance>();
                component.EditorConfigure(spiked, noSpikes, spike);

                SpriteRenderer renderer = root.GetComponent<SpriteRenderer>()
                    ?? root.AddComponent<SpriteRenderer>();
                renderer.sprite = spiked;
                renderer.sortingOrder = 31;

                CircleCollider2D collider = root.GetComponent<CircleCollider2D>()
                    ?? root.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
                collider.offset = Vector2.zero;
                collider.radius = spiked != null
                    ? Mathf.Max(spiked.bounds.extents.x, spiked.bounds.extents.y) * 0.82f
                    : 2.8f;

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureThornPlantPrefab(Sprite plantBase, Sprite closed, Sprite open, Sprite stem)
        {
            const string path = PrefabRoot + "Hindrance_49_VenusFlytrapRescue.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                RemoveOtherHindrances<ThornPlantHindrance>(root);
                ThornPlantHindrance component = root.GetComponent<ThornPlantHindrance>()
                    ?? root.AddComponent<ThornPlantHindrance>();
                component.EditorConfigure(plantBase, closed, open, stem);

                SpriteRenderer rootRenderer = root.GetComponent<SpriteRenderer>();
                if (rootRenderer != null) UnityEngine.Object.DestroyImmediate(rootRenderer, true);
                CircleCollider2D rootCollider = root.GetComponent<CircleCollider2D>();
                if (rootCollider != null) UnityEngine.Object.DestroyImmediate(rootCollider, true);

                ConfigureChild(root.transform, "Base", plantBase, 24, 0.19f);
                ConfigureChild(root.transform, "Stem", stem, 25, 0.13f);
                ConfigureChild(root.transform, "Flower", closed, 26, 0.17f);

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

private static void ConfigureChild(Transform root, string name, Sprite sprite,
            int sortingOrder, float scale)
        {
            Transform child = root.Find(name);
            if (child == null)
            {
                GameObject created = new GameObject(name);
                child = created.transform;
                child.SetParent(root, false);
            }

            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null)
                renderer = child.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            child.localScale = Vector3.one * scale;
        }

        private static void RemoveOtherHindrances<T>(GameObject root) where T : MonoBehaviour, IHindrance
        {
            MonoBehaviour[] behaviours = root.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour is IHindrance && !(behaviour is T))
                    UnityEngine.Object.DestroyImmediate(behaviour, true);
            }
        }

        private static void ConfigureDefinition(string fileName, HindranceType type,
            string displayName, string description, string instruction, int unlockLevel,
            Sprite[] states)
        {
            HindranceData data = AssetDatabase.LoadAssetAtPath<HindranceData>(
                DefinitionRoot + fileName);
            if (data == null) throw new InvalidOperationException("Missing definition: " + fileName);

            data.hindranceType = type;
            data.displayName = displayName;
            data.effectDescription = description;
            data.tutorialInstruction = instruction;
            data.unlockLevel = unlockLevel;
            data.baseWeight = 1f;
            data.maxSimultaneous = 1;
            data.cooldown = type == HindranceType.VenusFlytrapRescue ? 11f : 9f;
            data.minDuration = 5f;
            data.maxDuration = type == HindranceType.VenusFlytrapRescue ? 14f : 9f;
            data.interactionWindow = type == HindranceType.PorcupinePulse ? 7f : 5f;
            data.requiredInteractions = 1;
            data.normalLevelEligible = true;
            data.megaLevelEligible = false;
            data.debugShowcaseOnly = false;
            data.stateSprites = states.Where(sprite => sprite != null).ToArray();

            if (type == HindranceType.SpringMushroomBumpers)
            {
                data.category = HindranceCategory.PhysicalMovement;
                data.inputMode = HindranceInputMode.None;
                data.targetScope = HindranceTargetScope.World;
                data.compatibilityTags = HindranceCompatibilityTag.GlobalMotion;
                data.exclusionTags = HindranceCompatibilityTag.GlobalMotion;
                data.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    PrefabRoot + "Hindrance_31_SpringMushroomBumpers.prefab");
            }
            else if (type == HindranceType.PorcupinePulse)
            {
                data.category = HindranceCategory.DynamicRiskReward;
                data.inputMode = HindranceInputMode.Tap;
                data.targetScope = HindranceTargetScope.Global;
                data.compatibilityTags = HindranceCompatibilityTag.ExclusiveTarget;
                data.exclusionTags = HindranceCompatibilityTag.ExclusiveTarget;
                data.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    PrefabRoot + "Hindrance_48_PorcupinePulse.prefab");
            }
            else
            {
                data.category = HindranceCategory.DynamicRiskReward;
                data.inputMode = HindranceInputMode.None;
                data.targetScope = HindranceTargetScope.Animal;
                data.compatibilityTags = HindranceCompatibilityTag.PhysicalHolder |
                    HindranceCompatibilityTag.ExclusiveTarget;
                data.exclusionTags = data.compatibilityTags;
                data.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    PrefabRoot + "Hindrance_49_VenusFlytrapRescue.prefab");
            }

            EditorUtility.SetDirty(data);
        }

        private static void ConfigureLevels()
        {
            for (int levelNumber = 26; levelNumber <= 39; levelNumber++)
            {
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(
                    $"Assets/Levels/LevelData/Level_{levelNumber:D2}.asset");
                if (level == null) continue;

                level.SetHindrancesArray(LevelConfigs(levelNumber));
                level.SetHindranceInitialDelay(4.5f);
                level.SetHindranceSpawnInterval(7f);
                level.SetMaxHindrancesActive(level.IsMegaLevel ? 1 : 2);
                EditorUtility.SetDirty(level);
            }
        }

        private static HindranceConfig[] LevelConfigs(int level)
        {
            HindranceConfig C(HindranceType type, float weight) =>
                new HindranceConfig { type = type, weight = weight, initialDelay = 0f };

            switch (level)
            {
                case 26: return new[] { C(HindranceType.SpringMushroomBumpers, 1.4f), C(HindranceType.WindGust, 0.8f), C(HindranceType.BubbleShield, 0.7f) };
                case 27: return new[] { C(HindranceType.SpringMushroomBumpers, 1.3f), C(HindranceType.Bomb, 0.8f), C(HindranceType.FallingLeaves, 0.8f) };
                case 28: return new[] { C(HindranceType.SpringMushroomBumpers, 1.3f), C(HindranceType.IceCube, 0.8f), C(HindranceType.ThiefBird, 0.7f) };
                case 29: return new[] { C(HindranceType.SpringMushroomBumpers, 1.2f), C(HindranceType.InkSquid, 0.8f), C(HindranceType.AlarmClock, 0.8f) };
                case 31: return new[] { C(HindranceType.PorcupinePulse, 1.4f), C(HindranceType.WindGust, 0.8f), C(HindranceType.BubbleShield, 0.7f) };
                case 32: return new[] { C(HindranceType.PorcupinePulse, 1.3f), C(HindranceType.Bomb, 0.8f), C(HindranceType.FallingLeaves, 0.8f) };
                case 33: return new[] { C(HindranceType.PorcupinePulse, 1.2f), C(HindranceType.SpringMushroomBumpers, 0.9f), C(HindranceType.KnightHelmet, 0.7f) };
                case 34: return new[] { C(HindranceType.PorcupinePulse, 1.2f), C(HindranceType.IceCube, 0.8f), C(HindranceType.StormCloud, 0.7f) };
                case 36: return new[] { C(HindranceType.VenusFlytrapRescue, 1.4f), C(HindranceType.WindGust, 0.8f), C(HindranceType.BubbleShield, 0.7f) };
                case 37: return new[] { C(HindranceType.VenusFlytrapRescue, 1.3f), C(HindranceType.SpringMushroomBumpers, 0.9f), C(HindranceType.FallingLeaves, 0.8f) };
                case 38: return new[] { C(HindranceType.VenusFlytrapRescue, 1.2f), C(HindranceType.PorcupinePulse, 0.9f), C(HindranceType.Bomb, 0.8f) };
                case 39: return new[] { C(HindranceType.VenusFlytrapRescue, 1.2f), C(HindranceType.SpringMushroomBumpers, 0.9f), C(HindranceType.InkSquid, 0.8f) };
                default: return Array.Empty<HindranceConfig>();
            }
        }

        private static Sprite[] LoadSprites(string path)
        {
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            if (sprites.Length == 0) throw new InvalidOperationException("No sprites found at " + path);
            return sprites;
        }

        private static Sprite Named(Sprite[] sprites, string name)
        {
            Sprite sprite = sprites.FirstOrDefault(candidate =>
                string.Equals(candidate.name, name, StringComparison.OrdinalIgnoreCase));
            if (sprite == null) throw new InvalidOperationException("Missing sprite slice: " + name);
            return sprite;
        }
    }
}
#endif
