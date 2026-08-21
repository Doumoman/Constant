#if LEGACY_DISABLED
using System;
using StarNight.Debugging;
using StarNight.Grid;
using StarNight.Objects;
using StarNight.Player;
using StarNight.Tools.Grapple;
using StarNight.Tools.Mining;
using StarNight.Tools.Pestle;
using StarNight.Tools.Umbrella;
using StarNight.Tools.Water;
using StarNight.Tiles;
using UnityEngine;

namespace StarNight.Tools
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputAdapter))]
    public sealed class PlayerToolInventory2D : MonoBehaviour
    {
        [SerializeField] private PlayerInputAdapter input;
        [SerializeField] private CarrySystem carrySystem;
        [SerializeField] private PlayerMotor2D playerMotor;
        [SerializeField] private Rigidbody2D playerBody;
        [SerializeField] private Collider2D playerCollider;
        [SerializeField] private GridWorld gridWorld;
        [SerializeField] private Transform holdAnchor;
        [SerializeField] private Camera aimCamera;
        [SerializeField] private WaterInteractionRegistry2D waterRegistry;
        [SerializeField] private PestleInteractionRegistry2D pestleRegistry;
        [SerializeField] private WaterSource2D[] waterSources =
            Array.Empty<WaterSource2D>();
        [SerializeField] private P3ToolDiscoveryTelemetry telemetry;
        [SerializeField] private LayerMask grappleQueryMask = ~0;
        [SerializeField] private LayerMask grappleTerrainMask = ~0;
        [SerializeField, Min(0.25f)] private float pickupRadius = 1.35f;
        [SerializeField, Min(0f)] private float dropOffset = 0.85f;
        [SerializeField, Min(0.25f)] private float discoveryRadius = 4f;

        private float facingDirection = 1f;

        public event Action<HandToolPickup2D> HeldToolChanged;
        public event Action<P3ToolKind, bool> ToolUsed;

        public HandToolPickup2D HeldTool { get; private set; }
        public bool HasHeldTool => HeldTool != null && HeldTool.IsHeld;
        public float FacingDirection => facingDirection;

        public void Configure(
            PlayerInputAdapter inputAdapter,
            CarrySystem targetCarrySystem,
            PlayerMotor2D motor,
            Rigidbody2D targetBody,
            Collider2D targetCollider,
            GridWorld world,
            Transform targetHoldAnchor,
            Camera targetAimCamera,
            WaterInteractionRegistry2D targetWaterRegistry,
            PestleInteractionRegistry2D targetPestleRegistry,
            WaterSource2D[] targetWaterSources,
            P3ToolDiscoveryTelemetry targetTelemetry)
        {
            input = inputAdapter;
            carrySystem = targetCarrySystem;
            playerMotor = motor;
            playerBody = targetBody;
            playerCollider = targetCollider;
            gridWorld = world;
            holdAnchor = targetHoldAnchor;
            aimCamera = targetAimCamera;
            waterRegistry = targetWaterRegistry;
            pestleRegistry = targetPestleRegistry;
            waterSources = targetWaterSources ?? Array.Empty<WaterSource2D>();
            telemetry = targetTelemetry;
            carrySystem?.AttachToolInventory(this);
        }

        private void Awake()
        {
            if (input == null)
            {
                input = GetComponent<PlayerInputAdapter>();
            }

            if (carrySystem == null)
            {
                carrySystem = GetComponent<CarrySystem>();
            }

            if (playerMotor == null)
            {
                playerMotor = GetComponent<PlayerMotor2D>();
            }

            if (playerBody == null)
            {
                playerBody = GetComponent<Rigidbody2D>();
            }

            if (playerCollider == null)
            {
                playerCollider = GetComponent<Collider2D>();
            }

            if (gridWorld == null)
            {
                gridWorld = FindFirstObjectByType<GridWorld>();
            }

            if (holdAnchor == null && carrySystem != null)
            {
                holdAnchor = carrySystem.HoldAnchor;
            }

            if (aimCamera == null)
            {
                aimCamera = Camera.main;
            }

            if (waterRegistry == null)
            {
                waterRegistry = FindFirstObjectByType<WaterInteractionRegistry2D>();
            }

            if (pestleRegistry == null)
            {
                pestleRegistry = FindFirstObjectByType<PestleInteractionRegistry2D>();
            }

            if (telemetry == null)
            {
                telemetry = FindFirstObjectByType<P3ToolDiscoveryTelemetry>();
            }

            carrySystem?.AttachToolInventory(this);
        }

        private void Update()
        {
            if (input != null && Mathf.Abs(input.Move.x) > 0.01f)
            {
                facingDirection = Mathf.Sign(input.Move.x);
            }

            Rope.RopeClimber2D climber = GetComponent<Rope.RopeClimber2D>();
            if (climber != null)
            {
                climber.SetClimbInput(input != null ? input.Move.y : 0f);
            }

            UpdateUmbrella();
            MarkNearbyToolsSeen();

            if (HeldTool != null && !HeldTool.IsHeld)
            {
                UnsubscribeMining(HeldTool);
                HeldTool = null;
                HeldToolChanged?.Invoke(null);
            }
        }

        private void OnDisable()
        {
            DropHeldTool();
        }

        public bool TryPickupNearestTool()
        {
            HandToolPickup2D nearest = FindNearestPickup();
            return nearest != null && TryEquip(nearest);
        }

        public bool TryEquip(HandToolPickup2D pickup)
        {
            if (pickup == null || pickup == HeldTool || pickup.IsHeld)
            {
                return pickup != null && pickup == HeldTool;
            }

            carrySystem?.DropHeld();
            DropHeldTool();
            if (holdAnchor == null && carrySystem != null)
            {
                holdAnchor = carrySystem.HoldAnchor;
            }

            if (holdAnchor == null || !pickup.TryPickUp(holdAnchor))
            {
                return false;
            }

            HeldTool = pickup;
            ConfigureHeldTool(pickup);
            SubscribeMining(pickup);
            telemetry?.MarkSeen(pickup.Kind.ToP3ToolKind());
            HeldToolChanged?.Invoke(pickup);
            return true;
        }

        public bool DropHeldTool()
        {
            if (HeldTool == null)
            {
                return false;
            }

            HandToolPickup2D dropped = HeldTool;
            UnsubscribeMining(dropped);
            SetUmbrellaState(dropped, false, false);
            bool didDrop = dropped.Drop(CalculateDropPosition());
            if (didDrop || !dropped.IsHeld)
            {
                HeldTool = null;
                HeldToolChanged?.Invoke(null);
            }

            return didDrop;
        }

        public bool TryPlaceHeldTool(
            HandToolPickup2D expectedTool,
            Transform destination,
            bool remainAvailableForPickup = false)
        {
            if (HeldTool == null
                || HeldTool != expectedTool
                || destination == null)
            {
                return false;
            }

            HandToolPickup2D placed = HeldTool;
            UnsubscribeMining(placed);
            SetUmbrellaState(placed, false, false);
            if (!placed.TryPlaceAt(
                    destination,
                    remainAvailableForPickup))
            {
                SubscribeMining(placed);
                return false;
            }

            HeldTool = null;
            HeldToolChanged?.Invoke(null);
            return true;
        }

        public bool TryContextInteract()
        {
            if (!HasHeldTool || HeldTool.Kind != HandToolKind.WateringCan)
            {
                return false;
            }

            WateringCanTool2D wateringCan =
                HeldTool.GetComponent<WateringCanTool2D>();
            if (wateringCan == null || gridWorld == null)
            {
                return false;
            }

            GridPos actorCell = gridWorld.WorldToCell(PlayerPosition);
            for (int index = 0; index < waterSources.Length; index++)
            {
                WaterSource2D source = waterSources[index];
                if (source != null && source.TryRefill(wateringCan, actorCell))
                {
                    HeldTool.SetRemainingUses(wateringCan.Charges);
                    telemetry?.MarkSuccess(P3ToolKind.WateringCan);
                    return true;
                }
            }

            return false;
        }

        public bool TryUseHeldTool()
        {
            if (!HasHeldTool || gridWorld == null)
            {
                return false;
            }

            P3ToolKind toolKind = HeldTool.Kind.ToP3ToolKind();
            telemetry?.MarkUse(toolKind);
            Vector2 aim = ResolveAimDirection();
            GridPos actorCell = gridWorld.WorldToCell(PlayerPosition);
            bool accepted;
            bool succeeded;

            switch (HeldTool.Kind)
            {
                case HandToolKind.Pickaxe:
                case HandToolKind.Shovel:
                    AdjacentMiningTool2D mining =
                        HeldTool.GetComponent<AdjacentMiningTool2D>();
                    MiningUseResult miningResult = default;
                    accepted = mining != null
                        && mining.TryBeginUse(
                            PlayerPosition,
                            aim,
                            facingDirection < 0f ? -1 : 1,
                            out miningResult);
                    succeeded = accepted && miningResult.Succeeded;
                    if (mining != null)
                    {
                        HeldTool.SetRemainingUses(mining.RemainingDurability);
                    }
                    break;

                case HandToolKind.WateringCan:
                    WateringCanTool2D wateringCan =
                        HeldTool.GetComponent<WateringCanTool2D>();
                    WaterUseReport waterReport = WaterUseReport.Empty;
                    accepted = wateringCan != null
                        && wateringCan.TryUse(
                            actorCell,
                            ToCardinalGridDirection(aim),
                            out waterReport);
                    succeeded = accepted && waterReport.ReactionCount > 0;
                    if (wateringCan != null)
                    {
                        HeldTool.SetRemainingUses(wateringCan.Charges);
                    }
                    break;

                case HandToolKind.Pestle:
                    PestleTool2D pestle = HeldTool.GetComponent<PestleTool2D>();
                    PestleStrikeReport pestleReport = PestleStrikeReport.Empty;
                    accepted = pestle != null
                        && pestle.TryStrike(actorCell, out pestleReport);
                    succeeded = accepted && pestleReport.ReactionCount > 0;
                    break;

                case HandToolKind.Grapple:
                    GrappleLauncher2D grapple =
                        HeldTool.GetComponent<GrappleLauncher2D>();
                    GrappleFireResult grappleResult = grapple != null
                        ? grapple.TryUse(aim)
                        : GrappleFireResult.Miss(aim);
                    accepted = grappleResult.Fired;
                    succeeded = accepted;
                    break;

                case HandToolKind.WindUmbrella:
                    WindUmbrellaMotor2D umbrella =
                        HeldTool.GetComponent<WindUmbrellaMotor2D>();
                    accepted = umbrella != null;
                    if (umbrella != null)
                    {
                        umbrella.SetHeldAndOpen(true, true);
                    }
                    succeeded = umbrella != null && umbrella.IsOpen;
                    break;

                default:
                    accepted = false;
                    succeeded = false;
                    break;
            }

            if (succeeded)
            {
                telemetry?.MarkSuccess(toolKind);
            }

            ToolUsed?.Invoke(toolKind, succeeded);
            return accepted;
        }

        public HandToolPickup2D FindNearestPickup()
        {
            float maximumDistanceSquared = pickupRadius * pickupRadius;
            float bestDistanceSquared = maximumDistanceSquared;
            HandToolPickup2D best = null;
            Vector2 origin = PlayerPosition;

            foreach (HandToolPickup2D candidate in HandToolPickup2D.ActivePickups)
            {
                if (candidate == null
                    || candidate == HeldTool
                    || candidate.IsHeld
                    || !candidate.IsAvailableForPickup
                    || !candidate.isActiveAndEnabled)
                {
                    continue;
                }

                float distanceSquared =
                    ((Vector2)candidate.transform.position - origin).sqrMagnitude;
                if (distanceSquared > bestDistanceSquared)
                {
                    continue;
                }

                if (best == null
                    || distanceSquared < bestDistanceSquared
                    || candidate.GetInstanceID() < best.GetInstanceID())
                {
                    bestDistanceSquared = distanceSquared;
                    best = candidate;
                }
            }

            return best;
        }

        public Vector2 ResolveAimDirection()
        {
            Vector2 rawAim = input != null ? input.Aim : Vector2.zero;
            if (rawAim.sqrMagnitude > 4f && aimCamera != null)
            {
                Vector3 world = aimCamera.ScreenToWorldPoint(
                    new Vector3(rawAim.x, rawAim.y, -aimCamera.transform.position.z));
                rawAim = (Vector2)world - PlayerPosition;
            }

            if (rawAim.sqrMagnitude <= 0.04f && input != null)
            {
                rawAim = input.Move;
            }

            if (rawAim.sqrMagnitude <= 0.04f)
            {
                rawAim = Vector2.right * facingDirection;
            }

            return rawAim.normalized;
        }

        private Vector2 PlayerPosition =>
            playerBody != null ? playerBody.position : (Vector2)transform.position;

        private void ConfigureHeldTool(HandToolPickup2D pickup)
        {
            AdjacentMiningTool2D mining =
                pickup.GetComponent<AdjacentMiningTool2D>();
            if (mining != null && mining.GridWorld == null)
            {
                mining.Configure(
                    gridWorld,
                    FindFirstObjectByType<TileMutationService>(),
                    pickup.MaximumUses > 0
                        ? pickup.MaximumUses
                        : mining.MaximumDurability);
            }

            WateringCanTool2D wateringCan =
                pickup.GetComponent<WateringCanTool2D>();
            if (wateringCan != null)
            {
                wateringCan.Configure(
                    gridWorld,
                    waterRegistry,
                    pickup.HasFiniteUses
                        ? pickup.RemainingUses
                        : WateringCanTool2D.Capacity);
            }

            PestleTool2D pestle = pickup.GetComponent<PestleTool2D>();
            if (pestle != null)
            {
                pestle.Configure(gridWorld, pestleRegistry);
            }

            GrappleLauncher2D grapple =
                pickup.GetComponent<GrappleLauncher2D>();
            if (grapple != null)
            {
                grapple.Configure(
                    playerBody,
                    playerCollider,
                    holdAnchor,
                    grappleQueryMask,
                    grappleTerrainMask);
            }

            WindUmbrellaMotor2D umbrella =
                pickup.GetComponent<WindUmbrellaMotor2D>();
            if (umbrella != null)
            {
                umbrella.Configure(playerMotor, playerBody);
                umbrella.SetHeld(true);
            }
        }

        private void UpdateUmbrella()
        {
            if (!HasHeldTool || HeldTool.Kind != HandToolKind.WindUmbrella)
            {
                return;
            }

            WindUmbrellaMotor2D umbrella =
                HeldTool.GetComponent<WindUmbrellaMotor2D>();
            if (umbrella == null)
            {
                return;
            }

            umbrella.SetHeldAndOpen(
                true,
                input != null && input.UseHeldToolHeld);
            if (umbrella.IsOpen)
            {
                telemetry?.MarkSuccess(P3ToolKind.WindUmbrella);
            }
        }

        private void SetUmbrellaState(
            HandToolPickup2D pickup,
            bool held,
            bool open)
        {
            WindUmbrellaMotor2D umbrella =
                pickup != null
                    ? pickup.GetComponent<WindUmbrellaMotor2D>()
                    : null;
            umbrella?.SetHeldAndOpen(held, open);
        }

        private void MarkNearbyToolsSeen()
        {
            if (telemetry == null)
            {
                return;
            }

            float radiusSquared = discoveryRadius * discoveryRadius;
            Vector2 origin = PlayerPosition;
            foreach (HandToolPickup2D pickup in HandToolPickup2D.ActivePickups)
            {
                if (pickup != null
                    && ((Vector2)pickup.transform.position - origin).sqrMagnitude
                        <= radiusSquared)
                {
                    telemetry.MarkSeen(pickup.Kind.ToP3ToolKind());
                }
            }
        }

        private void SubscribeMining(HandToolPickup2D pickup)
        {
            AdjacentMiningTool2D mining =
                pickup != null
                    ? pickup.GetComponent<AdjacentMiningTool2D>()
                    : null;
            if (mining != null)
            {
                mining.UseResolved -= HandleMiningResolved;
                mining.UseResolved += HandleMiningResolved;
            }
        }

        private void UnsubscribeMining(HandToolPickup2D pickup)
        {
            AdjacentMiningTool2D mining =
                pickup != null
                    ? pickup.GetComponent<AdjacentMiningTool2D>()
                    : null;
            if (mining != null)
            {
                mining.UseResolved -= HandleMiningResolved;
            }
        }

        private void HandleMiningResolved(MiningUseResult result)
        {
            if (HeldTool == null)
            {
                return;
            }

            HeldTool.SetRemainingUses(result.RemainingDurability);
            if (result.Succeeded)
            {
                P3ToolKind kind = HeldTool.Kind.ToP3ToolKind();
                telemetry?.MarkSuccess(kind);
                ToolUsed?.Invoke(kind, true);
            }
        }

        private Vector2 CalculateDropPosition()
        {
            Vector2 desired =
                PlayerPosition + Vector2.right * facingDirection * dropOffset;
            if (gridWorld == null)
            {
                return desired;
            }

            GridPos desiredCell = gridWorld.WorldToCell(desired);
            if (gridWorld.IsWithinBounds(desiredCell)
                && !gridWorld.IsSolid(desiredCell)
                && !gridWorld.IsHazard(desiredCell))
            {
                return gridWorld.CellToWorldCenter(desiredCell);
            }

            return PlayerPosition;
        }

        private static GridPos ToCardinalGridDirection(Vector2 direction)
        {
            if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
            {
                return new GridPos(0, direction.y < 0f ? -1 : 1);
            }

            return new GridPos(direction.x < 0f ? -1 : 1, 0);
        }
    }
}

#endif
