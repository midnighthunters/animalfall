#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using AnimalFall.Data;
using AnimalFall.Managers;

namespace AnimalFall.MegaShooter.Editor
{
    public static class MegaShooterGenerator
    {
        public const string Root = "Assets/MegaShooter";
        public const string ArtRoot = Root + "/Art/Generated";
        public const string DataRoot = Root + "/Data";
        public const string PrefabRoot = Root + "/Prefabs";
        public const string ScenePath = "Assets/Scenes/MegaShooterScene.unity";
        private const string MegaResourceRoot = "Assets/Resources/megalevel";
        private const string LevelDatabasePath = "Assets/Levels/LevelDatabase.asset";
        private const int MegaCount = 20;

        private static readonly string[] AnimalIds =
        {
            "eagle_striker", "fox_comet", "turtle_guardian", "tiger_nova", "gorilla_bomber",
            "owl_volt", "chameleon_prism", "rhino_vanguard", "peacock_aurora", "lion_solaris"
        };

        private static readonly string[] AnimalNames =
        {
            "Eagle Striker", "Fox Comet", "Turtle Guardian", "Tiger Nova", "Gorilla Bomber",
            "Owl Volt", "Chameleon Prism", "Rhino Vanguard", "Peacock Aurora", "Lion Solaris"
        };

        private static readonly int[] AnimalUnlockLevels = { 5, 15, 25, 35, 45, 55, 65, 75, 85, 95 };

        private static readonly string[] EnemyIds =
        {
            "scout_drone", "v_fighter", "zigzag_interceptor", "dive_bomber", "shield_drone",
            "heavy_gunship", "splitter_craft", "mine_layer", "laser_frigate", "sniper_ship",
            "carrier", "cloaked_stalker", "repair_drone", "armored_rammer", "reflector_ace"
        };

        private static readonly string[] EnemyNames =
        {
            "Scout Drone", "V-Formation Fighter", "Zigzag Interceptor", "Dive Bomber", "Shield Drone",
            "Heavy Gunship", "Splitter Craft", "Mine Layer", "Laser Frigate", "Sniper Ship",
            "Carrier", "Cloaked Stalker", "Repair Drone", "Armored Rammer", "Reflector Ace"
        };

        private static readonly int[] EnemyFirstMega = { 1, 1, 2, 3, 4, 5, 7, 9, 6, 11, 6, 13, 14, 8, 16 };

        private static readonly string[] LevelTitles =
        {
            "First Flight", "Crossfire Patrol", "Mirage Chase", "Shielded Ambush", "The Iron Wall",
            "Carrier Siege", "Piercing Point", "Fang Rush", "Swarm Reactor", "Foundry Breaker",
            "Time Distortion", "Frozen Second", "Invisible Armada", "Spectrum Trap", "Break the Bulwark",
            "Siege Line", "Aurora Storm", "Celestial Pattern", "Eclipse Assault", "Galactic Last Stand"
        };

        private static readonly string[] BossNames =
        {
            "Scrap Wasp", "Twin-Core Raider", "Mirage Frigate", "Nebula Trickster", "Iron Bastion",
            "Fortress Ray", "Crimson Saber", "Solar Fang", "Hive Carrier", "Titan Foundry",
            "Chrono Sentinel", "Clockwork Colossus", "Phantom Prism", "Spectrum Warden", "Bulwark Destroyer",
            "Siege Leviathan", "Aurora Wing", "Celestial Fan", "Eclipse Emperor", "Galactic Overlord"
        };

        private static readonly int[] WaveCounts = { 3, 4, 4, 5, 5, 5, 6, 6, 6, 7, 7, 7, 8, 8, 8, 9, 9, 9, 10, 10 };
        private static readonly int[] EnemyCounts = { 18, 22, 26, 30, 34, 38, 42, 46, 50, 54, 58, 62, 66, 70, 74, 78, 82, 86, 92, 100 };
        private static readonly float[] EnemyHp = { .75f, .9f, 1f, 1.1f, 1.2f, 1.32f, 1.44f, 1.56f, 1.68f, 1.8f, 1.92f, 2.04f, 2.17f, 2.3f, 2.44f, 2.58f, 2.72f, 2.87f, 3.05f, 3.25f };
        private static readonly float[] BulletSpeed = { .65f, .72f, .78f, .84f, .9f, .96f, 1.02f, 1.08f, 1.14f, 1.2f, 1.26f, 1.32f, 1.38f, 1.43f, 1.48f, 1.52f, 1.56f, 1.59f, 1.62f, 1.65f };
        private static readonly float[] FireIntervals = { 2.4f, 2.25f, 2.1f, 1.98f, 1.86f, 1.75f, 1.65f, 1.56f, 1.48f, 1.4f, 1.33f, 1.26f, 1.2f, 1.14f, 1.08f, 1.03f, .98f, .93f, .89f, .85f };
        private static readonly float[] BossHp = { 1200, 1450, 1750, 2050, 2400, 2800, 3200, 3650, 4100, 4600, 5100, 5650, 6200, 6800, 7450, 8150, 8900, 9700, 10600, 11800 };
        private static readonly int[] BossPhases = { 2, 2, 2, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5 };
        private static readonly int[] ProjectileCaps = { 24, 28, 32, 36, 40, 44, 48, 52, 58, 64, 70, 76, 82, 88, 94, 100, 106, 112, 118, 120 };
        private static readonly float[] ParTimes = { 85, 92, 98, 105, 112, 119, 126, 133, 140, 147, 154, 161, 168, 175, 182, 189, 196, 203, 210, 220 };

        [MenuItem("Tools/Animal Fall/Mega Shooter/Generate Complete Feature")]
        public static void GenerateCompleteFeature()
        {
            EnsureFolders();
            GenerateMissingPlaceholderArt();
            GenerateMissingPrefabs();
            GenerateOrUpdateMegaLevelsOnly();
            GenerateMegaShooterScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            MegaShooterValidator.ValidateAll(true);
            Debug.Log("[MegaShooterGenerator] Complete feature generation finished.");
        }

        [MenuItem("Tools/Animal Fall/Mega Shooter/Generate Missing Placeholder Art")]
        public static void GenerateMissingPlaceholderArt()
        {
            EnsureFolders();
            Color[] colors = AnimalColors();
            for (int i = 0; i < AnimalIds.Length; i++)
            {
                GenerateSprite($"{ArtRoot}/Players/{AnimalIds[i]}_ship.png", 512, 512, colors[i], i, false, true);
                GenerateSprite($"{ArtRoot}/Portraits/{AnimalIds[i]}_portrait.png", 256, 256, colors[i], i + 2, false, false);
            }
            for (int i = 0; i < EnemyIds.Length; i++)
                GenerateSprite($"{ArtRoot}/Enemies/{EnemyIds[i]}.png", 256, 256, EnemyColor(i), i, false, true);
            for (int i = 0; i < 6; i++)
                GenerateSprite($"{ArtRoot}/Bosses/boss_silhouette_{i + 1}.png", 512, 512, EnemyColor(i + 4), i + 7, false, true);

            string[] projectileIds = { "bolt", "orb", "missile", "mine", "laser_warning", "laser_beam", "rail", "feather", "boulder", "solar", "prism" };
            for (int i = 0; i < projectileIds.Length; i++)
                GenerateSprite($"{ArtRoot}/Projectiles/{projectileIds[i]}.png", 128, 128,
                    i < 4 ? new Color(.25f, .95f, 1f) : new Color(1f, .25f + i * .025f, .35f), i + 11, false, false);

            string[] pickups = { "health", "counter", "shield", "weapon_boost" };
            for (int i = 0; i < pickups.Length; i++)
                GenerateSprite($"{ArtRoot}/Pickups/{pickups[i]}.png", 256, 256, colors[(i + 2) % colors.Length], i + 30, false, false);

            string[] backgrounds = { "meadow_orbit", "jungle_nebula", "arctic_expanse", "mystic_void", "storm_galaxy" };
            Color[] backgroundColors =
            {
                new Color(.04f,.16f,.28f), new Color(.08f,.20f,.18f), new Color(.04f,.12f,.26f),
                new Color(.12f,.05f,.25f), new Color(.16f,.04f,.12f)
            };
            for (int i = 0; i < backgrounds.Length; i++)
                GenerateSprite($"{ArtRoot}/Backgrounds/{backgrounds[i]}.png", 512, 1024, backgroundColors[i], i + 41, true, false);

            string[] icons = { "health", "counter", "wave", "boss_warning", "pause", "animal_power" };
            for (int i = 0; i < icons.Length; i++)
                GenerateSprite($"{ArtRoot}/UI/{icons[i]}.png", 256, 256, colors[(i + 4) % colors.Length], i + 51, false, false);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MegaShooterGenerator] Missing placeholder art generated without overwriting existing files.");
        }

        [MenuItem("Tools/Animal Fall/Mega Shooter/Generate Missing Prefabs")]
        public static void GenerateMissingPrefabs()
        {
            EnsureFolders();
            Sprite fallback = LoadSprite($"{ArtRoot}/Projectiles/bolt.png");
            Material warningMaterial = GetOrCreateWarningMaterial();
            CreatePlayerPrefab(fallback);
            CreateEnemyPrefab(fallback, warningMaterial);
            CreateBossPrefab(fallback);
            CreateProjectilePrefab(fallback);
            CreatePickupPrefab(LoadSprite($"{ArtRoot}/Pickups/health.png"));
            CreateEffectPrefab(LoadSprite($"{ArtRoot}/Projectiles/orb.png"));
            AssetDatabase.SaveAssets();
            Debug.Log("[MegaShooterGenerator] Missing pooled prefabs generated.");
        }

