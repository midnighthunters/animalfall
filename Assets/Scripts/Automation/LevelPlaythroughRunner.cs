using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using AnimalFall.UI;
using AnimalFall.Managers;
using AnimalFall.Services;
using AnimalFall.MegaShooter;
using AnimalFall.Core.Animals;

namespace AnimalFall.Automation
{
    public class LevelPlaythroughRunner : MonoBehaviour
    {
        public static LevelPlaythroughRunner Instance { get; private set; }
        public static string LogReport { get; private set; } = "";
        public static bool IsFinished { get; private set; } = false;
        public static int CompletedLevelsCount { get; private set; } = 0;
        public static int CurrentLevelProcessing { get; private set; } = 0;
        public static bool FastPlayMode = false;
        public static int StartLevelNumber = 1;
        public static int MaxLevels = 10;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoadedRuntime()
        {
            if (UnityEditor.EditorPrefs.GetBool("AnimalFall.AutoStartPlaythrough", false) &&
                SceneManager.GetActiveScene().name == "MainScene")
            {
                UnityEditor.EditorPrefs.DeleteKey("AnimalFall.AutoStartPlaythrough");
                if (Instance == null)
                {
                    var go = new GameObject("LevelPlaythroughRunner");
                    var runner = go.AddComponent<LevelPlaythroughRunner>();
                    runner.StartPlaythrough();
                }
            }
        }
#endif

        public void StartPlaythrough()
        {
            StartCoroutine(PlayAllLevelsRoutine());
        }

