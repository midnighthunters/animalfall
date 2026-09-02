#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace AnimalFall.MegaShooter.Editor
{
    /// <summary>
    /// Owns only the 20 mega encounters. Each chapter is an army level followed by its matching boss level.
    /// Normal LevelData content is deliberately outside this pipeline.
    /// </summary>
    public static class MegaVillainRosterGenerator
    {
        public const string VillainArtRoot = "Assets/Resources/icons/megalevelhindrances";
        public const string VillainDataRoot = MegaShooterGenerator.DataRoot + "/VillainRoster";
        public const string VillainProjectileSpriteSheetPath = "Assets/Resources/megalevel/villain_weapon.png";

        private static readonly string[] Ids =
        {
            "venom_emperor", "admiral_inkstorm", "ironhorn", "captain_chomper", "general_smash",
            "emperor_sting", "croc_commander", "doom_puffer", "queen_webula", "cosmic_draconis"
        };

        private static readonly string[] Names =
        {
            "Venom Emperor", "Admiral Inkstorm", "Ironhorn", "Captain Chomper", "General Smash",
            "Emperor Sting", "Croc Commander", "Doom Puffer", "Queen Webula", "Cosmic Draconis"
        };

        private static readonly string[] Species =
        {
            "King Cobra", "Octopus", "Rhino", "Shark", "Gorilla",
            "Scorpion", "Crocodile", "Pufferfish", "Spider", "Dragon"
        };

        private static readonly string[] ArtFiles =
        {
            "Venom_Emperor", "Admiral_Inkstorm", "Ironhorn", "Captain_Chomper", "General_Smash",
            "Emperor_Sting", "Croc_Commander", "Doom_Puffer", "Queen_Webula", "Cosmic_Draconis"
        };

        private static readonly MegaVillainArchetype[] Archetypes =
        {
            MegaVillainArchetype.VenomEmperor, MegaVillainArchetype.AdmiralInkstorm,
            MegaVillainArchetype.Ironhorn, MegaVillainArchetype.CaptainChomper,
            MegaVillainArchetype.GeneralSmash, MegaVillainArchetype.EmperorSting,
            MegaVillainArchetype.CrocCommander, MegaVillainArchetype.DoomPuffer,
            MegaVillainArchetype.QueenWebula, MegaVillainArchetype.CosmicDraconis
        };

        private static readonly Color[] Colors =
        {
            new Color(.30f, 1f, .08f), new Color(.56f, .12f, 1f), new Color(1f, .36f, .05f),
            new Color(.05f, .62f, 1f), new Color(1f, .22f, .04f), new Color(1f, .18f, .03f),
            new Color(.95f, .48f, .04f), new Color(.04f, .92f, .82f), new Color(.92f, .08f, 1f),
            new Color(1f, .08f, .26f)
        };

        private static readonly string[,] ArmyNames =
        {
            { "Fang Interceptor", "Venom Alchemist", "Cobra Sentinel" },
            { "Ink Marine", "Claw Engineer", "Abyss Angler" },
            { "Horn Vanguard", "Iron Charger", "Siege Brute" },
            { "Torpedo Shark", "Chomper Gunner", "Manta Scout" },
            { "Smash Cadet", "Meteor Raider", "Armored Enforcer" },
            { "Sting Lancer", "Claw Drone", "Poison Bomber" },
            { "Croc Striker", "Jaw Rocket", "Tank Guard" },
            { "Spike Scout", "Toxin Medusa", "Doom Guard" },
            { "Web Drone", "Silk Mystic", "Nightwing Spiderling" },
            { "Plasma Wyrm", "Meteor Phoenix", "Cosmic Mage" }
        };

        private static readonly string[,] AttackNames =
        {
            { "POISON LASER", "VENOM BLOBS", "HOMING FANGS" },
            { "INK BOMBS", "TENTACLE GRAB", "DARK-SCREEN INK ZONE" },
            { "IRONHORN CHARGE", "ROCKET SALVO", "GROUND SHOCKWAVE" },
            { "HOMING TORPEDOES", "SPINNING TEETH", "MINI SHARK DRONES" },
            { "METEOR THROW", "ENERGY PUNCH", "DOUBLE-FIST BULLET RING" },
            { "TAIL LASER", "CLAW DRONES", "POISON MINEFIELD" },
            { "MISSILE BARRAGE", "HEAVY CANNONBALLS", "MECHANICAL JAWS" },
            { "SPIKE STORM", "DOOM INFLATION", "FULL-SCREEN SPINE RING" },
            { "STICKY WEBS", "SPIDERLING SWARM", "LASER-WEB GRID" },
            { "PLASMA BREATH", "METEOR SHOWER", "COSMIC ASCENSION" }
        };

        public static MegaLevelData[] Generate(GameObject enemyPrefab, GameObject bossPrefab,
            GameObject projectilePrefab, GameObject effectPrefab, SuperAnimalData[] animals,
            Sprite[] backgrounds, MegaVFXProfile vfx)
        {
            EnsureFolders();
            NormalizeArmySheets();
            NormalizeVillainProjectileSheet();
            Sprite[] weaponSprites = LoadAllSprites(VillainProjectileSpriteSheetPath);
            if (weaponSprites.Length < Ids.Length)
            {
                Debug.LogError($"[MegaVillainRoster] {VillainProjectileSpriteSheetPath} must contain {Ids.Length} sliced villain projectile sprites; found {weaponSprites.Length}.");
                return Array.Empty<MegaLevelData>();
            }
            CreateDistinctMuzzleVfx(vfx, effectPrefab);

            var projectiles = new ProjectileData[10][];
            var armies = new EnemyShipData[10][];
            var bosses = new BossShipData[10];
            for (int family = 0; family < 10; family++)
            {
                // One dedicated projectile visual per villain family is shared by
                // its army variants, boss phases, and matching HUD weapon icon.
                Sprite weaponSprite = weaponSprites[family];
                projectiles[family] = GenerateProjectiles(family, projectilePrefab, weaponSprite);
                armies[family] = GenerateArmy(family, enemyPrefab, projectiles[family], weaponSprite);
                bosses[family] = GenerateBoss(family, bossPrefab, effectPrefab, projectiles[family], weaponSprite);
            }

            var levels = new MegaLevelData[20];
            for (int sequence = 0; sequence < levels.Length; sequence++)
                levels[sequence] = GenerateLevel(sequence, armies, bosses[sequence / 2],
                    animals, backgrounds, vfx);
            AssetDatabase.SaveAssets();
            return levels;
        }

        private static ProjectileData[] GenerateProjectiles(int family, GameObject prefab, Sprite sprite)
        {
            MegaProjectileMotion specialMotion = family switch
            {
                0 => MegaProjectileMotion.Homing,
                1 => MegaProjectileMotion.Sine,
                2 => MegaProjectileMotion.Homing,
                3 => MegaProjectileMotion.Homing,
                4 => MegaProjectileMotion.Homing,
                5 => MegaProjectileMotion.Homing,
                6 => MegaProjectileMotion.Homing,
                7 => MegaProjectileMotion.Straight,
                8 => MegaProjectileMotion.Sine,
                _ => MegaProjectileMotion.Homing
            };
            MegaProjectileMotion hazardMotion = family == 0 || family == 5
                ? MegaProjectileMotion.StationaryMine
                : family == 3 ? MegaProjectileMotion.Returning
                : family == 8 ? MegaProjectileMotion.Sine
                : MegaProjectileMotion.Straight;
            MegaProjectileMotion[] motions = { MegaProjectileMotion.Straight, specialMotion, hazardMotion };
            string[] suffixes = { "bolt", "special", "hazard" };
            var result = new ProjectileData[3];
            for (int variant = 0; variant < result.Length; variant++)
            {
                string path = $"{VillainDataRoot}/Projectiles/{Ids[family]}_{suffixes[variant]}.asset";
                ProjectileData data = GetOrCreate<ProjectileData>(path);
                data.stableId = $"{Ids[family]}_{suffixes[variant]}";
                data.prefab = prefab;
                data.sprite = sprite;
                data.motion = motions[variant];
                data.speed = variant == 2 && hazardMotion == MegaProjectileMotion.StationaryMine ? 1.4f : 6.2f + family * .18f + variant * .7f;
                data.damage = 1f + (family >= 4 ? .35f : 0f) + (variant == 2 ? .35f : 0f);
                data.lifetime = variant == 2 ? 7f : 6f;
                data.colliderRadius = variant == 2 ? .18f : .12f;
                data.homingStrength = motions[variant] == MegaProjectileMotion.Homing ? 2f + family * .06f : 0f;
                data.sineAmplitude = motions[variant] == MegaProjectileMotion.Sine ? 2.1f : 0f;
                data.sineFrequency = motions[variant] == MegaProjectileMotion.Sine ? 5.5f : 0f;
                data.reflectable = family < 8 || variant == 0;
                data.playerColor = new Color(.18f, .92f, 1f, 1f);
                data.enemyColor = Colors[family];
                data.poolingKey = data.stableId;
                EditorUtility.SetDirty(data);
                result[variant] = data;
            }
            return result;
        }

        private static EnemyShipData[] GenerateArmy(int family, GameObject prefab, ProjectileData[] projectiles, Sprite weaponSprite)
        {
            Sprite[] sprites = LoadAllSprites($"{VillainArtRoot}/army/{ArtFiles[family]}_Army.png");
            if (sprites.Length != 3)
                Debug.LogError($"[MegaVillainRoster] {ArtFiles[family]}_Army must contain exactly three sliced sprites; found {sprites.Length}.");
            var result = new EnemyShipData[3];
            MegaMovementPattern[] movement = { MegaMovementPattern.Sine, MegaMovementPattern.ZigZag, MegaMovementPattern.Hover };
            MegaWeaponPattern[] patterns = FamilyArmyPatterns(family);
            for (int variant = 0; variant < result.Length; variant++)
            {
                string path = $"{VillainDataRoot}/Armies/{Ids[family]}_{variant + 1:D2}.asset";
                EnemyShipData data = GetOrCreate<EnemyShipData>(path);
                data.stableId = $"{Ids[family]}_army_{variant + 1}";
                data.displayName = ArmyNames[family, variant];
                data.prefab = prefab;
                data.sprite = sprites.Length > variant ? sprites[variant] : null;
                data.weaponIcon = weaponSprite;
                data.colliderSize = new Vector2(.78f + variant * .08f, .66f + variant * .07f);
                data.hitPoints = 24f + family * 5f + variant * 8f;
                data.contactDamage = family >= 6 ? 2f : 1f;
                data.speed = Mathf.Max(1.35f, 2.8f - family * .06f - variant * .14f);
                data.score = 120 + family * 35 + variant * 45;
                data.movementPattern = family == 2 && variant == 2 ? MegaMovementPattern.Rammer : movement[variant];
                data.weaponPattern = patterns[variant];
                data.projectile = projectiles[variant];
                data.fireInterval = Mathf.Max(1.1f, 2.35f - family * .055f + variant * .12f);
                data.initialFireDelay = 1.15f + variant * .12f;
                data.telegraphTime = patterns[variant] == MegaWeaponPattern.Laser || patterns[variant] == MegaWeaponPattern.Sniper ? .95f : .85f;
                data.poolingKey = data.stableId;
                data.priorityTarget = variant == 2;
                data.pickupChance = variant == 2 ? .08f : .035f;
                EditorUtility.SetDirty(data);
                result[variant] = data;
            }
            return result;
        }

        private static MegaWeaponPattern[] FamilyArmyPatterns(int family)
        {
            return family switch
            {
                0 => new[] { MegaWeaponPattern.FixedSpread, MegaWeaponPattern.Mine, MegaWeaponPattern.AimedSingle },
                1 => new[] { MegaWeaponPattern.Burst, MegaWeaponPattern.FixedSpread, MegaWeaponPattern.Sniper },
                2 => new[] { MegaWeaponPattern.AimedSingle, MegaWeaponPattern.Burst, MegaWeaponPattern.Radial },
                3 => new[] { MegaWeaponPattern.AimedSingle, MegaWeaponPattern.FixedSpread, MegaWeaponPattern.Burst },
                4 => new[] { MegaWeaponPattern.Burst, MegaWeaponPattern.AimedSingle, MegaWeaponPattern.Radial },
                5 => new[] { MegaWeaponPattern.Laser, MegaWeaponPattern.Burst, MegaWeaponPattern.Mine },
                6 => new[] { MegaWeaponPattern.Burst, MegaWeaponPattern.AimedSingle, MegaWeaponPattern.FixedSpread },
                7 => new[] { MegaWeaponPattern.FixedSpread, MegaWeaponPattern.Mine, MegaWeaponPattern.Radial },
                8 => new[] { MegaWeaponPattern.FixedSpread, MegaWeaponPattern.Burst, MegaWeaponPattern.Laser },
                _ => new[] { MegaWeaponPattern.AimedSingle, MegaWeaponPattern.Radial, MegaWeaponPattern.Burst }
            };
        }

        private static BossShipData GenerateBoss(int family, GameObject prefab, GameObject effectPrefab,
            ProjectileData[] projectiles, Sprite weaponSprite)
        {
            string path = $"{VillainDataRoot}/Bosses/Boss_{family + 1:D2}_{ArtFiles[family]}.asset";
            BossShipData data = GetOrCreate<BossShipData>(path);
            data.stableId = Ids[family];
            data.displayName = $"{Species[family]} — {Names[family]}";
            data.archetype = Archetypes[family];
            data.prefab = prefab;
            data.sprite = LoadAllSprites($"{VillainArtRoot}/villains/{ArtFiles[family]}.png").FirstOrDefault();
            data.weaponIcon = weaponSprite;
            data.colliderSize = new Vector2(2.15f + family * .035f, 1.55f + family * .025f);
            data.baseHitPoints = 1400f + family * 850f;
            data.movementBounds = new Rect(-3.45f, 3.7f, 6.9f, 2.8f);
            data.score = 6000 + family * 1100;
            data.entranceDuration = family == 9 ? 2.8f : 2f;
            data.phases = CreateBossPhases(family, projectiles);
            data.entranceVFX = effectPrefab;
            data.deathVFX = effectPrefab;
            EditorUtility.SetDirty(data);
            return data;
        }

        private static BossPhaseData[] CreateBossPhases(int family, ProjectileData[] projectiles)
        {
            int phaseCount = family == 9 ? 4 : family >= 4 ? 3 : 2;
            var phases = new BossPhaseData[phaseCount];
            for (int phaseIndex = 0; phaseIndex < phaseCount; phaseIndex++)
            {
                float intensity = phaseIndex / (float)Mathf.Max(1, phaseCount - 1);
                phases[phaseIndex] = new BossPhaseData
                {
                    healthThreshold = 1f - phaseIndex / (float)phaseCount,
                    movementPattern = BossMovement(family, phaseIndex),
                    attacks = CreateBossAttacks(family, phaseIndex, projectiles),
                    attackInterval = Mathf.Max(.88f, 2.2f - family * .055f - phaseIndex * .18f),
                    projectileSpeedMultiplier = 1f + phaseIndex * .09f,
                    transitionDuration = family == 9 ? 1.35f : 1f,
                    bossScaleMultiplier = BossScale(family, phaseIndex),
                    phaseTint = Color.Lerp(Color.white, Colors[family], .16f + intensity * .22f),
                    cameraShake = .18f + intensity * .13f,
                    warningText = phaseIndex == 0 ? $"{Names[family].ToUpperInvariant()} ONLINE" :
                        family == 9 && phaseIndex == 1 ? "COSMIC DRACONIS — SECOND FORM" : $"PHASE {phaseIndex + 1}"
                };
            }
            return phases;
        }

        private static MegaMovementPattern BossMovement(int family, int phase)
        {
            return family switch
            {
                0 => phase % 2 == 0 ? MegaMovementPattern.Hover : MegaMovementPattern.SideSweep,
                1 => phase % 2 == 0 ? MegaMovementPattern.Hover : MegaMovementPattern.Stationary,
                2 => phase % 2 == 0 ? MegaMovementPattern.Rammer : MegaMovementPattern.SideSweep,
                3 => phase % 2 == 0 ? MegaMovementPattern.SideSweep : MegaMovementPattern.Orbit,
                4 => phase % 2 == 0 ? MegaMovementPattern.Hover : MegaMovementPattern.Rammer,
                5 => phase % 2 == 0 ? MegaMovementPattern.Stationary : MegaMovementPattern.SideSweep,
                6 => phase % 2 == 0 ? MegaMovementPattern.SideSweep : MegaMovementPattern.Stationary,
                7 => phase == 0 ? MegaMovementPattern.Hover : MegaMovementPattern.Stationary,
                8 => phase % 2 == 0 ? MegaMovementPattern.ZigZag : MegaMovementPattern.Stationary,
                _ => phase % 2 == 0 ? MegaMovementPattern.Orbit : MegaMovementPattern.Hover
            };
        }

        private static float BossScale(int family, int phase)
        {
            if (family == 7) return phase == 0 ? .62f : 1.18f + (phase - 1) * .26f;
            if (family == 9) return 1f + phase * .22f;
            return 1f + phase * .045f;
        }

        private static BossAttackPattern[] CreateBossAttacks(int family, int phase, ProjectileData[] projectiles)
        {
            var attacks = new BossAttackPattern[3];
            for (int attackIndex = 0; attackIndex < attacks.Length; attackIndex++)
            {
                MegaWeaponPattern pattern = BossPattern(family, attackIndex);
                int baseCount = pattern == MegaWeaponPattern.Radial ? 10 : pattern == MegaWeaponPattern.Laser ? 3 :
                    pattern == MegaWeaponPattern.Carrier ? 5 : pattern == MegaWeaponPattern.Mine ? 7 : 3;
                var attack = new BossAttackPattern
                {
                    attackName = AttackNames[family, attackIndex],
                    pattern = pattern,
                    projectile = projectiles[attackIndex],
                    weight = attackIndex == 0 ? 1.15f : 1f,
                    telegraphTime = pattern == MegaWeaponPattern.Laser || family == 2 ? 1.15f : .92f,
                    projectileCount = Mathf.Clamp(baseCount + phase * (pattern == MegaWeaponPattern.Radial ? 2 : 1), 1, 24),
                    spreadDegrees = pattern == MegaWeaponPattern.Laser ? 112f : pattern == MegaWeaponPattern.FixedSpread ? 86f : 58f,
                    volleyCount = pattern == MegaWeaponPattern.Burst ? Mathf.Min(5, 3 + phase) : 1,
                    volleyInterval = .16f,
                    aimed = pattern == MegaWeaponPattern.AimedSingle || pattern == MegaWeaponPattern.Sniper || pattern == MegaWeaponPattern.Laser,
                    reflectable = family < 8 && pattern != MegaWeaponPattern.Laser,
                    clearsBulletsBeforeAttack = pattern == MegaWeaponPattern.Laser || (family == 7 && attackIndex == 2),
                    muzzleColor = Colors[family]
                };
                if (family == 1 && attackIndex == 1)
                {
                    attack.playerMovementMultiplier = .18f;
                    attack.playerEffectDuration = 1.25f;
                }
                else if (family == 1 && attackIndex == 2)
                {
                    attack.playerMovementMultiplier = .72f;
                    attack.playerEffectDuration = 1.4f;
                    attack.screenObscureStrength = .72f;
                }
                else if (family == 8 && attackIndex == 0)
                {
                    attack.playerMovementMultiplier = .46f;
                    attack.playerEffectDuration = 1.65f;
                }
                attacks[attackIndex] = attack;
            }
            return attacks;
        }

        private static MegaWeaponPattern BossPattern(int family, int attack)
        {
            MegaWeaponPattern[,] patterns =
            {
                { MegaWeaponPattern.Laser, MegaWeaponPattern.Mine, MegaWeaponPattern.AimedSingle },
                { MegaWeaponPattern.Mine, MegaWeaponPattern.AimedSingle, MegaWeaponPattern.Radial },
                { MegaWeaponPattern.Sniper, MegaWeaponPattern.Burst, MegaWeaponPattern.Radial },
                { MegaWeaponPattern.AimedSingle, MegaWeaponPattern.FixedSpread, MegaWeaponPattern.Carrier },
                { MegaWeaponPattern.AimedSingle, MegaWeaponPattern.FixedSpread, MegaWeaponPattern.Radial },
                { MegaWeaponPattern.Laser, MegaWeaponPattern.Carrier, MegaWeaponPattern.Mine },
                { MegaWeaponPattern.Burst, MegaWeaponPattern.AimedSingle, MegaWeaponPattern.FixedSpread },
                { MegaWeaponPattern.Radial, MegaWeaponPattern.Burst, MegaWeaponPattern.Radial },
                { MegaWeaponPattern.FixedSpread, MegaWeaponPattern.Carrier, MegaWeaponPattern.Laser },
                { MegaWeaponPattern.FixedSpread, MegaWeaponPattern.Burst, MegaWeaponPattern.Radial }
            };
            return patterns[family, attack];
        }

        private static MegaLevelData GenerateLevel(int sequenceIndex, EnemyShipData[][] allArmies, BossShipData familyBoss,
            SuperAnimalData[] animals, Sprite[] backgrounds, MegaVFXProfile vfx)
        {
            int family = sequenceIndex / 2;
            EnemyShipData[] army = allArmies[family];
            int gameLevel = (sequenceIndex + 1) * 5;
            bool hasBoss = gameLevel % 10 == 0;
            string path = $"{MegaShooterGenerator.DataRoot}/Levels/Mega_{sequenceIndex + 1:D2}_Level_{gameLevel:D3}.asset";
            MegaLevelData data = GetOrCreate<MegaLevelData>(path);
            data.gameLevelNumber = gameLevel;
            data.megaSequenceIndex = sequenceIndex + 1;
            data.displayTitle = hasBoss ? $"{Names[family]} — Showdown" : $"{Names[family]} — Army Assault";
            data.description = hasBoss
                ? $"Break the {Names[family]} army formations, then defeat {Species[family]} — {Names[family]}."
                : $"Break a mixed villain coalition led by {Species[family]} — {Names[family]}. No boss enters this mission.";
            data.introImage = army.Length > 1 ? army[1].sprite : familyBoss.sprite;
            data.backgroundSpeed = .45f + sequenceIndex * .018f;
            data.backgroundColor = Color.Lerp(new Color(.012f, .025f, .09f), Colors[family] * .18f, .46f);
            data.accentColor = Colors[family];
            data.startingHealth = 5;
            data.movementSpeedMultiplier = 1f;
            data.playerPowerMultiplier = 1f + sequenceIndex * .055f;
            data.invulnerabilityDuration = 1f;
            data.counterChargeMultiplier = 1f;
            data.cameraBounds = new Rect(-5f, -8.9f, 10f, 17.8f);
            data.playerMovementBounds = new Rect(-4.25f, -7.7f, 8.5f, 6.3f);
            data.safeUiMargins = new Vector4(.2f, 1.2f, .2f, 1.15f);
            data.scrollSpeed = .7f + sequenceIndex * .014f;
            data.bottomProjectileExclusion = 1.4f;
            data.enemyHealthMultiplier = .75f + sequenceIndex * .13f;
            data.enemyDamageMultiplier = sequenceIndex >= 8 ? 1.15f : 1f;
            data.enemyProjectileSpeedMultiplier = .65f + sequenceIndex * .045f;
            data.ordinaryEnemyFireInterval = Mathf.Max(.9f, 2.35f - sequenceIndex * .075f);
            data.enemyFireIntervalMultiplier = 1f;
            data.spawnCadenceMultiplier = Mathf.Max(.62f, 1f - sequenceIndex * .016f);
            data.maximumActiveEnemies = Mathf.Clamp(28 + sequenceIndex / 2, 28, 38);
            data.maximumHostileProjectiles = Mathf.Clamp(24 + sequenceIndex * 4, 24, 100);
            int targetCount = (hasBoss ? 36 : 42) + family * 3;
            data.targetEnemyCount = targetCount;
            data.waves = CreateWaves(sequenceIndex, allArmies, targetCount);
            data.boss = hasBoss ? familyBoss : null;
            data.bossWarningText = hasBoss ? $"WARNING — {Names[family].ToUpperInvariant()} APPROACHING" : string.Empty;
            data.bossOverrides.healthMultiplier = 1f + family * .035f;
            data.bossOverrides.attackIntervalMultiplier = 1f;
            data.bossOverrides.projectileSpeedMultiplier = 1f;
            data.bossOverrides.dropCriticalHealthPickup = hasBoss && (family == 4 || family == 9);
            data.scoreMultiplier = 1f + sequenceIndex * .045f;
            data.comboTimeout = 2f;
            data.nearMissScore = 25 + sequenceIndex * 2;
            data.counterScore = 250 + sequenceIndex * 15;
            data.parTime = 90f + family * 13f + (hasBoss ? 42f : 0f);
            data.coinReward = 110 + sequenceIndex * 25;
            data.arcadeTokenReward = 3 + family / 2;
            data.unlockReward = !hasBoss ? string.Empty : Names[family];
            data.vfxProfile = vfx;
            data.cameraShakeScale = 1f;
            data.flashScale = 1f;
            data.reducedEffectsCompatible = true;
            data.deterministicSeed = 5101 + sequenceIndex * 97;
            data.randomizeSeed = false;
            data.featuredAnimal = animals[sequenceIndex % animals.Length];
            data.allowedAnimals = new SuperAnimalData[animals.Length];
            Array.Copy(animals, data.allowedAnimals, animals.Length);
            if (backgrounds != null && backgrounds.Length > 0)
            {
                Sprite background = backgrounds[family % backgrounds.Length];
                data.backgroundLayers = new[] { background, background, background, background };
                data.backgroundLayerSpeeds = new[] { .12f, .24f, .42f, .68f };
            }
            EditorUtility.SetDirty(data);
            return data;
        }

        private static MegaWaveData[] CreateWaves(int sequenceIndex, EnemyShipData[][] allArmies, int targetCount)
        {
            int family = sequenceIndex / 2;
            int waveCount = Mathf.Clamp(3 + family / 3, 3, 6);
            int remaining = targetCount;
            var waves = new MegaWaveData[waveCount];
            for (int waveIndex = 0; waveIndex < waveCount; waveIndex++)
            {
                int waveTotal = Mathf.CeilToInt(remaining / (float)(waveCount - waveIndex));
                remaining -= waveTotal;
                int firstCount = Mathf.CeilToInt(waveTotal * .34f);
                int secondCount = Mathf.CeilToInt(waveTotal * .27f);
                int thirdCount = Mathf.CeilToInt(waveTotal * .22f);
                int fourthCount = waveTotal - firstCount - secondCount - thirdCount;
                int allyFamilyA = (family + 1 + waveIndex) % allArmies.Length;
                int allyFamilyB = (family + 3 + sequenceIndex + waveIndex) % allArmies.Length;
                int allyFamilyC = (family + 7 + waveIndex * 2) % allArmies.Length;
                var groups = new List<EnemySpawnGroup>
                {
                    CreateGroup(allArmies[family][waveIndex % 3], firstCount, waveIndex * 4, sequenceIndex, false)
                };
                if (secondCount > 0)
                    groups.Add(CreateGroup(allArmies[allyFamilyA][(waveIndex + 1) % 3], secondCount, waveIndex * 4 + 1, sequenceIndex, false));
                if (thirdCount > 0)
                    groups.Add(CreateGroup(allArmies[allyFamilyB][(waveIndex + 2) % 3], thirdCount, waveIndex * 4 + 2, sequenceIndex, false));
                if (fourthCount > 0)
                    groups.Add(CreateGroup(allArmies[allyFamilyC][waveIndex % 3], fourthCount, waveIndex * 4 + 3,
                        sequenceIndex, waveIndex == waveCount - 1));
                waves[waveIndex] = new MegaWaveData
                {
                    waveName = waveIndex == 0 ? "Vanguard Formation" : waveIndex == waveCount - 1 ? "Elite Army Finale" : "Reinforcement Wing",
                    waveNumber = waveIndex + 1,
                    startDelay = waveIndex == 0 ? 1f : .65f,
                    completionDelay = .85f,
                    warningBanner = waveIndex == 0 ? $"{Names[family].ToUpperInvariant()} ARMY INBOUND" :
                        waveIndex == waveCount - 1 ? "ELITE FORMATION — CLEAR THE SKY" : string.Empty,
                    spawnGroups = groups.ToArray(),
                    healthMultiplier = 1f + waveIndex * .035f,
                    speedMultiplier = 1f + waveIndex * .025f,
                    fireRateMultiplier = 1f,
                    scoreMultiplier = 1f + waveIndex * .07f,
                    maximumSimultaneousEnemies = Mathf.Clamp(28 + sequenceIndex / 2, 28, 38),
                    completionCondition = MegaWaveCompletion.DefeatAll,
                    environmentEvent = sequenceIndex >= 10 && waveIndex == 1 ? MegaEnvironmentEvent.TimeRift : MegaEnvironmentEvent.None
                };
            }
            return waves;
        }

        private static EnemySpawnGroup CreateGroup(EnemyShipData enemy, int count, int wave, int sequence, bool elite)
        {
            MegaFormationType[] formations = { MegaFormationType.V, MegaFormationType.Arc, MegaFormationType.Line,
                MegaFormationType.Grid, MegaFormationType.Mirrored, MegaFormationType.AlternatingSides };
            MegaFormationType formation = formations[(wave + sequence) % formations.Length];
            int columns = formation == MegaFormationType.Line ? Mathf.Clamp(count, 3, 9)
                : formation == MegaFormationType.Grid ? Mathf.Clamp(count, 3, 8)
                : Mathf.Clamp(count, 3, 7);
            return new EnemySpawnGroup
            {
                enemy = enemy,
                count = Mathf.Max(1, count),
                formation = formation,
                spawnPath = formation == MegaFormationType.AlternatingSides
                    ? (wave % 2 == 0 ? MegaSpawnPath.Left : MegaSpawnPath.Right)
                    : MegaSpawnPath.Top,
                cadence = Mathf.Max(.08f, .16f - sequence * .0025f),
                columns = columns,
                rows = Mathf.Max(1, Mathf.CeilToInt(count / (float)columns)),
                spacing = formation == MegaFormationType.Line ? .92f : .82f,
                normalizedEntry = .5f,
                eliteChance = sequence >= 8 ? Mathf.Min(.22f, .04f + sequence * .007f) : 0f,
                explicitElite = elite,
                priorityTarget = elite
            };
        }

        private static void CreateDistinctMuzzleVfx(MegaVFXProfile vfx, GameObject fallback)
        {
            if (vfx == null) return;
            vfx.playerMuzzlePrefab = GetOrCreateMuzzle("MegaPlayerMuzzle", "aurora_feather", new Color(.1f, .9f, 1f));
            vfx.enemyMuzzlePrefab = GetOrCreateMuzzle("MegaEnemyMuzzle", "missile", new Color(1f, .12f, .03f));
            vfx.bossMuzzlePrefab = GetOrCreateMuzzle("MegaBossMuzzle", "solar", new Color(1f, .42f, .04f));
            if (vfx.playerMuzzlePrefab == null) vfx.playerMuzzlePrefab = fallback;
            if (vfx.enemyMuzzlePrefab == null) vfx.enemyMuzzlePrefab = fallback;
            if (vfx.bossMuzzlePrefab == null) vfx.bossMuzzlePrefab = fallback;
            EditorUtility.SetDirty(vfx);
        }

        private static void NormalizeArmySheets()
        {
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            for (int family = 0; family < ArtFiles.Length; family++)
            {
                string path = $"{VillainArtRoot}/army/{ArtFiles[family]}_Army.png";
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = 300f;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
                provider.InitSpriteEditorDataProvider();
                SpriteRect[] existing = provider.GetSpriteRects();
                var rects = new SpriteRect[3];
                float cellWidth = width / 3f;
                for (int variant = 0; variant < rects.Length; variant++)
                {
                    string spriteName = $"{ArtFiles[family]}_Army_{variant + 1}";
                    SpriteRect preserved = existing.FirstOrDefault(rect => rect.name == spriteName);
                    GUID spriteId = preserved != null ? preserved.spriteID : variant < existing.Length ? existing[variant].spriteID : GUID.Generate();
                    rects[variant] = new SpriteRect
                    {
                        name = spriteName,
                        rect = new Rect(variant * cellWidth, 0f, cellWidth, height),
                        alignment = SpriteAlignment.Center,
                        pivot = new Vector2(.5f, .5f),
                        spriteID = spriteId
                    };
                }
                provider.SetSpriteRects(rects);
                provider.Apply();
                importer.SaveAndReimport();
            }
        }

        // The villain weapon sheet is a 5 x 2 atlas. Keeping its sprite IDs stable
        // repairs existing ProjectileData references whenever the sheet is reimported.
        private static void NormalizeVillainProjectileSheet()
        {
            TextureImporter importer = AssetImporter.GetAtPath(VillainProjectileSpriteSheetPath) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            if (width <= 0 || height <= 0) return;

            var factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            SpriteRect[] existing = provider.GetSpriteRects();
            const int columns = 5;
            const int rows = 2;
            var rects = new SpriteRect[columns * rows];
            float cellWidth = width / (float)columns;
            float cellHeight = height / (float)rows;

            for (int index = 0; index < rects.Length; index++)
            {
                string spriteName = $"villain_weapon_{index}";
                SpriteRect preserved = existing.FirstOrDefault(rect => rect.name == spriteName);
                GUID spriteId = preserved != null ? preserved.spriteID : GUID.Generate();
                int column = index % columns;
                int row = index / columns;
                rects[index] = new SpriteRect
                {
                    name = spriteName,
                    rect = new Rect(column * cellWidth, (rows - 1 - row) * cellHeight, cellWidth, cellHeight),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(.5f, .5f),
                    spriteID = spriteId
                };
            }

            provider.SetSpriteRects(rects);
            provider.Apply();
            importer.SaveAndReimport();
        }

        private static GameObject GetOrCreateMuzzle(string name, string spriteName, Color color)
        {
            string path = $"{MegaShooterGenerator.PrefabRoot}/{name}.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null && existing.GetComponent<MegaPoolMember>() != null &&
                existing.GetComponent<MegaTimedPoolEffect>() != null &&
                GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(existing) == 0)
                return existing;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MegaShooterGenerator.ArtRoot}/Projectiles/{spriteName}.png");
            GameObject go = new GameObject(name);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 16;
            renderer.color = color;
            go.AddComponent<MegaPoolMember>();
            go.AddComponent<MegaTimedPoolEffect>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        private static Sprite[] LoadAllSprites(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                .OrderBy(sprite => SpriteIndex(sprite.name)).ThenBy(sprite => sprite.rect.x).ToArray();
        }

        private static int SpriteIndex(string name)
        {
            int separator = name.LastIndexOf('_');
            return separator >= 0 && int.TryParse(name.Substring(separator + 1), out int index) ? index : 0;
        }

        private static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolders()
        {
            EnsureFolder(VillainDataRoot);
            EnsureFolder(VillainDataRoot + "/Projectiles");
            EnsureFolder(VillainDataRoot + "/Armies");
            EnsureFolder(VillainDataRoot + "/Bosses");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            string name = path.Substring(path.LastIndexOf('/') + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
