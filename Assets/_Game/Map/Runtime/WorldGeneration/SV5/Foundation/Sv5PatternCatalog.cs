using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.SV5.Foundation;

namespace StarNight.Map.WorldGeneration.MicroPatterns
{
    /// <summary>
    /// SV5's typed 4x4 base-geometry catalog.  This deliberately does not
    /// model ports or 12x8 composition; SV5/SV5 own those contracts.
    /// Coordinates are lower-left origin, x right, y up, index = y * 4 + x.
    /// </summary>
    public enum Sv5PatternBaseCell
    {
        Air = 0,
        Solid = 1,
        OneWayPlatform = 2,
    }

    public enum Sv5PatternPrimaryRole
    {
        SlopeRiseRight = 0,
        SlopeRiseLeft = 1,
        CeilingFlat = 2,
        CeilingRough = 3,
        WallLeft = 4,
        WallRight = 5,
        VoidClear = 6,
        SparseAirPlatform = 7,
        StandableLedge = 8,
        VerticalPassage = 9,
    }

    public enum Sv5PatternTransform
    {
        R0 = 0,
        MirrorX = 1,
        MirrorY = 2,
        R180 = 3,
    }

    public enum Sv5PatternEdge
    {
        Left = 0,
        Right = 1,
        Up = 2,
        Down = 3,
    }

    [Flags]
    public enum Sv5PatternIntentTag : uint
    {
        None = 0,
        FlatFloor = 1u << 0,
        StepFloor = 1u << 1,
        ShortLedge = 1u << 2,
        LongLedge = 1u << 3,
        Takeoff = 1u << 4,
        Landing = 1u << 5,
        RunApproach = 1u << 6,
        GrabbableCorner = 1u << 7,
        LowCeiling = 1u << 8,
        OneTilePassage = 1u << 9,
        VerticalClearance = 1u << 10,
        LeftEntry = 1u << 11,
        RightExit = 1u << 12,
        UpEntry = 1u << 13,
        DownExit = 1u << 14,
        RequiredOk = 1u << 15,
        OptionalOnly = 1u << 16,
        SecretShell = 1u << 17,
        QuietFill = 1u << 18,
        LadderAllowed = 1u << 19,
        PillarAllowed = 1u << 20,
        HazardAllowed = 1u << 21,
        MechanismAllowed = 1u << 22,
        RewardAllowed = 1u << 23,
    }

    public readonly struct Sv5PatternLocalCell : IEquatable<Sv5PatternLocalCell>
    {
        public Sv5PatternLocalCell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }

