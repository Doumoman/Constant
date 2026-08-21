#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Stages.P5
{
    [DisallowMultipleComponent]
    public sealed class P5MomoShopOffer2D : P5ContextInteractable2D
    {
        [SerializeField] private P5MomoShop2D shop;
        [SerializeField] private P5ShopProductKind product;
        [SerializeField, Min(0)] private int price;
        [SerializeField] private GameObject productVisual;

        public event Action<P5MomoShopOffer2D> Sold;
        public event Action<P5MomoShopOffer2D> PurchaseAttemptRejected;

        public P5MomoShop2D Shop => shop;
        public P5ShopProductKind Product => product;
        public int Price => price;
        public bool IsSold { get; private set; }

        public void Configure(
            P5MomoShop2D targetShop,
            P5ShopProductKind productKind,
            int goldPrice,
            Transform interactionPoint = null,
            GameObject targetProductVisual = null,
            float interactionRadius = 1.35f)
        {
            int requiredPrice = DefaultPrice(productKind);
            if (goldPrice != requiredPrice)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(goldPrice),
                    goldPrice,
                    $"The fixed P5 offer price for {productKind} is {requiredPrice}.");
            }

            shop = targetShop;
            product = productKind;
            price = requiredPrice;
            productVisual = targetProductVisual;
            IsSold = false;
            if (productVisual != null)
            {
                productVisual.SetActive(true);
            }

            ConfigureInteraction(
                interactionPoint,
                interactionRadius,
                50);
        }

        public static int DefaultPrice(P5ShopProductKind productKind)
        {
            switch (productKind)
            {
                case P5ShopProductKind.RopeBundle3:
                    return 3;
                case P5ShopProductKind.BombBundle2:
                    return 4;
                case P5ShopProductKind.MoonCake:
                    return 3;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(productKind),
                        productKind,
                        null);
            }
        }

        protected override bool CanInteract(
            P5PlayerInteractionContext context)
        {
            return !IsSold && shop != null;
        }

        protected override bool TryInteract(
            P5PlayerInteractionContext context)
        {
            if (shop != null && shop.TryPurchase(this))
            {
                return true;
            }

            PurchaseAttemptRejected?.Invoke(this);
            return true;
        }

        public bool TryPurchaseForTests()
        {
            return shop != null && shop.TryPurchase(this);
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
