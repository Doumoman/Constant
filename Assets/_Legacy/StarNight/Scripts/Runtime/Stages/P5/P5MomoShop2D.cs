#if LEGACY_DISABLED
using System;
using StarNight.Tools;
using UnityEngine;

namespace StarNight.Stages.P5
{
    [DisallowMultipleComponent]
    public sealed class P5MomoShop2D : MonoBehaviour
    {
        public const int RequiredOfferCount = 3;

        [SerializeField] private P5RunState2D runState;
        [SerializeField] private PlayerConsumableTools2D consumableTools;
        [SerializeField] private P5MomoShopOffer2D[] offers =
            Array.Empty<P5MomoShopOffer2D>();

        public event Action<P5ShopProductKind, int> PurchaseCompleted;
        public event Action<P5ShopProductKind, int> PurchaseRejected;

        public P5RunState2D RunState => runState;
        public PlayerConsumableTools2D ConsumableTools => consumableTools;
        public int OfferCount => offers != null ? offers.Length : 0;
        public P5MomoShopOffer2D[] Offers => offers;

        public void Configure(
            P5RunState2D targetRunState,
            PlayerConsumableTools2D targetConsumableTools,
            P5MomoShopOffer2D[] stageOffers)
        {
            if (stageOffers == null
                || stageOffers.Length != RequiredOfferCount)
            {
                throw new ArgumentException(
                    "The P5 Momo corridor must expose exactly three world offers.",
                    nameof(stageOffers));
            }

            for (int index = 0; index < stageOffers.Length; index++)
            {
                if (stageOffers[index] == null)
                {
                    throw new ArgumentException(
                        "P5 Momo offers cannot contain null entries.",
                        nameof(stageOffers));
                }

                if (stageOffers[index].Shop != this)
                {
                    throw new ArgumentException(
                        "Each P5 Momo offer must be configured for this shop.",
                        nameof(stageOffers));
                }

                for (int other = index + 1;
                    other < stageOffers.Length;
                    other++)
                {
                    if (stageOffers[index] == stageOffers[other])
                    {
                        throw new ArgumentException(
                            "Each P5 Momo pedestal must use a distinct offer.",
                            nameof(stageOffers));
                    }
                }
            }

            bool hasRopes = false;
            bool hasBombs = false;
            bool hasMoonCake = false;
            for (int index = 0; index < stageOffers.Length; index++)
            {
                switch (stageOffers[index].Product)
                {
                    case P5ShopProductKind.RopeBundle3:
                        if (hasRopes)
                        {
                            throw DuplicateProduct(stageOffers[index].Product);
                        }

                        hasRopes = true;
                        break;
                    case P5ShopProductKind.BombBundle2:
                        if (hasBombs)
                        {
                            throw DuplicateProduct(stageOffers[index].Product);
                        }

                        hasBombs = true;
                        break;
                    case P5ShopProductKind.MoonCake:
                        if (hasMoonCake)
                        {
                            throw DuplicateProduct(stageOffers[index].Product);
                        }

                        hasMoonCake = true;
                        break;
                }
            }

            if (!hasRopes || !hasBombs || !hasMoonCake)
            {
                throw new ArgumentException(
                    "The P5 Momo corridor requires RopeBundle3, BombBundle2, "
                    + "and MoonCake exactly once each.",
                    nameof(stageOffers));
            }

            runState = targetRunState;
            consumableTools = targetConsumableTools;
            offers = stageOffers;
        }

        public bool ContainsOffer(P5MomoShopOffer2D offer)
        {
            if (offer == null || offers == null)
            {
                return false;
            }

            for (int index = 0; index < offers.Length; index++)
            {
                if (offers[index] == offer)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryPurchase(P5MomoShopOffer2D offer)
        {
            if (!ContainsOffer(offer)
                || offer.IsSold
                || runState == null
                || consumableTools == null
                || !CanGrant(offer.Product))
            {
                PurchaseRejected?.Invoke(
                    offer != null ? offer.Product : default,
                    offer != null ? offer.Price : 0);
                return false;
            }

            int price = offer.Price;
            if (!runState.TrySpendGold(price))
            {
                PurchaseRejected?.Invoke(offer.Product, price);
                return false;
            }

            if (!TryGrant(offer.Product))
            {
                runState.AddGold(price);
                PurchaseRejected?.Invoke(offer.Product, price);
                return false;
            }

            offer.MarkSold();
            PurchaseCompleted?.Invoke(offer.Product, price);
            return true;
        }

        private bool CanGrant(P5ShopProductKind product)
        {
            switch (product)
            {
                case P5ShopProductKind.RopeBundle3:
                    return consumableTools.RopeStock <= 96;
                case P5ShopProductKind.BombBundle2:
                    return consumableTools.BombStock <= 97;
                case P5ShopProductKind.MoonCake:
                    return runState.MoonCakes
                        < P5RunState2D.MaximumSmallNumber;
                default:
                    return false;
            }
        }

        private bool TryGrant(P5ShopProductKind product)
        {
            switch (product)
            {
                case P5ShopProductKind.RopeBundle3:
                    return consumableTools.AddRopes(3) == 3;
                case P5ShopProductKind.BombBundle2:
                    return consumableTools.AddBombs(2) == 2;
                case P5ShopProductKind.MoonCake:
                    return runState.AddMoonCakes(1) == 1;
                default:
                    return false;
            }
        }

        private static ArgumentException DuplicateProduct(
            P5ShopProductKind product)
        {
            return new ArgumentException(
                $"The P5 Momo corridor contains duplicate {product} offers.",
                "stageOffers");
        }
    }
}

#endif
