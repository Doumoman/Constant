#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Folklore.P9
{
    [DefaultExecutionOrder(-950)]
    [DisallowMultipleComponent]
    public sealed class P9FolkloreChainState2D : MonoBehaviour
    {
        [Header("Moon Palace gifts")]
        [SerializeField] private bool hasMoonCake;
        [SerializeField] private bool hasJadeRabbitMedicine;

        [Header("Correspondence events")]
        [SerializeField] private P9CorrespondenceResolution magpieResolution;
        [SerializeField] private P9CorrespondenceResolution turtleResolution;

        [Header("Branch relics")]
        [SerializeField] private bool hasRedWeaverThread;
        [SerializeField] private bool hasDragonPalaceOrb;

        public event Action<P9FolkloreItemKind, bool> ItemStateChanged;
        public event Action<
            P9CorrespondenceEventKind,
            P9CorrespondenceResolution> EventResolved;

        public bool HasMoonCake => hasMoonCake;
        public bool HasJadeRabbitMedicine => hasJadeRabbitMedicine;
        public bool HasRedWeaverThread => hasRedWeaverThread;
        public bool HasDragonPalaceOrb => hasDragonPalaceOrb;
        public bool HasBothMoonPalaceGifts =>
            hasMoonCake && hasJadeRabbitMedicine;
        public bool HasAnyBranchRelic =>
            hasRedWeaverThread || hasDragonPalaceOrb;
        public bool HasBothBranchRelics =>
            hasRedWeaverThread && hasDragonPalaceOrb;
        public bool CanOpenPostOfficeLetterDrawer => HasAnyBranchRelic;
        public bool MainProgressAlwaysAvailable => true;
        public bool OptionalEventsIgnoredWithoutPenalty => true;

        public void Configure(
            bool grantMoonCake,
            bool grantJadeRabbitMedicine)
        {
            hasMoonCake = grantMoonCake;
            hasJadeRabbitMedicine = grantJadeRabbitMedicine;
            magpieResolution = P9CorrespondenceResolution.None;
            turtleResolution = P9CorrespondenceResolution.None;
            hasRedWeaverThread = false;
            hasDragonPalaceOrb = false;
        }

        public bool HasItem(P9FolkloreItemKind item)
        {
            switch (item)
            {
                case P9FolkloreItemKind.MoonCake:
                    return hasMoonCake;
                case P9FolkloreItemKind.JadeRabbitMedicine:
                    return hasJadeRabbitMedicine;
                case P9FolkloreItemKind.RedWeaverThread:
                    return hasRedWeaverThread;
                case P9FolkloreItemKind.DragonPalaceOrb:
                    return hasDragonPalaceOrb;
                default:
                    return false;
            }
        }

        public bool GrantItem(P9FolkloreItemKind item)
        {
            if (item == P9FolkloreItemKind.None || HasItem(item))
            {
                return false;
            }

            SetItem(item, true);
            return true;
        }

        public P9FolkloreItemKind RequiredGift(
            P9CorrespondenceEventKind eventKind)
        {
            return eventKind == P9CorrespondenceEventKind.HungryMagpie
                ? P9FolkloreItemKind.MoonCake
                : P9FolkloreItemKind.JadeRabbitMedicine;
        }

        public P9CorrespondenceResolution ResolutionFor(
            P9CorrespondenceEventKind eventKind)
        {
            return eventKind == P9CorrespondenceEventKind.HungryMagpie
                ? magpieResolution
                : turtleResolution;
        }

        public bool IsEventResolved(P9CorrespondenceEventKind eventKind)
        {
            return ResolutionFor(eventKind)
                != P9CorrespondenceResolution.None;
        }

        public bool TryResolveWithGift(
            P9CorrespondenceEventKind eventKind,
            P9FolkloreItemKind offeredItem)
        {
            if (IsEventResolved(eventKind)
                || offeredItem != RequiredGift(eventKind)
                || !HasItem(offeredItem))
            {
                return false;
            }

            SetItem(offeredItem, false);
            SetResolution(
                eventKind,
                P9CorrespondenceResolution.MatchingGift);
            return true;
        }

        public bool TryResolveWithAlternative(
            P9CorrespondenceEventKind eventKind)
        {
            if (IsEventResolved(eventKind))
            {
                return false;
            }

            SetResolution(
                eventKind,
                P9CorrespondenceResolution.AlternativeRescue);
            return true;
        }

        public bool TryGrantBranchRelic(P9BranchKind branch)
        {
            switch (branch)
            {
                case P9BranchKind.MagpieBridge:
                    if (!IsEventResolved(
                            P9CorrespondenceEventKind.HungryMagpie))
                    {
                        return false;
                    }

                    return GrantItem(
                        P9FolkloreItemKind.RedWeaverThread);
                case P9BranchKind.DragonPalace:
                    if (!IsEventResolved(
                            P9CorrespondenceEventKind.InjuredTurtle))
                    {
                        return false;
                    }

                    return GrantItem(
                        P9FolkloreItemKind.DragonPalaceOrb);
                default:
                    return false;
            }
        }

        public bool GrantBossRelic(P9BranchKind branch)
        {
            switch (branch)
            {
                case P9BranchKind.MagpieBridge:
                    return GrantItem(
                        P9FolkloreItemKind.RedWeaverThread);
                case P9BranchKind.DragonPalace:
                    return GrantItem(
                        P9FolkloreItemKind.DragonPalaceOrb);
                default:
                    return false;
            }
        }

        public bool CanEnterOppositeBranchAfter(
            P9BranchKind completedBranch)
        {
            switch (completedBranch)
            {
                case P9BranchKind.MagpieBridge:
                    return hasRedWeaverThread
                        && hasJadeRabbitMedicine;
                case P9BranchKind.DragonPalace:
                    return hasDragonPalaceOrb && hasMoonCake;
                default:
                    return false;
            }
        }

        public void ResetForTests(
            bool grantMoonCake = true,
            bool grantMedicine = true)
        {
            Configure(grantMoonCake, grantMedicine);
        }

        private void SetResolution(
            P9CorrespondenceEventKind eventKind,
            P9CorrespondenceResolution resolution)
        {
            if (eventKind == P9CorrespondenceEventKind.HungryMagpie)
            {
                magpieResolution = resolution;
            }
            else
            {
                turtleResolution = resolution;
            }

            EventResolved?.Invoke(eventKind, resolution);
        }

        private void SetItem(P9FolkloreItemKind item, bool value)
        {
            switch (item)
            {
                case P9FolkloreItemKind.MoonCake:
                    hasMoonCake = value;
                    break;
                case P9FolkloreItemKind.JadeRabbitMedicine:
                    hasJadeRabbitMedicine = value;
                    break;
                case P9FolkloreItemKind.RedWeaverThread:
                    hasRedWeaverThread = value;
                    break;
                case P9FolkloreItemKind.DragonPalaceOrb:
                    hasDragonPalaceOrb = value;
                    break;
                default:
                    return;
            }

            ItemStateChanged?.Invoke(item, value);
        }
    }
}

#endif
