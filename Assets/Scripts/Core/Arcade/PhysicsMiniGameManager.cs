using System;
using UnityEngine;

namespace AnimalFall.Core.Arcade
{
    public class PhysicsMiniGameManager : MonoBehaviour
    {
        public static PhysicsMiniGameManager Instance { get; private set; }

        [SerializeField] private ArcadeSessionData[] availableGames;

        public IArcadeMiniGame ActiveGame { get; private set; }
        public ArcadeSessionData ActiveConfig { get; private set; }
        public bool IsPlaying { get; private set; }
        public float RemainingTime { get; private set; }
        public int HighScore { get; private set; }

        public event Action OnGameStarted;
        public event Action<int> OnGameEnded;

        private Vector2 savedGravity;
        private int activeMiniGameIndex;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public ArcadeSessionData GetCurrentEventGame()
        {
            if (availableGames == null || availableGames.Length == 0) return null;

            int dayIndex = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 86400);
            activeMiniGameIndex = dayIndex % availableGames.Length;
            return availableGames[activeMiniGameIndex];
        }

        public bool TryStartGame(ArcadeSessionData config, IArcadeMiniGame gameInstance)
        {
            if (IsPlaying || config == null || gameInstance == null) return false;

            if (!ArcadeTokenService.Instance.SpendTokens(config.tokenCost))
            {
                Debug.Log("[PhysicsMiniGameManager] Not enough tokens.");
                return false;
            }

            ActiveConfig = config;
            ActiveGame = gameInstance;

            savedGravity = Physics2D.gravity;
            Physics2D.gravity = new Vector2(0, config.gravity);

            RemainingTime = config.timeLimit;
            IsPlaying = true;

            ActiveGame.Setup(config);
            ActiveGame.StartGame();
            OnGameStarted?.Invoke();

            LoadHighScore(config.gameType);
            return true;
        }

        private void Update()
        {
            if (!IsPlaying || ActiveGame == null) return;

            RemainingTime -= Time.deltaTime;
            ActiveGame.OnUpdate();

            if (ActiveGame.IsComplete || RemainingTime <= 0f)
            {
                FinishGame();
            }
        }

        private void FinishGame()
        {
            IsPlaying = false;
            Physics2D.gravity = savedGravity;

            int score = ActiveGame != null ? ActiveGame.CurrentScore : 0;
            ActiveGame?.EndGame();

            if (score > HighScore)
            {
                HighScore = score;
                SaveHighScore(ActiveConfig.gameType, score);
            }

            int coins = CalculateReward(score);
            AnimalFall.Services.SaveService.Instance?.AddCoins(coins);

            OnGameEnded?.Invoke(score);

            ActiveGame = null;
            ActiveConfig = null;
        }

        private int CalculateReward(int score)
        {
            if (ActiveConfig == null) return 0;
            int reward = ActiveConfig.baseRewardCoins;
            if (score >= ActiveConfig.targetCount)
                reward += ActiveConfig.perfectBonusCoins;
            return reward;
        }

        public void ForceEnd()
        {
            if (IsPlaying) FinishGame();
        }

        private void LoadHighScore(MiniGameType type)
        {
            HighScore = PlayerPrefs.GetInt("arcade_hs_" + type, 0);
        }

        private void SaveHighScore(MiniGameType type, int score)
        {
            PlayerPrefs.SetInt("arcade_hs_" + type, score);
            PlayerPrefs.Save();
        }
    }
}
