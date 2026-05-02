using System;
using UnityEngine;

namespace AnimalFall.Core.Events
{
    public enum EventType
    {
        DailyQuest,
        TimedEvent,
        SpecialChallenge,
        WeekendBonus
    }

    [CreateAssetMenu(fileName = "NewGameEvent", menuName = "AnimalFall/Game Event")]
    public class GameEvent : ScriptableObject
    {
        [Header("Identity")]
        public string eventId;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        public EventType type;

        [Header("Requirements")]
        public int targetValue;
        public string trackingKey;

        [Header("Rewards")]
        public int rewardCoins;
        public int rewardGems;

        [Header("Timing")]
        public float durationHours = 24f;

        [NonSerialized] public float RemainingSeconds;
        [NonSerialized] public int CurrentProgress;
        [NonSerialized] public bool IsCompleted;
        [NonSerialized] public bool IsClaimed;
    }
}
