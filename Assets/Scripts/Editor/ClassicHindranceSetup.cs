#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using AnimalFall.Core.Animals;
using AnimalFall.Core.Hindrances;
using AnimalFall.Core.Hindrances.EnvironmentMods;
using AnimalFall.Core.Hindrances.Penalties;
using AnimalFall.Core.Hindrances.TapModifiers;
using AnimalFall.Data;

namespace AnimalFall.EditorTools
{
    /// <summary>Creates production assets and applies the level 40-100 classic-hindrance schedule.</summary>
    public static class ClassicHindranceSetup
    {
        private const string PrefabFolder = "Assets/Prefabs/Hindrances";
        private const string DefinitionFolder = "Assets/Resources/Hindrances/Definitions";
        private const string RegistryPath = "Assets/Resources/Hindrances/HindranceRegistry.asset";
        private const string BatSheetPath = "Assets/Resources/icons/hindrances/bat.png";

        private sealed class DefinitionSpec
        {
            public HindranceType Type;
            public int Unlock;
            public string DisplayName;
            public string Description;
            public string Tutorial;
            public HindranceCategory Category;
            public HindranceCompatibilityTag Tags;
            public HindranceInputMode InputMode;
            public HindranceTargetScope Scope;
            public float Duration;
            public float Cooldown;
            public float Weight;
        }

