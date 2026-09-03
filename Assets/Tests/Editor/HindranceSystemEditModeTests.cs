#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using AnimalFall.Core.Hindrances;
using AnimalFall.Data;
using AnimalFall.Effects;

namespace AnimalFall.Tests.Editor
{
    public sealed class HindranceSystemEditModeTests
    {
        private const string RegistryPath = "Assets/Resources/Hindrances/HindranceRegistry.asset";

        [Test]
        public void PersistedEnumIdsRemainStableAndNewIdsAreContiguous()
        {
            string[] expected =
            {
                "None", "Bomb", "AlarmClock", "PoisonVial", "ThiefBird", "KnightHelmet",
                "BubbleShield", "IceCube", "GhostAnimal", "InkSquid", "StormCloud", "Flashbang",
                "FallingLeaves", "WindGust", "ZeroGravity", "BlackHole", "Tornado", "MagnetTrap",
                "MirrorMode", "CursedSkull", "PairedAnimal", "SpiderwebCurtain", "FireflyLockAndKey",
                "RhythmTotem", "TrafficLightOwl", "TrackingRescueCage", "LassoRing", "EchoTapRune",
                "NumberedFlock", "MovingSafeHalo", "KeepersWhistle", "SpringMushroomBumpers",
                "ConveyorClouds", "CrumblingPerches", "PendulumVines", "SeesawBranch", "CarouselNests",
                "TrapdoorClouds", "RollingLog", "AcornHail", "WindmillGate", "LanternSpotlight",
                "EclipseSilhouettes", "MemoryFog", "ColourWashRain", "TimerMoth", "GoalSwapMonkey",
                "BeeSwarmGuard", "PorcupinePulse", "VenusFlytrapRescue", "RaccoonCoinHeist",
                "DogHelmet", "Octopus", "SpiderGun", "Pufferfish", "FrogSnatcher",
                "Jellyfish", "Laser", "Eagle", "WoodenPig", "Portal", "Fan", "BatSwarm",
                "PandaJailKey", "Crusher", "GravitySwitch", "BalloonWave", "SlimeGun", "CloudWave"
            };

            Assert.That(Enum.GetValues(typeof(HindranceType)).Length, Is.EqualTo(69));
            for (int id = 0; id <= 68; id++)
            {
                Assert.That(Enum.GetName(typeof(HindranceType), id), Is.EqualTo(expected[id]), $"Stable ID {id}");
                Assert.That((int)(HindranceType)id, Is.EqualTo(id));
            }
        }

        [Test]
        public void LevelTwentyOneUsesRequestedAnimalsAndFrogSnatcher()
        {
            LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(
                "Assets/Levels/LevelData/Level_21.asset");
            Assert.That(level, Is.Not.Null);
            CollectionAssert.AreEqual(new[]
            {
                AnimalFall.Core.Animals.AnimalSpecies.Dog,
                AnimalFall.Core.Animals.AnimalSpecies.Pig,
                AnimalFall.Core.Animals.AnimalSpecies.Monkey,
                AnimalFall.Core.Animals.AnimalSpecies.Raccoon
            }, level.SpawnPool.Select(animal => animal.species).ToArray());
            CollectionAssert.AreEqual(new[]
            {
                AnimalFall.Core.Animals.AnimalSpecies.Dog,
                AnimalFall.Core.Animals.AnimalSpecies.Pig,
                AnimalFall.Core.Animals.AnimalSpecies.Monkey,
                AnimalFall.Core.Animals.AnimalSpecies.Raccoon
            }, level.Goal.Targets.Select(target => target.species).ToArray());
            Assert.That(level.Hindrances.Length, Is.EqualTo(1));
            Assert.That(level.Hindrances[0].type, Is.EqualTo(HindranceType.FrogSnatcher));

            HindranceRegistry registry = AssetDatabase.LoadAssetAtPath<HindranceRegistry>(RegistryPath);
            HindranceData frog = registry.GetData(HindranceType.FrogSnatcher);
            Assert.That(frog, Is.Not.Null);
            Assert.That(frog.prefab.GetComponent<AnimalFall.Core.Hindrances.New.FrogSnatcherHindrance>(),
                Is.Not.Null);
            Assert.That(frog.stateSprites, Has.Length.EqualTo(3));
        }