        [MenuItem("Tools/Animal Fall/Mega Shooter/Generate or Update Mega Levels Only")]
        public static void GenerateOrUpdateMegaLevelsOnly()
        {
            EnsureFolders();
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/MegaPlayer.prefab");
            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/MegaEnemy.prefab");
            GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/MegaBoss.prefab");
            GameObject projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/MegaProjectile.prefab");
            GameObject effectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/MegaEffect.prefab");

            ProjectileData[] projectiles = GenerateProjectileData(projectilePrefab);
            WeaponData[] weapons = GenerateWeaponData(projectiles);
            SuperAnimalData[] animals = GenerateAnimalData(playerPrefab, weapons);
            EnemyShipData[] enemies = GenerateEnemyData(enemyPrefab, projectiles);
            MegaVFXProfile vfx = GenerateVfxProfile(effectPrefab);
            BossShipData[] bosses = GenerateBossData(bossPrefab, effectPrefab);
            Sprite[] backgrounds = LoadBackgrounds();
            ApplyMegaLevelArtwork(animals, weapons, enemies, bosses, projectiles);

            LevelDatabase database = AssetDatabase.LoadAssetAtPath<LevelDatabase>(LevelDatabasePath);
            if (database == null)
            {
                Debug.LogError($"[MegaShooterGenerator] Missing {LevelDatabasePath}.");
                return;
            }

            LevelData[] expanded = new LevelData[Mathf.Max(100, database.TotalLevels)];
            if (database.Levels != null) Array.Copy(database.Levels, expanded, Mathf.Min(database.Levels.Length, expanded.Length));

            for (int i = 0; i < MegaCount; i++)
            {
                int gameLevel = (i + 1) * 5;
                MegaLevelData mega = GenerateMegaLevel(i, animals, enemies, bosses[i], backgrounds, vfx);
                int slot = gameLevel - 1;
                LevelData levelAsset = expanded[slot];
                if (levelAsset == null)
                {
                    string path = $"Assets/Levels/LevelData/Level_{gameLevel:D2}.asset";
                    levelAsset = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                    if (levelAsset == null)
                    {
                        levelAsset = ScriptableObject.CreateInstance<LevelData>();
                        AssetDatabase.CreateAsset(levelAsset, path);
                        levelAsset.SetLevelNumber(gameLevel);
                        levelAsset.SetChapterTheme("Mega Shooter");
                        levelAsset.SetIsMegaLevel(true);
                    }
                    expanded[slot] = levelAsset;
                }

                MegaLevelData assigned = levelAsset.MegaShooterData != null ? levelAsset.MegaShooterData : mega;
                levelAsset.SetMegaShooter(LevelMode.MegaShooter, assigned);
                EditorUtility.SetDirty(levelAsset);
            }

            database.SetLevelsPreservingExisting(expanded);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log("[MegaShooterGenerator] Configured exactly 20 mega slots; normal level assets were not regenerated or edited.");
        }

        private static ProjectileData[] GenerateProjectileData(GameObject prefab)
        {
            string[] ids = { "feather", "missile", "shell_plasma", "claw_laser", "plasma_boulder", "volt_orb", "prism", "rail", "aurora_feather", "solar", "enemy_bolt" };
            string[] sprites = { "feather", "missile", "orb", "laser_beam", "boulder", "orb", "prism", "rail", "feather", "solar", "bolt" };
            MegaProjectileMotion[] motions =
            {
                MegaProjectileMotion.Straight, MegaProjectileMotion.Homing, MegaProjectileMotion.Straight,
                MegaProjectileMotion.Straight, MegaProjectileMotion.Straight, MegaProjectileMotion.Homing,
                MegaProjectileMotion.Straight, MegaProjectileMotion.Straight, MegaProjectileMotion.Returning,
                MegaProjectileMotion.Straight, MegaProjectileMotion.Straight
            };
            var result = new ProjectileData[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                ProjectileData data = GetOrCreate<ProjectileData>($"{DataRoot}/Projectiles/{ids[i]}.asset", out bool created);
                if (created) { data.stableId = ids[i]; data.motion = motions[i]; data.speed = i == 10 ? 4.2f : 10f; data.damage = i == 10 ? 1f : 5f; data.lifetime = 7f; data.colliderRadius = i == 4 ? .2f : .11f; data.homingStrength = motions[i] == MegaProjectileMotion.Homing ? 2.2f : 0f; data.reflectable = true; data.poolingKey = ids[i]; }
                if (data.prefab == null) data.prefab = prefab;
                if (data.sprite == null) data.sprite = LoadSprite($"{ArtRoot}/Projectiles/{sprites[i]}.png");
                EditorUtility.SetDirty(data);
                result[i] = data;
            }
            return result;
        }

        private static WeaponData[] GenerateWeaponData(ProjectileData[] projectiles)
        {
            float[] damage = { 5, 6, 13, 9, 17, 8, 10, 25, 4, 9 };
            float[] rate = { 5, 4, 4, 3, 3, 3, 5, 2, 3, 3 };
            int[] shots = { 2, 2, 1, 2, 1, 2, 1, 1, 4, 2 };
            var result = new WeaponData[AnimalIds.Length];
            for (int i = 0; i < result.Length; i++)
            {
                WeaponData data = GetOrCreate<WeaponData>($"{DataRoot}/Weapons/{AnimalIds[i]}_weapon.asset", out bool created);
                if (created)
                {
                    data.stableId = AnimalIds[i] + "_weapon";
                    data.displayName = AnimalNames[i] + " Primary";
                    data.damage = damage[i]; data.shotsPerSecond = rate[i]; data.projectileCount = shots[i];
                    data.spreadDegrees = i == 8 ? 70f : shots[i] > 1 ? 10f : 0f;
                    data.pierce = i == 3 ? 4 : i == 7 ? 2 : 0;
                    data.splashRadius = i == 4 ? 1.1f : 0f;
                    data.homingStrength = i == 1 || i == 5 ? 1.8f : 0f;
                    data.chainCount = i == 5 ? 3 : 0;
                    data.stronglyHomingEveryNthVolley = i == 1 ? 5 : 0;
                }
                if (data.projectile == null) data.projectile = projectiles[i];
                EditorUtility.SetDirty(data);
                result[i] = data;
            }
            return result;
        }

        private static SuperAnimalData[] GenerateAnimalData(GameObject prefab, WeaponData[] weapons)
        {
            MegaCounterType[] counters = (MegaCounterType[])Enum.GetValues(typeof(MegaCounterType));
            string[] descriptions =
            {
                "Fast twin feather bolts and agile movement. Sky Barrage clears small bullets in a focused storm.",
                "Homing comet missiles and evasive decoys. Lower raw boss damage, excellent target acquisition.",
                "Heavy shell-plasma fire, two bonus hull points, and a reflector shield. Slow but forgiving.",
                "Piercing claw lasers reward alignment. Claw Dash grants a precise invulnerable strike.",
                "Explosive plasma boulders punish clusters. Gravity Smash compresses and detonates the field.",
                "Chain-lightning orbs thrive in crowds. Time Eye slows hostiles without slowing your fire.",
                "A sustained prism weapon with evasive cloaking. Spectrum Shift converts pressure into opportunity.",
                "A high-impact rail cannon and frontal armor. Meteor Ram breaks priority defenses.",
                "A wide feather fan with strong center damage. Royal Fan clears and returns across the arena.",
                "Balanced solar bolts and low-health resolve. Solar Roar clears space before a concentrated blast."
            };
            var result = new SuperAnimalData[AnimalIds.Length];
            for (int i = 0; i < result.Length; i++)
            {
                SuperAnimalData data = GetOrCreate<SuperAnimalData>($"{DataRoot}/Animals/{AnimalIds[i]}.asset", out bool created);
                if (created)
                {
                    data.stableId = AnimalIds[i]; data.displayName = AnimalNames[i];
                    data.unlockMegaIndex = (AnimalUnlockLevels[i] / 5); data.unlockGameLevel = AnimalUnlockLevels[i];
                    data.baseHealth = i == 2 ? 7 : 5;
                    data.movementSpeed = i == 2 ? 7.5f : i == 0 ? 11.2f : 9.5f;
                    data.hitboxRadius = .22f;
                    data.selectionDescription = descriptions[i];
                    data.counter.type = counters[i]; data.counter.damage = 80f + i * 8f; data.counter.duration = 1.25f + i * .05f; data.counter.radius = 4.5f;
                    data.counter.reflectsProjectiles = i == 2; data.counter.clearsProjectiles = true; data.counter.meterRequirement = i == 9 ? 125f : 100f;
                    data.passive.movementMultiplier = i == 0 ? 1.12f : i == 2 ? .85f : 1f;
                    data.passive.bonusHealth = i == 2 ? 2 : 0;
                    data.passive.frontalDamageReduction = i == 7 ? .25f : 0f;
                    data.passive.nearMissChargeMultiplier = i == 5 ? 1.35f : 1f;
                    data.passive.lowHealthDamageBonus = i == 9 ? .12f : 0f;
                    data.stats.power = .55f + (i % 3) * .08f; data.stats.speed = i == 2 ? .3f : i == 0 ? .9f : .62f; data.stats.defense = i == 2 ? .95f : .5f; data.stats.coverage = i == 8 ? .95f : .55f;
                }
                if (data.playerPrefab == null) data.playerPrefab = prefab;
                if (data.primaryWeapon == null) data.primaryWeapon = weapons[i];
                if (data.shipSprite == null) data.shipSprite = LoadSprite($"{ArtRoot}/Players/{AnimalIds[i]}_ship.png");
                if (data.portrait == null) data.portrait = LoadSprite($"{ArtRoot}/Portraits/{AnimalIds[i]}_portrait.png");
                EditorUtility.SetDirty(data);
                result[i] = data;
            }
            return result;
        }