        [MenuItem("Tools/Animal Fall/Setup Classic Hindrances 40-100")]
        public static void Setup()
        {
            EnsureFolder("Assets/Prefabs", "Hindrances");
            EnsureFolder("Assets/Resources/Hindrances", "Definitions");

            Sprite jellyfish = FirstSprite("Assets/Resources/icons/hindrances/jellyfish.png");
            Sprite laser = FirstSprite("Assets/Resources/icons/hindrances/Laser.png");
            Sprite eagle = FirstSprite("Assets/Resources/icons/hindrances/eagle.png");
            Sprite woodenPig = FirstSprite("Assets/Resources/icons/hindrances/wooden_pig.png");
            Sprite[] tornadoFrames = OrderedSprites("Assets/Resources/icons/hindrances/tornado.png");
            Sprite[] portalSprites = OrderedSprites("Assets/Resources/icons/hindrances/portal.png");
            Sprite[] fanFrames = OrderedSprites("Assets/Resources/icons/hindrances/fan.png");
            ConfigureBatSheet();
            Sprite[] batFrames = OrderedSprites(BatSheetPath);
            Sprite bluePortal = portalSprites.FirstOrDefault(sprite => sprite.name == "portal_0") ?? portalSprites.FirstOrDefault();
            Sprite orangePortal = portalSprites.FirstOrDefault(sprite => sprite.name == "portal_2") ?? portalSprites.LastOrDefault();

            GameObject tornadoPrefab = CreateOrUpdatePrefab(
                HindranceType.Tornado, typeof(TornadoHindrance), tornadoFrames.FirstOrDefault(), false,
                component => ((TornadoHindrance)component).EditorConfigure(tornadoFrames));
            GameObject jellyfishPrefab = CreateOrUpdatePrefab(
                HindranceType.Jellyfish, typeof(JellyfishHindrance), jellyfish, true,
                component => ((JellyfishHindrance)component).EditorConfigure(jellyfish));
            GameObject laserPrefab = CreateOrUpdatePrefab(
                HindranceType.Laser, typeof(LaserHindrance), laser, false,
                component => ((LaserHindrance)component).EditorConfigure(laser));
            GameObject eaglePrefab = CreateOrUpdatePrefab(
                HindranceType.Eagle, typeof(EagleHindrance), eagle, false,
                component => ((EagleHindrance)component).EditorConfigure(eagle));
            GameObject woodenPigPrefab = CreateOrUpdatePrefab(
                HindranceType.WoodenPig, typeof(WoodenPigHindrance), woodenPig, true,
                component => ((WoodenPigHindrance)component).EditorConfigure(woodenPig));
            GameObject portalPrefab = CreateOrUpdatePrefab(
                HindranceType.Portal, typeof(PortalHindrance), bluePortal, false,
                component => ((PortalHindrance)component).EditorConfigure(bluePortal, orangePortal));
            GameObject fanPrefab = CreateOrUpdatePrefab(
                HindranceType.Fan, typeof(FanHindrance), fanFrames.FirstOrDefault(), false,
                component => ((FanHindrance)component).EditorConfigure(fanFrames));
            GameObject batSwarmPrefab = CreateOrUpdatePrefab(
                HindranceType.BatSwarm, typeof(BatSwarmHindrance), batFrames.FirstOrDefault(), false,
                component => ((BatSwarmHindrance)component).EditorConfigure(batFrames));

            var definitions = new Dictionary<HindranceType, HindranceData>
            {
                [HindranceType.Tornado] = CreateOrUpdateDefinition(Spec(HindranceType.Tornado, 51, "Tornado", "Carries nearby animals away in its vortex.", "Keep animals away from the tornado!", HindranceCategory.EnvironmentModifier, HindranceCompatibilityTag.GlobalMotion, HindranceInputMode.None, HindranceTargetScope.Global, 6f, 11f, 0.9f), tornadoPrefab, tornadoFrames.FirstOrDefault(), tornadoFrames),
                [HindranceType.Jellyfish] = CreateOrUpdateDefinition(Spec(HindranceType.Jellyfish, 40, "Jellyfish", "Tapping it shocks every animal on screen.", "Don't tap the jellyfish!", HindranceCategory.Penalty, HindranceCompatibilityTag.ExclusiveTarget, HindranceInputMode.Tap, HindranceTargetScope.World, 7f, 10f, 1f), jellyfishPrefab, jellyfish, new[] { jellyfish }),
                [HindranceType.Laser] = CreateOrUpdateDefinition(Spec(HindranceType.Laser, 43, "Laser", "A live beam eliminates animals that cross it.", "Avoid the laser beam!", HindranceCategory.EnvironmentModifier, HindranceCompatibilityTag.ExclusiveTarget, HindranceInputMode.None, HindranceTargetScope.World, 8f, 11f, 0.95f), laserPrefab, laser, new[] { laser }),
                [HindranceType.Eagle] = CreateOrUpdateDefinition(Spec(HindranceType.Eagle, 46, "Eagle", "Flies horizontally and carries away two animals.", "The eagle steals two animals!", HindranceCategory.Penalty, HindranceCompatibilityTag.GlobalMotion, HindranceInputMode.None, HindranceTargetScope.Animal, 7f, 12f, 0.85f), eaglePrefab, eagle, new[] { eagle }),
                [HindranceType.WoodenPig] = CreateOrUpdateDefinition(Spec(HindranceType.WoodenPig, 48, "Wooden Pig", "A harmless decoy that ignores taps.", "Wooden pigs are decoys.", HindranceCategory.TapModifier, HindranceCompatibilityTag.None, HindranceInputMode.Tap, HindranceTargetScope.World, 8f, 8f, 1.1f), woodenPigPrefab, woodenPig, new[] { woodenPig }),
                [HindranceType.Portal] = CreateOrUpdateDefinition(Spec(HindranceType.Portal, 53, "Portal Pair", "Animals entering one portal emerge from the other.", "Portals relocate falling animals.", HindranceCategory.EnvironmentModifier, HindranceCompatibilityTag.GlobalMotion, HindranceInputMode.None, HindranceTargetScope.Global, 9f, 12f, 0.9f), portalPrefab, bluePortal, new[] { bluePortal, orangePortal }),
                [HindranceType.Fan] = CreateOrUpdateDefinition(Spec(HindranceType.Fan, 56, "Fan", "Blows animals diagonally upward at 45 degrees.", "The fan blows up and right!", HindranceCategory.EnvironmentModifier, HindranceCompatibilityTag.GlobalMotion, HindranceInputMode.None, HindranceTargetScope.Global, 9f, 12f, 0.9f), fanPrefab, fanFrames.FirstOrDefault(), fanFrames),
                [HindranceType.BatSwarm] = CreateOrUpdateDefinition(Spec(HindranceType.BatSwarm, 59, "Bat Swarm", "Ten to twelve bats sweep across and carry away every visible animal.", "The bat swarm takes every animal!", HindranceCategory.Penalty, HindranceCompatibilityTag.GlobalMotion | HindranceCompatibilityTag.ExclusiveTarget, HindranceInputMode.None, HindranceTargetScope.Global, 6f, 14f, 0.75f), batSwarmPrefab, batFrames.FirstOrDefault(), batFrames)
            };

            UpdateRegistry(definitions);
            UpdateLevelAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ClassicHindranceSetup] Implemented Jellyfish, Laser, Eagle, Wooden Pig, Tornado, Portal, Fan, and Bat Swarm across normal levels 40-100.");
        }

