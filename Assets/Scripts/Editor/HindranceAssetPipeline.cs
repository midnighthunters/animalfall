#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using AnimalFall.Core.Hindrances;
using AnimalFall.Core.Hindrances.Advanced;
using AnimalFall.Core.Hindrances.EnvironmentMods;
using AnimalFall.Core.Hindrances.Penalties;
using AnimalFall.Core.Hindrances.ScreenBlockers;
using AnimalFall.Core.Hindrances.TapModifiers;
using AnimalFall.Core.Hindrances.New;
using AnimalFall.Data;
using AnimalFall.Effects;
using AnimalFall.Managers;

namespace AnimalFall.EditorTools
{
    public static class HindranceAssetPipeline
    {
        private const int Cell = 512;
        private const string SheetRoot = "Assets/Resources/icons/hindrances/Sheets";
        private const string DefinitionRoot = "Assets/Resources/Hindrances/Definitions";
        private const string PrefabRoot = "Assets/Prefabs/Hindrances";
        private const string SupportPrefabRoot = "Assets/Prefabs/Hindrances/VFX";
        private const string RegistryPath = "Assets/Resources/Hindrances/HindranceRegistry.asset";
        private const string AtlasPath = "Assets/Resources/Hindrances/HindranceAtlas.spriteatlas";
        private const string ManifestPath = "Assets/ArtSource/Hindrances/hindrance_manifest.json";
        private const string AssignmentPath = "Assets/ArtSource/Hindrances/hindrance_level_assignments.json";

        private sealed class SheetSpec
        {
            public readonly string File;
            public readonly string[] Names;
            public SheetSpec(string file, params string[] names) { File = file; Names = names; }
            public string Path => $"{SheetRoot}/{File}";
        }

        [Serializable] private sealed class Manifest { public SheetManifest[] sheets; }
        [Serializable] private sealed class SheetManifest
        { public string path; public int width; public int height; public int cellSize; public SpriteManifest[] sprites; }
        [Serializable] private sealed class SpriteManifest
        { public string logicalName; public int row; public int column; public string frame; public float pivotX; public float pivotY; public float pixelsPerUnit; public string intendedPrefab; }
        [Serializable] private sealed class AssignmentManifest { public LevelAssignment[] levels; public string[] showcaseOnly; }
        [Serializable] private sealed class LevelAssignment { public int level; public bool mega; public string[] hindrances; }

