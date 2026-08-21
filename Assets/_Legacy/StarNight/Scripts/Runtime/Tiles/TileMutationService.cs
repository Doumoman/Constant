#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Grid;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace StarNight.Tiles
{
    [DefaultExecutionOrder(30000)]
    [DisallowMultipleComponent]
    public sealed class TileMutationService : MonoBehaviour
    {
        [SerializeField] private GridWorld gridWorld;
        [SerializeField] private Tilemap terrain;
        [SerializeField] private Tilemap decoration;
        [SerializeField] private TilemapCollider2D terrainCollider;
        [SerializeField] private CompositeCollider2D compositeCollider;
        [SerializeField] private Collider2D playerCollider;
        [SerializeField] private TileDefinition[] definitions;
        [SerializeField] private Vector2Int requiredStart = new Vector2Int(2, 1);
        [SerializeField] private Vector2Int requiredExit = new Vector2Int(44, 1);
        [SerializeField] private Vector2Int[] protectedExitCells;

        private readonly List<TileMutationRequest> pending =
            new List<TileMutationRequest>(64);
        private readonly Dictionary<TileBase, TileDefinition> definitionByTile =
            new Dictionary<TileBase, TileDefinition>();
        private readonly HashSet<GridPos> protectedCells =
            new HashSet<GridPos>();

        private long nextSequence;
        private int mutationTick;
        private int pathVersion;
        private bool isFlushing;

        public event Action<TileMutationBatchReport> BatchCommitted;

        public int PendingCount => pending.Count;
        public int MutationTick => mutationTick;
        public int PathVersion => pathVersion;
        public GridPos RequiredStart => new GridPos(requiredStart.x, requiredStart.y);
        public GridPos RequiredExit => new GridPos(requiredExit.x, requiredExit.y);
        public TileMutationBatchReport LastReport { get; private set; } =
            TileMutationBatchReport.Empty;

        public void Configure(
            GridWorld world,
            Tilemap terrainTilemap,
            Tilemap decorationTilemap,
            TilemapCollider2D tilemapCollider,
            CompositeCollider2D composite,
            Collider2D targetPlayerCollider,
            TileDefinition[] tileDefinitions,
            GridPos start,
            GridPos exit,
            GridPos[] exitProtectedCells)
        {
            gridWorld = world;
            terrain = terrainTilemap;
            decoration = decorationTilemap;
            terrainCollider = tilemapCollider;
            compositeCollider = composite;
            playerCollider = targetPlayerCollider;
            definitions = tileDefinitions;
            requiredStart = new Vector2Int(start.X, start.Y);
            requiredExit = new Vector2Int(exit.X, exit.Y);
            protectedExitCells = ToVectorArray(exitProtectedCells);
            RebuildIndexes();
        }

        private void Awake()
        {
            if (gridWorld == null)
            {
                gridWorld = FindFirstObjectByType<GridWorld>();
            }

            if (terrain == null && gridWorld != null)
            {
                terrain = gridWorld.TerrainTilemap;
            }

            if (terrainCollider == null && terrain != null)
            {
                terrainCollider = terrain.GetComponent<TilemapCollider2D>();
            }

            if (compositeCollider == null && terrain != null)
            {
                compositeCollider = terrain.GetComponent<CompositeCollider2D>();
            }

            RebuildIndexes();
        }

        private void FixedUpdate()
        {
            if (!isFlushing && pending.Count > 0)
            {
                FlushPending();
            }
        }

        public long EnqueueDestroy(
            GridPos cell,
            TileBreakMethod method,
            UnityEngine.Object requester = null)
        {
            long sequence = ++nextSequence;
            pending.Add(new TileMutationRequest(
                sequence,
                cell,
                TileMutationKind.Destroy,
                method,
                null,
                requester));
            return sequence;
        }

        public long EnqueueSet(
            GridPos cell,
            TileDefinition replacement,
            TileBreakMethod method = TileBreakMethod.System,
            UnityEngine.Object requester = null)
        {
            long sequence = ++nextSequence;
            pending.Add(new TileMutationRequest(
                sequence,
                cell,
                TileMutationKind.Set,
                method,
                replacement != null ? replacement.Tile : null,
                requester));
            return sequence;
        }

        public TileMutationBatchReport FlushPending()
        {
            if (isFlushing
                || pending.Count == 0
                || terrain == null
                || gridWorld == null)
            {
                return TileMutationBatchReport.Empty;
            }

            isFlushing = true;
            try
            {
                return FlushPendingCore();
            }
            finally
            {
                isFlushing = false;
            }
        }

        private TileMutationBatchReport FlushPendingCore()
        {
            mutationTick++;
            List<TileMutationRequest> requests =
                new List<TileMutationRequest>(pending);
            pending.Clear();
            requests.Sort((left, right) => left.Sequence.CompareTo(right.Sequence));

            Dictionary<GridPos, TileMutationRequest> lastByCell =
                new Dictionary<GridPos, TileMutationRequest>();
            for (int index = 0; index < requests.Count; index++)
            {
                lastByCell[requests[index].Cell] = requests[index];
            }

            Dictionary<GridPos, TileBase> prospective =
                new Dictionary<GridPos, TileBase>();
            List<TileMutationRecord> records =
                new List<TileMutationRecord>(requests.Count);

            for (int index = 0; index < requests.Count; index++)
            {
                TileMutationRequest request = requests[index];
                TileBase previous = GetProspectiveTile(request.Cell, prospective);
                if (!ReferenceEquals(lastByCell[request.Cell], request))
                {
                    records.Add(new TileMutationRecord(
                        request,
                        previous,
                        previous,
                        TileMutationRejection.Superseded));
                    continue;
                }

                TileMutationRejection rejection =
                    ValidateRequest(request, previous);
                TileBase next = request.Kind == TileMutationKind.Destroy
                    ? null
                    : request.Replacement;

                if (rejection == TileMutationRejection.None)
                {
                    prospective[request.Cell] = next;
                    if (!CanReachWithOverrides(prospective))
                    {
                        prospective.Remove(request.Cell);
                        rejection = TileMutationRejection.ExitBlocked;
                        next = previous;
                    }
                }

                records.Add(new TileMutationRecord(
                    request,
                    previous,
                    next,
                    rejection));
            }

            bool changed = false;
            for (int index = 0; index < records.Count; index++)
            {
                TileMutationRecord record = records[index];
                if (!record.Committed)
                {
                    continue;
                }

                terrain.SetTile(ToVector3Int(record.Request.Cell), record.Next);
                if (record.Request.Kind == TileMutationKind.Destroy
                    && decoration != null)
                {
                    decoration.SetTile(ToVector3Int(record.Request.Cell), null);
                }

                changed = true;
            }

            if (changed)
            {
                RefreshCollision();
                pathVersion++;
            }

            LastReport = new TileMutationBatchReport(
                mutationTick,
                records,
                pathVersion);
            BatchCommitted?.Invoke(LastReport);
            return LastReport;
        }

        public bool IsCurrentExitReachable()
        {
            return CanReachWithOverrides(null);
        }

        public bool IsProtectedCell(GridPos cell)
        {
            return protectedCells.Contains(cell);
        }

        public bool TryGetDefinition(
            TileBase tile,
            out TileDefinition definition)
        {
            if (tile == null)
            {
                definition = null;
                return false;
            }

            return definitionByTile.TryGetValue(tile, out definition);
        }

        private TileMutationRejection ValidateRequest(
            TileMutationRequest request,
            TileBase previous)
        {
            if (!gridWorld.IsWithinBounds(request.Cell))
            {
                return TileMutationRejection.OutOfBounds;
            }

            if (protectedCells.Contains(request.Cell))
            {
                return TileMutationRejection.ProtectedExit;
            }

            if (previous != null
                && definitionByTile.TryGetValue(
                    previous,
                    out TileDefinition previousDefinition)
                && previousDefinition.IsProtected)
            {
                return TileMutationRejection.ProtectedExit;
            }

            if (request.Kind == TileMutationKind.Destroy)
            {
                if (previous == null)
                {
                    return TileMutationRejection.NoTile;
                }

                if (!definitionByTile.TryGetValue(
                        previous,
                        out TileDefinition definition))
                {
                    return TileMutationRejection.UndefinedTile;
                }

                if (definition.IsProtected)
                {
                    return TileMutationRejection.ProtectedExit;
                }

                return definition.CanBreak(request.Method)
                    ? TileMutationRejection.None
                    : TileMutationRejection.Indestructible;
            }

            if (request.Replacement == null)
            {
                return TileMutationRejection.MissingReplacement;
            }

            if (!definitionByTile.TryGetValue(
                    request.Replacement,
                    out TileDefinition replacementDefinition))
            {
                return TileMutationRejection.UndefinedTile;
            }

            if (replacementDefinition.Solid && OverlapsPlayer(request.Cell))
            {
                return TileMutationRejection.PlayerOverlap;
            }

            if (replacementDefinition.Solid && gridWorld.IsOccupied(request.Cell))
            {
                return TileMutationRejection.Occupied;
            }

            return TileMutationRejection.None;
        }

        private bool CanReachWithOverrides(
            Dictionary<GridPos, TileBase> overrides)
        {
            return GridReachabilityValidator.CanReach(
                gridWorld.CellBounds,
                RequiredStart,
                RequiredExit,
                cell => IsSolid(cell, overrides));
        }

        private bool IsSolid(
            GridPos cell,
            Dictionary<GridPos, TileBase> overrides)
        {
            TileBase tile;
            if (overrides != null && overrides.TryGetValue(cell, out TileBase replacement))
            {
                tile = replacement;
            }
            else
            {
                tile = terrain.GetTile(ToVector3Int(cell));
            }

            if (tile == null)
            {
                return false;
            }

            return !definitionByTile.TryGetValue(tile, out TileDefinition definition)
                || definition.Solid;
        }

        private bool OverlapsPlayer(GridPos cell)
        {
            if (playerCollider == null || !playerCollider.enabled)
            {
                return false;
            }

            Vector2 center = gridWorld.CellToWorldCenter(cell);
            Bounds playerBounds = playerCollider.bounds;
            Rect playerRect = new Rect(
                playerBounds.min.x,
                playerBounds.min.y,
                playerBounds.size.x,
                playerBounds.size.y);
            Rect cellRect = new Rect(
                center.x - 0.5f,
                center.y - 0.5f,
                1f,
                1f);
            return playerRect.Overlaps(cellRect);
        }

        private TileBase GetProspectiveTile(
            GridPos cell,
            Dictionary<GridPos, TileBase> overrides)
        {
            return overrides.TryGetValue(cell, out TileBase tile)
                ? tile
                : terrain.GetTile(ToVector3Int(cell));
        }

        private void RefreshCollision()
        {
            terrain.RefreshAllTiles();
            terrainCollider?.ProcessTilemapChanges();
            compositeCollider?.GenerateGeometry();
            Physics2D.SyncTransforms();
        }

        private void RebuildIndexes()
        {
            definitionByTile.Clear();
            if (definitions != null)
            {
                for (int index = 0; index < definitions.Length; index++)
                {
                    TileDefinition definition = definitions[index];
                    if (definition != null && definition.Tile != null)
                    {
                        definitionByTile[definition.Tile] = definition;
                    }
                }
            }

            protectedCells.Clear();
            if (protectedExitCells != null)
            {
                for (int index = 0; index < protectedExitCells.Length; index++)
                {
                    Vector2Int cell = protectedExitCells[index];
                    protectedCells.Add(new GridPos(cell.x, cell.y));
                }
            }
        }

        private static Vector3Int ToVector3Int(GridPos cell)
        {
            return new Vector3Int(cell.X, cell.Y, 0);
        }

        private static Vector2Int[] ToVectorArray(GridPos[] cells)
        {
            if (cells == null)
            {
                return new Vector2Int[0];
            }

            Vector2Int[] values = new Vector2Int[cells.Length];
            for (int index = 0; index < cells.Length; index++)
            {
                values[index] = new Vector2Int(cells[index].X, cells[index].Y);
            }

            return values;
        }
    }
}

#endif
