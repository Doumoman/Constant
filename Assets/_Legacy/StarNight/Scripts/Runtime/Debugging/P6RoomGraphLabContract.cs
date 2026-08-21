#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Generation.P6;
using StarNight.Rooms;
using UnityEngine;

namespace StarNight.Debugging
{
    [Serializable]
    public struct P6RoomGraphLabPlacement
    {
        [SerializeField] private int nodeId;
        [SerializeField] private string prefabId;
        [SerializeField] private RectInt macroBounds;
        [SerializeField] private RoomRole role;
        [SerializeField] private bool onMainPath;
        [SerializeField] private GameObject sourcePrefab;
        [SerializeField] private RoomTemplate2D instance;

        public int NodeId => nodeId;
        public string PrefabId => prefabId;
        public RectInt MacroBounds => macroBounds;
        public RoomRole Role => role;
        public bool OnMainPath => onMainPath;
        public GameObject SourcePrefab => sourcePrefab;
        public RoomTemplate2D Instance => instance;

        public P6RoomGraphLabPlacement(
            int id,
            string roomPrefabId,
            RectInt bounds,
            RoomRole assignedRole,
            bool isOnMainPath,
            GameObject source,
            RoomTemplate2D roomInstance)
        {
            nodeId = id;
            prefabId = roomPrefabId ?? string.Empty;
            macroBounds = bounds;
            role = assignedRole;
            onMainPath = isOnMainPath;
            sourcePrefab = source;
            instance = roomInstance;
        }
    }

    [Serializable]
    public struct P6RoomGraphLabEdge
    {
        [SerializeField] private int firstNodeId;
        [SerializeField] private int secondNodeId;
        [SerializeField] private int firstAccessId;
        [SerializeField] private int secondAccessId;
        [SerializeField] private P6EdgeKind kind;
        [SerializeField] private Transform visual;

        public int FirstNodeId => firstNodeId;
        public int SecondNodeId => secondNodeId;
        public int FirstAccessId => firstAccessId;
        public int SecondAccessId => secondAccessId;
        public P6EdgeKind Kind => kind;
        public Transform Visual => visual;

        public P6RoomGraphLabEdge(
            int first,
            int second,
            int firstAccess,
            int secondAccess,
            P6EdgeKind edgeKind,
            Transform edgeVisual)
        {
            if (first <= second)
            {
                firstNodeId = first;
                secondNodeId = second;
                firstAccessId = firstAccess;
                secondAccessId = secondAccess;
            }
            else
            {
                firstNodeId = second;
                secondNodeId = first;
                firstAccessId = secondAccess;
                secondAccessId = firstAccess;
            }

            kind = edgeKind;
            visual = edgeVisual;
        }
    }

    [Serializable]
    public struct P6RoomGraphLabAccess
    {
        [SerializeField] private int accessId;
        [SerializeField] private int nodeId;
        [SerializeField] private string socketId;
        [SerializeField] private P6SocketSides socketSide;
        [SerializeField] private Vector2Int cellOffset;
        [SerializeField] private Vector2Int openingSize;
        [SerializeField] private RoomTraversalType traversalType;
        [SerializeField] private bool mainRouteAllowed;
        [SerializeField] private Vector2Int validationAnchor;
        [SerializeField] private Vector2Int socketCell;
        [SerializeField] private Vector2Int portalCell;
        [SerializeField] private Transform visual;

        public int AccessId => accessId;
        public int NodeId => nodeId;
        public string SocketId => socketId;
        public P6SocketSides SocketSide => socketSide;
        public Vector2Int CellOffset => cellOffset;
        public Vector2Int OpeningSize => openingSize;
        public RoomTraversalType TraversalType => traversalType;
        public bool MainRouteAllowed => mainRouteAllowed;
        public Vector2Int ValidationAnchor => validationAnchor;
        public Vector2Int SocketCell => socketCell;
        public Vector2Int PortalCell => portalCell;
        public Transform Visual => visual;
        public bool IsToolFreeMainRouteAccess =>
            mainRouteAllowed
            && openingSize == Vector2Int.one
            && RoomSocketRules.IsMainRouteTraversal(traversalType);

        public P6RoomGraphLabAccess(
            P6RoomAccess access,
            Transform accessVisual)
        {
            if (access == null)
            {
                throw new ArgumentNullException(nameof(access));
            }

            accessId = access.AccessId;
            nodeId = access.NodeId;
            socketId = access.SocketId ?? string.Empty;
            socketSide = access.SocketSide;
            cellOffset = access.CellOffset;
            openingSize = access.OpeningSize;
            traversalType = access.TraversalType;
            mainRouteAllowed = access.MainRouteAllowed;
            validationAnchor = access.ValidationAnchor;
            socketCell = access.SocketCell;
            portalCell = access.PortalCell;
            visual = accessVisual;
        }
    }

    [Serializable]
    public struct P6RoomGraphLabCorridor
    {
        [SerializeField] private int firstNodeId;
        [SerializeField] private int secondNodeId;
        [SerializeField] private int firstAccessId;
        [SerializeField] private int secondAccessId;
        [SerializeField] private Vector2Int[] macroCells;
        [SerializeField] private Transform visual;

        public int FirstNodeId => firstNodeId;
        public int SecondNodeId => secondNodeId;
        public int FirstAccessId => firstAccessId;
        public int SecondAccessId => secondAccessId;
        public IReadOnlyList<Vector2Int> MacroCells => macroCells;
        public IReadOnlyList<Vector2Int> RoutingCells => macroCells;
        public Transform Visual => visual;

        public P6RoomGraphLabCorridor(
            int first,
            int second,
            int firstAccess,
            int secondAccess,
            Vector2Int[] cells,
            Transform corridorVisual)
        {
            if (first <= second)
            {
                firstNodeId = first;
                secondNodeId = second;
                firstAccessId = firstAccess;
                secondAccessId = secondAccess;
            }
            else
            {
                firstNodeId = second;
                secondNodeId = first;
                firstAccessId = secondAccess;
                secondAccessId = firstAccess;
            }

            macroCells = cells ?? Array.Empty<Vector2Int>();
            visual = corridorVisual;
        }
    }

    [DisallowMultipleComponent]
    public sealed class P6RoomGraphLabContract : MonoBehaviour
    {
        public const string LabId =
            "P6_MoonRoomGraphGeneratorLab_X2_VariableRooms_v2";
        public const int CanvasWidth = 8;
        public const int CanvasHeight = 6;
        public const int MinimumRoomCount = 9;
        public const int MaximumRoomCount = 14;
        public const int MaximumPrefabUses = 2;

        [Header("Deterministic X-2 generation")]
        [SerializeField] private string labId = LabId;
        [SerializeField] private RoomPrefabLibrary sourceLibrary;
        [SerializeField] private int fixedSeed;
        [SerializeField] private int acceptedSeed;
        [SerializeField] private P6StageSlot stageSlot = P6StageSlot.X2;
        [SerializeField] private P6StageArchetype archetype;
        [SerializeField] private string generationFingerprint = string.Empty;
        [SerializeField] private int routingScale;
        [SerializeField] private RectInt corridorRoutingBounds;

