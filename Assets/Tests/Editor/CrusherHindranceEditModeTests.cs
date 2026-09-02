using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using AnimalFall.Core.Hindrances;
using AnimalFall.Data;

namespace AnimalFall.Tests.Editor
{
    public sealed class CrusherHindranceEditModeTests
    {
        [Test]
        public void CrusherUsesTheCrusherSpriteAndIsScheduledOnItsNormalLevels()
        {
            HindranceRegistry registry = AssetDatabase.LoadAssetAtPath<HindranceRegistry>(
                "Assets/Resources/Hindrances/HindranceRegistry.asset");
            HindranceData definition = registry.GetData(HindranceType.Crusher);
            Assert.That(definition, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(definition.icon),
                Is.EqualTo("Assets/Resources/icons/hindrances/crusher.png"));
            Assert.That(definition.prefab.GetComponent<AnimalFall.Core.Hindrances.EnvironmentMods.CrusherHindrance>(),
                Is.Not.Null);
            var serialized = new SerializedObject(definition.prefab.GetComponent<AnimalFall.Core.Hindrances.EnvironmentMods.CrusherHindrance>());
            Assert.That(serialized.FindProperty("_crusherWorldSize").floatValue, Is.EqualTo(1.75f).Within(0.001f));

            foreach (int number in new[] { 68, 69, 71, 72 })
            {
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>($"Assets/Levels/LevelData/Level_{number:D2}.asset");
                Assert.That(level.Hindrances, Has.Some.Matches<HindranceConfig>(config => config.type == HindranceType.Crusher));
            }
        }
    }
}
