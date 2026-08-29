using UnityEngine;

namespace AnimalFall.MegaShooter
{
    [CreateAssetMenu(fileName = "MegaVFXProfile", menuName = "AnimalFall/Mega Shooter/VFX Profile")]
    public sealed class MegaVFXProfile : ScriptableObject
    {
        public GameObject hitSparkPrefab;
        public GameObject playerMuzzlePrefab;
        public GameObject enemyMuzzlePrefab;
        public GameObject bossMuzzlePrefab;
        public GameObject explosionPrefab;
        public GameObject eliteExplosionPrefab;
        public GameObject warningPrefab;
        public GameObject nearMissPrefab;
        public GameObject counterReadyPrefab;
        public GameObject bossDeathPrefab;
        [Range(0f, 1f)] public float masterShakeScale = 1f;
        [Range(0f, 1f)] public float masterFlashScale = 1f;
        public bool reducedShake;
        public bool reducedFlash;
    }
}
