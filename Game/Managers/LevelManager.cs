using UnityEngine;
using System;
using System.Collections;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [SerializeField] private LevelData[] allLevels;
    [SerializeField] private LevelData currentLevel;

    public static event Action levelLoadedEvent;
    public static event Action levelSuccessEvent;
    public static event Action levelFailedEvent;

    private float remainingTime;
    private bool isLevelActive;

    public LevelData CurrentLevelData => currentLevel;

    private void Awake()
    {
        // 1. Singleton Logic with Persistence
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return; // Important: Stop execution if this is a duplicate
        }
        else
        {
            Instance = this;
            // 2. This makes the object survive scene changes
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnEnable()
    {
        GoalPanel.allGoalsEndedEvent += OnGoalsCompleted;
        // 3. Listen for Scene Changes
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        GoalPanel.allGoalsEndedEvent -= OnGoalsCompleted;
        // Unsubscribe to prevent errors
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 4. This runs every time a new scene finishes loading
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Check if we loaded the "GameScene" before trying to run game logic
        // This prevents the game from trying to start in the Main Menu
        if (scene.name == "GameScene")
        {
            LoadLevel();
        }
        else
        {
            // If we are in the Menu, stop any running level timers from the previous game
            StopAllCoroutines();
            isLevelActive = false;
        }
    }

    // Note: Removed LoadLevel() from Start(), because OnSceneLoaded handles it now.
    private void Start()
    {
        // Optional: If you start directly in GameScene for testing, load manually
        if (SceneManager.GetActiveScene().name == "GameScene")
        {
            LoadLevel();
        }
    }

    // ... (Rest of your code remains exactly the same below) ...

    IEnumerator LevelTimer()
    {
        while (remainingTime > 0 && isLevelActive)
        {
            remainingTime -= Time.deltaTime;
            yield return null;
        }

        if (isLevelActive)
            LevelFailed();
    }

    public void LevelSuccess()
    {
        if (!isLevelActive) return;

        isLevelActive = false;
        levelSuccessEvent?.Invoke();
    }

    public void LoadNextLevel()
    {
        int next = PlayerPrefs.GetInt("Level", 0) + 1;
        PlayerPrefs.SetInt("Level", next);
        ReloadLevel();
    }

    public void ReloadLevel()
    {
        StopAllCoroutines();
        LoadLevel();
    }

    private void LoadLevel()
    {
        int index = PlayerPrefs.GetInt("Level", 0) % allLevels.Length;
        currentLevel = allLevels[index];

        levelLoadedEvent?.Invoke();

        if (GameManager.Instance != null)
            GameManager.Instance.StartLevel(currentLevel);

        remainingTime = currentLevel.timeLimit;
        isLevelActive = true;
        StartCoroutine(LevelTimer());
    }

    private void OnGoalsCompleted()
    {
        if (isLevelActive)
        {
            isLevelActive = false;
            levelSuccessEvent?.Invoke();
        }
    }

    public void LevelFailed()
    {
        if (!isLevelActive) return;
        isLevelActive = false;
        levelFailedEvent?.Invoke();
    }

    public void LoadGameSceneForLevel(int levelIndex)
    {
        PlayerPrefs.SetInt("Level", levelIndex);
        PlayerPrefs.Save();
        SceneManager.LoadScene("GameScene");
    }

    public void RestartCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}