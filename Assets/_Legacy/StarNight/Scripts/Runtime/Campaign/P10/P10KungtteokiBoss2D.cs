#if LEGACY_DISABLED
using StarNight.Explosions;
using StarNight.Stages.P5;
using StarNight.Tools;
using UnityEngine;

namespace StarNight.Campaign.P10
{
    [DisallowMultipleComponent]
    public sealed class P10KungtteokiBoss2D :
        P5ContextInteractable2D,
        IExplosionReceiver2D
    {
        public const int RequiredStarKnots = 3;
        public const float SafeDemonstrationDeadlineSeconds = 5f;

        [SerializeField] private P10StageNode2D stageNode;
        [SerializeField] private P5MoonRabbitPestleEvent2D rabbitEvent;
        [SerializeField] private SpriteRenderer[] starKnotVisuals =
            System.Array.Empty<SpriteRenderer>();
        [SerializeField] private GameObject[] crackedFloorVisuals =
            System.Array.Empty<GameObject>();
        [SerializeField] private GameObject firstFloorHelpMark;
        [SerializeField] private GameObject millWeightVisual;
        [SerializeField] private GameObject recoveryMoonCakeVisual;
        [SerializeField] private P10BossPhase phase;
        [SerializeField] private int remainingStarKnots =
            RequiredStarKnots;
        [SerializeField] private int brokenFloorCount;
        [SerializeField] private bool[] brokenFloors =
            new bool[RequiredStarKnots];
        [SerializeField] private bool millWeightUsed;
        [SerializeField] private bool safeDemonstrationPerformed;
        [SerializeField] private float encounterElapsed;
        [SerializeField] private bool rabbitHelpOverride;

        public P10BossPhase Phase => phase;
        public int RemainingStarKnots => remainingStarKnots;
        public int BrokenFloorCount => brokenFloorCount;
        public bool IsDefeated => phase == P10BossPhase.Defeated;
        public bool RabbitHelpApplied =>
            rabbitHelpOverride
            || (rabbitEvent != null && rabbitEvent.IsCompleted);
        public bool FirstCrackedFloorMarked =>
            RabbitHelpApplied && firstFloorHelpMark != null;
        public bool MillWeightAvailable =>
            RabbitHelpApplied && !millWeightUsed;
        public bool RecoveryMoonCakeReady =>
            IsDefeated && RabbitHelpApplied;
        public bool SafeDemonstrationPerformed =>
            safeDemonstrationPerformed;
        public bool FirstFiveSecondDemonstrationReady =>
            crackedFloorVisuals != null
            && crackedFloorVisuals.Length == RequiredStarKnots;
        public bool SupportsDirectSolution => true;
        public bool SupportsToolFreeEnvironmentalSolution => true;
        public bool BossStagePausesMaruClock => true;
        public bool FallsAreNonLethal => true;
        public int MaximumSimultaneousPatterns => 2;

        public void Configure(
            P10StageNode2D node,
            P5MoonRabbitPestleEvent2D moonRabbitEvent,
            SpriteRenderer[] knots,
            GameObject[] crackedFloors,
            GameObject firstFloorMark,
            GameObject millWeight,
            GameObject recoveryMoonCake)
        {
            stageNode = node;
            rabbitEvent = moonRabbitEvent;
            starKnotVisuals = knots
                ?? System.Array.Empty<SpriteRenderer>();
            crackedFloorVisuals = crackedFloors
                ?? System.Array.Empty<GameObject>();
            firstFloorHelpMark = firstFloorMark;
            millWeightVisual = millWeight;
            recoveryMoonCakeVisual = recoveryMoonCake;
            ConfigureInteraction(transform, 3.2f, 75);
            ResetEncounterForTests();
        }

        private void Update()
        {
            if (phase == P10BossPhase.SafeDemonstration)
            {
                TickEncounter(Time.deltaTime);
            }
        }

        public bool BeginEncounter()
        {
            if (phase != P10BossPhase.Idle)
            {
                return false;
            }

            phase = P10BossPhase.SafeDemonstration;
            encounterElapsed = 0f;
            RefreshVisuals();
            return true;
        }

        public void TickEncounter(float deltaSeconds)
        {
            if (phase != P10BossPhase.SafeDemonstration)
            {
                return;
            }

            encounterElapsed += Mathf.Max(0f, deltaSeconds);
            if (!safeDemonstrationPerformed
                && encounterElapsed <= SafeDemonstrationDeadlineSeconds)
            {
                safeDemonstrationPerformed = true;
                phase = P10BossPhase.Active;
                RefreshVisuals();
            }
        }

        public bool RegisterDownwardSlam()
        {
            if (phase != P10BossPhase.Active)
            {
                return false;
            }

            phase = P10BossPhase.Vulnerable;
            return true;
        }

        public bool TryDirectWeakPointHit(
            P10BossSolutionInput input)
        {
            if (phase != P10BossPhase.Vulnerable
                || !IsKungtteokiDirectInput(input))
            {
                return false;
            }

            DamageOneKnot();
            if (!IsDefeated)
            {
                phase = P10BossPhase.Active;
            }

            return true;
        }

        public bool TryBreakCrackedFloor(int floorIndex)
        {
            if ((phase != P10BossPhase.Active
                    && phase != P10BossPhase.Vulnerable)
                || floorIndex < 0
                || floorIndex >= RequiredStarKnots
                || brokenFloors[floorIndex])
            {
                return false;
            }

            brokenFloors[floorIndex] = true;
            brokenFloorCount++;
            if (floorIndex < crackedFloorVisuals.Length
                && crackedFloorVisuals[floorIndex] != null)
            {
                crackedFloorVisuals[floorIndex].SetActive(false);
            }

            if (brokenFloorCount >= RequiredStarKnots)
            {
                Defeat();
            }

            return true;
        }

        public bool TryUseMillWeight()
        {
            if (!MillWeightAvailable
                || (phase != P10BossPhase.Active
                    && phase != P10BossPhase.Vulnerable))
            {
                return false;
            }

            millWeightUsed = true;
            DamageOneKnot();
            RefreshVisuals();
            return true;
        }

        public void SetRabbitHelpForTests(bool helped)
        {
            rabbitHelpOverride = helped;
            RefreshVisuals();
        }

        public void ResetEncounterForTests()
        {
            phase = P10BossPhase.Idle;
            remainingStarKnots = RequiredStarKnots;
            brokenFloorCount = 0;
            brokenFloors = new bool[RequiredStarKnots];
            for (int index = 0;
                 index < crackedFloorVisuals.Length;
                 index++)
            {
                crackedFloorVisuals[index]?.SetActive(true);
            }

            millWeightUsed = false;
            safeDemonstrationPerformed = false;
            encounterElapsed = 0f;
            rabbitHelpOverride = false;
            stageNode?.ResetForTests();
            RefreshVisuals();
        }

        public void ReceiveExplosion(ExplosionHit2D hit)
        {
            if (IsDefeated)
            {
                return;
            }

            if (phase == P10BossPhase.Idle)
            {
                BeginEncounter();
            }

            if (phase == P10BossPhase.SafeDemonstration)
            {
                TickEncounter(0f);
            }

            if (phase == P10BossPhase.Active)
            {
                RegisterDownwardSlam();
            }

            if (phase == P10BossPhase.Vulnerable)
            {
                TryDirectWeakPointHit(P10BossSolutionInput.Bomb);
            }
        }

        protected override bool CanInteract(
            P5PlayerInteractionContext context)
        {
            return !IsDefeated;
        }

        protected override bool TryInteract(
            P5PlayerInteractionContext context)
        {
            if (phase == P10BossPhase.Idle)
            {
                bool began = BeginEncounter();
                TickEncounter(0f);
                return began;
            }

            if (phase == P10BossPhase.SafeDemonstration)
            {
                TickEncounter(0f);
                return true;
            }

            if (phase == P10BossPhase.Active)
            {
                return RegisterDownwardSlam();
            }

            return TryDirectWeakPointHit(
                ResolveDirectInput(context.ToolInventory));
        }

        private void DamageOneKnot()
        {
            if (IsDefeated || remainingStarKnots <= 0)
            {
                return;
            }

            remainingStarKnots--;
            if (remainingStarKnots <= 0)
            {
                Defeat();
            }

            RefreshVisuals();
        }

        private void Defeat()
        {
            remainingStarKnots = 0;
            phase = P10BossPhase.Defeated;
            stageNode?.MarkBossDefeated();
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            for (int index = 0;
                 index < starKnotVisuals.Length;
                 index++)
            {
                if (starKnotVisuals[index] != null)
                {
                    starKnotVisuals[index].enabled =
                        index < remainingStarKnots;
                }
            }

            if (firstFloorHelpMark != null)
            {
                firstFloorHelpMark.SetActive(RabbitHelpApplied);
            }

            if (millWeightVisual != null)
            {
                millWeightVisual.SetActive(MillWeightAvailable);
            }

            if (recoveryMoonCakeVisual != null)
            {
                recoveryMoonCakeVisual.SetActive(
                    RecoveryMoonCakeReady);
            }
        }

        private static bool IsKungtteokiDirectInput(
            P10BossSolutionInput input)
        {
            return input == P10BossSolutionInput.BasicWeakPoint
                || input == P10BossSolutionInput.Bomb
                || input == P10BossSolutionInput.Pickaxe
                || input == P10BossSolutionInput.Pestle
                || input == P10BossSolutionInput.MoonCake;
        }

        private static P10BossSolutionInput ResolveDirectInput(
            PlayerToolInventory2D inventory)
        {
            if (inventory == null || !inventory.HasHeldTool)
            {
                return P10BossSolutionInput.BasicWeakPoint;
            }

            switch (inventory.HeldTool.Kind)
            {
                case HandToolKind.Pickaxe:
                    return P10BossSolutionInput.Pickaxe;
                case HandToolKind.Pestle:
                    return P10BossSolutionInput.Pestle;
                default:
                    return P10BossSolutionInput.BasicWeakPoint;
            }
        }
    }
}

#endif
