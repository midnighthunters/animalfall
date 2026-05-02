using System;
using UnityEngine;

namespace AnimalFall.Managers
{
    public class LivesManager : MonoBehaviour
    {
        public static LivesManager Instance { get; private set; }

        [SerializeField] private int maxLives = 5;
        [SerializeField] private float regenTimeSeconds = 1800f;

        public int MaxLives => maxLives;
        public int CurrentLives { get; private set; }
        public float TimeUntilNextLife { get; private set; }
        public bool IsRegenerating => CurrentLives < maxLives;

        public event Action<int> OnLivesChanged;

        private float regenTimer;
        private const string LivesKey = "player_lives";
        private const string LivesTimestampKey = "player_lives_timestamp";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadLives();
        }

        private void Update()
        {
            if (CurrentLives >= maxLives) return;

            regenTimer -= Time.unscaledDeltaTime;
            TimeUntilNextLife = Mathf.Max(0f, regenTimer);

            if (regenTimer <= 0f)
            {
                AddLife();
                regenTimer = regenTimeSeconds;
                SaveLives();
            }
        }

        public bool HasLives() => CurrentLives > 0;

        public void UseLife()
        {
            if (CurrentLives <= 0) return;
            CurrentLives--;

            if (CurrentLives == maxLives - 1)
                regenTimer = regenTimeSeconds;

            SaveLives();
            OnLivesChanged?.Invoke(CurrentLives);
        }

        public void AddLife(int count = 1)
        {
            CurrentLives = Mathf.Min(CurrentLives + count, maxLives);
            SaveLives();
            OnLivesChanged?.Invoke(CurrentLives);
        }

        public void RefillLives()
        {
            CurrentLives = maxLives;
            SaveLives();
            OnLivesChanged?.Invoke(CurrentLives);
        }

        private void LoadLives()
        {
            CurrentLives = PlayerPrefs.GetInt(LivesKey, maxLives);
            long savedTimestamp = long.Parse(PlayerPrefs.GetString(LivesTimestampKey, "0"));

            if (savedTimestamp > 0 && CurrentLives < maxLives)
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                float elapsed = now - savedTimestamp;
                int livesRecovered = Mathf.FloorToInt(elapsed / regenTimeSeconds);

                if (livesRecovered > 0)
                {
                    CurrentLives = Mathf.Min(CurrentLives + livesRecovered, maxLives);
                    float remainder = elapsed - livesRecovered * regenTimeSeconds;
                    regenTimer = regenTimeSeconds - remainder;
                }
                else
                {
                    regenTimer = regenTimeSeconds - elapsed;
                }
            }
            else
            {
                regenTimer = regenTimeSeconds;
            }

            regenTimer = Mathf.Max(0f, regenTimer);
        }

        private void SaveLives()
        {
            PlayerPrefs.SetInt(LivesKey, CurrentLives);
            PlayerPrefs.SetString(LivesTimestampKey,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            PlayerPrefs.Save();
        }
    }
}