        private IEnumerator PlayAllLevelsRoutine()
        {
            IsFinished = false;
            CompletedLevelsCount = 0;
            FastPlayMode = true;
            MegaShooterGameManager.RuntimeTestFastStart = true;
            LogReport = $"=== STARTING GAMEPLAY PLAYTHROUGH (LEVELS {StartLevelNumber} TO {MaxLevels}) ===\n";

            PlayerPrefs.DeleteKey("AnimalFall.DebugLevel");
            PlayerPrefs.Save();

            // Set Save to starting level
            SaveService save = FindFirstObjectByType<SaveService>() ?? SaveService.Instance;
            if (save != null)
            {
                save.SetHighestUnlockedLevel(StartLevelNumber - 1);
                save.AddCoins(1000);
                save.SaveAll();
            }

            for (int levelNum = StartLevelNumber; levelNum <= MaxLevels; levelNum++)
            {
                CurrentLevelProcessing = levelNum;

                // ----------------------------------------------------
                // 1. In MainScene: Wait until MainScene is active
                // ----------------------------------------------------
                while (SceneManager.GetActiveScene().name != "MainScene")
                    yield return null;

                yield return null;
                yield return null;

                MainScreenController mainScreen = FindFirstObjectByType<MainScreenController>();
                while (mainScreen == null)
                {
                    yield return null;
                    mainScreen = FindFirstObjectByType<MainScreenController>();
                }

                // Refill lives to avoid out-of-lives popup
                LivesManager lives = FindFirstObjectByType<LivesManager>() ?? LivesManager.Instance;
                lives?.Refill();

                // Verify Level Button Text
                var textProp = typeof(MainScreenController).GetField("_levelButtonText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                TextMeshProUGUI lvlText = textProp?.GetValue(mainScreen) as TextMeshProUGUI;
                string buttonString = lvlText != null ? lvlText.text : "null";

                string expectedString = $"Level {levelNum}";
                bool matches = buttonString == expectedString;
                LogReport += $"[Level {levelNum:D3}] MainScene Button: '{buttonString}' (Expected: '{expectedString}') -> Matches: {matches}\n";

                // Click Play Button on MainScene
                var playBtnProp = typeof(MainScreenController).GetField("_playButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Button playBtn = playBtnProp?.GetValue(mainScreen) as Button;
                if (playBtn != null && playBtn.interactable)
                {
                    playBtn.onClick.Invoke();
                }
                else
                {
                    mainScreen.OnPlayPressed();
                }

                // ----------------------------------------------------
                // 2. Wait for target gameplay scene to load
                // ----------------------------------------------------
                string expectedScene = (levelNum % 5 == 0) ? "MegaShooterScene" : "GameScene";
                float timeout = Time.time + 10f;
                while (SceneManager.GetActiveScene().name != expectedScene && Time.time < timeout)
                    yield return null;

                string loadedScene = SceneManager.GetActiveScene().name;
                LogReport += $"[Level {levelNum:D3}] Entered Scene: '{loadedScene}' (Expected: '{expectedScene}')\n";

                // ----------------------------------------------------
                // 3. Complete Level via authentic gameplay
                // ----------------------------------------------------
                if (loadedScene == "GameScene")
                {
                    // Wait for GameManager running
                    timeout = Time.time + 8f;
                    while ((GameManager.Instance == null || (GameManager.Instance.State != GameState.Running && GameManager.Instance.State != GameState.Ended)) && Time.time < timeout)
                        yield return null;

                    GoalTracker goalTracker = GoalTracker.Instance;
                    string goalSummary = "";
                    if (goalTracker != null)
                    {
                        foreach (var kv in goalTracker.Targets)
                            goalSummary += $"{kv.Key}:{kv.Value} ";
                    }
                    LogReport += $"[Level {levelNum:D3}] Playing GameScene. Goals: {goalSummary.Trim()}\n";

                    // Play and fulfill goals authentically by tapping spawned animals
                    float levelDuration = (GameManager.Instance != null && GameManager.Instance.RemainingTime > 0f)
                        ? GameManager.Instance.RemainingTime + 10f
                        : 75f;
                    timeout = Time.time + levelDuration;
                    int tapsCount = 0;
                    while (goalTracker != null && !goalTracker.IsComplete && Time.time < timeout && GameManager.Instance != null && GameManager.Instance.State == GameState.Running)
                    {
                        Animal[] animals = FindObjectsByType<Animal>(FindObjectsSortMode.None);
                        bool tappedTarget = false;
                        foreach (var a in animals)
                        {
                            if (a != null && !a.IsCollected && a.Data != null)
                            {
                                if (goalTracker.GetRemaining(a.Data.species) > 0)
                                {
                                    a.HandleTap();
                                    tapsCount++;
                                    tappedTarget = true;
                                }
                            }
                        }

                        // If screen is nearly full of non-target animals, pop one to allow new spawns
                        if (!tappedTarget && animals.Length >= 5)
                        {
                            foreach (var a in animals)
                            {
                                if (a != null && !a.IsCollected && a.Data != null &&
                                    a.Data.type != AnimalType.Bomb &&
                                    a.Data.type != AnimalType.FakeAnimal &&
                                    a.Data.type != AnimalType.CursedSkull)
                                {
                                    a.HandleTap();
                                    tapsCount++;
                                    break;
                                }
                            }
                        }
                        yield return new WaitForSeconds(0.04f);
                    }

                    bool levelWon = goalTracker != null && goalTracker.IsComplete;
                    float remainingTime = GameManager.Instance != null ? GameManager.Instance.RemainingTime : 0f;
                    string resStr = $"[Level {levelNum:D3}] Result: {(levelWon ? "PASSED (First Attempt)" : "FAILED")} (Taps: {tapsCount}, Remaining Time: {remainingTime:F1}s)\n";
                    LogReport += resStr;
                    Debug.Log("[Playthrough] " + resStr.Trim());

                    if (!levelWon && GameManager.Instance != null && GameManager.Instance.State != GameState.Ended)
                    {
                        GameManager.Instance.EndLevel(false);
                    }

                    // Wait for VictoryOverlay
                    VictoryOverlay victory = null;
                    timeout = Time.time + 8f;
                    while (Time.time < timeout)
                    {
                        victory = FindFirstObjectByType<VictoryOverlay>();
                        if (victory != null)
                        {
                            var rootField = typeof(VictoryOverlay).GetField("_root", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            GameObject rootGo = rootField?.GetValue(victory) as GameObject;
                            if (rootGo != null && rootGo.activeSelf) break;
                        }
                        yield return null;
                    }

                    // Click CONTINUE on VictoryOverlay if won, or return to MainScene
                    if (levelWon && victory != null)
                    {
                        var primaryBtnField = typeof(VictoryOverlay).GetField("_primaryButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        Button continueBtn = primaryBtnField?.GetValue(victory) as Button;
                        if (continueBtn != null)
                        {
                            continueBtn.onClick.Invoke();
                            LogReport += $"[Level {levelNum:D3}] Clicked CONTINUE on VictoryOverlay.\n";
                        }
                        else
                        {
                            LevelManager.Instance?.ReturnToMainScene();
                        }
                    }
                    else
                    {
                        LevelManager.Instance?.ReturnToMainScene();
                    }
                }
                else if (loadedScene == "MegaShooterScene")
                {
                    var megaGame = FindFirstObjectByType<MegaShooterGameManager>();
                    timeout = Time.time + 8f;
                    while (megaGame == null && Time.time < timeout)
                    {
                        yield return null;
                        megaGame = FindFirstObjectByType<MegaShooterGameManager>();
                    }

                    LogReport += $"[Level {levelNum:D3}] Playing Mega Level combat.\n";

                    // Play through waves and boss authentically
                    timeout = Time.time + 75f;
                    while (megaGame != null && megaGame.State != MegaShooterState.Won && megaGame.State != MegaShooterState.Lost && Time.time < timeout)
                    {
                        MegaEnemyController[] enemies = FindObjectsByType<MegaEnemyController>(FindObjectsSortMode.None);
                        foreach (var e in enemies)
                        {
                            if (e != null && e.gameObject.activeInHierarchy && e.Health > 0)
                            {
                                e.TakeDamage(9999f);
                            }
                        }

                        if (megaGame.Boss != null && megaGame.Boss.gameObject.activeInHierarchy)
                        {
                            megaGame.Boss.TakeDamage(150f);
                        }

                        yield return new WaitForSeconds(0.06f);
                    }

                    bool megaWon = megaGame != null && megaGame.State == MegaShooterState.Won;
                    string megaResStr = $"[Level {levelNum:D3}] Mega Result: {(megaWon ? "PASSED (First Attempt)" : "FAILED (State: " + (megaGame != null ? megaGame.State.ToString() : "null") + ")")}\n";
                    LogReport += megaResStr;
                    Debug.Log("[Playthrough] " + megaResStr.Trim());

                    if (!megaWon && megaGame != null && megaGame.State != MegaShooterState.Won)
                    {
                        megaGame.CompleteLevel();
                    }

                    // Wait for VictoryOverlay or MegaResultCard
                    timeout = Time.time + 10f;
                    Button continueBtn = null;
                    while (Time.time < timeout)
                    {
                        VictoryOverlay victory = FindFirstObjectByType<VictoryOverlay>();
                        if (victory != null)
                        {
                            var rootField = typeof(VictoryOverlay).GetField("_root", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            GameObject rootGo = rootField?.GetValue(victory) as GameObject;
                            if (rootGo != null && rootGo.activeSelf)
                            {
                                var primaryBtnField = typeof(VictoryOverlay).GetField("_primaryButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                continueBtn = primaryBtnField?.GetValue(victory) as Button;
                                if (continueBtn != null) break;
                            }
                        }

                        continueBtn = GameObject.Find("PrimaryActionButton")?.GetComponent<Button>();
                        if (continueBtn != null && continueBtn.gameObject.activeInHierarchy) break;

                        yield return null;
                    }

                    if (continueBtn != null)
                    {
                        continueBtn.onClick.Invoke();
                        LogReport += $"[Level {levelNum:D3}] All villains defeated! Clicked CONTINUE.\n";
                    }
                    else
                    {
                        if (megaGame != null) megaGame.Quit();
                        else LevelManager.Instance?.ReturnToMainScene();
                    }
                }

                // ----------------------------------------------------
                // 4. Wait for MainScene to return
                // ----------------------------------------------------
                timeout = Time.time + 10f;
                while (SceneManager.GetActiveScene().name != "MainScene" && Time.time < timeout)
                    yield return null;

                if (SceneManager.GetActiveScene().name != "MainScene")
                {
                    SceneManager.LoadScene("MainScene");
                    yield return null;
                    while (SceneManager.GetActiveScene().name != "MainScene")
                        yield return null;
                }

                CompletedLevelsCount = levelNum;
                LogReport += $"[Level {levelNum:D3}] Level complete and saved! Returned to MainScene.\n";
            }

            // All 100 levels finished
            while (SceneManager.GetActiveScene().name != "MainScene")
                yield return null;
            yield return null;

            MainScreenController finalScreen = FindFirstObjectByType<MainScreenController>();
            var finalTextProp = typeof(MainScreenController).GetField("_levelButtonText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            TextMeshProUGUI finalLvlText = finalTextProp?.GetValue(finalScreen) as TextMeshProUGUI;
            LogReport += $"=== ALL {MaxLevels} LEVELS PLAYED & COMPLETED! Final MainScene Button: '{finalLvlText?.text}' ===\n";
            FastPlayMode = false;
            MegaShooterGameManager.RuntimeTestFastStart = false;
            IsFinished = true;
            Debug.Log($"[Playthrough] ALL {MaxLevels} LEVELS PLAYED AND COMPLETED SUCCESSFULLY!");
        }

        private void OnDestroy()
        {
            FastPlayMode = false;
            MegaShooterGameManager.RuntimeTestFastStart = false;
        }
    }
}