        private static readonly SheetSpec[] Sheets =
        {
            new SheetSpec("hindrance_icons_current_01.png",
                "icon_bomb","icon_alarm_clock","icon_poison_vial","icon_thief_bird",
                "icon_knight_helmet","icon_bubble_shield","icon_ice_cube","icon_ghost_animal",
                "icon_ink_squid","icon_storm_cloud","icon_flashbang","icon_falling_leaves",
                "icon_wind_gust","icon_zero_gravity","icon_black_hole","icon_tornado"),
            new SheetSpec("hindrance_icons_current_02.png",
                "icon_magnet_trap","icon_mirror_mode","icon_cursed_skull","icon_paired_animal",
                "helmet_three","helmet_two","helmet_one","helmet_broken",
                "bubble_intact","bubble_popped","ice_intact","ice_melting",
                "ghost_outline","thief_warning","blackhole_warning","tornado_accent"),
            new SheetSpec("hindrance_icons_01.png",
                "icon_spiderweb_curtain","icon_firefly_lock_key","icon_rhythm_totem","icon_traffic_light_owl",
                "icon_tracking_rescue_cage","icon_lasso_ring","icon_echo_tap_rune","icon_numbered_flock",
                "icon_moving_safe_halo","icon_keepers_whistle","icon_spring_mushrooms","icon_conveyor_clouds",
                "icon_crumbling_perches","icon_pendulum_vines","icon_seesaw_branch","icon_carousel_nests"),
            new SheetSpec("hindrance_icons_02.png",
                "icon_trapdoor_clouds","icon_rolling_log","icon_acorn_hail","icon_windmill_gate",
                "icon_lantern_spotlight","icon_eclipse_silhouettes","icon_memory_fog","icon_colour_wash_rain",
                "icon_timer_moth","icon_goal_swap_monkey","icon_bee_swarm_guard","icon_porcupine_pulse",
                "icon_venus_flytrap","icon_raccoon_heist","utility_warning","utility_success"),
            new SheetSpec("hindrance_interactions.png",
                "web_intact","web_stretched","web_snapped","web_anchor","lock_closed","lock_open","key_firefly","totem_anticipation",
                "totem_green","owl_green","owl_amber","owl_red","cage_closed","cage_open","lasso_loop","echo_replay"),
            new SheetSpec("hindrance_physics_props.png",
                "mushroom_idle","mushroom_compressed","conveyor_left","conveyor_right","perch_intact","perch_cracked","perch_broken","vine_pendulum",
                "seesaw_balanced","seesaw_left","nest_occupied","nest_open","trapdoor_open","trapdoor_closed","rolling_log","windmill_safe_gap"),
            new SheetSpec("hindrance_dynamic_states.png",
                "lantern","lantern_light","eclipse_disc","memory_fog","colour_rain","moth_flying","moth_landed","monkey_idle",
                "monkey_swap","bees_orbit","bee_safe_opening","porcupine_extended","porcupine_retracted","flytrap_closed","flytrap_open","raccoon_run"),
            new SheetSpec("hindrance_vfx.png",
                "vfx_warning_shadow","vfx_warning_pulse","vfx_success_pulse","vfx_sparkles","vfx_dust","vfx_puff","vfx_impact","vfx_cut",
                "vfx_progress","vfx_protection","vfx_ripple","vfx_swoosh","vfx_coins","vfx_leaves","vfx_splash","vfx_blocked"),
        };

        private static Dictionary<string, GameObject> _supportPrefabs;

        [MenuItem("Animal Fall/Hindrances/Rebuild Production Assets")]
        public static void RebuildAll()
        {
            EnsureFolder(DefinitionRoot); EnsureFolder(PrefabRoot); EnsureFolder(SupportPrefabRoot); EnsureFolder("Assets/ArtSource/Hindrances");
            foreach (SheetSpec sheet in Sheets) SliceSheet(sheet);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            CreateOrUpdateAtlas();
            Dictionary<string, Sprite> sprites = LoadSprites();
            _supportPrefabs = CreateSupportPrefabs(sprites);
            HindranceRegistry registry = CreateDefinitionsAndPrefabs(sprites);
            AssignRegistryToScenes(registry);
            ApplyCuratedLevelPools();
            WriteManifest();
            AssetDatabase.SaveAssets();
            List<string> issues = registry.ValidateRegistry(true);
            Debug.Log(issues.Count == 0
                ? "[HindranceAssetPipeline] Built and validated 50 definitions and prefabs."
                : "[HindranceAssetPipeline] Validation issues:\n" + string.Join("\n", issues));
            Selection.activeObject = registry;
        }

        [MenuItem("Animal Fall/Hindrances/Validate All")]
        public static void ValidateAll()
        {
            HindranceRegistry registry = AssetDatabase.LoadAssetAtPath<HindranceRegistry>(RegistryPath);
            if (registry == null) { Debug.LogError("Hindrance registry is missing. Run Rebuild Production Assets."); return; }
            List<string> issues = registry.ValidateRegistry(true);
            Debug.Log(issues.Count == 0 ? "[Hindrances] Validation passed: 50/50 complete."
                : "[Hindrances] Validation failed:\n" + string.Join("\n", issues));
        }

