using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class InactiveBufferAssigner
    {
        private const int OwnerNone = 0;
        private const int OwnerSite = 1;
        private const int OwnerMandatory = 2;
        private const int OwnerType0 = 3;

        public InactiveBufferAssignmentResult Assign(
            GeneratedWorldData world,
            SiteReservationSnapshot siteReservations,
            BiomePatchValidationPublication biomePublication,
            MandatoryRouteGraph graph,
            MandatoryRouteValidationReport validationReport,
            Type0RouteMaskAssignmentResult type0Assignments,
            OptionalReturnPolicyResult returnPolicies,
            string sourceMandatoryGraphDigest,
            InactiveBufferAssignmentSettings settings)
        {
            var nullErrors = new List<InactiveBufferAssignmentError>();
            AddNull(nullErrors, world, "World");
            AddNull(nullErrors, siteReservations, "SiteReservations");
            AddNull(nullErrors, biomePublication, "BiomePublication");
            AddNull(nullErrors, graph, "MandatoryGraph");
            AddNull(nullErrors, validationReport, "MandatoryValidationReport");
            AddNull(nullErrors, type0Assignments, "Type0Assignments");
            AddNull(nullErrors, returnPolicies, "ReturnPolicies");
            if (nullErrors.Count != 0)
                return Failure(InactiveBufferAssignmentStatus.InvalidInput, nullErrors, string.Empty, string.Empty, string.Empty, string.Empty);

            if (settings == null)
            {
                return Failure(
                    InactiveBufferAssignmentStatus.InvalidSettings,
                    OneError(InactiveBufferAssignmentErrorCode.NullInput, -1, "Settings", "settings", "Assignment settings are required."),
                    string.Empty, string.Empty, string.Empty, string.Empty);
            }
            if (!settings.RequireFullWorldAccounting || !settings.RequireClosedInactiveBoundaries ||
                !settings.ClassifyClaimAdjacentAsDecorativeBoundary)
            {
                return Failure(
                    InactiveBufferAssignmentStatus.InvalidSettings,
                    OneError(InactiveBufferAssignmentErrorCode.SourceMismatch, -1, "Settings", "frozenContract", "All frozen assignment settings must be enabled."),
                    string.Empty, string.Empty, string.Empty, string.Empty);
            }

            var sourceErrors = ValidateSourceChain(
                world, siteReservations, biomePublication, graph, validationReport,
                type0Assignments, returnPolicies, sourceMandatoryGraphDigest);
            if (sourceErrors.Count != 0)
            {
                return Failure(
                    InactiveBufferAssignmentStatus.InvalidSource, sourceErrors,
                    sourceMandatoryGraphDigest,
                    type0Assignments.CanonicalDigest,
                    type0Assignments.SourceGrowthDigest,
                    returnPolicies.CanonicalDigest);
            }

            var owners = new int[WorldGenConstants.SectorCount];
            var siteMembership = new bool[WorldGenConstants.SectorCount];
            var mandatoryMembership = new bool[WorldGenConstants.SectorCount];
            var type0Membership = new bool[WorldGenConstants.SectorCount];
            var accountingErrors = new List<InactiveBufferAssignmentError>();
            var duplicateCount = 0;
            var illegalOverlapCount = 0;
            var siteMandatoryOverlapCount = 0;
            var approvedReservedAdapterOverlapCount = 0;

            foreach (var sector in siteReservations.Sectors)
            {
                if (!sector.IsReserved) continue;
                if (!MarkMembership(siteMembership, sector.Index, "ReservedSite", accountingErrors, ref duplicateCount))
                    continue;
                owners[sector.Index] = OwnerSite;
            }
            foreach (var cell in graph.Cells)
            {
                if (!MarkMembership(mandatoryMembership, cell.SectorIndex, "Mandatory", accountingErrors, ref duplicateCount))
                    continue;
                if (siteMembership[cell.SectorIndex])
                {
                    siteMandatoryOverlapCount++;
                    if (!cell.IsApprovedReservedAdapter)
                    {
                        illegalOverlapCount++;
                        Add(accountingErrors, InactiveBufferAssignmentErrorCode.OwnershipOverlap,
                            cell.SectorIndex, "Mandatory", "reservedAdapter",
                            "Site and mandatory source membership may overlap only at an approved reserved adapter.");
                    }
                    else
                    {
                        approvedReservedAdapterOverlapCount++;
                    }
                    continue;
                }
                owners[cell.SectorIndex] = OwnerMandatory;
            }
            foreach (var assignment in type0Assignments.Assignments)
            {
                if (!MarkMembership(type0Membership, assignment.SectorIndex, "Type0", accountingErrors, ref duplicateCount))
                    continue;
                if (siteMembership[assignment.SectorIndex] || mandatoryMembership[assignment.SectorIndex])
                {
                    illegalOverlapCount++;
                    Add(accountingErrors, InactiveBufferAssignmentErrorCode.OwnershipOverlap,
                        assignment.SectorIndex, "Type0", "sectorIndex",
                        "Type0 source membership cannot overlap site or mandatory membership.");
                    continue;
                }
                owners[assignment.SectorIndex] = OwnerType0;
            }

            if (accountingErrors.Count != 0)
            {
                return Failure(
                    InactiveBufferAssignmentStatus.InvalidAccounting, accountingErrors,
                    sourceMandatoryGraphDigest, type0Assignments.CanonicalDigest,
                    type0Assignments.SourceGrowthDigest, returnPolicies.CanonicalDigest);
            }

            var topologyErrors = ValidateOpenEdges(graph, type0Assignments, owners);
            if (topologyErrors.Count != 0)
            {
                return Failure(
                    InactiveBufferAssignmentStatus.InvalidTopology, topologyErrors,
                    sourceMandatoryGraphDigest, type0Assignments.CanonicalDigest,
                    type0Assignments.SourceGrowthDigest, returnPolicies.CanonicalDigest);
            }

            var assignments = new List<InactiveBufferAssignment>();
            var decorativeCount = 0;
            var interiorCount = 0;
            var worldEdgeCount = 0;
            var protectedToInactiveEdges = 0;
            var inactiveNeighborReferences = 0;
            for (var sectorIndex = 0; sectorIndex < owners.Length; sectorIndex++)
            {
                if (owners[sectorIndex] != OwnerNone) continue;
                var protectedNeighbors = new List<int>();
                var inactiveNeighbors = new List<int>();
                foreach (var neighborIndex in GetNeighbors(sectorIndex))
                {
                    if (neighborIndex == SectorNeighborIndices.NoNeighbor) continue;
                    if (owners[neighborIndex] == OwnerNone) inactiveNeighbors.Add(neighborIndex);
                    else protectedNeighbors.Add(neighborIndex);
                }

                var kind = protectedNeighbors.Count == 0
                    ? InactiveBufferKind.InteriorInactive
                    : InactiveBufferKind.DecorativeBoundary;
                if (kind == InactiveBufferKind.DecorativeBoundary) decorativeCount++;
                else interiorCount++;
                var coord = WorldGridIndex.ToCoordinate(sectorIndex);
                var touchesWorldEdge = coord.X == 0 || coord.X == WorldGenConstants.SectorColumns - 1 ||
                                       coord.Y == 0 || coord.Y == WorldGenConstants.SectorRows - 1;
                if (touchesWorldEdge) worldEdgeCount++;
                protectedToInactiveEdges += protectedNeighbors.Count;
                inactiveNeighborReferences += inactiveNeighbors.Count;
                assignments.Add(new InactiveBufferAssignment(
                    sectorIndex, coord, GeneratedSectorRole.InactiveBuffer, kind,
                    protectedNeighbors, inactiveNeighbors, touchesWorldEdge));
            }

            var protectedCount = owners.Count(value => value != OwnerNone);
            var unassignedCount = WorldGenConstants.SectorCount - protectedCount - assignments.Count;
            if (protectedCount + assignments.Count != WorldGenConstants.SectorCount ||
                unassignedCount != 0 || (inactiveNeighborReferences & 1) != 0)
            {
                return Failure(
                    InactiveBufferAssignmentStatus.InvalidAccounting,
                    OneError(InactiveBufferAssignmentErrorCode.IncompleteAccounting, -1, "Ownership", "worldAccounting", "Protected and inactive ownership must account for every sector exactly once."),
                    sourceMandatoryGraphDigest, type0Assignments.CanonicalDigest,
                    type0Assignments.SourceGrowthDigest, returnPolicies.CanonicalDigest);
            }

            var diagnostics = new InactiveBufferAssignmentDiagnostics(
                world.Cells.Count,
                siteReservations.Reservations.Count,
                siteReservations.Sectors.Count(value => value.IsReserved),
                graph.Cells.Count,
                owners.Count(value => value == OwnerMandatory),
                type0Assignments.Assignments.Count,
                siteMandatoryOverlapCount,
                approvedReservedAdapterOverlapCount,
                protectedCount,
                assignments.Count,
                decorativeCount,
                interiorCount,
                worldEdgeCount,
                protectedToInactiveEdges,
                inactiveNeighborReferences / 2,
                unassignedCount,
                illegalOverlapCount,
                duplicateCount,
                0,
                0,
                0);
            var digest = ComputeDigest(
                assignments, diagnostics, settings, sourceMandatoryGraphDigest,
                type0Assignments.CanonicalDigest, type0Assignments.SourceGrowthDigest,
                returnPolicies.CanonicalDigest);
            return new InactiveBufferAssignmentResult(
                InactiveBufferAssignmentStatus.Completed, assignments, diagnostics,
                Array.Empty<InactiveBufferAssignmentError>(), sourceMandatoryGraphDigest,
                type0Assignments.CanonicalDigest, type0Assignments.SourceGrowthDigest,
                returnPolicies.CanonicalDigest, digest);
        }

        private static List<InactiveBufferAssignmentError> ValidateSourceChain(
            GeneratedWorldData world,
            SiteReservationSnapshot siteReservations,
            BiomePatchValidationPublication biomePublication,
            MandatoryRouteGraph graph,
            MandatoryRouteValidationReport validationReport,
            Type0RouteMaskAssignmentResult type0Assignments,
            OptionalReturnPolicyResult returnPolicies,
            string sourceMandatoryGraphDigest)
        {
            var errors = new List<InactiveBufferAssignmentError>();
            ValidateWorld(world, errors);
            ValidateSite(siteReservations, errors);
            ValidateBiome(biomePublication, errors);

            if (!ReferenceEquals(world, graph.RouteStampedWorld) ||
                !ReferenceEquals(world, validationReport.SourceWorld) ||
                !ReferenceEquals(graph, validationReport.SourceGraph))
                Add(errors, InactiveBufferAssignmentErrorCode.SourceMismatch, -1, "MandatoryGraph", "worldIdentity", "World, graph, and validation report identities must match.");
            if (!ReferenceEquals(siteReservations, graph.SourceTerminalSet.SourceSiteSnapshot))
                Add(errors, InactiveBufferAssignmentErrorCode.SourceMismatch, -1, "SiteReservations", "graphSource", "Site reservations must match the mandatory graph source.");
            if (!ReferenceEquals(biomePublication, graph.SourceTerminalSet.SourceBiomePublication))
                Add(errors, InactiveBufferAssignmentErrorCode.SourceMismatch, -1, "BiomePublication", "graphSource", "Biome publication must match the mandatory graph source.");
            if (!validationReport.IsValid || graph.Nodes.Count != graph.NodeCount ||
                graph.Edges.Count != graph.DirectedEdgeCount || graph.Cells.Count != graph.CellCount ||
                graph.DirectedEdgeCount != graph.UndirectedEdgeCount * 2)
                Add(errors, InactiveBufferAssignmentErrorCode.InvalidMandatoryGraph, -1, "MandatoryGraph", "validation", "Mandatory graph validation and accounting must be complete.");

            var graphSectors = new HashSet<int>();
            foreach (var cell in graph.Cells)
            {
                if (cell == null || cell.SectorIndex < 0 || cell.SectorIndex >= WorldGenConstants.SectorCount ||
                    cell.Coordinate != WorldGridIndex.ToCoordinate(cell.SectorIndex))
                {
                    Add(errors, InactiveBufferAssignmentErrorCode.InvalidSectorIndex,
                        cell == null ? -1 : cell.SectorIndex, "MandatoryGraph", "cells", "Mandatory graph cells require valid row-major identities.");
                    continue;
                }
                if (!graphSectors.Add(cell.SectorIndex))
                    Add(errors, InactiveBufferAssignmentErrorCode.DuplicateOwnership, cell.SectorIndex, "MandatoryGraph", "cells", "Mandatory graph sector ownership must be unique.");
            }

            if (!InactiveBufferAssignmentResult.IsCanonicalIdentity(sourceMandatoryGraphDigest))
                Add(errors, InactiveBufferAssignmentErrorCode.InvalidDigest, -1, "MandatoryGraph", "canonicalDigest", "Mandatory graph digest must be a canonical non-empty identity.");
            if (type0Assignments.Status != Type0RouteMaskAssignmentStatus.Completed || !type0Assignments.IsSuccess ||
                type0Assignments.SourceSnapshot == null)
                Add(errors, InactiveBufferAssignmentErrorCode.InvalidStatus, -1, "Type0Assignments", "status", "Type0 assignments must be completed.");
            if (!InactiveBufferAssignmentResult.IsLowerHexDigest(type0Assignments.CanonicalDigest) ||
                !InactiveBufferAssignmentResult.IsLowerHexDigest(type0Assignments.SourceGrowthDigest))
                Add(errors, InactiveBufferAssignmentErrorCode.InvalidDigest, -1, "Type0Assignments", "digests", "Type0 and growth digests must be lowercase SHA-256.");

            if (type0Assignments.SourceSnapshot != null)
            {
                if (!string.Equals(type0Assignments.SourceSnapshot.SourceMandatoryGraphDigest, sourceMandatoryGraphDigest, StringComparison.Ordinal))
                    Add(errors, InactiveBufferAssignmentErrorCode.SourceMismatch, -1, "Type0Assignments", "mandatoryGraphDigest", "Type0 mandatory graph digest must match the supplied graph digest.");
                if (!SetEquals(graphSectors, type0Assignments.SourceSnapshot.MandatoryRouteSectorIndices))
                    Add(errors, InactiveBufferAssignmentErrorCode.SourceMismatch, -1, "Type0Assignments", "mandatorySectors", "Type0 mandatory sector identity must match the graph.");
                ValidateType0(type0Assignments, errors);
            }

            if (returnPolicies.Status != OptionalReturnPolicyResolutionStatus.Completed || !returnPolicies.IsSuccess)
                Add(errors, InactiveBufferAssignmentErrorCode.InvalidStatus, -1, "ReturnPolicies", "status", "Return policies must be completed.");
            if (!InactiveBufferAssignmentResult.IsLowerHexDigest(returnPolicies.CanonicalDigest) ||
                !InactiveBufferAssignmentResult.IsLowerHexDigest(returnPolicies.SourceType0AssignmentDigest) ||
                !InactiveBufferAssignmentResult.IsLowerHexDigest(returnPolicies.SourceGrowthDigest))
                Add(errors, InactiveBufferAssignmentErrorCode.InvalidDigest, -1, "ReturnPolicies", "digests", "Return-policy source-chain digests must be lowercase SHA-256.");
            if (!string.Equals(returnPolicies.SourceType0AssignmentDigest, type0Assignments.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(returnPolicies.SourceGrowthDigest, type0Assignments.SourceGrowthDigest, StringComparison.Ordinal))
                Add(errors, InactiveBufferAssignmentErrorCode.SourceMismatch, -1, "ReturnPolicies", "sourceChain", "Return-policy Type0 and growth digests must match their sources.");
            ValidateReturns(type0Assignments, returnPolicies, errors);

            return errors;
        }

        private static void ValidateWorld(GeneratedWorldData world, List<InactiveBufferAssignmentError> errors)
        {
            if (WorldGenConstants.SectorColumns != 13 || WorldGenConstants.SectorRows != 13 ||
                WorldGenConstants.SectorCount != 169 || world.Cells.Count != WorldGenConstants.SectorCount)
            {
                Add(errors, InactiveBufferAssignmentErrorCode.InvalidWorld, -1, "World", "dimensions", "World must be the approved 13x13, 169-sector grid.");
                return;
            }
            var indices = new HashSet<int>();
            var coordinates = new HashSet<SectorCoord>();
            for (var ordinal = 0; ordinal < world.Cells.Count; ordinal++)
            {
                var cell = world.Cells[ordinal];
                if (cell == null || cell.Index != ordinal || cell.Coordinate != WorldGridIndex.ToCoordinate(ordinal) ||
                    !indices.Add(cell.Index) || !coordinates.Add(cell.Coordinate))
                    Add(errors, InactiveBufferAssignmentErrorCode.InvalidWorld, ordinal, "World", "cells", "World cells must have unique row-major index and coordinate identities.");
            }
        }

        private static void ValidateSite(SiteReservationSnapshot site, List<InactiveBufferAssignmentError> errors)
        {
            if (site.Reservations.Count == 0 || site.Sectors.Count != WorldGenConstants.SectorCount ||
                site.EntryAnchors.Count == 0 || site.CoreBiomeSeeds.Count == 0)
                Add(errors, InactiveBufferAssignmentErrorCode.InvalidSiteReservation, -1, "SiteReservations", "accounting", "Site reservation publication is incomplete.");
            var reserved = new HashSet<int>();
            for (var index = 0; index < site.Sectors.Count; index++)
            {
                var sector = site.Sectors[index];
                if (sector == null || sector.Index != index || sector.Coordinate != WorldGridIndex.ToCoordinate(index))
                    Add(errors, InactiveBufferAssignmentErrorCode.InvalidSiteReservation, index, "SiteReservations", "sectors", "Site sector identity must be row-major.");
                else if (sector.IsReserved && !reserved.Add(sector.Index))
                    Add(errors, InactiveBufferAssignmentErrorCode.DuplicateOwnership, sector.Index, "SiteReservations", "sectors", "Reserved sector ownership must be unique.");
            }
        }

        private static void ValidateBiome(BiomePatchValidationPublication biome, List<InactiveBufferAssignmentError> errors)
        {
            if (biome.SourceExport == null || biome.Snapshot == null || biome.WorldWithBiomeAssignments == null ||
                biome.Diagnostics == null || biome.WorldWithBiomeAssignments.Cells.Count != WorldGenConstants.SectorCount)
            {
                Add(errors, InactiveBufferAssignmentErrorCode.InvalidBiomePublication, -1, "BiomePublication", "publication", "Biome validation publication is incomplete.");
                return;
            }
            for (var index = 0; index < biome.WorldWithBiomeAssignments.Cells.Count; index++)
            {
                var cell = biome.WorldWithBiomeAssignments.Cells[index];
                if (cell.Index != index || cell.Coordinate != WorldGridIndex.ToCoordinate(index))
                    Add(errors, InactiveBufferAssignmentErrorCode.InvalidBiomePublication, index, "BiomePublication", "world", "Biome world identities must remain row-major.");
            }
        }

        private static void ValidateType0(
            Type0RouteMaskAssignmentResult type0,
            List<InactiveBufferAssignmentError> errors)
        {
            if (type0.Assignments.Count != type0.Diagnostics.AssignmentCount ||
                type0.Assignments.Count != type0.SourceSnapshot.Cells.Count ||
                type0.SourceSnapshot.Regions.Count != type0.Diagnostics.SourceRegionCount ||
                type0.RngDrawCount != 0 || type0.Diagnostics.SourceMutationCount != 0)
                Add(errors, InactiveBufferAssignmentErrorCode.InvalidType0Assignment, -1, "Type0Assignments", "accounting", "Type0 source accounting must be complete and side-effect free.");

            var sourceBySector = new Dictionary<int, OptionalRegionCell>();
            foreach (var cell in type0.SourceSnapshot.Cells)
            {
                if (!sourceBySector.TryAdd(cell.SectorIndex, cell))
                    Add(errors, InactiveBufferAssignmentErrorCode.DuplicateOwnership, cell.SectorIndex, "Type0Assignments", "sourceCells", "Type0 source sectors must be unique.");
            }
            var assignmentSectors = new HashSet<int>();
            foreach (var assignment in type0.Assignments)
            {
                if (assignment == null || assignment.SectorIndex < 0 || assignment.SectorIndex >= WorldGenConstants.SectorCount ||
                    assignment.Sector != WorldGridIndex.ToCoordinate(assignment.SectorIndex))
                {
                    Add(errors, InactiveBufferAssignmentErrorCode.InvalidSectorIndex,
                        assignment == null ? -1 : assignment.SectorIndex, "Type0Assignments", "assignments", "Type0 assignment sector identity is invalid.");
                    continue;
                }
                if (!assignmentSectors.Add(assignment.SectorIndex))
                    Add(errors, InactiveBufferAssignmentErrorCode.DuplicateOwnership, assignment.SectorIndex, "Type0Assignments", "assignments", "Type0 assignment sectors must be unique.");
                if (!sourceBySector.TryGetValue(assignment.SectorIndex, out var source) ||
                    source.RegionId != assignment.RegionId || source.Sector != assignment.Sector)
                    Add(errors, InactiveBufferAssignmentErrorCode.SourceMismatch, assignment.SectorIndex, "Type0Assignments", "sourceCells", "Type0 assignment identity must match its source cell.");
            }
        }

        private static void ValidateReturns(
            Type0RouteMaskAssignmentResult type0,
            OptionalReturnPolicyResult returns,
            List<InactiveBufferAssignmentError> errors)
        {
            if (type0.SourceSnapshot == null) return;
            if (returns.Assignments.Count != type0.SourceSnapshot.Regions.Count ||
                returns.Diagnostics.AssignmentCount != returns.Assignments.Count ||
                returns.Diagnostics.ReturnableCellCount != type0.Assignments.Count ||
                returns.Diagnostics.NonReturnableCellCount != 0 || returns.RngDrawCount != 0 ||
                returns.Diagnostics.SourceMutationCount != 0)
                Add(errors, InactiveBufferAssignmentErrorCode.InvalidReturnPolicy, -1, "ReturnPolicies", "accounting", "Return policies must cover every Type0 region and cell.");

            var regions = type0.SourceSnapshot.Regions.ToDictionary(value => value.RegionId);
            var seen = new HashSet<OptionalRegionId>();
            foreach (var assignment in returns.Assignments)
            {
                if (assignment == null || !seen.Add(assignment.RegionId) ||
                    !regions.TryGetValue(assignment.RegionId, out var region) ||
                    assignment.ReturnableCellCount != region.Cells.Count ||
                    assignment.RequiresReturnDevice || !assignment.UsesSameOpenedAttachmentBoundary)
                    Add(errors, InactiveBufferAssignmentErrorCode.InvalidReturnPolicy,
                        assignment == null ? -1 : assignment.AttachmentEntrySectorIndex,
                        "ReturnPolicies", "assignments", "Return assignment region identity must match Type0 one-to-one.");
            }
        }

        private static List<InactiveBufferAssignmentError> ValidateOpenEdges(
            MandatoryRouteGraph graph,
            Type0RouteMaskAssignmentResult type0,
            int[] owners)
        {
            var errors = new List<InactiveBufferAssignmentError>();
            foreach (var cell in graph.Cells)
            {
                // Approved reserved adapters intentionally expose site/exit-facing sockets that are
                // outside the mandatory protected projection. They are validated by the mandatory
                // graph source-chain and are not ordinary mandatory-to-inactive route openings.
                if (cell.IsApprovedReservedAdapter)
                    continue;
                ValidateOpenEdge(errors, owners, cell.SectorIndex, cell.OpenLeft, 0, "Mandatory", "openLeft");
                ValidateOpenEdge(errors, owners, cell.SectorIndex, cell.OpenRight, 1, "Mandatory", "openRight");
                ValidateOpenEdge(errors, owners, cell.SectorIndex, cell.OpenUp, 2, "Mandatory", "openUp");
                ValidateOpenEdge(errors, owners, cell.SectorIndex, cell.OpenDown, 3, "Mandatory", "openDown");
            }
            foreach (var assignment in type0.Assignments)
            {
                ValidateOpenEdge(errors, owners, assignment.SectorIndex, assignment.OpenMask.OpenLeft, 0, "Type0", "openLeft");
                ValidateOpenEdge(errors, owners, assignment.SectorIndex, assignment.OpenMask.OpenRight, 1, "Type0", "openRight");
                ValidateOpenEdge(errors, owners, assignment.SectorIndex, assignment.OpenMask.OpenUp, 2, "Type0", "openUp");
                ValidateOpenEdge(errors, owners, assignment.SectorIndex, assignment.OpenMask.OpenDown, 3, "Type0", "openDown");
            }
            return errors;
        }

        private static void ValidateOpenEdge(
            List<InactiveBufferAssignmentError> errors,
            int[] owners,
            int sectorIndex,
            bool open,
            int direction,
            string owner,
            string field)
        {
            if (!open) return;
            var neighbor = GetNeighbors(sectorIndex)[direction];
            if (neighbor != SectorNeighborIndices.NoNeighbor && owners[neighbor] == OwnerNone)
                Add(errors, InactiveBufferAssignmentErrorCode.OpenEdgeToInactive, sectorIndex, owner, field, "An open route edge cannot target an inactive sector.");
        }

        private static bool MarkMembership(
            bool[] membership,
            int sectorIndex,
            string ownerName,
            List<InactiveBufferAssignmentError> errors,
            ref int duplicateCount)
        {
            if (sectorIndex < 0 || sectorIndex >= membership.Length)
            {
                Add(errors, InactiveBufferAssignmentErrorCode.InvalidSectorIndex, -1, ownerName, "sectorIndex", "Protected ownership sector index is invalid.");
                return false;
            }
            if (membership[sectorIndex])
            {
                duplicateCount++;
                Add(errors, InactiveBufferAssignmentErrorCode.DuplicateOwnership, sectorIndex, ownerName, "sectorIndex", "Protected ownership contains a duplicate sector.");
                return false;
            }
            membership[sectorIndex] = true;
            return true;
        }

        private static int[] GetNeighbors(int sectorIndex)
        {
            return new[]
            {
                WorldGridIndex.GetLeftIndex(sectorIndex),
                WorldGridIndex.GetRightIndex(sectorIndex),
                WorldGridIndex.GetUpIndex(sectorIndex),
                WorldGridIndex.GetDownIndex(sectorIndex)
            };
        }

        private static string ComputeDigest(
            IReadOnlyList<InactiveBufferAssignment> assignments,
            InactiveBufferAssignmentDiagnostics diagnostics,
            InactiveBufferAssignmentSettings settings,
            string mandatoryDigest,
            string type0Digest,
            string growthDigest,
            string returnDigest)
        {
            var text = new StringBuilder();
            text.Append("mandatory=").Append(mandatoryDigest).Append('\n');
            text.Append("type0=").Append(type0Digest).Append('\n');
            text.Append("growth=").Append(growthDigest).Append('\n');
            text.Append("return=").Append(returnDigest).Append('\n');
            text.Append("settings=").Append(settings.RequireFullWorldAccounting ? '1' : '0').Append('|')
                .Append(settings.RequireClosedInactiveBoundaries ? '1' : '0').Append('|')
                .Append(settings.ClassifyClaimAdjacentAsDecorativeBoundary ? '1' : '0').Append('\n');
            foreach (var assignment in assignments)
            {
                text.Append("assignment=")
                    .Append(assignment.SectorIndex.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(assignment.Coord.X.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(assignment.Coord.Y.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append("INACTIVE_BUFFER|").Append(((int)assignment.Kind).ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(string.Join(",", assignment.ProtectedNeighborSectorIndices)).Append('|')
                    .Append(string.Join(",", assignment.InactiveNeighborSectorIndices)).Append('|')
                    .Append(assignment.TouchesWorldEdge ? '1' : '0').Append('\n');
            }
            text.Append("diagnostics=")
                .Append(diagnostics.WorldSectorCount).Append('|').Append(diagnostics.SiteReservationCount).Append('|')
                .Append(diagnostics.ReservedSiteSectorCount).Append('|').Append(diagnostics.MandatoryRouteCellCount).Append('|')
                .Append(diagnostics.MandatoryExclusiveSectorCount).Append('|').Append(diagnostics.Type0CellCount).Append('|')
                .Append(diagnostics.SiteMandatoryOverlapCount).Append('|')
                .Append(diagnostics.ApprovedReservedAdapterOverlapCount).Append('|')
                .Append(diagnostics.ProtectedUnionCount).Append('|')
                .Append(diagnostics.AssignmentCount).Append('|').Append(diagnostics.DecorativeBoundaryCount).Append('|')
                .Append(diagnostics.InteriorInactiveCount).Append('|').Append(diagnostics.WorldEdgeInactiveCount).Append('|')
                .Append(diagnostics.ProtectedToInactiveCardinalEdgeCount).Append('|')
                .Append(diagnostics.InactiveToInactiveUndirectedEdgeCount).Append('|')
                .Append(diagnostics.UnassignedSectorCount).Append('|').Append(diagnostics.IllegalOwnershipOverlapCount).Append('|')
                .Append(diagnostics.DuplicateSectorCount).Append('|').Append(diagnostics.OpenEdgeToInactiveCount).Append('|')
                .Append(diagnostics.RngDrawCount).Append('|').Append(diagnostics.SourceMutationCount);
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()));
                var result = new StringBuilder(hash.Length * 2);
                foreach (var value in hash) result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }

        private static bool SetEquals(HashSet<int> expected, IEnumerable<int> actual)
        {
            var values = new HashSet<int>(actual);
            return values.Count == expected.Count && values.SetEquals(expected);
        }

        private static void AddNull(List<InactiveBufferAssignmentError> errors, object value, string owner)
        {
            if (value == null)
                Add(errors, InactiveBufferAssignmentErrorCode.NullInput, -1, owner, "input", "Required source input is null.");
        }

        private static void Add(
            List<InactiveBufferAssignmentError> errors,
            InactiveBufferAssignmentErrorCode code,
            int sectorIndex,
            string owner,
            string field,
            string message)
        {
            errors.Add(new InactiveBufferAssignmentError(code, sectorIndex, owner, field, message));
        }

        private static List<InactiveBufferAssignmentError> OneError(
            InactiveBufferAssignmentErrorCode code,
            int sectorIndex,
            string owner,
            string field,
            string message)
        {
            return new List<InactiveBufferAssignmentError>
            {
                new InactiveBufferAssignmentError(code, sectorIndex, owner, field, message)
            };
        }

        private static InactiveBufferAssignmentResult Failure(
            InactiveBufferAssignmentStatus status,
            IEnumerable<InactiveBufferAssignmentError> errors,
            string mandatoryDigest,
            string type0Digest,
            string growthDigest,
            string returnDigest)
        {
            var diagnostics = new InactiveBufferAssignmentDiagnostics(
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            return new InactiveBufferAssignmentResult(
                status, Array.Empty<InactiveBufferAssignment>(), diagnostics, errors,
                mandatoryDigest, type0Digest, growthDigest, returnDigest, string.Empty);
        }
    }
}
