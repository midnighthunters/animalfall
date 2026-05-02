using UnityEngine;
using AnimalFall.Core.Goals;

namespace AnimalFall.Core.Levels
{
    [CreateAssetMenu(fileName = "NewLevelData", menuName = "AnimalFall/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Identity")]
        public int levelNumber;
        public int rewardCoins;

        [Header("Timer & Goal")]
        public float timeLimit;
        public Goal goal;

        [Header("Spawner")]
        public float spawnInterval = 0.6f;
        public float spawnVariance = 0.15f;
        public int maxOnScreen = 8;

        [Header("Mechanics")]
        public bool enableBombs;
        public bool enableShielded;
        public bool enableDecoys;

        [Header("Penalties")]
        public float wrongTapTimePenalty = 1.0f;
        public int wrongTapScorePenalty = 30;
        public float bombTimePenalty = 3.0f;
        public int bombScorePenalty = 50;
    }
}
