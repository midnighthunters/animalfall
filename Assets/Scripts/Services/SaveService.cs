// Task 12.2 — SaveService: JSON persistence, star rules, hindrance seen flags
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using AnimalFall.Core.Hindrances;

namespace AnimalFall.Services
{
    [System.Serializable]
    public class SaveData
    {
        public int   highestUnlockedLevel = 0;
        public int[] starRatings          = new int[100];
        public int   coins                = 0;
        public int   lives                = 5;
        public long  nextLifeUTC          = 0;
        public bool[] seenHindranceTypes  = new bool[69]; // indexed by stable HindranceType ID 0-68
        public string selectedSuperAnimalId = "eagle_striker";
        public List<string> unlockedSuperAnimalIds = new List<string> { "eagle_striker" };
        public List<string> unlockedSkins          = new List<string> { "default" };
        public string       equippedSkin           = "default";
        public int          arcadeTokens           = 0;
        public int[] megaBestScores = new int[100];
        public int[] megaBestStars = new int[100];
        public float[] megaBestTimes = new float[100];
        public bool[] megaCompleted = new bool[100];
        public string pendingSuperAnimalCelebrationId;
    }

    public class SaveService : MonoBehaviour
    {
        private const string PREFS_KEY = "AnimalFall_Save";
        private const string FILE_NAME = "AnimalFall_Save.json";

        public static event Action OnSaveDataLoaded;
        public static event Action OnSaveDataChanged;

