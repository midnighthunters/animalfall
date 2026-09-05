using UnityEngine;
using UnityEngine.SceneManagement;
using AnimalFall.Core;
using AnimalFall.Data;
using AnimalFall.Utils;
using AnimalFall.Effects;
using AnimalFall.Services;

namespace AnimalFall.Managers
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [SerializeField] private LevelDatabase _levelDatabase;
        [SerializeField] private ObjectPooler  _objectPooler;

        // Prefabs for pre-warming
        [SerializeField] private GameObject _animalPrefab;
        [SerializeField] private GameObject _battleEffectWhitePrefab;
        [SerializeField] private GameObject _explosionBamPrefab;
        [SerializeField] private GameObject _explosionZapPrefab;
        [SerializeField] private GameObject _floatingTextPrefab;

        [SerializeField] private string _gameSceneName = "GameScene";
        [SerializeField] private string _megaShooterSceneName = "MegaShooterScene";
        [SerializeField] private string _mainSceneName = "MainScene";

        private LevelData _currentLevel;
        private int _currentLevelIndex = -1;
        private SaveService _save;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);  // ONLY MonoBehaviour with DDOL
        }

        private void Start() => ResolveSaveService();

        public void Init(SaveService save) => _save = save;

        public LevelData CurrentLevel => _currentLevel;
        public int CurrentLevelIndex => _currentLevelIndex;
        public int TotalLevels => _levelDatabase != null ? _levelDatabase.TotalLevels : 0;
        public LevelDatabase Database => _levelDatabase;
        public SaveService Save => _save;
        public LevelData GetLevelData(int levelIndex) => _levelDatabase?.GetLevelOrNull(levelIndex);

        public string GetSceneNameForLevel(LevelData level)
            => level != null && level.IsConfiguredMegaShooter ? _megaShooterSceneName : _gameSceneName;

        /// <summary>
        /// Selects a configured level without loading a scene. This is useful for debug launchers
        /// that already live in GameScene and must set CurrentLevel before GameManager.Start.
        /// </summary>
        public bool TrySelectLevel(int levelIndex, bool prewarmNormalLevel = true)
        {
            int total = TotalLevels;
            if (levelIndex < 0 || levelIndex >= total)
            {
                Debug.LogError($"[LevelManager] Level index {levelIndex} is out of range (total={total}).");
                return false;
            }

            LevelData selected = _levelDatabase?.GetLevelOrNull(levelIndex);
            if (selected == null)
            {
                Debug.LogWarning($"[LevelManager] Level {levelIndex + 1} is not configured yet.");
                return false;
            }

            _currentLevel = selected;
            _currentLevelIndex = levelIndex;
            if (prewarmNormalLevel && !_currentLevel.IsConfiguredMegaShooter)
                PrewarmPoolsForLevel(_currentLevel);
            return true;
        }

        /// <summary>Assigns a database only when this manager has none. Intended for direct-scene QA launchers.</summary>
        public bool ConfigureDatabaseIfMissing(LevelDatabase database)
        {
            if (_levelDatabase != null) return true;
            _levelDatabase = database;
            if (_levelDatabase != null) return true;
            Debug.LogError("[LevelManager] Cannot configure a null LevelDatabase.");
            return false;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void LoadGameSceneForLevel(int levelIndex)
        {
            if (!TrySelectLevel(levelIndex)) return;
            SceneManager.LoadScene(GetSceneNameForLevel(_currentLevel));
        }

        public void RetryCurrentLevel()
        {
            if (_currentLevel == null || _currentLevelIndex < 0) return;
            SceneManager.LoadScene(GetSceneNameForLevel(_currentLevel));
        }

        public void ReturnToMainScene()
        {
            GameEvents.ClearAll();
            SceneManager.LoadScene(_mainSceneName);
        }

        public void LevelSuccess(int levelIndex)
        {
            if (!ResolveSaveService())
            {
                Debug.LogError("[LevelManager] Cannot save level progress because no SaveService is available.");
                return;
            }

            // Selection is database-indexed, so prefer it over the authored LevelNumber.
            int completedLevelIndex = _currentLevelIndex >= 0 ? _currentLevelIndex : levelIndex;
            if (completedLevelIndex < 0 || completedLevelIndex >= TotalLevels)
            {
                Debug.LogError($"[LevelManager] Cannot save completion for invalid level index {completedLevelIndex}.");
                return;
            }

            // Keep the menu on a valid playable level after the final level is completed.
            int nextLevel = Mathf.Min(completedLevelIndex + 1, TotalLevels - 1);
            if (nextLevel > _save.GetHighestUnlockedLevel())
                _save.SetHighestUnlockedLevel(nextLevel);
        }

        private bool ResolveSaveService()
        {
            if (_save != null) return true;
            _save = SaveService.Instance ?? FindFirstObjectByType<SaveService>();
            return _save != null;
        }

        public void LevelFailed()
        {
            (LivesManager.Instance ?? FindFirstObjectByType<LivesManager>())?.UseLife();
            GameEvents.OnLevelFailed?.Invoke();
        }

        // ── Pool pre-warm ─────────────────────────────────────────────────────

        private void PrewarmPoolsForLevel(LevelData level)
        {
            // Always use the static Instance so we hit the scene-local pooler
            var pooler = AnimalFall.Core.ObjectPooler.Instance ?? _objectPooler;
            if (pooler == null) { Debug.LogWarning("[LevelManager] No ObjectPooler found for prewarm."); return; }

            ImageLibrary.LoadAll();

            int animalCount = level.MaxOnScreen + 2;
            TryPrewarm(pooler, _animalPrefab,              animalCount,   "AnimalPrefab");
            TryPrewarm(pooler, _battleEffectWhitePrefab,   10,            "BattleEffectWhite");
            TryPrewarm(pooler, _explosionBamPrefab,        3,             "ExplosionBam");
            TryPrewarm(pooler, _explosionZapPrefab,        3,             "ExplosionZap");
            TryPrewarm(pooler, _floatingTextPrefab,        10,            "FloatingText");
        }

        private void TryPrewarm(AnimalFall.Core.ObjectPooler pooler, GameObject prefab, int count, string label)
        {
            if (prefab == null) { Debug.LogWarning($"[LevelManager] PrewarmPool: {label} prefab is null. Skipping."); return; }
            if (count <= 0)     { Debug.LogWarning($"[LevelManager] PrewarmPool: {label} count is 0. Skipping."); return; }
            pooler.CreatePool(prefab, count);
        }
    }
}
