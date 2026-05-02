// ============================================================
//  SaveManager.cs  –  Animal Fall  (REPLACEMENT)
//  Centralised save / load that:
//    • Writes a JSON file to Application.persistentDataPath
//    • Syncs with Firebase Realtime DB when signed in
//    • Replaces every PlayerPrefs.GetInt/SetInt in the project
//    • Emits EventBus signals on data change
// ============================================================

using System;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────
    public static SaveManager Instance { get; private set; }

    // ── Config ─────────────────────────────────────────────────
    private const string kFileName    = "animal_fall_save.json";
    private const float  kAutoSaveInterval = 60f;   // seconds

    // ── State ──────────────────────────────────────────────────
    public PlayerSaveData Data { get; private set; } = new();

    private string _savePath;
    private float  _autoSaveTimer;
    private bool   _isDirty;

    // ── Lifecycle ──────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _savePath = Path.Combine(Application.persistentDataPath, kFileName);
        Load();
    }

    private void Update()
    {
        if (!_isDirty) return;
        _autoSaveTimer += Time.unscaledDeltaTime;
        if (_autoSaveTimer >= kAutoSaveInterval)
        {
            _autoSaveTimer = 0f;
            Save();
        }
    }

    private void OnApplicationPause(bool pause) { if (pause && _isDirty) Save(); }
    private void OnApplicationQuit()            { if (_isDirty) Save(); }

    // ── Load ───────────────────────────────────────────────────
    public void Load()
    {
        if (File.Exists(_savePath))
        {
            try
            {
                string json = File.ReadAllText(_savePath);
                Data = JsonUtility.FromJson<PlayerSaveData>(json) ?? new PlayerSaveData();
                Debug.Log($"[SaveManager] Loaded save data (v{Data.saveVersion}).");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to parse save file – creating fresh. {ex.Message}");
                Data = new PlayerSaveData();
            }
        }
        else
        {
            Data = new PlayerSaveData();
            Debug.Log("[SaveManager] No save file found – starting fresh.");
        }

        EventBus.Publish(new OnSaveDataLoaded());
    }

    // ── Save ───────────────────────────────────────────────────
    public void Save()
    {
        try
        {
            Data.TouchSaveTime();
            string json = JsonUtility.ToJson(Data, prettyPrint: true);
            File.WriteAllText(_savePath, json);
            _isDirty = false;
            Debug.Log($"[SaveManager] Saved to {_savePath}");

            // Push to Firebase (non-blocking)
            FirebaseManager.Instance?.PushSaveData(Data);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Save failed: {ex.Message}");
        }
    }

    private void MarkDirty() { _isDirty = true; _autoSaveTimer = 0f; }

    // ── Economy API (drop-in replacements for old PlayerPrefs) ─

    public int  GetCoins()      => Data.coins;
    public int  GetGems()       => Data.gems;

    public void AddCoins(int amount)
    {
        Data.coins = Mathf.Max(0, Data.coins + amount);
        MarkDirty();
        EventBus.Publish(new OnCoinsChanged { newTotal = Data.coins });
    }

    public bool SpendCoins(int amount)
    {
        if (Data.coins < amount) return false;
        Data.coins -= amount;
        MarkDirty();
        EventBus.Publish(new OnCoinsChanged { newTotal = Data.coins });
        return true;
    }

    public void AddGems(int amount)
    {
        Data.gems = Mathf.Max(0, Data.gems + amount);
        MarkDirty();
    }

    // ── Progress API ───────────────────────────────────────────

    public int GetHighestUnlockedLevel()     => Data.highestUnlockedLevel;

    public void UnlockNextLevel(int currentIndex)
    {
        if (currentIndex + 1 > Data.highestUnlockedLevel)
        {
            Data.highestUnlockedLevel = currentIndex + 1;
            MarkDirty();
        }
    }

    public void RecordLevelResult(int levelIndex, int score, int stars)
    {
        Data.SetBestScore(levelIndex, score);
        Data.SetStars(levelIndex, stars);
        MarkDirty();
    }

    // ── Settings ───────────────────────────────────────────────
    public void SetVolumes(float master, float sfx, float music)
    {
        Data.masterVolume = master;
        Data.sfxVolume    = sfx;
        Data.musicVolume  = music;
        MarkDirty();
        AudioManager.Instance?.ApplyVolumes();
    }

    // ── Debug / Reset ──────────────────────────────────────────
    [ContextMenu("Reset All Progress")]
    public void ResetAllProgress()
    {
        Data = new PlayerSaveData();
        Save();
        Debug.Log("[SaveManager] Progress reset.");
    }
}
