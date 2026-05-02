using UnityEngine;

namespace AnimalFall.Core.Arcade
{
    public class ArcadeTokenService : MonoBehaviour
    {
        public static ArcadeTokenService Instance { get; private set; }

        private const string TokenKey = "arcade_tokens";

        public int CurrentTokens => PlayerPrefs.GetInt(TokenKey, 0);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void AddTokens(int amount)
        {
            int current = CurrentTokens + amount;
            PlayerPrefs.SetInt(TokenKey, Mathf.Max(0, current));
            PlayerPrefs.Save();
        }

        public bool SpendTokens(int amount)
        {
            int current = CurrentTokens;
            if (current < amount) return false;

            PlayerPrefs.SetInt(TokenKey, current - amount);
            PlayerPrefs.Save();
            return true;
        }

        public bool HasTokens(int amount) => CurrentTokens >= amount;

        public void AwardForLevelComplete(bool isMegaLevel, int stars)
        {
            int tokens = isMegaLevel ? 3 : 1;
            if (stars >= 3) tokens += 1;
            AddTokens(tokens);
        }
    }
}
