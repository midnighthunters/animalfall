using UnityEngine;
using AnimalFall.Core.PowerUps;
using AnimalFall.Services.Save;
using AnimalFall.UI;

namespace AnimalFall.Managers
{
    public class ShopManager : MonoBehaviour
    {
        [SerializeField] private PowerUpData[] storeItems;
        [SerializeField] private GameUIManager ui;

        public PowerUpData[] StoreItems => storeItems;

        public bool TryBuy(PowerUpData item)
        {
            if (SaveService.Instance == null) return false;

            int coins = SaveService.Instance.GetCoins();
            if (coins >= item.costCoins)
            {
                SaveService.Instance.SpendCoins(item.costCoins);
                ui?.ShowToast($"Purchased {item.displayName}!");
                return true;
            }

            ui?.ShowToast("Not enough coins");
            return false;
        }
    }
}
