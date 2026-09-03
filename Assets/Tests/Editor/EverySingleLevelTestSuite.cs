#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using AnimalFall.Data;
using AnimalFall.Managers;
using AnimalFall.Core.Animals;
using AnimalFall.MegaShooter;

namespace AnimalFall.Tests.Editor
{
    public sealed class EverySingleLevelTestSuite
    {
        private LevelDatabase _database;

        [SetUp]
        public void SetUp()
        {
            _database = AssetDatabase.LoadAssetAtPath<LevelDatabase>("Assets/Levels/LevelDatabase.asset");
            Assert.That(_database, Is.Not.Null, "LevelDatabase asset missing at Assets/Levels/LevelDatabase.asset");
        }

        [Test]
        public void AllOneHundredLevels_HaveValidConfigurationsAndModeAssignments()
        {
            Assert.That(_database.TotalLevels, Is.EqualTo(100));

            int normalCount = 0;
            int megaCount = 0;

            for (int i = 0; i < 100; i++)
            {
                int levelNum = i + 1;
                LevelData level = _database.GetLevel(i);
                Assert.That(level, Is.Not.Null, $"Level {levelNum} is null in database.");
                Assert.That(level.LevelNumber, Is.EqualTo(levelNum), $"Level {levelNum} has incorrect level number.");
                Assert.That(level.TimeLimit, Is.GreaterThan(0f), $"Level {levelNum} has non-positive time limit.");
                Assert.That(level.RewardCoins, Is.GreaterThan(0), $"Level {levelNum} has zero or negative coin reward.");

                if (levelNum % 5 == 0)
                {
                    megaCount++;
                    Assert.That(level.IsConfiguredMegaShooter, Is.True, $"Level {levelNum} must be configured as MegaShooter.");
                    Assert.That(level.MegaShooterData, Is.Not.Null, $"Level {levelNum} missing MegaShooterData.");
                }
                else
                {
                    normalCount++;
                    Assert.That(level.IsConfiguredMegaShooter, Is.False, $"Level {levelNum} must be standard mode.");
                    Assert.That(level.Goal, Is.Not.Null, $"Level {levelNum} missing GoalData.");
                    Assert.That(level.SpawnPool, Is.Not.Null, $"Level {levelNum} missing SpawnPool.");
                    Assert.That(level.SpawnPool.Length, Is.GreaterThan(0), $"Level {levelNum} has empty SpawnPool.");
                }
            }

            Assert.That(normalCount, Is.EqualTo(80));
            Assert.That(megaCount, Is.EqualTo(20));
        }

        [Test]
        public void EveryStandardLevel_AllGoalTargetsAreReachableFromSpawnPool()
        {
            for (int i = 0; i < 100; i++)
            {
                int levelNum = i + 1;
                if (levelNum % 5 == 0) continue; // Skip mega levels

                LevelData level = _database.GetLevel(i);
                GoalData goal = level.Goal;
                Assert.That(goal.Targets, Is.Not.Empty, $"Standard Level {levelNum} has empty goal targets.");

                var poolSpecies = new HashSet<AnimalSpecies>();
                foreach (AnimalData animal in level.SpawnPool)
                {
                    if (animal != null) poolSpecies.Add(animal.species);
                }

                foreach (GoalData.SpeciesTarget target in goal.Targets)
                {
                    Assert.That(target.count, Is.GreaterThan(0), $"Standard Level {levelNum} target count for {target.species} is <= 0.");
                    Assert.That(poolSpecies.Contains(target.species), Is.True,
                        $"Standard Level {levelNum} requires {target.species} in its goal, but {target.species} is not in SpawnPool!");
                }
            }
        }

        [Test]
        public void EveryStandardLevel_GoalTrackerTriggersVictoryUponCollection()
        {
            for (int i = 0; i < 100; i++)
            {
                int levelNum = i + 1;
                if (levelNum % 5 == 0) continue; // Skip mega levels

                LevelData level = _database.GetLevel(i);
                GameObject go = new GameObject($"Tracker_Level_{levelNum}");
                GoalTracker tracker = go.AddComponent<GoalTracker>();
                typeof(GoalTracker).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.Invoke(tracker, null);

                bool won = false;
                tracker.OnAllGoalsComplete += () => won = true;
                tracker.Setup(level.Goal);

                Assert.That(tracker.IsComplete, Is.False, $"Level {levelNum} GoalTracker completed immediately before collection.");

                foreach (GoalData.SpeciesTarget target in level.Goal.Targets)
                {
                    for (int c = 0; c < target.count; c++)
                    {
                        GameEvents.OnAnimalCollected?.Invoke(target.species, AnimalType.Normal, Vector3.zero);
                    }
                }

                Assert.That(tracker.IsComplete, Is.True, $"Level {levelNum} GoalTracker was not completed after all goal animals collected.");
                Assert.That(won, Is.True, $"Level {levelNum} OnAllGoalsComplete event did not fire.");

                Object.DestroyImmediate(go);
                GameEvents.ClearAll();
            }
        }

        [Test]
        public void EveryMegaLevel_HasAuthoredWavesOrBossAndValidPacing()
        {
            for (int i = 0; i < 100; i++)
            {
                int levelNum = i + 1;
                if (levelNum % 5 != 0) continue; // Only mega levels

                LevelData level = _database.GetLevel(i);
                MegaLevelData mega = level.MegaShooterData;
                Assert.That(mega, Is.Not.Null, $"Mega Level {levelNum} missing MegaLevelData.");
                Assert.That(mega.gameLevelNumber, Is.EqualTo(levelNum), $"Mega Level {levelNum} gameLevelNumber mismatch.");
                Assert.That(mega.parTime, Is.GreaterThan(0f), $"Mega Level {levelNum} has invalid parTime.");
                Assert.That(mega.coinReward, Is.GreaterThan(0), $"Mega Level {levelNum} has zero coin reward.");

                bool hasBoss = mega.boss != null;
                bool hasWaves = mega.waves != null && mega.waves.Length > 0;
                Assert.That(hasBoss || hasWaves, Is.True, $"Mega Level {levelNum} has neither waves nor boss.");
            }
        }
    }
}
#endif
