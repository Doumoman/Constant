#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Explosions;
using StarNight.Grid;
using UnityEngine;

namespace StarNight.Tools.Rope
{
    [DisallowMultipleComponent]
    public sealed class RopeInstallation2D : MonoBehaviour
    {
        private static readonly List<RopeInstallation2D> activeInstallations =
            new List<RopeInstallation2D>();

        [SerializeField] private GridWorld gridWorld;
        [SerializeField] private Vector2Int useCell;
        [SerializeField] private Vector2Int anchorCell;
        [SerializeField] private RopeAnchorKind anchorKind;
        [SerializeField] private RopeSegment2D[] segments = Array.Empty<RopeSegment2D>();
        [SerializeField] private bool isBroken;
        [SerializeField] private RopeDamageKind lastDamageKind;

        private readonly List<GridPos> cells = new List<GridPos>();

        public event Action<RopeInstallation2D, RopeDamageKind> Broken;

        public static IReadOnlyList<RopeInstallation2D> ActiveInstallations =>
            activeInstallations;

        public GridWorld GridWorld => gridWorld;
        public GridPos UseCell => new GridPos(useCell.x, useCell.y);
        public GridPos AnchorCell => new GridPos(anchorCell.x, anchorCell.y);
        public RopeAnchorKind AnchorKind => anchorKind;
        public IReadOnlyList<GridPos> Cells => cells;
        public bool IsBroken => isBroken;
        public RopeDamageKind LastDamageKind => lastDamageKind;

        private void OnEnable()
        {
            if (!isBroken && !activeInstallations.Contains(this))
            {
                activeInstallations.Add(this);
            }
        }

        private void OnDisable()
        {
            activeInstallations.Remove(this);
        }

        private void OnDestroy()
        {
            activeInstallations.Remove(this);
        }

        public void Configure(
            GridWorld world,
            RopeInstallPlan plan,
            IReadOnlyList<RopeSegment2D> configuredSegments)
        {
            gridWorld = world;
            useCell = new Vector2Int(plan.UseCell.X, plan.UseCell.Y);
            anchorCell = new Vector2Int(plan.AnchorCell.X, plan.AnchorCell.Y);
            anchorKind = plan.AnchorKind;
            isBroken = false;

            cells.Clear();
            for (int index = 0; index < plan.ClimbableCells.Count; index++)
            {
                cells.Add(plan.ClimbableCells[index]);
            }

            if (configuredSegments == null)
            {
                segments = Array.Empty<RopeSegment2D>();
                return;
            }

            segments = new RopeSegment2D[configuredSegments.Count];
            for (int index = 0; index < configuredSegments.Count; index++)
            {
                segments[index] = configuredSegments[index];
            }
        }

        public bool ContainsCell(GridPos cell)
        {
            return cells.Contains(cell);
        }

        public bool IntersectsExplosion(GridPos center)
        {
            for (int index = 0; index < cells.Count; index++)
            {
                if (ExplosionMask3x3.Contains(center, cells[index]))
                {
                    return true;
                }
            }

            return false;
        }

        public bool Break(
            RopeDamageKind damageKind,
            UnityEngine.Object source = null)
        {
            if (isBroken)
            {
                return false;
            }

            isBroken = true;
            lastDamageKind = damageKind;
            activeInstallations.Remove(this);

            for (int index = 0; index < segments.Length; index++)
            {
                if (segments[index] != null)
                {
                    segments[index].DisableImmediately();
                }
            }

            Broken?.Invoke(this, damageKind);
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }

            return true;
        }

        public bool BreakForTests(
            RopeDamageKind damageKind,
            bool destroyImmediately = false)
        {
            bool didBreak = Break(damageKind);
            if (didBreak && destroyImmediately && gameObject != null)
            {
                DestroyImmediate(gameObject);
            }

            return didBreak;
        }
    }
}

#endif
