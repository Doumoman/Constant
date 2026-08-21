using System;
using System.Collections.Generic;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class FootprintPlacementSolver
    {
        public FootprintPlacementResult SolveStart(
            SiteOriginCandidate candidate,
            FootprintPlacementBlockers blockers)
        {
            var errors = new List<FootprintPlacementError>();
            if (candidate == null)
            {
                Add(errors, FootprintPlacementErrorCode.MissingCandidate, string.Empty, string.Empty, -1,
                    "A site origin candidate is required.");
            }
            else if (candidate.Kind != SiteReservationKind.Start)
            {
                Add(errors, FootprintPlacementErrorCode.InvalidCandidate,
                    CanonicalOrEmpty(candidate.SourceDefinitionId), string.Empty, candidate.OriginIndex,
                    "SolveStart requires a Start candidate.");
            }

            if (blockers == null)
            {
                Add(errors, FootprintPlacementErrorCode.MissingBlockers, Source(candidate), string.Empty, -1,
                    "Placement blockers are required.");
            }

            if (errors.Count != 0) return FootprintPlacementResult.Failure(errors);

            var footprint = new SiteFootprint(
                1,
                1,
                SiteFootprintTransform.R0,
                new[]
                {
                    new SiteFootprintCell(
                        0,
                        0,
                        "START",
                        string.Empty,
                        string.Empty,
                        Array.Empty<SiteEntrySide>())
                });
            return Place(candidate, footprint, Array.Empty<PreparedEntry>(), blockers);
        }

        public FootprintPlacementResult SolveSpecialSite(
            SiteOriginCandidate candidate,
            SiteFootprintTransform transform,
            SpecialMapDefinition specialMap,
            IEnumerable<SpecialMapFootprintCellDefinition> footprintCells,
            IEnumerable<SpecialMapEntrySocketDefinition> entrySockets,
            FootprintPlacementBlockers blockers)
        {
            var errors = new List<FootprintPlacementError>();
            var sourceId = Source(candidate, specialMap);

            if (candidate == null)
            {
                Add(errors, FootprintPlacementErrorCode.MissingCandidate, sourceId, string.Empty, -1,
                    "A site origin candidate is required.");
            }
            else if (!IsSpecialKind(candidate.Kind))
            {
                Add(errors, FootprintPlacementErrorCode.InvalidCandidate, sourceId, string.Empty,
                    candidate.OriginIndex, "Special-site placement requires a Boss, Forge, or CoreResource candidate.");
            }

            if (blockers == null)
            {
                Add(errors, FootprintPlacementErrorCode.MissingBlockers, sourceId, string.Empty, -1,
                    "Placement blockers are required.");
            }

            if (!IsDefined(transform))
            {
                Add(errors, FootprintPlacementErrorCode.UnsupportedTransform, sourceId, string.Empty, -1,
                    "Only R0, MirrorX, MirrorY, and R180 transforms are supported.");
            }

            if (specialMap == null)
            {
                Add(errors, FootprintPlacementErrorCode.MissingSpecialMap, sourceId, string.Empty, -1,
                    "A special-map definition is required.");
            }
            else
            {
                if (!IsValidSpecialMap(specialMap, candidate))
                {
                    Add(errors, FootprintPlacementErrorCode.InvalidSpecialMap, sourceId, string.Empty, -1,
                        "Special-map identity, role, dimensions, count, and active state must be valid.");
                }
                if (candidate != null &&
                    !string.Equals(candidate.SourceDefinitionId, specialMap.SpecialMapId, StringComparison.Ordinal))
                {
                    Add(errors, FootprintPlacementErrorCode.SourceIdentityMismatch, sourceId, string.Empty, -1,
                        "Candidate and special-map source identities must match.");
                }
            }

            var cellSnapshot = SnapshotFootprintCells(footprintCells, sourceId, errors);
            var entrySnapshot = SnapshotEntries(entrySockets, sourceId, errors);
            if (specialMap != null)
            {
                ValidateFootprintCells(specialMap, candidate, cellSnapshot, errors);
                ValidateEntries(specialMap, candidate, entrySnapshot, errors);
            }

            if (errors.Count != 0) return FootprintPlacementResult.Failure(errors);

            var transformedCells = new List<SiteFootprintCell>(cellSnapshot.Count);
            foreach (var sourceCell in cellSnapshot)
            {
                SiteFootprintTransformer.TryTransformCoordinate(
                    specialMap.FootprintWidthSectors,
                    specialMap.FootprintHeightSectors,
                    transform,
                    sourceCell.LocalSectorX,
                    sourceCell.LocalSectorY,
                    out var localX,
                    out var localY);

                var requiredSides = new List<SiteEntrySide>();
                foreach (var token in sourceCell.RequiredOpenSides)
                {
                    SiteReservationTokenCodec.TryParseEntrySide(token, out var sourceSide);
                    SiteFootprintTransformer.TryTransformSide(transform, sourceSide, out var transformedSide);
                    requiredSides.Add(transformedSide);
                }

                transformedCells.Add(new SiteFootprintCell(
                    localX,
                    localY,
                    sourceCell.LocalRole,
                    sourceCell.RequiredPrimaryBiomeId,
                    sourceCell.FixedSectorRecipeId,
                    requiredSides));
            }

            var footprint = new SiteFootprint(
                specialMap.FootprintWidthSectors,
                specialMap.FootprintHeightSectors,
                transform,
                transformedCells);
            var preparedEntries = new List<PreparedEntry>(entrySnapshot.Count);
            foreach (var sourceEntry in entrySnapshot)
            {
                SiteFootprintTransformer.TryTransformCoordinate(
                    specialMap.FootprintWidthSectors,
                    specialMap.FootprintHeightSectors,
                    transform,
                    sourceEntry.LocalSectorX,
                    sourceEntry.LocalSectorY,
                    out var localX,
                    out var localY);
                SiteReservationTokenCodec.TryParseEntrySide(sourceEntry.Side, out var sourceSide);
                SiteFootprintTransformer.TryTransformSide(transform, sourceSide, out var side);
                preparedEntries.Add(new PreparedEntry(
                    sourceEntry.EntrySocketId,
                    localX,
                    localY,
                    side,
                    sourceEntry.AllowedRouteTypes,
                    sourceEntry.Required,
                    sourceEntry.ReturnPathRequired));
            }
            preparedEntries.Sort((left, right) =>
                string.Compare(left.EntrySocketId, right.EntrySocketId, StringComparison.Ordinal));

            return Place(candidate, footprint, preparedEntries, blockers);
        }

        private static FootprintPlacementResult Place(
            SiteOriginCandidate candidate,
            SiteFootprint footprint,
            IReadOnlyList<PreparedEntry> preparedEntries,
            FootprintPlacementBlockers blockers)
        {
            var errors = new List<FootprintPlacementError>();
            var sourceId = candidate.SourceDefinitionId;
            var occupied = new List<SectorCoord>(footprint.Cells.Count);
            var occupiedLookup = new HashSet<int>();

            foreach (var cell in footprint.Cells)
            {
                var sector = new SectorCoord(
                    candidate.Origin.X + cell.LocalX,
                    candidate.Origin.Y + cell.LocalY);
                if (!IsWorldSector(sector))
                {
                    Add(errors, FootprintPlacementErrorCode.FootprintOutsideWorld,
                        sourceId, string.Empty, -1, "Transformed footprint extends outside the world.");
                    continue;
                }
                occupied.Add(sector);
                occupiedLookup.Add(WorldGridIndex.ToIndex(sector));
            }
            if (errors.Count != 0) return FootprintPlacementResult.Failure(errors);

            foreach (var sector in occupied)
            {
                var index = WorldGridIndex.ToIndex(sector);
                if (blockers.IsOccupied(index))
                {
                    Add(errors, FootprintPlacementErrorCode.FootprintOverlap,
                        sourceId, string.Empty, index, "Transformed footprint overlaps an occupied sector.");
                }
                if (blockers.IsProtectedEntryApproach(index))
                {
                    Add(errors, FootprintPlacementErrorCode.BlocksExistingEntryApproach,
                        sourceId, string.Empty, index, "Transformed footprint blocks an existing entry approach.");
                }
            }
            if (errors.Count != 0) return FootprintPlacementResult.Failure(errors);

            var placementEntries = new List<FootprintPlacementEntry>(preparedEntries.Count);
            var faces = new HashSet<EntryFace>();
            foreach (var prepared in preparedEntries)
            {
                var footprintSector = new SectorCoord(
                    candidate.Origin.X + prepared.LocalX,
                    candidate.Origin.Y + prepared.LocalY);
                if (!footprint.TryGetCell(prepared.LocalX, prepared.LocalY, out _))
                {
                    Add(errors, FootprintPlacementErrorCode.EntryNotOnFootprint,
                        sourceId, prepared.EntrySocketId, IndexOrNone(footprintSector),
                        "Transformed entry must reference a transformed footprint cell.");
                    continue;
                }

                var footprintIndex = WorldGridIndex.ToIndex(footprintSector);
                if (!faces.Add(new EntryFace(footprintIndex, prepared.Side)))
                {
                    Add(errors, FootprintPlacementErrorCode.DuplicateEntryFace,
                        sourceId, prepared.EntrySocketId, footprintIndex,
                        "Transformed entry faces must be unique.");
                    continue;
                }

                SiteReservationTokenCodec.GetDelta(prepared.Side, out var deltaX, out var deltaY);
                var exterior = new SectorCoord(
                    footprintSector.X + deltaX,
                    footprintSector.Y + deltaY);
                if (!IsWorldSector(exterior))
                {
                    Add(errors, FootprintPlacementErrorCode.EntryOutsideWorld,
                        sourceId, prepared.EntrySocketId, -1,
                        "Transformed entry exterior is outside the world.");
                    continue;
                }

                var exteriorIndex = WorldGridIndex.ToIndex(exterior);
                if (occupiedLookup.Contains(exteriorIndex))
                {
                    Add(errors, FootprintPlacementErrorCode.EntryFacesOwnFootprint,
                        sourceId, prepared.EntrySocketId, exteriorIndex,
                        "Transformed entry faces the candidate's own footprint.");
                    continue;
                }
                if (blockers.IsOccupied(exteriorIndex))
                {
                    Add(errors, FootprintPlacementErrorCode.EntryApproachOccupied,
                        sourceId, prepared.EntrySocketId, exteriorIndex,
                        "Transformed entry approach is occupied.");
                    continue;
                }

                placementEntries.Add(new FootprintPlacementEntry(
                    prepared.EntrySocketId,
                    prepared.LocalX,
                    prepared.LocalY,
                    footprintSector,
                    prepared.Side,
                    exterior,
                    prepared.AllowedRouteTypes,
                    prepared.Required,
                    prepared.ReturnPathRequired));
            }

            if (errors.Count != 0) return FootprintPlacementResult.Failure(errors);
            return FootprintPlacementResult.Success(
                new FootprintPlacement(candidate, footprint, occupied, placementEntries));
        }

        private static List<SpecialMapFootprintCellDefinition> SnapshotFootprintCells(
            IEnumerable<SpecialMapFootprintCellDefinition> source,
            string sourceId,
            ICollection<FootprintPlacementError> errors)
        {
            var result = new List<SpecialMapFootprintCellDefinition>();
            if (source == null)
            {
                Add(errors, FootprintPlacementErrorCode.MissingFootprintCells, sourceId,
                    string.Empty, -1, "Footprint cell definitions are required.");
                return result;
            }
            foreach (var item in source) result.Add(item);
            if (result.Count == 0)
            {
                Add(errors, FootprintPlacementErrorCode.MissingFootprintCells, sourceId,
                    string.Empty, -1, "At least one footprint cell definition is required.");
            }
            return result;
        }

        private static List<SpecialMapEntrySocketDefinition> SnapshotEntries(
            IEnumerable<SpecialMapEntrySocketDefinition> source,
            string sourceId,
            ICollection<FootprintPlacementError> errors)
        {
            var result = new List<SpecialMapEntrySocketDefinition>();
            if (source == null)
            {
                Add(errors, FootprintPlacementErrorCode.MissingEntrySockets, sourceId,
                    string.Empty, -1, "Entry socket definitions are required.");
                return result;
            }
            foreach (var item in source) result.Add(item);
            if (result.Count == 0)
            {
                Add(errors, FootprintPlacementErrorCode.MissingEntrySockets, sourceId,
                    string.Empty, -1, "At least one entry socket definition is required.");
            }
            return result;
        }

        private static void ValidateFootprintCells(
            SpecialMapDefinition specialMap,
            SiteOriginCandidate candidate,
            IReadOnlyList<SpecialMapFootprintCellDefinition> cells,
            ICollection<FootprintPlacementError> errors)
        {
            var seen = new HashSet<CellKey>();
            foreach (var cell in cells)
            {
                if (cell == null)
                {
                    Add(errors, FootprintPlacementErrorCode.NullFootprintCell,
                        Source(candidate, specialMap), string.Empty, -1,
                        "Footprint cell definitions cannot contain null.");
                    continue;
                }

                var sourceId = Source(candidate, specialMap, cell.SpecialMapId);
                if (!seen.Add(new CellKey(cell.LocalSectorX, cell.LocalSectorY)))
                {
                    Add(errors, FootprintPlacementErrorCode.DuplicateFootprintCell,
                        sourceId, string.Empty, -1, "Footprint cell coordinates must be unique.");
                }
                if (!string.Equals(cell.SpecialMapId, specialMap.SpecialMapId, StringComparison.Ordinal) ||
                    (candidate != null &&
                     !string.Equals(cell.SpecialMapId, candidate.SourceDefinitionId, StringComparison.Ordinal)))
                {
                    Add(errors, FootprintPlacementErrorCode.SourceIdentityMismatch,
                        sourceId, string.Empty, -1,
                        "Footprint cell parent identity must match the candidate and special map.");
                }
                if (!IsValidFootprintCell(cell, specialMap))
                {
                    Add(errors, FootprintPlacementErrorCode.InvalidFootprintCell,
                        sourceId, string.Empty, -1,
                        "Footprint cell coordinate, payload, and required sides must be valid.");
                }
            }
        }

        private static void ValidateEntries(
            SpecialMapDefinition specialMap,
            SiteOriginCandidate candidate,
            IReadOnlyList<SpecialMapEntrySocketDefinition> entries,
            ICollection<FootprintPlacementError> errors)
        {
            var socketIds = new HashSet<string>(StringComparer.Ordinal);
            var hasRequired = false;
            foreach (var entry in entries)
            {
                if (entry == null)
                {
                    Add(errors, FootprintPlacementErrorCode.NullEntrySocket,
                        Source(candidate, specialMap), string.Empty, -1,
                        "Entry socket definitions cannot contain null.");
                    continue;
                }

                var sourceId = Source(candidate, specialMap, entry.SpecialMapId);
                var entryId = CanonicalOrEmpty(entry.EntrySocketId);
                if (!socketIds.Add(entry.EntrySocketId ?? string.Empty))
                {
                    Add(errors, FootprintPlacementErrorCode.DuplicateEntrySocketId,
                        sourceId, entryId, -1, "Entry socket IDs must be unique.");
                }
                if (!string.Equals(entry.SpecialMapId, specialMap.SpecialMapId, StringComparison.Ordinal) ||
                    (candidate != null &&
                     !string.Equals(entry.SpecialMapId, candidate.SourceDefinitionId, StringComparison.Ordinal)))
                {
                    Add(errors, FootprintPlacementErrorCode.SourceIdentityMismatch,
                        sourceId, entryId, -1,
                        "Entry socket parent identity must match the candidate and special map.");
                }
                if (!IsValidEntry(entry, specialMap))
                {
                    Add(errors, FootprintPlacementErrorCode.InvalidEntrySocket,
                        sourceId, entryId, -1,
                        "Entry socket coordinate, side, route types, and identity must be valid.");
                }
                if (entry.Required) hasRequired = true;
            }

            if (entries.Count != 0 && !hasRequired)
            {
                Add(errors, FootprintPlacementErrorCode.MissingRequiredEntry,
                    Source(candidate, specialMap), string.Empty, -1,
                    "At least one required entry socket is required.");
            }
        }

        private static bool IsValidSpecialMap(
            SpecialMapDefinition specialMap,
            SiteOriginCandidate candidate)
        {
            if (!ReservationValidation.IsCanonicalId(specialMap.SpecialMapId, false) ||
                !specialMap.Active || specialMap.RequiredCount <= 0 ||
                specialMap.FootprintWidthSectors < 1 ||
                specialMap.FootprintWidthSectors > WorldGenConstants.SectorColumns ||
                specialMap.FootprintHeightSectors < 1 ||
                specialMap.FootprintHeightSectors > WorldGenConstants.SectorRows ||
                !SiteReservationTokenCodec.TryParseKind(specialMap.SiteRole, out var kind) ||
                !IsSpecialKind(kind))
            {
                return false;
            }
            return candidate == null || kind == candidate.Kind;
        }

        private static bool IsValidFootprintCell(
            SpecialMapFootprintCellDefinition cell,
            SpecialMapDefinition specialMap)
        {
            if (cell.LocalSectorX < 0 || cell.LocalSectorX >= specialMap.FootprintWidthSectors ||
                cell.LocalSectorY < 0 || cell.LocalSectorY >= specialMap.FootprintHeightSectors ||
                !ReservationValidation.IsCanonicalId(cell.LocalRole, false) ||
                !ReservationValidation.IsCanonicalId(cell.RequiredPrimaryBiomeId, true) ||
                !ReservationValidation.IsCanonicalId(cell.FixedSectorRecipeId, true) ||
                cell.RequiredOpenSides == null)
            {
                return false;
            }

            var sides = new HashSet<SiteEntrySide>();
            foreach (var token in cell.RequiredOpenSides)
            {
                if (!SiteReservationTokenCodec.TryParseEntrySide(token, out var side) || !sides.Add(side))
                    return false;
            }
            return true;
        }

        private static bool IsValidEntry(
            SpecialMapEntrySocketDefinition entry,
            SpecialMapDefinition specialMap)
        {
            if (!ReservationValidation.IsCanonicalId(entry.EntrySocketId, false) ||
                entry.LocalSectorX < 0 || entry.LocalSectorX >= specialMap.FootprintWidthSectors ||
                entry.LocalSectorY < 0 || entry.LocalSectorY >= specialMap.FootprintHeightSectors ||
                !SiteReservationTokenCodec.TryParseEntrySide(entry.Side, out _) ||
                entry.AllowedRouteTypes == null || entry.AllowedRouteTypes.Count == 0)
            {
                return false;
            }

            var routes = new HashSet<int>();
            foreach (var route in entry.AllowedRouteTypes)
            {
                if (route < 1 || route > 3 || !routes.Add(route)) return false;
            }
            return true;
        }

        private static bool IsSpecialKind(SiteReservationKind kind)
        {
            return kind == SiteReservationKind.Boss || kind == SiteReservationKind.Forge ||
                   kind == SiteReservationKind.CoreResource;
        }

        private static bool IsDefined(SiteFootprintTransform transform)
        {
            return transform == SiteFootprintTransform.R0 ||
                   transform == SiteFootprintTransform.MirrorX ||
                   transform == SiteFootprintTransform.MirrorY ||
                   transform == SiteFootprintTransform.R180;
        }

        private static bool IsWorldSector(SectorCoord sector)
        {
            return sector.X >= 0 && sector.X < WorldGenConstants.SectorColumns &&
                   sector.Y >= 0 && sector.Y < WorldGenConstants.SectorRows;
        }

        private static int IndexOrNone(SectorCoord sector)
        {
            return IsWorldSector(sector) ? WorldGridIndex.ToIndex(sector) : -1;
        }

        private static string Source(SiteOriginCandidate candidate)
        {
            return candidate == null ? string.Empty : CanonicalOrEmpty(candidate.SourceDefinitionId);
        }

        private static string Source(
            SiteOriginCandidate candidate,
            SpecialMapDefinition specialMap,
            string childSource = null)
        {
            if (candidate != null && ReservationValidation.IsCanonicalId(candidate.SourceDefinitionId, false))
                return candidate.SourceDefinitionId;
            if (specialMap != null && ReservationValidation.IsCanonicalId(specialMap.SpecialMapId, false))
                return specialMap.SpecialMapId;
            return CanonicalOrEmpty(childSource);
        }

        private static string CanonicalOrEmpty(string value)
        {
            return ReservationValidation.IsCanonicalId(value, false) ? value : string.Empty;
        }

        private static void Add(
            ICollection<FootprintPlacementError> errors,
            FootprintPlacementErrorCode code,
            string sourceDefinitionId,
            string entrySocketId,
            int sectorIndex,
            string message)
        {
            errors.Add(new FootprintPlacementError(
                code,
                CanonicalOrEmpty(sourceDefinitionId),
                CanonicalOrEmpty(entrySocketId),
                sectorIndex,
                message));
        }

        private sealed class PreparedEntry
        {
            public PreparedEntry(
                string entrySocketId,
                int localX,
                int localY,
                SiteEntrySide side,
                IEnumerable<int> allowedRouteTypes,
                bool required,
                bool returnPathRequired)
            {
                EntrySocketId = entrySocketId;
                LocalX = localX;
                LocalY = localY;
                Side = side;
                AllowedRouteTypes = new List<int>(allowedRouteTypes).AsReadOnly();
                Required = required;
                ReturnPathRequired = returnPathRequired;
            }

            public string EntrySocketId { get; }
            public int LocalX { get; }
            public int LocalY { get; }
            public SiteEntrySide Side { get; }
            public IReadOnlyList<int> AllowedRouteTypes { get; }
            public bool Required { get; }
            public bool ReturnPathRequired { get; }
        }

        private readonly struct CellKey : IEquatable<CellKey>
        {
            private readonly int x;
            private readonly int y;

            public CellKey(int x, int y)
            {
                this.x = x;
                this.y = y;
            }

            public bool Equals(CellKey other) => x == other.x && y == other.y;
            public override bool Equals(object obj) => obj is CellKey other && Equals(other);
            public override int GetHashCode() => (x * 397) ^ y;
        }

        private readonly struct EntryFace : IEquatable<EntryFace>
        {
            private readonly int sectorIndex;
            private readonly SiteEntrySide side;

            public EntryFace(int sectorIndex, SiteEntrySide side)
            {
                this.sectorIndex = sectorIndex;
                this.side = side;
            }

            public bool Equals(EntryFace other) => sectorIndex == other.sectorIndex && side == other.side;
            public override bool Equals(object obj) => obj is EntryFace other && Equals(other);
            public override int GetHashCode() => (sectorIndex * 397) ^ (int)side;
        }
    }
}