        [Header("Generated snapshot")]
        [SerializeField] private P6RoomGraphLabPlacement[] placements =
            Array.Empty<P6RoomGraphLabPlacement>();
        [SerializeField] private GameObject[] sourcePrefabs =
            Array.Empty<GameObject>();
        [SerializeField] private string[] sourceRoomIds =
            Array.Empty<string>();
        [SerializeField] private P6RoomGraphLabEdge[] edges =
            Array.Empty<P6RoomGraphLabEdge>();
        [SerializeField] private P6RoomGraphLabCorridor[] corridors =
            Array.Empty<P6RoomGraphLabCorridor>();
        [SerializeField] private P6RoomGraphLabAccess[] accesses =
            Array.Empty<P6RoomGraphLabAccess>();
        [SerializeField] private Transform physicalCorridorRoot;
        [SerializeField]
        private P6PhysicalCorridorModule2D[] physicalCorridorModules =
            Array.Empty<P6PhysicalCorridorModule2D>();
        [SerializeField] private int startNodeId = -1;
        [SerializeField] private int exitNodeId = -1;

        [Header("Generated physical traversal proof")]
        [SerializeField] private bool startExitReachable;
        [SerializeField] private bool optionalRoomsReturnable;
        [SerializeField] private bool closedLoopSatisfied;
        [SerializeField] private bool mainPathLengthSatisfied;
        [SerializeField] private bool prefabUsageSatisfied;
        [SerializeField] private bool packingSatisfied;
        [SerializeField] private bool corridorNetworkConnected;
        [SerializeField] private bool socketClaimsUnique;
        [SerializeField] private bool oneCellOpeningsSatisfied;
        [SerializeField] private bool jumpEnvelopeSatisfied;
        [SerializeField] private bool toolFreeMainPathSatisfied;
        [SerializeField] private bool exitBlockProofSatisfied;
        [SerializeField] private bool physicalTraversalSatisfied;
        [SerializeField] private bool riskyChoiceSatisfied;
        [SerializeField] private bool x3RoleSequenceSatisfied;
        [SerializeField] private bool landmarkPrioritySatisfied;
        [SerializeField] private bool physicalLoopSatisfied;
        [SerializeField] private bool compositeProofSatisfied;

        [Header("Sprite-only visualization")]
        [SerializeField] private Transform moonBackdrop;
        [SerializeField] private Transform startMarker;
        [SerializeField] private Transform exitMarker;
        [SerializeField] private Transform mstOverlay;
        [SerializeField] private Transform loopOverlay;
        [SerializeField] private Transform corridorOverlay;
        [SerializeField] private Transform accessOverlay;
        [SerializeField] private Transform roleOverlay;
        [SerializeField] private Camera stageCamera;
        [SerializeField] private Light directionalLight;

        [Header("Validation")]
        [SerializeField] private string[] issues = Array.Empty<string>();
        [SerializeField, TextArea(3, 16)] private string lastValidation =
            "Not validated.";

        public RoomPrefabLibrary SourceLibrary => sourceLibrary;
        public int FixedSeed => fixedSeed;
        public int AcceptedSeed => acceptedSeed;
        public bool IsX2 => stageSlot == P6StageSlot.X2;
        public P6StageSlot StageSlot => stageSlot;
        public P6StageArchetype Archetype => archetype;
        public string GenerationFingerprint => generationFingerprint;
        public int RoutingScale => routingScale;
        public RectInt CorridorRoutingBounds => corridorRoutingBounds;
        public IReadOnlyList<P6RoomGraphLabPlacement> Placements => placements;
        public IReadOnlyList<GameObject> SourcePrefabs => sourcePrefabs;
        public IReadOnlyList<string> SourceRoomIds => sourceRoomIds;
        public IReadOnlyList<P6RoomGraphLabEdge> Edges => edges;
        public IReadOnlyList<P6RoomGraphLabCorridor> Corridors => corridors;
        public IReadOnlyList<P6RoomGraphLabAccess> Accesses => accesses;
        public Transform PhysicalCorridorRoot => physicalCorridorRoot;
        public IReadOnlyList<P6PhysicalCorridorModule2D>
            PhysicalCorridorModules => physicalCorridorModules;
        public int StartNodeId => startNodeId;
        public int ExitNodeId => exitNodeId;
        public bool StartExitReachable => startExitReachable;
        public bool OptionalRoomsReturnable => optionalRoomsReturnable;
        public bool ClosedLoopSatisfied => closedLoopSatisfied;
        public bool MainPathLengthSatisfied => mainPathLengthSatisfied;
        public bool PrefabUsageSatisfied => prefabUsageSatisfied;
        public bool PackingSatisfied => packingSatisfied;
        public bool CorridorNetworkConnected => corridorNetworkConnected;
        public bool SocketClaimsUnique => socketClaimsUnique;
        public bool OneCellOpeningsSatisfied => oneCellOpeningsSatisfied;
        public bool JumpEnvelopeSatisfied => jumpEnvelopeSatisfied;
        public bool ToolFreeMainPathSatisfied => toolFreeMainPathSatisfied;
        public bool ExitBlockProofSatisfied => exitBlockProofSatisfied;
        public bool PhysicalTraversalSatisfied => physicalTraversalSatisfied;
        public bool RiskyChoiceSatisfied => riskyChoiceSatisfied;
        public bool X3RoleSequenceSatisfied => x3RoleSequenceSatisfied;
        public bool LandmarkPrioritySatisfied => landmarkPrioritySatisfied;
        public bool PhysicalLoopSatisfied => physicalLoopSatisfied;
        public bool CompositeProofSatisfied => compositeProofSatisfied;
        public Transform MoonBackdrop => moonBackdrop;
        public Transform StartMarker => startMarker;
        public Transform ExitMarker => exitMarker;
        public Transform MstOverlay => mstOverlay;
        public Transform LoopOverlay => loopOverlay;
        public Transform CorridorOverlay => corridorOverlay;
        public Transform AccessOverlay => accessOverlay;
        public Transform RoleOverlay => roleOverlay;
        public Camera StageCamera => stageCamera;
        public Light DirectionalLight => directionalLight;
        public string LastValidation => lastValidation;
        public IReadOnlyList<string> Issues => issues;
        public bool ValidationPassed => issues.Length == 0
            && lastValidation == "PASS";

