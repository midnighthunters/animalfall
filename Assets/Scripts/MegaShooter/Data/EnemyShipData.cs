using UnityEngine;

namespace AnimalFall.MegaShooter
{
    [CreateAssetMenu(fileName = "MegaEnemy", menuName = "AnimalFall/Mega Shooter/Enemy Ship")]
    public sealed class EnemyShipData : ScriptableObject
    {
        public string stableId;
        public string displayName;
        public GameObject prefab;
        public Sprite sprite;
        public Sprite weaponIcon;
        public Sprite shadow;
        [Range(0.2f, 1f)] public float visualScale = 0.4f;
        public Vector2 colliderSize = new Vector2(0.6f, 0.6f);
        [Min(1f)] public float hitPoints = 30f;
        [Min(0f)] public float contactDamage = 1f;
        [Min(0.1f)] public float speed = 2.5f;
        [Min(0)] public int score = 100;
        public MegaMovementPattern movementPattern = MegaMovementPattern.Straight;
        public MegaWeaponPattern weaponPattern = MegaWeaponPattern.AimedSingle;
        public ProjectileData projectile;
        [Min(0.2f)] public float fireInterval = 2.4f;
        [Min(0f)] public float initialFireDelay = 1f;
        [Min(0f)] public float telegraphTime = 0.85f;
        public GameObject deathVFX;
        public GameObject hitVFX;
        public AudioClip fireAudio;
        public AudioClip deathAudio;
        [Range(0f, 1f)] public float pickupChance;
        public string poolingKey;
        public bool priorityTarget;
        public bool splitsOnDeath;
        public bool shieldsNearby;
        public bool repairsNearby;
    }
}
