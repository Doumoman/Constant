using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.MapAuthoring.Microchunks
{
    public sealed class MicrochunkSocketAndSlotValidationSummary
    {
        public MicrochunkSocketEdgeValidationResult SocketResult { get; }
        public MicrochunkObjectSlotValidationResult ObjectSlotResult { get; }
        public bool Success => SocketResult.Success && ObjectSlotResult.Success;
        public int IssueCount => SocketResult.IssueCount + ObjectSlotResult.IssueCount;

        public MicrochunkSocketAndSlotValidationSummary(
            MicrochunkSocketEdgeValidationResult socketResult,
            MicrochunkObjectSlotValidationResult objectSlotResult)
        {
            SocketResult = socketResult ?? throw new ArgumentNullException(nameof(socketResult));
            ObjectSlotResult = objectSlotResult ?? throw new ArgumentNullException(nameof(objectSlotResult));
        }
    }

    public sealed class MicrochunkSocketAndSlotEditorViewModel
    {
        public MicrochunkAuthoringGridViewModel Grid { get; }
        public MicrochunkSocketAuthoringCollection SocketAuthoring { get; }
        public MicrochunkObjectSlotAuthoringCollection ObjectSlotAuthoring { get; }

        public MicrochunkSocketAndSlotEditorViewModel()
            : this(
                new MicrochunkAuthoringGridViewModel(),
                new MicrochunkSocketAuthoringCollection(),
                new MicrochunkObjectSlotAuthoringCollection())
        {
        }

        public MicrochunkSocketAndSlotEditorViewModel(
            MicrochunkAuthoringGridViewModel grid,
            MicrochunkSocketAuthoringCollection socketAuthoring,
            MicrochunkObjectSlotAuthoringCollection objectSlotAuthoring)
        {
            Grid = grid ?? throw new ArgumentNullException(nameof(grid));
            SocketAuthoring = socketAuthoring ?? throw new ArgumentNullException(nameof(socketAuthoring));
            ObjectSlotAuthoring = objectSlotAuthoring ?? throw new ArgumentNullException(nameof(objectSlotAuthoring));
        }

        public IReadOnlyDictionary<string, MicrochunkSocketBandDefinition> ProjectBandsById()
        {
            return SocketAuthoring.ProjectBandsById();
        }

        public IReadOnlyList<MicrochunkSocketDefinition> ProjectSockets()
        {
            return SocketAuthoring.ProjectSockets();
        }

        public IReadOnlyList<MicrochunkObjectSlotDefinition> ProjectObjectSlots()
        {
            return ObjectSlotAuthoring.ProjectDefinitions();
        }

        public MicrochunkDefinition ProjectDefinition()
        {
            return new MicrochunkDefinition(
                new MicrochunkId(MicrochunkAuthoringGridViewModel.PreviewMicrochunkId),
                "Editor Socket and Slot Preview",
                MicrochunkConstants.WidthTiles,
                MicrochunkConstants.HeightTiles,
                MicrochunkUsageClass.Traversal,
                Array.Empty<string>(),
                Array.Empty<string>(),
                new[] { MicrochunkTransform.R0 },
                1,
                0,
                0,
                0,
                true,
                "PREFAB_MC_GRAY",
                true,
                "In-memory socket and object-slot editor projection only.",
                Grid.ProjectTileCells(),
                ProjectSockets(),
                ProjectObjectSlots());
        }

        public MicrochunkSocketEdgeValidationResult ValidateSocketEdges(
            IReadOnlyDictionary<string, MicrochunkEdgeSignatureDefinition> signaturesById)
        {
            if (signaturesById == null) throw new ArgumentNullException(nameof(signaturesById));
            return MicrochunkSocketEdgeValidator.ValidateDefinition(
                ProjectDefinition(),
                ProjectBandsById(),
                signaturesById);
        }

        public MicrochunkObjectSlotValidationResult ValidateObjectSlots(
            MicrochunkObjectSlotValidationPolicy policy)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            return MicrochunkObjectSlotValidator.ValidateDefinition(ProjectDefinition(), policy);
        }

        public MicrochunkSocketAndSlotValidationSummary Validate()
        {
            return new MicrochunkSocketAndSlotValidationSummary(
                ValidateSocketEdges(CreateAuthoringSignatureLookup()),
                ValidateObjectSlots(CreateAuthoringSlotPolicy()));
        }

        public IReadOnlyDictionary<string, MicrochunkEdgeSignatureDefinition> CreateAuthoringSignatureLookup()
        {
            var bands = SocketAuthoring.Bands.ToDictionary(row => row.BandId, StringComparer.Ordinal);
            var signatures = new SortedDictionary<string, MicrochunkEdgeSignatureDefinition>(StringComparer.Ordinal);
            foreach (var socket in SocketAuthoring.Sockets.OrderBy(value => value.SocketId, StringComparer.Ordinal))
            {
                if (signatures.ContainsKey(socket.EdgeSignatureId)) continue;
                var axis = bands.TryGetValue(socket.BandId, out var band)
                    ? MicrochunkSocketBandAuthoringRow.ToRuntimeAxis(band.SideToken)
                    : MicrochunkSocketBandAuthoringRow.ToRuntimeAxis(socket.SideToken);
                signatures.Add(
                    socket.EdgeSignatureId,
                    new MicrochunkEdgeSignatureDefinition(
                        socket.EdgeSignatureId,
                        axis,
                        socket.BandId,
                        MicrochunkSocketAuthoringRow.ParseTraversalKind(socket.TraversalKindToken),
                        0,
                        0,
                        0,
                        MicrochunkSocketAuthoringRow.ParseToolRequirement(socket.ToolRequirementToken),
                        socket.MandatoryAllowed,
                        Array.Empty<string>(),
                        "In-memory editor signature selection."));
            }
            return new ReadOnlyDictionary<string, MicrochunkEdgeSignatureDefinition>(signatures);
        }

        public MicrochunkObjectSlotValidationPolicy CreateAuthoringSlotPolicy()
        {
            var pools = new List<MicrochunkObjectSlotPoolDefinition>();
            foreach (var group in ObjectSlotAuthoring.Rows
                         .GroupBy(row => row.PoolId, StringComparer.Ordinal)
                         .OrderBy(group => group.Key, StringComparer.Ordinal))
            {
                pools.Add(new MicrochunkObjectSlotPoolDefinition(
                    group.Key,
                    group.Select(row => MicrochunkObjectSlotAuthoringRow.ParseCategory(row.CategoryToken)).Distinct(),
                    true,
                    true,
                    "In-memory editor pool selection."));
            }

            var markers = ObjectSlotAuthoring.Rows
                .Select(row => row.RequiredMarkerCode)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            return new MicrochunkObjectSlotValidationPolicy(pools, markers);
        }
    }
}
