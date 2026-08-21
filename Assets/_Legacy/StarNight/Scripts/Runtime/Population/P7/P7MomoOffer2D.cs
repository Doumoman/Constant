#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Population.P7
{
    [DisallowMultipleComponent]
    public sealed class P7MomoOffer2D : MonoBehaviour
    {
        [SerializeField] private P7MomoShop2D shop;
        [SerializeField] private P7ShopProductKind product;
        [SerializeField, Min(0)] private int price;
        [SerializeField] private GameObject productVisual;
        [SerializeField, Min(0)] private int priceIconCount;

        public event Action<P7MomoOffer2D> Sold;
        public event Action<P7MomoOffer2D> PurchaseRejected;

        public P7MomoShop2D Shop => shop;
        public P7ShopProductKind Product => product;
        public int Price => price;
        public int PriceIconCount => priceIconCount;
        public bool IsSold { get; private set; }
        public bool CanRemoveBeforePurchase => false;

        public void Configure(
            P7MomoShop2D targetShop,
            P7ShopProductKind productKind,
            int goldPrice,
            GameObject worldProductVisual,
            int worldPriceIconCount)
        {
            int requiredPrice = P7EconomyRules.Price(productKind);
            if (goldPrice != requiredPrice
                || worldPriceIconCount != requiredPrice)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(goldPrice),
                    goldPrice,
                    "Momo uses the fixed GDD price and displays the same "
                    + "number of world gold icons.");
            }

            shop = targetShop;
            product = productKind;
            price = requiredPrice;
            priceIconCount = worldPriceIconCount;
            productVisual = worldProductVisual;
            IsSold = false;
            if (productVisual != null)
            {
                productVisual.SetActive(true);
            }
        }

        public bool TryPurchase()
        {
            if (shop != null && shop.TryPurchase(this))
            {
                return true;
            }

            PurchaseRejected?.Invoke(this);
            return false;
        }

        internal void MarkSold()
        {
            if (IsSold)
            {
                return;
            }

            IsSold = true;
            if (productVisual != null)
            {
                productVisual.SetActive(false);
            }

            Sold?.Invoke(this);
        }
    }
}

#endif