        private static SaveService _instance;
        public static SaveService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<SaveService>();
                }
                return _instance;
            }
            private set => _instance = value;
        }

        private static readonly object _fileLock = new object();
        private SaveData _data = new SaveData();
        private bool _isLoaded;

        public static string SaveFilePath => Path.Combine(Application.persistentDataPath, FILE_NAME);
        public static string BackupFilePath => Path.Combine(Application.persistentDataPath, FILE_NAME + ".bak");
        public static string TempFilePath => Path.Combine(Application.persistentDataPath, FILE_NAME + ".tmp");

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAll();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveAll();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) SaveAll();
        }

        private void OnApplicationQuit()
        {
            SaveAll();
        }

        private void EnsureLoaded()
        {
            if (!_isLoaded && Application.isPlaying)
            {
                LoadAll();
            }
        }

        // ── Persistence ───────────────────────────────────────────────────────

        public void LoadAll()
        {
            string primaryJson = null;
            string backupJson = null;

            lock (_fileLock)
            {
                try
                {
                    if (File.Exists(SaveFilePath))
                        primaryJson = File.ReadAllText(SaveFilePath, System.Text.Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveService] Primary save file read failed: {ex.Message}");
                }

                try
                {
                    if (File.Exists(BackupFilePath))
                        backupJson = File.ReadAllText(BackupFilePath, System.Text.Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveService] Backup save file read failed: {ex.Message}");
                }
            }

            SaveData loaded = null;

            // 1. Try parsing primary file
            if (!string.IsNullOrEmpty(primaryJson))
            {
                try { loaded = JsonUtility.FromJson<SaveData>(primaryJson); }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveService] Primary save file corrupted: {ex.Message}");
                }
            }

            // 2. If primary failed or invalid, try backup file
            if (loaded == null && !string.IsNullOrEmpty(backupJson))
            {
                try { loaded = JsonUtility.FromJson<SaveData>(backupJson); }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveService] Backup save file corrupted: {ex.Message}");
                }
            }

            // 3. Fallback to PlayerPrefs (migration from older builds)
            if (loaded == null)
            {
                string prefsJson = PlayerPrefs.GetString(PREFS_KEY, "");
                if (!string.IsNullOrEmpty(prefsJson))
                {
                    try { loaded = JsonUtility.FromJson<SaveData>(prefsJson); }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[SaveService] PlayerPrefs save corrupted: {ex.Message}");
                    }
                }
            }

            _data = loaded ?? new SaveData();
            EnsureCapacity(100);
            _isLoaded = true;

            // If loaded from backup or PlayerPrefs because primary was missing/corrupted, re-save valid file
            if (loaded != null && (string.IsNullOrEmpty(primaryJson) || !File.Exists(SaveFilePath)))
            {
                SaveToFile(JsonUtility.ToJson(_data));
            }

            OnSaveDataLoaded?.Invoke();
        }

        public void EnsureCapacity(int totalLevels)
        {
            totalLevels = Mathf.Max(1, totalLevels);
            _data.starRatings = Grow(_data.starRatings, totalLevels);
            _data.megaBestScores = Grow(_data.megaBestScores, totalLevels);
            _data.megaBestStars = Grow(_data.megaBestStars, totalLevels);
            _data.megaBestTimes = Grow(_data.megaBestTimes, totalLevels);
            _data.megaCompleted = Grow(_data.megaCompleted, totalLevels);
            _data.seenHindranceTypes = Grow(_data.seenHindranceTypes, 69);
            if (_data.unlockedSuperAnimalIds == null)
                _data.unlockedSuperAnimalIds = new List<string>();
            if (!_data.unlockedSuperAnimalIds.Contains("eagle_striker"))
                _data.unlockedSuperAnimalIds.Add("eagle_striker");
            if (string.IsNullOrWhiteSpace(_data.selectedSuperAnimalId))
                _data.selectedSuperAnimalId = "eagle_striker";
            if (_data.highestUnlockedLevel == 0 && _data.nextLifeUTC == 0 && _data.lives <= 0)
                _data.lives = 5;
            _data.lives = Mathf.Clamp(_data.lives, 0, 5);
        }

        private static T[] Grow<T>(T[] source, int length)
        {
            if (source != null && source.Length >= length) return source;
            var result = new T[length];
            if (source != null) Array.Copy(source, result, source.Length);
            return result;
        }

        public void SaveAll()
        {
            string json = JsonUtility.ToJson(_data);

            // 1. Persist to PlayerPrefs as secondary backup
            try
            {
                PlayerPrefs.SetString(PREFS_KEY, json);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveService] PlayerPrefs save error: {ex.Message}");
            }

            // 2. Persist to disk with immediate fsync flush
            SaveToFile(json);

            OnSaveDataChanged?.Invoke();
        }

        private void SaveToFile(string json)
        {
            lock (_fileLock)
            {
                try
                {
                    string dir = Application.persistentDataPath;
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    string tempPath = TempFilePath;
                    string savePath = SaveFilePath;
                    string bakPath = BackupFilePath;

                    using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (var writer = new StreamWriter(fs, System.Text.Encoding.UTF8))
                    {
                        writer.Write(json);
                        writer.Flush();
                        fs.Flush(true); // forces disk sync (fsync)
                    }

                    if (File.Exists(savePath))
                    {
                        try
                        {
                            if (File.Exists(bakPath)) File.Delete(bakPath);
                            File.Copy(savePath, bakPath);
                        }
                        catch { }
                        File.Delete(savePath);
                    }

                    File.Move(tempPath, savePath);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SaveService] File save error: {ex.Message}");
                }
            }
        }

        /// <summary>Deletes persistent files and player prefs. For tests and profile reset.</summary>
        public static void DeleteSaveFiles()
        {
            lock (_fileLock)
            {
                try
                {
                    if (File.Exists(SaveFilePath)) File.Delete(SaveFilePath);
                    if (File.Exists(BackupFilePath)) File.Delete(BackupFilePath);
                    if (File.Exists(TempFilePath)) File.Delete(TempFilePath);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveService] Error deleting save files: {ex.Message}");
                }
                PlayerPrefs.DeleteKey(PREFS_KEY);
                PlayerPrefs.Save();
            }
        }

        public void ClearSave()
        {
            _data = new SaveData();
            EnsureCapacity(100);
            DeleteSaveFiles();
            OnSaveDataChanged?.Invoke();
        }

        // ── Stars ─────────────────────────────────────────────────────────────

        public int GetStars(int levelIndex)
        {
            EnsureLoaded();
            if (levelIndex < 0 || levelIndex >= _data.starRatings.Length) return -1;
            return _data.starRatings[levelIndex];
        }

        /// <summary>Overwrites only if newStars > existing, or if no prior result exists.</summary>
        public void SetStars(int levelIndex, int newStars)
        {
            EnsureLoaded();
            if (levelIndex < 0 || levelIndex >= _data.starRatings.Length) return;
            int existing = _data.starRatings[levelIndex];
            // -1 means unplayed; 0 = attempted; allow downward only on first-ever result
            bool noResult = (existing == 0 && GetHighestUnlockedLevel() <= levelIndex);
            if (noResult || newStars > existing)
            {
                _data.starRatings[levelIndex] = newStars;
                SaveAll();
            }
        }

        // ── Level progress ────────────────────────────────────────────────────

        public int  GetHighestUnlockedLevel()
        {
            EnsureLoaded();
            return _data.highestUnlockedLevel;
        }

        public void SetHighestUnlockedLevel(int v)
        {
            EnsureLoaded();
            _data.highestUnlockedLevel = v;
            SaveAll();
        }

        // ── Economy ───────────────────────────────────────────────────────────

        public int  GetCoins()
        {
            EnsureLoaded();
            return _data.coins;
        }

        public void AddCoins(int v)
        {
            EnsureLoaded();
            _data.coins += v;
            SaveAll();
        }

        public int  GetLives()
        {
            EnsureLoaded();
            return _data.lives;
        }

        public void SetLives(int v)
        {
            EnsureLoaded();
            _data.lives = v;
        }

        public long GetNextLifeUTC()
        {
            EnsureLoaded();
            return _data.nextLifeUTC;
        }

        public void SetNextLifeUTC(long v)
        {
            EnsureLoaded();
            _data.nextLifeUTC = v;
        }

        // ── Hindrance tutorial ────────────────────────────────────────────────

        public bool HasSeenHindrance(HindranceType t)
        {
            EnsureLoaded();
            int idx = (int)t;
            if (idx < 0 || idx >= _data.seenHindranceTypes.Length) return false;
            return _data.seenHindranceTypes[idx];
        }

        public void MarkHindranceSeen(HindranceType t)
        {
            EnsureLoaded();
            int idx = (int)t;
            if (idx < 0 || idx >= _data.seenHindranceTypes.Length) return;
            _data.seenHindranceTypes[idx] = true;
            SaveAll();
        }

        // ── Mega shooter progression ─────────────────────────────────────────

        public string GetSelectedSuperAnimalId() => _data.selectedSuperAnimalId;

        public void SetSelectedSuperAnimalId(string stableId)
        {
            if (string.IsNullOrWhiteSpace(stableId) || !IsSuperAnimalUnlocked(stableId)) return;
            _data.selectedSuperAnimalId = stableId;
            SaveAll();
        }

        public bool IsSuperAnimalUnlocked(string stableId)
            => !string.IsNullOrWhiteSpace(stableId) &&
               _data.unlockedSuperAnimalIds != null &&
               _data.unlockedSuperAnimalIds.Contains(stableId);

        public bool UnlockSuperAnimal(string stableId)
        {
            if (string.IsNullOrWhiteSpace(stableId) || IsSuperAnimalUnlocked(stableId)) return false;
            _data.unlockedSuperAnimalIds.Add(stableId);
            _data.pendingSuperAnimalCelebrationId = stableId;
            SaveAll();
            return true;
        }

        public bool ConsumeSuperAnimalCelebration(string stableId)
        {
            if (_data.pendingSuperAnimalCelebrationId != stableId) return false;
            _data.pendingSuperAnimalCelebrationId = string.Empty;
            SaveAll();
            return true;
        }

        public void RecordMegaResult(int levelIndex, int score, int stars, float completionTime)
        {
            EnsureCapacity(Mathf.Max(100, levelIndex + 1));
            if (levelIndex < 0 || levelIndex >= _data.megaCompleted.Length) return;
            _data.megaCompleted[levelIndex] = true;
            _data.megaBestScores[levelIndex] = Mathf.Max(_data.megaBestScores[levelIndex], score);
            _data.megaBestStars[levelIndex] = Mathf.Max(_data.megaBestStars[levelIndex], stars);
            float best = _data.megaBestTimes[levelIndex];
            if (completionTime > 0f && (best <= 0f || completionTime < best))
                _data.megaBestTimes[levelIndex] = completionTime;
            SetStars(levelIndex, stars);
            SaveAll();
        }

        public bool IsMegaCompleted(int levelIndex)
            => levelIndex >= 0 && levelIndex < _data.megaCompleted.Length && _data.megaCompleted[levelIndex];

        // ── Skins & Customization ─────────────────────────────────────────────

        public void UnlockSkin(string skinId)
        {
            if (string.IsNullOrEmpty(skinId)) return;
            if (_data.unlockedSkins == null) _data.unlockedSkins = new List<string>();
            if (!_data.unlockedSkins.Contains(skinId))
            {
                _data.unlockedSkins.Add(skinId);
                SaveAll();
            }
        }

        public bool IsSkinUnlocked(string skinId)
        {
            if (string.IsNullOrEmpty(skinId)) return false;
            return _data.unlockedSkins != null && _data.unlockedSkins.Contains(skinId);
        }

        public string GetEquippedSkin() => string.IsNullOrEmpty(_data.equippedSkin) ? "default" : _data.equippedSkin;

        public void EquipSkin(string skinId)
        {
            if (string.IsNullOrEmpty(skinId)) return;
            _data.equippedSkin = skinId;
            SaveAll();
        }

        // ── Arcade Tokens ─────────────────────────────────────────────────────

        public int GetArcadeTokens() => _data.arcadeTokens;
        public void AddArcadeTokens(int amount) { _data.arcadeTokens = Mathf.Max(0, _data.arcadeTokens + amount); SaveAll(); }
        public bool SpendArcadeTokens(int amount)
        {
            if (_data.arcadeTokens < amount) return false;
            _data.arcadeTokens -= amount;
            SaveAll();
            return true;
        }
    }
}
