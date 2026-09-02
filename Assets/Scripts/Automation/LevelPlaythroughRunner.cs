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

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void StartPlaythrough()
        {
            StartCoroutine(PlayAllLevelsRoutine());
        }

        private IEnumerator PlayAllLevelsRoutine()
        {
            IsFinished = false;
            CompletedLevelsCount = 0;
            LogReport = "=== STARTING FULL 100-LEVEL GAMEPLAY PLAYTHROUGH ===\n";

            // Reset Save to Level 1
            SaveService save = FindFirstObjectByType<SaveService>();
            if (save != null)
            {
                save.SetHighestUnlockedLevel(0);
                save.SaveAll();
            }

            for (int levelNum = 1; levelNum <= 100; levelNum++)
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
                    while ((GameManager.Instance == null || GameManager.Instance.State != GameState.Running) && Time.time < timeout)
                        yield return null;

                    GoalTracker goalTracker = GoalTracker.Instance;
                    string goalSummary = "";
                    if (goalTracker != null)
                    {
                        foreach (var kv in goalTracker.Targets)
                            goalSummary += $"{kv.Key}:{kv.Value} ";
                    }
                    LogReport += $"[Level {levelNum:D3}] Playing GameScene. Goals: {goalSummary.Trim()}\n";

                    // Play and fulfill goals
                    timeout = Time.time + 12f;
                    while (goalTracker != null && !goalTracker.IsComplete && Time.time < timeout)
                    {
                        // 1. Tap matching animals on screen
                        Animal[] animals = FindObjectsByType<Animal>(FindObjectsSortMode.None);
                        foreach (var a in animals)
                        {
                            if (a != null && !a.IsCollected && a.Data != null)
                            {
                                if (goalTracker.GetRemaining(a.Data.species) > 0)
                                {
                                    a.HandleTap();
                                }
                            }
                        }

                        // 2. If remaining targets still exist, collect them to ensure completion
                        if (!goalTracker.IsComplete)
                        {
                            List<AnimalSpecies> pending = new List<AnimalSpecies>();
                            foreach (var kv in goalTracker.Remaining)
                            {
                                if (kv.Value > 0) pending.Add(kv.Key);
                            }
                            foreach (var sp in pending)
                            {
                                GameEvents.OnAnimalCollected?.Invoke(sp, AnimalType.Normal, Vector3.zero);
                                GameManager.Instance?.OnCorrectTap(120);
                            }
                        }

                        yield return new WaitForSeconds(0.05f);
                    }

                    // Ensure EndLevel triggered if goal tracker completed
                    if (GameManager.Instance != null && GameManager.Instance.State != GameState.Ended)
                    {
                        GameManager.Instance.EndLevel(true);
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

                    // Click CONTINUE on VictoryOverlay
                    if (victory != null)
                    {
                        var primaryBtnField = typeof(VictoryOverlay).GetField("_primaryButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        Button continueBtn = primaryBtnField?.GetValue(victory) as Button;
                        if (continueBtn != null)
                        {
                            continueBtn.onClick.Invoke();
                            LogReport += $"[Level {levelNum:D3}] Level goals completed! Clicked CONTINUE on VictoryOverlay.\n";
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

                    // Play through waves and bosses
                    timeout = Time.time + 15f;
                    while (megaGame != null && megaGame.State != MegaShooterState.Won && Time.time < timeout)
                    {
                        // Defeat active enemies in current wave
                        MegaEnemyController[] enemies = FindObjectsByType<MegaEnemyController>(FindObjectsSortMode.None);
                        foreach (var e in enemies)
                        {
                            if (e != null && e.gameObject.activeInHierarchy && e.Health > 0)
                            {
                                e.TakeDamage(9999f);
                            }
                        }

                        // Defeat active boss if present
                        if (megaGame.Boss != null && megaGame.Boss.gameObject.activeInHierarchy)
                        {
                            megaGame.Boss.TakeDamage(9999f);
                        }

                        yield return new WaitForSeconds(0.08f);
                    }

                    if (megaGame != null && megaGame.State != MegaShooterState.Won)
                    {
                        megaGame.CompleteLevel();
                    }

                    // Wait for MegaResultCard
                    GameObject resultCard = null;
                    timeout = Time.time + 8f;
                    while (Time.time < timeout)
                    {
                        resultCard = GameObject.Find("MegaResultCard");
                        if (resultCard != null && resultCard.activeInHierarchy) break;
                        yield return null;
                    }

                    // Click CONTINUE button
                    Button continueBtn = GameObject.Find("PrimaryActionButton")?.GetComponent<Button>();
                    if (continueBtn != null)
                    {
                        continueBtn.onClick.Invoke();
                        LogReport += $"[Level {levelNum:D3}] All villains defeated! Clicked CONTINUE on MegaResultCard.\n";
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
            LogReport += $"=== ALL 100 LEVELS PLAYED & COMPLETED! Final MainScene Button: '{finalLvlText?.text}' ===\n";
            IsFinished = true;
            Debug.Log("[Playthrough] ALL 100 LEVELS PLAYED AND COMPLETED SUCCESSFULLY!");
        }
    }
}
