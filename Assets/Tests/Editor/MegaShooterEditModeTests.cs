#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using AnimalFall.Data;
using AnimalFall.Managers;
using AnimalFall.MegaShooter;
using AnimalFall.MegaShooter.Editor;

namespace AnimalFall.Tests.Editor
{
    public sealed class MegaShooterEditModeTests
    {
        private MegaLevelData[] _levels;
        private LevelDatabase _database;

        [SetUp]
        public void SetUp()
        {
            _levels = MegaShooterValidator.LoadMegaLevels();
            _database = AssetDatabase.LoadAssetAtPath<LevelDatabase>("Assets/Levels/LevelDatabase.asset");
        }

        [Test]
        public void ExactlyTwentyMegaLevelsCoverMultiplesOfFive()
        {
            Assert.That(_levels.Length, Is.EqualTo(20));
            CollectionAssert.AreEqual(Enumerable.Range(1, 20).Select(i => i * 5), _levels.Select(level => level.gameLevelNumber));
            CollectionAssert.AreEqual(Enumerable.Range(1, 20), _levels.Select(level => level.megaSequenceIndex));
        }

        [Test]
        public void MegaLevelsHaveCompleteEncounterAndDifficultyData()
        {
            foreach (MegaLevelData level in _levels)
            {
                Assert.That(level.IsValidMegaNumber, Is.True, level.name);
                Assert.That(level.waves, Is.Not.Null.And.Not.Empty, level.name);
                Assert.That(level.waves.All(wave => wave != null && wave.spawnGroups != null && wave.spawnGroups.Length > 0), Is.True, level.name);
                Assert.That(level.boss, Is.Not.Null, level.name);
                Assert.That(level.enemyHealthMultiplier, Is.GreaterThan(0f), level.name);
                Assert.That(level.enemyProjectileSpeedMultiplier, Is.GreaterThan(0f), level.name);
                Assert.That(level.ordinaryEnemyFireInterval, Is.GreaterThanOrEqualTo(.85f), level.name);
                Assert.That(level.maximumHostileProjectiles, Is.InRange(1, 120), level.name);
            }
        }

        [Test]
        public void DatabaseIsDynamicAndOnlyMultiplesOfFiveUseShooterMode()
        {
            Assert.That(_database, Is.Not.Null);
            Assert.That(_database.TotalLevels, Is.GreaterThanOrEqualTo(100));
            Assert.That(_database.GetLevelOrNull(-1), Is.Null);
            Assert.That(_database.GetLevelOrNull(_database.TotalLevels), Is.Null);
            for (int index = 0; index < _database.TotalLevels; index++)
            {
                LevelData level = _database.GetLevelOrNull(index);
                if ((index + 1) % 5 == 0)
                    Assert.That(level != null && level.IsConfiguredMegaShooter, Is.True, $"Level {index + 1}");
                else if (level != null)
                    Assert.That(level.Mode == LevelMode.Normal && level.MegaShooterData == null, Is.True, $"Level {index + 1}");
            }
        }

        [Test]
        public void SuperAnimalUnlocksAndStableIdsAreValid()
        {
            SuperAnimalData[] animals = LoadAssets<SuperAnimalData>(MegaShooterGenerator.DataRoot + "/Animals");
            Assert.That(animals.Length, Is.EqualTo(10));
            Assert.That(animals.Select(a => a.stableId).Distinct().Count(), Is.EqualTo(10));
            CollectionAssert.AreEqual(new[] { 5, 15, 25, 35, 45, 55, 65, 75, 85, 95 }, animals.OrderBy(a => a.unlockGameLevel).Select(a => a.unlockGameLevel));
            Assert.That(animals.All(a => a.unlockGameLevel == a.unlockMegaIndex * 5), Is.True);
        }

        [Test]
        public void BossThresholdsAreStrictlyDescendingAndReferencesAreComplete()
        {
            foreach (MegaLevelData level in _levels)
            {
                Assert.That(level.boss.prefab, Is.Not.Null, level.name);
                Assert.That(level.boss.sprite, Is.Not.Null, level.name);
                Assert.That(level.boss.phases.Length, Is.GreaterThanOrEqualTo(2), level.name);
                for (int i = 1; i < level.boss.phases.Length; i++)
                    Assert.That(level.boss.phases[i].healthThreshold, Is.LessThan(level.boss.phases[i - 1].healthThreshold), level.name);
                Assert.That(level.allowedAnimals.All(a => a != null && a.playerPrefab != null && a.primaryWeapon != null && a.primaryWeapon.projectile != null), Is.True, level.name);
                Assert.That(level.waves.SelectMany(w => w.spawnGroups).All(g => g != null && g.enemy != null && g.enemy.prefab != null && g.enemy.projectile != null && g.enemy.sprite != null), Is.True, level.name);
            }
        }

        [Test]
        public void LevelRoutingSelectsExistingAndDedicatedScenes()
        {
            GameObject go = new GameObject("LevelManagerRoutingTest");
            try
            {
                LevelManager manager = go.AddComponent<LevelManager>();
                Assert.That(manager.GetSceneNameForLevel(_database.GetLevelOrNull(0)), Is.EqualTo("GameScene"));
                Assert.That(manager.GetSceneNameForLevel(_database.GetLevelOrNull(4)), Is.EqualTo("MegaShooterScene"));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void GeneratorIsIdempotentForNormalAssetsAndDatabaseReferences()
        {
            LevelData[] beforeReferences = _database.Levels.ToArray();
            var normalJson = new Dictionary<LevelData, string>();
            for (int i = 0; i < beforeReferences.Length; i++)
                if ((i + 1) % 5 != 0 && beforeReferences[i] != null)
                    normalJson.Add(beforeReferences[i], EditorJsonUtility.ToJson(beforeReferences[i]));

            MegaShooterGenerator.GenerateOrUpdateMegaLevelsOnly();

            CollectionAssert.AreEqual(beforeReferences, _database.Levels);
            foreach (KeyValuePair<LevelData, string> item in normalJson)
                Assert.That(EditorJsonUtility.ToJson(item.Key), Is.EqualTo(item.Value), item.Key.name);
        }

        [Test]
        public void FullValidatorPasses()
        {
            Assert.That(MegaShooterValidator.ValidateAll(false), Is.True);
        }

        private static T[] LoadAssets<T>(string folder) where T : Object
            => AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<T>).Where(asset => asset != null).ToArray();
    }
}
#endif
