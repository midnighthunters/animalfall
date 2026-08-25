// Task 1.4 — VillainData ScriptableObject
using UnityEngine;

namespace AnimalFall.Data
{
    [CreateAssetMenu(fileName = "VillainData", menuName = "AnimalFall/Villain Data")]
    public class VillainData : ScriptableObject
    {
        [Tooltip("Display name of the villain.")]
        public string villainName;

        [Tooltip("Portrait sprite for VillainHUD.")]
        public Sprite portrait;

        [Tooltip("Number of HP phases (typically 3).")]
        [Range(1, 5)] public int hpPhases = 3;

        [Tooltip("Animals to rescue per phase before dealing 1 HP.")]
        public int[] animalsPerPhase;

        [Tooltip("Projectile spawn frequency per phase (seconds between shots).")]
        public float[] projectileFrequencyPerPhase;

        [Tooltip("Projectile prefab (pooled via ObjectPooler).")]
        public GameObject projectilePrefab;
    }
}
