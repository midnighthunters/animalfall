// ============================================================
//  LevelManager.cs  –  Animal Fall  (REFACTORED)
//  Changes:
//    • All PlayerPrefs → SaveManager
//    • All manual events → EventBus (static C# events retained
//      for GoalPanel compatibility)
//    • Scene names are constants, not magic strings
//    • Camera focus-on-player delay built in
// ============================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────
    public static LevelManager Instance { get; private set; }

    // ── Scene name constants ───────────────────────────────────
    public const string SCENE_MAIN    = "MainScene";
    public const string SCENE_GAME    = "GameScene";
    public const string SCENE_SPLASH  = "SplashScene";

    // ── Config ─────────────────────────────────────────────────
    [SerializeField] private LevelData[] allLevels;
    [SerializeField] private float       returnToMenuDelay = 2.5f;
    [SerializeField] private float       cameraFocusDelay  = 0.4f;

    // ── State ─────────────────────────────────────────────────
    public int       CurrentLevelIndex { get; private set; }
    public LevelData CurrentLevelData  => _currentLevel;
    public int       TotalLevels       => allLevels?.Length ?? 0;

    private LevelData _currentLevel;
    private bool      _isLevelActive;

    // ── Legacy static events (GoalPanel hooks these) ──────────
    public static event Action levelLoadedEvent;
    public static event Action levelSuccessEvent;
    public static event Action levelFailedEvent;

    // ── Lifecycle ─────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        GoalPanel.allGoalsEndedEvent += OnGoalsCompleted;
        SceneManager.sceneLoaded    += OnSceneLoaded;
    }

    private void OnDisable()
    {
        GoalPanel.allGoalsEndedEvent -= OnGoalsCompleted;
        SceneManager.sceneLoaded    -= OnSceneLoaded;
    }

    // ── Scene hooks ───────────────────────────────────────────
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == SCENE_GAME)
        {
            StartCoroutine(LoadLevelWithCameraFocus());
        }
        else if (scene.name == SCENE_MAIN)
        {
            _isLevelActive = false;
            StopAllCoroutines();
            AudioManager.Instance?.PlayMusic(AudioManager.MusicTrack.MainMenu);
        }
    }

    // ── Public entry point ────────────────────────────────────
    public void LoadGameSceneForLevel(int levelIndex)
    {
        CurrentLevelIndex = Mathf.Clamp(levelIndex, 0, TotalLevels - 1);
        SceneManager.LoadScene(SCENE_GAME);
    }

    // ── Level init ────────────────────────────────────────────
    private IEnumerator LoadLevelWithCameraFocus()
    {
        // Safety clamp
        if (CurrentLevelIndex >= allLevels.Length)
        {
            Debug.LogWarning("[LevelManager] Index out of bounds, wrapping to 0.");
            CurrentLevelIndex = 0;
        }

        _currentLevel = allLevels[CurrentLevelIndex];

        // Brief delay to let the scene finish building before camera pans
        yield return new WaitForSeconds(cameraFocusDelay);

        // Fire loaded event so GoalPanel and others can init
        levelLoadedEvent?.Invoke();

        if (GameManager.Instance != null)
            GameManager.Instance.StartLevel(_currentLevel);

        _isLevelActive = true;
    }

    // ── Goal completed hook ───────────────────────────────────
    private void OnGoalsCompleted()
    {
        if (_isLevelActive) LevelSuccess();
    }

    // ── Success / Fail ────────────────────────────────────────
    public void LevelSuccess()
    {
        if (!_isLevelActive) return;
        _isLevelActive = false;

        // Save progress via SaveManager (no more PlayerPrefs)
        if (SaveManager.Instance != null)
            SaveManager.Instance.UnlockNextLevel(CurrentLevelIndex);

        levelSuccessEvent?.Invoke();
        StartCoroutine(ReturnToMenuAfterDelay(returnToMenuDelay));
    }

    public void LevelFailed()
    {
        if (!_isLevelActive) return;
        _isLevelActive = false;
        levelFailedEvent?.Invoke();
    }

    // ── Return to menu ────────────────────────────────────────
    private IEnumerator ReturnToMenuAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(SCENE_MAIN);
    }

    // ── Query ─────────────────────────────────────────────────
    public int GetHighestUnlockedLevel() =>
        SaveManager.Instance?.GetHighestUnlockedLevel() ?? 0;

    // ── Debug ─────────────────────────────────────────────────
    [ContextMenu("Reset Progress")]
    public void ResetAllProgress() => SaveManager.Instance?.ResetAllProgress();
}