        private static void SliceSheet(SheetSpec sheet)
        {
            TextureImporter importer = AssetImporter.GetAtPath(sheet.Path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException($"Missing source sheet {sheet.Path}");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 256f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.isReadable = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            SetPlatform(importer, "Android", TextureImporterFormat.ASTC_6x6);
            SetPlatform(importer, "iPhone", TextureImporterFormat.ASTC_6x6);
            importer.SaveAndReimport();

            var factory = new SpriteDataProviderFactories(); factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            Dictionary<string, GUID> existing = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
            var rects = new SpriteRect[sheet.Names.Length];
            for (int i = 0; i < sheet.Names.Length; i++)
            {
                int row = i / 4, col = i % 4;
                rects[i] = new SpriteRect
                {
                    name = sheet.Names[i],
                    rect = new Rect(col * Cell, 2048 - (row + 1) * Cell, Cell, Cell),
                    pivot = new Vector2(0.5f, 0.5f),
                    alignment = SpriteAlignment.Custom,
                    spriteID = existing.TryGetValue(sheet.Names[i], out GUID id) ? id : GUID.Generate()
                };
            }
            provider.SetSpriteRects(rects); provider.Apply(); importer.SaveAndReimport();
        }

        private static void SetPlatform(TextureImporter importer, string platform, TextureImporterFormat format)
        {
            TextureImporterPlatformSettings setting = importer.GetPlatformTextureSettings(platform);
            setting.name = platform; setting.overridden = true; setting.maxTextureSize = 2048;
            setting.format = format; setting.compressionQuality = 80;
            importer.SetPlatformTextureSettings(setting);
        }

        private static void CreateOrUpdateAtlas()
        {
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AtlasPath);
            if (atlas == null) { atlas = new SpriteAtlas(); AssetDatabase.CreateAsset(atlas, AtlasPath); }
            var packing = atlas.GetPackingSettings(); packing.enableRotation = false; packing.enableTightPacking = false; packing.padding = 8; atlas.SetPackingSettings(packing);
            UnityEngine.Object[] current = atlas.GetPackables(); if (current.Length > 0) SpriteAtlasExtensions.Remove(atlas, current);
            UnityEngine.Object[] textures = Sheets.Select(s => AssetDatabase.LoadAssetAtPath<Texture2D>(s.Path)).Where(t => t != null).Cast<UnityEngine.Object>().ToArray();
            SpriteAtlasExtensions.Add(atlas, textures); EditorUtility.SetDirty(atlas);
        }

        private static Dictionary<string, Sprite> LoadSprites()
        {
            var result = new Dictionary<string, Sprite>();
            foreach (SheetSpec sheet in Sheets)
                foreach (Sprite sprite in AssetDatabase.LoadAllAssetsAtPath(sheet.Path).OfType<Sprite>()) result[sprite.name] = sprite;
            return result;
        }

        private static HindranceRegistry CreateDefinitionsAndPrefabs(Dictionary<string, Sprite> sprites)
        {
            HindranceRegistry registry = AssetDatabase.LoadAssetAtPath<HindranceRegistry>(RegistryPath);
            if (registry == null) { registry = ScriptableObject.CreateInstance<HindranceRegistry>(); AssetDatabase.CreateAsset(registry, RegistryPath); }
            var entries = new HindranceRegistry.Entry[50];
            for (int id = 1; id <= 50; id++)
            {
                HindranceType type = (HindranceType)id;
                Sprite icon = sprites[IconName(id)];
                string prefabPath = $"{PrefabRoot}/Hindrance_{id:D2}_{type}.prefab";
                GameObject prefab = CreatePrefab(type, icon, prefabPath);
                string definitionPath = $"{DefinitionRoot}/Hindrance_{id:D2}_{type}.asset";
                HindranceData data = AssetDatabase.LoadAssetAtPath<HindranceData>(definitionPath);
                if (data == null) { data = ScriptableObject.CreateInstance<HindranceData>(); AssetDatabase.CreateAsset(data, definitionPath); }
                ConfigureDefinition(data, type, prefab, icon, sprites);
                entries[id - 1] = new HindranceRegistry.Entry { type = type, data = data };
            }
            registry.EditorSetEntries(entries); return registry;
        }

