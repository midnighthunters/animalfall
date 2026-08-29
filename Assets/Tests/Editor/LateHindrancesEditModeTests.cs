#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using AnimalFall.Core.Hindrances;
using AnimalFall.Core.Hindrances.New;
using AnimalFall.Data;

namespace AnimalFall.Tests.Editor
{
    public sealed class LateHindrancesEditModeTests
    {
        private const string RegistryPath =
            "Assets/Resources/Hindrances/HindranceRegistry.asset";

        [Test]
        public void SuppliedSpriteStatesDriveDedicatedPrefabs()
        {
            HindranceRegistry registry =
                AssetDatabase.LoadAssetAtPath<HindranceRegistry>(RegistryPath);

            HindranceData mushroom = registry.GetData(HindranceType.SpringMushroomBumpers);
            Assert.That(mushroom.prefab.GetComponent<SpringMushroomHindrance>(), Is.Not.Null);
            CollectionAssert.AreEquivalent(new[] { "pressed", "stretched" },
                mushroom.stateSprites.Select(sprite => sprite.name));
            Assert.That(mushroom.prefab.GetComponent<CircleCollider2D>().isTrigger, Is.True);

            HindranceData porcupine = registry.GetData(HindranceType.PorcupinePulse);
            Assert.That(porcupine.prefab.GetComponent<PorcupineHindrance>(), Is.Not.Null);
            CollectionAssert.AreEquivalent(new[] { "porcupine_0", "porcupine_1", "porcupine_2" },
                porcupine.stateSprites.Select(sprite => sprite.name));
            Assert.That(porcupine.prefab.GetComponent<IPointerTapTarget>(), Is.Not.Null);

            HindranceData thorn = registry.GetData(HindranceType.VenusFlytrapRescue);
            Assert.That(thorn.displayName, Is.EqualTo("Thorn Plant"));
            Assert.That(thorn.prefab.GetComponent<ThornPlantHindrance>(), Is.Not.Null);
            CollectionAssert.AreEquivalent(new[] { "base", "close", "open", "stem" },
                thorn.stateSprites.Select(sprite => sprite.name));
            Assert.That(thorn.prefab.transform.Find("Base"), Is.Not.Null);
            Assert.That(thorn.prefab.transform.Find("Stem"), Is.Not.Null);
            Assert.That(thorn.prefab.transform.Find("Flower"), Is.Not.Null);
        }

        [Test]
        public void LevelsTwentySixThroughThirtyNineMixNewAndReturningHindrances()
        {
            int[] normalLevels = { 26, 27, 28, 29, 31, 32, 33, 34, 36, 37, 38, 39 };
            foreach (int number in normalLevels)
            {
                LevelData level = LoadLevel(number);
                Assert.That(level.Hindrances, Has.Length.EqualTo(3), level.name);
                Assert.That(level.Hindrances.Any(config => (int)config.type <= 20), Is.True,
                    level.name + " must bring back an earlier hindrance");
                Assert.That(level.MaxHindrancesActive, Is.EqualTo(2), level.name);
            }

            Assert.That(LoadLevel(26).Hindrances[0].type,
                Is.EqualTo(HindranceType.SpringMushroomBumpers));
            Assert.That(LoadLevel(31).Hindrances[0].type,
                Is.EqualTo(HindranceType.PorcupinePulse));
            Assert.That(LoadLevel(36).Hindrances[0].type,
                Is.EqualTo(HindranceType.VenusFlytrapRescue));

            Assert.That(LoadLevel(30).Hindrances, Is.Empty);
            Assert.That(LoadLevel(35).Hindrances, Is.Empty);
        }

        private static LevelData LoadLevel(int number)
        {
            LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(
                $"Assets/Levels/LevelData/Level_{number:D2}.asset");
            Assert.That(level, Is.Not.Null);
            return level;
        }
    }
}
#endif
