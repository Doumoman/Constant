using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace StarNight.Map.WorldGeneration.Baking
{
    /// <summary>
    /// Logical bake commands are applied to Unity Tilemaps only through this
    /// narrow boundary. It owns no Player, Camera, seed selection, or scene
    /// lifecycle. RMAP02 also uses its seven-layer fixture overload for the
    /// explicitly authored 60x40 physical course.
    /// </summary>
    [Serializable]
    public sealed class GeneratedUnityTilemapLayerBinding
    {
        [SerializeField] private GeneratedTilemapLayerId layerId;
        [SerializeField] private Tilemap tilemap;
        [SerializeField] private TileBase occupiedTile;

        public GeneratedUnityTilemapLayerBinding()
        {
        }

        public GeneratedUnityTilemapLayerBinding(
            GeneratedTilemapLayerId layerId,
            Tilemap tilemap,
            TileBase occupiedTile)
        {
            this.layerId = layerId;
            this.tilemap = tilemap;
            this.occupiedTile = occupiedTile;
        }

        public GeneratedTilemapLayerId LayerId { get { return layerId; } }
        public Tilemap Tilemap { get { return tilemap; } }
        public TileBase OccupiedTile { get { return occupiedTile; } }
    }

    public readonly struct Rmap02TilemapFixtureCell
    {
        public Rmap02TilemapFixtureCell(
            GeneratedTilemapLayerId layerId,
            int x,
            int y,
            bool isOccupied)
        {
            LayerId = layerId;
            Position = new Vector3Int(x, y, 0);
            IsOccupied = isOccupied;
        }

        public GeneratedTilemapLayerId LayerId { get; }
        public Vector3Int Position { get; }
        public bool IsOccupied { get; }
    }

    /// <summary>
    /// Explicit authored physical fixture. The seven logical layer identities
    /// and integer cell coordinates match the GeneratedCellPlacement/Bake seam,
    /// while its 60x40 size deliberately remains a non-seeded RMAP02 course.
    /// </summary>
    public sealed class Rmap02TilemapFixturePlan
    {
        private readonly ReadOnlyCollection<Rmap02TilemapFixtureCell> cells;

        private Rmap02TilemapFixturePlan(
            int width,
            int height,
            IEnumerable<Rmap02TilemapFixtureCell> cells)
        {
            Width = width;
            Height = height;
            this.cells = new ReadOnlyCollection<Rmap02TilemapFixtureCell>((cells ??
                Array.Empty<Rmap02TilemapFixtureCell>()).OrderBy(value => value.LayerId)
                .ThenBy(value => value.Position.y).ThenBy(value => value.Position.x).ToArray());
        }

        public const int WidthInTiles = 60;
        public const int HeightInTiles = 40;
        public const int SpawnX = 3;
        public const int SpawnY = 1;
        public const int ExitX = 42;
        public const int ExitY = 1;

        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<Rmap02TilemapFixtureCell> Cells { get { return cells; } }

        public static Rmap02TilemapFixturePlan Create()
        {
            var occupied = new Dictionary<string, Rmap02TilemapFixtureCell>(
                StringComparer.Ordinal);
            AddHorizontal(occupied, GeneratedTilemapLayerId.Terrain, 0, WidthInTiles - 1, 0);
            Add(occupied, GeneratedTilemapLayerId.Terrain, 12, 1);
            // A one-tile-high passage: Player is 0.8 tiles tall, so it can
            // pass while a jump below this ceiling proves an actual hit.
            AddHorizontal(occupied, GeneratedTilemapLayerId.Terrain, 18, 22, 2);
            AddVertical(occupied, GeneratedTilemapLayerId.Terrain, 50, 1, 6);

            // Non-solid display/provenance layers remain distinct from Terrain.
            AddHorizontal(occupied, GeneratedTilemapLayerId.Affordance, 3, ExitX, 1);
            Add(occupied, GeneratedTilemapLayerId.Material, 12, 1);
            Add(occupied, GeneratedTilemapLayerId.Hazard, 54, 1);
            Add(occupied, GeneratedTilemapLayerId.Marker, ExitX, ExitY);
            AddHorizontal(occupied, GeneratedTilemapLayerId.Protection, 0, WidthInTiles - 1, 0);
            AddHorizontal(occupied, GeneratedTilemapLayerId.SourceOwner, 0, WidthInTiles - 1, 0);

            return new Rmap02TilemapFixturePlan(WidthInTiles, HeightInTiles,
                occupied.Values);
        }

        private static void Add(
            IDictionary<string, Rmap02TilemapFixtureCell> cells,
            GeneratedTilemapLayerId layer,
            int x,
            int y)
        {
            if (x < 0 || x >= WidthInTiles || y < 0 || y >= HeightInTiles)
            {
                throw new ArgumentOutOfRangeException(nameof(x),
                    "RMAP02 fixture cells must remain inside 60x40.");
            }

            var cell = new Rmap02TilemapFixtureCell(layer, x, y, true);
            cells[layer + ":" + x + ":" + y] = cell;
        }

        private static void AddHorizontal(
            IDictionary<string, Rmap02TilemapFixtureCell> cells,
            GeneratedTilemapLayerId layer,
            int minX,
            int maxX,
            int y)
        {
            for (var x = minX; x <= maxX; x++)
            {
                Add(cells, layer, x, y);
            }
        }

        private static void AddVertical(
            IDictionary<string, Rmap02TilemapFixtureCell> cells,
            GeneratedTilemapLayerId layer,
            int x,
            int minY,
            int maxY)
        {
            for (var y = minY; y <= maxY; y++)
            {
                Add(cells, layer, x, y);
            }
        }
    }

    public sealed class GeneratedUnityTilemapApplyReport
    {
        internal GeneratedUnityTilemapApplyReport(
            int clearedTileCount,
            int appliedTileCount,
            IEnumerable<GeneratedTilemapLayerId> layers)
        {
            ClearedTileCount = clearedTileCount;
            AppliedTileCount = appliedTileCount;
            AppliedLayers = new ReadOnlyCollection<GeneratedTilemapLayerId>((layers ??
                Array.Empty<GeneratedTilemapLayerId>()).Distinct().OrderBy(value => value).ToArray());
        }

        public int ClearedTileCount { get; }
        public int AppliedTileCount { get; }
        public IReadOnlyList<GeneratedTilemapLayerId> AppliedLayers { get; }
    }

    public sealed class GeneratedUnityTilemapApplier : MonoBehaviour
    {
        // The builder and tests configure bindings after AddComponent.  The
        // saved RMAP02 scene serializes this as true, so runtime application
        // still occurs before the local Player bootstrap starts.
        [SerializeField] private bool applyFixtureOnAwake;
        [SerializeField] private GeneratedUnityTilemapLayerBinding[] layerBindings =
            Array.Empty<GeneratedUnityTilemapLayerBinding>();

        public GeneratedUnityTilemapApplyReport LastReport { get; private set; }

        public void Configure(
            IEnumerable<GeneratedUnityTilemapLayerBinding> bindings,
            bool applyFixtureOnAwake)
        {
            layerBindings = (bindings ?? Array.Empty<GeneratedUnityTilemapLayerBinding>()).ToArray();
            this.applyFixtureOnAwake = applyFixtureOnAwake;
        }

        public GeneratedUnityTilemapApplyReport ApplyFixture()
        {
            LastReport = ApplyCells(Rmap02TilemapFixturePlan.Create().Cells, layerBindings);
            return LastReport;
        }

        /// <summary>
        /// The later generated-world caller supplies an already validated
        /// GeneratedTilemapBakePlan. This method does not invoke the logical
        /// baker or alter its plan; it only projects commands to Tilemaps.
        /// </summary>
        public static GeneratedUnityTilemapApplyReport Apply(
            GeneratedTilemapBakePlan bakePlan,
            IEnumerable<GeneratedUnityTilemapLayerBinding> bindings)
        {
            if (bakePlan == null)
            {
                throw new ArgumentNullException(nameof(bakePlan));
            }

            var cells = bakePlan.Commands.Select(command =>
                new Rmap02TilemapFixtureCell(command.LayerId,
                    command.SectorLocalX, command.SectorLocalY, command.IsOccupied));
            return ApplyCells(cells, bindings);
        }

        private void Awake()
        {
            if (applyFixtureOnAwake)
            {
                ApplyFixture();
            }
        }

        private static GeneratedUnityTilemapApplyReport ApplyCells(
            IEnumerable<Rmap02TilemapFixtureCell> sourceCells,
            IEnumerable<GeneratedUnityTilemapLayerBinding> sourceBindings)
        {
            var bindings = (sourceBindings ?? Array.Empty<GeneratedUnityTilemapLayerBinding>())
                .OrderBy(value => value == null ? 0 : (int)value.LayerId).ToArray();
            if (bindings.Length != 7 || bindings.Any(value => value == null ||
                value.Tilemap == null || value.OccupiedTile == null) ||
                bindings.Select(value => value.LayerId).Distinct().Count() != 7)
            {
                throw new InvalidOperationException(
                    "Exactly one Tilemap and occupied Tile binding is required for each seven-layer id.");
            }

            var byLayer = bindings.ToDictionary(value => value.LayerId);
            var cleared = 0;
            foreach (var binding in bindings)
            {
                cleared += binding.Tilemap.GetTilesBlock(binding.Tilemap.cellBounds)
                    .Count(value => value != null);
                binding.Tilemap.ClearAllTiles();
            }

            var applied = 0;
            foreach (var group in (sourceCells ?? Array.Empty<Rmap02TilemapFixtureCell>())
                .Where(value => value.IsOccupied).GroupBy(value => value.LayerId))
            {
                GeneratedUnityTilemapLayerBinding binding;
                if (!byLayer.TryGetValue(group.Key, out binding))
                {
                    throw new InvalidOperationException("Missing Tilemap binding for " + group.Key + ".");
                }

                var positions = group.OrderBy(value => value.Position.y)
                    .ThenBy(value => value.Position.x).Select(value => value.Position).ToArray();
                var tiles = Enumerable.Repeat(binding.OccupiedTile, positions.Length).ToArray();
                binding.Tilemap.SetTiles(positions, tiles);
                applied += positions.Length;
            }

            Physics2D.SyncTransforms();
            return new GeneratedUnityTilemapApplyReport(cleared, applied,
                bindings.Select(value => value.LayerId));
        }
    }
}
