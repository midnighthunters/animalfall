using System.Collections.Generic;
using UnityEngine;
using AnimalFall.Core.Events;
using AnimalFall.Services.Save;

namespace AnimalFall.Managers
{
    public class EventManager : MonoBehaviour
    {
        public static EventManager Instance { get; private set; }

        [SerializeField] private GameEvent[] eventDefinitions;

        public List<DailyQuest> ActiveQuests { get; private set; }
        public List<GameEvent> ActiveEvents { get; private set; }
        public float TimeUntilDailyReset => EventScheduler.GetSecondsUntilDailyReset();

        private int lastQuestDay = -1;
        private const string QuestDayKey = "quest_day";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            RefreshQuests();
            LoadActiveEvents();
        }

        private void Update()
        {
            int currentDay = GetCurrentDay();
            if (currentDay != lastQuestDay)
                RefreshQuests();

            UpdateEventTimers();
        }

        private void RefreshQuests()
        {
            int currentDay = GetCurrentDay();
            int savedDay = PlayerPrefs.GetInt(QuestDayKey, -1);

            if (savedDay != currentDay)
            {
                ActiveQuests = EventScheduler.GenerateDailyQuests(3);
                lastQuestDay = currentDay;
                PlayerPrefs.SetInt(QuestDayKey, currentDay);
                PlayerPrefs.Save();
            }
            else if (ActiveQuests == null)
            {
                ActiveQuests = EventScheduler.GenerateDailyQuests(3);
                lastQuestDay = currentDay;
            }
        }

        private void LoadActiveEvents()
        {
            ActiveEvents = new List<GameEvent>();
            if (eventDefinitions == null) return;

            foreach (var evt in eventDefinitions)
            {
                if (evt == null) continue;
                evt.RemainingSeconds = evt.durationHours * 3600f;
                ActiveEvents.Add(evt);
            }
        }

        private void UpdateEventTimers()
        {
            if (ActiveEvents == null) return;

            for (int i = ActiveEvents.Count - 1; i >= 0; i--)
            {
                ActiveEvents[i].RemainingSeconds -= Time.deltaTime;
                if (ActiveEvents[i].RemainingSeconds <= 0)
                    ActiveEvents.RemoveAt(i);
            }
        }

        public void CheckQuestProgress(string trackingKey, int value)
        {
            if (ActiveQuests == null) return;

            foreach (var quest in ActiveQuests)
            {
                if (quest.TrackingKey == trackingKey && !quest.IsCompleted)
                    quest.AddProgress(value);
            }
        }

        public void ClaimReward(string questId)
        {
            if (ActiveQuests == null) return;

            foreach (var quest in ActiveQuests)
            {
                if (quest.QuestId == questId && quest.IsCompleted && !quest.IsClaimed)
                {
                    quest.IsClaimed = true;
                    SaveService.Instance?.AddCoins(quest.RewardCoins);
                    break;
                }
            }
        }

        private int GetCurrentDay()
        {
            return (int)(System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 86400);
        }
    }
}
