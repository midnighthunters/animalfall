using UnityEngine;

namespace AnimalFall.Core.Arcade
{
    [CreateAssetMenu(fileName = "NewArcadeSession", menuName = "AnimalFall/Arcade Session Data")]
    public class ArcadeSessionData : ScriptableObject
    {
        [Header("Identity")]
        public MiniGameType gameType;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;

        [Header("Cost & Rewards")]
        public int tokenCost = 1;
        public int baseRewardCoins = 50;
        public int perfectBonusCoins = 100;
        public int rewardLives;

        [Header("Difficulty")]
        public float gravity = -9.81f;
        public int targetCount = 5;
        public float timeLimit = 60f;

        [Header("Gorilla Artillery")]
        public float windStrengthMin = -3f;
        public float windStrengthMax = 3f;
        public float windChangeInterval = 5f;

        [Header("Rhino Demolition")]
        public float requiredDamageScore = 500f;
        public float rhinoMass = 10f;
        public float groundPoundForce = 50f;

        [Header("Armadillo Ricochet")]
        public int slamCharges = 3;
        public float slamForce = 15f;
        public int goldenScarabCount = 5;
    }
}