        public void Configure(
            RoomPrefabLibrary library,
            int requestedSeed,
            int resultSeed,
            P6StageArchetype generatedArchetype,
            string fingerprint,
            int generatedRoutingScale,
            RectInt routingBounds,
            P6RoomGraphLabPlacement[] roomPlacements,
            P6RoomGraphLabEdge[] graphEdges,
            P6RoomGraphLabCorridor[] routedCorridors,
            P6RoomGraphLabAccess[] roomAccesses,
            Transform generatedPhysicalCorridorRoot,
            P6PhysicalCorridorModule2D[] generatedPhysicalModules,
            P6ValidationReport validation,
            int start,
            int exit,
            Transform backdrop,
            Transform startVisual,
            Transform exitVisual,
            Transform mstVisuals,
            Transform loopVisuals,
            Transform corridorVisuals,
            Transform accessVisuals,
            Transform roleVisuals,
            Camera cameraComponent,
            Light mainLight)
        {
            labId = LabId;
            sourceLibrary = library;
            fixedSeed = requestedSeed;
            acceptedSeed = resultSeed;
            stageSlot = P6StageSlot.X2;
            archetype = generatedArchetype;
            generationFingerprint = fingerprint ?? string.Empty;
            routingScale = generatedRoutingScale;
            corridorRoutingBounds = routingBounds;
            placements = roomPlacements
                ?? Array.Empty<P6RoomGraphLabPlacement>();
            edges = graphEdges ?? Array.Empty<P6RoomGraphLabEdge>();
            corridors = routedCorridors
                ?? Array.Empty<P6RoomGraphLabCorridor>();
            accesses = roomAccesses
                ?? Array.Empty<P6RoomGraphLabAccess>();
            physicalCorridorRoot = generatedPhysicalCorridorRoot;
            physicalCorridorModules = generatedPhysicalModules
                ?? Array.Empty<P6PhysicalCorridorModule2D>();
            startNodeId = start;
            exitNodeId = exit;
            StoreValidationProof(validation);
            physicalLoopSatisfied =
                ContainsPhysicalGridCycle(corridors);
            compositeProofSatisfied =
                ComputeCompositeProof();
            moonBackdrop = backdrop;
            startMarker = startVisual;
            exitMarker = exitVisual;
            mstOverlay = mstVisuals;
            loopOverlay = loopVisuals;
            corridorOverlay = corridorVisuals;
            accessOverlay = accessVisuals;
            roleOverlay = roleVisuals;
            stageCamera = cameraComponent;
            directionalLight = mainLight;

            sourcePrefabs = new GameObject[placements.Length];
            sourceRoomIds = new string[placements.Length];
            for (int index = 0; index < placements.Length; index++)
            {
                sourcePrefabs[index] = placements[index].SourcePrefab;
                sourceRoomIds[index] = placements[index].PrefabId;
            }
        }

        private void StoreValidationProof(P6ValidationReport validation)
        {
            startExitReachable =
                validation != null && validation.StartExitReachable;
            optionalRoomsReturnable =
                validation != null && validation.OptionalRoomsReturnable;
            closedLoopSatisfied =
                validation != null && validation.ClosedLoopSatisfied;
            mainPathLengthSatisfied =
                validation != null && validation.MainPathLengthSatisfied;
            prefabUsageSatisfied =
                validation != null && validation.PrefabUsageSatisfied;
            packingSatisfied =
                validation != null && validation.PackingSatisfied;
            corridorNetworkConnected =
                validation != null && validation.CorridorNetworkConnected;
            socketClaimsUnique =
                validation != null && validation.SocketClaimsUnique;
            oneCellOpeningsSatisfied =
                validation != null && validation.OneCellOpeningsSatisfied;
            jumpEnvelopeSatisfied =
                validation != null && validation.JumpEnvelopeSatisfied;
            toolFreeMainPathSatisfied =
                validation != null && validation.ToolFreeMainPathSatisfied;
            exitBlockProofSatisfied =
                validation != null && validation.ExitBlockProofSatisfied;
            physicalTraversalSatisfied =
                validation != null && validation.PhysicalTraversalSatisfied;
            riskyChoiceSatisfied =
                validation != null && validation.RiskyChoiceSatisfied;
            x3RoleSequenceSatisfied =
                validation != null && validation.X3RoleSequenceSatisfied;
            landmarkPrioritySatisfied =
                validation != null && validation.LandmarkPrioritySatisfied;
        }

        private bool ComputeCompositeProof()
        {
            return startExitReachable
                && optionalRoomsReturnable
                && closedLoopSatisfied
                && mainPathLengthSatisfied
                && prefabUsageSatisfied
                && packingSatisfied
                && corridorNetworkConnected
                && socketClaimsUnique
                && oneCellOpeningsSatisfied
                && jumpEnvelopeSatisfied
                && toolFreeMainPathSatisfied
                && exitBlockProofSatisfied
                && physicalTraversalSatisfied
                && riskyChoiceSatisfied
                && x3RoleSequenceSatisfied
                && landmarkPrioritySatisfied
                && physicalLoopSatisfied;
        }

        [ContextMenu("Validate P6 Room Graph Lab")]
        public bool RefreshValidation()
        {
            List<string> foundIssues = new List<string>();
            ValidateIdentity(foundIssues);
            ValidatePlacements(foundIssues);
            ValidateGraph(foundIssues);
            ValidateAccesses(foundIssues);
            ValidateCorridors(foundIssues);
            ValidatePhysicalCorridors(foundIssues);
            ValidateProofSnapshot(foundIssues);
            ValidatePresentation(foundIssues);
            RegenerateAndCompare(foundIssues);
            issues = foundIssues.ToArray();
            lastValidation = issues.Length == 0
                ? "PASS"
                : string.Join(Environment.NewLine, issues);
            return issues.Length == 0;
        }

        private void ValidateIdentity(List<string> foundIssues)
        {
            if (labId != LabId)
            {
                foundIssues.Add("P6 Lab id does not match the fixed contract.");
            }

            if (sourceLibrary == null
                || sourceLibrary.Region != RoomRegion.MoonPalace
                || sourceLibrary.RoomPrefabs.Count != 33)
            {
                foundIssues.Add(
                    "The complete 33-room P4 Moon Palace library is required.");
            }

            if (stageSlot != P6StageSlot.X2)
            {
                foundIssues.Add("The fixed P6 Lab must generate an X-2 stage.");
            }

            if (routingScale != 2)
            {
                foundIssues.Add(
                    "The P6 Lab must use the routing-scale-2 lattice.");
            }

            RectInt expectedRoutingBounds = new RectInt(
                -1,
                -1,
                CanvasWidth * routingScale + 3,
                CanvasHeight * routingScale + 3);
            if (corridorRoutingBounds != expectedRoutingBounds)
            {
                foundIssues.Add(
                    "The P6 routing bounds do not match the fixed canvas.");
            }

            if (string.IsNullOrWhiteSpace(generationFingerprint))
            {
                foundIssues.Add("A deterministic generation fingerprint is required.");
            }
        }

