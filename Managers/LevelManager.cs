using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using AnimalFall.Core.Levels;
using AnimalFall.Core.Goals;

namespace AnimalFall.Managers
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [SerializeField] private LevelData[] allLevels;

        public int CurrentLevelIndex { get; private set; }
        public LevelData CurrentLevelData { get; private set; }
        public int TotalLevels => allLevels != null ? allLevels.Length : 0;

        public static event Action LevelLoadedEvent;
        public static event Action LevelSuccessEvent;
        public static event Action LevelFailedEvent;

        private bool isLevelActive;

        private const string HighestCompletedLevelKey = "HighestCompletedLevel";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            GoalPanel.AllGoalsCompleted += OnGoalsCompleted;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            GoalPanel.AllGoalsCompleted -= OnGoalsCompleted;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "GameScene")
                LoadLevel();
            else if (scene.name == "MainScene")
            {
                isLevelActive = false;
                StopAllCoroutines();
            }
        }

        public void LoadGameSceneForLevel(int levelIndex)
        {
            CurrentLevelIndex = levelIndex;
            SceneManager.LoadScene("GameScene");
        }

        private void LoadLevel()
        {
            if (allLevels == null || allLevels.Length == 0) return;

            if (CurrentLevelIndex >= allLevels.Length)
                CurrentLevelIndex = 0;

            CurrentLevelData = allLevels[CurrentLevelIndex];
            LevelLoadedEvent?.Invoke();

            if (GameManager.Instance != null)
                GameManager.Instance.StartLevel(CurrentLevelData);

            isLevelActive = true;
        }

        private void OnGoalsCompleted()
        {
            if (isLevelActive) LevelSuccess();
        }

        public void LevelSuccess()
        {
            if (!isLevelActive) return;
            isLevelActive = false;

            int highestCompleted = GetHighestUnlockedLevel();
            if (CurrentLevelIndex == highestCompleted)
            {
                PlayerPrefs.SetInt(HighestCompletedLevelKey, highestCompleted + 1);
                PlayerPrefs.Save();
            }

            LevelSuccessEvent?.Invoke();
            StartCoroutine(ReturnToMainScene(2f));
        }

        public void LevelFailed()
        {
            if (!isLevelActive) return;
            isLevelActive = false;
            LevelFailedEvent?.Invoke();
        }

        public int GetHighestUnlockedLevel()
        {
            return PlayerPrefs.GetInt(HighestCompletedLevelKey, 0);
        }

        private IEnumerator ReturnToMainScene(float delay)
        {
            yield return new WaitForSeconds(delay);
            SceneManager.LoadScene("MainScene");
        }

#if UNITY_EDITOR
        [UnityEngine.ContextMenu("Reset Progress")]
        public void ResetAllProgress()
        {
            PlayerPrefs.DeleteKey(HighestCompletedLevelKey);
            PlayerPrefs.Save();
            Debug.Log("[LevelManager] Progress reset.");
        }
#endif
    }
}
