#if LEGACY_DISABLED
using System;
using StarNight.Folklore.P9;
using UnityEngine;

namespace StarNight.Campaign.P11
{
    public enum P11StoryFact
    {
        LostParcelSorted = 0,
        PopoDefeated = 1,
        NaraeLetterRecovered = 2,
        DawnStarCoordinatesRecovered = 3,
        YoungCrowTrustedRani = 4,
        NestRootCooled = 5,
        NestSealReleased = 6,
        CrowNestRestored = 7,
        SunFlowerDefeated = 8,
        SunEmberRecovered = 9,
        FirstMemoryBellLit = 10,
        NaraeBellPatternCompleted = 11,
        MemoryRouteCompleted = 12
    }

    [DefaultExecutionOrder(-920)]
    [DisallowMultipleComponent]
    public sealed class P11StoryState2D : MonoBehaviour
    {
        [SerializeField] private P9FolkloreChainState2D folkloreState;
        [SerializeField] private bool lostParcelSorted;
        [SerializeField] private bool popoDefeated;
        [SerializeField] private bool hasNaraeLetter;
        [SerializeField] private bool hasDawnStarCoordinates;
        [SerializeField] private bool youngCrowTrustedRani;
        [SerializeField] private bool nestRootCooled;
        [SerializeField] private bool nestSealReleased;
        [SerializeField] private bool crowNestRestored;
        [SerializeField] private bool sunFlowerDefeated;
        [SerializeField] private bool hasSunEmber;
        [SerializeField] private bool firstMemoryBellLit;
        [SerializeField] private bool naraeBellPatternCompleted;
        [SerializeField] private bool memoryRouteCompleted;

        public event Action<P11StoryFact> StoryFactChanged;

        public P9FolkloreChainState2D FolkloreState => folkloreState;
        public bool LostParcelSorted => lostParcelSorted;
        public bool PopoDefeated => popoDefeated;
        public bool HasNaraeLetter => hasNaraeLetter;
        public bool HasDawnStarCoordinates =>
            hasDawnStarCoordinates;
        public bool YoungCrowTrustedRani => youngCrowTrustedRani;
        public bool NestRootCooled => nestRootCooled;
        public bool NestSealReleased => nestSealReleased;
        public bool CrowNestRestored => crowNestRestored;
        public bool SunFlowerDefeated => sunFlowerDefeated;
        public bool HasSunEmber => hasSunEmber;
        public bool FirstMemoryBellLit => firstMemoryBellLit;
        public bool NaraeBellPatternCompleted =>
            naraeBellPatternCompleted;
        public bool MemoryRouteCompleted => memoryRouteCompleted;
        public bool HasAnyBranchRelic =>
            folkloreState != null && folkloreState.HasAnyBranchRelic;
        public bool HasBothBranchRelics =>
            folkloreState != null && folkloreState.HasBothBranchRelics;
        public bool CanOpenLetterDrawer =>
            popoDefeated && HasAnyBranchRelic && !hasNaraeLetter;
        public bool CanRevealDawnCoordinates =>
            popoDefeated
            && HasBothBranchRelics
            && !hasDawnStarCoordinates;
        public bool CanUseLetterAtCrow =>
            hasNaraeLetter && !youngCrowTrustedRani;
        public bool CanRestoreCrowNest =>
            youngCrowTrustedRani
            && nestRootCooled
            && nestSealReleased
            && !crowNestRestored;
        public bool CanClaimSunEmber =>
            sunFlowerDefeated
            && crowNestRestored
            && !hasSunEmber;
        public bool CanLightFirstMemoryBell =>
            hasSunEmber && !firstMemoryBellLit;
        public bool CanAttemptMemoryPattern =>
            hasNaraeLetter && firstMemoryBellLit;
        public bool CanCompleteMemoryEnding =>
            hasNaraeLetter
            && hasSunEmber
            && naraeBellPatternCompleted;
        public bool MainProgressAlwaysAvailable => true;
        public bool OptionalStoryNeverGatesExit => true;

        public void Configure(P9FolkloreChainState2D chainState)
        {
            folkloreState = chainState;
            ResetStoryForTests();
        }

        public bool MarkLostParcelSorted()
        {
            return Set(
                ref lostParcelSorted,
                P11StoryFact.LostParcelSorted);
        }

        public bool MarkPopoDefeated()
        {
            return Set(ref popoDefeated, P11StoryFact.PopoDefeated);
        }

