using UnityEngine;
using System;
using System.Collections;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [SerializeField] private LevelData[] allLevels;

    // We make this static or public so it survives fully across reloads if needed, 
    // but DontDestroyOnLoad handles instance survival.
    public int CurrentLevelIndex { get; private set; }
    private LevelData currentLevel;

    public static event Action levelLoadedEvent;
    public static event Action levelSuccessEvent;
    public static event Action levelFailedEvent;

    private bool isLevelActive;

    public LevelData CurrentLevelData => currentLevel;
    public int TotalLevels => allLevels.Length;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        GoalPanel.allGoalsEndedEvent += OnGoalsCompleted;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        GoalPanel.allGoalsEndedEvent -= OnGoalsCompleted;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GameScene")
        {
            LoadLevel();
        }
        else if (scene.name == "MainScene")
        {
            // Ensure we aren't running game logic in the menu
            isLevelActive = false;
            StopAllCoroutines();
        }
    }

    // Call this from MainMenuManager
    public void LoadGameSceneForLevel(int levelIndex)
    {
        // Set the index BEFORE loading the scene
        CurrentLevelIndex = levelIndex;
        SceneManager.LoadScene("GameScene");
    }

    private void LoadLevel()
    {
        // Safety Check
        if (CurrentLevelIndex >= allLevels.Length)
        {
            Debug.LogWarning("Level index out of bounds. Wrapping to 0.");
            CurrentLevelIndex = 0;
        }

        currentLevel = allLevels[CurrentLevelIndex];

        // Notify others (GameManager, Spawner, GoalPanel)
        levelLoadedEvent?.Invoke();

        if (GameManager.Instance != null)
            GameManager.Instance.StartLevel(currentLevel);

        isLevelActive = true;
    }

    private void OnGoalsCompleted()
    {
        if (isLevelActive)
        {
            LevelSuccess();
        }
    }

    public void LevelSuccess()
    {
        if (!isLevelActive) return;

        isLevelActive = false;

        // --- SAVE PROGRESS LOGIC ---
        int highestCompleted = GetHighestUnlockedLevel();

        // If we just beat the highest unlocked level, unlock the next one
        if (CurrentLevelIndex == highestCompleted)
        {
            PlayerPrefs.SetInt("HighestCompletedLevel", highestCompleted + 1);
            PlayerPrefs.Save();
            Debug.Log($"Progress Saved! Next Level Unlocked: {highestCompleted + 1}");
        }

        levelSuccessEvent?.Invoke();

        // Go back to menu after 2 seconds
        StartCoroutine(ReturnToMainSceneAfterDelay(2f));
    }

    public void LevelFailed()
    {
        if (!isLevelActive) return;
        isLevelActive = false;
        levelFailedEvent?.Invoke();
    }

    private IEnumerator ReturnToMainSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene("MainScene");
    }

    public int GetHighestUnlockedLevel()
    {
        return PlayerPrefs.GetInt("HighestCompletedLevel", 0);
    }

    // Reset for debugging
    [ContextMenu("Reset Progress")]
    public void ResetAllProgress()
    {
        PlayerPrefs.DeleteKey("HighestCompletedLevel");
        PlayerPrefs.Save();
        Debug.Log("Progress Reset to Level 1");
    }
}