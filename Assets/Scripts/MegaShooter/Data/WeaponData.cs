using UnityEngine;

namespace AnimalFall.MegaShooter
{
    [CreateAssetMenu(fileName = "MegaWeapon", menuName = "AnimalFall/Mega Shooter/Weapon")]
    public sealed class WeaponData : ScriptableObject
    {
        public string stableId;
        public string displayName;
        public Sprite icon;
        public MegaWeaponPattern pattern = MegaWeaponPattern.FixedSpread;
        public ProjectileData projectile;
        [Min(0f)] public float damage = 10f;
        [Min(0.05f)] public float shotsPerSecond = 5f;
        [Range(1, 12)] public int burstCount = 1;
        [Range(1, 16)] public int projectileCount = 1;
        [Range(0f, 180f)] public float spreadDegrees;
        [Range(0, 20)] public int pierce;
        [Min(0f)] public float splashRadius;
        [Min(0f)] public float homingStrength;
        [Range(0, 8)] public int chainCount;
        [Min(0.1f)] public float projectileLifetime = 5f;
        [Range(1, 10)] public int stronglyHomingEveryNthVolley;
        public GameObject muzzleVFX;
        public AudioClip shotAudio;

        public float EstimatedDps => damage * Mathf.Max(0.05f, shotsPerSecond) * Mathf.Max(1, projectileCount);
    }
}
