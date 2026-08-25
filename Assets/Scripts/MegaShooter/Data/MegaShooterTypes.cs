using System;
using UnityEngine;

namespace AnimalFall.MegaShooter
{
    public enum LevelMode { Normal = 0, MegaShooter = 1 }
    public enum MegaShooterState { Intro, Countdown, Wave, WaveTransition, BossWarning, Boss, Won, Lost, Paused }
    public enum MegaFaction { Player, Enemy }
    public enum MegaWaveCompletion { DefeatAll, SurviveDuration, DefeatPriorityTargets }
    public enum MegaFormationType { Line, V, Grid, AlternatingSides, Arc, Column, Mirrored }
    public enum MegaSpawnPath { Top, Left, Right, DiveLane, Center, SideReentry }
    public enum MegaMovementPattern { Straight, Sine, ZigZag, Hover, Dive, Rammer, Orbit, Stationary, Cloak, SideSweep }
    public enum MegaWeaponPattern { None, AimedSingle, FixedSpread, Burst, Radial, Mine, Laser, Sniper, Carrier }
    public enum MegaProjectileMotion { Straight, Homing, Sine, Returning, StationaryMine, Beam }
    public enum MegaCounterType { SkyBarrage, MirageCounter, ShellReflector, ClawDash, GravitySmash, TimeEye, SpectrumShift, MeteorRam, RoyalFan, SolarRoar }
    public enum MegaPickupType { Health, CounterEnergy, Shield, WeaponBoost }
    public enum MegaEnvironmentEvent { None, SlowField, AsteroidDrift, TimeRift, MovingBarriers }

    [Serializable]
    public sealed class MegaStatBars
    {
        [Range(0f, 1f)] public float power = 0.5f;
        [Range(0f, 1f)] public float speed = 0.5f;
        [Range(0f, 1f)] public float defense = 0.5f;
        [Range(0f, 1f)] public float coverage = 0.5f;
    }

    [Serializable]
    public sealed class MegaPassiveData
    {
        [TextArea] public string description;
        public float movementMultiplier = 1f;
        public int bonusHealth;
        [Range(0f, 1f)] public float frontalDamageReduction;
        [Range(0f, 2f)] public float nearMissChargeMultiplier = 1f;
        [Range(0f, 1f)] public float lowHealthDamageBonus;
        [Range(0f, 1f)] public float consecutiveHitDamageCap;
        public float cloakAfterNoDamageSeconds;
    }

    [Serializable]
    public sealed class MegaCounterData
    {
        public MegaCounterType type;
        [Min(1f)] public float meterRequirement = 100f;
        [Min(0.1f)] public float duration = 1.5f;
        [Min(0f)] public float damage = 80f;
        [Min(0f)] public float radius = 4f;
        public bool reflectsProjectiles;
        public bool clearsProjectiles = true;
        [Min(0f)] public float cooldown = 0.25f;
        [Range(0f, 1f)] public float meterLossOnHit = 0.25f;
    }

    [Serializable]
    public sealed class MegaAudioReferences
    {
        public AudioClip music;
        public AudioClip shot;
        public AudioClip hit;
        public AudioClip explosion;
        public AudioClip warning;
        public AudioClip counterReady;
        public AudioClip counterActivate;
        public AudioClip victory;
    }

    [Serializable]
    public sealed class MegaBossOverride
    {
        [Min(0.1f)] public float healthMultiplier = 1f;
        [Min(0.1f)] public float attackIntervalMultiplier = 1f;
        [Min(0.1f)] public float projectileSpeedMultiplier = 1f;
        public bool dropCriticalHealthPickup;
        [Range(0f, 1f)] public float healthPickupBossThreshold = 0.5f;
        [Range(0f, 1f)] public float playerCriticalHealthThreshold = 0.35f;
    }

    [Serializable]
    public sealed class BossAttackPattern
    {
        public string attackName = "Volley";
        public MegaWeaponPattern pattern = MegaWeaponPattern.FixedSpread;
        [Range(0.01f, 10f)] public float weight = 1f;
        [Min(0.85f)] public float telegraphTime = 0.85f;
        [Range(1, 24)] public int projectileCount = 3;
        [Range(0f, 180f)] public float spreadDegrees = 35f;
        public bool aimed;
        public bool reflectable = true;
        public bool clearsBulletsBeforeAttack;
    }

    [Serializable]
    public sealed class BossPhaseData
    {
        [Tooltip("Normalized health at which this phase begins. Keep phases sorted from 1 down to 0.")]
        [Range(0f, 1f)] public float healthThreshold = 1f;
        public MegaMovementPattern movementPattern = MegaMovementPattern.Hover;
        public BossAttackPattern[] attacks = Array.Empty<BossAttackPattern>();
        [Min(0.2f)] public float attackInterval = 2f;
        [Min(0.1f)] public float projectileSpeedMultiplier = 1f;
        [Min(0f)] public float projectileDamageOverride;
        public EnemySpawnGroup[] addGroups = Array.Empty<EnemySpawnGroup>();
        [Min(0f)] public float vulnerableDuration = 6f;
        [Min(0f)] public float invulnerableDuration;
        public bool usesWeakPoint;
        public Vector2 weakPointOffset;
        [Min(0f)] public float transitionDuration = 1f;
        public Color phaseTint = Color.white;
        [Range(0f, 1f)] public float cameraShake = 0.25f;
        public string warningText;
        public GameObject transitionVFX;
        public AudioClip transitionAudio;
    }

    [Serializable]
    public sealed class EnemySpawnGroup
    {
        public EnemyShipData enemy;
        [Range(1, 40)] public int count = 4;
        public MegaFormationType formation = MegaFormationType.Line;
        public MegaSpawnPath spawnPath = MegaSpawnPath.Top;
        [Min(0f)] public float startDelay;
        [Min(0.05f)] public float cadence = 0.35f;
        [Range(1, 10)] public int columns = 4;
        [Range(1, 10)] public int rows = 1;
        [Min(0.1f)] public float spacing = 1f;
        [Range(0f, 1f)] public float normalizedEntry = 0.5f;
        [Min(0f)] public float movementSpeedOverride;
        public MegaWeaponPattern firePatternOverride;
        [Range(0f, 1f)] public float eliteChance;
        public bool explicitElite;
        public MegaPickupType pickupDropOverride;
        public bool priorityTarget;
    }

    [Serializable]
    public sealed class MegaWaveData
    {
        public string waveName = "Wave";
        [Min(1)] public int waveNumber = 1;
        [Min(0f)] public float startDelay = 1f;
        [Min(0f)] public float completionDelay = 1f;
        public string warningBanner;
        public EnemySpawnGroup[] spawnGroups = Array.Empty<EnemySpawnGroup>();
        [Min(0.1f)] public float healthMultiplier = 1f;
        [Min(0.1f)] public float speedMultiplier = 1f;
        [Min(0.1f)] public float fireRateMultiplier = 1f;
        [Min(0.1f)] public float scoreMultiplier = 1f;
        [Range(1, 20)] public int maximumSimultaneousEnemies = 8;
        public MegaEnvironmentEvent environmentEvent;
        public MegaWaveCompletion completionCondition = MegaWaveCompletion.DefeatAll;
        [Min(0f)] public float surviveDuration;
    }
}
