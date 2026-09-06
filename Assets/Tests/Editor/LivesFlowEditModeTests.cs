using AnimalFall.Managers;
using AnimalFall.Services;
using NUnit.Framework;
using UnityEngine;

namespace AnimalFall.Tests.Editor
{
    public sealed class LivesFlowEditModeTests
    {
        private GameObject _livesObject;
        private GameObject _saveObject;
        private LivesManager _livesManager;
        private SaveService _saveService;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAll();

            // Clear any lingering instances
            if (LivesManager.Instance != null)
            {
                Object.DestroyImmediate(LivesManager.Instance.gameObject);
            }
            if (SaveService.Instance != null)
            {
                Object.DestroyImmediate(SaveService.Instance.gameObject);
            }

            PlayerPrefs.DeleteKey("AnimalFall_Save");
            SaveService.DeleteSaveFiles();

            _saveObject = new GameObject("TestSaveService");
            _saveService = _saveObject.AddComponent<SaveService>();

            _livesObject = new GameObject("TestLivesManager");
            _livesManager = _livesObject.AddComponent<LivesManager>();
            _livesManager.Init(_saveService);
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.ClearAll();

            if (_livesObject != null)
            {
                Object.DestroyImmediate(_livesObject);
            }
            if (_saveObject != null)
            {
                Object.DestroyImmediate(_saveObject);
            }

            PlayerPrefs.DeleteKey("AnimalFall_Save");
            SaveService.DeleteSaveFiles();
        }

        [Test]
        public void FreshSaveProfile_InitializesWithMaxLives_AndHasLivesIsTrue()
        {
            Assert.That(_livesManager.CurrentLives, Is.EqualTo(5));
            Assert.That(_livesManager.HasLives(), Is.True);
            Assert.That(_saveService.GetLives(), Is.EqualTo(5));
        }

        [Test]
        public void LevelFailedEvent_DecrementsLivesAndInvokesOnLivesChanged()
        {
            int reportedLives = -1;
            _livesManager.OnLivesChanged += lives => reportedLives = lives;

            GameEvents.OnLevelFailed?.Invoke();

            Assert.That(_livesManager.CurrentLives, Is.EqualTo(4));
            Assert.That(reportedLives, Is.EqualTo(4));
            Assert.That(_saveService.GetLives(), Is.EqualTo(4));
        }

        [Test]
        public void MultipleFailures_DecrementsLivesToZero_AndDoesNotGoBelowZero()
        {
            for (int i = 0; i < 7; i++)
            {
                GameEvents.OnLevelFailed?.Invoke();
            }

            Assert.That(_livesManager.CurrentLives, Is.EqualTo(0));
            Assert.That(_livesManager.HasLives(), Is.False);
            Assert.That(_saveService.GetLives(), Is.EqualTo(0));
        }

        [Test]
        public void OfflineLivesRegen_ComputesCorrectly()
        {
            // P9 pure function test
            Assert.That(LivesManager.ComputeOfflineLives(0, 30), Is.EqualTo(1));
            Assert.That(LivesManager.ComputeOfflineLives(0, 60), Is.EqualTo(2));
            Assert.That(LivesManager.ComputeOfflineLives(0, 150), Is.EqualTo(5));
            Assert.That(LivesManager.ComputeOfflineLives(3, 30), Is.EqualTo(4));
            Assert.That(LivesManager.ComputeOfflineLives(3, 100), Is.EqualTo(5));
            Assert.That(LivesManager.ComputeOfflineLives(4, 29.9), Is.EqualTo(4));
        }

        [Test]
        public void Refill_RestoresMaxLivesImmediately()
        {
            _livesManager.UseLife();
            _livesManager.UseLife();
            Assert.That(_livesManager.CurrentLives, Is.EqualTo(3));

            _livesManager.Refill();

            Assert.That(_livesManager.CurrentLives, Is.EqualTo(5));
            Assert.That(_saveService.GetLives(), Is.EqualTo(5));
        }
    }
}
