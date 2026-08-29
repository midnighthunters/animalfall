using System.Reflection;
using AnimalFall.Core.Animals;
using AnimalFall.Data;
using AnimalFall.Managers;
using AnimalFall.Utils;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AnimalFall.Tests.Editor
{
    /// <summary>Guards the authored level catalogue and the normal-level goal-to-win path.</summary>
    public sealed class LevelCompletionFlowEditModeTests
    {
        private const string DatabasePath = "Assets/Levels/LevelDatabase.asset";
        private static readonly BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;

        private GameObject _trackerObject;
        private GameObject _managerObject;
        private GameObject _inputObject;
        private GameObject _animalObject;
        private GameObject _cameraObject;
        private AnimalData _testAnimalData;
        private LevelManager _previousLevelManager;

        [SetUp]
        public void SetUp()
        {
            _previousLevelManager = LevelManager.Instance;
            SetLevelManagerInstance(null);
            GameEvents.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.ClearAll();
            if (_managerObject != null) Object.DestroyImmediate(_managerObject);
            if (_trackerObject != null) Object.DestroyImmediate(_trackerObject);
            if (_inputObject != null) Object.DestroyImmediate(_inputObject);
            if (_animalObject != null) Object.DestroyImmediate(_animalObject);
            if (_cameraObject != null) Object.DestroyImmediate(_cameraObject);
            if (_testAnimalData != null) Object.DestroyImmediate(_testAnimalData);
            SetLevelManagerInstance(_previousLevelManager);
        }

        [Test]
        public void LevelsOneToOneHundred_CompleteWhenEveryNormalLevelGoalIsCollected()
        {
            LevelDatabase database = AssetDatabase.LoadAssetAtPath<LevelDatabase>(DatabasePath);
            Assert.That(database, Is.Not.Null);
            Assert.That(database.TotalLevels, Is.GreaterThanOrEqualTo(100));

            GoalTracker tracker = CreateTracker();
            GameManager manager = CreateManager(tracker);
            MethodInfo beginLevel = typeof(GameManager).GetMethod("BeginLevel", InstancePrivate);
            Assert.That(beginLevel, Is.Not.Null);

            int normalLevels = 0;
            int megaShooterLevels = 0;
            int wins = 0;
            GameEvents.OnLevelWon += () => wins++;

            const int attempts = 5;
            for (int pass = 0; pass < attempts; pass++)
            {
                for (int index = 0; index < 100; index++)
                {
                    int levelNumber = index + 1;
                    LevelData level = database.GetLevelOrNull(index);
                    Assert.That(level, Is.Not.Null, $"Pass {pass + 1}, level {levelNumber} is missing.");
                    Assert.That(level.LevelNumber, Is.EqualTo(levelNumber), $"Pass {pass + 1}, level {levelNumber} has the wrong authored number.");

                    if (level.IsConfiguredMegaShooter)
                    {
                        if (pass == 0) megaShooterLevels++;
                        Assert.That(level.MegaShooterData, Is.Not.Null, $"Pass {pass + 1}, mega level {levelNumber} has no completion configuration.");
                        continue;
                    }

                    if (pass == 0) normalLevels++;
                    Assert.That(level.Goal, Is.Not.Null, $"Pass {pass + 1}, normal level {levelNumber} has no goals.");
                    Assert.That(level.Goal.Targets, Is.Not.Empty, $"Pass {pass + 1}, normal level {levelNumber} has an empty goal list.");

                    beginLevel.Invoke(manager, new object[] { level });
                    Assert.That(manager.State, Is.EqualTo(GameState.Running), $"Pass {pass + 1}, level {levelNumber} did not start.");

                    foreach (GoalData.SpeciesTarget target in level.Goal.Targets)
                    {
                        Assert.That(target.count, Is.GreaterThan(0), $"Pass {pass + 1}, level {levelNumber} has an invalid target count.");
                        for (int collected = 0; collected < target.count; collected++)
                            GameEvents.OnAnimalCollected?.Invoke(target.species, AnimalType.Normal, Vector3.zero);
                    }

                    Assert.That(tracker.IsComplete, Is.True, $"Pass {pass + 1}, level {levelNumber} did not complete after all goals were collected.");
                    Assert.That(manager.State, Is.EqualTo(GameState.Ended), $"Pass {pass + 1}, level {levelNumber} did not enter its win state.");
                }
            }

            Assert.That(normalLevels, Is.EqualTo(80));
            Assert.That(megaShooterLevels, Is.EqualTo(20));
            Assert.That(wins, Is.EqualTo(normalLevels * attempts));
        }

        [Test]
        public void EveryNormalLevelCompletesThroughSyntheticAnimalTaps()
        {
            ImageLibrary.LoadAll();
            LevelDatabase database = AssetDatabase.LoadAssetAtPath<LevelDatabase>(DatabasePath);
            Assert.That(database, Is.Not.Null);

            GoalTracker tracker = CreateTracker();
            GameManager manager = CreateManager(tracker);
            _inputObject = new GameObject("Campaign Tap Input");
            InputManager input = _inputObject.AddComponent<InputManager>();
            typeof(GameManager).GetField("_inputManager", InstancePrivate).SetValue(manager, input);
            MethodInfo beginLevel = typeof(GameManager).GetMethod("BeginLevel", InstancePrivate);
            Assert.That(beginLevel, Is.Not.Null);

            int completed = 0;
            GameEvents.OnLevelWon += () => completed++;
            for (int index = 0; index < 100; index++)
            {
                LevelData level = database.GetLevelOrNull(index);
                Assert.That(level, Is.Not.Null, $"Level {index + 1} is missing.");
                if (level.IsConfiguredMegaShooter) continue;

                beginLevel.Invoke(manager, new object[] { level });
                Assert.That(manager.State, Is.EqualTo(GameState.Running), $"Level {level.LevelNumber} did not start.");
                foreach (GoalData.SpeciesTarget target in level.Goal.Targets)
                    for (int collected = 0; collected < target.count; collected++)
                        TapAnimal(input, level, target.species);

                Assert.That(tracker.IsComplete, Is.True, $"Level {level.LevelNumber} did not complete through taps.");
                Assert.That(manager.State, Is.EqualTo(GameState.Ended), $"Level {level.LevelNumber} did not end after its final tap.");
            }

            Assert.That(completed, Is.EqualTo(80));
        }

        [Test]
        public void RepeatedSpeciesTargets_AreCombinedBeforeCompletion()
        {
            GoalData goal = ScriptableObject.CreateInstance<GoalData>();
            FieldInfo targets = typeof(GoalData).GetField("_targets", InstancePrivate);
            targets.SetValue(goal, new[]
            {
                new GoalData.SpeciesTarget { species = AnimalSpecies.Chicken, count = 1 },
                new GoalData.SpeciesTarget { species = AnimalSpecies.Chicken, count = 2 }
            });

            GoalTracker tracker = CreateTracker();
            int completions = 0;
            tracker.OnAllGoalsComplete += () => completions++;
            tracker.Setup(goal);

            Assert.That(tracker.GetRemaining(AnimalSpecies.Chicken), Is.EqualTo(3));
            GameEvents.OnAnimalCollected?.Invoke(AnimalSpecies.Chicken, AnimalType.Normal, Vector3.zero);
            GameEvents.OnAnimalCollected?.Invoke(AnimalSpecies.Chicken, AnimalType.Normal, Vector3.zero);
            Assert.That(tracker.IsComplete, Is.False);
            GameEvents.OnAnimalCollected?.Invoke(AnimalSpecies.Chicken, AnimalType.Normal, Vector3.zero);

            Assert.That(tracker.IsComplete, Is.True);
            Assert.That(completions, Is.EqualTo(1));
            Object.DestroyImmediate(goal);
        }

        [Test]
        public void SyntheticTap_UsesTheSameAnimalCollectionRouteAsPlayerTap()
        {
            ImageLibrary.LoadAll();
            Camera camera = Camera.main;
            if (camera == null)
            {
                _cameraObject = new GameObject("Tap Test Camera");
                _cameraObject.tag = "MainCamera";
                camera = _cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.transform.position = new Vector3(0f, 0f, -10f);
            }

            _inputObject = new GameObject("Input Test");
            InputManager input = _inputObject.AddComponent<InputManager>();
            _animalObject = new GameObject("Tap Target");
            _animalObject.AddComponent<SpriteRenderer>();
            _animalObject.AddComponent<BoxCollider2D>();
            _animalObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            Animal animal = _animalObject.AddComponent<Animal>();
            _testAnimalData = ScriptableObject.CreateInstance<AnimalData>();
            _testAnimalData.species = AnimalSpecies.Chicken;
            _testAnimalData.type = AnimalType.Normal;
            _testAnimalData.lifetime = 30f;

            Vector3 viewportCenter = camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, -camera.transform.position.z));
            _animalObject.transform.position = new Vector3(viewportCenter.x, viewportCenter.y, 0f);
            animal.SetupForPool(_testAnimalData, null);
            _animalObject.GetComponent<SpriteRenderer>().sortingOrder = 10000;
            Physics2D.SyncTransforms();

            int collections = 0;
            GameEvents.OnAnimalCollected += (_, _, _) => collections++;
            input.DispatchSyntheticWorldTap(_animalObject.transform.position);

            Assert.That(animal.IsCollected, Is.True);
            Assert.That(collections, Is.EqualTo(1));
        }

        private static void TapAnimal(InputManager input, LevelData level, AnimalSpecies species)
        {
            GameObject target = new GameObject($"Tap Target {species}");
            target.AddComponent<SpriteRenderer>();
            target.AddComponent<BoxCollider2D>();
            target.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            Animal animal = target.AddComponent<Animal>();
            AnimalData data = ScriptableObject.CreateInstance<AnimalData>();
            data.species = species;
            data.type = AnimalType.Normal;
            data.isTargetSpecies = true;
            data.lifetime = 30f;

            target.transform.position = Vector3.zero;
            animal.SetupForPool(data, level);
            target.GetComponent<SpriteRenderer>().sortingOrder = 10000;
            Physics2D.SyncTransforms();
            for (int tap = 0; !animal.IsCollected && tap < 8; tap++)
                input.DispatchSyntheticWorldTap(target.transform.position);

            Assert.That(animal.IsCollected, Is.True, $"{species} on level {level.LevelNumber} was not collected by a tap.");
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(data);
        }

        private GoalTracker CreateTracker()
        {
            _trackerObject = new GameObject("GoalTracker Test");
            _trackerObject.SetActive(false);
            GoalTracker tracker = _trackerObject.AddComponent<GoalTracker>();
            typeof(GoalTracker).GetMethod("OnEnable", InstancePrivate).Invoke(tracker, null);
            return tracker;
        }

        private GameManager CreateManager(GoalTracker tracker)
        {
            _managerObject = new GameObject("GameManager Test");
            GameManager manager = _managerObject.AddComponent<GameManager>();
            typeof(GameManager).GetField("_goalTracker", InstancePrivate).SetValue(manager, tracker);
            return manager;
        }

        private static void SetLevelManagerInstance(LevelManager value)
        {
            typeof(LevelManager).GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .SetValue(null, value);
        }
    }
}
