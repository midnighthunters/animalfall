using UnityEngine;
using AnimalFall.Core.Goals;
using AnimalFall.Core.Levels;
using AnimalFall.Core.PowerUps;
using AnimalFall.Data.Schemas;

namespace AnimalFall.Data.MockData
{
    public static class MockDataConfig
    {
        public static UserProfile CreateMockUser()
        {
            return new UserProfile(
                uid: "mock_user_001",
                displayName: "AnimalHunter",
                email: "player@animalfall.dev"
            );
        }

        public static PlayerProgress CreateMockProgress()
        {
            return new PlayerProgress
            {
                highestCompletedLevel = 3,
                totalCoins = 1250,
                totalScore = 8500,
                gamesPlayed = 12
            };
        }

        public static PlayerInventory CreateMockInventory()
        {
            var inventory = new PlayerInventory();
            inventory.powerUps.Add(new OwnedPowerUp { powerUpId = "slow_time", quantity = 3 });
            inventory.powerUps.Add(new OwnedPowerUp { powerUpId = "magnet", quantity = 2 });
            inventory.powerUps.Add(new OwnedPowerUp { powerUpId = "extra_time", quantity = 5 });
            inventory.gems = 50;
            return inventory;
        }

        public static LeaderboardEntry[] CreateMockLeaderboard()
        {
            return new[]
            {
                new LeaderboardEntry("user_001", "FalconEye", 15200, 12),
                new LeaderboardEntry("user_002", "TigerPaw", 12800, 10),
                new LeaderboardEntry("user_003", "SwiftCat", 11500, 9),
                new LeaderboardEntry("user_004", "BoldBear", 9800, 8),
                new LeaderboardEntry("user_005", "QuickFox", 8200, 7),
                new LeaderboardEntry("user_006", "WildWolf", 7500, 6),
                new LeaderboardEntry("user_007", "GoldenEagle", 6100, 5),
                new LeaderboardEntry("user_008", "SilverStag", 5400, 4),
                new LeaderboardEntry("user_009", "IronHawk", 4200, 3),
                new LeaderboardEntry("user_010", "BronzeBull", 3100, 2),
            };
        }

        public static Goal CreateMockGoalForLevel(int levelNumber)
        {
            switch (levelNumber)
            {
                case 1: return new Goal { chickenCount = 5, dogCount = 3 };
                case 2: return new Goal { chickenCount = 5, dogCount = 3, cowCount = 2 };
                case 3: return new Goal { chickenCount = 6, dogCount = 4, cowCount = 3, catCount = 2 };
                case 4: return new Goal { chickenCount = 8, dogCount = 5, cowCount = 4, catCount = 3, monkeyCount = 2 };
                case 5: return new Goal { chickenCount = 10, dogCount = 6, cowCount = 5, catCount = 4, monkeyCount = 3, balloonCount = 2 };
                case 6: return new Goal { chickenCount = 12, dogCount = 8, cowCount = 6, catCount = 5, monkeyCount = 4, balloonCount = 3 };
                case 7: return new Goal { chickenCount = 14, dogCount = 10, cowCount = 8, catCount = 6, monkeyCount = 5, balloonCount = 4 };
                case 8: return new Goal { chickenCount = 16, dogCount = 12, cowCount = 10, catCount = 8, monkeyCount = 6, balloonCount = 5 };
                default: return new Goal { chickenCount = 5 + levelNumber, dogCount = 3 + levelNumber, cowCount = levelNumber };
            }
        }

        public struct ShopItemMock
        {
            public string id;
            public string displayName;
            public string description;
            public int coinCost;
            public PowerUpType type;
        }

        public static ShopItemMock[] CreateMockShopItems()
        {
            return new[]
            {
                new ShopItemMock
                {
                    id = "slow_time", displayName = "Slow Time",
                    description = "Slows all falling animals by 60% for 5 seconds",
                    coinCost = 100, type = PowerUpType.SlowTime
                },
                new ShopItemMock
                {
                    id = "magnet", displayName = "Magnet",
                    description = "Attracts nearby target animals for 4 seconds",
                    coinCost = 150, type = PowerUpType.Magnet
                },
                new ShopItemMock
                {
                    id = "multi_tap", displayName = "Multi Tap",
                    description = "Each tap counts as 3 taps for 5 seconds",
                    coinCost = 200, type = PowerUpType.MultiTap
                },
                new ShopItemMock
                {
                    id = "auto_tap", displayName = "Auto Tap",
                    description = "Automatically taps the nearest target at 3 taps/sec",
                    coinCost = 250, type = PowerUpType.AutoTap
                },
                new ShopItemMock
                {
                    id = "shield_breaker", displayName = "Shield Breaker",
                    description = "Instantly breaks the next shielded animal",
                    coinCost = 120, type = PowerUpType.ShieldBreaker
                },
                new ShopItemMock
                {
                    id = "bomb_clear", displayName = "Bomb Clear",
                    description = "Removes all bombs currently on screen",
                    coinCost = 180, type = PowerUpType.BombClear
                },
                new ShopItemMock
                {
                    id = "score_multiplier", displayName = "Score Boost",
                    description = "Doubles score earned for 6 seconds",
                    coinCost = 300, type = PowerUpType.ScoreMultiplier
                },
                new ShopItemMock
                {
                    id = "extra_time", displayName = "Extra Time",
                    description = "Adds 10 seconds to the level timer",
                    coinCost = 80, type = PowerUpType.ExtraTime
                }
            };
        }
    }
}