        private static EnemyShipData[] GenerateEnemyData(GameObject prefab, ProjectileData[] projectiles)
        {
            MegaMovementPattern[] movement =
            {
                MegaMovementPattern.Straight, MegaMovementPattern.Straight, MegaMovementPattern.ZigZag,
                MegaMovementPattern.Dive, MegaMovementPattern.Hover, MegaMovementPattern.Hover,
                MegaMovementPattern.Sine, MegaMovementPattern.Stationary, MegaMovementPattern.Hover,
                MegaMovementPattern.Hover, MegaMovementPattern.Hover, MegaMovementPattern.Cloak,
                MegaMovementPattern.Hover, MegaMovementPattern.Rammer, MegaMovementPattern.ZigZag
            };
            MegaWeaponPattern[] weapon =
            {
                MegaWeaponPattern.AimedSingle, MegaWeaponPattern.FixedSpread, MegaWeaponPattern.Burst,
                MegaWeaponPattern.AimedSingle, MegaWeaponPattern.AimedSingle, MegaWeaponPattern.Radial,
                MegaWeaponPattern.FixedSpread, MegaWeaponPattern.Mine, MegaWeaponPattern.Laser,
                MegaWeaponPattern.Sniper, MegaWeaponPattern.Carrier, MegaWeaponPattern.AimedSingle,
                MegaWeaponPattern.None, MegaWeaponPattern.None, MegaWeaponPattern.Burst
            };
            var result = new EnemyShipData[EnemyIds.Length];
            for (int i = 0; i < result.Length; i++)
            {
                EnemyShipData data = GetOrCreate<EnemyShipData>($"{DataRoot}/Enemies/{EnemyIds[i]}.asset", out bool created);
                if (created)
                {
                    data.stableId = EnemyIds[i]; data.displayName = EnemyNames[i];
                    data.hitPoints = 28f + i * 6f; data.contactDamage = 1f; data.speed = Mathf.Max(1.2f, 3.1f - i * .08f); data.score = 100 + i * 20;
                    data.movementPattern = movement[i]; data.weaponPattern = weapon[i]; data.fireInterval = Mathf.Max(.9f, 2.5f - i * .06f);
                    data.initialFireDelay = 1.2f; data.telegraphTime = (weapon[i] == MegaWeaponPattern.Laser || weapon[i] == MegaWeaponPattern.Sniper) ? .9f : .85f;
                    data.poolingKey = EnemyIds[i]; data.priorityTarget = i == 4 || i == 8 || i == 9 || i == 10 || i == 12;
                    data.splitsOnDeath = i == 6; data.shieldsNearby = i == 4; data.repairsNearby = i == 12; data.pickupChance = .04f;
                }
                if (data.prefab == null) data.prefab = prefab;
                if (data.projectile == null) data.projectile = projectiles[10];
                if (data.sprite == null) data.sprite = LoadSprite($"{ArtRoot}/Enemies/{EnemyIds[i]}.png");
                // Telegraph readability is a feature-wide safety invariant, so migrate stale generated values.
                data.telegraphTime = Mathf.Max(.85f, data.telegraphTime);
                EditorUtility.SetDirty(data);
                result[i] = data;
            }
            return result;
        }

        private static MegaVFXProfile GenerateVfxProfile(GameObject effectPrefab)
        {
            MegaVFXProfile data = GetOrCreate<MegaVFXProfile>($"{DataRoot}/VFX/DefaultMegaVFX.asset", out bool created);
            if (data.hitSparkPrefab == null) data.hitSparkPrefab = effectPrefab;
            if (data.explosionPrefab == null) data.explosionPrefab = effectPrefab;
            if (data.eliteExplosionPrefab == null) data.eliteExplosionPrefab = effectPrefab;
            if (data.warningPrefab == null) data.warningPrefab = effectPrefab;
            if (data.nearMissPrefab == null) data.nearMissPrefab = effectPrefab;
            if (data.counterReadyPrefab == null) data.counterReadyPrefab = effectPrefab;
            if (data.bossDeathPrefab == null) data.bossDeathPrefab = effectPrefab;
            if (created) { data.masterShakeScale = 1f; data.masterFlashScale = 1f; }
            EditorUtility.SetDirty(data);
            return data;
        }

        private static BossShipData[] GenerateBossData(GameObject prefab, GameObject effectPrefab)
        {
            var result = new BossShipData[MegaCount];
            for (int i = 0; i < result.Length; i++)
            {
                BossShipData data = GetOrCreate<BossShipData>($"{DataRoot}/Bosses/Boss_{i + 1:D2}_{SafeName(BossNames[i])}.asset", out bool created);
                if (created)
                {
                    data.stableId = "boss_" + (i + 1).ToString("D2");
                    data.displayName = BossNames[i];
                    data.baseHitPoints = BossHp[i];
                    data.colliderSize = new Vector2(2.4f + i * .025f, 1.45f + i * .02f);
                    data.movementBounds = new Rect(-3.5f, 4.2f, 7f, 2.2f);
                    data.score = 5000 + i * 750;
                    data.entranceDuration = 2f;
                    data.phases = CreateBossPhases(i, BossPhases[i]);
                }
                if (data.prefab == null) data.prefab = prefab;
                if (data.sprite == null) data.sprite = LoadSprite($"{ArtRoot}/Bosses/boss_silhouette_{i % 6 + 1}.png");
                if (data.entranceVFX == null) data.entranceVFX = effectPrefab;
                if (data.deathVFX == null) data.deathVFX = effectPrefab;
                EditorUtility.SetDirty(data);
                result[i] = data;
            }
            return result;
        }

        private static BossPhaseData[] CreateBossPhases(int megaIndex, int count)
        {
            var phases = new BossPhaseData[count];
            for (int p = 0; p < count; p++)
            {
                float hue = Mathf.Repeat((megaIndex * .08f) + p * .11f, 1f);
                phases[p] = new BossPhaseData
                {
                    healthThreshold = 1f - p / (float)count,
                    movementPattern = p % 2 == 0 ? MegaMovementPattern.Hover : MegaMovementPattern.SideSweep,
                    attackInterval = Mathf.Max(.85f, 2.25f - megaIndex * .045f - p * .1f),
                    projectileSpeedMultiplier = 1f + p * .06f,
                    transitionDuration = 1f,
                    phaseTint = Color.HSVToRGB(hue, .38f, 1f),
                    cameraShake = .16f + p * .025f,
                    warningText = p == 0 ? "CORE ONLINE" : $"PHASE {p + 1}",
                    attacks = CreateBossAttacks(megaIndex, p)
                };
            }
            return phases;
        }

        private static BossAttackPattern[] CreateBossAttacks(int megaIndex, int phase)
        {
            MegaWeaponPattern[] mechanisms =
            {
                MegaWeaponPattern.AimedSingle, MegaWeaponPattern.FixedSpread, MegaWeaponPattern.Burst,
                MegaWeaponPattern.Radial, MegaWeaponPattern.Mine, MegaWeaponPattern.Laser,
                MegaWeaponPattern.Sniper, MegaWeaponPattern.Carrier
            };
            MegaWeaponPattern firstMechanism = mechanisms[(megaIndex + phase) % mechanisms.Length];
            MegaWeaponPattern secondMechanism = mechanisms[(megaIndex + phase + 3) % mechanisms.Length];
            var first = new BossAttackPattern
            {
                attackName = AttackName(firstMechanism),
                pattern = firstMechanism,
                weight = 1f,
                telegraphTime = .9f,
                projectileCount = Mathf.Clamp(1 + phase * 2, 1, 9),
                spreadDegrees = 60f + phase * 8f,
                aimed = firstMechanism == MegaWeaponPattern.AimedSingle || firstMechanism == MegaWeaponPattern.Sniper || firstMechanism == MegaWeaponPattern.Laser,
                reflectable = firstMechanism != MegaWeaponPattern.Laser,
                clearsBulletsBeforeAttack = firstMechanism == MegaWeaponPattern.Laser
            };
            var second = new BossAttackPattern
            {
                attackName = AttackName(secondMechanism),
                pattern = secondMechanism,
                weight = .75f,
                telegraphTime = Mathf.Max(.85f, .95f - megaIndex * .002f),
                projectileCount = Mathf.Clamp(3 + megaIndex / 2 + phase, 3, 18),
                spreadDegrees = 105f,
                aimed = secondMechanism == MegaWeaponPattern.AimedSingle || secondMechanism == MegaWeaponPattern.Sniper || secondMechanism == MegaWeaponPattern.Laser,
                reflectable = megaIndex < 15 && secondMechanism != MegaWeaponPattern.Laser,
                clearsBulletsBeforeAttack = secondMechanism == MegaWeaponPattern.Laser
            };
            return new[] { first, second };
        }

        private static string AttackName(MegaWeaponPattern pattern)
        {
            switch (pattern)
            {
                case MegaWeaponPattern.AimedSingle: return "PREDATOR LOCK";
                case MegaWeaponPattern.FixedSpread: return "ROYAL FAN";
                case MegaWeaponPattern.Burst: return "TRIPLE REND";
                case MegaWeaponPattern.Radial: return "DARK HALO";
                case MegaWeaponPattern.Mine: return "VENOM FIELD";
                case MegaWeaponPattern.Laser: return "CORE LANCE";
                case MegaWeaponPattern.Sniper: return "DEATH MARK";
                case MegaWeaponPattern.Carrier: return "SWARM GATE";
                default: return "MEGA VOLLEY";
            }
        }