        [Test]
        public void LevelFourteenSchedulesVisiblePufferfishEveryTenSeconds()
        {
            LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(
                "Assets/Levels/LevelData/Level_14.asset");
            Assert.That(level, Is.Not.Null);
            Assert.That(level.Hindrances, Has.Length.EqualTo(1));
            Assert.That(level.Hindrances[0].type, Is.EqualTo(HindranceType.Pufferfish));
            Assert.That(level.HindranceInitialDelay, Is.EqualTo(10f));
            Assert.That(level.HindranceSpawnInterval, Is.EqualTo(10f));
            Assert.That(level.MaxHindrancesActive, Is.EqualTo(1));

            HindranceRegistry registry = AssetDatabase.LoadAssetAtPath<HindranceRegistry>(RegistryPath);
            HindranceData definition = registry.GetData(HindranceType.Pufferfish);
            var pufferfish = definition.prefab.GetComponent<
                AnimalFall.Core.Hindrances.EnvironmentMods.PufferfishHindrance>();
            Assert.That(pufferfish, Is.Not.Null);

            var serialized = new SerializedObject(pufferfish);
            Assert.That(serialized.FindProperty("_visibleLifetime").floatValue, Is.LessThan(10f));
            Assert.That(serialized.FindProperty("_viewportHeight").floatValue,
                Is.InRange(0.1f, 1f));
            Assert.That(serialized.FindProperty("_fallSpeed").floatValue, Is.GreaterThan(0f));
            Assert.That(serialized.FindProperty("_deflatedScaleMultiplier").floatValue,
                Is.LessThan(1f));

            SpriteRenderer renderer = definition.prefab.GetComponent<SpriteRenderer>();
            CircleCollider2D collider = definition.prefab.GetComponent<CircleCollider2D>();
            Assert.That(renderer, Is.Not.Null);
            Assert.That(collider, Is.Not.Null);
            Assert.That(collider.radius,
                Is.GreaterThanOrEqualTo(Mathf.Max(renderer.sprite.bounds.size.x,
                    renderer.sprite.bounds.size.y) * 0.5f));
        }

        [Test]
        public void RegistryContainsFiftyProductionReadyDefinitions()
        {
            HindranceRegistry registry = AssetDatabase.LoadAssetAtPath<HindranceRegistry>(RegistryPath);
            Assert.That(registry, Is.Not.Null);
            CollectionAssert.IsEmpty(registry.ValidateRegistry(true));
            Assert.That(registry.Entries.Count, Is.GreaterThanOrEqualTo(62));

            for (int id = 1; id <= 50; id++)
            {
                HindranceData data = registry.GetData((HindranceType)id);
                Assert.That(data, Is.Not.Null, $"ID {id}");
                Assert.That(data.prefab, Is.Not.Null, data.name);
                Assert.That(data.prefab.GetComponent<IHindrance>(), Is.Not.Null, data.name);
                Assert.That(data.icon, Is.Not.Null, data.name);
                Assert.That(AssetDatabase.GetAssetPath(data.icon), Does.StartWith("Assets/Resources/icons/hindrances/Sheets/hindrance_"), data.name);
                Assert.That(data.tutorialInstruction, Is.Not.Null.And.Not.Empty, data.name);
                Assert.That(data.stateSprites, Is.Not.Null.And.Not.Empty, data.name);
                Assert.That(data.normalLevelEligible, Is.True, data.name);
                Assert.That(data.megaLevelEligible, Is.False, data.name);
            }
        }

