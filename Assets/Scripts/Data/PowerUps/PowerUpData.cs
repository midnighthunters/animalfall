// Task 1.6 — PowerUpData ScriptableObject
using UnityEngine;

namespace AnimalFall.Data
{
    public enum PowerUpType { SlowTime, Magnet, MultiTap, AutoTap, FreezeAll }

    [CreateAssetMenu(fileName = "PowerUpData", menuName = "AnimalFall/Power-Up Data")]
    public class PowerUpData : ScriptableObject
    {
        [Tooltip("Power-up type identifier.")]
        public PowerUpType powerUpType;

        [Tooltip("Icon sprite from AnimalBlast icons/boosters/.")]
        public Sprite icon;

        [Tooltip("Cooldown in seconds after activation.")]
        [Range(5f, 120f)] public float cooldown = 30f;

        [Tooltip("Duration of the active effect in seconds.")]
        [Range(1f, 30f)] public float duration = 4f;

        [Tooltip("MultiTap: radius in world units for area collection.")]
        public float radius = 2f;

        [Tooltip("MultiTap/AutoTap: number of charges before power-up expires.")]
        public int charges = 3;
    }
}
