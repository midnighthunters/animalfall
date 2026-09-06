using System.IO;
using AnimalFall.Managers;
using AnimalFall.Services;
using NUnit.Framework;
using UnityEngine;

namespace AnimalFall.Tests.Editor
{
    public sealed class SavePersistenceEditModeTests
    {
        private GameObject _saveObject;
        private SaveService _saveService;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAll();

            if (SaveService.Instance != null)
            {
                Object.DestroyImmediate(SaveService.Instance.gameObject);
            }

            SaveService.DeleteSaveFiles();

            _saveObject = new GameObject("TestSaveService");
            _saveService = _saveObject.AddComponent<SaveService>();
            _saveService.LoadAll();
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.ClearAll();

            if (_saveObject != null)
            {
                Object.DestroyImmediate(_saveObject);
            }

            SaveService.DeleteSaveFiles();
        }

        [Test]
        public void FreshSave_InitializesWithLevelOneProgress()
        {
            Assert.That(_saveService.GetHighestUnlockedLevel(), Is.EqualTo(0));
            Assert.That(_saveService.GetLives(), Is.EqualTo(5));
        }

        [Test]
        public void LevelProgress_PersistsAcrossSimulatedAppRestart()
        {
            // Simulate playing and reaching Level 4 (index 3)
            _saveService.SetHighestUnlockedLevel(3);
            _saveService.AddCoins(150);
            _saveService.SetStars(0, 3);
            _saveService.SetStars(1, 3);
            _saveService.SetStars(2, 2);
            _saveService.SaveAll();

            // Verify file exists on disk
            Assert.That(File.Exists(SaveService.SaveFilePath), Is.True, "Save file must exist on disk.");

            // Destroy session (simulate closing app)
            Object.DestroyImmediate(_saveObject);

            // Re-create session (simulate opening app again)
            _saveObject = new GameObject("TestSaveService_Relaunch");
            _saveService = _saveObject.AddComponent<SaveService>();
            _saveService.LoadAll();

            Assert.That(_saveService.GetHighestUnlockedLevel(), Is.EqualTo(3), "Level progress must survive app restart.");
            Assert.That(_saveService.GetCoins(), Is.EqualTo(150), "Coins must survive app restart.");
            Assert.That(_saveService.GetStars(0), Is.EqualTo(3), "Stars must survive app restart.");
            Assert.That(_saveService.GetStars(1), Is.EqualTo(3));
            Assert.That(_saveService.GetStars(2), Is.EqualTo(2));
        }

        [Test]
        public void PlayerPrefsMigration_TransfersToPersistentFile()
        {
            // Clean out files
            SaveService.DeleteSaveFiles();

            // Seed legacy PlayerPrefs
            var legacyData = new SaveData { highestUnlockedLevel = 7, coins = 500 };
            PlayerPrefs.SetString("AnimalFall_Save", JsonUtility.ToJson(legacyData));
            PlayerPrefs.Save();

            // Load into SaveService
            _saveService.LoadAll();

            Assert.That(_saveService.GetHighestUnlockedLevel(), Is.EqualTo(7));
            Assert.That(_saveService.GetCoins(), Is.EqualTo(500));
            Assert.That(File.Exists(SaveService.SaveFilePath), Is.True, "Legacy PlayerPrefs must be migrated to disk file.");
        }

        [Test]
        public void BackupFile_RecoversCorruptedPrimaryFile()
        {
            // Save valid data
            _saveService.SetHighestUnlockedLevel(4);
            _saveService.SaveAll();

            // Trigger a second save so a backup file is created
            _saveService.AddCoins(50);
            _saveService.SaveAll();

            Assert.That(File.Exists(SaveService.BackupFilePath), Is.True, "Backup file must be created on multiple saves.");

            // Corrupt primary file
            File.WriteAllText(SaveService.SaveFilePath, "INVALID_CORRUPTED_JSON{{{");

            // Relaunch
            Object.DestroyImmediate(_saveObject);
            _saveObject = new GameObject("TestSaveService_CorruptRecovery");
            _saveService = _saveObject.AddComponent<SaveService>();
            _saveService.LoadAll();

            Assert.That(_saveService.GetHighestUnlockedLevel(), Is.EqualTo(4), "Should recover progress from backup file.");
        }

        [Test]
        public void LevelSuccess_SavesProgressionDirectlyToDisk()
        {
            GameObject levelMgrObject = new GameObject("TestLevelManager");
            LevelManager levelManager = levelMgrObject.AddComponent<LevelManager>();
            var db = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimalFall.Data.LevelDatabase>("Assets/Levels/LevelDatabase.asset");
            levelManager.ConfigureDatabaseIfMissing(db);
            levelManager.Init(_saveService);

            // Complete Level 1 (index 0)
            levelManager.LevelSuccess(0);

            Assert.That(_saveService.GetHighestUnlockedLevel(), Is.EqualTo(1), "LevelSuccess(0) must unlock level index 1 (Level 2).");
            Assert.That(_saveService.GetStars(0), Is.EqualTo(3), "LevelSuccess(0) must award 3 stars.");

            // Verify disk persistence immediately
            Assert.That(File.Exists(SaveService.SaveFilePath), Is.True);
            string json = File.ReadAllText(SaveService.SaveFilePath);
            Assert.That(json, Does.Contain("\"highestUnlockedLevel\":1"));

            // Destroy and reload from disk
            Object.DestroyImmediate(_saveObject);
            _saveObject = new GameObject("TestSaveService_VerifyDisk");
            _saveService = _saveObject.AddComponent<SaveService>();
            _saveService.LoadAll();

            Assert.That(_saveService.GetHighestUnlockedLevel(), Is.EqualTo(1));
            Assert.That(_saveService.GetStars(0), Is.EqualTo(3));

            Object.DestroyImmediate(levelMgrObject);
        }
    }
}