        public bool Equals(Sv5PatternLocalCell other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is Sv5PatternLocalCell other && Equals(other);
        public override int GetHashCode() => (X * 397) ^ Y;
        public override string ToString() => X.ToString(CultureInfo.InvariantCulture) + "," +
                                             Y.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class Sv5PatternSource
    {
        internal Sv5PatternSource(
            string sourcePatternId,
            string sourceMaskReference,
            string originalReference,
            Sv5PatternIntentTag intentTags,
            IEnumerable<Sv5PatternBaseCell> baseCells)
        {
            SourcePatternId = sourcePatternId ?? throw new ArgumentNullException(nameof(sourcePatternId));
            SourceMaskReference = sourceMaskReference ?? string.Empty;
            OriginalReference = originalReference ?? string.Empty;
            IntentTags = intentTags;
            BaseCells = new ReadOnlyCollection<Sv5PatternBaseCell>((baseCells ??
                throw new ArgumentNullException(nameof(baseCells))).ToArray());
            if (BaseCells.Count != Sv5PatternCatalog.CellCount)
            {
                throw new ArgumentException("A source pattern must contain exactly 16 base cells.",
                    nameof(baseCells));
            }
        }

        public string SourcePatternId { get; }
        public string SourceMaskReference { get; }
        public string OriginalReference { get; }
        public Sv5PatternIntentTag IntentTags { get; }
        public IReadOnlyList<Sv5PatternBaseCell> BaseCells { get; }
    }

    public sealed class Sv5PatternOrigin
    {
        internal Sv5PatternOrigin(
            Sv5PatternSource source,
            Sv5PatternTransform transform,
            Sv5PatternIntentTag transformedTags,
            IEnumerable<string> unresolvedDirectionalTags)
        {
            SourcePatternId = source.SourcePatternId;
            SourceMaskReference = source.SourceMaskReference;
            OriginalReference = source.OriginalReference;
            Transform = transform;
            IntentTags = transformedTags;
            UnresolvedDirectionalTags = new ReadOnlyCollection<string>((unresolvedDirectionalTags ??
                Array.Empty<string>()).Distinct(StringComparer.Ordinal).OrderBy(value => value,
                StringComparer.Ordinal).ToArray());
        }

        public string SourcePatternId { get; }
        public string SourceMaskReference { get; }
        public string OriginalReference { get; }
        public Sv5PatternTransform Transform { get; }
        public Sv5PatternIntentTag IntentTags { get; }
        public IReadOnlyList<string> UnresolvedDirectionalTags { get; }
    }

    public sealed class Sv5PatternEdgeCell
    {
        internal Sv5PatternEdgeCell(Sv5PatternLocalCell coordinate, Sv5PatternBaseCell cell)
        {
            Coordinate = coordinate;
            Cell = cell;
        }

        public Sv5PatternLocalCell Coordinate { get; }
        public Sv5PatternBaseCell Cell { get; }
    }

    public sealed class Sv5PatternStandableSpan
    {
        internal Sv5PatternStandableSpan(int y, int startX, int endX, int headroomCells,
            bool contextRequired)
        {
            Y = y;
            StartX = startX;
            EndX = endX;
            HeadroomCells = headroomCells;
            ContextRequired = contextRequired;
        }

        public int Y { get; }
        public int StartX { get; }
        public int EndX { get; }
        public int Length => EndX - StartX + 1;
        public int HeadroomCells { get; }
        public bool ContextRequired { get; }
    }

    public sealed class Sv5PatternAirComponent
    {
        internal Sv5PatternAirComponent(IEnumerable<Sv5PatternLocalCell> cells)
        {
            Cells = new ReadOnlyCollection<Sv5PatternLocalCell>((cells ?? Array.Empty<Sv5PatternLocalCell>())
                .OrderBy(value => value.Y).ThenBy(value => value.X).ToArray());
        }

        public IReadOnlyList<Sv5PatternLocalCell> Cells { get; }
    }

    public sealed class Sv5PatternAutomaticCharacteristics
    {
        internal Sv5PatternAutomaticCharacteristics(
            IDictionary<Sv5PatternEdge, IReadOnlyList<Sv5PatternEdgeCell>> edgeOpenCellSets,
            IEnumerable<Sv5PatternStandableSpan> standableSpans,
            IEnumerable<Sv5PatternLocalCell> launchCells,
            IEnumerable<Sv5PatternLocalCell> landingCells,
            IEnumerable<Sv5PatternLocalCell> grabCorners,
            IEnumerable<int> fallColumns,
            IEnumerable<Sv5PatternLocalCell> climbOverlayCells,
            int solidCount,
            int airCount,
            int oneWayCount,
            IEnumerable<Sv5PatternAirComponent> airComponents,
            IEnumerable<Sv5PatternTransform> symmetricTransforms,
            IEnumerable<string> contextRequired)
        {
            EdgeOpenCellSets = new ReadOnlyDictionary<Sv5PatternEdge, IReadOnlyList<Sv5PatternEdgeCell>>(
                new Dictionary<Sv5PatternEdge, IReadOnlyList<Sv5PatternEdgeCell>>(edgeOpenCellSets));
            StandableSpans = new ReadOnlyCollection<Sv5PatternStandableSpan>((standableSpans ??
                Array.Empty<Sv5PatternStandableSpan>()).ToArray());
            LaunchCells = SortedCells(launchCells);
            LandingCells = SortedCells(landingCells);
            GrabCorners = SortedCells(grabCorners);
            FallColumns = new ReadOnlyCollection<int>((fallColumns ?? Array.Empty<int>()).Distinct().OrderBy(x => x).ToArray());
            ClimbOverlayCells = SortedCells(climbOverlayCells);
            SolidCount = solidCount;
            AirCount = airCount;
            OneWayCount = oneWayCount;
            SolidRatio = solidCount / (double)Sv5PatternCatalog.CellCount;
            AirRatio = airCount / (double)Sv5PatternCatalog.CellCount;
            OneWayRatio = oneWayCount / (double)Sv5PatternCatalog.CellCount;
            AirComponents = new ReadOnlyCollection<Sv5PatternAirComponent>((airComponents ??
                Array.Empty<Sv5PatternAirComponent>()).ToArray());
            SymmetricTransforms = new ReadOnlyCollection<Sv5PatternTransform>((symmetricTransforms ??
                Array.Empty<Sv5PatternTransform>()).Distinct().OrderBy(value => value).ToArray());
            ContextRequired = new ReadOnlyCollection<string>((contextRequired ?? Array.Empty<string>())
                .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }

        public IReadOnlyDictionary<Sv5PatternEdge, IReadOnlyList<Sv5PatternEdgeCell>> EdgeOpenCellSets { get; }
        public IReadOnlyList<Sv5PatternStandableSpan> StandableSpans { get; }
        public IReadOnlyList<Sv5PatternLocalCell> LaunchCells { get; }
        public IReadOnlyList<Sv5PatternLocalCell> LandingCells { get; }
        public IReadOnlyList<Sv5PatternLocalCell> GrabCorners { get; }
        public IReadOnlyList<int> FallColumns { get; }
        public IReadOnlyList<Sv5PatternLocalCell> ClimbOverlayCells { get; }
        public int SolidCount { get; }
        public int AirCount { get; }
        public int OneWayCount { get; }
        public double SolidRatio { get; }
        public double AirRatio { get; }
        public double OneWayRatio { get; }
        public IReadOnlyList<Sv5PatternAirComponent> AirComponents { get; }
        public IReadOnlyList<Sv5PatternTransform> SymmetricTransforms { get; }
        public IReadOnlyList<string> ContextRequired { get; }

        private static IReadOnlyList<Sv5PatternLocalCell> SortedCells(IEnumerable<Sv5PatternLocalCell> values) =>
            new ReadOnlyCollection<Sv5PatternLocalCell>((values ?? Array.Empty<Sv5PatternLocalCell>())
                .Distinct().OrderBy(value => value.Y).ThenBy(value => value.X).ToArray());
    }

    public sealed class Sv5PatternCandidate
    {
        internal Sv5PatternCandidate(
            string candidateId,
            IEnumerable<Sv5PatternBaseCell> baseCells,
            Sv5PatternPrimaryRole primaryRole,
            Sv5PatternIntentTag intentTags,
            IEnumerable<Sv5PatternOrigin> origins,
            IEnumerable<string> tagConflicts,
            Sv5PatternAutomaticCharacteristics characteristics)
        {
            CandidateId = candidateId;
            BaseCells = new ReadOnlyCollection<Sv5PatternBaseCell>(baseCells.ToArray());
            PrimaryRole = primaryRole;
            IntentTags = intentTags;
            Origins = new ReadOnlyCollection<Sv5PatternOrigin>(origins.OrderBy(value => value.SourcePatternId,
                StringComparer.Ordinal).ThenBy(value => value.Transform).ToArray());
            TagConflicts = new ReadOnlyCollection<string>((tagConflicts ?? Array.Empty<string>()).Distinct(
                StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
            Characteristics = characteristics;
        }

        public string CandidateId { get; }
        public IReadOnlyList<Sv5PatternBaseCell> BaseCells { get; }
        public Sv5PatternPrimaryRole PrimaryRole { get; }
        public Sv5PatternIntentTag IntentTags { get; }
        public IReadOnlyList<Sv5PatternOrigin> Origins { get; }
        public IReadOnlyList<string> TagConflicts { get; }
        public Sv5PatternAutomaticCharacteristics Characteristics { get; }

        public Sv5PatternBaseCell GetCell(int x, int y)
        {
            if (x < 0 || x >= Sv5PatternCatalog.Width || y < 0 || y >= Sv5PatternCatalog.Height)
            {
                throw new ArgumentOutOfRangeException();
            }

            return BaseCells[y * Sv5PatternCatalog.Width + x];
        }
    }

    public sealed class Sv5PatternCatalogSnapshot
    {
        internal Sv5PatternCatalogSnapshot(
            IEnumerable<Sv5PatternCandidate> candidates,
            int generatedDistinctBaseGeometryCount,
            int sourceTransformRelationCount)
        {
            Candidates = new ReadOnlyCollection<Sv5PatternCandidate>((candidates ??
                Array.Empty<Sv5PatternCandidate>()).OrderBy(value => value.CandidateId,
                StringComparer.Ordinal).ToArray());
            GeneratedDistinctBaseGeometryCount = generatedDistinctBaseGeometryCount;
            SourceTransformRelationCount = sourceTransformRelationCount;
        }

        public IReadOnlyList<Sv5PatternCandidate> Candidates { get; }
        public int GeneratedDistinctBaseGeometryCount { get; }
        public int SourceTransformRelationCount { get; }
        public int RelationsCollapsedByBaseGeometry => SourceTransformRelationCount -
            GeneratedDistinctBaseGeometryCount;
        public int ExcludedAfterInitialPoolCount => GeneratedDistinctBaseGeometryCount - Candidates.Count;

        public bool TryGetCandidate(string candidateId, out Sv5PatternCandidate candidate)
        {
            candidate = Candidates.FirstOrDefault(value => string.Equals(value.CandidateId, candidateId,
                StringComparison.Ordinal));
            return candidate != null;
        }
    }

    public static class Sv5PatternCatalog
    {
        public const int Width = 4;
        public const int Height = 4;
        public const int CellCount = Width * Height;
        public const int InitialPoolCount = 48;
        public const string DataVersion = "SV5_FIRST_POOL_V1";
        public const string PatternMaskSource = "SV5_DETERMINISTIC_PATTERN_LIBRARY";

        private static readonly Lazy<Sv5PatternCatalogSnapshot> InitialPool =
            new Lazy<Sv5PatternCatalogSnapshot>(BuildInitialPoolInternal);

        public static Sv5PatternCatalogSnapshot BuildInitialPool() => InitialPool.Value;

        public static IReadOnlyList<Sv5PatternBaseCell> TransformCells(
            IReadOnlyList<Sv5PatternBaseCell> source,
            Sv5PatternTransform transform)
        {
            if (source == null || source.Count != CellCount)
            {
                throw new ArgumentException("Exactly 16 source cells are required.", nameof(source));
            }

            var result = new Sv5PatternBaseCell[CellCount];
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    int targetX;
                    int targetY;
                    switch (transform)
                    {
                        case Sv5PatternTransform.R0: targetX = x; targetY = y; break;
                        case Sv5PatternTransform.MirrorX: targetX = Width - 1 - x; targetY = y; break;
                        case Sv5PatternTransform.MirrorY: targetX = x; targetY = Height - 1 - y; break;
                        case Sv5PatternTransform.R180: targetX = Width - 1 - x; targetY = Height - 1 - y; break;
                        default: throw new ArgumentOutOfRangeException(nameof(transform));
                    }

                    result[targetY * Width + targetX] = source[y * Width + x];
                }
            }

            return new ReadOnlyCollection<Sv5PatternBaseCell>(result);
        }

        public static Sv5PatternPrimaryRole Classify(IReadOnlyList<Sv5PatternBaseCell> cells)
        {
            EnsureCells(cells);
            if (cells.All(value => value == Sv5PatternBaseCell.Air)) return Sv5PatternPrimaryRole.VoidClear;
            if (cells.Count(value => value == Sv5PatternBaseCell.OneWayPlatform) > 0 &&
                cells.Count(value => value == Sv5PatternBaseCell.Solid) <= 2)
                return Sv5PatternPrimaryRole.SparseAirPlatform;
            if (IsVerticalPassage(cells)) return Sv5PatternPrimaryRole.VerticalPassage;
            int slope = SlopeDirection(cells);
            if (slope > 0) return Sv5PatternPrimaryRole.SlopeRiseRight;
            if (slope < 0) return Sv5PatternPrimaryRole.SlopeRiseLeft;
            if (IsFullColumn(cells, 0) && !IsFullColumn(cells, Width - 1)) return Sv5PatternPrimaryRole.WallLeft;
            if (IsFullColumn(cells, Width - 1) && !IsFullColumn(cells, 0)) return Sv5PatternPrimaryRole.WallRight;
            if (IsFlatCeiling(cells)) return Sv5PatternPrimaryRole.CeilingFlat;
            if (TopCount(cells) >= 2 && BottomCount(cells) < TopCount(cells))
                return Sv5PatternPrimaryRole.CeilingRough;
            return Sv5PatternPrimaryRole.StandableLedge;
        }

        public static Sv5PatternAutomaticCharacteristics CalculateCharacteristics(
            IReadOnlyList<Sv5PatternBaseCell> cells)
        {
            EnsureCells(cells);
            var edges = new Dictionary<Sv5PatternEdge, IReadOnlyList<Sv5PatternEdgeCell>>();
            foreach (Sv5PatternEdge edge in Enum.GetValues(typeof(Sv5PatternEdge)))
            {
                var values = EdgeCoordinates(edge).Select(coordinate => new Sv5PatternEdgeCell(coordinate,
                        Cell(cells, coordinate.X, coordinate.Y)))
                    .Where(value => value.Cell != Sv5PatternBaseCell.Solid).ToArray();
                edges.Add(edge, new ReadOnlyCollection<Sv5PatternEdgeCell>(values));
            }

            var spans = FindStandableSpans(cells).ToArray();
            var launch = new List<Sv5PatternLocalCell>();
            var landing = new List<Sv5PatternLocalCell>();
            var grab = new List<Sv5PatternLocalCell>();
            var fallColumns = new List<int>();
            var climb = new List<Sv5PatternLocalCell>();
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var coordinate = new Sv5PatternLocalCell(x, y);
                    var value = Cell(cells, x, y);
                    if (value == Sv5PatternBaseCell.Air)
                    {
                        climb.Add(coordinate);
                        if (y > 0 && IsStandable(Cell(cells, x, y - 1)))
                        {
                            launch.Add(coordinate);
                            landing.Add(coordinate);
                        }
                    }
                    else if (value == Sv5PatternBaseCell.Solid && HasExposedSolidCorner(cells, x, y))
                    {
                        grab.Add(coordinate);
                    }
                }

            }

            for (var x = 0; x < Width; x++)
            {
                if (Enumerable.Range(0, Height).All(y => Cell(cells, x, y) == Sv5PatternBaseCell.Air))
                {
                    fallColumns.Add(x);
                }
            }

            var symmetric = Enum.GetValues(typeof(Sv5PatternTransform)).Cast<Sv5PatternTransform>()
                .Where(transform => SameCells(cells, TransformCells(cells, transform))).ToArray();
            var context = new List<string>();
            if (spans.Any(value => value.ContextRequired)) context.Add("HEADROOM_OUTSIDE_4X4_UNKNOWN");
            if (edges.Values.Any(value => value.Count > 0)) context.Add("EDGE_NEIGHBOR_PORT_CONTEXT_REQUIRED");
            if (launch.Count > 0 || landing.Count > 0 || cells.Any(value => value == Sv5PatternBaseCell.OneWayPlatform))
                context.Add("JUMP_ARC_AND_PLAYER_PROFILE_CONTEXT_REQUIRED");
            return new Sv5PatternAutomaticCharacteristics(edges, spans, launch, landing, grab, fallColumns,
                climb, cells.Count(value => value == Sv5PatternBaseCell.Solid),
                cells.Count(value => value == Sv5PatternBaseCell.Air),
                cells.Count(value => value == Sv5PatternBaseCell.OneWayPlatform),
                FindAirComponents(cells), symmetric, context);
        }

        public static string SerializeBaseCells16(IReadOnlyList<Sv5PatternBaseCell> cells)
        {
            EnsureCells(cells);
            return new string(cells.Select(ToToken).ToArray());
        }

        public static IReadOnlyList<Sv5PatternBaseCell> ParseBaseCells16(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != CellCount)
                throw new FormatException("BaseCells16 must hold exactly sixteen A/S/O tokens.");
            var cells = value.Select(ParseToken).ToArray();
            return new ReadOnlyCollection<Sv5PatternBaseCell>(cells);
        }