        private void ValidatePlacements(List<string> foundIssues)
        {
            if (placements == null
                || placements.Length < MinimumRoomCount
                || placements.Length > MaximumRoomCount)
            {
                foundIssues.Add(
                    $"P6 requires {MinimumRoomCount}-{MaximumRoomCount} rooms.");
                return;
            }

            if (sourcePrefabs == null
                || sourceRoomIds == null
                || sourcePrefabs.Length != placements.Length
                || sourceRoomIds.Length != placements.Length)
            {
                foundIssues.Add(
                    "Every generated placement needs a source prefab and id.");
                return;
            }

            RectInt canvas = new RectInt(
                Vector2Int.zero,
                new Vector2Int(CanvasWidth, CanvasHeight));
            HashSet<int> nodeIds = new HashSet<int>();
            Dictionary<string, int> uses =
                new Dictionary<string, int>(StringComparer.Ordinal);
            Dictionary<string, GameObject> libraryById =
                BuildLibraryLookup(foundIssues);
            Vector2Int macroCellSize = RoomTemplate2D.MacroCellSize;
            HashSet<Vector2Int> logicalSizes =
                new HashSet<Vector2Int>();
            int occupiedMacroCells = 0;

            for (int index = 0; index < placements.Length; index++)
            {
                P6RoomGraphLabPlacement placement = placements[index];
                occupiedMacroCells +=
                    placement.MacroBounds.width
                    * placement.MacroBounds.height;
                if (!nodeIds.Add(placement.NodeId))
                {
                    foundIssues.Add(
                        $"Duplicate generated node id {placement.NodeId}.");
                }

                if (string.IsNullOrWhiteSpace(placement.PrefabId)
                    || sourceRoomIds[index] != placement.PrefabId
                    || sourcePrefabs[index] != placement.SourcePrefab)
                {
                    foundIssues.Add(
                        $"Placement {index} source snapshot is inconsistent.");
                    continue;
                }

                if (!libraryById.TryGetValue(
                        placement.PrefabId,
                        out GameObject expectedSource)
                    || expectedSource != placement.SourcePrefab)
                {
                    foundIssues.Add(
                        $"Placement {index} is not sourced from the P4 library.");
                }

                RoomTemplate2D sourceTemplate = placement.SourcePrefab != null
                    ? placement.SourcePrefab.GetComponent<RoomTemplate2D>()
                    : null;
                if (sourceTemplate == null
                    || placement.Instance == null
                    || placement.Instance.RoomId != placement.PrefabId
                    || sourceTemplate.RoomId != placement.PrefabId
                    || sourceTemplate.MacroSize != placement.MacroBounds.size)
                {
                    foundIssues.Add(
                        $"Placement {index} room metadata is inconsistent.");
                }
                else
                {
                    logicalSizes.Add(sourceTemplate.LogicalSize);
                    Vector3 expectedPosition = new Vector3(
                        placement.MacroBounds.xMin * macroCellSize.x,
                        placement.MacroBounds.yMin * macroCellSize.y,
                        0f);
                    if (Vector3.Distance(
                            placement.Instance.transform.position,
                            expectedPosition) > 0.01f)
                    {
                        foundIssues.Add(
                            $"Placement {index} does not match its macro origin.");
                    }
                }

                if (placement.MacroBounds.width <= 0
                    || placement.MacroBounds.height <= 0
                    || !canvas.Contains(placement.MacroBounds.min)
                    || !canvas.Contains(
                        placement.MacroBounds.max - Vector2Int.one))
                {
                    foundIssues.Add(
                        $"Placement {index} lies outside the "
                        + $"{CanvasWidth} x {CanvasHeight} canvas.");
                }

                if (!uses.TryGetValue(placement.PrefabId, out int useCount))
                {
                    useCount = 0;
                }

                uses[placement.PrefabId] = useCount + 1;
            }

            if (logicalSizes.Count < 3)
            {
                foundIssues.Add(
                    "The Lab must show at least three authored logical "
                    + "room sizes.");
            }

            float occupancy =
                occupiedMacroCells
                / (float)(CanvasWidth * CanvasHeight);
            if (occupancy > 0.7f)
            {
                foundIssues.Add(
                    $"Room occupancy {occupancy:P0} leaves too little "
                    + "space for physical corridors.");
            }

            for (int first = 0; first < placements.Length; first++)
            {
                for (int second = first + 1;
                     second < placements.Length;
                     second++)
                {
                    if (placements[first].MacroBounds.Overlaps(
                            placements[second].MacroBounds))
                    {
                        foundIssues.Add(
                            $"Generated rooms {placements[first].NodeId} and "
                            + $"{placements[second].NodeId} overlap.");
                    }
                }
            }

            foreach (KeyValuePair<string, int> use in uses)
            {
                if (use.Value > MaximumPrefabUses)
                {
                    foundIssues.Add(
                        $"Prefab {use.Key} is used {use.Value} times.");
                }
            }
        }

        private Dictionary<string, GameObject> BuildLibraryLookup(
            List<string> foundIssues)
        {
            Dictionary<string, GameObject> lookup =
                new Dictionary<string, GameObject>(StringComparer.Ordinal);
            if (sourceLibrary == null)
            {
                return lookup;
            }

            for (int index = 0;
                 index < sourceLibrary.RoomPrefabs.Count;
                 index++)
            {
                GameObject prefab = sourceLibrary.RoomPrefabs[index];
                RoomTemplate2D template = prefab != null
                    ? prefab.GetComponent<RoomTemplate2D>()
                    : null;
                if (template == null
                    || string.IsNullOrWhiteSpace(template.RoomId)
                    || !lookup.TryAdd(template.RoomId, prefab))
                {
                    foundIssues.Add(
                        $"P4 library source {index} is null or duplicated.");
                }
            }

            return lookup;
        }

        private void ValidateAccesses(List<string> foundIssues)
        {
            if (accesses == null
                || accesses.Length == 0
                || edges == null)
            {
                foundIssues.Add(
                    "The graph requires a non-empty unique room access list.");
                return;
            }

            HashSet<int> accessIds = new HashSet<int>();
            HashSet<string> socketClaims =
                new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < accesses.Length; index++)
            {
                P6RoomGraphLabAccess access = accesses[index];
                if (!accessIds.Add(access.AccessId)
                    || access.AccessId != index)
                {
                    foundIssues.Add(
                        $"Room access {index} has a duplicate or unstable id.");
                }

                int placementIndex = FindPlacement(access.NodeId);
                if (placementIndex < 0
                    || string.IsNullOrWhiteSpace(access.SocketId)
                    || !socketClaims.Add(
                        $"{access.NodeId}:{access.SocketId}"))
                {
                    foundIssues.Add(
                        $"Room access {access.AccessId} has an invalid socket claim.");
                    continue;
                }

                if (!corridorRoutingBounds.Contains(access.SocketCell)
                    || !corridorRoutingBounds.Contains(access.PortalCell)
                    || access.PortalCell - access.SocketCell
                        != ExpectedPortalDelta(access.SocketSide))
                {
                    foundIssues.Add(
                        $"Room access {access.AccessId} has invalid routing endpoints.");
                }

                if (access.Visual == null)
                {
                    foundIssues.Add(
                        $"Room access {access.AccessId} has no sprite visual.");
                }

                RoomTemplate2D source =
                    placements[placementIndex].SourcePrefab != null
                        ? placements[placementIndex]
                            .SourcePrefab.GetComponent<RoomTemplate2D>()
                        : null;
                RoomSocket2D authored = null;
                if (source != null)
                {
                    for (int socketIndex = 0;
                         socketIndex < source.Sockets.Count;
                         socketIndex++)
                    {
                        RoomSocket2D candidate =
                            source.Sockets[socketIndex];
                        if (candidate != null
                            && candidate.SocketId == access.SocketId)
                        {
                            authored = candidate;
                            break;
                        }
                    }
                }

                if (authored == null
                    || ToP6Side(authored.Side) != access.SocketSide
                    || authored.CellOffset != access.CellOffset
                    || authored.OpeningSize != access.OpeningSize
                    || authored.TraversalType != access.TraversalType
                    || authored.MainRouteAllowed
                        != access.MainRouteAllowed
                    || authored.ValidationAnchor
                        != access.ValidationAnchor)
                {
                    foundIssues.Add(
                        $"Room access {access.AccessId} differs from its "
                        + "authored P4 socket.");
                }
                else
                {
                    CalculateExpectedPortal(
                        placements[placementIndex].MacroBounds,
                        source.LogicalSize,
                        authored,
                        out Vector2Int expectedSocket,
                        out Vector2Int expectedPortal);
                    if (access.SocketCell != expectedSocket
                        || access.PortalCell != expectedPortal)
                    {
                        foundIssues.Add(
                            $"Room access {access.AccessId} does not map "
                            + "to its exact scale-2 socket lane.");
                    }
                }
            }
        }

