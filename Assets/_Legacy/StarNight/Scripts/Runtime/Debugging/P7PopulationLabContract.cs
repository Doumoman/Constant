#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Population.P7;
using StarNight.Rooms;
using UnityEngine;

namespace StarNight.Debugging
{
    [DisallowMultipleComponent]
    public sealed class P7PopulationLabContract : MonoBehaviour
    {
        [SerializeField] private P6RoomGraphLabContract graphContract;
        [SerializeField] private P7RegionPlacementProfileAsset regionProfile;
        [SerializeField] private int populationSeed;
        [SerializeField] private string populationFingerprint;
        [SerializeField] private P7PopulationMarker2D[] markers =
            Array.Empty<P7PopulationMarker2D>();
        [SerializeField] private P7EconomyWallet2D wallet;
        [SerializeField] private P7ShopInventory2D shopInventory;
        [SerializeField] private P7MomoShop2D momoShop;
        [SerializeField] private Transform populationOverlay;
        [SerializeField] private int mainPathGold;
        [SerializeField] private int mainPathGoldAvailableForShop;
        [SerializeField] private int optionalRiskScore;
        [SerializeField] private int optionalRewardValue;
        [SerializeField] private bool roomBudgetsSatisfied;
        [SerializeField] private bool stageBudgetsSatisfied;
        [SerializeField] private bool slotClaimsUnique;
        [SerializeField] private bool mainPathPurchaseSatisfied;
        [SerializeField] private bool optionalRiskRewardSatisfied;
        [SerializeField] private bool shopContractSatisfied;
        [SerializeField] private bool exitApproachProtected;
        [SerializeField] private string lastValidation = string.Empty;
        [SerializeField] private string[] issues = Array.Empty<string>();

        public P6RoomGraphLabContract GraphContract => graphContract;
        public P7RegionPlacementProfileAsset RegionProfile => regionProfile;
        public int PopulationSeed => populationSeed;
        public string PopulationFingerprint => populationFingerprint;
        public IReadOnlyList<P7PopulationMarker2D> Markers => markers;
        public P7EconomyWallet2D Wallet => wallet;
        public P7ShopInventory2D ShopInventory => shopInventory;
        public P7MomoShop2D MomoShop => momoShop;
        public Transform PopulationOverlay => populationOverlay;
        public int MainPathGold => mainPathGold;
        public int MainPathGoldAvailableForShop =>
            mainPathGoldAvailableForShop;
        public int OptionalRiskScore => optionalRiskScore;
        public int OptionalRewardValue => optionalRewardValue;
        public bool RoomBudgetsSatisfied => roomBudgetsSatisfied;
        public bool StageBudgetsSatisfied => stageBudgetsSatisfied;
        public bool SlotClaimsUnique => slotClaimsUnique;
        public bool MainPathPurchaseSatisfied => mainPathPurchaseSatisfied;
        public bool OptionalRiskRewardSatisfied => optionalRiskRewardSatisfied;
        public bool ShopContractSatisfied => shopContractSatisfied;
        public bool ExitApproachProtected => exitApproachProtected;
        public string LastValidation => lastValidation;
        public IReadOnlyList<string> Issues => issues;
        public bool ValidationPassed => issues.Length == 0;

        public void Configure(
            P6RoomGraphLabContract sourceGraph,
            P7RegionPlacementProfileAsset profile,
            int seed,
            P7PopulationPlan plan,
            P7PopulationValidationReport validation,
            P7PopulationMarker2D[] populationMarkers,
            P7EconomyWallet2D economyWallet,
            P7ShopInventory2D inventory,
            P7MomoShop2D shop,
            Transform overlay)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            if (validation == null)
            {
                throw new ArgumentNullException(nameof(validation));
            }

            graphContract = sourceGraph;
            regionProfile = profile;
            populationSeed = seed;
            populationFingerprint = plan.Fingerprint;
            markers = populationMarkers ?? Array.Empty<P7PopulationMarker2D>();
            wallet = economyWallet;
            shopInventory = inventory;
            momoShop = shop;
            populationOverlay = overlay;
            mainPathGold = plan.MainPathGold;
            mainPathGoldAvailableForShop =
                plan.MainPathGoldAvailableForShop;
            optionalRiskScore = plan.OptionalRiskScore;
            optionalRewardValue = plan.OptionalRewardValue;
            roomBudgetsSatisfied = validation.RoomBudgetsSatisfied;
            stageBudgetsSatisfied = validation.StageBudgetsSatisfied;
            slotClaimsUnique = validation.SlotClaimsUnique;
            mainPathPurchaseSatisfied =
                validation.MainPathPurchaseSatisfied;
            optionalRiskRewardSatisfied =
                validation.OptionalRiskRewardSatisfied;
            shopContractSatisfied = validation.ShopContractSatisfied;
            exitApproachProtected = validation.ExitApproachProtected;
            RefreshValidation();
        }

