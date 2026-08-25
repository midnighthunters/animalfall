using UnityEngine;

namespace AnimalFall.MegaShooter
{
    [CreateAssetMenu(fileName = "MegaProjectile", menuName = "AnimalFall/Mega Shooter/Projectile")]
    public sealed class ProjectileData : ScriptableObject
    {
        public string stableId;
        public GameObject prefab;
        public Sprite sprite;
        public MegaProjectileMotion motion = MegaProjectileMotion.Straight;
        [Min(0.1f)] public float speed = 9f;
        [Min(0f)] public float damage = 1f;
        [Min(0.05f)] public float lifetime = 6f;
        [Min(0.01f)] public float colliderRadius = 0.12f;
        [Min(0f)] public float homingStrength;
        [Min(0f)] public float sineAmplitude;
        [Min(0f)] public float sineFrequency;
        public bool reflectable = true;
        public Color playerColor = new Color(0.25f, 1f, 1f, 1f);
        public Color enemyColor = new Color(1f, 0.2f, 0.35f, 1f);
        public GameObject impactVFX;
        public GameObject trailVFX;
        public AudioClip impactAudio;
        public string poolingKey;
    }
}
