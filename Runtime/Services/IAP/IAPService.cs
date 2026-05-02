using System;
using UnityEngine;
using AnimalFall.Services.Save;

#if UNITY_PURCHASING
using UnityEngine.Purchasing;
#endif

namespace AnimalFall.Services.IAP
{
#if UNITY_PURCHASING
    public class IAPService : MonoBehaviour, IStoreListener
#else
    public class IAPService : MonoBehaviour
#endif
    {
        public static IAPService Instance { get; private set; }

        public event Action<string, int> OnPurchaseComplete;
        public event Action<string> OnPurchaseFailed;

        [Serializable]
        public struct ProductConfig
        {
            public string productId;
            public int coinsGranted;
        }

        [SerializeField] private ProductConfig[] products = new[]
        {
            new ProductConfig { productId = "coins_small", coinsGranted = 100 },
            new ProductConfig { productId = "coins_medium", coinsGranted = 500 },
            new ProductConfig { productId = "coins_large", coinsGranted = 1200 },
            new ProductConfig { productId = "bundle_starter", coinsGranted = 1000 }
        };

#if UNITY_PURCHASING
        private IStoreController storeController;
        private IExtensionProvider storeExtensionProvider;
#endif

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitializePurchasing();
        }

        private void InitializePurchasing()
        {
#if UNITY_PURCHASING
            if (storeController != null) return;

            var module = StandardPurchasingModule.Instance();
            var builder = ConfigurationBuilder.Instance(module);

            foreach (var product in products)
                builder.AddProduct(product.productId, ProductType.Consumable);

            UnityPurchasing.Initialize(this, builder);
#endif
        }

        public bool IsInitialized
        {
            get
            {
#if UNITY_PURCHASING
                return storeController != null && storeExtensionProvider != null;
#else
                return false;
#endif
            }
        }

        public void BuyProduct(string productId)
        {
#if UNITY_PURCHASING
            if (!IsInitialized)
            {
                OnPurchaseFailed?.Invoke("Store not initialized");
                return;
            }

            Product product = storeController.products.WithID(productId);
            if (product != null && product.availableToPurchase)
                storeController.InitiatePurchase(product);
            else
                OnPurchaseFailed?.Invoke("Product not available");
#else
            Debug.Log("[IAP] Purchase simulated for: " + productId);
            int coins = GetCoinsForProduct(productId);
            if (SaveService.Instance != null)
                SaveService.Instance.AddCoins(coins);
            OnPurchaseComplete?.Invoke(productId, coins);
#endif
        }

#if UNITY_PURCHASING
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            storeController = controller;
            storeExtensionProvider = extensions;
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogError("[IAP] Initialization failed: " + error);
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            string id = args.purchasedProduct.definition.id;
            int coins = GetCoinsForProduct(id);

            if (coins > 0 && SaveService.Instance != null)
                SaveService.Instance.AddCoins(coins);

            OnPurchaseComplete?.Invoke(id, coins);
            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
        {
            Debug.LogError($"[IAP] Purchase failed: {product.definition.id} — {reason}");
            OnPurchaseFailed?.Invoke(reason.ToString());
        }
#endif

        private int GetCoinsForProduct(string productId)
        {
            foreach (var product in products)
            {
                if (product.productId == productId)
                    return product.coinsGranted;
            }
            return 0;
        }
    }
}
