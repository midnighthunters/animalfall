using System;
using UnityEngine;
using UnityEngine.Purchasing;

public class IAPManager : MonoBehaviour, IStoreListener
{
    public static IAPManager Instance { get; private set; }

    // Product IDs - these must match the product IDs you create in Unity IAP Catalog / Play Console / App Store
    public const string COINS_SMALL = "coins_small";
    public const string COINS_MEDIUM = "coins_medium";
    public const string COINS_LARGE = "coins_large";
    public const string BUNDER_STARTER = "bundle_starter";



    private IStoreController storeController;
    private IExtensionProvider storeExtensionProvider;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (!IsInitialized()) InitializePurchasing();
    }

    public void InitializePurchasing()
    {
        if (IsInitialized()) return;

        var module = StandardPurchasingModule.Instance();
        var builder = ConfigurationBuilder.Instance(module);

        // Define products - mark coins as consumable
        builder.AddProduct(COINS_SMALL, ProductType.Consumable);
        builder.AddProduct(COINS_MEDIUM, ProductType.Consumable);
        builder.AddProduct(COINS_LARGE, ProductType.Consumable);
        builder.AddProduct(BUNDER_STARTER, ProductType.Consumable);

        UnityPurchasing.Initialize(this, builder);
        Debug.Log("[IAP] Initializing purchasing...");
    }

    public bool IsInitialized()
    {
        return storeController != null && storeExtensionProvider != null;
    }

    // Public helper to start purchase
    public void BuyCoins100()
    {
        BuyProductID(COINS_SMALL);
    }
    public void BuyProduct(string productId)
    {
        BuyProductID(productId);
    }

    private void BuyProductID(string productId)
    {
        if (!IsInitialized())
        {
            Debug.LogWarning("[IAP] BuyProductID FAIL. Not initialized.");
            return;
        }

        Product product = storeController.products.WithID(productId);

        if (product != null && product.availableToPurchase)
        {
            Debug.Log($"[IAP] Purchasing product asychronously: '{product.definition.id}'");
            storeController.InitiatePurchase(product);
        }
        else
        {
            Debug.LogWarning("[IAP] BuyProductID: FAIL. Product not found or not available for purchase.");
        }
    }

    // IStoreListener callbacks
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        Debug.Log("[IAP] OnInitialized");
        storeController = controller;
        storeExtensionProvider = extensions;
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogError("[IAP] OnInitializeFailed: " + error);
    }

    // Add a reference to your ShopPanelController or a data source
    // For simplicity, let's just make a simple lookup inside IAPManager or use a static config.

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        string id = args.purchasedProduct.definition.id;
        Debug.Log($"[IAP] ProcessPurchase: {id}");

        // 1. Check if it's a known product
        // Ideally, you have a separate "GameData" class, but for now, let's switch:

        int coinsToGrant = 0;

        // Example of mapping IDs to logic
        switch (id)
        {
            case "coins_small": coinsToGrant = 100; break;
            case "coins_medium": coinsToGrant = 500; break;
            case "coins_large": coinsToGrant = 1200; break;
            case "bundle_starter":
                coinsToGrant = 1000;
                // UnlockSkin("warrior_skin"); // Example of bundle logic
                break;
            default:
                Debug.LogWarning($"Unknown product ID: {id}");
                break;
        }

        // 2. Grant the coins
        if (coinsToGrant > 0)
        {
            // CoinsManager.Instance.AddCoins(coinsToGrant);
        }

        return PurchaseProcessingResult.Complete;
    }

    // public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    // {
    //     Debug.Log($"[IAP] ProcessPurchase: {args.purchasedProduct.definition.id}");

    //     if (String.Equals(args.purchasedProduct.definition.id, PRODUCT_COINS_100, StringComparison.Ordinal))
    //     {
    //         // Grant 100 coins
    //         CoinsManager.Instance.AddCoins(100);
    //         Debug.Log("[IAP] Granted 100 coins");
    //         // If you persist purchases server-side, consider returning Pending until saved; otherwise Complete.
    //         return PurchaseProcessingResult.Complete;
    //     }

    //     // Unknown product, still complete to avoid stuck transactions
    //     return PurchaseProcessingResult.Complete;
    // }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.LogWarning($"[IAP] OnPurchaseFailed: {product.definition.storeSpecificId}, Reason: {failureReason}");
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.LogError($"[IAP] Initialization Failed: {error} | Message: {message}");
    }
}