        private static GameObject CreatePrefab(HindranceType type, Sprite icon, string path)
        {
            GameObject root = new GameObject(type.ToString());
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>(); renderer.sprite = icon; renderer.sortingOrder = 30;
            CircleCollider2D collider = root.AddComponent<CircleCollider2D>(); collider.radius = 0.72f; collider.isTrigger = true;
            AddRuntimeComponent(root, type);
            if (type == HindranceType.FallingLeaves && _supportPrefabs != null && _supportPrefabs.TryGetValue("leaf", out GameObject leaf))
            {
                SerializedObject serialized = new SerializedObject(root.GetComponent<FallingLeavesHindrance>());
                serialized.FindProperty("_leafPrefab").objectReferenceValue = leaf;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            root.transform.localScale = Vector3.one * 0.8f;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path); UnityEngine.Object.DestroyImmediate(root); return prefab;
        }

        private static Dictionary<string, GameObject> CreateSupportPrefabs(Dictionary<string, Sprite> sprites)
        {
            return new Dictionary<string, GameObject>
            {
                ["leaf"] = SaveSupportPrefab("Hindrance_Leaf", sprites["vfx_leaves"], 0.42f, 65),
                ["ink"] = SaveSupportPrefab("Overlay_Ink", sprites["icon_ink_squid"], 4.2f, 90),
                ["storm"] = SaveSupportPrefab("Overlay_Storm", sprites["icon_storm_cloud"], 4.8f, 88),
                ["flash"] = SaveSupportPrefab("Overlay_Flash", sprites["vfx_warning_pulse"], 9f, 95),
                ["border"] = SaveSupportPrefab("Overlay_Border", sprites["vfx_success_pulse"], 8f, 94)
            };
        }