        private static void ConfigureBatSheet()
        {
            AssetDatabase.ImportAsset(BatSheetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(BatSheetPath) as TextureImporter;
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(BatSheetPath);
            if (importer == null || texture == null)
                throw new InvalidOperationException($"Missing bat spritesheet at {BatSheetPath}.");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 256f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.isReadable = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();

            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(BatSheetPath);
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            Dictionary<string, GUID> existing = provider.GetSpriteRects()
                .ToDictionary(rect => rect.name, rect => rect.spriteID);
            var rects = new SpriteRect[5];
            for (int i = 0; i < rects.Length; i++)
            {
                int xMin = Mathf.RoundToInt(i * texture.width / 5f);
                int xMax = Mathf.RoundToInt((i + 1) * texture.width / 5f);
                string spriteName = "bat_" + i;
                rects[i] = new SpriteRect
                {
                    name = spriteName,
                    rect = new Rect(xMin, 0f, xMax - xMin, texture.height),
                    pivot = new Vector2(0.5f, 0.5f),
                    alignment = SpriteAlignment.Custom,
                    spriteID = existing.TryGetValue(spriteName, out GUID id) ? id : GUID.Generate()
                };
            }
            provider.SetSpriteRects(rects);
            provider.Apply();
            importer.SaveAndReimport();
        }

        private static DefinitionSpec Spec(HindranceType type, int unlock, string displayName,
            string description, string tutorial, HindranceCategory category,
            HindranceCompatibilityTag tags, HindranceInputMode inputMode,
            HindranceTargetScope scope, float duration, float cooldown, float weight)
            => new DefinitionSpec
            {
                Type = type, Unlock = unlock, DisplayName = displayName, Description = description,
                Tutorial = tutorial, Category = category, Tags = tags, InputMode = inputMode,
                Scope = scope, Duration = duration, Cooldown = cooldown, Weight = weight
            };

        private static GameObject CreateOrUpdatePrefab(HindranceType type, Type componentType,
            Sprite icon, bool needsTapCollider, Action<HindranceBase> configure)
        {
            string path = type == HindranceType.Tornado
                ? $"{PrefabFolder}/Hindrance_16_Tornado.prefab"
                : $"{PrefabFolder}/Hindrance_{(int)type:D2}_{type}.prefab";
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            GameObject root = exists ? PrefabUtility.LoadPrefabContents(path) : new GameObject(type.ToString());
            try
            {
                root.name = type.ToString();
                SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
                if (!renderer) renderer = root.AddComponent<SpriteRenderer>();
                if (!renderer) throw new InvalidOperationException($"Could not add SpriteRenderer to {root.name}.");
                renderer.sprite = icon;
                renderer.sortingOrder = 30;

                HindranceBase component = root.GetComponent(componentType) as HindranceBase;
                if (!component) component = root.AddComponent(componentType) as HindranceBase;
                if (!component) throw new InvalidOperationException($"Could not add {componentType.Name} to {root.name}.");
                foreach (HindranceBase extra in root.GetComponents<HindranceBase>())
                    if (extra != component) UnityEngine.Object.DestroyImmediate(extra);

                CircleCollider2D circle = root.GetComponent<CircleCollider2D>();
                if (needsTapCollider)
                {
                    if (circle == null) circle = root.AddComponent<CircleCollider2D>();
                    circle.isTrigger = true;
                    if (icon != null)
                    {
                        circle.radius = Mathf.Max(icon.bounds.size.x, icon.bounds.size.y) * 0.5f;
                        circle.offset = icon.bounds.center;
                    }
                }
                else if (circle != null)
                {
                    UnityEngine.Object.DestroyImmediate(circle);
                }

                configure(component);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                if (exists) PrefabUtility.UnloadPrefabContents(root);
                else UnityEngine.Object.DestroyImmediate(root);
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static HindranceData CreateOrUpdateDefinition(DefinitionSpec spec, GameObject prefab,
            Sprite icon, Sprite[] states)
        {
            string path = spec.Type == HindranceType.Tornado
                ? $"{DefinitionFolder}/Hindrance_16_Tornado.asset"
                : $"{DefinitionFolder}/Hindrance_{(int)spec.Type:D2}_{spec.Type}.asset";
            HindranceData data = AssetDatabase.LoadAssetAtPath<HindranceData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<HindranceData>();
                AssetDatabase.CreateAsset(data, path);
            }

            data.hindranceType = spec.Type;
            data.prefab = prefab;
            data.icon = icon;
            data.unlockLevel = spec.Unlock;
            data.displayName = spec.DisplayName;
            data.effectDescription = spec.Description;
            data.tutorialInstruction = spec.Tutorial;
            data.category = spec.Category;
            data.baseWeight = spec.Weight;
            data.difficultyTier = spec.Unlock < 50 ? 1 : 2;
            data.minDuration = spec.Duration;
            data.maxDuration = spec.Duration;
            data.maxSimultaneous = 1;
            data.cooldown = spec.Cooldown;
            data.compatibilityTags = spec.Tags;
            data.exclusionTags = HindranceCompatibilityTag.None;
            data.inputMode = spec.InputMode;
            data.targetScope = spec.Scope;
            data.normalLevelEligible = true;
            data.megaLevelEligible = false;
            data.debugShowcaseOnly = false;
            data.stateSprites = states.Where(sprite => sprite != null).ToArray();
            data.telegraphDuration = spec.Type == HindranceType.Laser ? 0.8f : 0.35f;
            data.interactionWindow = spec.Duration;
            data.requiredInteractions = 1;
            EditorUtility.SetDirty(data);
            return data;
        }

        private static void UpdateRegistry(Dictionary<HindranceType, HindranceData> changed)
        {
            HindranceRegistry registry = AssetDatabase.LoadAssetAtPath<HindranceRegistry>(RegistryPath);
            if (registry == null) throw new InvalidOperationException("HindranceRegistry asset is missing.");
            var entries = registry.Entries != null
                ? registry.Entries.Where(entry => entry != null).ToList()
                : new List<HindranceRegistry.Entry>();

            foreach (KeyValuePair<HindranceType, HindranceData> pair in changed)
            {
                HindranceRegistry.Entry entry = entries.FirstOrDefault(item => item.type == pair.Key);
                if (entry == null)
                {
                    entry = new HindranceRegistry.Entry { type = pair.Key };
                    entries.Add(entry);
                }
                entry.data = pair.Value;
            }
            entries.Sort((a, b) => ((int)a.type).CompareTo((int)b.type));
            registry.EditorSetEntries(entries.ToArray());
        }

        private static void UpdateLevelAssets()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { "Assets/Levels/LevelData" });
            foreach (string guid in guids)
            {
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid));
                if (level == null) continue;
                var configs = new List<HindranceConfig>();
                if (level.Hindrances != null)
                    configs.AddRange(level.Hindrances.Where(config => config != null &&
                        !LateClassicHindranceSchedule.IsLateClassicType(config.type)));