        public static string ExportCatalogCsv(Sv5PatternCatalogSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var lines = new List<string>
            {
                "CandidateId,BaseCells16,PrimaryRole,IntentTags,DataVersion,SourceCount"
            };
            lines.AddRange(snapshot.Candidates.Select(candidate => Csv(candidate.CandidateId,
                SerializeBaseCells16(candidate.BaseCells), candidate.PrimaryRole.ToString(),
                Tags(candidate.IntentTags), DataVersion, candidate.Origins.Count.ToString(CultureInfo.InvariantCulture))));
            return string.Join("\n", lines) + "\n";
        }

        public static string ExportOriginsCsv(Sv5PatternCatalogSnapshot snapshot)
        {
            var lines = new List<string>
            {
                "SourcePatternId,SourceMaskReference,OriginalReference,Transform,CandidateId,IntentTags,UnresolvedDirectionalTags,TagConflicts"
            };
            foreach (var candidate in snapshot.Candidates)
            foreach (var origin in candidate.Origins)
                lines.Add(Csv(origin.SourcePatternId, origin.SourceMaskReference, origin.OriginalReference,
                    origin.Transform.ToString(), candidate.CandidateId, Tags(origin.IntentTags),
                    string.Join(";", origin.UnresolvedDirectionalTags), string.Join(";", candidate.TagConflicts)));
            return string.Join("\n", lines) + "\n";
        }