        [Test]
        public void RuntimeSheetsImportAsNamedFourByFourSpriteGrids()
        {
            string[] sheets =
            {
                "hindrance_icons_current_01.png", "hindrance_icons_current_02.png",
                "hindrance_icons_01.png", "hindrance_icons_02.png", "hindrance_interactions.png",
                "hindrance_physics_props.png", "hindrance_dynamic_states.png", "hindrance_vfx.png"
            };

            foreach (string sheet in sheets)
            {
                string path = "Assets/Resources/icons/hindrances/Sheets/" + sheet;
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.That(texture, Is.Not.Null, path);
                Assert.That(texture.width, Is.EqualTo(2048), path);
                Assert.That(texture.height, Is.EqualTo(2048), path);
                Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
                Assert.That(sprites.Length, Is.EqualTo(16), path);
                Assert.That(sprites.Select(sprite => sprite.name).Distinct().Count(), Is.EqualTo(16), path);
            }
        }

        [Test]
        public void CuratedPoolsKeepMegaLevelsEmptyAndIntroduceNewTypesAlone()
        {
            LevelData[] levels = AssetDatabase.FindAssets("t:LevelData", new[] { "Assets/Levels" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<LevelData>)
                .Where(level => level != null && level.LevelNumber > 0)
                .OrderBy(level => level.LevelNumber)
                .ToArray();
            Assert.That(levels, Is.Not.Empty);

            foreach (LevelData mega in levels.Where(level => level.IsMegaLevel && !level.AllowNormalHindrancesInMegaLevel))
                Assert.That(mega.Hindrances, Is.Empty, mega.name);

            // Introductory solo set-pieces (such as Level 21 FrogSnatcher) must debut in an isolated normal level
            HindranceType[] soloDebuts = { HindranceType.FrogSnatcher };
            foreach (HindranceType type in soloDebuts)
            {
                LevelData first = levels.FirstOrDefault(level => level.Hindrances != null && level.Hindrances.Any(config => config.type == type));
                Assert.That(first, Is.Not.Null, string.Format("{0} needs a normal-level first encounter", type));
                Assert.That(first.IsMegaLevel, Is.False, first.name);
                Assert.That(first.Hindrances.Length, Is.EqualTo(1), string.Format("{0} first encounter must be isolated", type));
            }
        }

        [Test]
        public void AnimalHindranceFlowsAreIsolatedAcrossDistinctLevels()
        {
            HindranceType[] flows =
            {
                HindranceType.Octopus, HindranceType.SpiderGun, HindranceType.Pufferfish
            };

            HindranceRegistry registry = AssetDatabase.LoadAssetAtPath<HindranceRegistry>(RegistryPath);
            Assert.That(registry, Is.Not.Null);

            LevelData[] levels = AssetDatabase.FindAssets("t:LevelData", new[] { "Assets/Levels" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<LevelData>)
                .Where(level => level != null && level.LevelNumber > 0)
                .OrderBy(level => level.LevelNumber)
                .ToArray();
            Assert.That(levels, Is.Not.Empty);

            var firstEncounters = new Dictionary<HindranceType, int>();
            foreach (HindranceType flow in flows)
            {
                // Flow must be a real, spawnable, tappable definition.
                HindranceData data = registry.GetData(flow);
                Assert.That(data, Is.Not.Null, $"{flow} missing registry definition");
                Assert.That(data.prefab, Is.Not.Null, $"{flow} missing prefab");
                Assert.That(data.prefab.GetComponent<IPointerTapTarget>(), Is.Not.Null, $"{flow} prefab is not tappable");
                Assert.That(data.normalLevelEligible, Is.True, $"{flow} must be normal-level eligible");

                LevelData first = levels.FirstOrDefault(level =>
                    level.Hindrances != null && level.Hindrances.Any(config => config.type == flow));
                Assert.That(first, Is.Not.Null, $"{flow} needs a normal-level first encounter");
                Assert.That(first.IsMegaLevel, Is.False, $"{flow} first encounter cannot be a Mega level");
                Assert.That(first.Hindrances.Length, Is.EqualTo(1), $"{flow} first encounter must be isolated");
                firstEncounters[flow] = first.LevelNumber;
            }

            // Each flow debuts in a different level.
            Assert.That(firstEncounters.Values.Distinct().Count(), Is.EqualTo(flows.Length),
                "Octopus, Spider Gun and Pufferfish must debut in different levels");

            // Do not overclutter: never stack all three animal flows in one level,
            // and any level using them keeps a small simultaneous-active cap.
            foreach (LevelData level in levels)
            {
                if (level.Hindrances == null) continue;
                int flowCount = level.Hindrances.Count(config => flows.Contains(config.type));
                Assert.That(flowCount, Is.LessThanOrEqualTo(2), $"{level.name} stacks too many animal flows ({flowCount})");
                if (flowCount > 0)
                    Assert.That(level.MaxHindrancesActive, Is.LessThanOrEqualTo(2),
                        $"{level.name} allows too many simultaneous hindrances");
            }
        }

        [Test]
        public void CompatibilityGroupsAreMutuallyExclusiveByTag()
        {
            HindranceRegistry registry = AssetDatabase.LoadAssetAtPath<HindranceRegistry>(RegistryPath);
            AssertSameTag(registry, HindranceCompatibilityTag.InputTransform,
                HindranceType.MirrorMode, HindranceType.MagnetTrap, HindranceType.EchoTapRune);
            AssertSameTag(registry, HindranceCompatibilityTag.FullScreenVisibility,
                HindranceType.InkSquid, HindranceType.StormCloud, HindranceType.Flashbang,
                HindranceType.LanternSpotlight, HindranceType.EclipseSilhouettes, HindranceType.MemoryFog);
            AssertSameTag(registry, HindranceCompatibilityTag.GlobalMotion,
                HindranceType.WindGust, HindranceType.ZeroGravity, HindranceType.BlackHole);
        }

        [Test]
        public void NonAnimalInteractionPrefabsUseUnifiedPointerTargets()
        {
            HindranceRegistry registry = AssetDatabase.LoadAssetAtPath<HindranceRegistry>(RegistryPath);
            HindranceType[] directTargets =
            {
                HindranceType.Bomb, HindranceType.PoisonVial, HindranceType.CursedSkull,
                HindranceType.TimerMoth, HindranceType.RaccoonCoinHeist, HindranceType.RhythmTotem,
                HindranceType.EchoTapRune, HindranceType.SpringMushroomBumpers, HindranceType.RollingLog,
                HindranceType.AcornHail, HindranceType.WindmillGate
            };
            foreach (HindranceType type in directTargets)
                Assert.That(registry.GetData(type).prefab.GetComponent<IPointerTapTarget>(), Is.Not.Null, type.ToString());
        }

        [Test]
        public void ScopedEnvironmentTokensComposeAndReleaseIdempotently()
        {
            GameObject root = new GameObject("EnvironmentEffects_Test");
            EnvironmentEffects effects = root.AddComponent<EnvironmentEffects>();
            object firstOwner = new object();
            object secondOwner = new object();
            HindranceEffectToken first = effects.AddWind(firstOwner, new Vector2(1f, 0f));
            HindranceEffectToken second = effects.AddWind(secondOwner, new Vector2(0f, 2f));
            HindranceEffectToken gravity = effects.AddZeroGravity(firstOwner);

            Assert.That(effects.WindForce, Is.EqualTo(new Vector2(1f, 2f)));
            Assert.That(effects.IsZeroGravityActive, Is.True);
            first.Dispose(); first.Dispose();
            Assert.That(effects.WindForce, Is.EqualTo(new Vector2(0f, 2f)));
            gravity.Dispose();
            Assert.That(effects.IsZeroGravityActive, Is.False);
            second.Dispose();
            Assert.That(effects.WindForce, Is.EqualTo(Vector2.zero));
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void AssertSameTag(HindranceRegistry registry, HindranceCompatibilityTag tag, params HindranceType[] types)
        {
            foreach (HindranceType type in types)
            {
                HindranceData data = registry.GetData(type);
                Assert.That(data.compatibilityTags.HasFlag(tag), Is.True, type.ToString());
                Assert.That(data.exclusionTags.HasFlag(tag), Is.True, type.ToString());
            }
        }
    }
}
#endif
