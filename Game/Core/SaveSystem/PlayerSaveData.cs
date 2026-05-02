// ============================================================
//  PlayerSaveData.cs  –  Animal Fall
//  The canonical, serialisable schema for all player progress.
//  Replaces every scattered PlayerPrefs.GetInt / SetInt.
// ============================================================

using System;
using System.Collections.Generic;

[Serializable]
public class PlayerSaveData
{
    // ── Identity ───────────────────────────────────────────────
    public string   playerId        = "";   // Firebase UID (empty = guest)
    public string   displayName     = "Player";
    public string   avatarKey       = "default";

    // ── Economy ────────────────────────────────────────────────
    public int      coins           = 0;
    public int      gems            = 0;

    // ── Progress ───────────────────────────────────────────────
    /// <summary>Index of the highest unlocked level (0-based).</summary>
    public int      highestUnlockedLevel = 0;

    /// <summary>Per-level best scores  key = levelIndex.</summary>
    public SerializableDictionary<int, int> levelBestScores = new();

    /// <summary>Per-level star ratings (0-3).</summary>
    public SerializableDictionary<int, int> levelStars = new();

    // ── Inventory ──────────────────────────────────────────────
    /// <summary>PowerUp type → remaining uses.</summary>
    public SerializableDictionary<string, int> powerUpInventory = new();

    // ── Settings ──────────────────────────────────────────────
    public float    masterVolume    = 1f;
    public float    sfxVolume       = 1f;
    public float    musicVolume     = 0.6f;
    public bool     vibrateEnabled  = true;
    public bool     notifEnabled    = true;

    // ── Meta ───────────────────────────────────────────────────
    public string   saveVersion     = "1.0";
    public string   lastSaveUtc     = "";

    // ── Helpers ────────────────────────────────────────────────
    public void TouchSaveTime() =>
        lastSaveUtc = DateTime.UtcNow.ToString("o");

    public int GetBestScore(int levelIndex) =>
        levelBestScores.TryGetValue(levelIndex, out int v) ? v : 0;

    public void SetBestScore(int levelIndex, int score)
    {
        if (!levelBestScores.TryGetValue(levelIndex, out int prev) || score > prev)
            levelBestScores[levelIndex] = score;
    }

    public int GetStars(int levelIndex) =>
        levelStars.TryGetValue(levelIndex, out int v) ? v : 0;

    public void SetStars(int levelIndex, int stars)
    {
        if (!levelStars.TryGetValue(levelIndex, out int prev) || stars > prev)
            levelStars[levelIndex] = stars;
    }

    public int GetPowerUpCount(string key) =>
        powerUpInventory.TryGetValue(key, out int v) ? v : 0;

    public void AddPowerUp(string key, int amount = 1)
    {
        powerUpInventory[key] = GetPowerUpCount(key) + amount;
    }

    public bool SpendPowerUp(string key)
    {
        int count = GetPowerUpCount(key);
        if (count <= 0) return false;
        powerUpInventory[key] = count - 1;
        return true;
    }
}

// ── Minimal serialisable dictionary wrapper ───────────────────
// Unity's JsonUtility cannot serialize Dictionary<> natively.
[Serializable]
public class SerializableDictionary<TKey, TValue>
{
    [Serializable] private struct Pair { public TKey key; public TValue value; }
    private System.Collections.Generic.List<Pair> _pairs = new();
    private System.Collections.Generic.Dictionary<TKey, TValue> _dict = new();

    private void Rebuild()
    {
        _dict.Clear();
        foreach (var p in _pairs) _dict[p.key] = p.value;
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        if (_dict.Count != _pairs.Count) Rebuild();
        return _dict.TryGetValue(key, out value);
    }

    public TValue this[TKey key]
    {
        get  { if (_dict.Count != _pairs.Count) Rebuild(); return _dict[key]; }
        set
        {
            if (_dict.Count != _pairs.Count) Rebuild();
            _dict[key] = value;
            // sync list
            for (int i = 0; i < _pairs.Count; i++)
                if (_pairs[i].key.Equals(key)) { _pairs[i] = new Pair { key = key, value = value }; return; }
            _pairs.Add(new Pair { key = key, value = value });
        }
    }

    public bool ContainsKey(TKey key)
    {
        if (_dict.Count != _pairs.Count) Rebuild();
        return _dict.ContainsKey(key);
    }
}
