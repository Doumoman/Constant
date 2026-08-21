using System;
using System.Collections.Generic;
using StarNight.Map.WorldGeneration.Data;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MandatoryRouteMaskLookupBuilder
    {
        public MandatoryRouteMaskLookupBuildResult Build(WorldRouteDefinitionSet definitionSet)
        {
            if (definitionSet == null)
                return Invalid(new[] { Error(MandatoryRouteMaskLookupBuildErrorCode.MissingInput, string.Empty, string.Empty, -1,
                    "World route definition set is required.") });
            if (definitionSet.RouteMasks == null)
                return Invalid(new[] { Error(MandatoryRouteMaskLookupBuildErrorCode.MissingInput, string.Empty, string.Empty, -1,
                    "Route mask definitions are required.") });
            return Build(definitionSet.RouteMasks.Values);
        }

        public MandatoryRouteMaskLookupBuildResult Build(IEnumerable<SectorRouteMaskDefinition> routeMasks)
        {
            if (routeMasks == null)
                return Invalid(new[] { Error(MandatoryRouteMaskLookupBuildErrorCode.MissingInput, string.Empty, string.Empty, -1,
                    "Route mask definitions are required.") });

            List<SectorRouteMaskDefinition> rows;
            try
            {
                rows = new List<SectorRouteMaskDefinition>(routeMasks);
            }
            catch
            {
                return Invalid(new[] { Error(MandatoryRouteMaskLookupBuildErrorCode.MissingInput, string.Empty, string.Empty, -1,
                    "Route mask definitions could not be enumerated.") });
            }

            var errors = new List<MandatoryRouteMaskLookupBuildError>();
            var byId = new Dictionary<string, List<SectorRouteMaskDefinition>>(StringComparer.Ordinal);
            foreach (var row in rows)
            {
                if (row == null)
                {
                    errors.Add(Error(MandatoryRouteMaskLookupBuildErrorCode.MissingInput, string.Empty, string.Empty, -1,
                        "Route mask definition cannot be null."));
                    continue;
                }
                var id = row.RouteMaskId ?? string.Empty;
                if (!byId.TryGetValue(id, out var matches))
                {
                    matches = new List<SectorRouteMaskDefinition>();
                    byId.Add(id, matches);
                }
                matches.Add(row);
            }

            foreach (var pair in byId)
            {
                if (pair.Value.Count > 1)
                    errors.Add(Error(MandatoryRouteMaskLookupBuildErrorCode.DuplicateMaskId, pair.Key, pair.Key,
                        pair.Value[0].RouteType, "Route mask ID occurs more than once."));
            }

            ValidateRequired(byId, "ROUTE_T1_LR", 1, MandatoryRouteOpenMask.Type1Horizontal, errors);
            ValidateRequired(byId, "ROUTE_T2_LRD", 2, MandatoryRouteOpenMask.Type2Down, errors);
            ValidateRequired(byId, "ROUTE_T3_LRU", 3, MandatoryRouteOpenMask.Type3Up, errors);

            var candidates = new List<SectorRouteMaskDefinition>();
            foreach (var row in rows)
            {
                if (row == null) continue;
                if (row.RouteType == 0) continue;
                if (row.Active && row.MandatoryAllowed && row.RouteType >= 1 && row.RouteType <= 3)
                {
                    candidates.Add(row);
                    if (!IsRequiredId(row.RouteMaskId))
                        errors.Add(Error(MandatoryRouteMaskLookupBuildErrorCode.UnexpectedMandatoryMask,
                            row.RouteMaskId, string.Empty, row.RouteType,
                            "Active mandatory Type1/2/3 mask is not registered by this pass."));
                    ValidateCandidateShape(row, errors);
                }
                else if (row.Active && row.MandatoryAllowed && (row.RouteType < 1 || row.RouteType > 3))
                {
                    errors.Add(Error(MandatoryRouteMaskLookupBuildErrorCode.InvalidRouteType,
                        row.RouteMaskId, string.Empty, row.RouteType,
                        "Mandatory route type must be 1, 2, or 3."));
                }
            }
            ValidateCandidateDuplicates(candidates, errors);

            errors = SortAndDedupe(errors);
            if (errors.Count > 0) return Invalid(errors);

            var type1 = byId["ROUTE_T1_LR"][0];
            var type2 = byId["ROUTE_T2_LRD"][0];
            var type3 = byId["ROUTE_T3_LRU"][0];
            var records = new[]
            {
                CreateRecord(type1, MandatoryRouteMaskKind.Type1, MandatoryRouteOpenMask.Type1Horizontal),
                CreateRecord(type2, MandatoryRouteMaskKind.Type2, MandatoryRouteOpenMask.Type2Down),
                CreateRecord(type3, MandatoryRouteMaskKind.Type3, MandatoryRouteOpenMask.Type3Up)
            };
            var activeCount = 0;
            var mandatoryCount = 0;
            var ignoredType0Count = 0;
            foreach (var row in rows)
            {
                if (row == null) continue;
                if (row.Active) activeCount++;
                if (row.MandatoryAllowed) mandatoryCount++;
                if (row.RouteType == 0) ignoredType0Count++;
            }
            var lookup = new MandatoryRouteMaskLookup(records);
            var diagnostics = new MandatoryRouteMaskLookupDiagnostics(
                rows.Count, activeCount, mandatoryCount, 3, 1, 1, 1, ignoredType0Count, 0, 0, 0);
            return new MandatoryRouteMaskLookupBuildResult(
                MandatoryRouteMaskLookupBuildStatus.Completed, lookup, diagnostics,
                Array.Empty<MandatoryRouteMaskLookupBuildError>());
        }

        internal static List<MandatoryRouteMaskLookupBuildError> SortAndDedupe(
            IEnumerable<MandatoryRouteMaskLookupBuildError> source)
        {
            var values = new List<MandatoryRouteMaskLookupBuildError>();
            foreach (var error in source) if (error != null) values.Add(error);
            values.Sort(MandatoryRouteMaskLookupBuildError.Compare);
            var result = new List<MandatoryRouteMaskLookupBuildError>();
            foreach (var error in values)
                if (result.Count == 0 || MandatoryRouteMaskLookupBuildError.Compare(result[result.Count - 1], error) != 0)
                    result.Add(error);
            return result;
        }

        private static void ValidateRequired(
            IReadOnlyDictionary<string, List<SectorRouteMaskDefinition>> byId,
            string id,
            int routeType,
            MandatoryRouteOpenMask expectedMask,
            ICollection<MandatoryRouteMaskLookupBuildError> errors)
        {
            if (!byId.TryGetValue(id, out var matches) || matches.Count == 0)
            {
                errors.Add(Error(MandatoryRouteMaskLookupBuildErrorCode.MissingRequiredMask, id, string.Empty, routeType,
                    "Required mandatory route mask is missing."));
                return;
            }
            foreach (var row in matches)
            {
                if (!row.Active)
                    errors.Add(Error(MandatoryRouteMaskLookupBuildErrorCode.InactiveRequiredMask, id, string.Empty, row.RouteType,
                        "Required mandatory route mask must be active."));
                if (!row.MandatoryAllowed)
                    errors.Add(Error(MandatoryRouteMaskLookupBuildErrorCode.MandatoryNotAllowed, id, string.Empty, row.RouteType,
                        "Required mandatory route mask must be mandatory-allowed."));
                if (row.RouteType != routeType)
                    errors.Add(Error(MandatoryRouteMaskLookupBuildErrorCode.InvalidRouteType, id, string.Empty, row.RouteType,
                        "Required mandatory route mask has the wrong route type."));
                var mask = OpenMask(row);
                if (mask.HasVerticalPairConflict)
                    errors.Add(Error(MandatoryRouteMaskLookupBuildErrorCode.UnsupportedVerticalPair, id, string.Empty, row.RouteType,
                        "Mandatory route masks cannot open both Up and Down."));
                if (mask != expectedMask)
                    errors.Add(Error(MandatoryRouteMaskLookupBuildErrorCode.InvalidOpenMask, id, string.Empty, row.RouteType,
                        "Required mandatory route mask has the wrong open-side matrix."));
            }
        }

        private static void ValidateCandidateShape(
            SectorRouteMaskDefinition row,
            ICollection<MandatoryRouteMaskLookupBuildError> errors)
        {
            var mask = OpenMask(row);
            if (!mask.HasHorizontalRun)
                errors.Add(Error(MandatoryRouteMaskLookupBuildErrorCode.InvalidOpenMask,
                    row.RouteMaskId, string.Empty, row.RouteType,
                    "Mandatory Type1/2/3 masks must open Left and Right."));
            if (mask.HasVerticalPairConflict)
                errors.Add(Error(MandatoryRouteMaskLookupBuildErrorCode.UnsupportedVerticalPair,
                    row.RouteMaskId, string.Empty, row.RouteType,
                    "Mandatory route masks cannot open both Up and Down."));
            if (ExpectedMask(row.RouteType) != mask)
                errors.Add(Error(MandatoryRouteMaskLookupBuildErrorCode.InvalidOpenMask,
                    row.RouteMaskId, string.Empty, row.RouteType,
                    "Mandatory open-side matrix must match its route type."));
        }

        private static void ValidateCandidateDuplicates(
            IReadOnlyList<SectorRouteMaskDefinition> candidates,
            ICollection<MandatoryRouteMaskLookupBuildError> errors)
        {
            for (var leftIndex = 0; leftIndex < candidates.Count; leftIndex++)
            {
                var left = candidates[leftIndex];
                for (var rightIndex = leftIndex + 1; rightIndex < candidates.Count; rightIndex++)
                {
                    var right = candidates[rightIndex];
                    var first = string.Compare(left.RouteMaskId, right.RouteMaskId, StringComparison.Ordinal) <= 0 ? left : right;
                    var second = ReferenceEquals(first, left) ? right : left;
                    if (left.RouteType == right.RouteType)
                        errors.Add(Error(MandatoryRouteMaskLookupBuildErrorCode.DuplicateRouteType,
                            first.RouteMaskId, second.RouteMaskId, left.RouteType,
                            "Mandatory route type occurs more than once."));
                    if (OpenMask(left) == OpenMask(right))
                        errors.Add(Error(MandatoryRouteMaskLookupBuildErrorCode.DuplicateOpenMask,
                            first.RouteMaskId, second.RouteMaskId, left.RouteType,
                            "Mandatory open mask occurs more than once."));
                }
            }
        }

        private static MandatoryRouteMaskRecord CreateRecord(
            SectorRouteMaskDefinition row, MandatoryRouteMaskKind kind, MandatoryRouteOpenMask mask) =>
            new MandatoryRouteMaskRecord(new MandatoryRouteMaskId(row.RouteMaskId), kind, row.RouteType, mask,
                row.MandatoryAllowed, row.Active, row.DescriptionKo, row);

        private static MandatoryRouteOpenMask OpenMask(SectorRouteMaskDefinition row) =>
            new MandatoryRouteOpenMask(row.OpenL, row.OpenR, row.OpenU, row.OpenD);

        private static MandatoryRouteOpenMask ExpectedMask(int routeType)
        {
            switch (routeType)
            {
                case 1: return MandatoryRouteOpenMask.Type1Horizontal;
                case 2: return MandatoryRouteOpenMask.Type2Down;
                case 3: return MandatoryRouteOpenMask.Type3Up;
                default: return default(MandatoryRouteOpenMask);
            }
        }

        private static bool IsRequiredId(string id) =>
            string.Equals(id, "ROUTE_T1_LR", StringComparison.Ordinal) ||
            string.Equals(id, "ROUTE_T2_LRD", StringComparison.Ordinal) ||
            string.Equals(id, "ROUTE_T3_LRU", StringComparison.Ordinal);

        private static MandatoryRouteMaskLookupBuildResult Invalid(IEnumerable<MandatoryRouteMaskLookupBuildError> errors) =>
            new MandatoryRouteMaskLookupBuildResult(MandatoryRouteMaskLookupBuildStatus.InvalidInput, null, null, SortAndDedupe(errors));

        private static MandatoryRouteMaskLookupBuildError Error(
            MandatoryRouteMaskLookupBuildErrorCode code, string firstId, string secondId, int routeType, string message) =>
            new MandatoryRouteMaskLookupBuildError(code, firstId, secondId, routeType, message);
    }
}
