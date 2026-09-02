using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using AnimalFall.Core.Hindrances;
using AnimalFall.Data;

namespace AnimalFall.Tests.Editor
{
    public sealed class GravitySwitchHindranceEditModeTests
    {
        [Test]
        public void GravitySwitchUsesClosedAndOpenSwitchSpritesAndIsScheduledOnNormalLevels()
        {
            HindranceRegistry registry = AssetDatabase.LoadAssetAtPath<HindranceRegistry>(
                "Assets/Resources/Hindrances/HindranceRegistry.asset");
            HindranceData definition = registry.GetData(HindranceType.GravitySwitch);
            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.stateSprites, Has.Length.EqualTo(2));
            Assert.That(definition.stateSprites[0].name, Is.EqualTo("switch_0"));
            Assert.That(definition.stateSprites[1].name, Is.EqualTo("switch_1"));
            Assert.That(definition.prefab.GetComponent<AnimalFall.Core.Hindrances.EnvironmentMods.GravitySwitchHindrance>(),
                Is.Not.Null);

            foreach (int number in new[] { 73, 74, 76, 77 })
            {
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>($"Assets/Levels/LevelData/Level_{number:D2}.asset");
                Assert.That(level.Hindrances, Has.Some.Matches<HindranceConfig>(config => config.type == HindranceType.GravitySwitch));
            }
        }
    }
}
