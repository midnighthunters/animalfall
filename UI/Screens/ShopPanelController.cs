using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AnimalFall.Core.PowerUps;
using AnimalFall.Core.Skins;
using AnimalFall.Managers;
using AnimalFall.Services.Save;

namespace AnimalFall.UI.Screens
{
    public class ShopPanelController : MonoBehaviour
    {
        [Header("Tabs")]
        [SerializeField] private Button powerUpsTab;
        [SerializeField] private Button skinsTab;
        [SerializeField] private Button buffsTab;
        [SerializeField] private Button closeButton;

        [Header("Content Panels")]
        [SerializeField] private GameObject powerUpsContent;
        [SerializeField] private GameObject skinsContent;
        [SerializeField] private GameObject buffsContent;

        [Header("Templates")]
        [SerializeField] private GameObject shopItemPrefab;

        [Header("Data")]
        [SerializeField] private PowerUpData[] availablePowerUps;
        [SerializeField] private SkinData[] availableSkins;

        [Header("Player Info")]
        [SerializeField] private TMP_Text coinBalanceText;

        private void OnEnable()
        {
            SetupTabs();
            ShowTab(powerUpsContent);
            UpdateCoinBalance();
            PopulatePowerUps();
            PopulateSkins();
        }

        private void SetupTabs()
        {
            powerUpsTab?.onClick.RemoveAllListeners();
            skinsTab?.onClick.RemoveAllListeners();
            buffsTab?.onClick.RemoveAllListeners();
            closeButton?.onClick.RemoveAllListeners();

            powerUpsTab?.onClick.AddListener(() => ShowTab(powerUpsContent));
            skinsTab?.onClick.AddListener(() => ShowTab(skinsContent));
            buffsTab?.onClick.AddListener(() => ShowTab(buffsContent));
            closeButton?.onClick.AddListener(() => gameObject.SetActive(false));
        }

        private void ShowTab(GameObject panel)
        {
            if (powerUpsContent != null) powerUpsContent.SetActive(false);
            if (skinsContent != null) skinsContent.SetActive(false);
            if (buffsContent != null) buffsContent.SetActive(false);
            if (panel != null) panel.SetActive(true);
        }

        private void UpdateCoinBalance()
        {
            if (coinBalanceText != null && SaveService.Instance != null)
                coinBalanceText.text = SaveService.Instance.GetCoins().ToString("N0");
        }

        private void PopulatePowerUps()
        {
            if (powerUpsContent == null || shopItemPrefab == null || availablePowerUps == null)
                return;

            foreach (Transform child in powerUpsContent.transform)
                Destroy(child.gameObject);

            foreach (var pu in availablePowerUps)
            {
                GameObject item = Instantiate(shopItemPrefab, powerUpsContent.transform);
                SetupShopItem(item, pu.displayName, pu.icon, pu.costCoins,
                    () => TryBuyPowerUp(pu));
            }
        }

        private void PopulateSkins()
        {
            if (skinsContent == null || shopItemPrefab == null || availableSkins == null)
                return;

            foreach (Transform child in skinsContent.transform)
                Destroy(child.gameObject);

            foreach (var skin in availableSkins)
            {
                GameObject item = Instantiate(shopItemPrefab, skinsContent.transform);
                SetupShopItem(item, skin.displayName, skin.previewSprite, skin.costCoins,
                    () => TryBuySkin(skin));
            }
        }

        private void SetupShopItem(GameObject item, string name, Sprite icon,
            int cost, UnityEngine.Events.UnityAction onBuy)
        {
            TMP_Text nameText = item.transform.Find("Name")?.GetComponent<TMP_Text>();
            Image iconImage = item.transform.Find("Icon")?.GetComponent<Image>();
            TMP_Text costText = item.transform.Find("Cost")?.GetComponent<TMP_Text>();
            Button buyButton = item.transform.Find("BuyButton")?.GetComponent<Button>();

            if (nameText != null) nameText.text = name;
            if (iconImage != null && icon != null) iconImage.sprite = icon;
            if (costText != null) costText.text = cost.ToString();
            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(onBuy);
            }
        }

        private void TryBuyPowerUp(PowerUpData pu)
        {
            if (ShopManager.Instance != null && ShopManager.Instance.TryBuy(pu))
                UpdateCoinBalance();
        }

        private void TryBuySkin(SkinData skin)
        {
            if (SaveService.Instance == null) return;

            int coins = SaveService.Instance.GetCoins();
            if (coins >= skin.costCoins)
            {
                SaveService.Instance.SpendCoins(skin.costCoins);
                SaveService.Instance.UnlockSkin(skin.skinId);
                UpdateCoinBalance();
            }
        }
    }
}
