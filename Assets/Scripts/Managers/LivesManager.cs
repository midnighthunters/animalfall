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

        private int      _currentLives;
        private bool     _timerRunning;
        private float    _regenTimer; // seconds until next life
        private SaveService _save;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Init(SaveService save)
        {
            _save = save;
            int storedLives = save?.GetLives() ?? MAX_LIVES;
            long nextUTC    = save?.GetNextLifeUTC() ?? 0L;

            // Offline catch-up
            double offlineMins = (DateTime.UtcNow - DateTimeOffset.FromUnixTimeSeconds(nextUTC).UtcDateTime).TotalMinutes;
            if (offlineMins > 0 && storedLives < MAX_LIVES)
            {
                storedLives = ComputeOfflineLives(storedLives, offlineMins);
                save?.SetLives(storedLives);
            }

            _currentLives = Mathf.Clamp(storedLives, 0, MAX_LIVES);
            if (_currentLives < MAX_LIVES) StartRegenTimer();
        }

        public bool HasLives()   => _currentLives > 0;
        public int  CurrentLives => _currentLives;

        public void UseLife()
        {
            if (_currentLives <= 0) return;
            _currentLives--;
            _save?.SetLives(_currentLives);
            if (_currentLives < MAX_LIVES && !_timerRunning) StartRegenTimer();
        }

        public void AddLife()
        {
            if (_currentLives >= MAX_LIVES) return;
            _currentLives++;
            _save?.SetLives(_currentLives);
        }

        /// <summary>Property-testable pure function (P9).</summary>
        public static int ComputeOfflineLives(int startLives, double offlineMinutes)
            => Mathf.Min(MAX_LIVES, startLives + Mathf.FloorToInt((float)(offlineMinutes / REGEN_MINUTES)));

        private void StartRegenTimer()
        {
            _timerRunning = true;
            _regenTimer   = REGEN_MINUTES * 60f;
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
                AddLife();
                if (_currentLives < MAX_LIVES) _regenTimer = REGEN_MINUTES * 60f;
                else _timerRunning = false;
            }
        }
    }
}
