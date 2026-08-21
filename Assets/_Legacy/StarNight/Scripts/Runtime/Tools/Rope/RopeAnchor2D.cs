#if LEGACY_DISABLED
using System.Collections.Generic;
using StarNight.Grid;
using UnityEngine;

namespace StarNight.Tools.Rope
{
    [DisallowMultipleComponent]
    public sealed class RopeAnchor2D : MonoBehaviour
    {
        private static readonly List<RopeAnchor2D> activeAnchors =
            new List<RopeAnchor2D>();

        [SerializeField] private GridWorld gridWorld;
        [SerializeField] private RopeAnchorKind anchorKind = RopeAnchorKind.Ring;
        [SerializeField] private bool useExplicitCell;
        [SerializeField] private Vector2Int explicitCell;

        public RopeAnchorKind AnchorKind => anchorKind;
        public GridWorld GridWorld => gridWorld;

        public GridPos Cell
        {
            get
            {
                if (useExplicitCell)
                {
                    return new GridPos(explicitCell.x, explicitCell.y);
                }

                if (gridWorld != null)
                {
                    return gridWorld.WorldToCell(transform.position);
                }

                return new GridPos(
                    Mathf.FloorToInt(transform.position.x),
                    Mathf.FloorToInt(transform.position.y));
            }
        }

        private void Awake()
        {
            ResolveGridWorld();
        }

        private void OnEnable()
        {
            ResolveGridWorld();
            if (!activeAnchors.Contains(this))
            {
                activeAnchors.Add(this);
            }
        }

        private void OnDisable()
        {
            activeAnchors.Remove(this);
        }

        public void Configure(
            GridWorld world,
            GridPos cell,
            RopeAnchorKind kind = RopeAnchorKind.Ring)
        {
            gridWorld = world;
            explicitCell = new Vector2Int(cell.X, cell.Y);
            useExplicitCell = true;
            anchorKind = kind;
            EnsureRegistered();
        }

        public void ConfigureFromTransform(
            GridWorld world,
            RopeAnchorKind kind = RopeAnchorKind.Ring)
        {
            gridWorld = world;
            useExplicitCell = false;
            anchorKind = kind;
            EnsureRegistered();
        }

        public static bool TryFind(
            GridWorld world,
            GridPos cell,
            out RopeAnchor2D anchor)
        {
            anchor = null;
            int lowestInstanceId = int.MaxValue;

            for (int index = activeAnchors.Count - 1; index >= 0; index--)
            {
                RopeAnchor2D candidate = activeAnchors[index];
                if (candidate == null)
                {
                    activeAnchors.RemoveAt(index);
                    continue;
                }

                if (!candidate.isActiveAndEnabled
                    || candidate.gridWorld != world
                    || candidate.Cell != cell)
                {
                    continue;
                }

                int instanceId = candidate.GetInstanceID();
                if (anchor == null || instanceId < lowestInstanceId)
                {
                    anchor = candidate;
                    lowestInstanceId = instanceId;
                }
            }

            return anchor != null;
        }

        private void ResolveGridWorld()
        {
            if (gridWorld != null)
            {
                return;
            }

            gridWorld = GetComponentInParent<GridWorld>();
            if (gridWorld == null)
            {
                gridWorld = FindFirstObjectByType<GridWorld>();
            }
        }

        private void EnsureRegistered()
        {
            if (isActiveAndEnabled && !activeAnchors.Contains(this))
            {
                activeAnchors.Add(this);
            }
        }
    }
}

#endif
