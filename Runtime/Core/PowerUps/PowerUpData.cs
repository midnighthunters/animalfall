using UnityEngine;

namespace AnimalFall.Core.PowerUps
{
    [CreateAssetMenu(fileName = "NewPowerUpData", menuName = "AnimalFall/Power-Up Data")]
    public class PowerUpData : ScriptableObject
    {
        [Header("Identity")]
        public PowerUpType type;
        public string displayName;
        public Sprite icon;

        [Header("Cost")]
        public int costCoins = 100;
        public bool isPremium;

        [Header("Behavior")]
        public float duration = 5f;
        public float value = 1f;
        public int usesPerLevel = 1;
    }
}
