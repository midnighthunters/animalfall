using System;
using System.Collections.Generic;

namespace AnimalFall.Data.Schemas
{
    /// <summary>
    /// Firestore: users/{uid}
    /// </summary>
    [Serializable]
    public class UserProfile
    {
        public string uid;
        public string displayName;
        public string email;
        public string avatarUrl;
        public long createdAt;
        public long lastLoginAt;

        public UserProfile() { }

        public UserProfile(string uid, string displayName, string email)
        {
            this.uid = uid;
            this.displayName = displayName;
            this.email = email;
            this.avatarUrl = "";
            this.createdAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            this.lastLoginAt = this.createdAt;
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                { "uid", uid },
                { "displayName", displayName },
                { "email", email },
                { "avatarUrl", avatarUrl },
                { "createdAt", createdAt },
                { "lastLoginAt", lastLoginAt }
            };
        }
    }

    /// <summary>
    /// Firestore: users/{uid}/progress/data
    /// </summary>
    [Serializable]
    public class PlayerProgress
    {
        public int highestCompletedLevel;
        public int totalCoins;
        public int totalScore;
        public int gamesPlayed;
        public long lastPlayedAt;

        public PlayerProgress()
        {
            highestCompletedLevel = 0;
            totalCoins = 0;
            totalScore = 0;
            gamesPlayed = 0;
            lastPlayedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                { "highestCompletedLevel", highestCompletedLevel },
                { "totalCoins", totalCoins },
                { "totalScore", totalScore },
                { "gamesPlayed", gamesPlayed },
                { "lastPlayedAt", lastPlayedAt }
            };
        }
    }

    /// <summary>
    /// Firestore: leaderboard/{uid}
    /// </summary>
    [Serializable]
    public class LeaderboardEntry
    {
        public string uid;
        public string displayName;
        public int highScore;
        public int highestLevel;
        public long updatedAt;

        public LeaderboardEntry() { }

        public LeaderboardEntry(string uid, string displayName, int highScore, int highestLevel)
        {
            this.uid = uid;
            this.displayName = displayName;
            this.highScore = highScore;
            this.highestLevel = highestLevel;
            this.updatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                { "uid", uid },
                { "displayName", displayName },
                { "highScore", highScore },
                { "highestLevel", highestLevel },
                { "updatedAt", updatedAt }
            };
        }
    }

    /// <summary>
    /// Firestore: users/{uid}/inventory/items
    /// </summary>
    [Serializable]
    public class PlayerInventory
    {
        public List<OwnedPowerUp> powerUps;
        public List<string> unlockedSkins;
        public int gems;

        public PlayerInventory()
        {
            powerUps = new List<OwnedPowerUp>();
            unlockedSkins = new List<string>();
            gems = 0;
        }

        public Dictionary<string, object> ToDictionary()
        {
            var powerUpList = new List<Dictionary<string, object>>();
            foreach (var p in powerUps)
                powerUpList.Add(p.ToDictionary());

            return new Dictionary<string, object>
            {
                { "powerUps", powerUpList },
                { "unlockedSkins", unlockedSkins },
                { "gems", gems }
            };
        }
    }

    [Serializable]
    public class OwnedPowerUp
    {
        public string powerUpId;
        public int quantity;

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                { "powerUpId", powerUpId },
                { "quantity", quantity }
            };
        }
    }
}
