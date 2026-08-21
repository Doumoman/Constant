#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.IO;
using StarNight.Debugging;
using StarNight.Generation.P6;
using StarNight.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarNight.Editor
{
    public static class P6RoomGraphLabBuilder
    {
        public const string ScenePath =
            "Assets/StarNight/Scenes/Labs/P6_MoonRoomGraphGeneratorLab.unity";
        public const int ShowcaseSeedSearchStart = 620600;

        private const string SkySpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/Sky B.png";
        private const string MountainASpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/Mounts A.png";
        private const string MountainBSpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/Mounts B.png";
        private const string MoonSpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/Moon B.png";
        private const string CloudSpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/Clouds small A.png";
        private const string SquareSpritePath =
            "Assets/2D Fantasy sprite bundle/Mount pack/Sprites/Square.png";
        private const string StarSpritePath =
            "Assets/2D Fantasy sprite bundle/Cristal Dungeon sprite pack/Cristal Sprites/Star particle.png";
        private const string CrystalSpritePath =
            "Assets/2D Fantasy sprite bundle/Cristal Dungeon sprite pack/Cristal Sprites/Crystal elements.png";

        private static readonly Color MstColor =
            new Color(0.18f, 0.92f, 1f, 0.92f);
        private static readonly Color LoopColor =
            new Color(1f, 0.72f, 0.18f, 0.98f);
        private static readonly Color CorridorColor =
            new Color(0.72f, 0.38f, 1f, 0.58f);

        [MenuItem("StarNight/P6/Rebuild Moon Room Graph Generator Lab")]
        public static void Rebuild()
        {
            EnsureFolder(
                Path.GetDirectoryName(ScenePath)?.Replace('\\', '/'));
            P6RoomCatalogEntry[] catalog =
                P6RoomCatalogAdapter.LoadMoonPalaceEntries();
            P6RoomPrefabDescriptor[] descriptors =
                CreateDescriptors(catalog);
            ShowcaseGeneration showcase =
                FindShowcaseGeneration(descriptors, catalog);
            P6GenerationResult result = showcase.Result;
            if (!result.Accepted)
            {
                throw new InvalidOperationException(
                    "P6 showcase generation failed: "
                    + $"{result.Failure} {result.FailureReason}");
            }

            SourceArt art = LoadSourceArt();
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            GameObject root =
                new GameObject("P6_MoonRoomGraphGeneratorLab");

            Transform backdrop = BuildBackdrop(
                root.transform,
                art,
                result.Plan.CorridorNetwork.RoutingBounds,
                result.Plan.CorridorNetwork.RoutingScale);
            BuildMacroGrid(
                root.transform,
                art.Square,
                result.Plan.CanvasBounds);

            GameObject roomsObject = CreateChild(root.transform, "Rooms");
            Transform physicalCorridorRoot =
                CreateChild(root.transform, "PhysicalCorridorModules")
                    .transform;
            GameObject overlayObject =
                CreateChild(root.transform, "SpriteOnlyGraphOverlay");
            Transform corridorOverlay =
                CreateChild(overlayObject.transform, "AStarCorridors")
                    .transform;
            Transform mstOverlay =
                CreateChild(overlayObject.transform, "MST")
                    .transform;
            Transform loopOverlay =
                CreateChild(overlayObject.transform, "AdditionalLoops")
                    .transform;
            Transform accessOverlay =
                CreateChild(overlayObject.transform, "AccessEndpoints")
                    .transform;
            Transform roleOverlay =
                CreateChild(overlayObject.transform, "Roles")
                    .transform;
            Transform endpointsOverlay =
                CreateChild(overlayObject.transform, "StartExit")
                    .transform;

            Dictionary<string, P6RoomCatalogEntry> catalogById =
                BuildCatalogLookup(catalog);
            Dictionary<int, P6RoomNode> nodesById =
                BuildNodeLookup(result.Plan.Rooms);
            Dictionary<int, P6RoomAccess> accessesById =
                BuildAccessLookup(
                    result.Plan.CorridorNetwork.RoomAccesses);
            P6RoomGraphLabPlacement[] placements =
                BuildRooms(
                    scene,
                    roomsObject.transform,
                    roleOverlay,
                    result.Plan,
                    catalogById,
                    art);
            P6RoomGraphLabAccess[] accesses =
                BuildAccesses(
                    result.Plan.CorridorNetwork.RoomAccesses,
                    result.Plan.CorridorNetwork.RoutingScale,
                    accessOverlay,
                    art);
            P6RoomGraphLabEdge[] edges =
                BuildGraphEdges(
                    result.Plan.Edges,
                    accessesById,
                    result.Plan.CorridorNetwork.RoutingScale,
                    mstOverlay,
                    loopOverlay,
                    art);
            P6RoomGraphLabCorridor[] corridors =
                BuildCorridors(
                    result.Plan.Edges,
                    corridorOverlay,
                    art,
                    result.Plan.CorridorNetwork.RoutingScale);
            P6PhysicalCorridorModule2D[] physicalCorridorModules =
                BuildPhysicalCorridorModules(
                    result.Plan,
                    placements,
                    physicalCorridorRoot,
                    art);
            BuildMainRoutePulse(
                result.Plan,
                nodesById,
                roleOverlay,
                art);

            Transform startMarker = BuildEndpointMarker(
                endpointsOverlay,
                "Start",
                RoomCenter(nodesById[result.Plan.StartNodeId]),
                new Color(0.3f, 1f, 0.58f, 0.98f),
                art);
            Transform exitMarker = BuildEndpointMarker(
                endpointsOverlay,
                "Exit",
                RoomCenter(nodesById[result.Plan.ExitNodeId]),
                new Color(1f, 0.3f, 0.66f, 0.98f),
                art);

            Bounds visualBounds =
                ToWorldBounds(
                    result.Plan.CorridorNetwork.RoutingBounds,
                    result.Plan.CorridorNetwork.RoutingScale);
            Camera camera = BuildCamera(root.transform, visualBounds);
            Light mainLight = BuildDirectionalLight(root.transform);

            P6RoomGraphLabContract contract =
                root.AddComponent<P6RoomGraphLabContract>();
            contract.Configure(
                AssetDatabase.LoadAssetAtPath<RoomPrefabLibrary>(
                    P6RoomCatalogAdapter.MoonPalaceLibraryPath),
                showcase.RequestedSeed,
                result.AcceptedSeed,
                result.Plan.Archetype,
                result.Fingerprint,
                result.Plan.CorridorNetwork.RoutingScale,
                result.Plan.CorridorNetwork.RoutingBounds,
                placements,
                edges,
                corridors,
                accesses,
                physicalCorridorRoot,
                physicalCorridorModules,
                result.Validation,
                result.Plan.StartNodeId,
                result.Plan.ExitNodeId,
                backdrop,
                startMarker,
                exitMarker,
                mstOverlay,
                loopOverlay,
                corridorOverlay,
                accessOverlay,
                roleOverlay,
                camera,
                mainLight);
            if (!contract.RefreshValidation())
            {
                throw new InvalidOperationException(
                    "P6 Lab contract failed before save:"
                    + Environment.NewLine
                    + contract.LastValidation);
            }

            P7PopulationLabBuilder.Decorate(
                root.transform,
                result.Plan,
                catalog);
            P8MaruSystemBuilder.DecorateLab(
                root.transform,
                result.Plan);
            ValidatePrefabSources(contract);
            ValidateNoMissingScripts(root);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    $"Failed to save P6 Lab scene: {ScenePath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[StarNight P6] Room graph Lab rebuilt and validated: "
                + $"requestedSeed={showcase.RequestedSeed}, "
                + $"acceptedSeed={result.AcceptedSeed}, "
                + $"archetype={result.Plan.Archetype}, "
                + $"rooms={result.Plan.Rooms.Count}, "
                + $"mst={result.Plan.MstEdges.Count}, "
                + $"loops={result.Plan.AdditionalEdges.Count}, "
                + $"accesses={result.Plan.CorridorNetwork.RoomAccesses.Count}, "
                + $"routingScale={result.Plan.CorridorNetwork.RoutingScale}, "
                + $"physical={result.Validation.PhysicalTraversalSatisfied}, "
                + $"fingerprint={result.Fingerprint}, {ScenePath}");
        }

        [MenuItem("StarNight/P6/Validate Moon Room Graph Generator Lab")]
        public static void Validate()
        {
            Scene scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);
            P6RoomGraphLabContract[] contracts =
                UnityEngine.Object.FindObjectsByType<P6RoomGraphLabContract>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            if (contracts.Length != 1)
            {
                throw new InvalidOperationException(
                    "P6 Lab must contain exactly one "
                    + $"{nameof(P6RoomGraphLabContract)}.");
            }

            P6RoomGraphLabContract contract = contracts[0];
            if (!contract.RefreshValidation())
            {
                throw new InvalidOperationException(
                    "P6 Lab validation failed:"
                    + Environment.NewLine
                    + contract.LastValidation);
            }

            ValidatePrefabSources(contract);
            ValidateNoMissingScripts(contract.transform.root.gameObject);
            ValidateCameraAndLight();
            EditorSceneManager.SaveScene(scene);
            Debug.Log(
                "[StarNight P6] Room graph Lab validation PASS: "
                + $"seed={contract.FixedSeed}, "
                + $"rooms={contract.Placements.Count}, "
                + $"fingerprint={contract.GenerationFingerprint}");
        }

        private static ShowcaseGeneration FindShowcaseGeneration(
            IReadOnlyList<P6RoomPrefabDescriptor> descriptors,
            IReadOnlyList<P6RoomCatalogEntry> catalog)
        {
            const int searchLimit = 256;
            for (int offset = 0; offset < searchLimit; offset++)
            {
                int requestedSeed = ShowcaseSeedSearchStart + offset;
                P6GenerationResult result =
                    P6RoomGraphGenerator.Generate(
                        P6GenerationRequest.CreateMoonPalace(
                            requestedSeed,
                            P6StageSlot.X2,
                            descriptors,
                            P6StageArchetype.Circuit));
                if (result.Accepted
                    && SupportsP7PopulationShowcase(result.Plan, catalog))
                {
                    return new ShowcaseGeneration(requestedSeed, result);
                }
            }

            throw new InvalidOperationException(
                "No deterministic P6 X-2 showcase seed exposed both "
                + "enemy and trap budget rooms for the integrated P7 Lab.");
        }

        private static bool SupportsP7PopulationShowcase(
            P6RoomGraphPlan plan,
            IReadOnlyList<P6RoomCatalogEntry> catalog)
        {
            var templateById =
                new Dictionary<string, RoomTemplate2D>(
                    StringComparer.Ordinal);
            for (int index = 0; index < catalog.Count; index++)
            {
                P6RoomCatalogEntry entry = catalog[index];
                templateById[entry.PrefabId] =
                    entry.Prefab.GetComponent<RoomTemplate2D>();
            }

            int exitApproachNodeId =
                plan.MainPathNodeIds.Count >= 2
                    ? plan.MainPathNodeIds[plan.MainPathNodeIds.Count - 2]
                    : plan.ExitNodeId;
            bool hasEnemyRoom = false;
            bool hasTrapRoom = false;
            RoomRole protectedRoles =
                RoomRole.Start
                | RoomRole.Exit
                | RoomRole.ShopCorridor
                | RoomRole.SupplyCorridor;
            for (int index = 0; index < plan.Rooms.Count; index++)
            {
                P6RoomNode room = plan.Rooms[index];
                if ((room.Role & protectedRoles) != 0
                    || !templateById.TryGetValue(
                        room.PrefabId,
                        out RoomTemplate2D template)
                    || template == null)
                {
                    continue;
                }

                hasEnemyRoom |= template.EnemyBudget > 0;
                hasTrapRoom |=
                    room.Id != exitApproachNodeId
                    && template.HazardBudget > 0;
            }

            return hasEnemyRoom && hasTrapRoom;
        }

        private static P6RoomPrefabDescriptor[] CreateDescriptors(
            IReadOnlyList<P6RoomCatalogEntry> catalog)
        {
            P6RoomPrefabDescriptor[] descriptors =
                new P6RoomPrefabDescriptor[catalog.Count];
            for (int index = 0; index < catalog.Count; index++)
            {
                P6RoomCatalogEntry entry = catalog[index];
                descriptors[index] =
                    P6RoomCatalogAdapter.CreateDescriptor(entry);
            }

            return descriptors;
        }

        private static Dictionary<string, P6RoomCatalogEntry>
            BuildCatalogLookup(
                IReadOnlyList<P6RoomCatalogEntry> catalog)
        {
            Dictionary<string, P6RoomCatalogEntry> lookup =
                new Dictionary<string, P6RoomCatalogEntry>(
                    StringComparer.Ordinal);
            for (int index = 0; index < catalog.Count; index++)
            {
                lookup.Add(catalog[index].PrefabId, catalog[index]);
            }

            return lookup;
        }

        private static Dictionary<int, P6RoomNode> BuildNodeLookup(
            IReadOnlyList<P6RoomNode> rooms)
        {
            Dictionary<int, P6RoomNode> lookup =
                new Dictionary<int, P6RoomNode>();
            for (int index = 0; index < rooms.Count; index++)
            {
                lookup.Add(rooms[index].Id, rooms[index]);
            }

            return lookup;
        }

        private static Dictionary<int, P6RoomAccess> BuildAccessLookup(
            IReadOnlyList<P6RoomAccess> accesses)
        {
            Dictionary<int, P6RoomAccess> lookup =
                new Dictionary<int, P6RoomAccess>();
            for (int index = 0; index < accesses.Count; index++)
            {
                lookup.Add(accesses[index].AccessId, accesses[index]);
            }

            return lookup;
        }

        private static P6RoomGraphLabPlacement[] BuildRooms(
            Scene scene,
            Transform roomsParent,
            Transform roleParent,
            P6RoomGraphPlan plan,
            IReadOnlyDictionary<string, P6RoomCatalogEntry> catalog,
            SourceArt art)
        {
            P6RoomGraphLabPlacement[] placements =
                new P6RoomGraphLabPlacement[plan.Rooms.Count];
            Vector2Int macroSize = RoomTemplate2D.MacroCellSize;
            for (int index = 0; index < plan.Rooms.Count; index++)
            {
                P6RoomNode node = plan.Rooms[index];
                if (!catalog.TryGetValue(
                        node.PrefabId,
                        out P6RoomCatalogEntry entry))
                {
                    throw new InvalidOperationException(
                        $"P6 selected unknown P4 prefab {node.PrefabId}.");
                }

                GameObject instance =
                    (GameObject)PrefabUtility.InstantiatePrefab(
                        entry.Prefab,
                        scene);
                if (instance == null)
                {
                    throw new InvalidOperationException(
                        $"Failed to instantiate {entry.PrefabPath}.");
                }

                instance.name =
                    $"Node_{node.Id:00}_{node.Role}_{node.PrefabId}";
                instance.transform.SetParent(roomsParent);
                instance.transform.position = new Vector3(
                    node.MacroBounds.xMin * macroSize.x,
                    node.MacroBounds.yMin * macroSize.y,
                    0f);
                RoomTemplate2D template =
                    instance.GetComponent<RoomTemplate2D>();
                if (template == null)
                {
                    throw new InvalidOperationException(
                        $"P4 prefab instance has no RoomTemplate2D: "
                        + entry.PrefabPath);
                }

                BuildRoleMarker(roleParent, node, art);
                placements[index] =
                    new P6RoomGraphLabPlacement(
                        node.Id,
                        node.PrefabId,
                        node.MacroBounds,
                        node.Role,
                        node.OnMainPath,
                        entry.Prefab,
                        template);
            }

            return placements;
        }

        private static P6RoomGraphLabAccess[] BuildAccesses(
            IReadOnlyList<P6RoomAccess> roomAccesses,
            int routingScale,
            Transform parent,
            SourceArt art)
        {
            P6RoomGraphLabAccess[] snapshots =
                new P6RoomGraphLabAccess[roomAccesses.Count];
            for (int index = 0; index < roomAccesses.Count; index++)
            {
                P6RoomAccess access = roomAccesses[index];
                GameObject visual = CreateChild(
                    parent,
                    $"Access_{access.AccessId:00}_"
                    + $"Node_{access.NodeId:00}_{access.SocketId}");
                Vector2 socket = RoutingCellCenter(
                    access.SocketCell,
                    routingScale);
                Vector2 portal = RoutingCellCenter(
                    access.PortalCell,
                    routingScale);
                Color color = access.IsToolFreeMainRouteAccess
                    ? new Color(0.32f, 1f, 0.78f, 0.98f)
                    : new Color(0.88f, 0.56f, 1f, 0.94f);

                CreateLine(
                    visual.transform,
                    art.Square,
                    "SocketPortalLink",
                    socket,
                    portal,
                    0.3f,
                    color,
                    54);
                CreatePart(
                    visual.transform,
                    art.Square,
                    "SocketClaim",
                    socket,
                    new Vector2(0.86f, 0.86f),
                    45f,
                    color,
                    55);
                CreatePart(
                    visual.transform,
                    art.Star,
                    "Portal",
                    portal,
                    new Vector2(0.72f, 0.72f),
                    0f,
                    Color.white,
                    56);

                snapshots[index] =
                    new P6RoomGraphLabAccess(
                        access,
                        visual.transform);
            }

            return snapshots;
        }

        private static P6RoomGraphLabEdge[] BuildGraphEdges(
            IReadOnlyList<P6GraphEdge> graphEdges,
            IReadOnlyDictionary<int, P6RoomAccess> accesses,
            int routingScale,
            Transform mstParent,
            Transform loopParent,
            SourceArt art)
        {
            P6RoomGraphLabEdge[] snapshots =
                new P6RoomGraphLabEdge[graphEdges.Count];
            for (int index = 0; index < graphEdges.Count; index++)
            {
                P6GraphEdge edge = graphEdges[index];
                Transform parent = edge.Kind == P6EdgeKind.Mst
                    ? mstParent
                    : loopParent;
                GameObject visual = CreateChild(
                    parent,
                    $"{edge.Kind}_{edge.FirstNodeId:00}_{edge.SecondNodeId:00}");
                Vector2 first = RoutingCellCenter(
                    accesses[edge.FirstAccessId].PortalCell,
                    routingScale);
                Vector2 second = RoutingCellCenter(
                    accesses[edge.SecondAccessId].PortalCell,
                    routingScale);

                if (edge.Kind == P6EdgeKind.Mst)
                {
                    CreateLine(
                        visual.transform,
                        art.Square,
                        "MstGlow",
                        first,
                        second,
                        0.72f,
                        new Color(0.08f, 0.35f, 0.58f, 0.36f),
                        48);
                    CreateLine(
                        visual.transform,
                        art.Square,
                        "MstCore",
                        first,
                        second,
                        0.22f,
                        MstColor,
                        49);
                }
                else
                {
                    CreateDashedLine(
                        visual.transform,
                        art.Square,
                        first,
                        second,
                        1.15f,
                        0.48f,
                        LoopColor,
                        51);
                    CreatePart(
                        visual.transform,
                        art.Star,
                        "LoopSeal",
                        (first + second) * 0.5f,
                        new Vector2(1.7f, 1.7f),
                        0f,
                        LoopColor,
                        52);
                }

                snapshots[index] =
                    new P6RoomGraphLabEdge(
                        edge.FirstNodeId,
                        edge.SecondNodeId,
                        edge.FirstAccessId,
                        edge.SecondAccessId,
                        edge.Kind,
                        visual.transform);
            }

            return snapshots;
        }

        private static P6RoomGraphLabCorridor[] BuildCorridors(
            IReadOnlyList<P6GraphEdge> graphEdges,
            Transform parent,
            SourceArt art,
            int routingScale)
        {
            P6RoomGraphLabCorridor[] snapshots =
                new P6RoomGraphLabCorridor[graphEdges.Count];
            for (int index = 0; index < graphEdges.Count; index++)
            {
                P6GraphEdge edge = graphEdges[index];
                GameObject visual = CreateChild(
                    parent,
                    $"AStar_{edge.FirstNodeId:00}_{edge.SecondNodeId:00}");
                Vector2Int[] cells =
                    new Vector2Int[edge.CorridorCells.Count];
                for (int cellIndex = 0;
                     cellIndex < edge.CorridorCells.Count;
                     cellIndex++)
                {
                    cells[cellIndex] = edge.CorridorCells[cellIndex];
                    Vector2 center = RoutingCellCenter(
                        cells[cellIndex],
                        routingScale);
                    CreatePart(
                        visual.transform,
                        art.Square,
                        $"AStarCell_{cellIndex:00}",
                        center,
                        new Vector2(0.95f, 0.95f),
                        45f,
                        CorridorColor,
                        44);
                    if (cellIndex > 0)
                    {
                        CreateLine(
                            visual.transform,
                            art.Square,
                            $"AStarStep_{cellIndex:00}",
                            RoutingCellCenter(
                                cells[cellIndex - 1],
                                routingScale),
                            center,
                            0.42f,
                            new Color(
                                CorridorColor.r,
                                CorridorColor.g,
                                CorridorColor.b,
                                0.36f),
                            43);
                    }
                }

                snapshots[index] =
                    new P6RoomGraphLabCorridor(
                        edge.FirstNodeId,
                        edge.SecondNodeId,
                        edge.FirstAccessId,
                        edge.SecondAccessId,
                        cells,
                        visual.transform);
            }

            return snapshots;
        }

        private static P6PhysicalCorridorModule2D[]
            BuildPhysicalCorridorModules(
                P6RoomGraphPlan plan,
                IReadOnlyList<P6RoomGraphLabPlacement> placements,
                Transform parent,
                SourceArt art)
        {
            var modules =
                new List<P6PhysicalCorridorModule2D>();
            var cells = new HashSet<Vector2Int>(
                plan.CorridorNetwork.Cells);
            foreach (Vector2Int cell in cells)
            {
                P6CorridorConnectionSides connections =
                    GetCorridorConnections(cell, cells);
                P6CorridorModuleKind kind =
                    ClassifyCorridorModule(connections);
                GameObject moduleObject = CreateChild(
                    parent,
                    $"Corridor_{cell.x}_{cell.y}_{kind}");
                P6PhysicalCorridorModule2D module =
                    moduleObject.AddComponent<
                        P6PhysicalCorridorModule2D>();
                List<BoxCollider2D> surfaces =
                    BuildRoutingCellSurfaces(
                        moduleObject.transform,
                        cell,
                        connections,
                        plan.CorridorNetwork.RoutingScale,
                        art);
                module.Configure(
                    cell,
                    kind,
                    connections,
                    false,
                    -1,
                    surfaces.ToArray());
                modules.Add(module);
            }

            var placementByNode =
                new Dictionary<int, P6RoomGraphLabPlacement>();
            for (int index = 0; index < placements.Count; index++)
            {
                placementByNode.Add(
                    placements[index].NodeId,
                    placements[index]);
            }

            for (int index = 0;
                 index < plan.CorridorNetwork.RoomAccesses.Count;
                 index++)
            {
                P6RoomAccess access =
                    plan.CorridorNetwork.RoomAccesses[index];
                if (!placementByNode.TryGetValue(
                        access.NodeId,
                        out P6RoomGraphLabPlacement placement)
                    || placement.Instance == null)
                {
                    throw new InvalidOperationException(
                        $"Physical corridor access {access.AccessId} "
                        + "has no room placement.");
                }

                GameObject bridgeObject = CreateChild(
                    parent,
                    $"AccessBridge_{access.AccessId:00}_"
                    + $"Node_{access.NodeId:00}");
                List<BoxCollider2D> surfaces =
                    BuildAccessBridgeSurfaces(
                        bridgeObject.transform,
                        placement,
                        access,
                        plan.CorridorNetwork.RoutingScale,
                        art);
                P6PhysicalCorridorModule2D bridge =
                    bridgeObject.AddComponent<
                        P6PhysicalCorridorModule2D>();
                bridge.Configure(
                    access.PortalCell,
                    P6CorridorModuleKind.AccessBridge,
                    P6CorridorConnectionSides.None,
                    true,
                    access.AccessId,
                    surfaces.ToArray());
                modules.Add(bridge);
            }

            return modules.ToArray();
        }

        private static List<BoxCollider2D>
            BuildRoutingCellSurfaces(
                Transform parent,
                Vector2Int cell,
                P6CorridorConnectionSides connections,
                int routingScale,
                SourceArt art)
        {
            var surfaces = new List<BoxCollider2D>();
            Vector2 center = RoutingCellCenter(cell, routingScale);
            float baseY = center.y - 1.5f;
            Color stone = new Color(0.13f, 0.2f, 0.32f, 1f);
            Color edge = new Color(0.36f, 0.94f, 0.92f, 0.9f);

            surfaces.Add(
                CreatePhysicalSurface(
                    parent,
                    art.Square,
                    "Floor",
                    new Vector2(center.x, baseY),
                    new Vector2(5.8f, 0.55f),
                    stone));
            CreatePart(
                parent,
                art.Square,
                "FloorGlow",
                new Vector2(center.x, baseY + 0.31f),
                new Vector2(5.8f, 0.08f),
                0f,
                edge,
                18);

            bool vertical =
                (connections
                    & (P6CorridorConnectionSides.Up
                        | P6CorridorConnectionSides.Down))
                != 0;
            if (!vertical)
            {
                return surfaces;
            }

            float direction =
                ((cell.x + cell.y) & 1) == 0 ? 1f : -1f;
            for (int step = 1; step <= 3; step++)
            {
                float t = step / 3f;
                float x =
                    center.x
                    + direction
                    * (step % 2 == 0 ? -1.25f : 1.25f);
                float y = Mathf.Lerp(baseY, baseY + 4f, t);
                surfaces.Add(
                    CreatePhysicalSurface(
                        parent,
                        art.Square,
                        $"Stair_{step:00}",
                        new Vector2(x, y),
                        new Vector2(2.7f, 0.48f),
                        stone));
                CreatePart(
                    parent,
                    art.Square,
                    $"StairGlow_{step:00}",
                    new Vector2(x, y + 0.27f),
                    new Vector2(2.7f, 0.07f),
                    0f,
                    edge,
                    18);
            }

            return surfaces;
        }

        private static List<BoxCollider2D>
            BuildAccessBridgeSurfaces(
                Transform parent,
                P6RoomGraphLabPlacement placement,
                P6RoomAccess access,
                int routingScale,
                SourceArt art)
        {
            Vector3 roomOrigin =
                placement.Instance.transform.position;
            Vector2 start = new Vector2(
                roomOrigin.x + access.ValidationAnchor.x + 0.5f,
                roomOrigin.y + access.ValidationAnchor.y - 0.28f);
            Vector2 portal = RoutingCellCenter(
                access.PortalCell,
                routingScale);
            Vector2 end = new Vector2(portal.x, portal.y - 1.5f);
            float horizontalSteps =
                Mathf.Abs(end.x - start.x) / 2.3f;
            float verticalSteps =
                Mathf.Abs(end.y - start.y) / 1.15f;
            int stepCount = Mathf.Clamp(
                Mathf.CeilToInt(
                    Mathf.Max(horizontalSteps, verticalSteps)),
                1,
                24);

            var surfaces = new List<BoxCollider2D>(
                stepCount + 1);
            Color stone = new Color(0.16f, 0.24f, 0.36f, 1f);
            Color edge = new Color(0.5f, 1f, 0.8f, 0.92f);
            for (int step = 0; step <= stepCount; step++)
            {
                Vector2 position = Vector2.Lerp(
                    start,
                    end,
                    step / (float)stepCount);
                surfaces.Add(
                    CreatePhysicalSurface(
                        parent,
                        art.Square,
                        $"BridgeStep_{step:00}",
                        position,
                        new Vector2(2.5f, 0.48f),
                        stone));
                CreatePart(
                    parent,
                    art.Square,
                    $"BridgeGlow_{step:00}",
                    position + Vector2.up * 0.27f,
                    new Vector2(2.5f, 0.07f),
                    0f,
                    edge,
                    19);
            }

            return surfaces;
        }

        private static BoxCollider2D CreatePhysicalSurface(
            Transform parent,
            Sprite sprite,
            string name,
            Vector2 position,
            Vector2 size,
            Color color)
        {
            SpriteRenderer renderer = CreatePart(
                parent,
                sprite,
                name,
                position,
                size,
                0f,
                color,
                17);
            BoxCollider2D collider =
                renderer.gameObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = false;
            collider.size = sprite.bounds.size;
            collider.offset = sprite.bounds.center;
            return collider;
        }

        private static P6CorridorConnectionSides
            GetCorridorConnections(
                Vector2Int cell,
                HashSet<Vector2Int> cells)
        {
            P6CorridorConnectionSides result =
                P6CorridorConnectionSides.None;
            if (cells.Contains(cell + Vector2Int.left))
            {
                result |= P6CorridorConnectionSides.Left;
            }
            if (cells.Contains(cell + Vector2Int.right))
            {
                result |= P6CorridorConnectionSides.Right;
            }
            if (cells.Contains(cell + Vector2Int.down))
            {
                result |= P6CorridorConnectionSides.Down;
            }
            if (cells.Contains(cell + Vector2Int.up))
            {
                result |= P6CorridorConnectionSides.Up;
            }
            return result;
        }

        private static P6CorridorModuleKind
            ClassifyCorridorModule(
                P6CorridorConnectionSides connections)
        {
            int count = 0;
            foreach (P6CorridorConnectionSides side in new[]
            {
                P6CorridorConnectionSides.Left,
                P6CorridorConnectionSides.Right,
                P6CorridorConnectionSides.Down,
                P6CorridorConnectionSides.Up
            })
            {
                if ((connections & side) != 0)
                {
                    count++;
                }
            }

            if (count >= 3)
            {
                return P6CorridorModuleKind.Junction;
            }

            bool horizontal =
                (connections
                    & (P6CorridorConnectionSides.Left
                        | P6CorridorConnectionSides.Right))
                != 0;
            bool vertical =
                (connections
                    & (P6CorridorConnectionSides.Down
                        | P6CorridorConnectionSides.Up))
                != 0;
            if (horizontal && vertical)
            {
                return P6CorridorModuleKind.Corner;
            }
            if (vertical)
            {
                return P6CorridorModuleKind.VerticalStairs;
            }
            if (horizontal && count == 2)
            {
                return P6CorridorModuleKind.Horizontal;
            }
            return P6CorridorModuleKind.End;
        }

        private static void BuildRoleMarker(
            Transform parent,
            P6RoomNode node,
            SourceArt art)
        {
            GameObject marker =
                CreateChild(parent, $"Role_{node.Id:00}_{node.Role}");
            Vector2 center = RoomCenter(node);
            float top = node.MacroBounds.height
                * RoomTemplate2D.MacroCellSize.y * 0.5f;
            marker.transform.position =
                new Vector3(center.x, center.y + top - 1.2f, 0f);
            Color color = RoleColor(node.Role);
            Sprite glyph = UsesCrystalGlyph(node.Role)
                ? art.Crystal
                : art.Star;
            CreatePart(
                marker.transform,
                art.Square,
                "RoleDiamond",
                Vector2.zero,
                new Vector2(1.75f, 1.75f),
                45f,
                new Color(color.r, color.g, color.b, 0.86f),
                58);
            CreatePart(
                marker.transform,
                glyph,
                "RoleGlyph",
                Vector2.zero,
                new Vector2(0.82f, 0.82f),
                0f,
                Color.white,
                59);
            if (node.OnMainPath)
            {
                CreatePart(
                    marker.transform,
                    art.Star,
                    "MainRouteSpark",
                    new Vector2(1.15f, 0f),
                    new Vector2(0.45f, 0.45f),
                    0f,
                    MstColor,
                    60);
            }
        }

        private static void BuildMainRoutePulse(
            P6RoomGraphPlan plan,
            IReadOnlyDictionary<int, P6RoomNode> nodes,
            Transform parent,
            SourceArt art)
        {
            GameObject route = CreateChild(parent, "MainRoutePulse");
            for (int index = 0;
                 index < plan.MainPathNodeIds.Count;
                 index++)
            {
                int nodeId = plan.MainPathNodeIds[index];
                Vector2 center = RoomCenter(nodes[nodeId]);
                CreatePart(
                    route.transform,
                    art.Star,
                    $"Main_{index:00}",
                    center,
                    new Vector2(0.52f, 0.52f),
                    0f,
                    new Color(0.82f, 0.98f, 1f, 0.94f),
                    57);
            }
        }

        private static Transform BuildEndpointMarker(
            Transform parent,
            string name,
            Vector2 position,
            Color color,
            SourceArt art)
        {
            GameObject marker = CreateChild(parent, name);
            marker.transform.position =
                new Vector3(position.x, position.y, 0f);
            CreatePart(
                marker.transform,
                art.Square,
                "OuterDiamond",
                Vector2.zero,
                new Vector2(4.4f, 4.4f),
                45f,
                new Color(color.r, color.g, color.b, 0.28f),
                61);
            CreatePart(
                marker.transform,
                art.Square,
                "CoreDiamond",
                Vector2.zero,
                new Vector2(2.4f, 2.4f),
                45f,
                color,
                62);
            CreatePart(
                marker.transform,
                art.Star,
                "EndpointStar",
                Vector2.zero,
                new Vector2(1.15f, 1.15f),
                0f,
                Color.white,
                63);

            float direction = name == "Start" ? 1f : -1f;
            CreateLine(
                marker.transform,
                art.Square,
                "ArrowUpper",
                new Vector2(direction * 2.7f, 0f),
                new Vector2(direction * 1.8f, 0.72f),
                0.26f,
                color,
                63);
            CreateLine(
                marker.transform,
                art.Square,
                "ArrowLower",
                new Vector2(direction * 2.7f, 0f),
                new Vector2(direction * 1.8f, -0.72f),
                0.26f,
                color,
                63);
            return marker.transform;
        }

        private static Transform BuildBackdrop(
            Transform parent,
            SourceArt art,
            RectInt routingBounds,
            int routingScale)
        {
            GameObject backdrop =
                CreateChild(parent, "MoonPalaceBackdrop_2DFantasy");
            Bounds bounds = ToWorldBounds(
                routingBounds,
                routingScale);
            float width = Mathf.Max(84f, bounds.size.x + 24f);
            float height = Mathf.Max(48f, bounds.size.y + 16f);
            Vector2 center = bounds.center;
            CreatePart(
                backdrop.transform,
                art.Sky,
                "MoonSky",
                center,
                new Vector2(width, height),
                0f,
                new Color(0.2f, 0.3f, 0.58f, 1f),
                -200);
            CreatePart(
                backdrop.transform,
                art.MountainA,
                "MoonMountain_Left",
                new Vector2(center.x - width * 0.25f, bounds.min.y + 6f),
                new Vector2(width * 0.56f, 18f),
                0f,
                new Color(0.28f, 0.36f, 0.62f, 0.72f),
                -190);
            CreatePart(
                backdrop.transform,
                art.MountainB,
                "MoonMountain_Right",
                new Vector2(center.x + width * 0.25f, bounds.min.y + 5f),
                new Vector2(width * 0.56f, 17f),
                0f,
                new Color(0.18f, 0.25f, 0.48f, 0.8f),
                -189);
            CreatePart(
                backdrop.transform,
                art.Moon,
                "Moon",
                new Vector2(
                    bounds.max.x - 5.5f,
                    bounds.max.y - 4.8f),
                new Vector2(10f, 10f),
                0f,
                new Color(0.84f, 0.94f, 1f, 0.82f),
                -180);

            Vector2[] stars =
            {
                new Vector2(-24f, 12f),
                new Vector2(-17f, -6f),
                new Vector2(-9f, 14f),
                new Vector2(2f, 9f),
                new Vector2(12f, 15f),
                new Vector2(21f, 5f),
                new Vector2(27f, 13f)
            };
            for (int index = 0; index < stars.Length; index++)
            {
                CreatePart(
                    backdrop.transform,
                    art.Star,
                    $"BackdropStar_{index:00}",
                    center + stars[index],
                    new Vector2(
                        0.55f + (index % 3) * 0.18f,
                        0.55f + (index % 3) * 0.18f),
                    0f,
                    new Color(0.7f, 0.92f, 1f, 0.62f),
                    -175);
            }

            CreatePart(
                backdrop.transform,
                art.Cloud,
                "CloudVeil",
                new Vector2(center.x - width * 0.15f, bounds.max.y - 4f),
                new Vector2(18f, 5f),
                0f,
                new Color(0.58f, 0.74f, 0.94f, 0.24f),
                -176);
            return backdrop.transform;
        }

        private static void BuildMacroGrid(
            Transform parent,
            Sprite square,
            RectInt canvas)
        {
            GameObject grid = CreateChild(
                parent,
                $"MacroCanvas_{canvas.width}x{canvas.height}");
            Vector2Int cell = RoomTemplate2D.MacroCellSize;
            float xMin = canvas.xMin * cell.x;
            float xMax = canvas.xMax * cell.x;
            float yMin = canvas.yMin * cell.y;
            float yMax = canvas.yMax * cell.y;
            Color gridColor = new Color(0.42f, 0.72f, 1f, 0.18f);
            for (int x = canvas.xMin; x <= canvas.xMax; x++)
            {
                float worldX = x * cell.x;
                CreateLine(
                    grid.transform,
                    square,
                    $"GridV_{x:00}",
                    new Vector2(worldX, yMin),
                    new Vector2(worldX, yMax),
                    x == canvas.xMin || x == canvas.xMax
                        ? 0.2f
                        : 0.1f,
                    gridColor,
                    38);
            }

            for (int y = canvas.yMin; y <= canvas.yMax; y++)
            {
                float worldY = y * cell.y;
                CreateLine(
                    grid.transform,
                    square,
                    $"GridH_{y:00}",
                    new Vector2(xMin, worldY),
                    new Vector2(xMax, worldY),
                    y == canvas.yMin || y == canvas.yMax
                        ? 0.2f
                        : 0.1f,
                    gridColor,
                    38);
            }
        }

        private static Camera BuildCamera(
            Transform parent,
            Bounds bounds)
        {
            GameObject cameraObject = CreateChild(parent, "Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(
                bounds.center.x,
                bounds.center.y,
                -20f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor =
                new Color(0.025f, 0.045f, 0.12f, 1f);
            float heightSize = bounds.extents.y + 4f;
            float widthSize = bounds.extents.x / 1.68f + 3f;
            camera.orthographicSize =
                Mathf.Max(heightSize, widthSize);
            return camera;
        }

        private static Light BuildDirectionalLight(Transform parent)
        {
            GameObject lightObject =
                CreateChild(parent, "Directional Light");
            lightObject.transform.rotation =
                Quaternion.Euler(42f, -28f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.76f, 0.88f, 1f, 1f);
            light.intensity = 1.05f;
            return light;
        }

        private static void ValidatePrefabSources(
            P6RoomGraphLabContract contract)
        {
            for (int index = 0;
                 index < contract.Placements.Count;
                 index++)
            {
                P6RoomGraphLabPlacement placement =
                    contract.Placements[index];
                if (placement.Instance == null
                    || placement.SourcePrefab == null)
                {
                    throw new InvalidOperationException(
                        $"P6 placement {index} has a missing source.");
                }

                GameObject source =
                    PrefabUtility.GetCorrespondingObjectFromSource(
                        placement.Instance.gameObject);
                if (source != placement.SourcePrefab)
                {
                    throw new InvalidOperationException(
                        $"P6 placement {index} is not a live instance of "
                        + placement.SourcePrefab.name);
                }
            }
        }

        private static void ValidateNoMissingScripts(GameObject root)
        {
            Transform[] transforms =
                root.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                int missing =
                    GameObjectUtility
                        .GetMonoBehavioursWithMissingScriptCount(
                            transforms[index].gameObject);
                if (missing > 0)
                {
                    throw new InvalidOperationException(
                        $"{transforms[index].name} has {missing} "
                        + "missing script component(s).");
                }
            }
        }

        private static void ValidateCameraAndLight()
        {
            Camera[] cameras =
                UnityEngine.Object.FindObjectsByType<Camera>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            Light[] lights =
                UnityEngine.Object.FindObjectsByType<Light>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            int directionalCount = 0;
            for (int index = 0; index < lights.Length; index++)
            {
                if (lights[index].type == LightType.Directional
                    && lights[index].gameObject.activeInHierarchy)
                {
                    directionalCount++;
                }
            }

            if (cameras.Length != 1
                || !cameras[0].orthographic
                || !cameras[0].CompareTag("MainCamera")
                || directionalCount != 1)
            {
                throw new InvalidOperationException(
                    "P6 Lab requires one orthographic Main Camera and "
                    + "exactly one active Directional Light.");
            }
        }

        private static Color RoleColor(RoomRole role)
        {
            if ((role & RoomRole.Start) != 0)
            {
                return new Color(0.3f, 1f, 0.58f, 1f);
            }

            if ((role & RoomRole.Exit) != 0)
            {
                return new Color(1f, 0.3f, 0.66f, 1f);
            }

            if ((role & RoomRole.ShopCorridor) != 0)
            {
                return new Color(1f, 0.64f, 0.16f, 1f);
            }

            if ((role & RoomRole.SupplyCorridor) != 0)
            {
                return new Color(0.28f, 0.72f, 1f, 1f);
            }

            if ((role & RoomRole.RecordRoom) != 0)
            {
                return new Color(0.72f, 0.45f, 1f, 1f);
            }

            if ((role & RoomRole.MaruStatue) != 0)
            {
                return new Color(1f, 0.36f, 0.3f, 1f);
            }

            if ((role & RoomRole.RiskyChoice) != 0)
            {
                return new Color(1f, 0.24f, 0.16f, 1f);
            }

            if ((role & RoomRole.LandmarkJunction) != 0)
            {
                return new Color(1f, 0.82f, 0.18f, 1f);
            }

            if ((role & RoomRole.Landmark) != 0)
            {
                return new Color(1f, 0.88f, 0.34f, 1f);
            }

            if ((role & RoomRole.LocalEvent) != 0)
            {
                return new Color(0.36f, 1f, 0.82f, 1f);
            }

            if ((role & RoomRole.Shortcut) != 0)
            {
                return LoopColor;
            }

            return new Color(0.62f, 0.86f, 1f, 1f);
        }

        private static bool UsesCrystalGlyph(RoomRole role)
        {
            return (role & (
                RoomRole.RecordRoom
                | RoomRole.MaruStatue
                | RoomRole.Landmark
                | RoomRole.LandmarkJunction
                | RoomRole.LocalEvent)) != 0;
        }

        private static Vector2 RoomCenter(P6RoomNode node)
        {
            Vector2Int cell = RoomTemplate2D.MacroCellSize;
            return new Vector2(
                (node.MacroBounds.xMin
                    + node.MacroBounds.width * 0.5f) * cell.x,
                (node.MacroBounds.yMin
                    + node.MacroBounds.height * 0.5f) * cell.y);
        }

        private static Vector2 RoutingCellCenter(
            Vector2Int cell,
            int routingScale)
        {
            Vector2Int size = RoomTemplate2D.MacroCellSize;
            return new Vector2(
                cell.x * size.x / (float)routingScale,
                cell.y * size.y / (float)routingScale);
        }

        private static Bounds ToWorldBounds(
            RectInt bounds,
            int routingScale)
        {
            Vector2Int cell = RoomTemplate2D.MacroCellSize;
            Vector3 minimum = new Vector3(
                bounds.xMin * cell.x / (float)routingScale,
                bounds.yMin * cell.y / (float)routingScale,
                0f);
            Vector3 maximum = new Vector3(
                bounds.xMax * cell.x / (float)routingScale,
                bounds.yMax * cell.y / (float)routingScale,
                0f);
            Bounds result = new Bounds(
                (minimum + maximum) * 0.5f,
                maximum - minimum);
            result.Expand(new Vector3(4f, 4f, 1f));
            return result;
        }

        private static void CreateDashedLine(
            Transform parent,
            Sprite sprite,
            Vector2 from,
            Vector2 to,
            float dashLength,
            float width,
            Color color,
            int sortingOrder)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.001f)
            {
                return;
            }

            Vector2 direction = delta / length;
            float stride = dashLength * 1.75f;
            int count = Mathf.Max(1, Mathf.CeilToInt(length / stride));
            for (int index = 0; index < count; index++)
            {
                float start = index * stride;
                float end = Mathf.Min(start + dashLength, length);
                if (end <= start)
                {
                    continue;
                }

                CreateLine(
                    parent,
                    sprite,
                    $"Dash_{index:00}",
                    from + direction * start,
                    from + direction * end,
                    width,
                    color,
                    sortingOrder);
            }
        }

        private static SpriteRenderer CreateLine(
            Transform parent,
            Sprite sprite,
            string name,
            Vector2 from,
            Vector2 to,
            float width,
            Color color,
            int sortingOrder)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            return CreatePart(
                parent,
                sprite,
                name,
                (from + to) * 0.5f,
                new Vector2(Mathf.Max(0.01f, length), width),
                angle,
                color,
                sortingOrder);
        }

        private static SpriteRenderer CreatePart(
            Transform parent,
            Sprite sprite,
            string name,
            Vector2 position,
            Vector2 size,
            float rotation,
            Color color,
            int sortingOrder)
        {
            GameObject visual = CreateChild(parent, name);
            visual.transform.localPosition =
                new Vector3(position.x, position.y, 0f);
            visual.transform.localRotation =
                Quaternion.Euler(0f, 0f, rotation);
            visual.transform.localScale = CalculateFitScale(sprite, size);
            SpriteRenderer renderer =
                visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static Vector3 CalculateFitScale(
            Sprite sprite,
            Vector2 size)
        {
            if (sprite == null)
            {
                return Vector3.one;
            }

            Vector2 spriteSize = sprite.bounds.size;
            return new Vector3(
                spriteSize.x > 0f ? size.x / spriteSize.x : 1f,
                spriteSize.y > 0f ? size.y / spriteSize.y : 1f,
                1f);
        }

        private static SourceArt LoadSourceArt()
        {
            return new SourceArt(
                LoadSprite(SkySpritePath),
                LoadSprite(MountainASpritePath),
                LoadSprite(MountainBSpritePath),
                LoadSprite(MoonSpritePath),
                LoadSprite(CloudSpritePath),
                LoadSprite(SquareSpritePath),
                LoadSprite(StarSpritePath),
                LoadSprite(CrystalSpritePath));
        }

        private static Sprite LoadSprite(string path)
        {
            UnityEngine.Object[] assets =
                AssetDatabase.LoadAllAssetsAtPath(path);
            for (int index = 0; index < assets.Length; index++)
            {
                if (assets[index] is Sprite sprite)
                {
                    return sprite;
                }
            }

            throw new InvalidOperationException(
                $"P6 source sprite is missing: {path}");
        }

        private static GameObject CreateChild(
            Transform parent,
            string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent);
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;
            return child;
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path)
                || path == "Assets"
                || AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent =
                Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            string name = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, name);
        }

        private readonly struct ShowcaseGeneration
        {
            public ShowcaseGeneration(
                int requestedSeed,
                P6GenerationResult result)
            {
                RequestedSeed = requestedSeed;
                Result = result;
            }

            public int RequestedSeed { get; }
            public P6GenerationResult Result { get; }
        }

        private readonly struct SourceArt
        {
            public SourceArt(
                Sprite sky,
                Sprite mountainA,
                Sprite mountainB,
                Sprite moon,
                Sprite cloud,
                Sprite square,
                Sprite star,
                Sprite crystal)
            {
                Sky = sky;
                MountainA = mountainA;
                MountainB = mountainB;
                Moon = moon;
                Cloud = cloud;
                Square = square;
                Star = star;
                Crystal = crystal;
            }

            public Sprite Sky { get; }
            public Sprite MountainA { get; }
            public Sprite MountainB { get; }
            public Sprite Moon { get; }
            public Sprite Cloud { get; }
            public Sprite Square { get; }
            public Sprite Star { get; }
            public Sprite Crystal { get; }
        }
    }
}

#endif
