// Task 6.10 — LivesManager: lives cap, regen timer, offline catch-up
using System;
using UnityEngine;
using AnimalFall.Services;

namespace AnimalFall.Managers
{
    public class LivesManager : MonoBehaviour
    {
        public static LivesManager Instance { get; private set; }

        private const int   MAX_LIVES      = 5;
        private const int   REGEN_MINUTES  = 30;
        private const long  REGEN_SECONDS  = REGEN_MINUTES * 60L;

        private int      _currentLives;
        private bool     _timerRunning;
        private float    _regenTimer; // seconds until next life
        private SaveService _save;

        /// <summary>Raised after a life is spent, restored, or regenerated.</summary>
        public event Action<int> OnLivesChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // All active Awake methods finish before Start, so this avoids the bootstrap-order race.
            Init(FindFirstObjectByType<SaveService>());
        }

        public void Init(SaveService save)
        {
            _save = save;
            int storedLives = Mathf.Clamp(save?.GetLives() ?? MAX_LIVES, 0, MAX_LIVES);
            long nextUTC = save?.GetNextLifeUTC() ?? 0L;
            long nowUTC = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // A valid timestamp is the deadline for the *next* life.  Older
            // saves did not write one, so do not treat Unix epoch as elapsed
            // time and accidentally grant a full set of lives.
            if (storedLives < MAX_LIVES)
            {
                if (nextUTC <= 0L)
                {
                    nextUTC = nowUTC + REGEN_SECONDS;
                }
                else if (nowUTC >= nextUTC)
                {
                    long regenerated = 1L + ((nowUTC - nextUTC) / REGEN_SECONDS);
                    storedLives = Mathf.Min(MAX_LIVES, storedLives + (int)regenerated);
                    nextUTC = storedLives >= MAX_LIVES
                        ? 0L
                        : nextUTC + (regenerated * REGEN_SECONDS);
                }
            }
            else
            {
                nextUTC = 0L;
            }

            _currentLives = storedLives;
            SaveState(nextUTC);
            if (_currentLives < MAX_LIVES)
            {
                StartRegenTimer(nextUTC, nowUTC);
            }
            else
            {
                _timerRunning = false;
                _regenTimer = 0f;
            }
        }

        public bool HasLives() => _currentLives > 0;
        public int CurrentLives => _currentLives;

        public void UseLife()
        {
            if (_currentLives <= 0) return;
            _currentLives--;
            if (!_timerRunning) StartRegenTimer();
            SaveCurrentLives();
            OnLivesChanged?.Invoke(_currentLives);
        }

        public void AddLife()
        {
            if (_currentLives >= MAX_LIVES) return;
            _currentLives++;
            if (_currentLives >= MAX_LIVES)
            {
                _timerRunning = false;
                _regenTimer = 0f;
            }
            SaveCurrentLives();
            OnLivesChanged?.Invoke(_currentLives);
        }

        /// <summary>Restores the player to the life cap and persists the result immediately.</summary>
        public void Refill()
        {
            _currentLives = MAX_LIVES;
            _timerRunning = false;
            _regenTimer = 0f;
            SaveCurrentLives();
            OnLivesChanged?.Invoke(_currentLives);
        }

        private void SaveCurrentLives()
        {
            if (_save == null) _save = FindFirstObjectByType<SaveService>();
            if (_save == null) return;

            long nextUTC = _currentLives >= MAX_LIVES
                ? 0L
                : DateTimeOffset.UtcNow.ToUnixTimeSeconds() + Mathf.CeilToInt(Mathf.Max(0f, _regenTimer));
            SaveState(nextUTC);
        }

        /// <summary>Property-testable pure function (P9).</summary>
        public static int ComputeOfflineLives(int startLives, double offlineMinutes)
            => Mathf.Min(MAX_LIVES, startLives + Mathf.FloorToInt((float)(offlineMinutes / REGEN_MINUTES)));

        private void SaveState(long nextUTC)
        {
            if (_save == null) _save = FindFirstObjectByType<SaveService>();
            if (_save == null) return;

            _save.SetLives(_currentLives);
            _save.SetNextLifeUTC(nextUTC);
            _save.SaveAll();
        }

        private void StartRegenTimer()
        {
            _timerRunning = true;
            _regenTimer   = REGEN_MINUTES * 60f;
        }

        private void StartRegenTimer(long nextUTC, long nowUTC)
        {
            _timerRunning = true;
            _regenTimer = Mathf.Max(1f, nextUTC - nowUTC);
        }

        private void Update()
        {
            if (!_timerRunning || _currentLives >= MAX_LIVES)
            {
                if (_currentLives >= MAX_LIVES) _timerRunning = false;
                return;
            }

            _regenTimer -= Time.deltaTime;
            if (_regenTimer <= 0f)
            {
                // Move the timer to the following regeneration deadline before
                // saving the newly awarded life.
                _regenTimer = REGEN_MINUTES * 60f;
                AddLife();
            }
        }
    }
}
