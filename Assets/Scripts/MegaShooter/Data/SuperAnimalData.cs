using UnityEngine;

namespace AnimalFall.MegaShooter
{
    [CreateAssetMenu(fileName = "SuperAnimal", menuName = "AnimalFall/Mega Shooter/Super Animal")]
    public sealed class SuperAnimalData : ScriptableObject
    {
        [Header("Identity")]
        public string stableId;
        public string displayName;
        public Sprite portrait;
        public Sprite shipSprite;
        public GameObject playerPrefab;
        [Range(1, 20)] public int unlockMegaIndex = 1;
        [Range(5, 100)] public int unlockGameLevel = 5;

        [Header("Flight")]
        [Range(1, 12)] public int baseHealth = 5;
        [Min(0.1f)] public float movementSpeed = 10f;
        [Min(0.05f)] public float hitboxRadius = 0.22f;
        public Vector2[] muzzleOffsets = { new Vector2(-0.18f, 0.45f), new Vector2(0.18f, 0.45f) };

        [Header("Combat")]
        public WeaponData primaryWeapon;
        public MegaPassiveData passive = new MegaPassiveData();
        public MegaCounterData counter = new MegaCounterData();

        [Header("Presentation")]
        [TextArea] public string selectionDescription;
        public MegaStatBars stats = new MegaStatBars();
        public GameObject engineVFX;
        public GameObject muzzleVFX;
        public GameObject projectileVFX;
        public GameObject counterVFX;
        public GameObject hitVFX;
        public GameObject deathVFX;
        public Sprite[] optionalSkins;
    }
}
