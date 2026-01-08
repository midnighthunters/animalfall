using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public PowerUpData[] storeItems;
    public UIManager ui;

    public void Buy(PowerUpData item)
    {
        int coins = SaveManager.Instance.GetCoins();
        if (coins >= item.costCoins)
        {
            SaveManager.Instance.SpendCoins(item.costCoins);
            // grant item: add to inventory, or directly enable in-level use
            ui.ShowMessage($"Bought {item.displayName}");
        }
        else ui.ShowMessage("Not enough coins");
    }
}
