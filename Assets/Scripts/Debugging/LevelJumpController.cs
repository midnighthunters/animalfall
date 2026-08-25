using UnityEngine;
using UnityEngine.SceneManagement;
using AnimalFall.Data;
using AnimalFall.Managers;

namespace AnimalFall.Debugging
{
    /// <summary>
    /// Development launcher for starting any configured database level from GameScene.
    /// Place it in GameScene, assign LevelDatabase, choose a one-based level number, and
    /// enable Jump On Game Scene Start. It is inert by default and in release builds.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class LevelJumpController : MonoBehaviour
    {
        [Header("Level Source")]
        [SerializeField] private LevelDatabase _levelDatabase;
        [SerializeField] private GameManager _gameManager;

        [Header("Jump Target")]
        [Tooltip("One-based game level number. Only configured database slots can be started.")]
        [SerializeField, Min(1)] private int _levelNumber = 1;
        [Tooltip("When enabled, pressing Play while GameScene is open starts the selected level.")]
        [SerializeField] private bool _jumpOnGameSceneStart;

        [Header("Build Safety")]
        [Tooltip("Keep disabled for production. Editor and Development builds are always allowed.")]
        [SerializeField] private bool _allowInReleaseBuild;

        public LevelDatabase Database => _levelDatabase;
        public int LevelNumber => _levelNumber;
        public bool JumpOnGameSceneStart => _jumpOnGameSceneStart;
        public LevelData SelectedLevel => GetConfiguredLevel(_levelNumber);

        private void Start()
        {
            if (_jumpOnGameSceneStart)
                PrepareSelectedLevelAtGameSceneStart();
        }

        /// <summary>Reloads the appropriate scene and starts the current Inspector target.</summary>
        [ContextMenu("Jump To Selected Level")]
        public void JumpToSelectedLevel() => JumpToLevel(_levelNumber);

        /// <summary>Public int entry point suitable for UI Button events and debug consoles.</summary>
        public void JumpToLevel(int oneBasedLevelNumber)
        {
            if (!CanJump()) return;
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[LevelJump] Enter Play Mode or enable 'Jump On Game Scene Start'.");
                return;
            }

            if (!TryResolveLevel(oneBasedLevelNumber, out LevelData level)) return;
            LevelManager manager = EnsureLevelManager(out bool created);
            if (manager == null || !manager.TrySelectLevel(oneBasedLevelNumber - 1, !created)) return;

            _levelNumber = oneBasedLevelNumber;
            string sceneName = manager.GetSceneNameForLevel(level);
            Debug.Log($"[LevelJump] Jumping to Level {oneBasedLevelNumber} ({level.name}) in {sceneName}.");
            SceneManager.LoadScene(sceneName);
        }

        public LevelData GetConfiguredLevel(int oneBasedLevelNumber)
        {
            if (_levelDatabase == null || oneBasedLevelNumber < 1 || oneBasedLevelNumber > _levelDatabase.TotalLevels)
                return null;
            return _levelDatabase.GetLevelOrNull(oneBasedLevelNumber - 1);
        }

        private void PrepareSelectedLevelAtGameSceneStart()
        {
            if (!CanJump() || !TryResolveLevel(_levelNumber, out LevelData level)) return;
            LevelManager manager = EnsureLevelManager(out bool created);
            if (manager == null || !manager.TrySelectLevel(_levelNumber - 1, !created)) return;

            if (level.IsConfiguredMegaShooter)
            {
                string sceneName = manager.GetSceneNameForLevel(level);
                Debug.Log($"[LevelJump] Level {_levelNumber} is a mega level; routing to {sceneName}.");
                SceneManager.LoadScene(sceneName);
                return;
            }

            if (_gameManager == null) _gameManager = FindFirstObjectByType<GameManager>();
            if (_gameManager == null)
            {
                Debug.LogError("[LevelJump] GameManager was not found in GameScene.");
                return;
            }

            _gameManager.SetDirectStartLevel(level);
            Debug.Log($"[LevelJump] Prepared normal Level {_levelNumber} ({level.name}) before GameManager.Start.");
        }

        private LevelManager EnsureLevelManager(out bool created)
        {
            created = false;
            LevelManager manager = LevelManager.Instance;
            if (manager == null)
            {
                GameObject go = new GameObject("[Debug] Direct Scene LevelManager");
                manager = go.AddComponent<LevelManager>();
                created = true;
            }

            if (!manager.ConfigureDatabaseIfMissing(_levelDatabase))
            {
                if (created) Destroy(manager.gameObject);
                return null;
            }
            return manager;
        }

        private bool TryResolveLevel(int oneBasedLevelNumber, out LevelData level)
        {
            level = null;
            if (_levelDatabase == null)
            {
                Debug.LogError("[LevelJump] Assign a LevelDatabase in the Inspector.");
                return false;
            }
            if (oneBasedLevelNumber < 1 || oneBasedLevelNumber > _levelDatabase.TotalLevels)
            {
                Debug.LogError($"[LevelJump] Level {oneBasedLevelNumber} is outside 1–{_levelDatabase.TotalLevels}.");
                return false;
            }

            level = _levelDatabase.GetLevelOrNull(oneBasedLevelNumber - 1);
            if (level != null) return true;
            Debug.LogError($"[LevelJump] Level {oneBasedLevelNumber} has no LevelData in the database.");
            return false;
        }

        private bool CanJump()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return true;
#else
            if (_allowInReleaseBuild) return true;
            Debug.LogWarning("[LevelJump] Disabled in non-development builds.");
            return false;
#endif
        }

        private void OnValidate()
        {
            int maximum = _levelDatabase != null ? Mathf.Max(1, _levelDatabase.TotalLevels) : int.MaxValue;
            _levelNumber = Mathf.Clamp(_levelNumber, 1, maximum);
        }
    }
}
