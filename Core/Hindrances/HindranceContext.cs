using AnimalFall.Core.Animals;
using AnimalFall.Effects;
using AnimalFall.Managers;

namespace AnimalFall.Core.Hindrances
{
    public class HindranceContext
    {
        public GameManager GameManager;
        public InputManager InputManager;
        public Spawner Spawner;
        public LivesManager LivesManager;
        public ScreenEffects ScreenEffects;
        public EnvironmentEffects EnvironmentEffects;
        public AudioManager AudioManager;
        public ScoreManager ScoreManager;

        public static HindranceContext Create()
        {
            return new HindranceContext
            {
                GameManager = GameManager.Instance,
                InputManager = InputManager.Instance,
                LivesManager = LivesManager.Instance,
                ScreenEffects = ScreenEffects.Instance,
                EnvironmentEffects = EnvironmentEffects.Instance,
                AudioManager = AudioManager.Instance,
                ScoreManager = ScoreManager.Instance
            };
        }
    }
}