        public bool TryOpenNaraeLetterDrawer()
        {
            return CanOpenLetterDrawer
                && Set(
                    ref hasNaraeLetter,
                    P11StoryFact.NaraeLetterRecovered);
        }

        public bool TryRevealDawnStarCoordinates()
        {
            return CanRevealDawnCoordinates
                && Set(
                    ref hasDawnStarCoordinates,
                    P11StoryFact.DawnStarCoordinatesRecovered);
        }

        public bool TryPresentLetterToYoungCrow()
        {
            return CanUseLetterAtCrow
                && Set(
                    ref youngCrowTrustedRani,
                    P11StoryFact.YoungCrowTrustedRani);
        }

        public bool CoolNestRoot()
        {
            return Set(
                ref nestRootCooled,
                P11StoryFact.NestRootCooled);
        }

        public bool ReleaseNestSeal()
        {
            return Set(
                ref nestSealReleased,
                P11StoryFact.NestSealReleased);
        }

        public bool TryRestoreCrowNest()
        {
            return CanRestoreCrowNest
                && Set(
                    ref crowNestRestored,
                    P11StoryFact.CrowNestRestored);
        }

        public bool MarkSunFlowerDefeated()
        {
            return Set(
                ref sunFlowerDefeated,
                P11StoryFact.SunFlowerDefeated);
        }

        public bool TryClaimSunEmber()
        {
            return CanClaimSunEmber
                && Set(
                    ref hasSunEmber,
                    P11StoryFact.SunEmberRecovered);
        }

        public bool TryLightFirstMemoryBell()
        {
            return CanLightFirstMemoryBell
                && Set(
                    ref firstMemoryBellLit,
                    P11StoryFact.FirstMemoryBellLit);
        }

        public bool MarkNaraeBellPatternCompleted()
        {
            return CanAttemptMemoryPattern
                && Set(
                    ref naraeBellPatternCompleted,
                    P11StoryFact.NaraeBellPatternCompleted);
        }

        public bool TryCompleteMemoryRoute()
        {
            return CanCompleteMemoryEnding
                && Set(
                    ref memoryRouteCompleted,
                    P11StoryFact.MemoryRouteCompleted);
        }

        public bool HasFact(P11StoryFact fact)
        {
            switch (fact)
            {
                case P11StoryFact.LostParcelSorted:
                    return lostParcelSorted;
                case P11StoryFact.PopoDefeated:
                    return popoDefeated;
                case P11StoryFact.NaraeLetterRecovered:
                    return hasNaraeLetter;
                case P11StoryFact.DawnStarCoordinatesRecovered:
                    return hasDawnStarCoordinates;
                case P11StoryFact.YoungCrowTrustedRani:
                    return youngCrowTrustedRani;
                case P11StoryFact.NestRootCooled:
                    return nestRootCooled;
                case P11StoryFact.NestSealReleased:
                    return nestSealReleased;
                case P11StoryFact.CrowNestRestored:
                    return crowNestRestored;
                case P11StoryFact.SunFlowerDefeated:
                    return sunFlowerDefeated;
                case P11StoryFact.SunEmberRecovered:
                    return hasSunEmber;
                case P11StoryFact.FirstMemoryBellLit:
                    return firstMemoryBellLit;
                case P11StoryFact.NaraeBellPatternCompleted:
                    return naraeBellPatternCompleted;
                case P11StoryFact.MemoryRouteCompleted:
                    return memoryRouteCompleted;
                default:
                    return false;
            }
        }

        public void ResetStoryForTests()
        {
            lostParcelSorted = false;
            popoDefeated = false;
            hasNaraeLetter = false;
            hasDawnStarCoordinates = false;
            youngCrowTrustedRani = false;
            nestRootCooled = false;
            nestSealReleased = false;
            crowNestRestored = false;
            sunFlowerDefeated = false;
            hasSunEmber = false;
            firstMemoryBellLit = false;
            naraeBellPatternCompleted = false;
            memoryRouteCompleted = false;
        }

        public void GrantMemoryRouteItemsForTests()
        {
            popoDefeated = true;
            hasNaraeLetter = true;
            youngCrowTrustedRani = true;
            nestRootCooled = true;
            nestSealReleased = true;
            crowNestRestored = true;
            sunFlowerDefeated = true;
            hasSunEmber = true;
        }

        private bool Set(ref bool field, P11StoryFact fact)
        {
            if (field)
            {
                return false;
            }

            field = true;
            StoryFactChanged?.Invoke(fact);
            return true;
        }
    }
}

#endif