        public bool RefreshValidation()
        {
            var found = new List<string>();
            if (graphContract == null
                || graphContract.transform.root != transform.root
                || !graphContract.RefreshValidation())
            {
                found.Add("P7 requires the valid P6 graph contract on this root.");
            }

            if (regionProfile == null
                || regionProfile.Region != RoomRegion.MoonPalace)
            {
                found.Add("P7 Moon Lab requires its Moon Palace region profile.");
            }

            if (string.IsNullOrWhiteSpace(populationFingerprint))
            {
                found.Add("P7 population fingerprint is missing.");
            }

            if (populationOverlay == null
                || populationOverlay.root != transform.root)
            {
                found.Add("P7 population overlay is missing.");
            }

            if (wallet == null || shopInventory == null)
            {
                found.Add("P7 Lab economy wallet or shop inventory is missing.");
            }

            if (!roomBudgetsSatisfied
                || !stageBudgetsSatisfied
                || !slotClaimsUnique
                || !mainPathPurchaseSatisfied
                || !optionalRiskRewardSatisfied
                || !shopContractSatisfied
                || !exitApproachProtected)
            {
                found.Add("P7 serialized population validation flags are incomplete.");
            }

            ValidateMarkers(found);
            ValidateShop(found);
            issues = found.ToArray();
            lastValidation =
                issues.Length == 0
                    ? "PASS"
                    : string.Join(Environment.NewLine, issues);
            return issues.Length == 0;
        }

        private void ValidateMarkers(List<string> found)
        {
            if (markers == null || markers.Length == 0)
            {
                found.Add("P7 Lab has no population markers.");
                return;
            }

            var slotClaims = new HashSet<string>(StringComparer.Ordinal);
            int mainGold = 0;
            int balancedRewardCount = 0;
            for (int index = 0; index < markers.Length; index++)
            {
                P7PopulationMarker2D marker = markers[index];
                if (marker == null
                    || marker.transform.root != transform.root
                    || string.IsNullOrWhiteSpace(marker.SlotId)
                    || !slotClaims.Add($"{marker.NodeId}:{marker.SlotId}"))
                {
                    found.Add($"P7 marker {index} is missing or duplicates a slot.");
                    continue;
                }

                if (marker.Route == P7PopulationRoute.Main
                    && (marker.Kind == P7PopulationKind.SmallGold
                        || marker.Kind == P7PopulationKind.BigGold))
                {
                    mainGold += marker.Value;
                }

                if (marker.Route == P7PopulationRoute.Optional
                    && marker.RiskScore > 0
                    && (marker.Kind == P7PopulationKind.BigGold
                        || marker.Kind == P7PopulationKind.TreasureChest))
                {
                    balancedRewardCount++;
                    if (marker.Value
                        != P7EconomyRules.RewardValueForRisk(
                            marker.RiskScore))
                    {
                        found.Add("P7 optional reward value does not match risk.");
                    }
                }
            }

            if (mainGold != mainPathGold)
            {
                found.Add(
                    $"P7 marker gold mismatch: {mainGold}/{mainPathGold}.");
            }

            if (balancedRewardCount != 1
                || optionalRewardValue
                    != P7EconomyRules.RewardValueForRisk(
                        optionalRiskScore))
            {
                found.Add("P7 Lab requires one risk-matched optional reward.");
            }
        }

        private void ValidateShop(List<string> found)
        {
            if (momoShop == null)
            {
                found.Add("P7 Lab Momo shop is missing.");
                return;
            }

            if (momoShop.OfferCount
                != P7EconomyRules.RequiredWorldOfferCount
                || momoShop.Wallet != wallet
                || momoShop.Inventory != shopInventory)
            {
                found.Add("P7 Momo shop is not wired to three world offers.");
                return;
            }

            if (momoShop.Offers.Any(offer =>
                    offer == null
                    || offer.Shop != momoShop
                    || offer.Price
                        != P7EconomyRules.Price(offer.Product)
                    || offer.PriceIconCount != offer.Price
                    || offer.CanRemoveBeforePurchase))
            {
                found.Add("P7 Momo offer price or anti-theft contract failed.");
            }

            int minimum = momoShop.Offers.Min(offer => offer.Price);
            if (mainPathGoldAvailableForShop < minimum)
            {
                found.Add(
                    "P7 main route cannot afford an entry-price Momo offer.");
            }
        }
    }
}

#endif