        private static void ApplyMegaLevelArtwork(SuperAnimalData[] animals, WeaponData[] weapons,
            EnemyShipData[] enemies, BossShipData[] bosses, ProjectileData[] projectiles)
        {
            Sprite[] heroSprites = LoadAllSprites($"{MegaResourceRoot}/heroes.png");
            Sprite[] villainSprites = LoadAllSprites($"{MegaResourceRoot}/villains.png");
            Sprite[] weaponSprites = LoadAllSprites($"{MegaResourceRoot}/weapons.png");
            if (heroSprites.Length < 10 || villainSprites.Length < 10 || weaponSprites.Length < 20)
            {
                Debug.LogError("[MegaShooterGenerator] Megalevel sprite sheets are not sliced as expected (10 heroes, 10 villains, 20 weapons).");
                return;
            }

            for (int i = 0; i < animals.Length && i < 10; i++)
            {
                animals[i].shipSprite = heroSprites[i];
                animals[i].portrait = heroSprites[i];
                weapons[i].icon = weaponSprites[i];
                if (weapons[i].projectile != null) weapons[i].projectile.sprite = weaponSprites[i];
                EditorUtility.SetDirty(animals[i]);
                EditorUtility.SetDirty(weapons[i]);
                if (weapons[i].projectile != null) EditorUtility.SetDirty(weapons[i].projectile);
            }

            MegaWeaponPattern[] villainPatterns =
            {
                MegaWeaponPattern.FixedSpread, MegaWeaponPattern.AimedSingle, MegaWeaponPattern.Burst,
                MegaWeaponPattern.Radial, MegaWeaponPattern.Mine, MegaWeaponPattern.Sniper,
                MegaWeaponPattern.FixedSpread, MegaWeaponPattern.Burst, MegaWeaponPattern.Laser,
                MegaWeaponPattern.AimedSingle
            };
            for (int i = 0; i < enemies.Length; i++)
            {
                int artIndex = i % 10;
                enemies[i].sprite = villainSprites[artIndex];
                enemies[i].weaponIcon = weaponSprites[10 + artIndex];
                enemies[i].weaponPattern = villainPatterns[artIndex];
                enemies[i].projectile = projectiles[10];
                enemies[i].speed = Mathf.Min(enemies[i].speed, 2.65f);
                enemies[i].fireInterval = Mathf.Max(enemies[i].fireInterval, 2f);
                EditorUtility.SetDirty(enemies[i]);
            }

            string[] bossPaths = Directory.GetFiles($"{MegaResourceRoot}/boss", "*.png");
            Array.Sort(bossPaths, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < bosses.Length && bossPaths.Length > 0; i++)
            {
                int artIndex = i % Mathf.Min(8, bossPaths.Length);
                bosses[i].sprite = LoadSprite(bossPaths[artIndex].Replace('\\', '/'));
                bosses[i].weaponIcon = weaponSprites[10 + (i % 10)];
                EditorUtility.SetDirty(bosses[i]);
            }
        }

        private static MegaLevelData GenerateMegaLevel(int i, SuperAnimalData[] animals, EnemyShipData[] enemies,
            BossShipData boss, Sprite[] backgrounds, MegaVFXProfile vfx)
        {
            int gameLevel = (i + 1) * 5;
            MegaLevelData data = GetOrCreate<MegaLevelData>($"{DataRoot}/Levels/Mega_{i + 1:D2}_Level_{gameLevel:D3}.asset", out bool created);
            if (created)
            {
                data.gameLevelNumber = gameLevel;
                data.displayTitle = LevelTitles[i];
                data.megaSequenceIndex = i + 1;
                data.description = BuildLevelDescription(i);
                data.backgroundSpeed = .45f + i * .02f;
                data.backgroundColor = BackgroundColor(i);
                data.accentColor = AnimalColors()[Mathf.Min(9, i / 2)];
                data.startingHealth = 5;
                data.movementSpeedMultiplier = 1f;
                data.playerPowerMultiplier = 1f + .062f * i;
                data.invulnerabilityDuration = 1f;
                data.counterChargeMultiplier = 1f;
                data.cameraBounds = new Rect(-5f, -8.9f, 10f, 17.8f);
                data.playerMovementBounds = new Rect(-4.25f, -7.7f, 8.5f, 6.3f);
                data.safeUiMargins = new Vector4(.2f, 1.2f, .2f, 1.15f);
                data.scrollSpeed = .7f + i * .015f;
                data.bottomProjectileExclusion = 1.4f;
                data.enemyHealthMultiplier = EnemyHp[i];
                data.enemyDamageMultiplier = i >= 8 ? 1.15f : 1f;
                data.enemyProjectileSpeedMultiplier = BulletSpeed[i];
                data.ordinaryEnemyFireInterval = FireIntervals[i];
                data.enemyFireIntervalMultiplier = 1f;
                data.spawnCadenceMultiplier = Mathf.Max(.58f, 1f - i * .018f);
                data.maximumActiveEnemies = Mathf.Clamp(6 + i / 2, 6, 16);
                data.maximumHostileProjectiles = ProjectileCaps[i];
                data.targetEnemyCount = EnemyCounts[i];
                data.scoreMultiplier = 1f + i * .05f;
                data.comboTimeout = 2f;
                data.nearMissScore = 25 + i * 2;
                data.counterScore = 250 + i * 15;
                data.parTime = ParTimes[i];
                data.coinReward = 100 + i * 25;
                data.arcadeTokenReward = 3 + i / 5;
                data.unlockReward = i % 2 == 0 ? AnimalNames[Mathf.Min(9, i / 2)] : string.Empty;
                data.bossWarningText = $"WARNING — {BossNames[i].ToUpperInvariant()} APPROACHING";
                data.bossOverrides.dropCriticalHealthPickup = i == 9 || i == 19;
                data.bossOverrides.healthPickupBossThreshold = .5f;
                data.bossOverrides.playerCriticalHealthThreshold = .35f;
                data.deterministicSeed = 5001 + i * 97;
                data.randomizeSeed = false;
            }

            if (data.featuredAnimal == null) data.featuredAnimal = animals[Mathf.Min(9, i / 2)];
            if (data.allowedAnimals == null || data.allowedAnimals.Length == 0)
            {
                int allowedCount = Mathf.Min(10, i / 2 + 1);
                data.allowedAnimals = new SuperAnimalData[allowedCount];
                Array.Copy(animals, data.allowedAnimals, allowedCount);
            }
            if (data.backgroundLayers == null || data.backgroundLayers.Length == 0)
                data.backgroundLayers = new[] { backgrounds[BackgroundIndex(i)], backgrounds[BackgroundIndex(i)], backgrounds[BackgroundIndex(i)], backgrounds[BackgroundIndex(i)] };
            if (data.backgroundLayerSpeeds == null || data.backgroundLayerSpeeds.Length == 0)
                data.backgroundLayerSpeeds = new[] { .12f, .24f, .42f, .68f };
            data.waves = CreateWaves(i, enemies);
            if (data.boss == null) data.boss = boss;
            if (data.vfxProfile == null) data.vfxProfile = vfx;
            EditorUtility.SetDirty(data);
            return data;
        }

        private static MegaWaveData[] CreateWaves(int megaIndex, EnemyShipData[] enemies)
        {
            int waveCount = WaveCounts[megaIndex];
            int remaining = EnemyCounts[megaIndex];
            var waves = new MegaWaveData[waveCount];
            var unlocked = new List<EnemyShipData>();
            for (int i = 0; i < enemies.Length; i++) if (EnemyFirstMega[i] <= megaIndex + 1) unlocked.Add(enemies[i]);
            EnemyShipData firstType = unlocked[megaIndex % unlocked.Count];
            EnemyShipData secondType = unlocked[(megaIndex + Mathf.Max(1, unlocked.Count / 2)) % unlocked.Count];
            if (secondType == firstType && unlocked.Count > 1) secondType = unlocked[(megaIndex + 1) % unlocked.Count];

            for (int w = 0; w < waveCount; w++)
            {
                int wavesLeft = waveCount - w;
                int thisWave = Mathf.CeilToInt(remaining / (float)wavesLeft);
                remaining -= thisWave;
                int firstCount = Mathf.CeilToInt(thisWave * .55f);
                int secondCount = thisWave - firstCount;
                EnemyShipData first = (w & 1) == 0 ? firstType : secondType;
                EnemyShipData second = (w & 1) == 0 ? secondType : firstType;
                var groups = secondCount > 0 ? new EnemySpawnGroup[2] : new EnemySpawnGroup[1];
                groups[0] = CreateGroup(first, firstCount, w, megaIndex, false);
                if (secondCount > 0) groups[1] = CreateGroup(second, secondCount, w + 1, megaIndex, w == waveCount - 1);
                waves[w] = new MegaWaveData
                {
                    waveName = w == 0 ? "Teach" : w == waveCount - 1 ? "Test" : "Practice",
                    waveNumber = w + 1,
                    startDelay = w == 0 ? 1f : .6f,
                    completionDelay = .8f,
                    warningBanner = w == 0 ? first.displayName.ToUpperInvariant() : string.Empty,
                    spawnGroups = groups,
                    healthMultiplier = 1f,
                    speedMultiplier = 1f + w * .025f,
                    fireRateMultiplier = 1f,
                    scoreMultiplier = 1f + w * .04f,
                    maximumSimultaneousEnemies = Mathf.Clamp(6 + megaIndex / 2, 6, 16),
                    completionCondition = MegaWaveCompletion.DefeatAll,
                    environmentEvent = megaIndex >= 10 && w == waveCount / 2 ? MegaEnvironmentEvent.SlowField : MegaEnvironmentEvent.None
                };
            }
            return waves;
        }

