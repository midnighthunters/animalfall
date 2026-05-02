// ============================================================
//  ShopManager.cs  –  Animal Fall  (REFACTORED)
//  Changes:
//    • SaveManager replaces PlayerPrefs coin calls
//    • EventBus subscription for coin display refresh
//    • Inventory stored in SaveManager.Data.powerUpInventory
// ============================================================

using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [SerializeField] private PowerUpData[] storeItems;

    // ── Buy ───────────────────────────────────────────────────
    public bool Buy(PowerUpData item)
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("[ShopManager] SaveManager not found.");
            return false;
        }

        if (!SaveManager.Instance.SpendCoins(item.costCoins))
        {
            UIManager.FindObjectOfType<UIManager>()?.ShowMessage("Not enough coins!");
            AudioManager.Instance?.PlaySFX(AudioManager.SfxType.UIBack);
            return false;
        }

        // Add to inventory
        string key = item.type.ToString();
        SaveManager.Instance.Data.AddPowerUp(key, item.usesPerLevel);
        SaveManager.Instance.Save();

        UIManager.FindObjectOfType<UIManager>()?.ShowMessage($"Bought {item.displayName}!");
        AudioManager.Instance?.PlaySFX(AudioManager.SfxType.CoinPickup);
        return true;
    }

    // ── Use in level ──────────────────────────────────────────
    public bool UseFromInventory(PowerUpData item)
    {
        string key = item.type.ToString();
        if (SaveManager.Instance == null || !SaveManager.Instance.Data.SpendPowerUp(key))
            return false;

        SaveManager.Instance.Save();
        PowerUpManager.Instance?.UsePowerUp(item);
        EventBus.Publish(new OnPowerUpActivated { type = item.type, duration = item.duration });
        AudioManager.Instance?.PlaySFX(AudioManager.SfxType.PowerUp);
        return true;
    }

    public int GetInventoryCount(PowerUpData item) =>
        SaveManager.Instance?.Data.GetPowerUpCount(item.type.ToString()) ?? 0;
}
