// Task 4.1 — HindranceContext struct: carries manager refs to hindrances
using AnimalFall.Managers;
using AnimalFall.Effects;

namespace AnimalFall.Core.Hindrances
{
    public struct HindranceContext
    {
        public GameManager       GameManager;
        public HindranceManager  HindranceManager;
        public EnvironmentEffects EnvironmentEffects;
        public ScreenEffects      ScreenEffects;
        public AudioManager       AudioManager;
        public LivesManager       LivesManager;
        public InputManager       InputManager;
    }
}