        private void ValidateGraph(List<string> foundIssues)
        {
            if (placements == null
                || edges == null
                || accesses == null)
            {
                foundIssues.Add("The generated graph snapshot is missing.");
                return;
            }

            if (startNodeId == exitNodeId
                || FindPlacement(startNodeId) < 0
                || FindPlacement(exitNodeId) < 0)
            {
                foundIssues.Add("Start and Exit must reference distinct rooms.");
            }

            int mstCount = 0;
            int loopCount = 0;
            Dictionary<int, List<int>> adjacency =
                new Dictionary<int, List<int>>();
            HashSet<string> edgeKeys =
                new HashSet<string>(StringComparer.Ordinal);
            HashSet<int> claimedAccessIds = new HashSet<int>();
            for (int index = 0; index < placements.Length; index++)
            {
                adjacency[placements[index].NodeId] = new List<int>();
            }

            for (int index = 0; index < edges.Length; index++)
            {
                P6RoomGraphLabEdge edge = edges[index];
                if (edge.Kind == P6EdgeKind.Mst)
                {
                    mstCount++;
                }
                else
                {
                    loopCount++;
                }

                string key = EdgeKey(
                    edge.FirstNodeId,
                    edge.SecondNodeId);
                if (!edgeKeys.Add(key)
                    || !adjacency.ContainsKey(edge.FirstNodeId)
                    || !adjacency.ContainsKey(edge.SecondNodeId)
                    || edge.FirstNodeId == edge.SecondNodeId)
                {
                    foundIssues.Add(
                        $"Graph edge {index} has invalid endpoints.");
                    continue;
                }

                int firstAccessIndex =
                    FindAccess(edge.FirstAccessId);
                int secondAccessIndex =
                    FindAccess(edge.SecondAccessId);
                if (firstAccessIndex < 0
                    || secondAccessIndex < 0
                    || accesses[firstAccessIndex].NodeId
                        != edge.FirstNodeId
                    || accesses[secondAccessIndex].NodeId
                        != edge.SecondNodeId
                    )
                {
                    foundIssues.Add(
                        $"Graph edge {index} has invalid access endpoints.");
                }
                else
                {
                    claimedAccessIds.Add(edge.FirstAccessId);
                    claimedAccessIds.Add(edge.SecondAccessId);
                }

                adjacency[edge.FirstNodeId].Add(edge.SecondNodeId);
                adjacency[edge.SecondNodeId].Add(edge.FirstNodeId);
                if (edge.Visual == null)
                {
                    foundIssues.Add(
                        $"Graph edge {index} has no sprite visual.");
                }
            }

            if (mstCount != placements.Length - 1)
            {
                foundIssues.Add(
                    "MST edge count must be exactly room count minus one.");
            }

            if (loopCount < 1 || edges.Length < placements.Length)
            {
                foundIssues.Add(
                    "X-2 must contain at least one closed-loop edge.");
            }

            if (!CanReach(startNodeId, exitNodeId, adjacency))
            {
                foundIssues.Add("Start cannot reach Exit in the graph snapshot.");
            }

            if (accesses != null
                && claimedAccessIds.Count != accesses.Length)
            {
                foundIssues.Add(
                    "Not every room access endpoint is referenced by an edge.");
            }
        }

        private void ValidateCorridors(List<string> foundIssues)
        {
            if (edges == null
                || corridors == null
                || corridors.Length != edges.Length)
            {
                foundIssues.Add(
                    "Every final graph edge needs one A* corridor snapshot.");
                return;
            }

            Dictionary<string, P6RoomGraphLabEdge> edgeByKey =
                new Dictionary<string, P6RoomGraphLabEdge>(
                    StringComparer.Ordinal);
            for (int index = 0; index < edges.Length; index++)
            {
                P6RoomGraphLabEdge edge = edges[index];
                edgeByKey[EdgeKey(
                    edge.FirstNodeId,
                    edge.SecondNodeId)] = edge;
            }

            Dictionary<string, P6RoomGraphLabCorridor> corridorByKey =
                new Dictionary<string, P6RoomGraphLabCorridor>(
                    StringComparer.Ordinal);
            for (int index = 0; index < corridors.Length; index++)
            {
                P6RoomGraphLabCorridor corridor = corridors[index];
                string key = EdgeKey(
                    corridor.FirstNodeId,
                    corridor.SecondNodeId);
                if (!edgeByKey.TryGetValue(
                        key,
                        out P6RoomGraphLabEdge edge)
                    || !corridorByKey.TryAdd(key, corridor)
                    || corridor.MacroCells == null
                    || corridor.MacroCells.Count == 0)
                {
                    foundIssues.Add(
                        $"A* corridor {index} is empty or has invalid endpoints.");
                    continue;
                }

                if (corridor.FirstAccessId != edge.FirstAccessId
                    || corridor.SecondAccessId != edge.SecondAccessId)
                {
                    foundIssues.Add(
                        $"A* corridor {index} access ids differ from its edge.");
                    continue;
                }

                int firstAccessIndex =
                    FindAccess(corridor.FirstAccessId);
                int secondAccessIndex =
                    FindAccess(corridor.SecondAccessId);
                if (firstAccessIndex < 0
                    || secondAccessIndex < 0
                    || corridor.MacroCells[0]
                        != accesses[firstAccessIndex].PortalCell
                    || corridor.MacroCells[
                        corridor.MacroCells.Count - 1]
                        != accesses[secondAccessIndex].PortalCell)
                {
                    foundIssues.Add(
                        $"A* corridor {index} does not terminate at its "
                        + "selected access portals.");
                }

                for (int cellIndex = 0;
                     cellIndex < corridor.MacroCells.Count;
                     cellIndex++)
                {
                    Vector2Int cell = corridor.MacroCells[cellIndex];
                    if (!corridorRoutingBounds.Contains(cell))
                    {
                        foundIssues.Add(
                            $"A* corridor {index} leaves its routing bounds.");
                        break;
                    }

                    for (int roomIndex = 0;
                         roomIndex < placements.Length;
                         roomIndex++)
                    {
                        if (ScaledRoomInterior(
                                placements[roomIndex].MacroBounds)
                            .Contains(cell))
                        {
                            foundIssues.Add(
                                $"A* corridor {index} enters room "
                                + $"{placements[roomIndex].NodeId} interior.");
                            break;
                        }
                    }

                    if (cellIndex > 0
                        && ManhattanDistance(
                            corridor.MacroCells[cellIndex - 1],
                            cell) != 1)
                    {
                        foundIssues.Add(
                            $"A* corridor {index} is not 4-neighbour contiguous.");
                        break;
                    }
                }

                if (corridor.Visual == null)
                {
                    foundIssues.Add(
                        $"A* corridor {index} has no sprite visual.");
                }
            }

            if (corridorByKey.Count != edgeByKey.Count)
            {
                foundIssues.Add(
                    "A* corridor keys do not exactly match graph edge keys.");
                return;
            }

            HashSet<string> occupiedSegments =
                new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < edges.Length; index++)
            {
                P6RoomGraphLabEdge edge = edges[index];
                if (edge.Kind != P6EdgeKind.Mst)
                {
                    continue;
                }

                AddPhysicalSegments(
                    occupiedSegments,
                    corridorByKey[EdgeKey(
                        edge.FirstNodeId,
                        edge.SecondNodeId)].MacroCells);
            }

