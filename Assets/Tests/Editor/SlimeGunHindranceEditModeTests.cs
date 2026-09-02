using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using AnimalFall.Core.Hindrances;
using AnimalFall.Data;

namespace AnimalFall.Tests.Editor
{
    public sealed class SlimeGunHindranceEditModeTests
    {
        [Test]
        public void SlimeGunUsesTheRequestedSpriteAndIsScheduledOnFourNormalLevels()
        {
            HindranceRegistry registry = AssetDatabase.LoadAssetAtPath<HindranceRegistry>(
                "Assets/Resources/Hindrances/HindranceRegistry.asset");
            HindranceData definition = registry.GetData(HindranceType.SlimeGun);
            Assert.That(definition, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(definition.icon),
                Is.EqualTo("Assets/Resources/icons/hindrances/slime_gun.png"));
            Assert.That(definition.prefab.GetComponent<AnimalFall.Core.Hindrances.EnvironmentMods.SlimeGunHindrance>(),
                Is.Not.Null);

            foreach (int number in new[] { 86, 87, 88, 89 })
            {
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>($"Assets/Levels/LevelData/Level_{number:D2}.asset");
                Assert.That(level.Hindrances, Has.Some.Matches<HindranceConfig>(config => config.type == HindranceType.SlimeGun));
            }
        }
    }
}
