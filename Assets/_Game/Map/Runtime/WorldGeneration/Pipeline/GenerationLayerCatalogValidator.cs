using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Pipeline
{
    public enum GenerationLayerValidationErrorCode
    {
        NullLayer,
        UnexpectedLayerCount,
        MissingLayerId,
        DuplicateLayerId,
        DuplicateStableOrder,
        WrongStableOrder,
        InvalidResponsibility,
        MissingResponsibility,
        DuplicateResponsibilityOwner,
        WrongResponsibilityOwner,
        MissingOrderInvariant,
        DuplicateOrderInvariant,
        LayerOrderInvariantViolation,
        MicroChunkNotFinal,
        PacingAssignmentAuthorityClaimed,
        InvalidPacingMode,
        InvalidAccessMode,
        InvalidGeneralAccessAuthority,
        InvalidSpecialEntryAuthority,
        InvalidPacingRole,
        DuplicatePacingRole,
        InvalidAccessClass,
        InvalidPacingToken,
        InvalidAccessToken,
        PacingTokenContainsAccessMeaning,
        AccessTokenContainsPacingOrMovementMeaning,
        InvalidMandatoryMapping,
        RemovalChangesAccess,
        MicroChunkNotProvenanceOnly,
        MutableCollectionExposure,
        NonDeterministicDigest,
    }

    public sealed class GenerationLayerValidationError
    {
        public GenerationLayerValidationError(
            GenerationLayerValidationErrorCode code,
            GenerationLayerId? layerId,
            LayerResponsibilityId? responsibilityId,
            string detail)
        {
            Code = code;
            LayerId = layerId;
            ResponsibilityId = responsibilityId;
            Detail = detail ?? string.Empty;
        }

        public GenerationLayerValidationErrorCode Code { get; }
        public GenerationLayerId? LayerId { get; }
        public LayerResponsibilityId? ResponsibilityId { get; }
        public string Detail { get; }

        internal string StableKey =>
            ((int)Code) + "|" +
            (LayerId.HasValue ? ((int)LayerId.Value).ToString() : "-") + "|" +
            (ResponsibilityId.HasValue ? ((int)ResponsibilityId.Value).ToString() : "-") + "|" +
            Detail;

        internal static int Compare(
            GenerationLayerValidationError left,
            GenerationLayerValidationError right)
        {
            var value = left.Code.CompareTo(right.Code);
            if (value != 0) return value;
            value = Nullable.Compare(left.LayerId, right.LayerId);
            if (value != 0) return value;
            value = Nullable.Compare(left.ResponsibilityId, right.ResponsibilityId);
            if (value != 0) return value;
            return string.Compare(left.Detail, right.Detail, StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return Code + ":" +
                   (LayerId.HasValue ? LayerId.Value.ToString() : "-") + ":" +
                   (ResponsibilityId.HasValue ? ResponsibilityId.Value.ToString() : "-") + ":" +
                   Detail;
        }
    }

    public sealed class GenerationLayerValidationResult
    {
        private readonly ReadOnlyCollection<GenerationLayerValidationError> errors;

        internal GenerationLayerValidationResult(IEnumerable<GenerationLayerValidationError> source)
        {
            var values = source
                .GroupBy(value => value.StableKey, StringComparer.Ordinal)
                .Select(group => group.First())
                .ToList();
            values.Sort(GenerationLayerValidationError.Compare);
            errors = new ReadOnlyCollection<GenerationLayerValidationError>(values);
        }

        public bool IsValid => errors.Count == 0;
        public IReadOnlyList<GenerationLayerValidationError> Errors => errors;

        public int Count(GenerationLayerValidationErrorCode code)
        {
            return errors.Count(value => value.Code == code);
        }
    }

    public static class GenerationLayerCatalogValidator
    {
        private const int ExpectedLayerCount = 7;

        private static readonly ReadOnlyCollection<string> AccessMeanings =
            new ReadOnlyCollection<string>(new[]
            {
                "MANDATORY", "OPTIONAL", "TOOL", "HIDDEN", "PROGRESSION_GATE",
            });

        private static readonly ReadOnlyCollection<string> PacingOrMovementMeanings =
            new ReadOnlyCollection<string>(new[]
            {
                "QUIET", "TRAVERSAL", "DISCOVERY", "RISK", "RECOVERY", "SAFE",
                "MACHINERY", "FLOW", "ACTIVITY", "NARRATIVE", "REWARD", "LANDMARK",
                "RESOURCE", "BOSS", "INTEGRATED", "WALK", "JUMP", "DROP", "CLIMB",
            });

        public static GenerationLayerValidationResult Validate(
            IEnumerable<GenerationLayerContract> contracts)
        {
            return Validate(
                contracts,
                GenerationLayerCatalog.OrderInvariants,
                PacingRoleTokenCodec.Entries,
                AccessClassTokenCodec.Entries,
                AccessClass.MandatoryNoTool);
        }

        public static GenerationLayerValidationResult Validate(
            IEnumerable<GenerationLayerContract> contracts,
            IEnumerable<GenerationLayerOrderInvariant> invariants,
            IEnumerable<PacingRoleToken> pacingTokens,
            IEnumerable<AccessClassToken> accessTokens,
            AccessClass mandatoryMapping)
        {
            if (contracts == null) throw new ArgumentNullException(nameof(contracts));
            if (invariants == null) throw new ArgumentNullException(nameof(invariants));
            if (pacingTokens == null) throw new ArgumentNullException(nameof(pacingTokens));
            if (accessTokens == null) throw new ArgumentNullException(nameof(accessTokens));

            var source = contracts.ToArray();
            var invariantSource = invariants.ToArray();
            var pacingTokenSource = pacingTokens.ToArray();
            var accessTokenSource = accessTokens.ToArray();
            var errors = new List<GenerationLayerValidationError>();

            foreach (var ignored in source.Where(value => value == null))
            {
                Add(errors, GenerationLayerValidationErrorCode.NullLayer, null, null, "null");
            }

            var entries = source.Where(value => value != null).ToArray();
            if (entries.Length != ExpectedLayerCount)
            {
                Add(errors, GenerationLayerValidationErrorCode.UnexpectedLayerCount,
                    null, null, entries.Length.ToString());
            }

            ValidateLayerIdentity(entries, errors);
            ValidateResponsibilities(entries, errors);
            ValidateOrder(entries, invariantSource, errors);
            ValidateModesAndCompatibility(entries, errors);
            ValidateTokens(pacingTokenSource, accessTokenSource, errors);
            ValidateMandatoryAndRemovalContracts(entries, mandatoryMapping, errors);
            ValidateImmutabilityAndDigest(
                entries,
                invariantSource.Where(value => value != null).ToArray(),
                pacingTokenSource.Where(value => value != null).ToArray(),
                accessTokenSource.Where(value => value != null).ToArray(),
                errors);

            return new GenerationLayerValidationResult(errors);
        }

        private static void ValidateLayerIdentity(
            IReadOnlyCollection<GenerationLayerContract> entries,
            ICollection<GenerationLayerValidationError> errors)
        {
            foreach (var group in entries.GroupBy(value => value.LayerId).Where(group => group.Count() > 1))
            {
                Add(errors, GenerationLayerValidationErrorCode.DuplicateLayerId,
                    group.Key, null, group.Count().ToString());
            }

            foreach (GenerationLayerId layerId in Enum.GetValues(typeof(GenerationLayerId)))
            {
                if (!entries.Any(value => value.LayerId == layerId))
                {
                    Add(errors, GenerationLayerValidationErrorCode.MissingLayerId,
                        layerId, null, layerId.ToString());
                }
            }

            foreach (var group in entries.GroupBy(value => value.Order).Where(group => group.Count() > 1))
            {
                Add(errors, GenerationLayerValidationErrorCode.DuplicateStableOrder,
                    null, null, group.Key.ToString());
            }

            foreach (var entry in entries)
            {
                if (!Enum.IsDefined(typeof(GenerationLayerId), entry.LayerId) ||
                    entry.Order != (int)entry.LayerId)
                {
                    Add(errors, GenerationLayerValidationErrorCode.WrongStableOrder,
                        entry.LayerId, null, entry.Order.ToString());
                }
            }
        }

        private static void ValidateResponsibilities(
            IReadOnlyCollection<GenerationLayerContract> entries,
            ICollection<GenerationLayerValidationError> errors)
        {
            foreach (var entry in entries)
            {
                foreach (var responsibility in entry.OwnedResponsibilities)
                {
                    if (!Enum.IsDefined(typeof(LayerResponsibilityId), responsibility))
                    {
                        Add(errors, GenerationLayerValidationErrorCode.InvalidResponsibility,
                            entry.LayerId, responsibility, ((int)responsibility).ToString());
                    }
                }
            }

            foreach (LayerResponsibilityId responsibility in Enum.GetValues(typeof(LayerResponsibilityId)))
            {
                var owners = entries
                    .SelectMany(value => value.OwnedResponsibilities
                        .Where(item => item == responsibility)
                        .Select(item => value.LayerId))
                    .ToArray();
                if (owners.Length == 0)
                {
                    Add(errors, GenerationLayerValidationErrorCode.MissingResponsibility,
                        null, responsibility, responsibility.ToString());
                    continue;
                }

                if (owners.Length > 1)
                {
                    Add(errors, GenerationLayerValidationErrorCode.DuplicateResponsibilityOwner,
                        null, responsibility, owners.Length.ToString());
                }

                var expected = ExpectedOwner(responsibility);
                foreach (var owner in owners.Where(value => value != expected))
                {
                    Add(errors, GenerationLayerValidationErrorCode.WrongResponsibilityOwner,
                        owner, responsibility, expected.ToString());
                }
            }
        }

        private static void ValidateOrder(
            IReadOnlyCollection<GenerationLayerContract> entries,
            IReadOnlyCollection<GenerationLayerOrderInvariant> invariants,
            ICollection<GenerationLayerValidationError> errors)
        {
            foreach (var ignored in invariants.Where(value => value == null))
            {
                Add(errors, GenerationLayerValidationErrorCode.MissingOrderInvariant,
                    null, null, "null");
            }

            var validInvariants = invariants.Where(value => value != null).ToArray();
            foreach (LayerOrderInvariantId invariantId in Enum.GetValues(typeof(LayerOrderInvariantId)))
            {
                var matches = validInvariants.Where(value => value.InvariantId == invariantId).ToArray();
                if (matches.Length == 0)
                {
                    Add(errors, GenerationLayerValidationErrorCode.MissingOrderInvariant,
                        null, null, invariantId.ToString());
                }
                else if (matches.Length > 1)
                {
                    Add(errors, GenerationLayerValidationErrorCode.DuplicateOrderInvariant,
                        null, null, invariantId.ToString());
                }
            }

            foreach (var invariant in validInvariants)
            {
                var before = entries.Where(value => value.LayerId == invariant.Before).ToArray();
                var after = entries.Where(value => value.LayerId == invariant.After).ToArray();
                if (before.Length != 1 || after.Length != 1) continue;

                if (invariant.RequiresFinalLayer)
                {
                    var maximum = entries.OrderBy(value => value.Order).LastOrDefault();
                    if (maximum == null || maximum.LayerId != GenerationLayerId.MicroChunk)
                    {
                        Add(errors, GenerationLayerValidationErrorCode.LayerOrderInvariantViolation,
                            GenerationLayerId.MicroChunk, null, invariant.InvariantId.ToString());
                    }
                }
                else if (before[0].Order >= after[0].Order)
                {
                    Add(errors, GenerationLayerValidationErrorCode.LayerOrderInvariantViolation,
                        before[0].LayerId, null, invariant.InvariantId.ToString());
                }
            }

            var final = entries.OrderBy(value => value.Order).LastOrDefault();
            if (final == null || final.LayerId != GenerationLayerId.MicroChunk)
            {
                Add(errors, GenerationLayerValidationErrorCode.MicroChunkNotFinal,
                    final == null ? (GenerationLayerId?)null : final.LayerId,
                    null,
                    final == null ? "empty" : final.LayerId.ToString());
            }
        }

        private static void ValidateModesAndCompatibility(
            IReadOnlyCollection<GenerationLayerContract> entries,
            ICollection<GenerationLayerValidationError> errors)
        {
            foreach (var entry in entries)
            {
                if (entry.ClaimsPacingAssignmentAuthority)
                {
                    Add(errors, GenerationLayerValidationErrorCode.PacingAssignmentAuthorityClaimed,
                        entry.LayerId, null, entry.LayerId.ToString());
                }

                if (!Enum.IsDefined(typeof(LayerPacingMode), entry.PacingMode) ||
                    entry.PacingMode != ExpectedPacingMode(entry.LayerId))
                {
                    Add(errors, GenerationLayerValidationErrorCode.InvalidPacingMode,
                        entry.LayerId, null, entry.PacingMode.ToString());
                }

                if (!Enum.IsDefined(typeof(LayerAccessMode), entry.AccessMode) ||
                    entry.AccessMode != ExpectedAccessMode(entry.LayerId))
                {
                    Add(errors, GenerationLayerValidationErrorCode.InvalidAccessMode,
                        entry.LayerId, null, entry.AccessMode.ToString());
                }

                if ((entry.AccessMode == LayerAccessMode.GeneralAuthority) !=
                    (entry.LayerId == GenerationLayerId.RouteType))
                {
                    Add(errors, GenerationLayerValidationErrorCode.InvalidGeneralAccessAuthority,
                        entry.LayerId, null, entry.AccessMode.ToString());
                }

                if ((entry.AccessMode == LayerAccessMode.SpecialEntryAuthority) !=
                    (entry.LayerId == GenerationLayerId.SpecialRegion))
                {
                    Add(errors, GenerationLayerValidationErrorCode.InvalidSpecialEntryAuthority,
                        entry.LayerId, null, entry.AccessMode.ToString());
                }

                if (entry.CompatiblePacingRoles.Count == 0)
                {
                    Add(errors, GenerationLayerValidationErrorCode.InvalidPacingRole,
                        entry.LayerId, null, "empty");
                }

                foreach (var role in entry.CompatiblePacingRoles)
                {
                    if (!PacingRoleTokenCodec.IsPublished(role))
                    {
                        Add(errors, GenerationLayerValidationErrorCode.InvalidPacingRole,
                            entry.LayerId, null, ((int)role).ToString());
                    }
                }

                foreach (var duplicate in entry.CompatiblePacingRoles
                    .GroupBy(value => value).Where(group => group.Count() > 1))
                {
                    Add(errors, GenerationLayerValidationErrorCode.DuplicatePacingRole,
                        entry.LayerId, null, duplicate.Key.ToString());
                }

                if (entry.CompatibleAccessClasses.Count == 0)
                {
                    Add(errors, GenerationLayerValidationErrorCode.InvalidAccessClass,
                        entry.LayerId, null, "empty");
                }

                foreach (var access in entry.CompatibleAccessClasses)
                {
                    if (!AccessClassTokenCodec.IsPublished(access))
                    {
                        Add(errors, GenerationLayerValidationErrorCode.InvalidAccessClass,
                            entry.LayerId, null, ((int)access).ToString());
                    }
                }
            }
        }

        private static void ValidateTokens(
            IReadOnlyCollection<PacingRoleToken> pacingTokens,
            IReadOnlyCollection<AccessClassToken> accessTokens,
            ICollection<GenerationLayerValidationError> errors)
        {
            foreach (PacingRole role in Enum.GetValues(typeof(PacingRole)))
            {
                if (role == PacingRole.None) continue;
                var matches = pacingTokens.Where(value => value != null && value.Role == role).ToArray();
                if (matches.Length != 1 ||
                    !string.Equals(matches[0].Token, PacingRoleTokenCodec.ToToken(role), StringComparison.Ordinal))
                {
                    Add(errors, GenerationLayerValidationErrorCode.InvalidPacingToken,
                        null, null, role.ToString());
                }
            }

            foreach (var entry in pacingTokens)
            {
                if (entry == null || !PacingRoleTokenCodec.IsPublished(entry.Role))
                {
                    Add(errors, GenerationLayerValidationErrorCode.InvalidPacingToken,
                        null, null, entry == null ? "null" : ((int)entry.Role).ToString());
                    continue;
                }

                if (ContainsAny(entry.Token, AccessMeanings))
                {
                    Add(errors, GenerationLayerValidationErrorCode.PacingTokenContainsAccessMeaning,
                        null, null, entry.Token);
                }
            }

            foreach (AccessClass access in Enum.GetValues(typeof(AccessClass)))
            {
                if (access == AccessClass.Unspecified) continue;
                var matches = accessTokens.Where(value => value != null && value.AccessClass == access).ToArray();
                if (matches.Length != 1 ||
                    !string.Equals(matches[0].Token, AccessClassTokenCodec.ToToken(access), StringComparison.Ordinal))
                {
                    Add(errors, GenerationLayerValidationErrorCode.InvalidAccessToken,
                        null, null, access.ToString());
                }
            }

            foreach (var entry in accessTokens)
            {
                if (entry == null || !AccessClassTokenCodec.IsPublished(entry.AccessClass))
                {
                    Add(errors, GenerationLayerValidationErrorCode.InvalidAccessToken,
                        null, null, entry == null ? "null" : ((int)entry.AccessClass).ToString());
                    continue;
                }

                if (ContainsAny(entry.Token, PacingOrMovementMeanings))
                {
                    Add(errors, GenerationLayerValidationErrorCode.AccessTokenContainsPacingOrMovementMeaning,
                        null, null, entry.Token);
                }
            }
        }

        private static void ValidateMandatoryAndRemovalContracts(
            IReadOnlyCollection<GenerationLayerContract> entries,
            AccessClass mandatoryMapping,
            ICollection<GenerationLayerValidationError> errors)
        {
            if (mandatoryMapping != AccessClass.MandatoryNoTool)
            {
                Add(errors, GenerationLayerValidationErrorCode.InvalidMandatoryMapping,
                    GenerationLayerId.RouteType, null, mandatoryMapping.ToString());
            }

            foreach (var layerId in new[]
            {
                GenerationLayerId.RouteType,
                GenerationLayerId.TerrainCluster,
                GenerationLayerId.MicroPattern,
                GenerationLayerId.ActivityStructure,
            })
            {
                var entry = entries.FirstOrDefault(value => value.LayerId == layerId);
                if (entry != null && entry.CompatibleAccessClasses.Contains(AccessClass.ProgressionGate))
                {
                    Add(errors, GenerationLayerValidationErrorCode.InvalidMandatoryMapping,
                        layerId, null, AccessClass.ProgressionGate.ToString());
                }
            }

            foreach (var layerId in new[]
            {
                GenerationLayerId.ActivityStructure,
                GenerationLayerId.EventOverlay,
            })
            {
                var entry = entries.FirstOrDefault(value => value.LayerId == layerId);
                if (entry != null && !entry.PreservesAccessWhenRemoved)
                {
                    Add(errors, GenerationLayerValidationErrorCode.RemovalChangesAccess,
                        layerId, null, layerId.ToString());
                }
            }

            var microChunk = entries.FirstOrDefault(value => value.LayerId == GenerationLayerId.MicroChunk);
            if (microChunk != null &&
                (!microChunk.StoresAccessProvenanceOnly ||
                 microChunk.AccessMode != LayerAccessMode.PreserveOnly))
            {
                Add(errors, GenerationLayerValidationErrorCode.MicroChunkNotProvenanceOnly,
                    GenerationLayerId.MicroChunk, null, microChunk.AccessMode.ToString());
            }
        }

        private static void ValidateImmutabilityAndDigest(
            IReadOnlyCollection<GenerationLayerContract> entries,
            IReadOnlyList<GenerationLayerOrderInvariant> invariants,
            IReadOnlyList<PacingRoleToken> pacingTokens,
            IReadOnlyList<AccessClassToken> accessTokens,
            ICollection<GenerationLayerValidationError> errors)
        {
            foreach (var entry in entries)
            {
                if (IsMutable(entry.OwnedResponsibilities) ||
                    IsMutable(entry.CompatiblePacingRoles) ||
                    IsMutable(entry.CompatibleAccessClasses))
                {
                    Add(errors, GenerationLayerValidationErrorCode.MutableCollectionExposure,
                        entry.LayerId, null, entry.LayerId.ToString());
                }
            }

            try
            {
                var first = GenerationLayerCatalog.ComputeStableDigest(
                    entries, invariants, pacingTokens, accessTokens);
                var repeated = GenerationLayerCatalog.ComputeStableDigest(
                    entries, invariants, pacingTokens, accessTokens);
                var reversed = GenerationLayerCatalog.ComputeStableDigest(
                    entries.Reverse(), invariants.Reverse(), pacingTokens.Reverse(), accessTokens.Reverse());
                var renamed = GenerationLayerCatalog.ComputeStableDigest(
                    entries.Select((value, index) => value.WithDisplayId("DISPLAY_" + index)),
                    invariants,
                    pacingTokens,
                    accessTokens);
                if (!string.Equals(first, repeated, StringComparison.Ordinal) ||
                    !string.Equals(first, reversed, StringComparison.Ordinal) ||
                    !string.Equals(first, renamed, StringComparison.Ordinal))
                {
                    Add(errors, GenerationLayerValidationErrorCode.NonDeterministicDigest,
                        null, null, "mismatch");
                }
            }
            catch (Exception exception)
            {
                Add(errors, GenerationLayerValidationErrorCode.NonDeterministicDigest,
                    null, null, exception.GetType().Name);
            }
        }

        private static GenerationLayerId ExpectedOwner(LayerResponsibilityId responsibility)
        {
            switch (responsibility)
            {
                case LayerResponsibilityId.SectorExternalConnectivity:
                case LayerResponsibilityId.GeneralRouteAccess:
                    return GenerationLayerId.RouteType;
                case LayerResponsibilityId.WorldReservedLandmark:
                case LayerResponsibilityId.SpecialEntryAccess:
                    return GenerationLayerId.SpecialRegion;
                case LayerResponsibilityId.StaticTerrainTraversal:
                    return GenerationLayerId.TerrainCluster;
                case LayerResponsibilityId.LocalPatternTileOperation:
                    return GenerationLayerId.MicroPattern;
                case LayerResponsibilityId.StrongGameplayIncident:
                    return GenerationLayerId.ActivityStructure;
                case LayerResponsibilityId.MarkerOnlyRunVariation:
                    return GenerationLayerId.EventOverlay;
                case LayerResponsibilityId.SliceStorageAndBoundaryProjection:
                    return GenerationLayerId.MicroChunk;
                default:
                    throw new ArgumentOutOfRangeException(nameof(responsibility));
            }
        }

        private static LayerPacingMode ExpectedPacingMode(GenerationLayerId layerId)
        {
            return layerId == GenerationLayerId.RouteType || layerId == GenerationLayerId.MicroChunk
                ? LayerPacingMode.PreserveOnly
                : LayerPacingMode.CompatibilityOnly;
        }

        private static LayerAccessMode ExpectedAccessMode(GenerationLayerId layerId)
        {
            switch (layerId)
            {
                case GenerationLayerId.RouteType: return LayerAccessMode.GeneralAuthority;
                case GenerationLayerId.SpecialRegion: return LayerAccessMode.SpecialEntryAuthority;
                case GenerationLayerId.TerrainCluster:
                case GenerationLayerId.MicroPattern:
                case GenerationLayerId.ActivityStructure:
                    return LayerAccessMode.CompatibilityOnly;
                case GenerationLayerId.EventOverlay:
                case GenerationLayerId.MicroChunk:
                    return LayerAccessMode.PreserveOnly;
                default:
                    return (LayerAccessMode)(-1);
            }
        }

        private static bool ContainsAny(string value, IEnumerable<string> fragments)
        {
            return fragments.Any(fragment =>
                value.IndexOf(fragment, StringComparison.Ordinal) >= 0);
        }

        private static bool IsMutable<T>(IReadOnlyList<T> values)
        {
            var list = values as IList<T>;
            return list != null && !list.IsReadOnly;
        }

        private static void Add(
            ICollection<GenerationLayerValidationError> errors,
            GenerationLayerValidationErrorCode code,
            GenerationLayerId? layerId,
            LayerResponsibilityId? responsibilityId,
            string detail)
        {
            errors.Add(new GenerationLayerValidationError(code, layerId, responsibilityId, detail));
        }
    }
}