        private static GameObject SaveSupportPrefab(string name, Sprite sprite, float scale, int sortingOrder)
        {
            string path = $"{SupportPrefabRoot}/{name}.prefab";
            GameObject root = new GameObject(name);
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite; renderer.sortingOrder = sortingOrder;
            root.transform.localScale = Vector3.one * scale;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void AddRuntimeComponent(GameObject root, HindranceType type)
        {
            switch (type)
            {
                case HindranceType.Bomb: root.AddComponent<BombHindrance>(); break;
                case HindranceType.AlarmClock: root.AddComponent<AlarmClockHindrance>(); break;
                case HindranceType.PoisonVial: root.AddComponent<PoisonVialHindrance>(); break;
                case HindranceType.ThiefBird: root.AddComponent<ThiefBirdHindrance>(); break;
                case HindranceType.KnightHelmet: root.AddComponent<KnightHelmetHindrance>(); break;
                case HindranceType.BubbleShield: root.AddComponent<BubbleShieldHindrance>(); break;
                case HindranceType.IceCube: root.AddComponent<IceCubeHindrance>(); break;
                case HindranceType.GhostAnimal: root.AddComponent<GhostAnimalHindrance>(); break;
                case HindranceType.InkSquid: root.AddComponent<InkSquidHindrance>(); break;
                case HindranceType.StormCloud: root.AddComponent<StormCloudHindrance>(); break;
                case HindranceType.Flashbang: root.AddComponent<FlashbangHindrance>(); break;
                case HindranceType.FallingLeaves: root.AddComponent<FallingLeavesHindrance>(); break;
                case HindranceType.WindGust: root.AddComponent<WindGustHindrance>(); break;
                case HindranceType.ZeroGravity: root.AddComponent<ZeroGravityHindrance>(); break;
                case HindranceType.BlackHole: root.AddComponent<BlackHoleHindrance>(); break;
                case HindranceType.Tornado: root.AddComponent<TornadoHindrance>(); break;
                case HindranceType.MagnetTrap: root.AddComponent<MagnetTrapHindrance>(); break;
                case HindranceType.MirrorMode: root.AddComponent<MirrorModeHindrance>(); break;
                case HindranceType.CursedSkull: root.AddComponent<CursedSkullHindrance>(); break;
                case HindranceType.PairedAnimal: root.AddComponent<PairedAnimalHindrance>(); break;
                default:
                    if ((int)type <= 30) root.AddComponent<InteractionRuleHindrance>().EditorConfigure(type, 7f, Required(type));
                    else if ((int)type <= 40) root.AddComponent<PhysicalMovementHindrance>().EditorConfigure(type, 7f);
                    else if ((int)type <= 44) root.AddComponent<VisibilityMemoryHindrance>().EditorConfigure(type, 6f);
                    else root.AddComponent<DynamicRiskRewardHindrance>().EditorConfigure(type, 7f, Required(type));
                    break;
            }
        }

        private static void ConfigureDefinition(HindranceData data, HindranceType type, GameObject prefab, Sprite icon, Dictionary<string, Sprite> sprites)
        {
            data.hindranceType = type; data.prefab = prefab; data.icon = icon; data.displayName = SplitName(type.ToString());
            data.tutorialInstruction = Tutorial(type); data.effectDescription = data.tutorialInstruction;
            data.unlockLevel = Mathf.Max(3, 3 + ((int)type - 1) * 2); data.baseWeight = 1f; data.maxSimultaneous = 1;
            data.cooldown = 12f; data.minDuration = 4f; data.maxDuration = 8f; data.interactionWindow = 5f; data.requiredInteractions = Required(type);
            data.category = Category(type); data.inputMode = InputMode(type); data.targetScope = Scope(type);
            data.compatibilityTags = Tags(type); data.exclusionTags = Tags(type); data.normalLevelEligible = true; data.megaLevelEligible = false;
            string statePrefix = (int)type <= 30 ? "web_" : (int)type <= 40 ? "mushroom_" : (int)type <= 44 ? "lantern" : "moth_";
            Sprite[] states = sprites.Where(kv => kv.Key.StartsWith(statePrefix, StringComparison.OrdinalIgnoreCase)).Select(kv => kv.Value).ToArray();
            data.stateSprites = states.Length > 0 ? states : new[] { icon, sprites["vfx_warning_pulse"], sprites["vfx_success_pulse"] };
            EditorUtility.SetDirty(data);
        }

        private static string IconName(int id)
        {
            if (id <= 16) return Sheets[0].Names[id - 1];
            if (id <= 20) return Sheets[1].Names[id - 17];
            if (id <= 36) return Sheets[2].Names[id - 21];
            return Sheets[3].Names[id - 37];
        }

        private static HindranceCategory Category(HindranceType t) => (int)t <= 20 ?
            ((int)t <= 4 ? HindranceCategory.Penalty : (int)t <= 8 ? HindranceCategory.TapModifier : (int)t <= 12 ? HindranceCategory.ScreenBlocker : (int)t <= 16 ? HindranceCategory.EnvironmentModifier : HindranceCategory.Advanced) :
            ((int)t <= 30 ? HindranceCategory.InteractionRule : (int)t <= 40 ? HindranceCategory.PhysicalMovement : (int)t <= 44 ? HindranceCategory.VisibilityMemory : HindranceCategory.DynamicRiskReward);

        private static HindranceInputMode InputMode(HindranceType t)
        {
            if (t == HindranceType.SpiderwebCurtain || t == HindranceType.IceCube) return HindranceInputMode.Trace;
            if (t == HindranceType.LassoRing) return HindranceInputMode.Lasso;
            if (t == HindranceType.TrackingRescueCage) return HindranceInputMode.Hold;
            if (t == HindranceType.LanternSpotlight) return HindranceInputMode.Drag;
            if (t == HindranceType.RhythmTotem) return HindranceInputMode.Rhythm;
            return HindranceInputMode.Tap;
        }

        private static HindranceTargetScope Scope(HindranceType t) =>
            t == HindranceType.RaccoonCoinHeist ? HindranceTargetScope.OptionalReward :
            Tags(t).HasFlag(HindranceCompatibilityTag.FullScreenVisibility) ? HindranceTargetScope.Global :
            Tags(t).HasFlag(HindranceCompatibilityTag.ExclusiveTarget) || Tags(t).HasFlag(HindranceCompatibilityTag.PhysicalHolder) ? HindranceTargetScope.Animal : HindranceTargetScope.World;

        private static HindranceCompatibilityTag Tags(HindranceType t)
        {
            if (t == HindranceType.MirrorMode || t == HindranceType.MagnetTrap || t == HindranceType.EchoTapRune) return HindranceCompatibilityTag.InputTransform;
            if (t == HindranceType.IceCube || t == HindranceType.SpiderwebCurtain || t == HindranceType.LassoRing || t == HindranceType.TrackingRescueCage) return HindranceCompatibilityTag.ExclusiveGesture | HindranceCompatibilityTag.ExclusiveTarget;
            if (t == HindranceType.InkSquid || t == HindranceType.StormCloud || t == HindranceType.Flashbang || t == HindranceType.LanternSpotlight || t == HindranceType.EclipseSilhouettes || t == HindranceType.MemoryFog) return HindranceCompatibilityTag.FullScreenVisibility;
            if (t == HindranceType.WindGust || t == HindranceType.ZeroGravity || t == HindranceType.BlackHole) return HindranceCompatibilityTag.GlobalMotion;
            if (t == HindranceType.ConveyorClouds || t == HindranceType.CrumblingPerches || t == HindranceType.PendulumVines || t == HindranceType.SeesawBranch || t == HindranceType.CarouselNests || t == HindranceType.TrapdoorClouds || t == HindranceType.VenusFlytrapRescue) return HindranceCompatibilityTag.PhysicalHolder | HindranceCompatibilityTag.ExclusiveTarget;
            if (t == HindranceType.KeepersWhistle || t == HindranceType.NumberedFlock || t == HindranceType.GoalSwapMonkey || t == HindranceType.MemoryFog) return HindranceCompatibilityTag.GoalRule;
            if (t == HindranceType.RaccoonCoinHeist) return HindranceCompatibilityTag.OptionalReward;
            if (t == HindranceType.ThiefBird || t == HindranceType.PairedAnimal || t == HindranceType.FireflyLockAndKey || t == HindranceType.BeeSwarmGuard || t == HindranceType.PorcupinePulse || t == HindranceType.KnightHelmet) return HindranceCompatibilityTag.ExclusiveTarget;
            return HindranceCompatibilityTag.None;
        }

        private static int Required(HindranceType t) => t == HindranceType.TimerMoth || t == HindranceType.RaccoonCoinHeist ? 3 : t == HindranceType.VenusFlytrapRescue ? 4 : t == HindranceType.TrackingRescueCage ? 2 : 1;

        private static string Tutorial(HindranceType t)
        {
            switch (t)
            {
                case HindranceType.SpiderwebCurtain: return "Trace the glowing strand!";
                case HindranceType.FireflyLockAndKey: return "Catch the matching key!";
                case HindranceType.RhythmTotem: return "Tap on the green beat!";
                case HindranceType.TrackingRescueCage: return "Hold the moving latch!";
                case HindranceType.LassoRing: return "Circle the marked animal!";
                case HindranceType.LanternSpotlight: return "Move the light, then tap!";
                case HindranceType.TimerMoth: return "Tap the moth three times!";
                case HindranceType.VenusFlytrapRescue: return "Tap left and right!";
                case HindranceType.RaccoonCoinHeist: return "Tap the bag for coins!";
                default: return $"Counter the {SplitName(t.ToString())}!";
            }
        }

        private static string SplitName(string value) => System.Text.RegularExpressions.Regex.Replace(value, "([a-z])([A-Z])", "$1 $2");

        private static void AssignRegistryToScenes(HindranceRegistry registry)
        {
            string[] paths = { "Assets/Scenes/GameScene.unity", "Assets/Scenes/MainScene.unity" };
            Scene original = SceneManager.GetActiveScene();
            foreach (string path in paths)
            {
                if (!File.Exists(path)) continue;
                Scene scene = SceneManager.GetSceneByPath(path);
                bool wasLoaded = scene.IsValid() && scene.isLoaded;
                if (!wasLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (HindranceManager manager in root.GetComponentsInChildren<HindranceManager>(true))
                    {
                        SerializedObject so = new SerializedObject(manager); so.FindProperty("_registry").objectReferenceValue = registry; so.ApplyModifiedPropertiesWithoutUndo();
                    }
                    foreach (ScreenEffects effects in root.GetComponentsInChildren<ScreenEffects>(true))
                    {
                        SerializedObject so = new SerializedObject(effects);
                        so.FindProperty("_inkOverlayPrefab").objectReferenceValue = _supportPrefabs["ink"];
                        so.FindProperty("_stormGradientPrefab").objectReferenceValue = _supportPrefabs["storm"];
                        so.FindProperty("_flashbangPrefab").objectReferenceValue = _supportPrefabs["flash"];
                        so.FindProperty("_borderFlashPrefab").objectReferenceValue = _supportPrefabs["border"];
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
                EditorSceneManager.SaveScene(scene);
                if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
            }
            if (original.IsValid() && !original.isLoaded) EditorSceneManager.OpenScene(original.path);
        }

        [MenuItem("Animal Fall/Hindrances/Apply Curated Level Pools")]
        public static void ApplyCuratedLevelPools()
        {
            LevelData[] levels = AssetDatabase.FindAssets("t:LevelData", new[] { "Assets/Levels" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<LevelData>)
                .Where(level => level != null && level.LevelNumber > 0)
                .OrderBy(level => level.LevelNumber)
                .ToArray();

            int earlyNormalIndex = 0;
            int nextCurrentType = 1;
            int nextNewType = 21;
            var assignments = new List<LevelAssignment>(levels.Length);
            var assigned = new HashSet<HindranceType>();

            foreach (LevelData level in levels)
            {
                HindranceConfig[] configs;
                if (level.IsMegaLevel && !level.AllowNormalHindrancesInMegaLevel)
                {
                    configs = Array.Empty<HindranceConfig>();
                }
                else if (level.LevelNumber < 3)
                {
                    configs = Array.Empty<HindranceConfig>();
                }
                else if (level.LevelNumber < 13)
                {
                    int count = earlyNormalIndex == 0 ? 2 : 3;
                    configs = new HindranceConfig[count];
                    for (int i = 0; i < count; i++)
                    {
                        HindranceType type = (HindranceType)nextCurrentType;
                        nextCurrentType = nextCurrentType == 20 ? 1 : nextCurrentType + 1;
                        configs[i] = Config(type, i == 0 ? 1.2f : 0.8f);
                        assigned.Add(type);
                    }
                    earlyNormalIndex++;
                }
                else if (nextNewType <= 50)
                {
                    HindranceType type = (HindranceType)nextNewType++;
                    configs = new[] { Config(type, 1f) };
                    assigned.Add(type);
                }
                else
                {
                    HindranceType first = (HindranceType)(1 + (level.LevelNumber * 7) % 50);
                    HindranceCompatibilityTag firstTags = Tags(first);
                    HindranceType second = Enumerable.Range(1, 50).Select(id => (HindranceType)id)
                        .First(type => type != first && (Tags(type) & firstTags) == 0);
                    configs = new[] { Config(first, 1.1f), Config(second, 0.9f) };
                    assigned.Add(first); assigned.Add(second);
                }

                level.SetHindrancesArray(configs);
                EditorUtility.SetDirty(level);
                assignments.Add(new LevelAssignment
                {
                    level = level.LevelNumber,
                    mega = level.IsMegaLevel,
                    hindrances = configs.Select(config => config.type.ToString()).ToArray()
                });
            }

            string[] showcaseOnly = Enumerable.Range(1, 50).Select(id => (HindranceType)id)
                .Where(type => !assigned.Contains(type)).Select(type => type.ToString()).ToArray();
            File.WriteAllText(AssignmentPath, JsonUtility.ToJson(new AssignmentManifest
            {
                levels = assignments.ToArray(),
                showcaseOnly = showcaseOnly
            }, true));
            AssetDatabase.ImportAsset(AssignmentPath);
        }

        private static HindranceConfig Config(HindranceType type, float weight) => new HindranceConfig
        {
            type = type,
            weight = weight,
            initialDelay = 0f
        };

        private static void WriteManifest()
        {
            var manifest = new Manifest { sheets = Sheets.Select(sheet => new SheetManifest
            {
                path = sheet.Path, width = 2048, height = 2048, cellSize = Cell,
                sprites = sheet.Names.Select((name, i) => new SpriteManifest
                {
                    logicalName = name, row = i / 4, column = i % 4, frame = name,
                    pivotX = 0.5f, pivotY = 0.5f, pixelsPerUnit = 256f,
                    intendedPrefab = name.StartsWith("icon_") ? "Assets/Prefabs/Hindrances" : "ReusableStateOrVFX"
                }).ToArray()
            }).ToArray() };
            File.WriteAllText(ManifestPath, JsonUtility.ToJson(manifest, true)); AssetDatabase.ImportAsset(ManifestPath);
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/'); string current = parts[0];
            for (int i = 1; i < parts.Length; i++) { string next = current + "/" + parts[i]; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]); current = next; }
        }
    }

    public sealed class HindranceShowcaseWindow : EditorWindow
    {
        private HindranceType _type = HindranceType.Bomb;
        private int _tier;
        private int _seed = 12345;
        private GameObject _spawned;

        [MenuItem("Animal Fall/Hindrances/Showcase")]
        private static void Open() => GetWindow<HindranceShowcaseWindow>("Hindrance Showcase");

        private void OnGUI()
        {
            EditorGUILayout.LabelField("50 Hindrance Showcase", EditorStyles.boldLabel);
            _type = (HindranceType)EditorGUILayout.EnumPopup("Hindrance", _type);
            _tier = EditorGUILayout.IntSlider("Difficulty tier", _tier, 0, 3);
            _seed = EditorGUILayout.IntField("Deterministic seed", _seed);
            EditorGUILayout.LabelField("Active", _spawned != null ? _spawned.name : "None");
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Spawn")) Spawn();
                if (GUILayout.Button("Deactivate / Reset")) ResetSpawn();
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Resolution presets");
            using (new EditorGUILayout.HorizontalScope())
            { if (GUILayout.Button("Narrow phone")) SetGameView(720, 1280); if (GUILayout.Button("Tall phone")) SetGameView(1080, 2400); if (GUILayout.Button("Tablet")) SetGameView(1536, 2048); }
        }

        private void Spawn()
        {
            ResetSpawn(); UnityEngine.Random.InitState(_seed);
            HindranceRegistry registry = AssetDatabase.LoadAssetAtPath<HindranceRegistry>("Assets/Resources/Hindrances/HindranceRegistry.asset");
            HindranceData data = registry != null ? registry.GetData(_type) : null;
            if (data?.prefab == null) { Debug.LogError($"No prefab for {_type}"); return; }
            _spawned = PrefabUtility.InstantiatePrefab(data.prefab) as GameObject;
            if (_spawned != null) { Undo.RegisterCreatedObjectUndo(_spawned, "Spawn hindrance showcase"); _spawned.transform.position = Vector3.zero; Selection.activeObject = _spawned; }
        }

        private void ResetSpawn() { if (_spawned != null) Undo.DestroyObjectImmediate(_spawned); _spawned = null; }
        private static void SetGameView(int width, int height) => Debug.Log($"[Hindrance Showcase] Verify at {width}x{height} portrait using Game View preset.");
    }
}
#endif
