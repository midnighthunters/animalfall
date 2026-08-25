using System;

namespace AnimalFall.Core.Events
{
    [Serializable]
    public class DailyQuest
    {
        public string QuestId;
        public string Description;
        public string TrackingKey;
        public int TargetValue;
        public int RewardCoins;
        public int CurrentProgress;
        public bool IsCompleted;
        public bool IsClaimed;

        public float ProgressPercent =>
            TargetValue > 0 ? (float)CurrentProgress / TargetValue : 0f;

        public DailyQuest(string id, string desc, string key, int target, int reward)
        {
            QuestId = id;
            Description = desc;
            TrackingKey = key;
            TargetValue = target;
            RewardCoins = reward;
            CurrentProgress = 0;
            IsCompleted = false;
            IsClaimed = false;
        }

        public void AddProgress(int amount)
        {
            CurrentProgress += amount;
            if (CurrentProgress >= TargetValue)
                IsCompleted = true;
        }
    }
}
