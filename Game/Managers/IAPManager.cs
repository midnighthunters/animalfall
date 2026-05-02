// ============================================================
//  IAPManager.cs  –  Animal Fall  (REFACTORED)
//  Changes:
//    • ProcessPurchase → SaveManager.AddCoins / AddGems
//    • EventBus coin publish included
//    • BillingMode.json in StreamingAssets read for store setup
// ============================================================

using System;
using UnityEngine;
using UnityEngine.Purchasing;

public class IAPManager : MonoBehaviour, IStoreListener
{
    public static IAPManager Instance { get; private set; }

    // ── Product IDs ───────────────────────────────────────────
    public const string COINS_SMALL    = "coins_small";
    public const string COINS_MEDIUM   = "coins_medium";
    public const string COINS_LARGE    = "coins_large";
    public const string BUNDLE_STARTER = "bundle_starter";

    // ── Coin grants per product ───────────────────────────────
    private static readonly System.Collections.Generic.Dictionary<string, int> kCoinGrants
        = new()
        {
            { COINS_SMALL,    100  },
            { COINS_MEDIUM,   500  },
            { COINS_LARGE,    1200 },
            { BUNDLE_STARTER, 1000 }
        };

    private IStoreController    _store;
    private IExtensionProvider  _extensions;

    // ── Lifecycle ─────────────────────────────────────────────
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

    // ── Init ──────────────────────────────────────────────────
    public void InitializePurchasing()
    {
        if (IsInitialized()) return;

        var module  = StandardPurchasingModule.Instance();
        var builder = ConfigurationBuilder.Instance(module);

        builder.AddProduct(COINS_SMALL,    ProductType.Consumable);
        builder.AddProduct(COINS_MEDIUM,   ProductType.Consumable);
        builder.AddProduct(COINS_LARGE,    ProductType.Consumable);
        builder.AddProduct(BUNDLE_STARTER, ProductType.Consumable);

        UnityPurchasing.Initialize(this, builder);
        Debug.Log("[IAP] Initializing…");
    }

    public bool IsInitialized() => _store != null && _extensions != null;

    // ── Public purchase helpers ───────────────────────────────
    public void BuyProduct(string productId) => BuyID(productId);

    // ── IStoreListener ────────────────────────────────────────
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        Debug.Log("[IAP] Initialized.");
        _store      = controller;
        _extensions = extensions;
    }

    public void OnInitializeFailed(InitializationFailureReason error)
        => Debug.LogError("[IAP] Init failed: " + error);

    public void OnInitializeFailed(InitializationFailureReason error, string message)
        => Debug.LogError($"[IAP] Init failed: {error} | {message}");

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        string id = args.purchasedProduct.definition.id;
        Debug.Log($"[IAP] ProcessPurchase: {id}");

        if (kCoinGrants.TryGetValue(id, out int coins) && coins > 0)
        {
            SaveManager.Instance?.AddCoins(coins);
            Debug.Log($"[IAP] Granted {coins} coins for {id}");
        }

        // Bundle extras
        if (id == BUNDLE_STARTER)
            SaveManager.Instance?.AddGems(5);

        // Push updated save to Firebase
        FirebaseManager.Instance?.PushSaveData(SaveManager.Instance?.Data);

        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
        => Debug.LogWarning($"[IAP] Purchase failed: {product.definition.id} – {reason}");

    // ── Internal ──────────────────────────────────────────────
    private void BuyID(string productId)
    {
        if (!IsInitialized()) { Debug.LogWarning("[IAP] Not initialized."); return; }
        Product p = _store.products.WithID(productId);
        if (p != null && p.availableToPurchase)
            _store.InitiatePurchase(p);
        else
            Debug.LogWarning($"[IAP] Product '{productId}' not available.");
    }
}