        private static Sprite[] LoadAllSprites(string path)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            var sprites = new List<Sprite>();
            for (int i = 0; i < assets.Length; i++) if (assets[i] is Sprite sprite) sprites.Add(sprite);
            sprites.Sort((a, b) => SpriteIndex(a.name).CompareTo(SpriteIndex(b.name)));
            return sprites.ToArray();
        }

        private static int SpriteIndex(string spriteName)
        {
            int separator = spriteName.LastIndexOf('_');
            return separator >= 0 && int.TryParse(spriteName.Substring(separator + 1), out int index) ? index : int.MaxValue;
        }

        private static EnemySpawnGroup CreateGroup(EnemyShipData enemy, int count, int wave, int megaIndex, bool priority)
        {
            return new EnemySpawnGroup
            {
                enemy = enemy,
                count = Mathf.Max(1, count),
                formation = (MegaFormationType)((wave + megaIndex) % Enum.GetValues(typeof(MegaFormationType)).Length),
                spawnPath = wave % 4 == 1 ? MegaSpawnPath.Left : wave % 4 == 2 ? MegaSpawnPath.Right : MegaSpawnPath.Top,
                startDelay = 0f,
                cadence = Mathf.Max(.16f, .45f - megaIndex * .009f),
                columns = Mathf.Clamp(count, 2, 6),
                rows = Mathf.Max(1, Mathf.CeilToInt(count / 6f)),
                spacing = .9f,
                normalizedEntry = .5f,
                eliteChance = megaIndex >= 6 ? Mathf.Min(.3f, .04f + megaIndex * .01f) : 0f,
                priorityTarget = priority && enemy.priorityTarget
            };
        }

        private static string BuildLevelDescription(int index)
            => $"Mega {index + 1}: {LevelTitles[index]}. Clear {WaveCounts[index]} authored waves, master readable telegraphs, then defeat {BossNames[index]}.";

        private static int BackgroundIndex(int megaIndex)
        {
            if (megaIndex < 2) return 0;
            if (megaIndex < 4) return 1;
            if (megaIndex < 6) return 2;
            if (megaIndex < 8) return 3;
            if (megaIndex < 10) return 4;
            return (megaIndex - 10) / 2 % 5;
        }

        private static Color BackgroundColor(int megaIndex)
        {
            Color[] colors = { new Color(.015f,.07f,.16f), new Color(.025f,.11f,.1f), new Color(.02f,.07f,.16f), new Color(.08f,.025f,.16f), new Color(.11f,.02f,.07f) };
            return colors[BackgroundIndex(megaIndex)];
        }

        private static void CreatePlayerPrefab(Sprite sprite)
        {
            string path = $"{PrefabRoot}/MegaPlayer.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            GameObject go = CreatePhysicsSprite("MegaPlayer", sprite, true, false);
            go.AddComponent<MegaPoolMember>();
            go.AddComponent<SuperAnimalController>();
            go.AddComponent<AutoWeaponController>();
            go.AddComponent<MegaCounterController>();
            SavePrefab(go, path);
        }

        private static void CreateEnemyPrefab(Sprite sprite, Material warningMaterial)
        {
            string path = $"{PrefabRoot}/MegaEnemy.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            GameObject go = CreatePhysicsSprite("MegaEnemy", sprite, false, true);
            go.AddComponent<MegaPoolMember>();
            go.AddComponent<MegaEnemyController>();
            GameObject warning = new GameObject("TelegraphLine");
            warning.transform.SetParent(go.transform, false);
            LineRenderer line = warning.AddComponent<LineRenderer>();
            line.sharedMaterial = warningMaterial;
            line.startColor = new Color(1f, .15f, .15f, .22f);
            line.endColor = new Color(1f, .45f, .1f, .55f);
            line.startWidth = .06f; line.endWidth = .02f; line.sortingOrder = 7; line.useWorldSpace = true;
            warning.SetActive(false);
            SavePrefab(go, path);
        }

        private static void CreateBossPrefab(Sprite sprite)
        {
            string path = $"{PrefabRoot}/MegaBoss.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            GameObject go = CreatePhysicsSprite("MegaBoss", sprite, false, true);
            go.transform.localScale = Vector3.one * 1.8f;
            go.AddComponent<MegaPoolMember>();
            go.AddComponent<MegaBossController>();
            SavePrefab(go, path);
        }

        private static void CreateProjectilePrefab(Sprite sprite)
        {
            string path = $"{PrefabRoot}/MegaProjectile.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            GameObject go = CreatePhysicsSprite("MegaProjectile", sprite, true, false, true);
            go.transform.localScale = Vector3.one * .42f;
            go.AddComponent<MegaPoolMember>();
            go.AddComponent<MegaProjectile>();
            SavePrefab(go, path);
        }

        private static void CreatePickupPrefab(Sprite sprite)
        {
            string path = $"{PrefabRoot}/MegaPickup.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            GameObject go = CreatePhysicsSprite("MegaPickup", sprite, true, false, true);
            go.transform.localScale = Vector3.one * .55f;
            go.AddComponent<MegaPoolMember>();
            go.AddComponent<MegaPickupController>();
            SavePrefab(go, path);
        }

        private static void CreateEffectPrefab(Sprite sprite)
        {
            string path = $"{PrefabRoot}/MegaEffect.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            GameObject go = new GameObject("MegaEffect");
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite; renderer.sortingOrder = 15; renderer.color = new Color(.3f, .95f, 1f, .8f);
            go.AddComponent<MegaPoolMember>();
            go.AddComponent<MegaTimedPoolEffect>();
            SavePrefab(go, path);
        }

        private static GameObject CreatePhysicsSprite(string name, Sprite sprite, bool circle, bool box, bool triggerOnly = false)
        {
            GameObject go = new GameObject(name);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = name.Contains("Projectile") ? 12 : name.Contains("Player") ? 10 : 8;
            if (circle)
            {
                CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
            }
            if (box)
            {
                BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
            }
            Rigidbody2D body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            return go;
        }

        private static void SavePrefab(GameObject go, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
        }

        private static Material GetOrCreateWarningMaterial()
        {
            string path = Root + "/Materials/WarningLine.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Shader shader = Shader.Find("Sprites/Default");
            material = new Material(shader) { name = "WarningLine" };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void GenerateSprite(string path, int width, int height, Color color, int variant, bool background, bool ship)
        {
            if (File.Exists(Path.GetFullPath(path))) return;
            EnsureAssetFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[width * height];
            Color32 clear = background ? (Color32)color : new Color32(0, 0, 0, 0);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            if (background)
            {
                var random = new System.Random(variant * 173 + 11);
                for (int y = 0; y < height; y++)
                {
                    float t = y / (float)(height - 1);
                    Color row = Color.Lerp(color * .45f, color * 1.25f, t);
                    row.a = 1f;
                    for (int x = 0; x < width; x++) pixels[y * width + x] = row;
                }
                int starCount = width * height / 900;
                for (int s = 0; s < starCount; s++)
                {
                    int x = random.Next(2, width - 2); int y = random.Next(2, height - 2); int r = random.Next(1, 3);
                    Color32 star = s % 7 == 0 ? new Color32(100, 235, 255, 220) : new Color32(255, 255, 255, 180);
                    DrawCircle(pixels, width, height, x, y, r, star);
                }
            }
            else
            {
                Color32 main = color;
                Color32 light = Color.Lerp(color, Color.white, .68f);
                Color32 dark = Color.Lerp(color, Color.black, .48f);
                int cx = width / 2; int cy = height / 2; int radius = Mathf.Min(width, height) / (ship ? 3 : 3);
                DrawCircle(pixels, width, height, cx, cy, radius, dark);
                DrawDiamond(pixels, width, height, cx, cy + (ship ? radius / 3 : 0), radius, main);
                DrawDiamond(pixels, width, height, cx, cy + radius / 5, Mathf.Max(4, radius / 2), light);
                int wing = radius + (variant % 4) * radius / 8;
                DrawTriangle(pixels, width, height, cx - radius / 3, cy, cx - wing, cy - radius / 2, cx - radius / 2, cy + radius / 3, main);
                DrawTriangle(pixels, width, height, cx + radius / 3, cy, cx + wing, cy - radius / 2, cx + radius / 2, cy + radius / 3, main);
                DrawCircle(pixels, width, height, cx, cy + radius / 3, Mathf.Max(3, radius / 8), new Color32(245, 255, 255, 255));
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(Path.GetFullPath(path), texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = background ? 55f : ship ? 256f : 100f;
                importer.alphaIsTransparency = !background;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }
        }

        private static void DrawCircle(Color32[] pixels, int width, int height, int cx, int cy, int radius, Color32 color)
        {
            int r2 = radius * radius;
            for (int y = -radius; y <= radius; y++)
                for (int x = -radius; x <= radius; x++)
                    if (x * x + y * y <= r2) SetPixel(pixels, width, height, cx + x, cy + y, color);
        }

        private static void DrawDiamond(Color32[] pixels, int width, int height, int cx, int cy, int radius, Color32 color)
        {
            for (int y = -radius; y <= radius; y++)
            {
                int half = radius - Mathf.Abs(y);
                for (int x = -half; x <= half; x++) SetPixel(pixels, width, height, cx + x, cy + y, color);
            }
        }

        private static void DrawTriangle(Color32[] pixels, int width, int height, int ax, int ay, int bx, int by, int cx, int cy, Color32 color)
        {
            int minX = Mathf.Max(0, Mathf.Min(ax, Mathf.Min(bx, cx)));
            int maxX = Mathf.Min(width - 1, Mathf.Max(ax, Mathf.Max(bx, cx)));
            int minY = Mathf.Max(0, Mathf.Min(ay, Mathf.Min(by, cy)));
            int maxY = Mathf.Min(height - 1, Mathf.Max(ay, Mathf.Max(by, cy)));
            float area = Edge(ax, ay, bx, by, cx, cy);
            if (Mathf.Abs(area) < .01f) return;
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    float w0 = Edge(bx, by, cx, cy, x, y);
                    float w1 = Edge(cx, cy, ax, ay, x, y);
                    float w2 = Edge(ax, ay, bx, by, x, y);
                    if ((w0 >= 0 && w1 >= 0 && w2 >= 0) || (w0 <= 0 && w1 <= 0 && w2 <= 0)) SetPixel(pixels, width, height, x, y, color);
                }
        }

        private static float Edge(float ax, float ay, float bx, float by, float px, float py) => (px - ax) * (by - ay) - (py - ay) * (bx - ax);
        private static void SetPixel(Color32[] pixels, int width, int height, int x, int y, Color32 color) { if (x >= 0 && x < width && y >= 0 && y < height) pixels[y * width + x] = color; }

        private static Color[] AnimalColors() => new[]
        {
            new Color(.95f,.82f,.25f), new Color(1f,.42f,.12f), new Color(.15f,.86f,.92f), new Color(1f,.24f,.12f), new Color(.65f,.25f,.92f),
            new Color(.2f,.55f,1f), new Color(.35f,1f,.58f), new Color(.62f,.82f,1f), new Color(.85f,.22f,.72f), new Color(1f,.7f,.12f)
        };
        private static Color EnemyColor(int i) => Color.HSVToRGB(Mathf.Repeat(.93f - i * .047f, 1f), .72f, .95f);

        private static Sprite[] LoadBackgrounds()
        {
            string[] names = { "meadow_orbit", "jungle_nebula", "arctic_expanse", "mystic_void", "storm_galaxy" };
            var result = new Sprite[names.Length];
            for (int i = 0; i < result.Length; i++) result[i] = LoadSprite($"{ArtRoot}/Backgrounds/{names[i]}.png");
            return result;
        }

        private static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        private static T GetOrCreate<T>(string path, out bool created) where T : ScriptableObject
        {
            EnsureAssetFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            created = asset == null;
            if (!created) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                Root, ArtRoot, ArtRoot + "/Players", ArtRoot + "/Portraits", ArtRoot + "/Enemies", ArtRoot + "/Bosses",
                ArtRoot + "/Projectiles", ArtRoot + "/Pickups", ArtRoot + "/Backgrounds", ArtRoot + "/UI",
                DataRoot, DataRoot + "/Levels", DataRoot + "/Animals", DataRoot + "/Enemies", DataRoot + "/Bosses",
                DataRoot + "/Weapons", DataRoot + "/Projectiles", DataRoot + "/VFX", PrefabRoot, Root + "/Materials"
            };
            for (int i = 0; i < folders.Length; i++) EnsureAssetFolder(folders[i]);
        }

        private static void EnsureAssetFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static string SafeName(string value)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '_');
            return value.Replace(' ', '_');
        }

        [MenuItem("Tools/Animal Fall/Mega Shooter/Generate Dedicated Scene")]
        public static void GenerateMegaShooterScene()
        {
            EnsureFolders();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                EnsureSceneInBuildSettings();
                Debug.Log("[MegaShooterGenerator] MegaShooterScene already exists; preserved existing scene wiring.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraGo = new GameObject("MainCamera");
            cameraGo.tag = "MainCamera";
            Camera camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 8.9f; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.01f, .025f, .08f); cameraGo.transform.position = new Vector3(0f, 0f, -10f);
            cameraGo.AddComponent<AudioListener>();

            GameObject systems = new GameObject("MegaShooterSystems");
            GameObject poolsGo = Child(systems, "MegaObjectPools");
            MegaObjectPools pools = poolsGo.AddComponent<MegaObjectPools>();
            GameObject directorGo = Child(systems, "WaveDirector");
            MegaWaveDirector director = directorGo.AddComponent<MegaWaveDirector>();
            MegaEnemySpawner spawner = directorGo.AddComponent<MegaEnemySpawner>();
            SetObjectReference(director, "_spawner", spawner);
            MegaShooterInput shooterInput = Child(systems, "MegaShooterInput").AddComponent<MegaShooterInput>();
            MegaCameraEffects cameraEffects = Child(systems, "MegaCameraEffects").AddComponent<MegaCameraEffects>();
            MegaDebugOverlay debug = Child(systems, "MegaDebugOverlay").AddComponent<MegaDebugOverlay>();
            Child(systems, "LivesManager").AddComponent<LivesManager>();

            GameObject starfieldGo = new GameObject("Starfield");
            MegaStarfield starfield = starfieldGo.AddComponent<MegaStarfield>();
            SpriteRenderer[] layers = new SpriteRenderer[4];
            Sprite bg = LoadSprite($"{ArtRoot}/Backgrounds/meadow_orbit.png");
            for (int i = 0; i < layers.Length; i++)
            {
                GameObject layer = Child(starfieldGo, $"Parallax_{i + 1}");
                layers[i] = layer.AddComponent<SpriteRenderer>();
                layers[i].sprite = bg; layers[i].sortingOrder = -20 + i; layers[i].color = new Color(1f, 1f, 1f, .28f + i * .12f);
                layer.transform.localScale = Vector3.one * (1.02f + i * .015f);
            }
            SetArrayReference(starfield, "_layers", layers);

            GameObject containers = new GameObject("CombatContainers");
            Transform playerContainer = Child(containers, "Player").transform;
            Transform enemyContainer = Child(containers, "Enemies").transform;
            Transform projectileContainer = Child(containers, "Projectiles").transform;
            Transform pickupContainer = Child(containers, "Pickups").transform;

            Canvas canvas = CreateCanvas();
            GameObject safeArea = Child(canvas.gameObject, "SafeArea");
            RectTransform safeRect = safeArea.AddComponent<RectTransform>();
            Stretch(safeRect);
            safeArea.AddComponent<MegaSafeArea>();
            MegaHUD hud = safeArea.AddComponent<MegaHUD>();
            BuildHud(hud, safeRect);

            Image flash = CreatePanel(canvas.transform, "ReducedFlashOverlay", new Color(1f, 1f, 1f, 0f), Vector2.zero, Vector2.one);
            flash.raycastTarget = false;
            flash.gameObject.SetActive(false);
            SetObjectReference(cameraEffects, "_camera", camera);
            SetObjectReference(cameraEffects, "_flashOverlay", flash);

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            GameObject managerGo = Child(systems, "MegaShooterGameManager");
            MegaShooterGameManager manager = managerGo.AddComponent<MegaShooterGameManager>();
            manager.worldCamera = camera; manager.pools = pools; manager.waveDirector = director; manager.shooterInput = shooterInput;
            manager.hud = hud; manager.cameraEffects = cameraEffects; manager.starfield = starfield; manager.debugOverlay = debug;
            manager.playerContainer = playerContainer; manager.enemyContainer = enemyContainer; manager.projectileContainer = projectileContainer; manager.pickupContainer = pickupContainer;
            manager.pickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/MegaPickup.prefab");
            manager.healthPickupSprite = LoadSprite($"{ArtRoot}/Pickups/health.png");
            manager.counterPickupSprite = LoadSprite($"{ArtRoot}/Pickups/counter.png");
            manager.defaultEnemyProjectile = AssetDatabase.LoadAssetAtPath<ProjectileData>($"{DataRoot}/Projectiles/enemy_bolt.asset");
            manager.debugLevel = AssetDatabase.LoadAssetAtPath<MegaLevelData>($"{DataRoot}/Levels/Mega_01_Level_005.asset");

            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureSceneInBuildSettings();
            Selection.activeObject = managerGo;
            Debug.Log($"[MegaShooterGenerator] Created and wired {ScenePath}.");
        }

        [MenuItem("Tools/Animal Fall/Mega Shooter/Revamp Mega Scene Presentation")]
        public static void RevampMegaScenePresentation()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            MegaHUD hud = UnityEngine.Object.FindFirstObjectByType<MegaHUD>();
            if (canvas == null || hud == null)
            {
                Debug.LogError("[MegaShooterGenerator] MegaShooterScene HUD is missing.");
                return;
            }

            Sprite[] icons = LoadAllSprites($"{MegaResourceRoot}/icons.png");
            if (icons.Length < 21)
            {
                Debug.LogError("[MegaShooterGenerator] icons.png is not sliced into the expected 21 sprites.");
                return;
            }

            RectTransform safeRoot = (RectTransform)hud.transform;
            Image hudFrame = EnsureImage(safeRoot, "MegaHudFrame", icons[0], new Vector2(.02f, .935f), new Vector2(.98f, .995f));
            hudFrame.type = Image.Type.Sliced;
            hudFrame.color = new Color(1f, 1f, 1f, .92f);
            hudFrame.transform.SetAsFirstSibling();

            ApplySprite(hud.healthText?.transform.parent, icons[0], new Color(1f, 1f, 1f, .9f));
            ApplySprite(hud.pauseButton, icons[4]);
            ApplySprite(hud.counterButton, icons[5]);
            ApplySprite(hud.startButton, icons[1]);
            ApplySprite(hud.previousAnimalButton, icons[4]);
            ApplySprite(hud.nextAnimalButton, icons[4]);
            ApplySprite(hud.resumeButton, icons[1]);
            ApplySprite(hud.retryButton, icons[1]);
            ApplySprite(hud.quitButton, icons[1]);
            ApplySprite(hud.resultRetryButton, icons[1]);
            ApplySprite(hud.resultQuitButton, icons[1]);
            if (hud.animalPortrait != null) AddImageFrame(hud.animalPortrait, icons[3]);

            Sprite heroCurrent = hud.selectionWeaponIcon != null ? hud.selectionWeaponIcon.sprite : null;
            Sprite villainOneCurrent = hud.villainOnePortrait != null ? hud.villainOnePortrait.sprite : null;
            Sprite villainTwoCurrent = hud.villainTwoPortrait != null ? hud.villainTwoPortrait.sprite : null;
            Sprite villainOneWeaponCurrent = hud.villainOneWeaponIcon != null ? hud.villainOneWeaponIcon.sprite : null;
            Sprite villainTwoWeaponCurrent = hud.villainTwoWeaponIcon != null ? hud.villainTwoWeaponIcon.sprite : null;
            Sprite bossCurrent = hud.bossPortrait != null ? hud.bossPortrait.sprite : null;
            Sprite bossWeaponCurrent = hud.bossWeaponIcon != null ? hud.bossWeaponIcon.sprite : null;

            Image healthIcon = EnsureImage(safeRoot, "HealthIcon", icons[6], new Vector2(.025f, .945f), new Vector2(.082f, .99f));
            healthIcon.preserveAspect = true;
            Image waveIcon = EnsureImage(safeRoot, "WaveIcon", icons[18], new Vector2(.445f, .948f), new Vector2(.495f, .987f));
            waveIcon.preserveAspect = true;
            Image coinIcon = EnsureImage(safeRoot, "ScoreIcon", icons[14], new Vector2(.49f, .915f), new Vector2(.535f, .95f));
            coinIcon.preserveAspect = true;
            if (hud.healthText != null) hud.healthText.rectTransform.anchoredPosition += new Vector2(58f, 0f);

            Image selectionPanel = hud.selectionRoot != null ? hud.selectionRoot.GetComponent<Image>() : null;
            if (selectionPanel != null)
            {
                selectionPanel.sprite = icons[3];
                selectionPanel.type = Image.Type.Sliced;
                selectionPanel.color = new Color(.82f, .92f, 1f, .96f);
            }

            if (hud.selectionPortrait != null)
            {
                hud.selectionPortrait.rectTransform.anchorMin = new Vector2(.5f, .5f);
                hud.selectionPortrait.rectTransform.anchorMax = new Vector2(.5f, .5f);
                hud.selectionPortrait.rectTransform.pivot = new Vector2(.5f, .5f);
                hud.selectionPortrait.rectTransform.anchoredPosition = new Vector2(0f, 210f);
                hud.selectionPortrait.rectTransform.sizeDelta = new Vector2(300f, 300f);
                AddImageFrame(hud.selectionPortrait, icons[5]);
            }

            hud.selectionWeaponIcon = EnsureImage(hud.selectionRoot.transform, "HeroWeapon", heroCurrent,
                new Vector2(.12f, .405f), new Vector2(.27f, .505f));
            hud.selectionWeaponIcon.preserveAspect = true;

            Image intelPanel = EnsureImage(hud.selectionRoot.transform, "EnemyIntelPanel", icons[0],
                new Vector2(.04f, .04f), new Vector2(.96f, .27f));
            intelPanel.type = Image.Type.Sliced;
            EnsureText(intelPanel.transform, "IntelTitle", "MISSION THREATS  •  TWO VILLAIN TYPES  +  MEGA VILLAIN",
                new Vector2(.03f, .78f), new Vector2(.97f, .98f), 22, TextAnchor.MiddleCenter);
            hud.villainOnePortrait = EnsureImage(intelPanel.transform, "VillainOne", villainOneCurrent, new Vector2(.05f, .08f), new Vector2(.22f, .75f));
            hud.villainTwoPortrait = EnsureImage(intelPanel.transform, "VillainTwo", villainTwoCurrent, new Vector2(.29f, .08f), new Vector2(.46f, .75f));
            hud.bossPortrait = EnsureImage(intelPanel.transform, "MegaVillain", bossCurrent, new Vector2(.62f, .05f), new Vector2(.82f, .78f));
            hud.villainOneWeaponIcon = EnsureImage(intelPanel.transform, "VillainOneWeapon", villainOneWeaponCurrent, new Vector2(.18f, .1f), new Vector2(.27f, .42f));
            hud.villainTwoWeaponIcon = EnsureImage(intelPanel.transform, "VillainTwoWeapon", villainTwoWeaponCurrent, new Vector2(.42f, .1f), new Vector2(.51f, .42f));
            hud.bossWeaponIcon = EnsureImage(intelPanel.transform, "MegaVillainWeapon", bossWeaponCurrent, new Vector2(.79f, .1f), new Vector2(.9f, .46f));
            foreach (Image preview in new[] { hud.villainOnePortrait, hud.villainTwoPortrait, hud.bossPortrait,
                         hud.villainOneWeaponIcon, hud.villainTwoWeaponIcon, hud.bossWeaponIcon })
                if (preview != null) preview.preserveAspect = true;

            if (hud.selectionDescription != null)
            {
                hud.selectionDescription.rectTransform.anchorMin = new Vector2(.28f, .395f);
                hud.selectionDescription.rectTransform.anchorMax = new Vector2(.84f, .49f);
                hud.selectionDescription.rectTransform.offsetMin = Vector2.zero;
                hud.selectionDescription.rectTransform.offsetMax = Vector2.zero;
                hud.selectionDescription.fontSize = 21;
                hud.selectionDescription.alignment = TextAnchor.MiddleLeft;
            }
            if (hud.selectionLockText != null)
            {
                hud.selectionLockText.rectTransform.anchorMin = new Vector2(.68f, .50f);
                hud.selectionLockText.rectTransform.anchorMax = new Vector2(.88f, .55f);
                hud.selectionLockText.rectTransform.offsetMin = Vector2.zero;
                hud.selectionLockText.rectTransform.offsetMax = Vector2.zero;
            }
            if (hud.startButton != null)
            {
                RectTransform startRect = (RectTransform)hud.startButton.transform;
                startRect.anchorMin = new Vector2(.5f, .34f);
                startRect.anchorMax = new Vector2(.5f, .34f);
                startRect.pivot = new Vector2(.5f, .5f);
                startRect.anchoredPosition = Vector2.zero;
                startRect.sizeDelta = new Vector2(410f, 90f);
            }

            if (hud.bossHealthRoot != null)
            {
                Image panel = hud.bossHealthRoot.GetComponent<Image>();
                if (panel != null) { panel.sprite = icons[0]; panel.type = Image.Type.Sliced; panel.color = Color.white; }
                if (hud.bossHealthFill != null) hud.bossHealthFill.color = new Color(1f, .04f, .12f, 1f);
                Image crest = EnsureImage(hud.bossHealthRoot.transform, "BossCrest", icons[16], new Vector2(.015f, .1f), new Vector2(.12f, .9f));
                crest.preserveAspect = true;
            }

            EditorUtility.SetDirty(hud);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[MegaShooterGenerator] Revamped only MegaShooterScene with megalevel icons and threat preview panels.");
        }

        private static Image EnsureImage(Transform parent, string name, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax)
        {
            Transform existing = parent.Find(name);
            Image image;
            if (existing != null) image = existing.GetComponent<Image>();
            else
            {
                GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(parent, false);
                image = go.GetComponent<Image>();
            }
            RectTransform rect = image.rectTransform;
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static Text EnsureText(Transform parent, string name, string value, Vector2 anchorMin, Vector2 anchorMax, int size, TextAnchor alignment)
        {
            Transform existing = parent.Find(name);
            Text label;
            if (existing != null) label = existing.GetComponent<Text>();
            else
            {
                GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                go.transform.SetParent(parent, false);
                label = go.GetComponent<Text>();
            }
            RectTransform rect = label.rectTransform;
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = value; label.fontSize = size; label.alignment = alignment; label.color = Color.white; label.raycastTarget = false;
            return label;
        }

        private static void ApplySprite(Component target, Sprite sprite)
        {
            if (target == null) return;
            Image image = target.GetComponent<Image>();
            if (image == null) return;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        private static void ApplySprite(Transform target, Sprite sprite, Color color)
        {
            if (target == null) return;
            Image image = target.GetComponent<Image>();
            if (image == null) return;
            image.sprite = sprite; image.type = Image.Type.Sliced; image.color = color;
        }

        private static void AddImageFrame(Image target, Sprite frameSprite)
        {
            if (target == null || target.transform.parent.Find(target.name + "Frame") != null) return;
            Image frame = EnsureImage(target.transform.parent, target.name + "Frame", frameSprite,
                target.rectTransform.anchorMin, target.rectTransform.anchorMax);
            frame.rectTransform.pivot = target.rectTransform.pivot;
            frame.rectTransform.anchoredPosition = target.rectTransform.anchoredPosition;
            frame.rectTransform.sizeDelta = target.rectTransform.sizeDelta + new Vector2(22f, 22f);
            frame.color = new Color(1f, 1f, 1f, .92f);
            frame.transform.SetSiblingIndex(target.transform.GetSiblingIndex());
            target.transform.SetAsLastSibling();
        }

        private static void BuildHud(MegaHUD hud, RectTransform safeRoot)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hud.healthText = CreateText(safeRoot, "Health", "HP 5/5", font, 34, TextAnchor.MiddleLeft, new Vector2(20, -20), new Vector2(280, 70), new Vector2(0, 1));
            hud.waveText = CreateText(safeRoot, "Wave", "WAVE 1/3", font, 38, TextAnchor.MiddleCenter, new Vector2(-170, -20), new Vector2(340, 70), new Vector2(.5f, 1));
            hud.scoreText = CreateText(safeRoot, "Score", "0", font, 28, TextAnchor.MiddleCenter, new Vector2(-140, -88), new Vector2(280, 54), new Vector2(.5f, 1));
            hud.pauseButton = CreateButton(safeRoot, "PauseButton", "Ⅱ", font, new Vector2(-108, -20), new Vector2(88, 70), new Vector2(1, 1), new Color(.18f, .32f, .55f, .95f));
            hud.animalPortrait = CreateImage(safeRoot, "AnimalPortrait", LoadSprite($"{ArtRoot}/Portraits/eagle_striker_portrait.png"), new Vector2(22, 22), new Vector2(108, 108), new Vector2(0, 0));

            hud.counterButton = CreateButton(safeRoot, "CounterButton", "COUNTER", font, new Vector2(-228, 24), new Vector2(204, 130), new Vector2(1, 0), new Color(.08f, .32f, .48f, .96f));
            hud.counterFill = CreateImage(hud.counterButton.transform, "CounterFill", null, Vector2.zero, Vector2.zero, new Vector2(.5f, .5f));
            RectTransform counterFillRect = hud.counterFill.rectTransform; Stretch(counterFillRect); counterFillRect.SetAsFirstSibling();
            hud.counterFill.type = Image.Type.Filled; hud.counterFill.fillMethod = Image.FillMethod.Radial360; hud.counterFill.fillAmount = 0f; hud.counterFill.color = new Color(.2f, .9f, 1f, .55f); hud.counterFill.raycastTarget = false;

            GameObject bossRoot = CreatePanel(safeRoot, "BossHealthRoot", new Color(.04f, .05f, .12f, .88f), new Vector2(.16f, .84f), new Vector2(.84f, .95f)).gameObject;
            hud.bossHealthRoot = bossRoot;
            hud.bossNameText = CreateText(bossRoot.transform, "BossName", "BOSS", font, 28, TextAnchor.MiddleCenter, new Vector2(0, -4), new Vector2(0, 46), new Vector2(.5f, 1));
            hud.bossHealthFill = CreateImage(bossRoot.transform, "BossHealth", null, new Vector2(18, 14), new Vector2(-36, 28), Vector2.zero);
            RectTransform bossFillRect = hud.bossHealthFill.rectTransform; bossFillRect.anchorMin = new Vector2(0, 0); bossFillRect.anchorMax = new Vector2(1, 0); bossFillRect.pivot = new Vector2(.5f, 0); bossFillRect.offsetMin = new Vector2(18, 12); bossFillRect.offsetMax = new Vector2(-18, 38);
            hud.bossHealthFill.type = Image.Type.Filled; hud.bossHealthFill.fillMethod = Image.FillMethod.Horizontal; hud.bossHealthFill.color = new Color(1f, .2f, .28f);
            bossRoot.SetActive(false);

            hud.bannerRoot = CreatePanel(safeRoot, "Banner", new Color(.1f, .03f, .08f, .86f), new Vector2(0, .43f), new Vector2(1, .57f)).gameObject;
            hud.bannerText = CreateText(hud.bannerRoot.transform, "BannerText", "WARNING", font, 44, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero, new Vector2(.5f, .5f));
            Stretch(hud.bannerText.rectTransform); hud.bannerRoot.SetActive(false);
            hud.countdownText = CreateText(safeRoot, "Countdown", "3", font, 96, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(420, 180), new Vector2(.5f, .5f));
            hud.countdownText.gameObject.SetActive(false);

            BuildSelection(hud, safeRoot, font);
            BuildPause(hud, safeRoot, font);
            BuildResults(hud, safeRoot, font);
        }

        private static void BuildSelection(MegaHUD hud, RectTransform root, Font font)
        {
            hud.selectionRoot = CreatePanel(root, "AnimalSelection", new Color(.025f, .05f, .13f, .96f), new Vector2(.05f, .1f), new Vector2(.95f, .9f)).gameObject;
            hud.selectionTitle = CreateText(hud.selectionRoot.transform, "Title", "EAGLE STRIKER", font, 54, TextAnchor.MiddleCenter, new Vector2(0, -48), new Vector2(0, 90), new Vector2(.5f, 1));
            RectTransform titleRect = hud.selectionTitle.rectTransform; titleRect.anchorMin = new Vector2(0, 1); titleRect.anchorMax = new Vector2(1, 1); titleRect.offsetMin = new Vector2(20, -140); titleRect.offsetMax = new Vector2(-20, -35);
            hud.selectionPortrait = CreateImage(hud.selectionRoot.transform, "Portrait", LoadSprite($"{ArtRoot}/Portraits/eagle_striker_portrait.png"), new Vector2(-170, -70), new Vector2(340, 340), new Vector2(.5f, .72f));
            hud.selectionDescription = CreateText(hud.selectionRoot.transform, "Description", "Fast twin feather bolts.", font, 30, TextAnchor.UpperCenter, new Vector2(-360, -50), new Vector2(720, 300), new Vector2(.5f, .48f));
            hud.selectionDescription.horizontalOverflow = HorizontalWrapMode.Wrap; hud.selectionDescription.verticalOverflow = VerticalWrapMode.Truncate;
            hud.selectionLockText = CreateText(hud.selectionRoot.transform, "LockState", "READY", font, 32, TextAnchor.MiddleCenter, new Vector2(-200, 70), new Vector2(400, 70), new Vector2(.5f, .25f));
            hud.previousAnimalButton = CreateButton(hud.selectionRoot.transform, "Previous", "‹", font, new Vector2(40, -40), new Vector2(120, 120), new Vector2(0, .55f), new Color(.12f, .3f, .52f));
            hud.nextAnimalButton = CreateButton(hud.selectionRoot.transform, "Next", "›", font, new Vector2(-160, -40), new Vector2(120, 120), new Vector2(1, .55f), new Color(.12f, .3f, .52f));
            hud.startButton = CreateButton(hud.selectionRoot.transform, "Start", "LAUNCH", font, new Vector2(-210, 55), new Vector2(420, 110), new Vector2(.5f, 0), new Color(.08f, .72f, .72f));

            hud.unlockRoot = CreatePanel(hud.selectionRoot.transform, "UnlockCelebration", new Color(.12f, .04f, .2f, .98f), new Vector2(.08f, .22f), new Vector2(.92f, .78f)).gameObject;
            hud.unlockText = CreateText(hud.unlockRoot.transform, "UnlockText", "NEW SUPER ANIMAL", font, 48, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero, new Vector2(.5f, .5f)); Stretch(hud.unlockText.rectTransform);
            hud.unlockContinueButton = CreateButton(hud.unlockRoot.transform, "Continue", "CONTINUE", font, new Vector2(-180, 45), new Vector2(360, 90), new Vector2(.5f, 0), new Color(.75f, .28f, .85f));
            hud.unlockRoot.SetActive(false);
        }

        private static void BuildPause(MegaHUD hud, RectTransform root, Font font)
        {
            hud.pauseRoot = CreatePanel(root, "PausePanel", new Color(.02f, .035f, .09f, .97f), new Vector2(.12f, .23f), new Vector2(.88f, .77f)).gameObject;
            Text title = CreateText(hud.pauseRoot.transform, "PauseTitle", "PAUSED", font, 58, TextAnchor.MiddleCenter, new Vector2(0, -45), new Vector2(0, 100), new Vector2(.5f, 1)); StretchHorizontal(title.rectTransform, 20, -145, -30);
            hud.resumeButton = CreateButton(hud.pauseRoot.transform, "Resume", "RESUME", font, new Vector2(-210, 80), new Vector2(420, 100), new Vector2(.5f, .5f), new Color(.08f, .68f, .72f));
            hud.retryButton = CreateButton(hud.pauseRoot.transform, "Retry", "RETRY", font, new Vector2(-210, -35), new Vector2(420, 100), new Vector2(.5f, .5f), new Color(.2f, .38f, .62f));
            hud.quitButton = CreateButton(hud.pauseRoot.transform, "Quit", "QUIT", font, new Vector2(-210, -150), new Vector2(420, 100), new Vector2(.5f, .5f), new Color(.5f, .16f, .28f));
            hud.pauseRoot.SetActive(false);
        }

        private static void BuildResults(MegaHUD hud, RectTransform root, Font font)
        {
            hud.resultRoot = CreatePanel(root, "ResultsPanel", new Color(.025f, .05f, .13f, .98f), new Vector2(.08f, .17f), new Vector2(.92f, .83f)).gameObject;
            hud.resultTitle = CreateText(hud.resultRoot.transform, "ResultTitle", "MEGA VICTORY!", font, 58, TextAnchor.MiddleCenter, new Vector2(0, -45), new Vector2(0, 110), new Vector2(.5f, 1)); StretchHorizontal(hud.resultTitle.rectTransform, 20, -160, -30);
            hud.resultSummary = CreateText(hud.resultRoot.transform, "Summary", "Score 0", font, 38, TextAnchor.MiddleCenter, new Vector2(-320, -100), new Vector2(640, 300), new Vector2(.5f, .58f));
            hud.resultRetryButton = CreateButton(hud.resultRoot.transform, "ResultRetry", "RETRY", font, new Vector2(-210, 160), new Vector2(420, 100), new Vector2(.5f, 0), new Color(.18f, .4f, .68f));
            hud.resultQuitButton = CreateButton(hud.resultRoot.transform, "ResultQuit", "MAP", font, new Vector2(-210, 45), new Vector2(420, 100), new Vector2(.5f, 0), new Color(.12f, .65f, .62f));
            hud.resultRoot.SetActive(false);
        }

        private static Canvas CreateCanvas()
        {
            GameObject go = new GameObject("MegaShooterCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            CanvasScaler scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = .5f;
            return canvas;
        }

        private static Image CreatePanel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform; rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            Image image = go.GetComponent<Image>(); image.color = color;
            return image;
        }

        private static Text CreateText(Transform parent, string name, string value, Font font, int size, TextAnchor alignment, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 anchor)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform; rect.anchorMin = anchor; rect.anchorMax = anchor; rect.pivot = anchor; rect.anchoredPosition = anchoredPosition; rect.sizeDelta = sizeDelta;
            Text text = go.GetComponent<Text>(); text.font = font; text.text = value; text.fontSize = size; text.alignment = alignment; text.color = Color.white; text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Font font, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 anchor, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform; rect.anchorMin = anchor; rect.anchorMax = anchor; rect.pivot = anchor; rect.anchoredPosition = anchoredPosition; rect.sizeDelta = sizeDelta;
            go.GetComponent<Image>().color = color;
            Button button = go.GetComponent<Button>();
            Text text = CreateText(go.transform, "Label", label, font, 32, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero, new Vector2(.5f, .5f)); Stretch(text.rectTransform);
            return button;
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 anchor)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform; rect.anchorMin = anchor; rect.anchorMax = anchor; rect.pivot = anchor; rect.anchoredPosition = anchoredPosition; rect.sizeDelta = sizeDelta;
            Image image = go.GetComponent<Image>(); image.sprite = sprite; image.preserveAspect = true;
            return image;
        }

        private static GameObject Child(GameObject parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; rect.pivot = new Vector2(.5f, .5f); }
        private static void StretchHorizontal(RectTransform rect, float side, float bottom, float top) { rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1); rect.offsetMin = new Vector2(side, bottom); rect.offsetMax = new Vector2(-side, top); }

        private static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null) { property.objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo(); }
        }

        private static void SetArrayReference<T>(UnityEngine.Object target, string propertyName, T[] values) where T : UnityEngine.Object
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) return;
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureSceneInBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < scenes.Count; i++) if (scenes[i].path == ScenePath) { scenes[i].enabled = true; EditorBuildSettings.scenes = scenes.ToArray(); return; }
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
