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
                "BeeSwarmGuard", "PorcupinePulse", "VenusFlytrapRescue", "RaccoonCoinHeist"
            };

            Assert.That(Enum.GetValues(typeof(HindranceType)).Length, Is.EqualTo(51));
            for (int id = 0; id <= 50; id++)
            {
                Assert.That(Enum.GetName(typeof(HindranceType), id), Is.EqualTo(expected[id]), $"Stable ID {id}");
                Assert.That((int)(HindranceType)id, Is.EqualTo(id));
            }
        }

        [Test]
        public void RegistryContainsFiftyProductionReadyDefinitions()
        {
            HindranceRegistry registry = AssetDatabase.LoadAssetAtPath<HindranceRegistry>(RegistryPath);
            Assert.That(registry, Is.Not.Null);
            CollectionAssert.IsEmpty(registry.ValidateRegistry(true));
            Assert.That(registry.Entries.Count, Is.EqualTo(50));

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

            for (int id = 21; id <= 50; id++)
            {
                HindranceType type = (HindranceType)id;
                LevelData first = levels.FirstOrDefault(level => level.Hindrances != null && level.Hindrances.Any(config => config.type == type));
                Assert.That(first, Is.Not.Null, $"{type} needs a normal-level first encounter");
                Assert.That(first.IsMegaLevel, Is.False, first.name);
                Assert.That(first.Hindrances.Length, Is.EqualTo(1), $"{type} first encounter must be isolated");
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