        public static string ExportCharacteristicsCsv(Sv5PatternCatalogSnapshot snapshot)
        {
            var lines = new List<string>
            {
                "CandidateId,EdgeOpenCellSet,StandableSpans,LaunchCells,LandingCells,GrabCorners,FallColumns,ClimbOverlayCells,SolidAirOneWay,AirComponents,SymmetricTransforms,ContextRequired"
            };
            foreach (var candidate in snapshot.Candidates)
            {
                var c = candidate.Characteristics;
                string edges = string.Join(";", c.EdgeOpenCellSets.OrderBy(value => value.Key).Select(value =>
                    value.Key + ":" + string.Join("/", value.Value.Select(cell => cell.Coordinate + "=" + cell.Cell))));
                string spans = string.Join(";", c.StandableSpans.Select(value => value.StartX + "-" +
                    value.EndX + "@" + value.Y + " headroom=" + value.HeadroomCells +
                    (value.ContextRequired ? "?" : string.Empty)));
                lines.Add(Csv(candidate.CandidateId, edges, spans, Cells(c.LaunchCells), Cells(c.LandingCells),
                    Cells(c.GrabCorners), string.Join(";", c.FallColumns), Cells(c.ClimbOverlayCells),
                    c.SolidCount + "/" + c.AirCount + "/" + c.OneWayCount,
                    string.Join(";", c.AirComponents.Select(component => Cells(component.Cells))),
                    string.Join(";", c.SymmetricTransforms), string.Join(";", c.ContextRequired)));
            }
            return string.Join("\n", lines) + "\n";
        }

