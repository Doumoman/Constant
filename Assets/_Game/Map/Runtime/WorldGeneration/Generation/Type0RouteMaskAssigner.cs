using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Data;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class Type0RouteMaskAssigner
    {
        public Type0RouteMaskAssignmentResult Assign(
            OptionalRegionGrowthResult growth,
            WorldRouteDefinitionSet definitionSet)
        {
            if (definitionSet == null)
            {
                return InvalidInput(growth, "NULL_DEFINITION_SET", "World route definition set cannot be null.");
            }

            return Assign(growth, definitionSet.RouteMasks.Values);
        }

        public Type0RouteMaskAssignmentResult Assign(
            OptionalRegionGrowthResult growth,
            IEnumerable<SectorRouteMaskDefinition> routeMasks)
        {
            var inputErrors = ValidateGrowth(growth);
            if (routeMasks == null)
            {
                inputErrors.Add(Error("NULL_ROUTE_MASKS", "Route mask definitions cannot be null."));
            }

            if (inputErrors.Count > 0)
            {
                return Failure(
                    Type0RouteMaskAssignmentStatus.InvalidInput,
                    growth == null ? null : growth.Snapshot,
                    Array.Empty<Type0RouteMaskRecord>(),
                    EmptyDiagnostics(growth),
                    inputErrors,
                    growth == null ? string.Empty : growth.CanonicalDigest,
                    string.Empty);
            }

            List<SectorRouteMaskDefinition> definitions;
            try
            {
                definitions = new List<SectorRouteMaskDefinition>(routeMasks);
            }
            catch (Exception exception)
            {
                return Failure(
                    Type0RouteMaskAssignmentStatus.InvalidInput,
                    growth.Snapshot,
                    Array.Empty<Type0RouteMaskRecord>(),
                    EmptyDiagnostics(growth),
                    new[] { Error("ROUTE_MASK_ENUMERATION_FAILED", "Route mask enumeration failed: " + exception.GetType().Name + ".") },
                    growth.CanonicalDigest,
                    string.Empty);
            }

            var catalog = BuildCatalog(definitions);
            if (catalog.Errors.Count > 0)
            {
                var catalogDiagnostics = CreateDiagnostics(
                    definitions.Count, 0, catalog.IgnoredNonType0DefinitionCount,
                    growth.Snapshot.Regions.Count, growth.Snapshot.Cells.Count,
                    0, 0, 0, 0, 0, 0, 0);
                return Failure(
                    Type0RouteMaskAssignmentStatus.InvalidCatalog,
                    growth.Snapshot,
                    Array.Empty<Type0RouteMaskRecord>(),
                    catalogDiagnostics,
                    catalog.Errors,
                    growth.CanonicalDigest,
                    string.Empty);
            }

            var assignmentErrors = new List<Type0RouteMaskAssignmentError>();
            var staged = new List<Type0RouteMaskAssignment>();
            var cellsByIndex = new Dictionary<int, OptionalRegionCell>();
            foreach (var cell in growth.Snapshot.Cells)
                cellsByIndex.Add(cell.SectorIndex, cell);
            var mandatory = new HashSet<int>(growth.Snapshot.MandatoryRouteSectorIndices);
            var masksByShape = catalog.Records.ToDictionary(value => value.OpenMask, value => value);

            var internalEdges = 0;
            var attachmentClosed = 0;
            var mandatoryBaseOpen = 0;
            var closedCrossRegion = 0;
            var horizontalThrough = 0;
            var unsupported = 0;

            foreach (var cell in growth.Snapshot.Cells)
            {
                var left = SameRegion(cell, WorldGridIndex.GetLeftIndex(cell.SectorIndex), cellsByIndex);
                var right = SameRegion(cell, WorldGridIndex.GetRightIndex(cell.SectorIndex), cellsByIndex);
                var up = SameRegion(cell, WorldGridIndex.GetUpIndex(cell.SectorIndex), cellsByIndex);
                var down = SameRegion(cell, WorldGridIndex.GetDownIndex(cell.SectorIndex), cellsByIndex);

                if (right) internalEdges++;
                if (up) internalEdges++;
                if (left && right)
                {
                    horizontalThrough++;
                    unsupported++;
                    assignmentErrors.Add(new Type0RouteMaskAssignmentError(
                        "HORIZONTAL_THROUGH_UNSUPPORTED", cell.RegionId, cell.SectorIndex,
                        default(Type0RouteMaskId), "Required Type0 shape opens left and right simultaneously."));
                    continue;
                }

                var required = new Type0RouteOpenMask(left, right, up, down);
                if (!masksByShape.TryGetValue(required, out var mask))
                {
                    unsupported++;
                    assignmentErrors.Add(new Type0RouteMaskAssignmentError(
                        "REQUIRED_MASK_NOT_REGISTERED", cell.RegionId, cell.SectorIndex,
                        default(Type0RouteMaskId), "Required Type0 shape has no exact registered mask."));
                    continue;
                }

                staged.Add(new Type0RouteMaskAssignment(cell, mask));
            }

            foreach (var cell in growth.Snapshot.Cells)
            {
                CountCrossRegion(cell, WorldGridIndex.GetRightIndex(cell.SectorIndex), cellsByIndex, ref closedCrossRegion);
                CountCrossRegion(cell, WorldGridIndex.GetUpIndex(cell.SectorIndex), cellsByIndex, ref closedCrossRegion);
            }

            if (assignmentErrors.Count == 0)
            {
                var stagedBySector = staged.ToDictionary(value => value.SectorIndex, value => value);
                foreach (var region in growth.Snapshot.Regions)
                {
                    var attachment = region.Attachment;
                    var attachmentAssignment = stagedBySector[attachment.EntrySectorIndex];
                    var towardMandatoryDx = -attachment.EntrySideFromMandatoryDx;
                    var towardMandatoryDy = -attachment.EntrySideFromMandatoryDy;
                    if (IsOpen(attachmentAssignment.OpenMask, towardMandatoryDx, towardMandatoryDy))
                        mandatoryBaseOpen++;
                    else
                        attachmentClosed++;
                }

                foreach (var assignment in staged)
                {
                    foreach (var side in CreateSides())
                    {
                        var neighbor = GetNeighbor(assignment.SectorIndex, side.Dx, side.Dy);
                        if (neighbor >= 0 && mandatory.Contains(neighbor) && IsOpen(assignment.OpenMask, side.Dx, side.Dy))
                            mandatoryBaseOpen++;
                    }
                }
            }

            var diagnostics = CreateDiagnostics(
                definitions.Count, catalog.Records.Count, catalog.IgnoredNonType0DefinitionCount,
                growth.Snapshot.Regions.Count, growth.Snapshot.Cells.Count,
                assignmentErrors.Count == 0 ? staged.Count : 0,
                internalEdges, attachmentClosed, mandatoryBaseOpen, closedCrossRegion,
                horizontalThrough, unsupported);

            if (assignmentErrors.Count > 0)
            {
                return Failure(
                    Type0RouteMaskAssignmentStatus.UnsupportedTopology,
                    growth.Snapshot,
                    catalog.Records,
                    diagnostics,
                    assignmentErrors,
                    growth.CanonicalDigest,
                    catalog.Digest);
            }

            staged.Sort(CompareAssignments);
            var canonicalDigest = ComputeAssignmentDigest(
                growth.CanonicalDigest, catalog.Digest, staged, diagnostics);
            return new Type0RouteMaskAssignmentResult(
                Type0RouteMaskAssignmentStatus.Completed,
                growth.Snapshot,
                catalog.Records,
                staged,
                diagnostics,
                Array.Empty<Type0RouteMaskAssignmentError>(),
                growth.CanonicalDigest,
                catalog.Digest,
                canonicalDigest);
        }

        private static CatalogBuild BuildCatalog(IReadOnlyList<SectorRouteMaskDefinition> definitions)
        {
            var expected = CreateExpectedMasks();
            var expectedById = expected.ToDictionary(value => value.Id, value => value, StringComparer.Ordinal);
            var seenExpected = new HashSet<string>(StringComparer.Ordinal);
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            var recordsById = new Dictionary<string, Type0RouteMaskRecord>(StringComparer.Ordinal);
            var recordsByShape = new Dictionary<Type0RouteOpenMask, Type0RouteMaskRecord>();
            var errors = new List<Type0RouteMaskAssignmentError>();
            var ignored = 0;

            foreach (var definition in definitions.OrderBy(
                         value => value == null ? string.Empty : value.RouteMaskId,
                         StringComparer.Ordinal))
            {
                if (definition == null)
                {
                    errors.Add(Error("NULL_ROUTE_MASK_DEFINITION", "Route mask definitions cannot contain null."));
                    continue;
                }

                var idText = definition.RouteMaskId;
                var expectedMask = default(ExpectedMask);
                var isExpected = idText != null && expectedById.TryGetValue(idText, out expectedMask);
                var hasType0Prefix = idText != null && idText.StartsWith("ROUTE_T0_", StringComparison.Ordinal);
                var isType0Candidate = definition.RouteType == 0 || isExpected || hasType0Prefix;
                if (!isType0Candidate)
                {
                    ignored++;
                    continue;
                }

                if (!seenIds.Add(idText ?? string.Empty))
                {
                    errors.Add(CatalogError("DUPLICATE_MASK_ID", idText, "Type0 route mask ID occurs more than once."));
                    continue;
                }

                if (!isExpected)
                {
                    if (definition.Active || hasType0Prefix)
                    {
                        errors.Add(CatalogError(
                            "UNEXPECTED_ACTIVE_TYPE0_ID", idText,
                            "Active Type0 definitions must use one of the exact 12 registered IDs."));
                    }
                    continue;
                }

                seenExpected.Add(idText);
                var valid = true;
                if (definition.RouteType != 0)
                {
                    errors.Add(CatalogError("WRONG_ROUTE_TYPE", idText, "Required Type0 definition must have route_type=0."));
                    valid = false;
                }
                if (!definition.Active)
                {
                    errors.Add(CatalogError("REQUIRED_MASK_INACTIVE", idText, "Required Type0 definition must be active."));
                    valid = false;
                }
                if (definition.MandatoryAllowed)
                {
                    errors.Add(CatalogError("MANDATORY_ALLOWED_FORBIDDEN", idText, "Type0 definitions cannot allow mandatory routing."));
                    valid = false;
                }
                if (definition.OpenL && definition.OpenR)
                {
                    errors.Add(CatalogError("HORIZONTAL_THROUGH_FORBIDDEN", idText, "Type0 definitions cannot open left and right simultaneously."));
                    valid = false;
                }

                if (definition.OpenL != expectedMask.Left ||
                    definition.OpenR != expectedMask.Right ||
                    definition.OpenU != expectedMask.Up ||
                    definition.OpenD != expectedMask.Down)
                {
                    errors.Add(CatalogError("WRONG_REGISTERED_SHAPE", idText, "Required Type0 definition shape does not match its registered ID."));
                    valid = false;
                }

                if (!Type0RouteMaskId.TryCreate(idText, out var maskId))
                {
                    errors.Add(CatalogError("INVALID_TYPE0_MASK_ID", idText, "Type0 route mask ID grammar is invalid."));
                    valid = false;
                }

                if (!valid) continue;
                var shape = new Type0RouteOpenMask(
                    definition.OpenL, definition.OpenR, definition.OpenU, definition.OpenD);
                var record = new Type0RouteMaskRecord(
                    maskId, definition.RouteType, shape, definition.MandatoryAllowed,
                    definition.Active, definition.DescriptionKo, definition);
                if (recordsByShape.TryGetValue(shape, out var duplicate))
                {
                    errors.Add(CatalogError(
                        "DUPLICATE_OPEN_MASK", idText,
                        "Type0 open mask duplicates " + duplicate.MaskId.Value + "."));
                    continue;
                }

                recordsByShape.Add(shape, record);
                recordsById.Add(idText, record);
            }

            foreach (var required in expected)
            {
                if (!seenExpected.Contains(required.Id))
                {
                    errors.Add(CatalogError(
                        "MISSING_REQUIRED_MASK", required.Id,
                        "Required Type0 route mask definition is missing."));
                }
            }

            if (errors.Count > 0)
                return new CatalogBuild(Array.Empty<Type0RouteMaskRecord>(), ignored, string.Empty, errors);

            var records = expected.Select(value => recordsById[value.Id]).ToList();
            return new CatalogBuild(records, ignored, ComputeCatalogDigest(records), errors);
        }

        private static List<Type0RouteMaskAssignmentError> ValidateGrowth(OptionalRegionGrowthResult growth)
        {
            var errors = new List<Type0RouteMaskAssignmentError>();
            if (growth == null)
            {
                errors.Add(Error("NULL_GROWTH_RESULT", "Optional region growth result cannot be null."));
                return errors;
            }

            var snapshot = growth.Snapshot;
            if (snapshot == null)
            {
                errors.Add(Error("NULL_GROWTH_SNAPSHOT", "Optional region growth snapshot cannot be null."));
                return errors;
            }

            if (!IsLowerHexDigest(growth.CanonicalDigest) ||
                string.IsNullOrWhiteSpace(growth.SourceAttachmentDigest) ||
                string.IsNullOrWhiteSpace(growth.SourceMandatoryGraphDigest))
            {
                errors.Add(Error("INVALID_GROWTH_IDENTITY", "Growth and source digests must be canonical identities."));
            }
            if (growth.RngDrawCount != 0)
                errors.Add(Error("GROWTH_RNG_NOT_ZERO", "Optional region growth must consume zero RNG draws."));
            if (snapshot.SourceMandatoryNodeCount != 47 ||
                snapshot.SourceMandatoryDirectedEdgeCount != 96 ||
                snapshot.SourceMandatoryRouteCellCount != 47)
            {
                errors.Add(Error("MANDATORY_GRAPH_IDENTITY_MISMATCH", "Mandatory graph identity must remain 47/96/47."));
            }
            if (!string.Equals(snapshot.SourceMandatoryGraphDigest, growth.SourceMandatoryGraphDigest, StringComparison.Ordinal))
                errors.Add(Error("MANDATORY_GRAPH_DIGEST_MISMATCH", "Snapshot and growth graph digests must match."));
            if (growth.Diagnostics == null ||
                growth.Diagnostics.AcceptedRegionCount != snapshot.Regions.Count ||
                growth.Diagnostics.AcceptedCellCount != snapshot.Cells.Count)
            {
                errors.Add(Error("GROWTH_ACCOUNTING_MISMATCH", "Growth diagnostics must match snapshot counts."));
            }

            var mandatory = new HashSet<int>(snapshot.MandatoryRouteSectorIndices);
            foreach (var region in snapshot.Regions)
            {
                var bridges = new List<Tuple<int, int>>();
                foreach (var cell in region.Cells)
                {
                    foreach (var side in CreateSides())
                    {
                        var neighbor = GetNeighbor(cell.SectorIndex, side.Dx, side.Dy);
                        if (neighbor >= 0 && mandatory.Contains(neighbor))
                            bridges.Add(Tuple.Create(cell.SectorIndex, neighbor));
                    }
                }
                if (bridges.Count != 1 ||
                    bridges[0].Item1 != region.Attachment.EntrySectorIndex ||
                    bridges[0].Item2 != region.Attachment.MandatoryRouteSectorIndex)
                {
                    errors.Add(new Type0RouteMaskAssignmentError(
                        "INVALID_MANDATORY_BRIDGE", region.RegionId,
                        region.Attachment.EntrySectorIndex, default(Type0RouteMaskId),
                        "Every optional region must preserve exactly one attachment-to-mandatory bridge."));
                }
            }

            return errors;
        }

        private static Type0RouteMaskAssignmentResult InvalidInput(
            OptionalRegionGrowthResult growth,
            string code,
            string message)
        {
            return Failure(
                Type0RouteMaskAssignmentStatus.InvalidInput,
                growth == null ? null : growth.Snapshot,
                Array.Empty<Type0RouteMaskRecord>(),
                EmptyDiagnostics(growth),
                new[] { Error(code, message) },
                growth == null ? string.Empty : growth.CanonicalDigest,
                string.Empty);
        }

        private static Type0RouteMaskAssignmentResult Failure(
            Type0RouteMaskAssignmentStatus status,
            OptionalRegionSnapshot snapshot,
            IEnumerable<Type0RouteMaskRecord> records,
            Type0RouteMaskAssignmentDiagnostics diagnostics,
            IEnumerable<Type0RouteMaskAssignmentError> errors,
            string growthDigest,
            string catalogDigest)
        {
            return new Type0RouteMaskAssignmentResult(
                status, snapshot, records, Array.Empty<Type0RouteMaskAssignment>(), diagnostics,
                errors, growthDigest, catalogDigest, string.Empty);
        }

        private static Type0RouteMaskAssignmentDiagnostics EmptyDiagnostics(OptionalRegionGrowthResult growth)
        {
            var regions = growth == null || growth.Snapshot == null ? 0 : growth.Snapshot.Regions.Count;
            var cells = growth == null || growth.Snapshot == null ? 0 : growth.Snapshot.Cells.Count;
            return CreateDiagnostics(0, 0, 0, regions, cells, 0, 0, 0, 0, 0, 0, 0);
        }

        private static Type0RouteMaskAssignmentDiagnostics CreateDiagnostics(
            int sourceDefinitions,
            int registered,
            int ignored,
            int sourceRegions,
            int sourceCells,
            int assignments,
            int internalEdges,
            int attachmentClosed,
            int mandatoryOpen,
            int crossRegionClosed,
            int horizontalThrough,
            int unsupported)
        {
            return new Type0RouteMaskAssignmentDiagnostics(
                sourceDefinitions, registered, ignored, sourceRegions, sourceCells,
                assignments, internalEdges, attachmentClosed, mandatoryOpen,
                crossRegionClosed, horizontalThrough, unsupported, 0, 0);
        }

        private static bool SameRegion(
            OptionalRegionCell cell,
            int neighborIndex,
            IReadOnlyDictionary<int, OptionalRegionCell> cellsByIndex)
        {
            return neighborIndex >= 0 &&
                   cellsByIndex.TryGetValue(neighborIndex, out var neighbor) &&
                   neighbor.RegionId == cell.RegionId;
        }

        private static void CountCrossRegion(
            OptionalRegionCell cell,
            int neighborIndex,
            IReadOnlyDictionary<int, OptionalRegionCell> cellsByIndex,
            ref int count)
        {
            if (neighborIndex >= 0 &&
                cellsByIndex.TryGetValue(neighborIndex, out var neighbor) &&
                neighbor.RegionId != cell.RegionId)
            {
                count++;
            }
        }

        private static bool IsOpen(Type0RouteOpenMask mask, int dx, int dy)
        {
            if (dx == -1 && dy == 0) return mask.OpenLeft;
            if (dx == 1 && dy == 0) return mask.OpenRight;
            if (dx == 0 && dy == 1) return mask.OpenUp;
            if (dx == 0 && dy == -1) return mask.OpenDown;
            throw new ArgumentException("Direction must be cardinal.");
        }

        private static int GetNeighbor(int sectorIndex, int dx, int dy)
        {
            if (dx == -1 && dy == 0) return WorldGridIndex.GetLeftIndex(sectorIndex);
            if (dx == 1 && dy == 0) return WorldGridIndex.GetRightIndex(sectorIndex);
            if (dx == 0 && dy == 1) return WorldGridIndex.GetUpIndex(sectorIndex);
            if (dx == 0 && dy == -1) return WorldGridIndex.GetDownIndex(sectorIndex);
            throw new ArgumentException("Direction must be cardinal.");
        }

        private static Side[] CreateSides()
        {
            return new[]
            {
                new Side(-1, 0), new Side(1, 0), new Side(0, 1), new Side(0, -1)
            };
        }

        private static List<ExpectedMask> CreateExpectedMasks()
        {
            return new List<ExpectedMask>
            {
                new ExpectedMask("ROUTE_T0_NONE", false, false, false, false),
                new ExpectedMask("ROUTE_T0_L", true, false, false, false),
                new ExpectedMask("ROUTE_T0_R", false, true, false, false),
                new ExpectedMask("ROUTE_T0_U", false, false, true, false),
                new ExpectedMask("ROUTE_T0_D", false, false, false, true),
                new ExpectedMask("ROUTE_T0_LU", true, false, true, false),
                new ExpectedMask("ROUTE_T0_LD", true, false, false, true),
                new ExpectedMask("ROUTE_T0_RU", false, true, true, false),
                new ExpectedMask("ROUTE_T0_RD", false, true, false, true),
                new ExpectedMask("ROUTE_T0_UD", false, false, true, true),
                new ExpectedMask("ROUTE_T0_LUD", true, false, true, true),
                new ExpectedMask("ROUTE_T0_RUD", false, true, true, true)
            };
        }

        private static string ComputeCatalogDigest(IReadOnlyList<Type0RouteMaskRecord> records)
        {
            var text = new StringBuilder();
            foreach (var record in records)
            {
                text.Append(record.MaskId.Value).Append('|')
                    .Append(record.RouteType.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(Bits(record.OpenMask)).Append('|')
                    .Append(record.MandatoryAllowed ? '1' : '0').Append('|')
                    .Append(record.Active ? '1' : '0').Append('\n');
            }
            return Sha256(text.ToString());
        }

        private static string ComputeAssignmentDigest(
            string growthDigest,
            string catalogDigest,
            IReadOnlyList<Type0RouteMaskAssignment> assignments,
            Type0RouteMaskAssignmentDiagnostics diagnostics)
        {
            var text = new StringBuilder();
            text.Append("S|").Append(growthDigest).Append('|').Append(catalogDigest).Append('\n');
            foreach (var assignment in assignments)
            {
                text.Append("A|").Append(assignment.RegionId.Value).Append('|')
                    .Append(assignment.SectorIndex.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(assignment.Depth.Value.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(assignment.IsAttachmentCell ? '1' : '0').Append('|')
                    .Append(assignment.MaskId.Value).Append('|')
                    .Append(Bits(assignment.OpenMask)).Append('\n');
            }
            text.Append("D|")
                .Append(Invariant(diagnostics.SourceRouteMaskDefinitionCount)).Append('|')
                .Append(Invariant(diagnostics.RegisteredType0MaskCount)).Append('|')
                .Append(Invariant(diagnostics.IgnoredNonType0DefinitionCount)).Append('|')
                .Append(Invariant(diagnostics.SourceRegionCount)).Append('|')
                .Append(Invariant(diagnostics.SourceCellCount)).Append('|')
                .Append(Invariant(diagnostics.AssignmentCount)).Append('|')
                .Append(Invariant(diagnostics.InternalUndirectedEdgeCount)).Append('|')
                .Append(Invariant(diagnostics.AttachmentBoundaryClosedCount)).Append('|')
                .Append(Invariant(diagnostics.MandatoryBoundaryBaseOpenCount)).Append('|')
                .Append(Invariant(diagnostics.ClosedCrossRegionAdjacencyCount)).Append('|')
                .Append(Invariant(diagnostics.HorizontalThroughCount)).Append('|')
                .Append(Invariant(diagnostics.UnsupportedRequiredMaskCount)).Append('|')
                .Append(Invariant(diagnostics.RngDrawCount)).Append('|')
                .Append(Invariant(diagnostics.SourceMutationCount)).Append('\n');
            return Sha256(text.ToString());
        }

        private static string Bits(Type0RouteOpenMask mask)
        {
            return string.Concat(
                mask.OpenLeft ? "1" : "0",
                mask.OpenRight ? "1" : "0",
                mask.OpenUp ? "1" : "0",
                mask.OpenDown ? "1" : "0");
        }

        private static string Invariant(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string Sha256(string value)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
                var result = new StringBuilder(64);
                foreach (var item in hash)
                    result.Append(item.ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }

        private static bool IsLowerHexDigest(string value)
        {
            if (value == null || value.Length != 64) return false;
            foreach (var character in value)
            {
                if ((character < '0' || character > '9') &&
                    (character < 'a' || character > 'f')) return false;
            }
            return true;
        }

        private static int CompareAssignments(Type0RouteMaskAssignment left, Type0RouteMaskAssignment right)
        {
            var region = left.RegionId.CompareTo(right.RegionId);
            return region != 0 ? region : left.SectorIndex.CompareTo(right.SectorIndex);
        }

        private static Type0RouteMaskAssignmentError Error(string code, string message)
        {
            return new Type0RouteMaskAssignmentError(
                code, default(OptionalRegionId), -1, default(Type0RouteMaskId), message);
        }

        private static Type0RouteMaskAssignmentError CatalogError(string code, string id, string message)
        {
            Type0RouteMaskId.TryCreate(id, out var maskId);
            return new Type0RouteMaskAssignmentError(
                code, default(OptionalRegionId), -1, maskId, message);
        }

        private readonly struct ExpectedMask
        {
            public ExpectedMask(string id, bool left, bool right, bool up, bool down)
            {
                Id = id; Left = left; Right = right; Up = up; Down = down;
            }
            public string Id { get; }
            public bool Left { get; }
            public bool Right { get; }
            public bool Up { get; }
            public bool Down { get; }
        }

        private readonly struct Side
        {
            public Side(int dx, int dy) { Dx = dx; Dy = dy; }
            public int Dx { get; }
            public int Dy { get; }
        }

        private sealed class CatalogBuild
        {
            public CatalogBuild(
                IEnumerable<Type0RouteMaskRecord> records,
                int ignoredNonType0DefinitionCount,
                string digest,
                IEnumerable<Type0RouteMaskAssignmentError> errors)
            {
                Records = new List<Type0RouteMaskRecord>(records);
                IgnoredNonType0DefinitionCount = ignoredNonType0DefinitionCount;
                Digest = digest;
                Errors = new List<Type0RouteMaskAssignmentError>(errors);
            }
            public IReadOnlyList<Type0RouteMaskRecord> Records { get; }
            public int IgnoredNonType0DefinitionCount { get; }
            public string Digest { get; }
            public IReadOnlyList<Type0RouteMaskAssignmentError> Errors { get; }
        }
    }
}
