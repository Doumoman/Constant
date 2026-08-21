#if LEGACY_DISABLED
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Debugging;
using StarNight.Generation.P6;
using StarNight.Rooms;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace StarNight.Tests.PlayMode
{
    public sealed class P6RoomGraphLabPlayModeTests
    {
        private const string ScenePath =
            "Assets/StarNight/Scenes/Labs/"
            + "P6_MoonRoomGraphGeneratorLab.unity";

        [UnityTest]
        public IEnumerator MoonRoomGraphLab_RegeneratesItsFixedX2SnapshotAndSurvivesOneFrame()
        {
            yield return LoadLabSceneAsync();

            Scene scene = SceneManager.GetActiveScene();
            Assert.That(scene.IsValid(), Is.True);
            Assert.That(scene.isLoaded, Is.True);
            Assert.That(scene.path, Is.EqualTo(ScenePath));

            P6RoomGraphLabContract[] contracts =
                FindSceneComponents<P6RoomGraphLabContract>(scene);
            Assert.That(
                contracts.Length,
                Is.EqualTo(1),
                "The P6 Lab requires exactly one scene contract.");
            P6RoomGraphLabContract contract = contracts[0];

            Assert.That(
                contract.FixedSeed,
                Is.GreaterThanOrEqualTo(620600),
                "The integrated P6/P7 showcase search must expose a legal "
                + "enemy room and a legal trap room.");
            Assert.That(contract.IsX2, Is.True);
            Assert.That(contract.StageSlot, Is.EqualTo(P6StageSlot.X2));
            Assert.That(
                contract.GenerationFingerprint,
                Is.Not.Null.And.Not.Empty);
            Assert.That(
                contract.SourceLibrary,
                Is.Not.Null);
            Assert.That(
                contract.SourceLibrary.Region,
                Is.EqualTo(RoomRegion.MoonPalace));
            Assert.That(
                contract.SourceLibrary.RoomPrefabs.Count,
                Is.EqualTo(33));

            Assert.That(
                contract.RefreshValidation(),
                Is.True,
                contract.LastValidation);
            Assert.That(
                contract.ValidationPassed,
                Is.True,
                contract.LastValidation);
            Assert.That(contract.Issues, Is.Empty);
            Assert.That(contract.LastValidation, Is.EqualTo("PASS"));
            Assert.That(contract.RiskyChoiceSatisfied, Is.True);
            Assert.That(contract.LandmarkPrioritySatisfied, Is.True);
            Assert.That(contract.X3RoleSequenceSatisfied, Is.True);

            AssertPlacements(contract, scene);
            AssertGraphAndCorridors(contract);
            AssertPhysicalCorridors(contract, scene);
            AssertPresentation(contract, scene);
            Assert.That(
                CountMissingMonoBehaviours(scene),
                Is.EqualTo(0),
                "The P6 Lab contains missing MonoBehaviour scripts.");
            AssertNoVisibleText(scene);

            yield return null;

            Assert.That(contract, Is.Not.Null);
            Assert.That(contract.enabled, Is.True);
            Assert.That(contract.gameObject.activeInHierarchy, Is.True);
            Assert.That(contract.ValidationPassed, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        private static void AssertPlacements(
            P6RoomGraphLabContract contract,
            Scene scene)
        {
            Assert.That(
                contract.Placements.Count,
                Is.InRange(
                    P6RoomGraphLabContract.MinimumRoomCount,
                    P6RoomGraphLabContract.MaximumRoomCount));
            Assert.That(
                contract.SourcePrefabs.Count,
                Is.EqualTo(contract.Placements.Count));
            Assert.That(
                contract.SourceRoomIds.Count,
                Is.EqualTo(contract.Placements.Count));

            RectInt canvas = new RectInt(
                0,
                0,
                P6RoomGraphLabContract.CanvasWidth,
                P6RoomGraphLabContract.CanvasHeight);
            HashSet<int> nodeIds = new HashSet<int>();
            Dictionary<string, int> uses =
                new Dictionary<string, int>();
            RoomRole assignedRoles = RoomRole.None;

            for (int first = 0;
                 first < contract.Placements.Count;
                 first++)
            {
                P6RoomGraphLabPlacement placement =
                    contract.Placements[first];
                Assert.That(nodeIds.Add(placement.NodeId), Is.True);
                Assert.That(
                    Contains(canvas, placement.MacroBounds),
                    Is.True,
                    $"Placement {placement.NodeId} exceeds the canvas.");
                Assert.That(placement.SourcePrefab, Is.Not.Null);
                Assert.That(placement.Instance, Is.Not.Null);
                Assert.That(
                    placement.Instance.gameObject.scene,
                    Is.EqualTo(scene));
                Assert.That(
                    placement.Instance.RoomId,
                    Is.EqualTo(placement.PrefabId));
                Assert.That(
                    contract.SourcePrefabs[first],
                    Is.SameAs(placement.SourcePrefab));
                Assert.That(
                    contract.SourceRoomIds[first],
                    Is.EqualTo(placement.PrefabId));

                for (int second = first + 1;
                     second < contract.Placements.Count;
                     second++)
                {
                    Assert.That(
                        placement.MacroBounds.Overlaps(
                            contract.Placements[second].MacroBounds),
                        Is.False,
                        $"Placements {placement.NodeId} and "
                        + $"{contract.Placements[second].NodeId} overlap.");
                }

                uses.TryGetValue(
                    placement.PrefabId,
                    out int count);
                uses[placement.PrefabId] = count + 1;
                assignedRoles |= placement.Role;
            }

            Assert.That(
                uses.Values.All(count =>
                    count <= P6RoomGraphLabContract.MaximumPrefabUses),
                Is.True);
            RoomRole required =
                RoomRole.Landmark
                | RoomRole.ShopCorridor
                | RoomRole.RecordRoom
                | RoomRole.MaruStatue;
            Assert.That(
                (assignedRoles & required),
                Is.EqualTo(required));
            Assert.That(
                nodeIds.Contains(contract.StartNodeId),
                Is.True);
            Assert.That(
                nodeIds.Contains(contract.ExitNodeId),
                Is.True);
            Assert.That(contract.StartNodeId, Is.Not.EqualTo(contract.ExitNodeId));
            Assert.That(contract.StartMarker, Is.Not.Null);
            Assert.That(contract.ExitMarker, Is.Not.Null);
        }

        private static void AssertGraphAndCorridors(
            P6RoomGraphLabContract contract)
        {
            int roomCount = contract.Placements.Count;
            Assert.That(contract.RoutingScale, Is.EqualTo(2));
            Assert.That(
                contract.CorridorRoutingBounds,
                Is.EqualTo(
                    new RectInt(
                        -1,
                        -1,
                        P6RoomGraphLabContract.CanvasWidth * 2 + 3,
                        P6RoomGraphLabContract.CanvasHeight * 2 + 3)),
                "The Lab snapshot must use the routing-scale-2 lattice.");
            int mstCount = contract.Edges.Count(edge =>
                edge.Kind == P6EdgeKind.Mst);
            int loopCount = contract.Edges.Count(edge =>
                edge.Kind == P6EdgeKind.Additional);
            Assert.That(mstCount, Is.EqualTo(roomCount - 1));
            Assert.That(loopCount, Is.InRange(1, 3));
            Assert.That(
                contract.Edges.Count,
                Is.EqualTo(mstCount + loopCount));
            Assert.That(
                contract.Corridors.Count,
                Is.EqualTo(contract.Edges.Count));

            HashSet<int> validNodes =
                new HashSet<int>(
                    contract.Placements.Select(
                        placement => placement.NodeId));
            Dictionary<int, P6RoomGraphLabPlacement> placementByNode =
                contract.Placements.ToDictionary(
                    placement => placement.NodeId);
            Dictionary<int, P6RoomGraphLabAccess> accessById =
                new Dictionary<int, P6RoomGraphLabAccess>();
            Assert.That(contract.Accesses, Is.Not.Empty);
            for (int index = 0;
                 index < contract.Accesses.Count;
                 index++)
            {
                P6RoomGraphLabAccess access =
                    contract.Accesses[index];
                Assert.That(
                    accessById.ContainsKey(access.AccessId),
                    Is.False,
                    $"Duplicate Lab access id {access.AccessId}.");
                accessById.Add(access.AccessId, access);
                Assert.That(
                    access.AccessId,
                    Is.EqualTo(index),
                    "Lab access ids must remain dense snapshot indices.");
                Assert.That(
                    placementByNode.ContainsKey(access.NodeId),
                    Is.True,
                    $"Access {access.AccessId} owns an invalid node.");
                Assert.That(access.SocketId, Is.Not.Null.And.Not.Empty);
                Assert.That(
                    contract.CorridorRoutingBounds.Contains(
                        access.SocketCell),
                    Is.True);
                Assert.That(
                    contract.CorridorRoutingBounds.Contains(
                        access.PortalCell),
                    Is.True);
                Assert.That(
                    ScaledRoomInterior(
                            placementByNode[access.NodeId].MacroBounds)
                        .Contains(access.SocketCell),
                    Is.True,
                    $"Access {access.AccessId} socket is outside its room.");
                Assert.That(
                    ScaledRoomInterior(
                            placementByNode[access.NodeId].MacroBounds)
                        .Contains(access.PortalCell),
                    Is.False,
                    $"Access {access.AccessId} portal remained inside "
                    + "its room.");
                Assert.That(
                    access.PortalCell - access.SocketCell,
                    Is.EqualTo(
                        ExpectedPortalDelta(access.SocketSide)));
                Assert.That(access.Visual, Is.Not.Null);
            }

            HashSet<string> edgeKeys =
                new HashSet<string>();
            Dictionary<string, P6RoomGraphLabEdge> edgeByKey =
                new Dictionary<string, P6RoomGraphLabEdge>();
            for (int index = 0;
                 index < contract.Edges.Count;
                 index++)
            {
                P6RoomGraphLabEdge edge = contract.Edges[index];
                Assert.That(validNodes.Contains(edge.FirstNodeId), Is.True);
                Assert.That(validNodes.Contains(edge.SecondNodeId), Is.True);
                Assert.That(
                    edge.FirstNodeId,
                    Is.Not.EqualTo(edge.SecondNodeId));
                Assert.That(
                    accessById.ContainsKey(edge.FirstAccessId),
                    Is.True);
                Assert.That(
                    accessById.ContainsKey(edge.SecondAccessId),
                    Is.True);
                Assert.That(
                    accessById[edge.FirstAccessId].NodeId,
                    Is.EqualTo(edge.FirstNodeId));
                Assert.That(
                    accessById[edge.SecondAccessId].NodeId,
                    Is.EqualTo(edge.SecondNodeId));
                Assert.That(
                    edgeKeys.Add(EdgeKey(
                        edge.FirstNodeId,
                        edge.SecondNodeId)),
                    Is.True);
                edgeByKey.Add(
                    EdgeKey(
                        edge.FirstNodeId,
                        edge.SecondNodeId),
                    edge);
                Assert.That(edge.Visual, Is.Not.Null);
            }

            HashSet<string> corridorKeys =
                new HashSet<string>();
            Dictionary<string, P6RoomGraphLabCorridor> corridorByKey =
                new Dictionary<string, P6RoomGraphLabCorridor>();
            for (int index = 0;
                 index < contract.Corridors.Count;
                 index++)
            {
                P6RoomGraphLabCorridor corridor =
                    contract.Corridors[index];
                string key = EdgeKey(
                    corridor.FirstNodeId,
                    corridor.SecondNodeId);
                Assert.That(edgeKeys.Contains(key), Is.True);
                Assert.That(corridorKeys.Add(key), Is.True);
                corridorByKey.Add(key, corridor);
                Assert.That(corridor.Visual, Is.Not.Null);
                Assert.That(
                    corridor.MacroCells.Count,
                    Is.GreaterThan(0));

                P6RoomGraphLabEdge edge = edgeByKey[key];
                Assert.That(
                    corridor.FirstAccessId,
                    Is.EqualTo(edge.FirstAccessId));
                Assert.That(
                    corridor.SecondAccessId,
                    Is.EqualTo(edge.SecondAccessId));
                Assert.That(
                    accessById.ContainsKey(corridor.FirstAccessId),
                    Is.True);
                Assert.That(
                    accessById.ContainsKey(corridor.SecondAccessId),
                    Is.True);
                P6RoomGraphLabAccess firstAccess =
                    accessById[corridor.FirstAccessId];
                P6RoomGraphLabAccess secondAccess =
                    accessById[corridor.SecondAccessId];
                Assert.That(
                    firstAccess.NodeId,
                    Is.EqualTo(corridor.FirstNodeId));
                Assert.That(
                    secondAccess.NodeId,
                    Is.EqualTo(corridor.SecondNodeId));
                Assert.That(
                    corridor.MacroCells[0],
                    Is.EqualTo(firstAccess.PortalCell));
                Assert.That(
                    corridor.MacroCells[
                        corridor.MacroCells.Count - 1],
                    Is.EqualTo(secondAccess.PortalCell));
                if (corridor.MacroCells.Count == 1)
                {
                    Assert.That(
                        firstAccess.PortalCell,
                        Is.EqualTo(secondAccess.PortalCell),
                        $"Single-cell corridor {key} must be a shared portal.");
                }

                for (int cellIndex = 0;
                     cellIndex < corridor.MacroCells.Count;
                     cellIndex++)
                {
                    Vector2Int cell =
                        corridor.MacroCells[cellIndex];
                    Assert.That(
                        contract.CorridorRoutingBounds.Contains(cell),
                        Is.True);
                    for (int roomIndex = 0;
                         roomIndex < contract.Placements.Count;
                         roomIndex++)
                    {
                        Assert.That(
                            ScaledRoomInterior(
                                    contract.Placements[roomIndex]
                                        .MacroBounds)
                                .Contains(cell),
                            Is.False,
                            $"Corridor {key} enters placement "
                            + $"{contract.Placements[roomIndex].NodeId} "
                            + $"at {cell}.");
                    }
                    if (cellIndex > 0)
                    {
                        Vector2Int previous =
                            corridor.MacroCells[cellIndex - 1];
                        int distance =
                            Mathf.Abs(cell.x - previous.x)
                            + Mathf.Abs(cell.y - previous.y);
                        Assert.That(
                            distance,
                            Is.EqualTo(1),
                            $"Corridor {key} is discontinuous.");
                    }
                }
            }

            Assert.That(corridorKeys.SetEquals(edgeKeys), Is.True);
            AssertAdditionalPhysicalSegments(
                edgeByKey,
                corridorByKey);
            Assert.That(
                ContainsPhysicalGridCycle(
                    contract.Corridors.SelectMany(
                        corridor => corridor.MacroCells)),
                Is.True,
                "The X-2 Lab corridor lattice has no physical loop.");
            Assert.That(
                IsGraphConnected(
                    validNodes,
                    contract.Edges,
                    contract.StartNodeId),
                Is.True);
        }

        private static void AssertAdditionalPhysicalSegments(
            IReadOnlyDictionary<string, P6RoomGraphLabEdge> edgeByKey,
            IReadOnlyDictionary<string, P6RoomGraphLabCorridor> corridorByKey)
        {
            HashSet<string> occupied =
                new HashSet<string>();
            foreach (KeyValuePair<string, P6RoomGraphLabEdge> pair
                     in edgeByKey.Where(pair =>
                         pair.Value.Kind == P6EdgeKind.Mst))
            {
                AddPhysicalSegments(
                    occupied,
                    corridorByKey[pair.Key].MacroCells);
            }

            foreach (KeyValuePair<string, P6RoomGraphLabEdge> pair
                     in edgeByKey.Where(pair =>
                         pair.Value.Kind == P6EdgeKind.Additional))
            {
                IReadOnlyList<Vector2Int> cells =
                    corridorByKey[pair.Key].MacroCells;
                bool addsSegment = false;
                for (int index = 1;
                     index < cells.Count;
                     index++)
                {
                    if (!occupied.Contains(
                            PhysicalSegmentKey(
                                cells[index - 1],
                                cells[index])))
                    {
                        addsSegment = true;
                        break;
                    }
                }

                Assert.That(
                    addsSegment,
                    Is.True,
                    $"Additional Lab corridor {pair.Key} "
                    + "does not add a physical segment.");
                AddPhysicalSegments(occupied, cells);
            }
        }

        private static void AddPhysicalSegments(
            HashSet<string> destination,
            IReadOnlyList<Vector2Int> cells)
        {
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
            IEnumerable<Vector2Int> source)
        {
            HashSet<Vector2Int> cells =
                new HashSet<Vector2Int>(source);
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
                    Assert.Fail($"Invalid Lab socket side {side}.");
                    return Vector2Int.zero;
            }
        }

        private static RectInt ScaledRoomInterior(RectInt macroBounds)
        {
            return new RectInt(
                macroBounds.xMin * 2 + 1,
                macroBounds.yMin * 2 + 1,
                macroBounds.width * 2 - 1,
                macroBounds.height * 2 - 1);
        }

        private static void AssertPresentation(
            P6RoomGraphLabContract contract,
            Scene scene)
        {
            Assert.That(contract.MoonBackdrop, Is.Not.Null);
            Assert.That(contract.MstOverlay, Is.Not.Null);
            Assert.That(contract.LoopOverlay, Is.Not.Null);
            Assert.That(contract.CorridorOverlay, Is.Not.Null);
            Assert.That(contract.RoleOverlay, Is.Not.Null);
            Assert.That(contract.StageCamera, Is.Not.Null);
            Assert.That(contract.StageCamera.orthographic, Is.True);
            Assert.That(
                contract.StageCamera.gameObject.scene,
                Is.EqualTo(scene));
            Assert.That(Camera.main, Is.SameAs(contract.StageCamera));
            Assert.That(contract.DirectionalLight, Is.Not.Null);
            Assert.That(
                contract.DirectionalLight.type,
                Is.EqualTo(LightType.Directional));
            Assert.That(contract.DirectionalLight.enabled, Is.True);
            Assert.That(
                contract.DirectionalLight.gameObject.activeInHierarchy,
                Is.True);

            Light[] directional = FindSceneComponents<Light>(scene)
                .Where(light =>
                    light.type == LightType.Directional
                    && light.enabled
                    && light.gameObject.activeInHierarchy)
                .ToArray();
            Assert.That(directional.Length, Is.EqualTo(1));
            Assert.That(
                directional[0],
                Is.SameAs(contract.DirectionalLight));
        }

        private static void AssertPhysicalCorridors(
            P6RoomGraphLabContract contract,
            Scene scene)
        {
            Assert.That(contract.PhysicalCorridorRoot, Is.Not.Null);
            Assert.That(
                contract.PhysicalCorridorRoot.gameObject.scene,
                Is.EqualTo(scene));
            Assert.That(
                contract.PhysicalCorridorModules,
                Is.Not.Empty);

            HashSet<Vector2Int> expectedCells =
                new HashSet<Vector2Int>(
                    contract.Corridors.SelectMany(
                        corridor => corridor.RoutingCells));
            P6PhysicalCorridorModule2D[] routeModules =
                contract.PhysicalCorridorModules
                    .Where(module => !module.IsAccessBridge)
                    .ToArray();
            P6PhysicalCorridorModule2D[] accessBridges =
                contract.PhysicalCorridorModules
                    .Where(module => module.IsAccessBridge)
                    .ToArray();

            Assert.That(
                routeModules.Select(module => module.RoutingCell),
                Is.EquivalentTo(expectedCells));
            Assert.That(
                accessBridges.Length,
                Is.EqualTo(contract.Accesses.Count));
            Assert.That(
                accessBridges.Select(module => module.AccessId),
                Is.EquivalentTo(
                    contract.Accesses.Select(access => access.AccessId)));

            foreach (P6PhysicalCorridorModule2D module
                     in contract.PhysicalCorridorModules)
            {
                Assert.That(module, Is.Not.Null);
                Assert.That(module.gameObject.scene, Is.EqualTo(scene));
                Assert.That(module.HasPhysicalCollision, Is.True);
                Assert.That(module.Surfaces, Is.Not.Empty);
                Assert.That(
                    module.Surfaces.All(surface =>
                        surface != null
                        && surface.enabled
                        && !surface.isTrigger),
                    Is.True);
            }

            Assert.That(
                routeModules.Any(module =>
                    module.ModuleKind
                        != P6CorridorModuleKind.End),
                Is.True,
                "The physical corridor network must contain connected "
                + "straight, stair, corner, or junction modules.");
        }

        private static bool IsGraphConnected(
            IReadOnlyCollection<int> nodes,
            IReadOnlyList<P6RoomGraphLabEdge> edges,
            int start)
        {
            Dictionary<int, List<int>> adjacency =
                nodes.ToDictionary(
                    node => node,
                    _ => new List<int>());
            for (int index = 0; index < edges.Count; index++)
            {
                P6RoomGraphLabEdge edge = edges[index];
                adjacency[edge.FirstNodeId].Add(edge.SecondNodeId);
                adjacency[edge.SecondNodeId].Add(edge.FirstNodeId);
            }

            HashSet<int> visited = new HashSet<int>();
            Queue<int> frontier = new Queue<int>();
            visited.Add(start);
            frontier.Enqueue(start);
            while (frontier.Count > 0)
            {
                int current = frontier.Dequeue();
                foreach (int next in adjacency[current])
                {
                    if (visited.Add(next))
                    {
                        frontier.Enqueue(next);
                    }
                }
            }

            return visited.Count == nodes.Count;
        }

        private static bool Contains(RectInt outer, RectInt inner)
        {
            return inner.xMin >= outer.xMin
                && inner.yMin >= outer.yMin
                && inner.xMax <= outer.xMax
                && inner.yMax <= outer.yMax;
        }

        private static string EdgeKey(int first, int second)
        {
            int low = Mathf.Min(first, second);
            int high = Mathf.Max(first, second);
            return $"{low}:{high}";
        }

        private static void AssertNoVisibleText(Scene scene)
        {
            MonoBehaviour[] behaviours =
                FindSceneComponents<MonoBehaviour>(scene);
            string[] forbiddenTypeNames =
            {
                "UnityEngine.UI.Text",
                "TMPro.TextMeshPro",
                "TMPro.TextMeshProUGUI"
            };
            Assert.That(
                behaviours.Any(behaviour =>
                    behaviour != null
                    && forbiddenTypeNames.Contains(
                        behaviour.GetType().FullName)),
                Is.False,
                "P6 Lab overlays must remain sprite-only.");
            Assert.That(
                FindSceneComponents<TextMesh>(scene),
                Is.Empty);
        }

        private static T[] FindSceneComponents<T>(Scene scene)
            where T : Component
        {
            List<T> components = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                components.AddRange(
                    roots[index].GetComponentsInChildren<T>(true));
            }

            return components.ToArray();
        }

        private static int CountMissingMonoBehaviours(Scene scene)
        {
            int missingCount = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0;
                 rootIndex < roots.Length;
                 rootIndex++)
            {
                Transform[] transforms =
                    roots[rootIndex]
                        .GetComponentsInChildren<Transform>(true);
                for (int index = 0;
                     index < transforms.Length;
                     index++)
                {
#if UNITY_EDITOR
                    missingCount +=
                        GameObjectUtility
                            .GetMonoBehavioursWithMissingScriptCount(
                                transforms[index].gameObject);
#else
                    MonoBehaviour[] behaviours =
                        transforms[index]
                            .GetComponents<MonoBehaviour>();
                    for (int behaviourIndex = 0;
                         behaviourIndex < behaviours.Length;
                         behaviourIndex++)
                    {
                        if (behaviours[behaviourIndex] == null)
                        {
                            missingCount++;
                        }
                    }
#endif
                }
            }

            return missingCount;
        }

        private static IEnumerator LoadLabSceneAsync()
        {
            AsyncOperation operation;
#if UNITY_EDITOR
            Assert.That(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    ScenePath),
                Is.Not.Null,
                $"Missing P6 Lab scene: {ScenePath}");
            operation =
                EditorSceneManager.LoadSceneAsyncInPlayMode(
                    ScenePath,
                    new LoadSceneParameters(LoadSceneMode.Single));
#else
            operation =
                SceneManager.LoadSceneAsync(
                    ScenePath,
                    LoadSceneMode.Single);
#endif
            Assert.That(
                operation,
                Is.Not.Null,
                $"Unable to load P6 Lab scene: {ScenePath}");
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
        }
    }
}

#endif
