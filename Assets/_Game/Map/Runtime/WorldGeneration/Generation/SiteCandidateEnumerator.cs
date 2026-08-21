using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SiteCandidateEnumerator
    {
        private const string BossId = "SITE_MOON_BOSS_VAULT";
        private const string ForgeId = "SITE_MOON_SEAL_FORGE";
        private const string CassiaId = "SITE_CASSIA_SAP_HEART";
        private const string YeastId = "SITE_DEEP_STAR_YEAST";
        private const string MeteorId = "SITE_MOON_CORE_METEOR";

        private static readonly IReadOnlyList<ExpectedSite> ExpectedSites =
            new ReadOnlyCollection<ExpectedSite>(new[]
            {
                new ExpectedSite(SiteReservationKind.Boss, BossId),
                new ExpectedSite(SiteReservationKind.Forge, ForgeId),
                new ExpectedSite(SiteReservationKind.CoreResource, CassiaId),
                new ExpectedSite(SiteReservationKind.CoreResource, YeastId),
                new ExpectedSite(SiteReservationKind.CoreResource, MeteorId)
            });

        public SiteCandidateEnumerationResult Enumerate(
            GridInitializationResult grid,
            WorldProfileDefinition worldProfile,
            GenerationProfileDefinition generationProfile,
            IEnumerable<SpecialMapDefinition> specialMaps)
        {
            var errors = new List<SiteCandidateEnumerationError>();
            ValidateGrid(grid, errors);
            ValidateProfiles(worldProfile, generationProfile, errors);
            ValidateSpecialMaps(specialMaps, errors);

            if (errors.Count != 0)
            {
                SortErrors(errors);
                return new SiteCandidateEnumerationResult(null, errors);
            }

            var startGroup = CreateGroup(
                SiteReservationKind.Start,
                worldProfile.WorldProfileId,
                generationProfile.StartEdgeRingMin,
                generationProfile.StartEdgeRingMax);
            var siteGroups = new List<SiteCandidateGroup>(ExpectedSites.Count);
            foreach (var expected in ExpectedSites)
            {
                siteGroups.Add(CreateGroup(expected.Kind, expected.SourceDefinitionId, 0, 6));
            }

            var catalog = new SiteCandidateCatalog(
                grid.WorldData.Seed,
                worldProfile.WorldProfileId,
                generationProfile.GenerationProfileId,
                startGroup,
                siteGroups);
            return new SiteCandidateEnumerationResult(
                catalog,
                Array.Empty<SiteCandidateEnumerationError>());
        }

        private static void ValidateGrid(
            GridInitializationResult grid,
            ICollection<SiteCandidateEnumerationError> errors)
        {
            if (grid == null)
            {
                Add(errors, SiteCandidateEnumerationErrorCode.MissingGrid, string.Empty,
                    "Grid initialization result is required.");
                return;
            }

            try
            {
                if (grid.WorldData == null || grid.WorldData.Cells == null ||
                    grid.WorldData.Cells.Count != WorldGenConstants.SectorCount ||
                    grid.Neighbors == null || grid.Neighbors.Count != WorldGenConstants.SectorCount)
                {
                    AddInvalidGrid(errors);
                    return;
                }

                for (var index = 0; index < WorldGenConstants.SectorCount; index++)
                {
                    var coordinate = WorldGridIndex.ToCoordinate(index);
                    var cell = grid.WorldData.Cells[index];
                    var neighbors = grid.Neighbors[index];
                    if (cell == null || cell.Index != index || cell.Coordinate != coordinate ||
                        neighbors == null || neighbors.Index != index ||
                        neighbors.LeftIndex != WorldGridIndex.GetLeftIndex(index) ||
                        neighbors.RightIndex != WorldGridIndex.GetRightIndex(index) ||
                        neighbors.UpIndex != WorldGridIndex.GetUpIndex(index) ||
                        neighbors.DownIndex != WorldGridIndex.GetDownIndex(index))
                    {
                        AddInvalidGrid(errors);
                        return;
                    }
                }
            }
            catch (Exception)
            {
                AddInvalidGrid(errors);
            }
        }

        private static void ValidateProfiles(
            WorldProfileDefinition worldProfile,
            GenerationProfileDefinition generationProfile,
            ICollection<SiteCandidateEnumerationError> errors)
        {
            if (worldProfile == null)
            {
                Add(errors, SiteCandidateEnumerationErrorCode.MissingWorldProfile, string.Empty,
                    "World profile is required.");
            }
            else
            {
                if (!worldProfile.Active)
                    Add(errors, SiteCandidateEnumerationErrorCode.InactiveProfile,
                        CanonicalOrEmpty(worldProfile.WorldProfileId), "World profile must be active.");
                if (!HasFixedWorldDimensions(worldProfile))
                    Add(errors, SiteCandidateEnumerationErrorCode.InvalidWorldDimensions,
                        CanonicalOrEmpty(worldProfile.WorldProfileId), "World profile dimensions must match the fixed world grid.");
            }

            if (generationProfile == null)
            {
                Add(errors, SiteCandidateEnumerationErrorCode.MissingGenerationProfile, string.Empty,
                    "Generation profile is required.");
            }
            else
            {
                if (!generationProfile.Active)
                    Add(errors, SiteCandidateEnumerationErrorCode.InactiveProfile,
                        CanonicalOrEmpty(generationProfile.GenerationProfileId), "Generation profile must be active.");
                if (generationProfile.StartEdgeRingMin < 0 ||
                    generationProfile.StartEdgeRingMin > generationProfile.StartEdgeRingMax ||
                    generationProfile.StartEdgeRingMax > 6 ||
                    generationProfile.StartEdgeRingMin != 0 ||
                    generationProfile.StartEdgeRingMax != 1)
                    Add(errors, SiteCandidateEnumerationErrorCode.InvalidStartRing,
                        CanonicalOrEmpty(generationProfile.GenerationProfileId), "Generation profile start rings must be exactly 0 through 1.");
            }

            if (worldProfile != null && generationProfile != null &&
                !string.Equals(generationProfile.WorldProfileId, worldProfile.WorldProfileId, StringComparison.Ordinal))
                Add(errors, SiteCandidateEnumerationErrorCode.ProfileWorldMismatch,
                    CanonicalOrEmpty(generationProfile.GenerationProfileId), "Generation profile world identity must match the world profile.");
        }

        private static Dictionary<string, SpecialMapDefinition> ValidateSpecialMaps(
            IEnumerable<SpecialMapDefinition> specialMaps,
            ICollection<SiteCandidateEnumerationError> errors)
        {
            var definitions = new Dictionary<string, SpecialMapDefinition>(StringComparer.Ordinal);
            if (specialMaps == null)
            {
                Add(errors, SiteCandidateEnumerationErrorCode.MissingSpecialMapInput, string.Empty,
                    "Special map input is required.");
                return definitions;
            }

            foreach (var definition in specialMaps)
            {
                if (definition == null)
                {
                    Add(errors, SiteCandidateEnumerationErrorCode.NullSpecialMap, string.Empty,
                        "Special map input cannot contain null.");
                    continue;
                }

                var sourceId = CanonicalOrEmpty(definition.SpecialMapId);
                if (!ReservationValidation.IsCanonicalId(definition.SpecialMapId, false))
                {
                    Add(errors, SiteCandidateEnumerationErrorCode.InvalidSiteDefinition, string.Empty,
                        "Special map ID must be a canonical non-empty ID.");
                    continue;
                }
                if (!definitions.TryAdd(definition.SpecialMapId, definition))
                {
                    Add(errors, SiteCandidateEnumerationErrorCode.DuplicateSpecialMapId, sourceId,
                        "Special map IDs must be unique.");
                }
            }

            foreach (var expected in ExpectedSites)
            {
                if (!definitions.TryGetValue(expected.SourceDefinitionId, out var definition) || !definition.Active)
                {
                    Add(errors, SiteCandidateEnumerationErrorCode.MissingRequiredSite,
                        expected.SourceDefinitionId, "Required active special site is missing.");
                    continue;
                }

                if (!SiteReservationTokenCodec.TryParseKind(definition.SiteRole, out var kind) || kind != expected.Kind)
                    Add(errors, SiteCandidateEnumerationErrorCode.SiteRoleMismatch,
                        expected.SourceDefinitionId, "Required special site role does not match its fixed role.");
                if (definition.RequiredCount != 1)
                    Add(errors, SiteCandidateEnumerationErrorCode.InvalidRequiredCount,
                        expected.SourceDefinitionId, "Required special site count must be exactly one.");
                if (!IsValidDefinition(definition))
                    Add(errors, SiteCandidateEnumerationErrorCode.InvalidSiteDefinition,
                        expected.SourceDefinitionId, "Special site definition has invalid dimensions, biome, distance, or route types.");
            }

            foreach (var pair in definitions)
            {
                var definition = pair.Value;
                if (!definition.Active || IsExpected(pair.Key)) continue;
                if (!SiteReservationTokenCodec.TryParseKind(definition.SiteRole, out var kind))
                {
                    if (definition.RequiredCount > 0)
                        Add(errors, SiteCandidateEnumerationErrorCode.UnexpectedRequiredSite,
                            pair.Key, "Active required special site has an unknown role.");
                    continue;
                }
                if (kind == SiteReservationKind.Village) continue;
                if ((kind == SiteReservationKind.Boss || kind == SiteReservationKind.Forge ||
                     kind == SiteReservationKind.CoreResource) && definition.RequiredCount > 0)
                    Add(errors, SiteCandidateEnumerationErrorCode.UnexpectedRequiredSite,
                        pair.Key, "Unexpected active required special site is not part of the fixed catalog.");
            }

            return definitions;
        }

        private static SiteCandidateGroup CreateGroup(
            SiteReservationKind kind,
            string sourceDefinitionId,
            int minimumEdgeRing,
            int maximumEdgeRing)
        {
            var candidates = new List<SiteOriginCandidate>();
            for (var originIndex = 0; originIndex < WorldGenConstants.SectorCount; originIndex++)
            {
                var origin = WorldGridIndex.ToCoordinate(originIndex);
                var edgeRing = SiteOriginCandidate.CalculateEdgeRing(origin);
                if (edgeRing < minimumEdgeRing || edgeRing > maximumEdgeRing) continue;
                candidates.Add(new SiteOriginCandidate(
                    kind,
                    sourceDefinitionId,
                    0,
                    origin,
                    originIndex,
                    edgeRing,
                    candidates.Count));
            }
            return new SiteCandidateGroup(kind, sourceDefinitionId, 0, candidates);
        }

        private static bool HasFixedWorldDimensions(WorldProfileDefinition profile)
        {
            return profile.WidthTiles == WorldGenConstants.WorldWidthTiles &&
                   profile.HeightTiles == WorldGenConstants.WorldHeightTiles &&
                   profile.SectorWidthTiles == WorldGenConstants.SectorWidthTiles &&
                   profile.SectorHeightTiles == WorldGenConstants.SectorHeightTiles &&
                   profile.SectorCols == WorldGenConstants.SectorColumns &&
                   profile.SectorRows == WorldGenConstants.SectorRows &&
                   profile.MicroWidthTiles == WorldGenConstants.MicroChunkWidthTiles &&
                   profile.MicroHeightTiles == WorldGenConstants.MicroChunkHeightTiles &&
                   profile.MicroColsPerSector == WorldGenConstants.MicroChunkColumnsPerSector &&
                   profile.MicroRowsPerSector == WorldGenConstants.MicroChunkRowsPerSector;
        }

        private static bool IsValidDefinition(SpecialMapDefinition definition)
        {
            if (!ReservationValidation.IsCanonicalId(definition.PrimaryBiomeId, false) ||
                definition.FootprintWidthSectors < 1 || definition.FootprintWidthSectors > WorldGenConstants.SectorColumns ||
                definition.FootprintHeightSectors < 1 || definition.FootprintHeightSectors > WorldGenConstants.SectorRows ||
                definition.MinGraphDistanceFromStart < 0 || definition.MinGraphDistanceToOtherCoreSites < 0 ||
                definition.AllowedEntryRouteTypes == null || definition.AllowedEntryRouteTypes.Count == 0)
                return false;

            var seen = new HashSet<int>();
            foreach (var routeType in definition.AllowedEntryRouteTypes)
            {
                if (routeType < 1 || routeType > 3 || !seen.Add(routeType)) return false;
            }
            return true;
        }

        private static bool IsExpected(string sourceDefinitionId)
        {
            foreach (var expected in ExpectedSites)
            {
                if (string.Equals(expected.SourceDefinitionId, sourceDefinitionId, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static string CanonicalOrEmpty(string value)
        {
            return ReservationValidation.IsCanonicalId(value, false) ? value : string.Empty;
        }

        private static void AddInvalidGrid(ICollection<SiteCandidateEnumerationError> errors)
        {
            Add(errors, SiteCandidateEnumerationErrorCode.InvalidGrid, string.Empty,
                "Grid must contain the exact fixed cells, indices, coordinates, and topology.");
        }

        private static void Add(
            ICollection<SiteCandidateEnumerationError> errors,
            SiteCandidateEnumerationErrorCode code,
            string sourceDefinitionId,
            string message)
        {
            errors.Add(new SiteCandidateEnumerationError(code, sourceDefinitionId, message));
        }

        private static void SortErrors(List<SiteCandidateEnumerationError> errors)
        {
            errors.Sort((left, right) =>
            {
                var source = string.Compare(left.SourceDefinitionId, right.SourceDefinitionId, StringComparison.Ordinal);
                if (source != 0) return source;
                var code = left.ErrorCode.CompareTo(right.ErrorCode);
                return code != 0
                    ? code
                    : string.Compare(left.Message, right.Message, StringComparison.Ordinal);
            });
        }

        private sealed class ExpectedSite
        {
            public ExpectedSite(SiteReservationKind kind, string sourceDefinitionId)
            {
                Kind = kind;
                SourceDefinitionId = sourceDefinitionId;
            }

            public SiteReservationKind Kind { get; }
            public string SourceDefinitionId { get; }
        }
    }
}
