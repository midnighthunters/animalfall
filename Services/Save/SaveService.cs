using System.Collections.Generic;
using UnityEngine;

namespace AnimalFall.Services.Save
{
    public class SaveService : MonoBehaviour
    {
        public static SaveService Instance { get; private set; }

        private const string CoinsKey = "player_coins";
        private const string HighScoreKey = "player_highscore";
        private const string SfxVolumeKey = "settings_sfx";
        private const string MusicVolumeKey = "settings_music";
        private const string UnlockedSkinsKey = "unlocked_skins";
        private const string EquippedSkinKey = "equipped_skin";
        private const string TotalAnimalsCollectedKey = "total_animals";
        private const string TotalLevelsPlayedKey = "total_levels";

        private HashSet<string> unlockedSkins;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadSkins();
        }

        // Coins
        public int GetCoins() => PlayerPrefs.GetInt(CoinsKey, 0);

        public void AddCoins(int amount)
        {
            int current = GetCoins() + amount;
            PlayerPrefs.SetInt(CoinsKey, Mathf.Max(0, current));
            PlayerPrefs.Save();
        }

        public void SpendCoins(int amount)
        {
            int current = GetCoins() - amount;
            PlayerPrefs.SetInt(CoinsKey, Mathf.Max(0, current));
            PlayerPrefs.Save();
        }

        // High Score
        public int GetHighScore() => PlayerPrefs.GetInt(HighScoreKey, 0);

        public void SetHighScore(int score)
        {
            if (score > GetHighScore())
            {
                PlayerPrefs.SetInt(HighScoreKey, score);
                PlayerPrefs.Save();
            }
        }

        // Audio Settings
        public float GetSFXVolume() => PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        public void SetSFXVolume(float vol) { PlayerPrefs.SetFloat(SfxVolumeKey, vol); PlayerPrefs.Save(); }

        public float GetMusicVolume() => PlayerPrefs.GetFloat(MusicVolumeKey, 0.5f);
        public void SetMusicVolume(float vol) { PlayerPrefs.SetFloat(MusicVolumeKey, vol); PlayerPrefs.Save(); }

        // Skins
        public void UnlockSkin(string skinId)
        {
            if (string.IsNullOrEmpty(skinId)) return;
            unlockedSkins.Add(skinId);
            SaveSkins();
        }

        public bool IsSkinUnlocked(string skinId)
        {
            return unlockedSkins != null && unlockedSkins.Contains(skinId);
        }

        public string GetEquippedSkin()
        {
            return PlayerPrefs.GetString(EquippedSkinKey, "default");
        }

        public void EquipSkin(string skinId)
        {
            PlayerPrefs.SetString(EquippedSkinKey, skinId);
            PlayerPrefs.Save();
        }

        // Stats
        public int GetTotalAnimalsCollected() => PlayerPrefs.GetInt(TotalAnimalsCollectedKey, 0);

        public void AddAnimalsCollected(int count)
        {
            int total = GetTotalAnimalsCollected() + count;
            PlayerPrefs.SetInt(TotalAnimalsCollectedKey, total);
            PlayerPrefs.Save();
        }

        public int GetTotalLevelsPlayed() => PlayerPrefs.GetInt(TotalLevelsPlayedKey, 0);

        public void IncrementLevelsPlayed()
        {
            int total = GetTotalLevelsPlayed() + 1;
            PlayerPrefs.SetInt(TotalLevelsPlayedKey, total);
            PlayerPrefs.Save();
        }

        // Skin persistence
        private void LoadSkins()
        {
            unlockedSkins = new HashSet<string>();
            string raw = PlayerPrefs.GetString(UnlockedSkinsKey, "");
            if (!string.IsNullOrEmpty(raw))
            {
                string[] ids = raw.Split(',');
                foreach (var id in ids)
                {
                    string trimmed = id.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        unlockedSkins.Add(trimmed);
                }
            }
        }

        private void SaveSkins()
        {
            string joined = string.Join(",", unlockedSkins);
            PlayerPrefs.SetString(UnlockedSkinsKey, joined);
            PlayerPrefs.Save();
        }

        // Clear
        public void ClearAllData()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            unlockedSkins?.Clear();
        }
    }
}
