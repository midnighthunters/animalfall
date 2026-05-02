using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnimalFall.Managers
{
    public enum GameState
    {
        Splash,
        Auth,
        MainMenu,
        Game,
        MegaLevel,
        Paused,
        Results,
        ArcadeRoom
    }

    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        public GameState CurrentState { get; private set; }
        public GameState PreviousState { get; private set; }

        public event Action<GameState, GameState> OnStateChanged;

        private GameState stateBeforePause;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CurrentState = GameState.Splash;
        }

        public void TransitionTo(GameState newState)
        {
            if (CurrentState == newState) return;

            PreviousState = CurrentState;
            CurrentState = newState;
            OnStateChanged?.Invoke(PreviousState, newState);

            switch (newState)
            {
                case GameState.Splash:
                    SceneManager.LoadScene("SplashScene");
                    break;
                case GameState.Auth:
                    SceneManager.LoadScene("AuthScene");
                    break;
                case GameState.MainMenu:
                    Time.timeScale = 1f;
                    SceneManager.LoadScene("MainScene");
                    break;
                case GameState.Game:
                case GameState.MegaLevel:
                    Time.timeScale = 1f;
                    SceneManager.LoadScene("GameScene");
                    break;
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
                case GameState.Results:
                    break;
                case GameState.ArcadeRoom:
                    Time.timeScale = 1f;
                    SceneManager.LoadScene("ArcadeScene");
                    break;
            }
        }

        public void PushPause()
        {
            if (CurrentState == GameState.Paused) return;
            stateBeforePause = CurrentState;
            TransitionTo(GameState.Paused);
        }

        public void PopPause()
        {
            if (CurrentState != GameState.Paused) return;
            Time.timeScale = 1f;
            PreviousState = GameState.Paused;
            CurrentState = stateBeforePause;
            OnStateChanged?.Invoke(GameState.Paused, CurrentState);
        }

        public bool IsPlaying =>
            CurrentState == GameState.Game || CurrentState == GameState.MegaLevel;

        public bool IsInArcade => CurrentState == GameState.ArcadeRoom;
    }
}
