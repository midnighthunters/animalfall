namespace AnimalFall.Core.Arcade
{
    public interface IArcadeMiniGame
    {
        MiniGameType GameType { get; }
        void Setup(ArcadeSessionData config);
        void StartGame();
        void EndGame();
        void OnUpdate();
        bool IsComplete { get; }
        int CurrentScore { get; }
    }
}
