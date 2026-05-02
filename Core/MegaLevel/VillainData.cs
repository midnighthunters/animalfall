using UnityEngine;
using AnimalFall.Core.Hindrances;

namespace AnimalFall.Core.MegaLevel
{
    [CreateAssetMenu(fileName = "NewVillainData", menuName = "AnimalFall/Villain Data")]
    public class VillainData : ScriptableObject
    {
        [Header("Identity")]
        public string villainName;
        public Sprite sprite;

        [Header("Stats")]
        public int maxHP = 100;
        public float shieldDuration = 5f;
        public float vulnerableWindow = 3f;
        public float attackInterval = 4f;
        public int damagePerHit = 10;

        [Header("Attacks")]
        public HindranceType[] attackHindrances;
        public GameObject projectilePrefab;
        public float projectileSpeed = 3f;
        public int projectilesPerAttack = 3;

        [Header("Phases")]
        public float phase2HPPercent = 0.5f;
        public float phase2AttackSpeedMultiplier = 1.5f;
        public float phase3HPPercent = 0.25f;
        public float phase3AttackSpeedMultiplier = 2f;
    }
}