                bool normalInRange = level.LevelNumber >= LateClassicHindranceSchedule.FirstLevel &&
                    level.LevelNumber <= LateClassicHindranceSchedule.LastLevel &&
                    !level.IsMegaLevel && !level.IsConfiguredMegaShooter;
                if (normalInRange) configs.AddRange(LateClassicHindranceSchedule.BuildConfigs(level.LevelNumber));

                level.SetHindrancesArray(configs.ToArray());
                if (normalInRange)
                {
                    level.SetMaxHindrancesActive(Mathf.Max(2, level.MaxHindrancesActive));
                    level.SetHindranceInitialDelay(Mathf.Min(5f, level.HindranceInitialDelay));
                    level.SetHindranceSpawnInterval(Mathf.Min(8f, level.HindranceSpawnInterval));
                }
                EditorUtility.SetDirty(level);
            }
        }

        private static Sprite FirstSprite(string path) => OrderedSprites(path).FirstOrDefault();

        private static Sprite[] OrderedSprites(string path)
        {
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            Array.Sort(sprites, (a, b) => ExtractIndex(a.name).CompareTo(ExtractIndex(b.name)));
            return sprites;
        }

        private static int ExtractIndex(string name)
        {
            int split = name.LastIndexOf('_');
            return split >= 0 && int.TryParse(name.Substring(split + 1), out int value) ? value : 0;
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
