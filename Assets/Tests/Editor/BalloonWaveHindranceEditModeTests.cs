using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using AnimalFall.Core.Hindrances;
using AnimalFall.Data;

namespace AnimalFall.Tests.Editor
{
    public sealed class BalloonWaveHindranceEditModeTests
    {
        [Test]
        public void BalloonWaveUsesTheCompleteBalloonSpritesheetAndIsScheduledOnNormalLevels()
        {
            HindranceRegistry registry = AssetDatabase.LoadAssetAtPath<HindranceRegistry>(
                "Assets/Resources/Hindrances/HindranceRegistry.asset");
            HindranceData definition = registry.GetData(HindranceType.BalloonWave);
            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.stateSprites, Has.Length.EqualTo(5));
            Assert.That(definition.stateSprites[0].name, Is.EqualTo("balloon_0"));
            Assert.That(definition.stateSprites[4].name, Is.EqualTo("balloon_4"));
            Assert.That(definition.prefab.GetComponent<AnimalFall.Core.Hindrances.EnvironmentMods.BalloonWaveHindrance>(),
                Is.Not.Null);

            foreach (int number in new[] { 81, 82, 83, 84 })
            {
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>($"Assets/Levels/LevelData/Level_{number:D2}.asset");
                Assert.That(level.Hindrances, Has.Some.Matches<HindranceConfig>(config => config.type == HindranceType.BalloonWave));
            }
        }
    }
}
