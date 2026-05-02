using UnityEngine;
using AnimalFall.Core.PowerUps;
using AnimalFall.Services.Save;

namespace AnimalFall.Managers
{
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool TryBuy(PowerUpData powerUp)
        {
            if (SaveService.Instance == null || powerUp == null) return false;

            int coins = SaveService.Instance.GetCoins();
            if (coins < powerUp.costCoins) return false;

            SaveService.Instance.SpendCoins(powerUp.costCoins);
            AddPowerUpToInventory(powerUp);
            return true;
        }

        private void AddPowerUpToInventory(PowerUpData powerUp)
        {
            string key = "inventory_" + powerUp.name;
            int count = PlayerPrefs.GetInt(key, 0);
            PlayerPrefs.SetInt(key, count + 1);
            PlayerPrefs.Save();
        }

        public int GetInventoryCount(string powerUpName)
        {
            return PlayerPrefs.GetInt("inventory_" + powerUpName, 0);
        }

        public bool ConsumePowerUp(string powerUpName)
        {
            string key = "inventory_" + powerUpName;
            int count = PlayerPrefs.GetInt(key, 0);
            if (count <= 0) return false;

            PlayerPrefs.SetInt(key, count - 1);
            PlayerPrefs.Save();
            return true;
        }
    }
}
