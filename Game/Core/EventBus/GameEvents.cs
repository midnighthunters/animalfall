// ============================================================
//  GameEvents.cs  –  Animal Fall  |  Centralized Event Bus
//  All game-wide signals are defined here as strongly-typed
//  structs.  No string keys, no reflection, zero allocations
//  on the hot path (value-type payloads).
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

// ── 1. Event payload structs ──────────────────────────────────

public struct OnLevelStarted      { public int levelIndex; }
public struct OnLevelCompleted    { public int levelIndex; public int score; public int coinsEarned; }
public struct OnLevelFailed       { public int levelIndex; }
public struct OnAnimalCollected   { public AnimalSpecies species; public int points; public Vector3 worldPos; }
public struct OnAnimalMissed      { public AnimalSpecies species; }
public struct OnComboUpdated      { public int streak; public float multiplier; }
public struct OnScoreChanged      { public int newScore; }
public struct OnCoinsChanged      { public int newTotal; }
public struct OnTimerTick         { public float remaining; }
public struct OnPowerUpActivated  { public PowerUpType type; public float duration; }
public struct OnFirebaseAuthReady { public bool isSignedIn; public string userId; }
public struct OnSaveDataLoaded    { }
public struct OnPoolWarmed        { }

// ── 2. The bus itself ─────────────────────────────────────────

/// <summary>
/// Dead-simple, allocation-free event bus.  
/// Usage:  EventBus.Publish(new OnScoreChanged { newScore = 999 });
///         EventBus.Subscribe&lt;OnScoreChanged&gt;(OnScoreChangedHandler);
///         EventBus.Unsubscribe&lt;OnScoreChanged&gt;(OnScoreChangedHandler);
/// </summary>
public static class EventBus
{
    // Dictionary keyed by event type; value is a non-generic delegate list
    private static readonly Dictionary<Type, object> _handlers = new(32);

    // ── Subscribe ──────────────────────────────────────────────
    public static void Subscribe<T>(Action<T> handler) where T : struct
    {
        Type t = typeof(T);
        if (_handlers.TryGetValue(t, out object existing))
            _handlers[t] = (Action<T>)existing + handler;
        else
            _handlers[t] = handler;
    }

    // ── Unsubscribe ────────────────────────────────────────────
    public static void Unsubscribe<T>(Action<T> handler) where T : struct
    {
        Type t = typeof(T);
        if (_handlers.TryGetValue(t, out object existing))
        {
            var combined = (Action<T>)existing - handler;
            if (combined == null) _handlers.Remove(t);
            else _handlers[t] = combined;
        }
    }

    // ── Publish ────────────────────────────────────────────────
    public static void Publish<T>(T evt) where T : struct
    {
        if (_handlers.TryGetValue(typeof(T), out object existing))
            ((Action<T>)existing)?.Invoke(evt);
    }

    // ── Utility ────────────────────────────────────────────────
    /// <summary>Clear all handlers — call on scene unload if needed.</summary>
    public static void Clear() => _handlers.Clear();
}