            for (int index = 0; index < edges.Length; index++)
            {
                P6RoomGraphLabEdge edge = edges[index];
                if (edge.Kind != P6EdgeKind.Additional)
                {
                    continue;
                }

                IReadOnlyList<Vector2Int> cells =
                    corridorByKey[EdgeKey(
                        edge.FirstNodeId,
                        edge.SecondNodeId)].MacroCells;
                bool addsPhysicalSegment = false;
                for (int cellIndex = 1;
                     cellIndex < cells.Count;
                     cellIndex++)
                {
                    if (!occupiedSegments.Contains(
                        PhysicalSegmentKey(
                            cells[cellIndex - 1],
                            cells[cellIndex])))
                    {
                        addsPhysicalSegment = true;
                        break;
                    }
                }

                if (!addsPhysicalSegment)
                {
                    foundIssues.Add(
                        $"Additional corridor {edge.FirstNodeId}:"
                        + $"{edge.SecondNodeId} adds no physical segment.");
                }

                AddPhysicalSegments(occupiedSegments, cells);
            }

            bool derivedPhysicalLoop =
                ContainsPhysicalGridCycle(corridors);
            if (!derivedPhysicalLoop)
            {
                foundIssues.Add(
                    "The X-2 corridor lattice has no physical cycle.");
            }

