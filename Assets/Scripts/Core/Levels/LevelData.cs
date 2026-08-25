// Task 1.1 — LevelData ScriptableObject (full rewrite)
using UnityEngine;
using AnimalFall.Data;
using AnimalFall.Core.Hindrances;
using AnimalFall.MegaShooter;

namespace AnimalFall.Data
{
    [System.Serializable]
    public class HindranceConfig
    {
        [Tooltip("Hindrance type to potentially spawn.")]
        public HindranceType type;
        [Tooltip("Relative spawn weight (> 0). Higher = spawns more frequently.")]
        [Range(0.01f, 10f)] public float weight = 1f;
        [Tooltip("Additional seconds delay before this type can first spawn.")]
        [Range(0f, 30f)] public float initialDelay;
    }

    [CreateAssetMenu(fileName = "Level_XX", menuName = "AnimalFall/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("1-based level index. The configured database may contain up to 100 entries.")]
        [SerializeField] private int _levelNumber;
        [Tooltip("Display name of the chapter this level belongs to.")]
        [SerializeField] private string _chapterTheme;
        [Tooltip("Background sprite: bg_chapter<N>.png from panels/.")]
        [SerializeField] private Sprite _chapterBackground;

        [Header("Timer & Goal")]
        [Tooltip("Countdown duration in seconds (10-120).")]
        [SerializeField, Range(10f, 120f)] private float _timeLimit = 60f;
        [Tooltip("Per-species rescue targets. Must be a ScriptableObject asset.")]
        [SerializeField] private GoalData _goal;

        [Header("Spawner")]
        [Tooltip("Animals eligible to spawn this level (1-12 entries).")]
        [SerializeField] private AnimalData[] _spawnPool;
        [Tooltip("Base seconds between spawns (0.1-2.0).")]
        [SerializeField, Range(0.1f, 2.0f)] private float _spawnInterval = 0.6f;
        [Tooltip("+- randomness on spawn interval (0-0.5).")]
        [SerializeField, Range(0f, 0.5f)] private float _spawnVariance = 0.15f;
        [Tooltip("Max simultaneous animals on screen (1-20).")]
        [SerializeField, Range(1, 20)] private int _maxOnScreen = 8;

        [Header("Hindrances")]
        [Tooltip("Hindrance entries with type, weight, initial delay.")]
        [SerializeField] private HindranceConfig[] _hindrances;
        [Tooltip("Seconds between hindrance activations (1-30).")]
        [SerializeField, Range(1f, 30f)] private float _hindranceSpawnInterval = 6f;
        [Tooltip("Seconds before first hindrance spawns (2-15).")]
        [SerializeField, Range(2f, 15f)] private float _hindranceInitialDelay = 5f;
        [Tooltip("Max simultaneous active hindrances (1-5).")]
        [SerializeField, Range(1, 5)] private int _maxHindrancesActive = 2;

        [Header("Penalties")]
        [Tooltip("Seconds deducted per wrong tap.")]
        [SerializeField] private float _wrongTapTimePenalty = 1.0f;
        [Tooltip("Score deducted per wrong tap.")]
        [SerializeField] private int _wrongTapScorePenalty = 30;
        [Tooltip("Seconds deducted when a bomb is tapped.")]
        [SerializeField] private float _bombTimePenalty = 3.0f;
        [Tooltip("Score deducted when a bomb is tapped.")]
        [SerializeField] private int _bombScorePenalty = 50;

        [Header("Rewards")]
        [Tooltip("Coins awarded on level win (0-500).")]
        [SerializeField, Range(0, 500)] private int _rewardCoins;

        [Header("Mega Level")]
        [Tooltip("True for every 5th level (L5, L10, ... L50).")]
        [SerializeField] private bool _isMegaLevel;
        [Tooltip("Explicit opt-in for normal hindrances during a mega level. Defaults false.")]
        [SerializeField] private bool _allowNormalHindrancesInMegaLevel;
        [Tooltip("Required when isMegaLevel = true.")]
        [SerializeField] private VillainData _villain;

        [Header("Mega Shooter")]
        [Tooltip("Normal = existing falling-animal gameplay. MegaShooter = dedicated shooter scene.")]
        [SerializeField] private LevelMode _levelMode = LevelMode.Normal;
        [Tooltip("Required only when Level Mode is MegaShooter.")]
        [SerializeField] private MegaLevelData _megaShooterData;

        // Public read-only accessors
        public int LevelNumber              => _levelNumber;
        public string ChapterTheme          => _chapterTheme;
        public Sprite ChapterBackground     => _chapterBackground;
        public float TimeLimit              => _timeLimit;
        public GoalData Goal                => _goal;
        public AnimalData[] SpawnPool       => _spawnPool;
        public float SpawnInterval          => _spawnInterval;
        public float SpawnVariance          => _spawnVariance;
        public int MaxOnScreen              => _maxOnScreen;
        public HindranceConfig[] Hindrances => _hindrances;
        public float HindranceSpawnInterval => _hindranceSpawnInterval;
        public float HindranceInitialDelay  => _hindranceInitialDelay;
        public int MaxHindrancesActive      => _maxHindrancesActive;
        public float WrongTapTimePenalty    => _wrongTapTimePenalty;
        public int WrongTapScorePenalty     => _wrongTapScorePenalty;
        public float BombTimePenalty        => _bombTimePenalty;
        public int BombScorePenalty         => _bombScorePenalty;
        public int RewardCoins              => _rewardCoins;
        public bool IsMegaLevel             => _isMegaLevel;
        public bool AllowNormalHindrancesInMegaLevel => _allowNormalHindrancesInMegaLevel;
        public VillainData Villain          => _villain;
        public LevelMode Mode                => _levelMode;
        public MegaLevelData MegaShooterData => _megaShooterData;
        public bool IsConfiguredMegaShooter => _levelMode == LevelMode.MegaShooter && _megaShooterData != null;

        // Editor-only setters used by LevelDatabase generator
#if UNITY_EDITOR
        public void SetLevelNumber(int v)          => _levelNumber = v;
        public void SetChapterTheme(string v)      => _chapterTheme = v;
        public void SetTimeLimit(float v)          => _timeLimit = v;
        public void SetSpawnInterval(float v)      => _spawnInterval = v;
        public void SetSpawnVariance(float v)      => _spawnVariance = v;
        public void SetMaxOnScreen(int v)          => _maxOnScreen = v;
        public void SetMaxHindrancesActive(int v)  => _maxHindrancesActive = v;
        public void SetHindrancesArray(HindranceConfig[] v) => _hindrances = v;
        public void SetHindranceSpawnInterval(float v)      => _hindranceSpawnInterval = v;
        public void SetHindranceInitialDelay(float v)       => _hindranceInitialDelay = v;
        public void SetWrongTapTimePenalty(float v)         => _wrongTapTimePenalty = v;
        public void SetBombTimePenalty(float v)             => _bombTimePenalty = v;
        public void SetIsMegaLevel(bool v)         => _isMegaLevel = v;
        public void SetRewardCoins(int v)          => _rewardCoins = v;
        public void SetMegaShooter(LevelMode mode, MegaLevelData data)
        {
            _levelMode = mode;
            _megaShooterData = data;
        }
#endif
    }
}
