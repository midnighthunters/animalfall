#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using AnimalFall.Data;
using AnimalFall.Debugging;
using AnimalFall.Managers;

namespace AnimalFall.Tests.Editor
{
    public sealed class LevelJumpControllerEditModeTests
    {
        private LevelDatabase _database;

        [SetUp]
        public void SetUp()
        {
            _database = AssetDatabase.LoadAssetAtPath<LevelDatabase>("Assets/Levels/LevelDatabase.asset");
            Assert.That(_database, Is.Not.Null);
        }

        [Test]
        public void ResolvesConfiguredNormalAndMegaLevelsByOneBasedNumber()
        {
            GameObject go = new GameObject("LevelJumpTest");
            try
            {
                LevelJumpController controller = go.AddComponent<LevelJumpController>();
                SerializedObject serialized = new SerializedObject(controller);
                serialized.FindProperty("_levelDatabase").objectReferenceValue = _database;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(controller.GetConfiguredLevel(1)?.LevelNumber, Is.EqualTo(1));
                Assert.That(controller.GetConfiguredLevel(12)?.LevelNumber, Is.EqualTo(12));
                Assert.That(controller.GetConfiguredLevel(15)?.IsConfiguredMegaShooter, Is.True);
                Assert.That(controller.GetConfiguredLevel(0), Is.Null);
                Assert.That(controller.GetConfiguredLevel(_database.TotalLevels + 1), Is.Null);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void LevelManagerCanSelectWithoutReloadingScene()
        {
            GameObject go = new GameObject("LevelManagerSelectionTest");
            try
            {
                LevelManager manager = go.AddComponent<LevelManager>();
                Assert.That(manager.ConfigureDatabaseIfMissing(_database), Is.True);
                Assert.That(manager.TrySelectLevel(11, false), Is.True);
                Assert.That(manager.CurrentLevel.LevelNumber, Is.EqualTo(12));
                Assert.That(manager.CurrentLevelIndex, Is.EqualTo(11));
                Assert.That(manager.TrySelectLevel(14, false), Is.True);
                Assert.That(manager.CurrentLevel.IsConfiguredMegaShooter, Is.True);
                Assert.That(manager.GetSceneNameForLevel(manager.CurrentLevel), Is.EqualTo("MegaShooterScene"));
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
#endif