            if (physicalLoopSatisfied != derivedPhysicalLoop)
            {
                foundIssues.Add(
                    "Stored physical-loop proof differs from the corridor lattice.");
            }
        }

        private void ValidatePhysicalCorridors(
            List<string> foundIssues)
        {
            if (physicalCorridorRoot == null
                || physicalCorridorModules == null
                || physicalCorridorModules.Length == 0)
            {
                foundIssues.Add(
                    "The A* lattice must be assembled into physical "
                    + "corridor modules.");
                return;
            }

            HashSet<Vector2Int> expectedCells =
                new HashSet<Vector2Int>();
            for (int corridorIndex = 0;
                 corridorIndex < corridors.Length;
                 corridorIndex++)
            {
                IReadOnlyList<Vector2Int> cells =
                    corridors[corridorIndex].RoutingCells;
                for (int cellIndex = 0;
                     cellIndex < cells.Count;
                     cellIndex++)
                {
                    expectedCells.Add(cells[cellIndex]);
                }
            }

            HashSet<Vector2Int> assembledCells =
                new HashSet<Vector2Int>();
            HashSet<int> assembledAccesses =
                new HashSet<int>();
            for (int index = 0;
                 index < physicalCorridorModules.Length;
                 index++)
            {
                P6PhysicalCorridorModule2D module =
                    physicalCorridorModules[index];
                if (module == null
                    || !module.transform.IsChildOf(
                        physicalCorridorRoot)
                    || !module.HasPhysicalCollision)
                {
                    foundIssues.Add(
                        $"Physical corridor module {index} is missing "
                        + "its hierarchy or collision surface.");
                    continue;
                }

                if (module.IsAccessBridge)
                {
                    if (module.ModuleKind
                            != P6CorridorModuleKind.AccessBridge
                        || !assembledAccesses.Add(module.AccessId))
                    {
                        foundIssues.Add(
                            $"Physical access bridge {index} is invalid "
                            + "or duplicated.");
                    }
                }
                else if (!assembledCells.Add(module.RoutingCell))
                {
                    foundIssues.Add(
                        $"Physical routing cell {module.RoutingCell} "
                        + "is duplicated.");
                }
            }

            if (!assembledCells.SetEquals(expectedCells))
            {
                foundIssues.Add(
                    "Physical corridor modules do not cover every "
                    + "A* routing cell exactly once.");
            }

            if (accesses == null
                || assembledAccesses.Count != accesses.Length)
            {
                foundIssues.Add(
                    "Every claimed room socket needs one physical "
                    + "access bridge.");
            }
            else
            {
                for (int index = 0; index < accesses.Length; index++)
                {
                    if (!assembledAccesses.Contains(
                            accesses[index].AccessId))
                    {
                        foundIssues.Add(
                            $"Room access {accesses[index].AccessId} "
                            + "has no physical bridge.");
                    }
                }
            }
        }

        private void ValidateProofSnapshot(List<string> foundIssues)
        {
            if (!startExitReachable)
            {
                foundIssues.Add("Start-to-Exit reachability proof failed.");
            }
            if (!optionalRoomsReturnable)
            {
                foundIssues.Add("Optional-room returnability proof failed.");
            }
            if (!closedLoopSatisfied)
            {
                foundIssues.Add("X-2 closed-loop graph proof failed.");
            }
            if (!mainPathLengthSatisfied)
            {
                foundIssues.Add("Main-path length proof failed.");
            }
            if (!prefabUsageSatisfied)
            {
                foundIssues.Add("Prefab usage proof failed.");
            }
            if (!packingSatisfied)
            {
                foundIssues.Add("Variable-size packing proof failed.");
            }
            if (!corridorNetworkConnected)
            {
                foundIssues.Add("Physical corridor connectivity proof failed.");
            }
            if (!socketClaimsUnique)
            {
                foundIssues.Add("Unique socket-claim proof failed.");
            }
            if (!oneCellOpeningsSatisfied)
            {
                foundIssues.Add("One-cell opening proof failed.");
            }
            if (!jumpEnvelopeSatisfied)
            {
                foundIssues.Add("Jump-envelope proof failed.");
            }
            if (!toolFreeMainPathSatisfied)
            {
                foundIssues.Add("Tool-free main-path proof failed.");
            }
            if (!exitBlockProofSatisfied)
            {
                foundIssues.Add("Exit-block prevention proof failed.");
            }
            if (!physicalTraversalSatisfied)
            {
                foundIssues.Add("Composite physical-traversal proof failed.");
            }
            if (!riskyChoiceSatisfied)
            {
                foundIssues.Add("Risky-choice role placement proof failed.");
            }
            if (!x3RoleSequenceSatisfied)
            {
                foundIssues.Add("X-3 role sequence proof failed.");
            }
            if (!landmarkPrioritySatisfied)
            {
                foundIssues.Add("Landmark-junction priority proof failed.");
            }
            if (!physicalLoopSatisfied)
            {
                foundIssues.Add("Physical X-2 loop proof failed.");
            }

            bool derivedComposite = ComputeCompositeProof();
            if (!compositeProofSatisfied
                || compositeProofSatisfied != derivedComposite)
            {
                foundIssues.Add(
                    "Stored composite P6 proof is false or inconsistent.");
            }
        }

        private void ValidatePresentation(List<string> foundIssues)
        {
            if (moonBackdrop == null
                || startMarker == null
                || exitMarker == null
                || mstOverlay == null
                || loopOverlay == null
                || corridorOverlay == null
                || accessOverlay == null
                || roleOverlay == null)
            {
                foundIssues.Add(
                    "Moon backdrop and all sprite-only overlays are required.");
            }

            if (stageCamera == null
                || !stageCamera.orthographic
                || !stageCamera.CompareTag("MainCamera"))
            {
                foundIssues.Add("An orthographic Main Camera is required.");
            }

            if (directionalLight == null
                || directionalLight.type != LightType.Directional
                || !directionalLight.gameObject.activeInHierarchy)
            {
                foundIssues.Add("One active Directional Light is required.");
            }
        }

        private void RegenerateAndCompare(List<string> foundIssues)
        {
            if (sourceLibrary == null)
            {
                return;
            }

            P6RoomPrefabDescriptor[] descriptors =
                BuildGeneratorDescriptors(foundIssues);
            if (descriptors.Length != sourceLibrary.RoomPrefabs.Count)
            {
                return;
            }

            P6GenerationRequest request =
                P6GenerationRequest.CreateMoonPalace(
                    fixedSeed,
                    P6StageSlot.X2,
                    descriptors,
                    archetype);
            P6GenerationResult result =
                P6RoomGraphGenerator.Generate(request);
            if (!result.Accepted)
            {
                foundIssues.Add(
                    "Fixed-seed regeneration failed: "
                    + $"{result.Failure} {result.FailureReason}");
                return;
            }

            if (result.AcceptedSeed != acceptedSeed
                || result.Fingerprint != generationFingerprint
                || result.Plan.Archetype != archetype
                || result.Plan.StageSlot != P6StageSlot.X2)
            {
                foundIssues.Add(
                    "Fixed-seed regeneration does not match the scene snapshot.");
                return;
            }

            ComparePlan(result.Plan, result.Validation, foundIssues);
        }

        private P6RoomPrefabDescriptor[] BuildGeneratorDescriptors(
            List<string> foundIssues)
        {
            List<P6RoomPrefabDescriptor> descriptors =
                new List<P6RoomPrefabDescriptor>();
            for (int index = 0;
                 index < sourceLibrary.RoomPrefabs.Count;
                 index++)
            {
                GameObject prefab = sourceLibrary.RoomPrefabs[index];
                RoomTemplate2D template = prefab != null
                    ? prefab.GetComponent<RoomTemplate2D>()
                    : null;
                if (template == null)
                {
                    foundIssues.Add(
                        $"Cannot regenerate from P4 source {index}.");
                    continue;
                }

                P6RoomPrefabDescriptor descriptor =
                    P6RoomTraversalProofFactory.CreateDescriptor(
                        template,
                        1,
                        MaximumPrefabUses);
                if (!descriptor.HasValidatedTraversalProof)
                {
                    foundIssues.Add(
                        $"{template.RoomId} has no passed tile traversal "
                        + "proof.");
                }

                descriptors.Add(descriptor);
            }

            descriptors.Sort(
                (left, right) => string.CompareOrdinal(
                    left.PrefabId,
                    right.PrefabId));
            return descriptors.ToArray();
        }

        private void ComparePlan(
            P6RoomGraphPlan plan,
            P6ValidationReport validation,
            List<string> foundIssues)
        {
            if (plan == null
                || plan.CorridorNetwork == null
                || placements == null
                || edges == null
                || corridors == null
                || accesses == null)
            {
                foundIssues.Add(
                    "Regenerated graph or scene snapshot is missing.");
                return;
            }

            if (plan.Rooms.Count != placements.Length
                || plan.Edges.Count != edges.Length
                || corridors.Length != edges.Length
                || plan.StartNodeId != startNodeId
                || plan.ExitNodeId != exitNodeId
                || plan.CorridorNetwork.RoutingScale != routingScale
                || plan.CorridorNetwork.RoutingBounds
                    != corridorRoutingBounds
                || plan.CorridorNetwork.RoomAccesses.Count
                    != accesses.Length)
            {
                foundIssues.Add(
                    "Regenerated graph dimensions differ from the snapshot.");
                return;
            }

            for (int index = 0; index < placements.Length; index++)
            {
                P6RoomNode node = plan.Rooms[index];
                P6RoomGraphLabPlacement placement = placements[index];
                if (node.Id != placement.NodeId
                    || node.PrefabId != placement.PrefabId
                    || node.MacroBounds != placement.MacroBounds
                    || node.Role != placement.Role
                    || node.OnMainPath != placement.OnMainPath)
                {
                    foundIssues.Add(
                        $"Regenerated room {index} differs from the snapshot.");
                }
            }

            for (int index = 0; index < edges.Length; index++)
            {
                P6GraphEdge generated = plan.Edges[index];
                P6RoomGraphLabEdge edge = edges[index];
                P6RoomGraphLabCorridor corridor = corridors[index];
                if (generated.FirstNodeId != edge.FirstNodeId
                    || generated.SecondNodeId != edge.SecondNodeId
                    || generated.FirstAccessId != edge.FirstAccessId
                    || generated.SecondAccessId != edge.SecondAccessId
                    || generated.Kind != edge.Kind
                    || generated.FirstNodeId != corridor.FirstNodeId
                    || generated.SecondNodeId != corridor.SecondNodeId
                    || generated.FirstAccessId
                        != corridor.FirstAccessId
                    || generated.SecondAccessId
                        != corridor.SecondAccessId
                    || !SameCells(
                        generated.CorridorCells,
                        corridor.MacroCells))
                {
                    foundIssues.Add(
                        $"Regenerated edge {index} differs from the snapshot.");
                }
            }

            for (int index = 0; index < accesses.Length; index++)
            {
                P6RoomAccess generated =
                    plan.CorridorNetwork.RoomAccesses[index];
                P6RoomGraphLabAccess access = accesses[index];
                if (generated.AccessId != access.AccessId
                    || generated.NodeId != access.NodeId
                    || generated.SocketId != access.SocketId
                    || generated.SocketSide != access.SocketSide
                    || generated.CellOffset != access.CellOffset
                    || generated.OpeningSize != access.OpeningSize
                    || generated.TraversalType
                        != access.TraversalType
                    || generated.MainRouteAllowed
                        != access.MainRouteAllowed
                    || generated.ValidationAnchor
                        != access.ValidationAnchor
                    || generated.SocketCell != access.SocketCell
                    || generated.PortalCell != access.PortalCell)
                {
                    foundIssues.Add(
                        $"Regenerated room access {index} differs from the snapshot.");
                }
            }

            if (!plan.HasClosedLoop)
            {
                foundIssues.Add(
                    "Regenerated X-2 graph no longer contains a closed loop.");
            }

            if (validation == null
                || validation.StartExitReachable != startExitReachable
                || validation.OptionalRoomsReturnable
                    != optionalRoomsReturnable
                || validation.ClosedLoopSatisfied
                    != closedLoopSatisfied
                || validation.MainPathLengthSatisfied
                    != mainPathLengthSatisfied
                || validation.PrefabUsageSatisfied
                    != prefabUsageSatisfied
                || validation.PackingSatisfied != packingSatisfied
                || validation.CorridorNetworkConnected
                    != corridorNetworkConnected
                || validation.SocketClaimsUnique != socketClaimsUnique
                || validation.OneCellOpeningsSatisfied
                    != oneCellOpeningsSatisfied
                || validation.JumpEnvelopeSatisfied
                    != jumpEnvelopeSatisfied
                || validation.ToolFreeMainPathSatisfied
                    != toolFreeMainPathSatisfied
                || validation.ExitBlockProofSatisfied
                    != exitBlockProofSatisfied
                || validation.PhysicalTraversalSatisfied
                    != physicalTraversalSatisfied
                || validation.RiskyChoiceSatisfied
                    != riskyChoiceSatisfied
                || validation.X3RoleSequenceSatisfied
                    != x3RoleSequenceSatisfied
                || validation.LandmarkPrioritySatisfied
                    != landmarkPrioritySatisfied)
            {
                foundIssues.Add(
                    "Regenerated physical proof differs from the scene snapshot.");
            }
        }

        private int FindPlacement(int nodeId)
        {
            for (int index = 0; index < placements.Length; index++)
            {
                if (placements[index].NodeId == nodeId)
                {
                    return index;
                }
            }

            return -1;
        }

        private int FindAccess(int accessId)
        {
            if (accesses == null)
            {
                return -1;
            }

            for (int index = 0; index < accesses.Length; index++)
            {
                if (accesses[index].AccessId == accessId)
                {
                    return index;
                }
            }

            return -1;
        }

        private static RectInt ScaledRoomInterior(RectInt macroBounds)
        {
            return new RectInt(
                macroBounds.xMin * 2 + 1,
                macroBounds.yMin * 2 + 1,
                macroBounds.width * 2 - 1,
                macroBounds.height * 2 - 1);
        }

        private static void CalculateExpectedPortal(
            RectInt macroBounds,
            Vector2Int logicalSize,
            RoomSocket2D socketDefinition,
            out Vector2Int socket,
            out Vector2Int portal)
        {
            RectInt interior = ScaledRoomInterior(macroBounds);
            if (socketDefinition.Side == RoomSocketSide.Left
                || socketDefinition.Side == RoomSocketSide.Right)
            {
                int y = interior.yMin + ScaleSocketLane(
                    socketDefinition.CellOffset.y,
                    logicalSize.y,
                    interior.height);
                socket = new Vector2Int(
                    socketDefinition.Side == RoomSocketSide.Left
                        ? interior.xMin
                        : interior.xMax - 1,
                    y);
            }
            else
            {
                int x = interior.xMin + ScaleSocketLane(
                    socketDefinition.CellOffset.x,
                    logicalSize.x,
                    interior.width);
                socket = new Vector2Int(
                    x,
                    socketDefinition.Side == RoomSocketSide.Bottom
                        ? interior.yMin
                        : interior.yMax - 1);
            }

            portal = socket + ExpectedPortalDelta(
                ToP6Side(socketDefinition.Side));
        }

        private static int ScaleSocketLane(
            int logicalOffset,
            int logicalSpan,
            int routingSpan)
        {
            if (logicalSpan <= 1 || routingSpan <= 1)
            {
                return 0;
            }

            return Mathf.Clamp(
                Mathf.RoundToInt(
                    logicalOffset * (routingSpan - 1f)
                    / (logicalSpan - 1f)),
                0,
                routingSpan - 1);
        }

        private static int ManhattanDistance(
            Vector2Int first,
            Vector2Int second)
        {
            return Mathf.Abs(first.x - second.x)
                + Mathf.Abs(first.y - second.y);
        }

        private static Vector2Int ExpectedPortalDelta(
            P6SocketSides side)
        {
            switch (side)
            {
                case P6SocketSides.Left:
                    return Vector2Int.left;
                case P6SocketSides.Right:
                    return Vector2Int.right;
                case P6SocketSides.Bottom:
                    return Vector2Int.down;
                case P6SocketSides.Top:
                    return Vector2Int.up;
                default:
                    return new Vector2Int(int.MaxValue, int.MaxValue);
            }
        }

        private static void AddPhysicalSegments(
            HashSet<string> destination,
            IReadOnlyList<Vector2Int> cells)
        {
            if (cells == null)
            {
                return;
            }

            for (int index = 1; index < cells.Count; index++)
            {
                destination.Add(
                    PhysicalSegmentKey(
                        cells[index - 1],
                        cells[index]));
            }
        }

        private static string PhysicalSegmentKey(
            Vector2Int first,
            Vector2Int second)
        {
            if (first.x > second.x
                || (first.x == second.x && first.y > second.y))
            {
                Vector2Int swap = first;
                first = second;
                second = swap;
            }

            return $"{first.x},{first.y}>{second.x},{second.y}";
        }

        private static bool ContainsPhysicalGridCycle(
            IReadOnlyList<P6RoomGraphLabCorridor> source)
        {
            if (source == null)
            {
                return false;
            }

            HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
            for (int corridorIndex = 0;
                 corridorIndex < source.Count;
                 corridorIndex++)
            {
                IReadOnlyList<Vector2Int> corridorCells =
                    source[corridorIndex].MacroCells;
                if (corridorCells == null)
                {
                    continue;
                }

                for (int cellIndex = 0;
                     cellIndex < corridorCells.Count;
                     cellIndex++)
                {
                    cells.Add(corridorCells[cellIndex]);
                }
            }

            int segments = 0;
            foreach (Vector2Int cell in cells)
            {
                if (cells.Contains(cell + Vector2Int.right))
                {
                    segments++;
                }

                if (cells.Contains(cell + Vector2Int.up))
                {
                    segments++;
                }
            }

            int components = 0;
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            foreach (Vector2Int origin in cells)
            {
                if (!visited.Add(origin))
                {
                    continue;
                }

                components++;
                Queue<Vector2Int> frontier = new Queue<Vector2Int>();
                frontier.Enqueue(origin);
                while (frontier.Count > 0)
                {
                    Vector2Int current = frontier.Dequeue();
                    Vector2Int[] neighbours =
                    {
                        current + Vector2Int.left,
                        current + Vector2Int.right,
                        current + Vector2Int.down,
                        current + Vector2Int.up
                    };
                    for (int index = 0;
                         index < neighbours.Length;
                         index++)
                    {
                        if (cells.Contains(neighbours[index])
                            && visited.Add(neighbours[index]))
                        {
                            frontier.Enqueue(neighbours[index]);
                        }
                    }
                }
            }

            return cells.Count >= 4
                && segments > cells.Count - components;
        }

        private static bool CanReach(
            int start,
            int goal,
            IReadOnlyDictionary<int, List<int>> adjacency)
        {
            if (!adjacency.ContainsKey(start)
                || !adjacency.ContainsKey(goal))
            {
                return false;
            }

            Queue<int> open = new Queue<int>();
            HashSet<int> visited = new HashSet<int>();
            open.Enqueue(start);
            visited.Add(start);
            while (open.Count > 0)
            {
                int current = open.Dequeue();
                if (current == goal)
                {
                    return true;
                }

                List<int> neighbors = adjacency[current];
                for (int index = 0; index < neighbors.Count; index++)
                {
                    if (visited.Add(neighbors[index]))
                    {
                        open.Enqueue(neighbors[index]);
                    }
                }
            }

            return false;
        }

        private static bool SameCells(
            IReadOnlyList<Vector2Int> first,
            IReadOnlyList<Vector2Int> second)
        {
            if (first == null
                || second == null
                || first.Count != second.Count)
            {
                return false;
            }

            for (int index = 0; index < first.Count; index++)
            {
                if (first[index] != second[index])
                {
                    return false;
                }
            }

            return true;
        }

        private static string EdgeKey(int first, int second)
        {
            return first < second
                ? $"{first}:{second}"
                : $"{second}:{first}";
        }

        private static P6SocketSides ToP6Side(RoomSocketSide side)
        {
            switch (side)
            {
                case RoomSocketSide.Left:
                    return P6SocketSides.Left;
                case RoomSocketSide.Right:
                    return P6SocketSides.Right;
                case RoomSocketSide.Bottom:
                    return P6SocketSides.Bottom;
                case RoomSocketSide.Top:
                    return P6SocketSides.Top;
                default:
                    return P6SocketSides.None;
            }
        }
    }
}

#endif
