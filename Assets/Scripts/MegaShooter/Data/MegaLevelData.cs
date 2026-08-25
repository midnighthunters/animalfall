using System;
using UnityEngine;

namespace AnimalFall.MegaShooter
{
    [CreateAssetMenu(fileName = "MegaLevel", menuName = "AnimalFall/Mega Shooter/Mega Level")]
    public sealed class MegaLevelData : ScriptableObject
    {
        [Header("Identity")]
        [Range(5, 100)] public int gameLevelNumber = 5;
        public string displayTitle = "First Flight";
        [Range(1, 20)] public int megaSequenceIndex = 1;
        [TextArea] public string description;

        [Header("Presentation")]
        public Sprite[] backgroundLayers = Array.Empty<Sprite>();
        public float[] backgroundLayerSpeeds = Array.Empty<float>();
        [Min(0f)] public float backgroundSpeed = 0.5f;
        public Color backgroundColor = new Color(0.015f, 0.035f, 0.12f, 1f);
        public Color accentColor = new Color(0.2f, 0.9f, 1f, 1f);
        public Sprite introImage;
        public string bossWarningText = "WARNING — BOSS APPROACHING";

        [Header("Player")]
        public SuperAnimalData featuredAnimal;
        public SuperAnimalData[] allowedAnimals = Array.Empty<SuperAnimalData>();
        [Range(1, 12)] public int startingHealth = 5;
        [Min(0.1f)] public float movementSpeedMultiplier = 1f;
        [Min(0.1f)] public float playerPowerMultiplier = 1f;
        [Min(0.1f)] public float invulnerabilityDuration = 1f;
        [Min(0.1f)] public float counterChargeMultiplier = 1f;

        [Header("Arena")]
        public Rect cameraBounds = new Rect(-5f, -8.9f, 10f, 17.8f);
        public Rect playerMovementBounds = new Rect(-4.2f, -7.6f, 8.4f, 6.2f);
        public Vector4 safeUiMargins = new Vector4(0.2f, 1.2f, 0.2f, 1.1f);
        [Min(0f)] public float scrollSpeed = 0.7f;
        [Min(0f)] public float bottomProjectileExclusion = 1.4f;

        [Header("Waves & Boss")]
        public MegaWaveData[] waves = Array.Empty<MegaWaveData>();
        public BossShipData boss;
        public MegaBossOverride bossOverrides = new MegaBossOverride();

        [Header("Difficulty")]
        [Min(0.1f)] public float enemyHealthMultiplier = 0.75f;
        [Min(0.1f)] public float enemyDamageMultiplier = 1f;
        [Min(0.1f)] public float enemyProjectileSpeedMultiplier = 0.65f;
        [Min(0.85f)] public float ordinaryEnemyFireInterval = 2.4f;
        [Min(0.1f)] public float enemyFireIntervalMultiplier = 1f;
        [Min(0.1f)] public float spawnCadenceMultiplier = 1f;
        [Range(1, 20)] public int maximumActiveEnemies = 6;
        [Range(1, 120)] public int maximumHostileProjectiles = 24;
        [Min(1)] public int targetEnemyCount = 18;

        [Header("Scoring")]
        [Min(0f)] public float scoreMultiplier = 1f;
        [Min(0f)] public float comboTimeout = 2f;
        [Min(0)] public int nearMissScore = 25;
        [Min(0)] public int counterScore = 250;

        [Header("Completion")]
        [Min(1f)] public float parTime = 85f;
        [Min(0)] public int coinReward = 100;
        [Min(0)] public int arcadeTokenReward = 3;
        public string unlockReward;

        [Header("VFX & Audio")]
        public MegaVFXProfile vfxProfile;
        [Range(0f, 2f)] public float cameraShakeScale = 1f;
        [Range(0f, 2f)] public float flashScale = 1f;
        public bool reducedEffectsCompatible = true;
        public MegaAudioReferences audio = new MegaAudioReferences();

        [Header("Debug")]
        public int deterministicSeed = 5001;
        public bool randomizeSeed;

        public bool IsValidMegaNumber => gameLevelNumber >= 5 && gameLevelNumber <= 100 && gameLevelNumber % 5 == 0;
    }
}