        public static void ValidateCatalogCsv(string csv, int expectedRows)
        {
            if (string.IsNullOrEmpty(csv)) throw new FormatException("CSV is required.");
            string[] rows = csv.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (rows.Length != expectedRows + 1) throw new FormatException("Unexpected CSV row count.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 1; index < rows.Length; index++)
            {
                string[] fields = SplitCsv(rows[index]);
                if (fields.Length != 6 || !ids.Add(fields[0])) throw new FormatException("Duplicate or malformed catalog row.");
                ParseBaseCells16(fields[1]);
                if (!Enum.TryParse(fields[2], out Sv5PatternPrimaryRole ignored))
                    throw new FormatException("Unknown primary role.");
            }
        }

        private static Sv5PatternCatalogSnapshot BuildInitialPoolInternal()
        {
            var pending = new Dictionary<string, PendingCandidate>(StringComparer.Ordinal);
            foreach (var source in BuildSources().OrderBy(value => value.SourcePatternId, StringComparer.Ordinal))
            {
                foreach (Sv5PatternTransform transform in Enum.GetValues(typeof(Sv5PatternTransform)))
                {
                    IReadOnlyList<Sv5PatternBaseCell> cells = TransformCells(source.BaseCells, transform);
                    string key = SerializeBaseCells16(cells);
                    if (!pending.TryGetValue(key, out PendingCandidate candidate))
                    {
                        candidate = new PendingCandidate(cells);
                        pending.Add(key, candidate);
                    }

                    TransformTags(source.IntentTags, transform, out Sv5PatternIntentTag transformedTags,
                        out IReadOnlyList<string> unresolved);
                    candidate.Origins.Add(new Sv5PatternOrigin(source, transform, transformedTags, unresolved));
                }
            }

            var all = pending.Values.Select(value => value.ToCandidate()).OrderBy(value => value.CandidateId,
                StringComparer.Ordinal).ToArray();
            var selected = new List<Sv5PatternCandidate>();
            foreach (Sv5PatternPrimaryRole role in Enum.GetValues(typeof(Sv5PatternPrimaryRole)))
            {
                Sv5PatternCandidate representative = all.FirstOrDefault(value => value.PrimaryRole == role);
                if (representative == null)
                    throw new InvalidOperationException("Initial pool has no representative for " + role + ".");
                selected.Add(representative);
            }
            foreach (var candidate in all)
            {
                if (selected.Count >= InitialPoolCount) break;
                if (!selected.Contains(candidate)) selected.Add(candidate);
            }
            if (selected.Count != InitialPoolCount)
                throw new InvalidOperationException("Initial pool must contain exactly 48 distinct base geometries.");
            if (selected.Count(value => value.PrimaryRole == Sv5PatternPrimaryRole.VoidClear) != 1)
                throw new InvalidOperationException("VOID_CLEAR must be represented by exactly one candidate.");
            return new Sv5PatternCatalogSnapshot(selected, all.Length,
                pending.Values.Sum(value => value.Origins.Count));
        }

        private static IEnumerable<Sv5PatternSource> BuildSources()
        {
            // The first eight sources are direct conversions of the SV5 deterministic pool.
            // Bit index y*4+x is solid.
            foreach (Sv5PatternMaskCandidate source in Sv5PatternMaskLibrary.Build()
                         .Candidates.Take(8))
            {
                yield return new Sv5PatternSource(source.CandidateId, source.MaskU16Hex, PatternMaskSource,
                    Sv5PatternIntentTag.QuietFill | Sv5PatternIntentTag.OptionalOnly,
                    Enumerable.Range(0, CellCount).Select(index =>
                        (source.Mask & (1 << index)) != 0 ? Sv5PatternBaseCell.Solid : Sv5PatternBaseCell.Air));
            }

            yield return Seed("SV5_VOID", "", "SV5_AUTHORED_TYPED_V1",
                Sv5PatternIntentTag.QuietFill | Sv5PatternIntentTag.OptionalOnly,
                "....", "....", "....", "....");
            yield return Seed("SV5_SLOPE_A", "0x8EFC", "SV5_AUTHORED_TYPED_V1",
                Sv5PatternIntentTag.FlatFloor | Sv5PatternIntentTag.Takeoff | Sv5PatternIntentTag.LeftEntry |
                Sv5PatternIntentTag.RightExit | Sv5PatternIntentTag.RequiredOk,
                "####", ".###", "..##", "...#");
            yield return Seed("SV5_SLOPE_B", "0xC8F7", "SV5_AUTHORED_TYPED_V1",
                Sv5PatternIntentTag.StepFloor | Sv5PatternIntentTag.RunApproach | Sv5PatternIntentTag.Landing |
                Sv5PatternIntentTag.HazardAllowed,
                "####", "..##", "...#", "....");
            yield return Seed("SV5_CEILING_FLAT", "0xF000", "SV5_AUTHORED_TYPED_V1",
                Sv5PatternIntentTag.LowCeiling | Sv5PatternIntentTag.OneTilePassage |
                Sv5PatternIntentTag.MechanismAllowed,
                "....", "....", "....", "####");
            yield return Seed("SV5_CEILING_ROUGH", "0xDB00", "SV5_AUTHORED_TYPED_V1",
                Sv5PatternIntentTag.LowCeiling | Sv5PatternIntentTag.SecretShell |
                Sv5PatternIntentTag.RewardAllowed,
                "....", "....", "##.#", "####");
            yield return Seed("SV5_WALL_A", "0x111F", "SV5_AUTHORED_TYPED_V1",
                Sv5PatternIntentTag.GrabbableCorner | Sv5PatternIntentTag.LeftEntry |
                Sv5PatternIntentTag.LadderAllowed,
                "#...", "#...", "##..", "#...");
            yield return Seed("SV5_WALL_B", "0x888F", "SV5_AUTHORED_TYPED_V1",
                Sv5PatternIntentTag.GrabbableCorner | Sv5PatternIntentTag.RightExit |
                Sv5PatternIntentTag.PillarAllowed,
                "...#", "...#", "..##", "...#");
            yield return Seed("SV5_VERTICAL", "0x9999", "SV5_AUTHORED_TYPED_V1",
                Sv5PatternIntentTag.VerticalClearance | Sv5PatternIntentTag.UpEntry |
                Sv5PatternIntentTag.DownExit | Sv5PatternIntentTag.LadderAllowed | Sv5PatternIntentTag.PillarAllowed,
                "#..#", "#..#", "#..#", "#..#");
            yield return Seed("SV5_LEDGE_A", "0x0037", "SV5_AUTHORED_TYPED_V1",
                Sv5PatternIntentTag.ShortLedge | Sv5PatternIntentTag.Landing | Sv5PatternIntentTag.RequiredOk,
                ".###", "..#.", "....", "....");
            yield return Seed("SV5_LEDGE_B", "0x00EF", "SV5_AUTHORED_TYPED_V1",
                Sv5PatternIntentTag.LongLedge | Sv5PatternIntentTag.RunApproach | Sv5PatternIntentTag.OptionalOnly,
                "####", ".#..", "....", "....");
            yield return Seed("SV5_ONEWAY_A", "0x0000", "SV5_AUTHORED_TYPED_V1",
                Sv5PatternIntentTag.Takeoff | Sv5PatternIntentTag.Landing | Sv5PatternIntentTag.RequiredOk |
                Sv5PatternIntentTag.RewardAllowed,
                ".==.", "....", "....", "....");
            yield return Seed("SV5_ONEWAY_B", "0x0000", "SV5_AUTHORED_TYPED_V1",
                Sv5PatternIntentTag.ShortLedge | Sv5PatternIntentTag.OneTilePassage | Sv5PatternIntentTag.SecretShell,
                "....", "..=.", ".=..", "....");
            // Same geometry, incompatible authored use.  The catalog preserves
            // both origins and emits a conflict instead of granting a use case.
            yield return Seed("SV5_LEDGE_CONFLICT", "0x0037", "SV5_AUTHORED_TYPED_V1",
                Sv5PatternIntentTag.ShortLedge | Sv5PatternIntentTag.SecretShell,
                ".###", "..#.", "....", "....");
            yield return Seed("SV5_COMPLEX_A", "0x4EF3", "SV5_AUTHORED_TYPED_V1",
                Sv5PatternIntentTag.StepFloor | Sv5PatternIntentTag.GrabbableCorner |
                Sv5PatternIntentTag.HazardAllowed | Sv5PatternIntentTag.MechanismAllowed,
                "##..", ".##.", "...#", "..##");
        }

        private static Sv5PatternSource Seed(string id, string mask, string reference,
            Sv5PatternIntentTag tags, params string[] rowsBottomToTop)
        {
            if (rowsBottomToTop == null || rowsBottomToTop.Length != Height || rowsBottomToTop.Any(row => row == null || row.Length != Width))
                throw new ArgumentException("Seed rows must be four four-cell rows.");
            return new Sv5PatternSource(id, mask, reference, tags, rowsBottomToTop.SelectMany(row => row.Select(ParseToken)));
        }

        private static bool IsVerticalPassage(IReadOnlyList<Sv5PatternBaseCell> cells) =>
            IsFullColumn(cells, 0) && IsFullColumn(cells, Width - 1) &&
            Enumerable.Range(0, Height).All(y => Cell(cells, 1, y) == Sv5PatternBaseCell.Air &&
                Cell(cells, 2, y) == Sv5PatternBaseCell.Air);

        private static bool IsFullColumn(IReadOnlyList<Sv5PatternBaseCell> cells, int x) =>
            Enumerable.Range(0, Height).All(y => Cell(cells, x, y) == Sv5PatternBaseCell.Solid);

        private static int SlopeDirection(IReadOnlyList<Sv5PatternBaseCell> cells)
        {
            int[] heights = Enumerable.Range(0, Width).Select(x => SolidHeight(cells, x)).ToArray();
            bool rising = heights.Zip(heights.Skip(1), (left, right) => right >= left).All(value => value) &&
                heights.Distinct().Count() >= 3;
            bool falling = heights.Zip(heights.Skip(1), (left, right) => right <= left).All(value => value) &&
                heights.Distinct().Count() >= 3;
            return rising ? 1 : falling ? -1 : 0;
        }

        private static int SolidHeight(IReadOnlyList<Sv5PatternBaseCell> cells, int x)
        {
            var height = 0;
            while (height < Height && Cell(cells, x, height) == Sv5PatternBaseCell.Solid) height++;
            return height;
        }

        private static bool IsFlatCeiling(IReadOnlyList<Sv5PatternBaseCell> cells) =>
            Enumerable.Range(0, Width).All(x => Cell(cells, x, Height - 1) == Sv5PatternBaseCell.Solid) &&
            Enumerable.Range(0, Width).All(x => Cell(cells, x, Height - 2) == Sv5PatternBaseCell.Air);

        private static int TopCount(IReadOnlyList<Sv5PatternBaseCell> cells) =>
            Enumerable.Range(0, Width).Count(x => Cell(cells, x, Height - 1) == Sv5PatternBaseCell.Solid);

        private static int BottomCount(IReadOnlyList<Sv5PatternBaseCell> cells) =>
            Enumerable.Range(0, Width).Count(x => Cell(cells, x, 0) == Sv5PatternBaseCell.Solid);

        private static IEnumerable<Sv5PatternStandableSpan> FindStandableSpans(IReadOnlyList<Sv5PatternBaseCell> cells)
        {
            for (var y = 0; y < Height; y++)
            {
                var start = -1;
                for (var x = 0; x <= Width; x++)
                {
                    bool standable = x < Width && IsStandable(Cell(cells, x, y)) &&
                        (y == Height - 1 || Cell(cells, x, y + 1) == Sv5PatternBaseCell.Air);
                    if (standable && start < 0) start = x;
                    if ((!standable || x == Width) && start >= 0)
                    {
                        int end = x - 1;
                        int headroom = 0;
                        while (y + 1 + headroom < Height && Enumerable.Range(start, end - start + 1)
                            .All(column => Cell(cells, column, y + 1 + headroom) == Sv5PatternBaseCell.Air)) headroom++;
                        yield return new Sv5PatternStandableSpan(y, start, end, headroom,
                            y + 1 + headroom >= Height);
                        start = -1;
                    }
                }
            }
        }

        private static bool HasExposedSolidCorner(IReadOnlyList<Sv5PatternBaseCell> cells, int x, int y)
        {
            bool leftAir = x == 0 || Cell(cells, x - 1, y) == Sv5PatternBaseCell.Air;
            bool rightAir = x == Width - 1 || Cell(cells, x + 1, y) == Sv5PatternBaseCell.Air;
            bool aboveAir = y == Height - 1 || Cell(cells, x, y + 1) == Sv5PatternBaseCell.Air;
            return aboveAir && (leftAir || rightAir);
        }

        private static IEnumerable<Sv5PatternAirComponent> FindAirComponents(IReadOnlyList<Sv5PatternBaseCell> cells)
        {
            var remaining = new HashSet<Sv5PatternLocalCell>(Enumerable.Range(0, Height).SelectMany(y =>
                Enumerable.Range(0, Width).Where(x => Cell(cells, x, y) == Sv5PatternBaseCell.Air)
                .Select(x => new Sv5PatternLocalCell(x, y))));
            while (remaining.Count > 0)
            {
                var queue = new Queue<Sv5PatternLocalCell>();
                var component = new List<Sv5PatternLocalCell>();
                Sv5PatternLocalCell first = remaining.OrderBy(value => value.Y).ThenBy(value => value.X).First();
                queue.Enqueue(first);
                remaining.Remove(first);
                while (queue.Count > 0)
                {
                    Sv5PatternLocalCell current = queue.Dequeue();
                    component.Add(current);
                    foreach (Sv5PatternLocalCell next in Neighbors(current).Where(remaining.Contains).ToArray())
                    {
                        remaining.Remove(next);
                        queue.Enqueue(next);
                    }
                }
                yield return new Sv5PatternAirComponent(component);
            }
        }

        private static IEnumerable<Sv5PatternLocalCell> Neighbors(Sv5PatternLocalCell cell)
        {
            foreach (var offset in new[] { new Sv5PatternLocalCell(-1, 0), new Sv5PatternLocalCell(1, 0),
                         new Sv5PatternLocalCell(0, -1), new Sv5PatternLocalCell(0, 1) })
            {
                int x = cell.X + offset.X;
                int y = cell.Y + offset.Y;
                if (x >= 0 && x < Width && y >= 0 && y < Height) yield return new Sv5PatternLocalCell(x, y);
            }
        }

        private static IEnumerable<Sv5PatternLocalCell> EdgeCoordinates(Sv5PatternEdge edge)
        {
            switch (edge)
            {
                case Sv5PatternEdge.Left: return Enumerable.Range(0, Height).Select(y => new Sv5PatternLocalCell(0, y));
                case Sv5PatternEdge.Right: return Enumerable.Range(0, Height).Select(y => new Sv5PatternLocalCell(Width - 1, y));
                case Sv5PatternEdge.Up: return Enumerable.Range(0, Width).Select(x => new Sv5PatternLocalCell(x, Height - 1));
                case Sv5PatternEdge.Down: return Enumerable.Range(0, Width).Select(x => new Sv5PatternLocalCell(x, 0));
                default: throw new ArgumentOutOfRangeException(nameof(edge));
            }
        }

        private static void TransformTags(Sv5PatternIntentTag source, Sv5PatternTransform transform,
            out Sv5PatternIntentTag result, out IReadOnlyList<string> unresolved)
        {
            result = source;
            var review = new List<string>();
            bool flipX = transform == Sv5PatternTransform.MirrorX || transform == Sv5PatternTransform.R180;
            bool flipY = transform == Sv5PatternTransform.MirrorY || transform == Sv5PatternTransform.R180;
            if (flipX)
            {
                RemoveUnrepresentable(ref result, Sv5PatternIntentTag.LeftEntry, "RIGHT_ENTRY_UNREPRESENTABLE", review);
                RemoveUnrepresentable(ref result, Sv5PatternIntentTag.RightExit, "LEFT_EXIT_UNREPRESENTABLE", review);
            }
            if (flipY)
            {
                RemoveUnrepresentable(ref result, Sv5PatternIntentTag.UpEntry, "DOWN_ENTRY_UNREPRESENTABLE", review);
                RemoveUnrepresentable(ref result, Sv5PatternIntentTag.DownExit, "UP_EXIT_UNREPRESENTABLE", review);
            }
            unresolved = review;
        }

        private static void RemoveUnrepresentable(ref Sv5PatternIntentTag tags, Sv5PatternIntentTag tag,
            string reason, ICollection<string> review)
        {
            if ((tags & tag) == 0) return;
            tags &= ~tag;
            review.Add(reason);
        }

        private static bool IsStandable(Sv5PatternBaseCell value) =>
            value == Sv5PatternBaseCell.Solid || value == Sv5PatternBaseCell.OneWayPlatform;

        private static Sv5PatternBaseCell Cell(IReadOnlyList<Sv5PatternBaseCell> cells, int x, int y) =>
            cells[y * Width + x];

        private static void EnsureCells(IReadOnlyList<Sv5PatternBaseCell> cells)
        {
            if (cells == null || cells.Count != CellCount || cells.Any(value => value < Sv5PatternBaseCell.Air ||
                value > Sv5PatternBaseCell.OneWayPlatform))
                throw new ArgumentException("Base geometry requires exactly sixteen AIR/SOLID/ONE_WAY_PLATFORM cells.");
        }

        private static bool SameCells(IReadOnlyList<Sv5PatternBaseCell> left, IReadOnlyList<Sv5PatternBaseCell> right) =>
            left.SequenceEqual(right);

        private static char ToToken(Sv5PatternBaseCell value)
        {
            switch (value)
            {
                case Sv5PatternBaseCell.Air: return 'A';
                case Sv5PatternBaseCell.Solid: return 'S';
                case Sv5PatternBaseCell.OneWayPlatform: return 'O';
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        private static Sv5PatternBaseCell ParseToken(char value)
        {
            switch (value)
            {
                case '.': case 'A': return Sv5PatternBaseCell.Air;
                case '#': case 'S': return Sv5PatternBaseCell.Solid;
                case '=': case 'O': return Sv5PatternBaseCell.OneWayPlatform;
                default: throw new FormatException("Unknown base cell token: " + value);
            }
        }

        private static string Tags(Sv5PatternIntentTag tags) => tags == Sv5PatternIntentTag.None
            ? string.Empty
            : string.Join(";", Enum.GetValues(typeof(Sv5PatternIntentTag)).Cast<Sv5PatternIntentTag>()
                .Where(value => value != Sv5PatternIntentTag.None && (tags & value) == value));

        private static string Cells(IEnumerable<Sv5PatternLocalCell> cells) => string.Join(";", (cells ??
            Array.Empty<Sv5PatternLocalCell>()).OrderBy(value => value.Y).ThenBy(value => value.X));

        private static string Csv(params string[] fields) => string.Join(",", fields.Select(field => "\"" +
            (field ?? string.Empty).Replace("\"", "\"\"") + "\""));

        private static string[] SplitCsv(string row)
        {
            var values = new List<string>();
            var value = new StringBuilder();
            bool quoted = false;
            for (var index = 0; index < row.Length; index++)
            {
                char current = row[index];
                if (current == '"' && quoted && index + 1 < row.Length && row[index + 1] == '"')
                {
                    value.Append('"');
                    index++;
                }
                else if (current == '"') quoted = !quoted;
                else if (current == ',' && !quoted) { values.Add(value.ToString()); value.Clear(); }
                else value.Append(current);
            }
            if (quoted) throw new FormatException("Unclosed CSV quote.");
            values.Add(value.ToString());
            return values.ToArray();
        }

        private sealed class PendingCandidate
        {
            public PendingCandidate(IReadOnlyList<Sv5PatternBaseCell> cells)
            {
                Cells = cells;
                Origins = new List<Sv5PatternOrigin>();
            }

            public IReadOnlyList<Sv5PatternBaseCell> Cells { get; }
            public List<Sv5PatternOrigin> Origins { get; }

            public Sv5PatternCandidate ToCandidate()
            {
                var tags = Origins.Aggregate(Sv5PatternIntentTag.None, (current, value) => current | value.IntentTags);
                var conflicts = new List<string>();
                bool required = Origins.Any(value => (value.IntentTags & Sv5PatternIntentTag.RequiredOk) != 0);
                bool secret = Origins.Any(value => (value.IntentTags & Sv5PatternIntentTag.SecretShell) != 0);
                if (required && secret) conflicts.Add("REQUIRED_OK_VS_SECRET_SHELL_REVIEW_REQUIRED");
                string id = "SV5_" + Hash(SerializeBaseCells16(Cells)).Substring(0, 12).ToUpperInvariant();
                return new Sv5PatternCandidate(id, Cells, Classify(Cells), tags, Origins, conflicts,
                    CalculateCharacteristics(Cells));
            }
        }

        private static string Hash(string value)
        {
            using (SHA256 sha = SHA256.Create())
            {
                return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(value)).Select(byteValue =>
                    byteValue.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }
    }
}
