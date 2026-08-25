// GameEvents — static C# Action event bus
using System;
using UnityEngine;
using AnimalFall.Core.Animals;
using AnimalFall.Core.Hindrances;

namespace AnimalFall.Managers
{
    /// <summary>All invocations use null-conditional ?.Invoke() — silent no-op when no subscribers.</summary>
    public static class GameEvents
    {
        // ── Animal events ──────────────────────────────────────────────────────
        /// <summary>species, type, world position</summary>
        public static Action<AnimalSpecies, AnimalType, Vector3> OnAnimalCollected;
        public static Action                      OnWrongTap;
        public static Action<Vector3>             OnBombTapped;
        public static Action                      OnAnimalMissed;
        public static Action                      OnCursedSkullTapped;

        // ── Level flow ─────────────────────────────────────────────────────────
        public static Action<int>  OnLevelStarted;
        public static Action       OnLevelWon;
        public static Action       OnLevelFailed;
        public static Action       OnTimerWarning;

        // ── Score / combo ──────────────────────────────────────────────────────
        public static Action<int>         OnScoreChanged;
        public static Action<int, float>  OnComboChanged;

        // ── Hindrance ─────────────────────────────────────────────────────────
        public static Action<HindranceType> OnHindranceActivated;
        public static Action<HindranceType> OnHindranceDeactivated;

        // ── MegaLevel ─────────────────────────────────────────────────────────
        public static Action<int, int> OnVillainPhaseChanged;

        // ── Input ─────────────────────────────────────────────────────────────
        public static Action<Vector2> OnScreenTapped;
        public static Action<Vector2> OnSwipeDetected;
        public static Action<Vector2, Vector2> OnSwipeDetailed;
        public static Action<Animal> OnPairedAnimalTapped;

        // ── Stars ──────────────────────────────────────────────────────────────
        public static Action<int, int, float, float> OnStarsCalculated;

        // ── Audio ─────────────────────────────────────────────────────────────
        public static Action<SfxType>         OnSfxRequested;
        public static Action<SfxType, float>  OnSfxRequestedPitch;

        public static void ClearAll()
        {
            OnAnimalCollected   = null;
            OnWrongTap          = null;
            OnBombTapped        = null;
            OnAnimalMissed      = null;
            OnCursedSkullTapped = null;
            OnLevelStarted      = null;
            OnLevelWon          = null;
            OnLevelFailed       = null;
            OnTimerWarning      = null;
            OnScoreChanged      = null;
            OnComboChanged      = null;
            OnHindranceActivated   = null;
            OnHindranceDeactivated = null;
            OnVillainPhaseChanged  = null;
            OnScreenTapped      = null;
            OnSwipeDetected     = null;
            OnSwipeDetailed     = null;
            OnPairedAnimalTapped = null;
            OnStarsCalculated   = null;
            OnSfxRequested      = null;
            OnSfxRequestedPitch = null;
        }
    }

    public enum SfxType
    {
        Collect,
        WrongTap,
        Explosion,
        ComboUp,
        MegaCombo,
        LevelWin,
        LevelLose,
        HindranceActivate,
        ShieldHit,
        PowerUpActivate,
        TimerWarning
    }
}
