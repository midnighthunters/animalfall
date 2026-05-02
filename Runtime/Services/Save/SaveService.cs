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

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

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

        public int GetHighScore() => PlayerPrefs.GetInt(HighScoreKey, 0);

        public void SetHighScore(int score)
        {
            if (score > GetHighScore())
            {
                PlayerPrefs.SetInt(HighScoreKey, score);
                PlayerPrefs.Save();
            }
        }

        public float GetSFXVolume() => PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        public void SetSFXVolume(float vol) { PlayerPrefs.SetFloat(SfxVolumeKey, vol); PlayerPrefs.Save(); }

        public float GetMusicVolume() => PlayerPrefs.GetFloat(MusicVolumeKey, 0.5f);
        public void SetMusicVolume(float vol) { PlayerPrefs.SetFloat(MusicVolumeKey, vol); PlayerPrefs.Save(); }

        public void ClearAllData()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
    }
}
