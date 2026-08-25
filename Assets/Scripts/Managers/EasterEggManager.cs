using System;
using System.Collections;
using UnityEngine;
using AnimalFall.Services.Save;

namespace AnimalFall.Managers
{
    public class EasterEggManager : MonoBehaviour
    {
        public static EasterEggManager Instance { get; private set; }

        public bool IsGoldenSkinUnlocked { get; private set; }
        public event Action<string> OnEasterEggTriggered;

        [Header("Easter Egg Settings")]
        [SerializeField] private int cloudTapsRequired = 10;
        [SerializeField] private float coinRainDuration = 2f;
        [SerializeField] private int coinRainAmount = 50;
        [SerializeField] private float rainbowSpawnChance = 0.001f;
        [SerializeField] private int rainbowCoinBonus = 500;

        private int[] cornerTapSequence = new int[4];
        private int cornerTapIndex;
        private int cloudTapCount;
        private float lastCloudTapTime;
        private bool shopkeeperDragged;

        private const string GoldenSkinKey = "easter_golden_skin";
        private const string RainbowAchievementKey = "easter_rainbow";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            IsGoldenSkinUnlocked = PlayerPrefs.GetInt(GoldenSkinKey, 0) == 1;
        }

        public void OnCornerTapped(int cornerIndex)
        {
            if (IsGoldenSkinUnlocked) return;

            int[] konamiCorners = { 0, 1, 2, 3 };

            if (cornerIndex == konamiCorners[cornerTapIndex])
            {
                cornerTapIndex++;
                if (cornerTapIndex >= konamiCorners.Length)
                {
                    UnlockGoldenSkin();
                    cornerTapIndex = 0;
                }
            }
            else
            {
                cornerTapIndex = 0;
            }
        }

        private void UnlockGoldenSkin()
        {
            IsGoldenSkinUnlocked = true;
            PlayerPrefs.SetInt(GoldenSkinKey, 1);
            PlayerPrefs.Save();

            SaveService.Instance?.UnlockSkin("golden_animal");
            OnEasterEggTriggered?.Invoke("golden_skin");

            Debug.Log("[EasterEgg] Golden Animal skin unlocked!");
        }

        public bool CheckRainbowSpawn()
        {
            return UnityEngine.Random.value < rainbowSpawnChance;
        }

        public void OnRainbowCollected()
        {
            SaveService.Instance?.AddCoins(rainbowCoinBonus);
            PlayerPrefs.SetInt(RainbowAchievementKey, 1);
            PlayerPrefs.Save();

            OnEasterEggTriggered?.Invoke("rainbow_animal");
            Debug.Log("[EasterEgg] Rainbow Animal collected! Bonus coins awarded.");
        }

        public void OnBackgroundCloudTapped()
        {
            float now = Time.time;
            if (now - lastCloudTapTime > 2f)
                cloudTapCount = 0;

            lastCloudTapTime = now;
            cloudTapCount++;

            if (cloudTapCount >= cloudTapsRequired)
            {
                cloudTapCount = 0;
                StartCoroutine(CoinRain());
                OnEasterEggTriggered?.Invoke("cloud_coin_rain");
            }
        }

        private IEnumerator CoinRain()
        {
            float elapsed = 0f;
            int coinsPerTick = coinRainAmount / 10;

            while (elapsed < coinRainDuration)
            {
                SaveService.Instance?.AddCoins(coinsPerTick);
                elapsed += coinRainDuration / 10f;
                yield return new WaitForSeconds(coinRainDuration / 10f);
            }
        }

        public void OnShopkeeperDraggedOffScreen()
        {
            if (shopkeeperDragged) return;
            shopkeeperDragged = true;

            string today = DateTime.UtcNow.ToString("yyyyMMdd");
            string lastClaim = PlayerPrefs.GetString("shopkeeper_drag_day", "");

            if (lastClaim != today)
            {
                PlayerPrefs.SetString("shopkeeper_drag_day", today);
                PlayerPrefs.Save();

                int reward = UnityEngine.Random.Range(20, 80);
                SaveService.Instance?.AddCoins(reward);
                OnEasterEggTriggered?.Invoke("shopkeeper_daily");

                Debug.Log($"[EasterEgg] Shopkeeper daily reward: {reward} coins!");
            }
        }

        public void ResetDailyFlags()
        {
            shopkeeperDragged = false;
            cloudTapCount = 0;
        }
    }
}
