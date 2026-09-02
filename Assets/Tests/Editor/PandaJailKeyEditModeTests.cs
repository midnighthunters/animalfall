#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using AnimalFall.Core.Hindrances;
using AnimalFall.Core.Hindrances.New;
using AnimalFall.Data;

namespace AnimalFall.Tests.Editor
{
    public sealed class PandaJailKeyEditModeTests
    {
        [Test]
        public void PandaJailKeyIsRegisteredAndScheduledForLevelsSixtyThreeToSixtySeven()
        {
            HindranceRegistry registry = AssetDatabase.LoadAssetAtPath<HindranceRegistry>(
                "Assets/Resources/Hindrances/HindranceRegistry.asset");
            HindranceData definition = registry.GetData(HindranceType.PandaJailKey);

            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.unlockLevel, Is.EqualTo(63));
            Assert.That(definition.prefab.GetComponent<PandaJailKeyHindrance>(), Is.Not.Null);
            Assert.That(definition.tutorialInstruction, Does.Contain("key"));

            foreach (int levelNumber in new[] { 63, 64, 66, 67 })
            {
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(
                    $"Assets/Levels/LevelData/Level_{levelNumber:D2}.asset");
                Assert.That(level.Hindrances.Any(config => config.type == HindranceType.PandaJailKey), Is.True,
                    $"Level {levelNumber} should include Panda Jail & Key.");
            }

            LevelData megaLevel = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Levels/LevelData/Level_65.asset");
            Assert.That(megaLevel.Hindrances.Any(config => config.type == HindranceType.PandaJailKey), Is.False);
        }
    }
}
#endif
