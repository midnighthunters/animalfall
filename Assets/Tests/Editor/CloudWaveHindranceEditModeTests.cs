using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using AnimalFall.Core.Hindrances;
using AnimalFall.Data;

namespace AnimalFall.Tests.Editor
{
    public sealed class CloudWaveHindranceEditModeTests
    {
        [Test]
        public void CloudWaveUsesTheCloudSpriteAndIsScheduledOnFourNormalLevels()
        {
            HindranceRegistry registry = AssetDatabase.LoadAssetAtPath<HindranceRegistry>(
                "Assets/Resources/Hindrances/HindranceRegistry.asset");
            Assert.That(registry, Is.Not.Null);
            HindranceData definition = registry.GetData(HindranceType.CloudWave);
            Assert.That(definition, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(definition.icon),
                Is.EqualTo("Assets/Resources/icons/hindrances/cloud.png"));
            var cloudWave = definition.prefab.GetComponent<AnimalFall.Core.Hindrances.EnvironmentMods.CloudWaveHindrance>();
            Assert.That(cloudWave, Is.Not.Null);
            var serialized = new SerializedObject(cloudWave);
            Assert.That(serialized.FindProperty("_cloudCount").intValue, Is.GreaterThanOrEqualTo(10));
            Assert.That(serialized.FindProperty("_cloudWorldSize").floatValue, Is.EqualTo(1.65f).Within(0.001f));

            foreach (int number in new[] { 91, 92, 93, 94 })
            {
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>($"Assets/Levels/LevelData/Level_{number:D2}.asset");
                Assert.That(level, Is.Not.Null);
                Assert.That(level.Hindrances, Has.Some.Matches<HindranceConfig>(config => config.type == HindranceType.CloudWave));
            }
        }
    }
}
