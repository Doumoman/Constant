using System;
using System.Collections.Generic;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public sealed class Microchunk96CellValidator
    {
        public const string MissingCellRecordReason =
            Microchunk96CellValidationViolation.MissingCellRecordReason;
        public const string DuplicateCellCoordinateReason =
            Microchunk96CellValidationViolation.DuplicateCellCoordinateReason;
        public const string CellCoordinateOutOfRangeReason =
            Microchunk96CellValidationViolation.CellCoordinateOutOfRangeReason;

        public Microchunk96CellValidationResult ValidateRecords(
            IEnumerable<Microchunk96CellRecord> records)
        {
            return ValidateRecords(records, Microchunk96CellValidationPolicy.Default);
        }

        public Microchunk96CellValidationResult ValidateRecords(
            IEnumerable<Microchunk96CellRecord> records,
            Microchunk96CellValidationPolicy policy)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));
            if (policy == null) throw new ArgumentNullException(nameof(policy));

            var snapshot = SnapshotRecords(records);
            var ids = snapshot.Select(record => record.MicrochunkId).Distinct().ToList();
            return ValidateSnapshot(snapshot, ids, policy);
        }

        public Microchunk96CellValidationResult ValidateRecords(
            MicrochunkId microchunkId,
            IEnumerable<Microchunk96CellRecord> records,
            Microchunk96CellValidationPolicy policy)
        {
            if (!microchunkId.IsValid) throw new ArgumentException("A valid microchunk ID is required.", nameof(microchunkId));
            if (records == null) throw new ArgumentNullException(nameof(records));
            if (policy == null) throw new ArgumentNullException(nameof(policy));

            var snapshot = SnapshotRecords(records);
            if (snapshot.Any(record => record.MicrochunkId != microchunkId))
            {
                throw new ArgumentException("Every record must belong to the requested microchunk.", nameof(records));
            }

            return ValidateSnapshot(snapshot, new[] { microchunkId }, policy);
        }

        public Microchunk96CellValidationResult ValidateDefinition(MicrochunkDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            return ValidateDefinition(definition, Microchunk96CellValidationPolicy.ForDefinition(definition));
        }

        public Microchunk96CellValidationResult ValidateDefinition(
            MicrochunkDefinition definition,
            Microchunk96CellValidationPolicy policy)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (policy == null) throw new ArgumentNullException(nameof(policy));

            var records = definition.TileCells
                .Select((cell, ordinal) => new Microchunk96CellRecord(
                    definition.Id,
                    ordinal,
                    cell.Coordinate.X,
                    cell.Coordinate.Y,
                    cell))
                .ToList();
            return ValidateSnapshot(records, new[] { definition.Id }, policy);
        }

        private static List<Microchunk96CellRecord> SnapshotRecords(IEnumerable<Microchunk96CellRecord> records)
        {
            var snapshot = new List<Microchunk96CellRecord>();
            foreach (var record in records)
            {
                if (record == null)
                {
                    throw new ArgumentException("Cell records cannot contain null.", nameof(records));
                }

                snapshot.Add(record);
            }

            return snapshot;
        }

        private static Microchunk96CellValidationResult ValidateSnapshot(
            IReadOnlyList<Microchunk96CellRecord> records,
            IEnumerable<MicrochunkId> requestedIds,
            Microchunk96CellValidationPolicy policy)
        {
            var ids = requestedIds.Distinct().OrderBy(id => id).ToList();
            var groups = records
                .GroupBy(record => record.MicrochunkId)
                .ToDictionary(group => group.Key, group => group.ToList());
            var violations = new List<Microchunk96CellValidationViolation>();
            var uniqueCount = 0;
            var missingCount = 0;
            var duplicateCount = 0;
            var outOfRangeCount = 0;
            var rowCountMismatchCount = 0;

            foreach (var id in ids)
            {
                List<Microchunk96CellRecord> group;
                if (!groups.TryGetValue(id, out group))
                {
                    group = new List<Microchunk96CellRecord>();
                }

                var inRange = new Dictionary<MicrochunkLocalCoord, List<Microchunk96CellRecord>>();
                var inRangeRecordCount = 0;
                foreach (var record in group)
                {
                    MicrochunkLocalCoord coordinate;
                    if (!MicrochunkLocalCoord.TryCreate(record.RawLocalX, record.RawLocalY, out coordinate))
                    {
                        outOfRangeCount++;
                        violations.Add(new Microchunk96CellValidationViolation(
                            id,
                            record.SourceOrdinal,
                            record.RawLocalX,
                            record.RawLocalY,
                            null,
                            Microchunk96CellValidationViolation.CellCoordinateOutOfRangeReason));
                        continue;
                    }

                    inRangeRecordCount++;

                    List<Microchunk96CellRecord> coordinateRecords;
                    if (!inRange.TryGetValue(coordinate, out coordinateRecords))
                    {
                        coordinateRecords = new List<Microchunk96CellRecord>();
                        inRange.Add(coordinate, coordinateRecords);
                    }
                    coordinateRecords.Add(record);
                }

                if (group.Count != MicrochunkConstants.CellCount ||
                    inRangeRecordCount != MicrochunkConstants.CellCount)
                {
                    rowCountMismatchCount++;
                }

                uniqueCount += inRange.Count;
                foreach (var pair in inRange.OrderBy(pair => pair.Key))
                {
                    if (pair.Value.Count <= 1)
                    {
                        continue;
                    }

                    var ordered = pair.Value
                        .OrderBy(record => record.SourceOrdinal)
                        .ThenBy(record => record.RawLocalY)
                        .ThenBy(record => record.RawLocalX)
                        .ToList();
                    for (var index = 1; index < ordered.Count; index++)
                    {
                        var duplicate = ordered[index];
                        duplicateCount++;
                        violations.Add(new Microchunk96CellValidationViolation(
                            id,
                            duplicate.SourceOrdinal,
                            duplicate.RawLocalX,
                            duplicate.RawLocalY,
                            pair.Key,
                            Microchunk96CellValidationViolation.DuplicateCellCoordinateReason));
                    }
                }

                for (var y = 0; y < MicrochunkConstants.HeightTiles; y++)
                {
                    for (var x = 0; x < MicrochunkConstants.WidthTiles; x++)
                    {
                        var coordinate = new MicrochunkLocalCoord(x, y);
                        if (inRange.ContainsKey(coordinate))
                        {
                            continue;
                        }

                        missingCount++;
                        if (policy.RequireCompleteCoverage)
                        {
                            violations.Add(new Microchunk96CellValidationViolation(
                                id,
                                null,
                                null,
                                null,
                                coordinate,
                                Microchunk96CellValidationViolation.MissingCellRecordReason));
                        }
                    }
                }
            }

            violations.Sort(CompareViolations);
            return new Microchunk96CellValidationResult(
                ids.Count,
                records.Count,
                uniqueCount,
                missingCount,
                duplicateCount,
                outOfRangeCount,
                rowCountMismatchCount,
                violations);
        }

        private static int CompareViolations(
            Microchunk96CellValidationViolation left,
            Microchunk96CellValidationViolation right)
        {
            var comparison = left.MicrochunkId.CompareTo(right.MicrochunkId);
            if (comparison != 0) return comparison;

            comparison = ReasonOrder(left.Reason).CompareTo(ReasonOrder(right.Reason));
            if (comparison != 0) return comparison;

            comparison = CoordinateOrder(left).CompareTo(CoordinateOrder(right));
            if (comparison != 0) return comparison;

            return Nullable.Compare(left.SourceOrdinal, right.SourceOrdinal);
        }

        private static int ReasonOrder(string reason)
        {
            if (reason == Microchunk96CellValidationViolation.MissingCellRecordReason) return 0;
            if (reason == Microchunk96CellValidationViolation.DuplicateCellCoordinateReason) return 1;
            if (reason == Microchunk96CellValidationViolation.CellCoordinateOutOfRangeReason) return 2;
            return int.MaxValue;
        }

        private static long CoordinateOrder(Microchunk96CellValidationViolation violation)
        {
            if (violation.NormalizedLocalCoordinate.HasValue)
            {
                return violation.NormalizedLocalCoordinate.Value.RowMajorIndex;
            }

            if (violation.RawLocalX.HasValue && violation.RawLocalY.HasValue)
            {
                return ((long)violation.RawLocalY.Value * int.MaxValue) + violation.RawLocalX.Value;
            }

            return long.MaxValue;
        }
    }
}
