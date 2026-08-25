using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimalFall.Core.Events
{
    public static class EventScheduler
    {
        private static readonly DailyQuest[] questTemplates = new[]
        {
            new DailyQuest("q_collect_20", "Collect 20 animals", "animals_collected", 20, 50),
            new DailyQuest("q_collect_50", "Collect 50 animals", "animals_collected", 50, 100),
            new DailyQuest("q_score_3000", "Score 3000 points", "score_earned", 3000, 75),
            new DailyQuest("q_combo_5", "Get a 5x combo", "max_combo", 5, 60),
            new DailyQuest("q_levels_3", "Complete 3 levels", "levels_completed", 3, 80),
            new DailyQuest("q_powerup_2", "Use 2 power-ups", "powerups_used", 2, 40),
            new DailyQuest("q_no_bombs", "Complete a level without tapping bombs", "bomb_free_levels", 1, 120),
            new DailyQuest("q_golden_1", "Collect a golden animal", "golden_collected", 1, 90),
        };

        public static List<DailyQuest> GenerateDailyQuests(int count = 3)
        {
            var result = new List<DailyQuest>();
            var indices = new List<int>();

            for (int i = 0; i < questTemplates.Length; i++)
                indices.Add(i);

            int daysSeed = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 86400);
            System.Random rng = new System.Random(daysSeed);

            for (int i = indices.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int tmp = indices[i];
                indices[i] = indices[j];
                indices[j] = tmp;
            }

            for (int i = 0; i < Mathf.Min(count, indices.Count); i++)
            {
                var template = questTemplates[indices[i]];
                result.Add(new DailyQuest(
                    template.QuestId,
                    template.Description,
                    template.TrackingKey,
                    template.TargetValue,
                    template.RewardCoins));
            }

            return result;
        }

        public static float GetSecondsUntilDailyReset()
        {
            DateTime now = DateTime.UtcNow;
            DateTime nextMidnight = now.Date.AddDays(1);
            return (float)(nextMidnight - now).TotalSeconds;
        }
    }
}
