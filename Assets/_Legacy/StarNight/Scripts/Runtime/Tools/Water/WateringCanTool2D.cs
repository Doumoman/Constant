#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Grid;
using UnityEngine;

namespace StarNight.Tools.Water
{
    [DisallowMultipleComponent]
    public sealed class WateringCanTool2D : MonoBehaviour
    {
        public const int Capacity = 6;

        [SerializeField] private GridWorld gridWorld;
        [SerializeField] private WaterInteractionRegistry2D interactionRegistry;
        [SerializeField, Range(0, Capacity)] private int charges = Capacity;

        private readonly List<WaterReactionRecord> reactions =
            new List<WaterReactionRecord>(Capacity);

        public event Action<int, int> ChargesChanged;
        public event Action<WaterUseReport> Used;
        public event Action Recharged;

        public int Charges => charges;
        public int MaxCharges => Capacity;
        public bool HasWater => charges > 0;
        public WaterUseReport LastReport { get; private set; } =
            WaterUseReport.Empty;

        private void Awake()
        {
            if (gridWorld == null)
            {
                gridWorld = FindFirstObjectByType<GridWorld>();
            }

            if (interactionRegistry == null)
            {
                interactionRegistry =
                    FindFirstObjectByType<WaterInteractionRegistry2D>();
            }

            charges = Mathf.Clamp(charges, 0, Capacity);
        }

        public void Configure(
            GridWorld world,
            WaterInteractionRegistry2D registry,
            int initialCharges = Capacity)
        {
            gridWorld = world;
            interactionRegistry = registry;
            SetCharges(initialCharges);
        }

        public bool TryUse(
            GridPos origin,
            GridPos rawDirection)
        {
            return TryUse(origin, rawDirection, out _);
        }

        public bool TryUse(
            GridPos origin,
            GridPos rawDirection,
            out WaterUseReport report)
        {
            report = WaterUseReport.Empty;
            GridPos direction =
                WaterStreamResolver.NormalizeCardinal(rawDirection);
            if (gridWorld == null
                || charges <= 0
                || (direction.X == 0 && direction.Y == 0))
            {
                return false;
            }

            IReadOnlyList<GridPos> cells = WaterStreamResolver.Resolve(
                origin,
                direction,
                gridWorld.CellBounds,
                gridWorld.IsSolid);

            reactions.Clear();
            if (interactionRegistry != null)
            {
                for (int index = 0; index < cells.Count; index++)
                {
                    GridPos cell = cells[index];
                    WaterApplication application = new WaterApplication(
                        origin,
                        direction,
                        cell,
                        index,
                        this);
                    interactionRegistry.ApplyWater(
                        cell,
                        application,
                        reactions);
                }
            }

            SetCharges(charges - 1);
            GridPos[] stableCells = new GridPos[cells.Count];
            for (int index = 0; index < cells.Count; index++)
            {
                stableCells[index] = cells[index];
            }

            WaterReactionRecord[] stableReactions = reactions.ToArray();
            report = new WaterUseReport(stableCells, stableReactions);
            LastReport = report;
            Used?.Invoke(report);
            return true;
        }

        public bool TryRecharge(
            WaterSource2D source,
            GridPos actorCell)
        {
            if (source == null
                || charges >= Capacity
                || !source.CanRefillAt(actorCell))
            {
                return false;
            }

            SetCharges(Capacity);
            Recharged?.Invoke();
            return true;
        }

        public void SetChargesForTests(int value)
        {
            SetCharges(value);
        }

        private void SetCharges(int value)
        {
            int next = Mathf.Clamp(value, 0, Capacity);
            if (next == charges)
            {
                return;
            }

            charges = next;
            ChargesChanged?.Invoke(charges, Capacity);
        }
    }
}

#endif
