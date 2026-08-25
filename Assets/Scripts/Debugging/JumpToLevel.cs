using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using AnimalFall.Data;
using AnimalFall.Managers;

namespace AnimalFall.Debugging
{
    /// <summary>
    /// Small UI/script-facing launcher for development level testing.
    /// Add it to GameScene, set a one-based level number, then call StartLevel()
    /// from a Button, another script, or the component context menu.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class JumpToLevel : MonoBehaviour
    {
        [SerializeField] private LevelJumpController _controller;
        [SerializeField, Min(1)] private int _levelNumber = 13;
        [Tooltip("When enabled, this launcher starts the selected level after entering Play Mode.")]
        [SerializeField] private bool _startOnPlay;

        public int LevelNumber => _levelNumber;
        public LevelJumpController Controller => ResolveController();

        private void Start()
        {
            if (_startOnPlay)
                StartCoroutine(StartAfterSceneReady());
        }

        /// <summary>Starts the Inspector-selected level. Unity UI Button friendly.</summary>
        public void StartLevel() => StartLevel(_levelNumber);

        /// <summary>Starts a one-based level. Unity UI Button events can pass this value.</summary>
        public void StartLevel(int oneBasedLevelNumber)
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            Debug.LogWarning("[JumpToLevel] Disabled in non-development builds.");
            return;
#endif
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[JumpToLevel] Enter Play Mode before calling StartLevel().");
                return;
            }

            _levelNumber = Mathf.Max(1, oneBasedLevelNumber);
            LevelJumpController controller = ResolveController();
            if (controller != null)
            {
                controller.JumpToLevel(_levelNumber);
                return;
            }

            LevelManager manager = LevelManager.Instance;
            LevelDatabase database = manager != null ? manager.Database : null;
            LevelData level = database != null ? database.GetLevelOrNull(_levelNumber - 1) : null;
            if (manager == null || database == null || level == null)
            {
                Debug.LogError("[JumpToLevel] No LevelJumpController or persistent LevelManager/database was found.");
                return;
            }

            if (!manager.TrySelectLevel(_levelNumber - 1, true)) return;
            string sceneName = manager.GetSceneNameForLevel(level);
            Debug.Log($"[JumpToLevel] Starting Level {_levelNumber} ({level.name}) in {sceneName}.");
            SceneManager.LoadScene(sceneName);
        }

        [ContextMenu("Start Selected Level")]
        private void StartSelectedLevelFromContextMenu() => StartLevel();

        private IEnumerator StartAfterSceneReady()
        {
            // Let LevelJumpController and the scene's persistent managers finish Awake/Start first.
            yield return null;
            StartLevel();
        }

        private LevelJumpController ResolveController()
        {
            if (_controller != null) return _controller;
            _controller = GetComponent<LevelJumpController>();
            if (_controller != null) return _controller;
            _controller = FindFirstObjectByType<LevelJumpController>();
            return _controller;
        }

        private void OnValidate()
        {
            _levelNumber = Mathf.Max(1, _levelNumber);
        }
    }
}
