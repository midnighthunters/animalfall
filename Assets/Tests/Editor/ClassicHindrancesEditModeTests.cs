#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using AnimalFall.Core.Hindrances;
using AnimalFall.Core.Hindrances.EnvironmentMods;
using AnimalFall.Core.Hindrances.Penalties;
using AnimalFall.Core.Hindrances.TapModifiers;
using AnimalFall.Data;

namespace AnimalFall.Tests.Editor
{
    public sealed class ClassicHindrancesEditModeTests
    {
        private const string RegistryPath = "Assets/Resources/Hindrances/HindranceRegistry.asset";

        [Test]
        public void ClassicDefinitionsUseRequestedRuntimeBehaviours()
        {
            HindranceRegistry registry = AssetDatabase.LoadAssetAtPath<HindranceRegistry>(RegistryPath);
            Assert.That(registry, Is.Not.Null);
            Assert.That(registry.GetData(HindranceType.Jellyfish).prefab.GetComponent<JellyfishHindrance>(), Is.Not.Null);
            Assert.That(registry.GetData(HindranceType.Laser).prefab.GetComponent<LaserHindrance>(), Is.Not.Null);
            Assert.That(registry.GetData(HindranceType.Eagle).prefab.GetComponent<EagleHindrance>(), Is.Not.Null);
            Assert.That(registry.GetData(HindranceType.WoodenPig).prefab.GetComponent<WoodenPigHindrance>(), Is.Not.Null);
            Assert.That(registry.GetData(HindranceType.Tornado).prefab.GetComponent<TornadoHindrance>(), Is.Not.Null);
            Assert.That(registry.GetData(HindranceType.Portal).prefab.GetComponent<PortalHindrance>(), Is.Not.Null);
            Assert.That(registry.GetData(HindranceType.Fan).prefab.GetComponent<FanHindrance>(), Is.Not.Null);
            Assert.That(registry.GetData(HindranceType.BatSwarm).prefab.GetComponent<BatSwarmHindrance>(), Is.Not.Null);
            Assert.That(registry.GetData(HindranceType.WoodenPig).prefab.GetComponent<IPointerTapTarget>(), Is.Not.Null);
        }

        [Test]
        public void AnimatedClassicHindrancesUseCompleteSpritesheets()
        {
            HindranceRegistry registry = AssetDatabase.LoadAssetAtPath<HindranceRegistry>(RegistryPath);
            Assert.That(registry.GetData(HindranceType.Tornado).stateSprites, Has.Length.EqualTo(12));
            Assert.That(registry.GetData(HindranceType.Fan).stateSprites, Has.Length.EqualTo(8));
            Assert.That(registry.GetData(HindranceType.Portal).stateSprites, Has.Length.EqualTo(2));
            Assert.That(registry.GetData(HindranceType.BatSwarm).stateSprites, Has.Length.EqualTo(5));
            CollectionAssert.AreEqual(new[] { "bat_0", "bat_1", "bat_2", "bat_3", "bat_4" },
                registry.GetData(HindranceType.BatSwarm).stateSprites.Select(sprite => sprite.name));
        }

        [Test]
        public void BatSwarmCreatesTenToTwelveCarriers()
        {
            HindranceRegistry registry = AssetDatabase.LoadAssetAtPath<HindranceRegistry>(RegistryPath);
            BatSwarmHindrance swarm = registry.GetData(HindranceType.BatSwarm).prefab
                .GetComponent<BatSwarmHindrance>();
            var serialized = new SerializedObject(swarm);
            Assert.That(serialized.FindProperty("_minimumBats").intValue, Is.EqualTo(10));
            Assert.That(serialized.FindProperty("_maximumBats").intValue, Is.EqualTo(12));
        }

        [Test]
        public void ClassicHindrancesAreScheduledOnlyOnNormalLevelsFortyToOneHundred()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { "Assets/Levels/LevelData" });
            var seen = new HashSet<HindranceType>();
            foreach (string guid in guids)
            {
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid));
                HindranceType[] classics = (level.Hindrances ?? System.Array.Empty<HindranceConfig>())
                    .Where(config => config != null && LateClassicHindranceSchedule.IsLateClassicType(config.type))
                    .Select(config => config.type).ToArray();
                if (classics.Length > 0)
                {
                    Assert.That(level.LevelNumber, Is.InRange(40, 100), level.name);
                    Assert.That(level.IsMegaLevel || level.IsConfiguredMegaShooter, Is.False, level.name);
                    foreach (HindranceType type in classics) seen.Add(type);
                }

                bool expected = level.LevelNumber >= 40 && level.LevelNumber <= 100 &&
                    !level.IsMegaLevel && !level.IsConfiguredMegaShooter;
                if (expected) Assert.That(classics, Is.Not.Empty, level.name);
            }
            CollectionAssert.AreEquivalent(LateClassicHindranceSchedule.Types, seen);
        }
    }
}
#endif
