#if LEGACY_DISABLED
using System;
using System.Linq;
using UnityEngine;

namespace StarNight.Population.P7
{
    [DisallowMultipleComponent]
    public sealed class P7MomoShop2D : MonoBehaviour
    {
        [SerializeField] private P7EconomyWallet2D wallet;
        [SerializeField] private P7ShopInventory2D inventory;
        [SerializeField] private P7MomoOffer2D[] offers =
            Array.Empty<P7MomoOffer2D>();

        public event Action<P7ShopProductKind, int> PurchaseCompleted;
        public event Action<P7ShopProductKind, int> PurchaseRejected;

        public P7EconomyWallet2D Wallet => wallet;
        public P7ShopInventory2D Inventory => inventory;
        public P7MomoOffer2D[] Offers => offers;
        public int OfferCount => offers?.Length ?? 0;

        public void Configure(
            P7EconomyWallet2D targetWallet,
            P7ShopInventory2D targetInventory,
            P7MomoOffer2D[] worldOffers)
        {
            if (targetWallet == null)
            {
                throw new ArgumentNullException(nameof(targetWallet));
            }

            if (targetInventory == null)
            {
                throw new ArgumentNullException(nameof(targetInventory));
            }

            ValidateOffers(worldOffers);
            wallet = targetWallet;
            inventory = targetInventory;
            offers = worldOffers;
        }

        public bool TryPurchase(P7MomoOffer2D offer)
        {
            if (offer == null
                || offers == null
                || !offers.Contains(offer)
                || offer.Shop != this
                || offer.IsSold
                || wallet == null
                || inventory == null
                || !inventory.CanGrant(offer.Product))
            {
                PurchaseRejected?.Invoke(
                    offer != null ? offer.Product : default,
                    offer != null ? offer.Price : 0);
                return false;
            }

            if (!wallet.TrySpendGold(offer.Price))
            {
                PurchaseRejected?.Invoke(offer.Product, offer.Price);
                return false;
            }

            if (!inventory.TryGrant(offer.Product))
            {
                wallet.AddGold(offer.Price);
                PurchaseRejected?.Invoke(offer.Product, offer.Price);
                return false;
            }

            offer.MarkSold();
            PurchaseCompleted?.Invoke(offer.Product, offer.Price);
            return true;
        }

        private void ValidateOffers(P7MomoOffer2D[] worldOffers)
        {
            if (worldOffers == null
                || worldOffers.Length
                    != P7EconomyRules.RequiredWorldOfferCount)
            {
                throw new ArgumentException(
                    "Momo must expose exactly three physical world offers.",
                    nameof(worldOffers));
            }

            for (int index = 0; index < worldOffers.Length; index++)
            {
                P7MomoOffer2D offer = worldOffers[index];
                if (offer == null || offer.Shop != this)
                {
                    throw new ArgumentException(
                        "Every Momo pedestal must reference this shop.",
                        nameof(worldOffers));
                }

                for (int other = index + 1;
                     other < worldOffers.Length;
                     other++)
                {
                    if (offer == worldOffers[other]
                        || offer.Product == worldOffers[other].Product)
                    {
                        throw new ArgumentException(
                            "Momo world offers and products must be unique.",
                            nameof(worldOffers));
                    }
                }
            }

            if (!worldOffers.Any(offer =>
                    offer.Price <= P7EconomyRules.BigGoldValue))
            {
                throw new ArgumentException(
                    "Momo requires at least one entry-price product.",
                    nameof(worldOffers));
            }
        }
    }

}

#endif
