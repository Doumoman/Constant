using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.MicroPatterns
{
    public static class MicroPatternOrderedRenderer
    {
        public static MicroPatternRenderResult Render(
            IEnumerable<MicroPatternRenderRequest> requests,
            MicroPatternRenderTarget target)
        {
            var errors = new List<MicroPatternRenderError>();
            var requestSnapshot = requests == null
                ? Array.Empty<MicroPatternRenderRequest>()
                : requests.ToArray();
            if (requests == null || requestSnapshot.Length == 0)
            {
                Add(errors, MicroPatternRenderErrorCode.MissingInput,
                    "requests", "At least one render request is required.");
            }

            ValidateRequests(requestSnapshot, errors);
            var expectedCoordinates = CollectExpectedCoordinates(requestSnapshot);
            ValidateTarget(target, expectedCoordinates, errors);

            if (errors.Count != 0)
            {
                AddAtomic(errors);
                return new MicroPatternRenderResult(null, errors);
            }

            var rawWrites = CollectWrites(requestSnapshot);
            var coalescedWrites = new List<MicroPatternLayerWrite>();
            var conflicts = new List<MicroPatternRenderConflict>();
            foreach (var layerGroup in rawWrites
                         .GroupBy(value => new RenderLayerKey(value.TargetCoordinate, value.Layer))
                         .OrderBy(value => value.Key))
            {
                var alternatives = layerGroup
                    .GroupBy(value => value.SemanticValue, StringComparer.Ordinal)
                    .Select(group => Coalesce(group))
                    .OrderBy(value => value.SemanticValue, StringComparer.Ordinal)
                    .ToArray();
                if (alternatives.Length == 1)
                {
                    coalescedWrites.Add(alternatives[0]);
                }
                else
                {
                    conflicts.Add(new MicroPatternRenderConflict(
                        layerGroup.Key.Coordinate,
                        layerGroup.Key.Layer,
                        alternatives));
                }
            }

            if (conflicts.Count != 0)
            {
                foreach (var conflict in conflicts.OrderBy(value => value))
                {
                    Add(errors, MicroPatternRenderErrorCode.ConflictingLayerWrite,
                        CoordinatePath(conflict.TargetCoordinate) + "/" + conflict.Layer,
                        string.Join(",", conflict.Alternatives.Select(value => value.SemanticValue)));
                }
                AddAtomic(errors);
                return new MicroPatternRenderResult(null, errors, conflicts);
            }

            var targetByCoordinate = target.Cells.ToDictionary(value => value.TargetCoordinate);
            var current = targetByCoordinate.ToDictionary(pair => pair.Key, pair => pair.Value);
            var appliedWrites = new List<MicroPatternLayerWrite>();
            foreach (var write in coalescedWrites.OrderBy(value => value))
            {
                var state = current[write.TargetCoordinate];
                var idempotent = string.Equals(
                    state.GetSemanticValue(write.Layer),
                    write.SemanticValue,
                    StringComparison.Ordinal);
                var applied = write.WithIdempotence(idempotent);
                current[write.TargetCoordinate] = state.Apply(applied);
                appliedWrites.Add(applied);
            }

            var deltas = appliedWrites
                .GroupBy(value => value.TargetCoordinate)
                .OrderBy(value => value.Key.Y)
                .ThenBy(value => value.Key.X)
                .Select(group => new MicroPatternRenderedCellDelta(
                    group.Key,
                    targetByCoordinate[group.Key],
                    current[group.Key],
                    group))
                .ToArray();
            var digest = MicroPatternRenderCanonicalDigest.Compute(
                requestSnapshot,
                target,
                appliedWrites,
                deltas);
            var delta = new MicroPatternRenderDelta(
                requestSnapshot,
                target,
                appliedWrites,
                deltas,
                digest);
            return new MicroPatternRenderResult(delta, errors);
        }

        private static void ValidateRequests(
            IEnumerable<MicroPatternRenderRequest> requests,
            ICollection<MicroPatternRenderError> errors)
        {
            var validIds = new HashSet<MicroPatternRenderRequestId>();
            foreach (var request in requests)
            {
                if (request == null)
                {
                    Add(errors, MicroPatternRenderErrorCode.MissingInput,
                        "requests/null", "Render request is required.");
                    continue;
                }

                var path = "requests/" + (request.Id.Value.Length == 0 ? "EMPTY" : request.Id.Value);
                if (!IsRequestId(request.Id.Value))
                {
                    Add(errors, MicroPatternRenderErrorCode.InvalidRequestId,
                        path + "/id", request.Id.Value);
                }
                else if (!validIds.Add(request.Id))
                {
                    Add(errors, MicroPatternRenderErrorCode.DuplicateRequestId,
                        path + "/id", request.Id.Value);
                }

                ValidateApplicationPlan(request.ApplicationPlan, path + "/plan", errors);
            }
        }

        private static void ValidateApplicationPlan(
            MicroPatternApplicationPlan plan,
            string path,
            ICollection<MicroPatternRenderError> errors)
        {
            if (plan == null)
            {
                Add(errors, MicroPatternRenderErrorCode.InvalidApplicationPlan,
                    path, "Successful application plan is required.");
                return;
            }

            var structurallyValid = true;
            if (!IsPatternId(plan.SourcePatternId.Value) || !IsSha256(plan.SourceDigest) ||
                plan.ProtectedMask == null || plan.Cells.Count != MicroPatternDefinition.RequiredCellCount)
            {
                structurallyValid = false;
                Add(errors, MicroPatternRenderErrorCode.InvalidApplicationPlan,
                    path, "Source identity, digest, mask, and exact 16-cell coverage are required.");
            }

            var localCoordinates = new HashSet<LocalTileCoord>();
            var targetCoordinates = new HashSet<LocalTileCoord>();
            foreach (var cell in plan.Cells)
            {
                if (cell == null)
                {
                    structurallyValid = false;
                    Add(errors, MicroPatternRenderErrorCode.InvalidApplicationPlan,
                        path + "/cells/null", "Prepared cell is required.");
                    continue;
                }

                var cellPath = path + "/" + Coordinate(cell.TargetCoordinate);
                if (!localCoordinates.Add(cell.LocalCoordinate) ||
                    !targetCoordinates.Add(cell.TargetCoordinate) ||
                    cell.Instructions.Count != 6)
                {
                    structurallyValid = false;
                    Add(errors, MicroPatternRenderErrorCode.InvalidApplicationPlan,
                        cellPath, "Unique local/target coordinates and six instructions are required.");
                }

                var layers = new HashSet<MicroPatternLayer>();
                foreach (var instruction in cell.Instructions)
                {
                    if (instruction == null)
                    {
                        structurallyValid = false;
                        Add(errors, MicroPatternRenderErrorCode.InvalidApplicationPlan,
                            cellPath + "/instruction", "Instruction is required.");
                        continue;
                    }

                    if (!IsDefinedOperation(instruction.Operation))
                    {
                        structurallyValid = false;
                        Add(errors, MicroPatternRenderErrorCode.UnsupportedOperation,
                            cellPath + "/" + instruction.Layer,
                            Number((int)instruction.Operation));
                    }
                    if (!layers.Add(instruction.Layer) || !IsLayerOperationValid(instruction))
                    {
                        structurallyValid = false;
                        Add(errors, MicroPatternRenderErrorCode.LayerOperationMismatch,
                            cellPath + "/" + instruction.Layer,
                            instruction.Operation + "|" + instruction.PayloadId);
                    }
                }
            }

            if (!IsSha256(plan.StableDigest))
            {
                Add(errors, MicroPatternRenderErrorCode.PlanDigestMismatch,
                    path + "/digest", plan.StableDigest);
            }
            else if (structurallyValid)
            {
                var recomputed = MicroPatternRenderCanonicalDigest.ComputeApplicationPlan(plan);
                if (!string.Equals(plan.StableDigest, recomputed, StringComparison.Ordinal))
                {
                    Add(errors, MicroPatternRenderErrorCode.PlanDigestMismatch,
                        path + "/digest", plan.StableDigest + "!=" + recomputed);
                }
            }
        }

        private static HashSet<LocalTileCoord> CollectExpectedCoordinates(
            IEnumerable<MicroPatternRenderRequest> requests)
        {
            var result = new HashSet<LocalTileCoord>();
            foreach (var request in requests)
            {
                if (request == null || request.ApplicationPlan == null) continue;
                foreach (var cell in request.ApplicationPlan.Cells)
                    if (cell != null) result.Add(cell.TargetCoordinate);
            }
            return result;
        }

        private static void ValidateTarget(
            MicroPatternRenderTarget target,
            ISet<LocalTileCoord> expectedCoordinates,
            ICollection<MicroPatternRenderError> errors)
        {
            if (target == null)
            {
                Add(errors, MicroPatternRenderErrorCode.MissingInput,
                    "target", "Render target is required.");
                return;
            }

            var byCoordinate = new Dictionary<LocalTileCoord, int>();
            foreach (var cell in target.Cells)
            {
                if (cell == null)
                {
                    Add(errors, MicroPatternRenderErrorCode.MissingInput,
                        "target/cells/null", "Target cell is required.");
                    continue;
                }

                int count;
                byCoordinate.TryGetValue(cell.TargetCoordinate, out count);
                byCoordinate[cell.TargetCoordinate] = count + 1;
                ValidateLayerState(cell, errors);
                ValidateExistingProvenance(cell, errors);
            }

            foreach (var pair in byCoordinate.Where(value => value.Value > 1)
                         .OrderBy(value => value.Key.Y).ThenBy(value => value.Key.X))
            {
                Add(errors, MicroPatternRenderErrorCode.DuplicateTargetCell,
                    CoordinatePath(pair.Key), Number(pair.Value));
            }
            foreach (var coordinate in expectedCoordinates.Where(value => !byCoordinate.ContainsKey(value))
                         .OrderBy(value => value.Y).ThenBy(value => value.X))
            {
                Add(errors, MicroPatternRenderErrorCode.MissingTargetCell,
                    CoordinatePath(coordinate), "Expected by application-plan union.");
            }
            foreach (var coordinate in byCoordinate.Keys.Where(value => !expectedCoordinates.Contains(value))
                         .OrderBy(value => value.Y).ThenBy(value => value.X))
            {
                Add(errors, MicroPatternRenderErrorCode.ExtraTargetCell,
                    CoordinatePath(coordinate), "Not present in application-plan union.");
            }
        }

        private static void ValidateLayerState(
            MicroPatternRenderCellState cell,
            ICollection<MicroPatternRenderError> errors)
        {
            var values = new[]
            {
                Tuple.Create(MicroPatternLayer.Surface, cell.SurfaceId),
                Tuple.Create(MicroPatternLayer.Affordance, cell.AffordanceId),
                Tuple.Create(MicroPatternLayer.Material, cell.MaterialId),
                Tuple.Create(MicroPatternLayer.Hazard, cell.HazardId),
                Tuple.Create(MicroPatternLayer.Marker, cell.MarkerId),
            };
            foreach (var value in values)
            {
                if (value.Item2.Length != 0 && !IsStableToken(value.Item2))
                {
                    Add(errors, MicroPatternRenderErrorCode.InvalidLayerState,
                        CoordinatePath(cell.TargetCoordinate) + "/" + value.Item1,
                        value.Item2);
                }
            }
        }

        private static void ValidateExistingProvenance(
            MicroPatternRenderCellState cell,
            ICollection<MicroPatternRenderError> errors)
        {
            var seen = new HashSet<MicroPatternRenderSourceEvidence>();
            foreach (var source in cell.Provenance)
            {
                var path = CoordinatePath(cell.TargetCoordinate) + "/provenance";
                if (source == null)
                {
                    Add(errors, MicroPatternRenderErrorCode.InvalidExistingProvenance,
                        path + "/null", "Source evidence is required.");
                    continue;
                }

                var richEmpty = source.RequestId.Value.Length == 0 &&
                                source.SourcePatternId.Value.Length == 0 &&
                                source.SourceDigest.Length == 0 && source.PlanDigest.Length == 0 &&
                                source.ProtectedProvenance.Count == 0;
                var richValid = IsRequestId(source.RequestId.Value) &&
                                IsPatternId(source.SourcePatternId.Value) &&
                                IsSha256(source.SourceDigest) && IsSha256(source.PlanDigest);
                if (!IsDefinedLayer(source.Layer) || !IsStableToken(source.StableSourceId) ||
                    (!richEmpty && !richValid) || !seen.Add(source) ||
                    source.ProtectedProvenance.Any(value => value == null ||
                        !IsDefinedProtectedKind(value.SourceKind) || !IsStableToken(value.SourceId)))
                {
                    Add(errors, MicroPatternRenderErrorCode.InvalidExistingProvenance,
                        path + "/" + source.StableSourceId,
                        source.ToString());
                }
            }
        }

        private static List<MicroPatternLayerWrite> CollectWrites(
            IEnumerable<MicroPatternRenderRequest> requests)
        {
            var writes = new List<MicroPatternLayerWrite>();
            foreach (var request in requests.OrderBy(value => value.Id))
            {
                var plan = request.ApplicationPlan;
                foreach (var cell in plan.Cells.OrderBy(value => value.TargetCoordinate.Y)
                             .ThenBy(value => value.TargetCoordinate.X))
                {
                    MicroPatternProtectedMaskEntry maskEntry;
                    var protectedProvenance = plan.ProtectedMask.TryGetEntry(
                        cell.TargetCoordinate, out maskEntry)
                        ? maskEntry.Provenance
                        : Array.Empty<MicroPatternProtectedCell>();
                    foreach (var instruction in cell.Instructions.OrderBy(value => (int)value.Layer))
                    {
                        if (instruction.Operation == MicroPatternOperation.NoChange) continue;
                        var evidence = new MicroPatternRenderSourceEvidence(
                            instruction.Layer,
                            request.Id.Value,
                            request.Id,
                            plan.SourcePatternId,
                            plan.SourceDigest,
                            plan.StableDigest,
                            protectedProvenance);
                        writes.Add(new MicroPatternLayerWrite(
                            cell.TargetCoordinate,
                            Stage(instruction.Layer),
                            instruction.Layer,
                            instruction.Operation,
                            SemanticValue(instruction),
                            new[] { evidence },
                            false));
                    }
                }
            }
            return writes;
        }

        private static MicroPatternLayerWrite Coalesce(
            IEnumerable<MicroPatternLayerWrite> source)
        {
            var copy = source.OrderBy(value => value).ToArray();
            var first = copy[0];
            return new MicroPatternLayerWrite(
                first.TargetCoordinate,
                first.Stage,
                first.Layer,
                first.Operation,
                first.SemanticValue,
                copy.SelectMany(value => value.Provenance),
                false);
        }

        private static MicroPatternRenderStage Stage(MicroPatternLayer layer) =>
            (MicroPatternRenderStage)((int)layer * 10);

        private static string SemanticValue(MicroPatternInstruction instruction)
        {
            if (instruction.Operation == MicroPatternOperation.AddSolid) return "SOLID";
            if (instruction.Operation == MicroPatternOperation.CarveAir) return "AIR";
            return instruction.PayloadId;
        }

        private static bool IsLayerOperationValid(MicroPatternInstruction instruction)
        {
            if (!IsDefinedLayer(instruction.Layer)) return false;
            if (instruction.Operation == MicroPatternOperation.NoChange)
                return instruction.PayloadId.Length == 0;
            switch (instruction.Layer)
            {
                case MicroPatternLayer.Geometry:
                    return (instruction.Operation == MicroPatternOperation.AddSolid ||
                            instruction.Operation == MicroPatternOperation.CarveAir) &&
                           instruction.PayloadId.Length == 0;
                case MicroPatternLayer.Surface:
                    return instruction.Operation == MicroPatternOperation.SetSurface &&
                           IsStableToken(instruction.PayloadId);
                case MicroPatternLayer.Affordance:
                    return instruction.Operation == MicroPatternOperation.SetAffordance &&
                           IsStableToken(instruction.PayloadId);
                case MicroPatternLayer.Material:
                    return instruction.Operation == MicroPatternOperation.SetMaterial &&
                           IsStableToken(instruction.PayloadId);
                case MicroPatternLayer.Hazard:
                    return instruction.Operation == MicroPatternOperation.SetHazard &&
                           IsStableToken(instruction.PayloadId);
                case MicroPatternLayer.Marker:
                    return instruction.Operation == MicroPatternOperation.SetMarker &&
                           IsStableToken(instruction.PayloadId);
                default:
                    return false;
            }
        }

        private static bool IsRequestId(string value)
        {
            return value != null && value.StartsWith("MPR_", StringComparison.Ordinal) &&
                   value.Length > 4 && IsStableToken(value);
        }

        private static bool IsPatternId(string value)
        {
            return value != null && value.StartsWith("MP_", StringComparison.Ordinal) &&
                   value.Length > 3 && IsStableToken(value);
        }

        internal static bool IsStableToken(string value)
        {
            if (string.IsNullOrEmpty(value) || value[0] < 'A' || value[0] > 'Z') return false;
            for (var index = 1; index < value.Length; index++)
            {
                var character = value[index];
                if ((character < 'A' || character > 'Z') &&
                    (character < '0' || character > '9') &&
                    character != '_') return false;
            }
            return true;
        }

        private static bool IsSha256(string value)
        {
            if (value == null || value.Length != 64) return false;
            foreach (var character in value)
                if ((character < '0' || character > '9') &&
                    (character < 'a' || character > 'f')) return false;
            return true;
        }

        private static bool IsDefinedLayer(MicroPatternLayer value) =>
            value >= MicroPatternLayer.Geometry && value <= MicroPatternLayer.Marker;
        private static bool IsDefinedOperation(MicroPatternOperation value) =>
            value >= MicroPatternOperation.NoChange && value <= MicroPatternOperation.SetMarker;
        private static bool IsDefinedProtectedKind(MicroPatternProtectedSourceKind value) =>
            value >= MicroPatternProtectedSourceKind.RouteSpine &&
            value <= MicroPatternProtectedSourceKind.SpecialFixedEntry;

        private static void AddAtomic(ICollection<MicroPatternRenderError> errors)
        {
            Add(errors, MicroPatternRenderErrorCode.AtomicRenderRejected,
                "render", "Errors prevent delta publication.");
        }

        private static void Add(
            ICollection<MicroPatternRenderError> errors,
            MicroPatternRenderErrorCode code,
            string path,
            string detail)
        {
            errors.Add(new MicroPatternRenderError(code, path, detail));
        }

        private static string CoordinatePath(LocalTileCoord value) =>
            "target[" + Coordinate(value) + "]";
        private static string Coordinate(LocalTileCoord value) =>
            Number(value.X) + "," + Number(value.Y);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        private readonly struct RenderLayerKey : IComparable<RenderLayerKey>
        {
            public RenderLayerKey(LocalTileCoord coordinate, MicroPatternLayer layer)
            {
                Coordinate = coordinate;
                Layer = layer;
            }

            public LocalTileCoord Coordinate { get; }
            public MicroPatternLayer Layer { get; }

            public int CompareTo(RenderLayerKey other)
            {
                var comparison = Coordinate.Y.CompareTo(other.Coordinate.Y);
                if (comparison != 0) return comparison;
                comparison = Coordinate.X.CompareTo(other.Coordinate.X);
                return comparison != 0 ? comparison : ((int)Layer).CompareTo((int)other.Layer);
            }

            public override bool Equals(object obj)
            {
                return obj is RenderLayerKey other &&
                       Coordinate.Equals(other.Coordinate) && Layer == other.Layer;
            }
            public override int GetHashCode()
            {
                unchecked { return (Coordinate.GetHashCode() * 397) ^ (int)Layer; }
            }
        }
    }

    public static class MicroPatternRenderCanonicalDigest
    {
        internal static string Compute(
            IEnumerable<MicroPatternRenderRequest> requests,
            MicroPatternRenderTarget target,
            IEnumerable<MicroPatternLayerWrite> writes,
            IEnumerable<MicroPatternRenderedCellDelta> deltas)
        {
            var material = new StringBuilder();
            Append(material, "RULESET", MicroPatternRenderDelta.RulesetVersion);
            foreach (var request in requests.OrderBy(value => value.Id))
            {
                Append(material, "REQUEST", request.Id.Value,
                    request.ApplicationPlan.SourcePatternId.Value,
                    request.ApplicationPlan.SourceDigest,
                    request.ApplicationPlan.StableDigest);
            }
            foreach (var cell in target.Cells.OrderBy(value => value.TargetCoordinate.Y)
                         .ThenBy(value => value.TargetCoordinate.X))
            {
                AppendState(material, "INPUT", cell);
            }
            foreach (var write in writes.OrderBy(value => value))
            {
                AppendWrite(material, "WRITE", write);
            }
            foreach (var delta in deltas.OrderBy(value => value.TargetCoordinate.Y)
                         .ThenBy(value => value.TargetCoordinate.X))
            {
                Append(material, "DELTA", Number(delta.TargetCoordinate.X),
                    Number(delta.TargetCoordinate.Y), delta.ValuesEqual ? "EQUAL" : "CHANGED");
                AppendState(material, "BEFORE", delta.Before);
                AppendState(material, "AFTER", delta.After);
                foreach (var write in delta.Writes) AppendWrite(material, "DELTA_WRITE", write);
            }
            return Sha256(material.ToString());
        }

        internal static string ComputeApplicationPlan(MicroPatternApplicationPlan plan)
        {
            var material = new StringBuilder();
            Append(material, "SOURCE_ID", plan.SourcePatternId.Value);
            Append(material, "SOURCE_DIGEST", plan.SourceDigest);
            Append(material, "TRANSFORM", plan.Transform.ToString());
            Append(material, "ORIGIN", Number(plan.Origin.X), Number(plan.Origin.Y));
            Append(material, "POLICY", plan.ProtectedPolicy.ToString());
            foreach (var cell in plan.Cells.OrderBy(value => value.LocalCoordinate.Y)
                         .ThenBy(value => value.LocalCoordinate.X))
            {
                Append(material, "CELL", Number(cell.LocalCoordinate.X), Number(cell.LocalCoordinate.Y),
                    Number(cell.TargetCoordinate.X), Number(cell.TargetCoordinate.Y));
                foreach (var instruction in cell.Instructions.OrderBy(value => (int)value.Layer))
                {
                    Append(material, "INSTRUCTION", instruction.Layer.ToString(),
                        instruction.Operation.ToString(), instruction.PayloadId);
                }
            }
            foreach (var entry in plan.ProtectedMask.Entries)
            {
                Append(material, "MASK", Number(entry.TargetCoordinate.X),
                    Number(entry.TargetCoordinate.Y));
                foreach (var source in entry.Provenance)
                    Append(material, "PROVENANCE", source.SourceKind.ToString(), source.SourceId);
            }
            foreach (var hit in plan.ProtectedHits.OrderBy(value => value.TargetCoordinate.Y)
                         .ThenBy(value => value.TargetCoordinate.X))
            {
                Append(material, "HIT", Number(hit.LocalCoordinate.X), Number(hit.LocalCoordinate.Y),
                    Number(hit.TargetCoordinate.X), Number(hit.TargetCoordinate.Y),
                    Number(hit.RemovedWriteCount));
                foreach (var source in hit.Provenance)
                    Append(material, "HIT_SOURCE", source.SourceKind.ToString(), source.SourceId);
            }
            return Sha256(material.ToString());
        }

        private static void AppendState(
            StringBuilder material,
            string label,
            MicroPatternRenderCellState state)
        {
            Append(material, label, Number(state.TargetCoordinate.X), Number(state.TargetCoordinate.Y),
                state.Solid ? "SOLID" : "AIR", state.SurfaceId, state.AffordanceId,
                state.MaterialId, state.HazardId, state.MarkerId);
            foreach (var source in state.Provenance.OrderBy(value => value))
                AppendEvidence(material, label + "_SOURCE", source);
        }

        private static void AppendWrite(
            StringBuilder material,
            string label,
            MicroPatternLayerWrite write)
        {
            Append(material, label, Number((int)write.Stage),
                Number(write.TargetCoordinate.X), Number(write.TargetCoordinate.Y),
                write.Layer.ToString(), write.Operation.ToString(), write.SemanticValue,
                write.IsIdempotent ? "IDEMPOTENT" : "MUTATING");
            foreach (var source in write.Provenance)
                AppendEvidence(material, label + "_SOURCE", source);
        }

        private static void AppendEvidence(
            StringBuilder material,
            string label,
            MicroPatternRenderSourceEvidence source)
        {
            Append(material, label, source.Layer.ToString(), source.StableSourceId,
                source.RequestId.Value, source.SourcePatternId.Value,
                source.SourceDigest, source.PlanDigest);
            foreach (var protectedCell in source.ProtectedProvenance)
            {
                Append(material, label + "_PROTECTED",
                    Number(protectedCell.TargetCoordinate.X),
                    Number(protectedCell.TargetCoordinate.Y),
                    protectedCell.SourceKind.ToString(),
                    protectedCell.SourceId);
            }
        }

        private static string Sha256(string material)
        {
            using (var sha256 = SHA256.Create())
            {
                return string.Concat(sha256.ComputeHash(Encoding.UTF8.GetBytes(material))
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        private static void Append(StringBuilder target, params string[] fields)
        {
            foreach (var field in fields)
            {
                var value = field ?? string.Empty;
                target.Append(value.Length.ToString(CultureInfo.InvariantCulture));
                target.Append(':');
                target.Append(value);
            }
            target.Append('\n');
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
