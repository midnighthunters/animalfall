using System.Reflection;
using AnimalFall.Core.Animals;
using AnimalFall.Data;
using AnimalFall.Managers;
using AnimalFall.Utils;
using NUnit.Framework;
using UnityEngine;

namespace AnimalFall.Tests.Editor
{
    public sealed class AnimalMovementBoundaryTests
    {
        private GameObject _cameraObject;
        private Camera _camera;
        private GameObject _animalObject;
        private Animal _animal;
        private AnimalMovement _movement;
        private AnimalData _testData;
        private static readonly BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;

        [SetUp]
        public void SetUp()
        {
            ImageLibrary.LoadAll();
            ActiveAnimalRegistry.Clear();
            GameEvents.ClearAll();

            _cameraObject = new GameObject("Test Main Camera");
            _cameraObject.tag = "MainCamera";
            _camera = _cameraObject.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 5.5f;
            _cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            _testData = ScriptableObject.CreateInstance<AnimalData>();
            _testData.species = AnimalSpecies.Pig;
            _testData.type = AnimalType.Normal;
            _testData.movementPattern = MovementPattern.Drift;
            _testData.speedMin = 1.5f;
            _testData.speedMax = 2.0f;
            _testData.lifetime = 10f;

            _animalObject = new GameObject("Test Animal");
            _animalObject.AddComponent<SpriteRenderer>();
            _animalObject.AddComponent<BoxCollider2D>();
            _animalObject.AddComponent<Rigidbody2D>();
            _animal = _animalObject.AddComponent<Animal>();
            _movement = _animalObject.GetComponent<AnimalMovement>();
        }

        [TearDown]
        public void TearDown()
        {
            ActiveAnimalRegistry.Clear();
            GameEvents.ClearAll();
            if (_animalObject != null) Object.DestroyImmediate(_animalObject);
            if (_cameraObject != null) Object.DestroyImmediate(_cameraObject);
            if (_testData != null) Object.DestroyImmediate(_testData);
        }

        [Test]
        public void NormalFallingAnimal_SpawnedAboveScreen_DoesNotDespawnOnEarlyFrames()
        {
            float z = Mathf.Abs(_camera.transform.position.z);
            float screenTop = _camera.ViewportToWorldPoint(new Vector3(0.5f, 1f, z)).y;
            float spawnPos = screenTop + 0.65f; // Standard Spawner height: 6.15f
            _animalObject.transform.position = new Vector3(0f, spawnPos, 0f);

            _animal.SetupForPool(_testData, null);

            // Invoke Update on AnimalMovement via reflection to simulate frame ticks
            MethodInfo updateMethod = typeof(AnimalMovement).GetMethod("Update", InstancePrivate);
            Assert.That(updateMethod, Is.Not.Null);

            for (int frame = 0; frame < 10; frame++)
            {
                updateMethod.Invoke(_movement, null);
                Assert.That(_animal.IsCollected, Is.False, $"Animal should not despawn on frame {frame + 1} while falling from above screen.");
            }
        }

        [Test]
        public void NormalFallingAnimal_SpawnedWithClearanceAboveScreen_DoesNotDespawn()
        {
            float z = Mathf.Abs(_camera.transform.position.z);
            float screenTop = _camera.ViewportToWorldPoint(new Vector3(0.5f, 1f, z)).y;
            float spawnPos = screenTop + 0.65f + 1.65f; // Stacked clearance spawn height: 7.80f
            _animalObject.transform.position = new Vector3(0f, spawnPos, 0f);

            _animal.SetupForPool(_testData, null);

            MethodInfo updateMethod = typeof(AnimalMovement).GetMethod("Update", InstancePrivate);
            Assert.That(updateMethod, Is.Not.Null);

            for (int frame = 0; frame < 10; frame++)
            {
                updateMethod.Invoke(_movement, null);
                Assert.That(_animal.IsCollected, Is.False, $"Animal should not despawn on frame {frame + 1} when spawned with lane clearance.");
            }
        }

        [Test]
        public void NormalFallingAnimal_ExitingBottom_DespawnsAndTriggersMiss()
        {
            float z = Mathf.Abs(_camera.transform.position.z);
            float screenBottom = _camera.ViewportToWorldPoint(new Vector3(0.5f, 0f, z)).y;
            _animalObject.transform.position = new Vector3(0f, screenBottom - 0.7f, 0f);

            _animal.SetupForPool(_testData, null);

            bool missedFired = false;
            GameEvents.OnAnimalMissed += () => missedFired = true;

            MethodInfo updateMethod = typeof(AnimalMovement).GetMethod("Update", InstancePrivate);
            Assert.That(updateMethod, Is.Not.Null);
            updateMethod.Invoke(_movement, null);

            Assert.That(missedFired, Is.True, "Animal falling below screen bottom should invoke OnAnimalMissed.");
            Assert.That(_animal.IsCollected, Is.True, "Animal falling below screen bottom should be marked collected/despawned.");
        }

        [Test]
        public void UpwardMovingAnimal_ExitingTop_DespawnsCleanly()
        {
            _testData.movementPattern = MovementPattern.FloatUp;
            float z = Mathf.Abs(_camera.transform.position.z);
            float screenTop = _camera.ViewportToWorldPoint(new Vector3(0.5f, 1f, z)).y;
            _animalObject.transform.position = new Vector3(0f, screenTop + 0.7f, 0f);

            _animal.SetupForPool(_testData, null);

            MethodInfo updateMethod = typeof(AnimalMovement).GetMethod("Update", InstancePrivate);
            Assert.That(updateMethod, Is.Not.Null);
            updateMethod.Invoke(_movement, null);

            Assert.That(_animal.IsCollected, Is.True, "FloatUp animal above screen top should despawn cleanly.");
        }

        [Test]
        public void BubbleShieldAnimal_SpawnedAboveScreen_FallsDownAndDoesNotDespawn()
        {
            _animalObject.transform.position = new Vector3(0f, 6.0f, 0f);
            _animal.SetupForPool(_testData, null);
            _movement.Configure(_testData, null);

            // Set IsBubble
            var isBubbleProp = typeof(Animal).GetProperty("IsBubble");
            isBubbleProp.SetValue(_animal, true);

            MethodInfo updateMethod = typeof(AnimalMovement).GetMethod("Update", InstancePrivate);
            Assert.That(updateMethod, Is.Not.Null);

            float initialY = _animalObject.transform.position.y;
            for (int frame = 0; frame < 10; frame++)
            {
                updateMethod.Invoke(_movement, null);
                Assert.That(_animal.IsCollected, Is.False, $"Bubble animal must not despawn on frame {frame + 1} while falling.");
            }

            Assert.That(_animalObject.transform.position.y, Is.LessThan(initialY), "Bubble animal should move downward into the screen.");
        }
    }
}
