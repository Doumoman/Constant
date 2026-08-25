using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Character.Integration;
using StarNight.Character.Live.Adapters;
using StarNight.Character.Live.Cameras;
using StarNight.Character.Live.Rooms;
using StarNight.Character.Live.Run;
using StarNight.Character.MapIntegration;
using StarNight.Character.RoomTransition;
using StarNight.Character.RunState;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Microchunks;
using UnityEngine;
using UnityEngine.TestTools;

namespace StarNight.Character.Tests.PlayMode
{
    /// <summary>
    /// 생성 런 스모크: 공용 MAP 계약으로 조립한 샘플을 L02_02 어댑터로
    /// 투영하고, 시작 스냅샷으로 런을 시작해 투영 루트/준비 소스로 A→B
    /// 전환한다. 플레이어 텔레포트 없음·미생성 셀 차단을 함께 검증한다.
    /// </summary>
    public sealed class CharacterLiveGeneratedRunPlayModeTests
    {
        private static MicrochunkTileCell BuildCell(int x, int y)
        {
            string ground = y == 0 ? "G1" : "NONE";
            return new MicrochunkTileCell(
                new MicrochunkLocalCoord(x, y),
                ground, "NONE", "NONE", "NONE", "NONE", "NONE", "NONE", "NONE");
        }

        private static MicrochunkDefinition BuildFloorDefinition(string id)
        {
            var cells = new List<MicrochunkTileCell>();
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 12; x++)
                {
                    cells.Add(BuildCell(x, y));
                }
            }

            return new MicrochunkDefinition(
                new MicrochunkId(id), id, 12, 8,
                MicrochunkUsageClass.Traversal,
                new[] { "test" }, new[] { "any" },
                new[] { MicrochunkTransform.R0 },
                1, 0, 0, 0, true, "test-prefab", true, string.Empty,
                cells,
                new MicrochunkSocketDefinition[0],
                new MicrochunkObjectSlotDefinition[0]);
        }

        private static CharacterLiveGeneratedMapProjection ProjectSample()
        {
            var definition = BuildFloorDefinition("test.floor");
            var sector = new SectorCoord(0, 0);
            var placed = new List<CharacterLivePlacedMicrochunk>
            {
                new CharacterLivePlacedMicrochunk(
                    sector, new MicroChunkCoord(0, 0),
                    definition, MicrochunkTransform.R0),
                new CharacterLivePlacedMicrochunk(
                    sector, new MicroChunkCoord(1, 0),
                    definition, MicrochunkTransform.R0)
            };

            WorldTileCoord startCell, roomATile, roomBTile, exitCell, entryCell;
            WorldCoordinateUtility.TryCreateWorldTile(5, 1, out startCell);
            WorldCoordinateUtility.TryCreateWorldTile(0, 0, out roomATile);
            WorldCoordinateUtility.TryCreateWorldTile(12, 0, out roomBTile);
            WorldCoordinateUtility.TryCreateWorldTile(11, 1, out exitCell);
            WorldCoordinateUtility.TryCreateWorldTile(12, 1, out entryCell);
            CharacterRoomId roomA = CharacterRoomId.FromWorldTile(roomATile);
            CharacterRoomId roomB = CharacterRoomId.FromWorldTile(roomBTile);

            var routes = new List<CharacterGeneratedRouteEdgeSnapshot>
            {
                new CharacterGeneratedRouteEdgeSnapshot(
                    1, roomA, roomB, CharacterRouteBoundarySide.Right,
                    exitCell, entryCell, CharacterRouteRequirement.BasicMovement)
            };

            var input = new CharacterLiveGeneratedMapAdapterInput(
                1, 12345, true, startCell, placed, routes, null, null, null);
            var inventory = new CharacterRunInventoryState(1, 4, 4);
            return CharacterLiveGeneratedMapAdapter.Project(input, 1, in inventory);
        }

        [Test]
        public void Projection_IsUsable_AndUngeneratedCellsBlocked()
        {
            CharacterLiveGeneratedMapProjection projection = ProjectSample();

            Assert.AreEqual(0, projection.AdapterDiagnostics.Count);
            Assert.IsTrue(projection.ValidationResult.Passed);
            Assert.IsTrue(projection.IsUsable);
            Assert.AreEqual(2, projection.Snapshot.Rooms.Count);
            Assert.IsTrue(projection.Snapshot.Start.HasStartCell);

            // 생성 셀: 바닥 고체 / 생성-빈 구분.
            WorldTileCoord floorCell, emptyCell, ungeneratedCell;
            WorldCoordinateUtility.TryCreateWorldTile(5, 0, out floorCell);
            WorldCoordinateUtility.TryCreateWorldTile(5, 3, out emptyCell);
            WorldCoordinateUtility.TryCreateWorldTile(40, 3, out ungeneratedCell);
            CharacterMapCellState state;
            Assert.IsTrue(projection.WorldQuery.TryGetCellState(floorCell, out state));
            Assert.IsTrue(state.IsSolid);
            Assert.IsTrue(projection.WorldQuery.TryGetCellState(emptyCell, out state));
            Assert.IsTrue(state.IsEmpty);

            // 미생성 셀은 false — 통과 가능 빈 공간으로 취급하지 않는다.
            Assert.IsFalse(
                projection.WorldQuery.TryGetCellState(ungeneratedCell, out state));

            // 미배치 방 준비 없음(게이트 차단 경로).
            WorldTileCoord farTile;
            WorldCoordinateUtility.TryCreateWorldTile(40, 0, out farTile);
            bool isReady;
            bool found = projection.ReadinessSource.TryGetRoomReadiness(
                CharacterRoomId.FromWorldTile(farTile), out isReady);
            Assert.IsFalse(found && isReady);
        }

        [UnityTest]
        public IEnumerator GeneratedRun_RoutesAToB_CameraMoves_PlayerNotTeleported()
        {
            CharacterLiveGeneratedMapProjection projection = ProjectSample();
            Assert.IsTrue(projection.IsUsable);

            // 시작 스냅샷 → 스폰 요청 → 세션 시작.
            CharacterPlayerSpawnRequest spawnRequest;
            CharacterIntegrationDiagnostic diagnostic;
            Assert.IsTrue(CharacterSpawnIntegrationPolicy.TryCreateSpawnRequest(
                projection.Snapshot.Start, 1, out spawnRequest, out diagnostic));
            var session = new CharacterLiveRunSession();
            Assert.IsTrue(session.TryStartRun(in spawnRequest));
            CharacterRoomId roomA = spawnRequest.StartRoomId;

            // 플레이어 대역 + 카메라 드라이버(투영 방 중심 스냅).
            var playerGo = new GameObject("GeneratedRunPlayerProbe");
            playerGo.transform.position = new Vector3(
                spawnRequest.WorldCenter.x, spawnRequest.WorldCenter.y, 0f);
            var cameraGo = new GameObject("GeneratedRunCamera");
            var camera = cameraGo.AddComponent<Camera>();
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);
            var cameraDriver = cameraGo.AddComponent<CharacterLiveCameraRoomDriver>();
#if UNITY_EDITOR
            var so = new UnityEditor.SerializedObject(cameraDriver);
            so.FindProperty("targetCamera").objectReferenceValue = camera;
            so.ApplyModifiedPropertiesWithoutUndo();
#endif
            cameraDriver.MoveToRoom(roomA);
            Assert.AreEqual(new Vector3(6f, 4f, -10f), cameraGo.transform.position);

            // 정책(투영 준비 소스 게이트) + 소비자(투영 루트 소스).
            var gate = new CharacterRoomBoundaryGate(projection.ReadinessSource);
            var policy = new CharacterCameraRoomTransitionPolicy(
                gate, CharacterRoomTransitionSettings.Default);
            policy.SetActiveRoom(spawnRequest.StartCell);
            var consumer = new CharacterLiveRouteTransitionConsumer();

            // 경계(x=12)+margin 통과 위치에서 hysteresis 안정화까지 평가.
            playerGo.transform.position = new Vector3(12.4f, 1.5f, 0f);
            Vector3 positionBeforeTransition = playerGo.transform.position;
            int requests = 0;
            for (int sample = 0; sample < 4; sample++)
            {
                CharacterRoomTransitionResult result =
                    policy.Evaluate(playerGo.transform.position);
                if (!result.HasRequest)
                {
                    continue;
                }

                requests++;
                CharacterRoomTransitionRequest request = result.Request;
                Assert.IsTrue(consumer.TryConsume(
                    in request,
                    projection.RouteSource.DeclaredEdges,
                    projection.RouteSource.Readiness,
                    session));
                cameraDriver.MoveToRoom(session.CurrentRoomId);
                yield return null;
            }

            // 안정화된 경계 통과 1건당 정확히 1회 수락.
            Assert.AreEqual(1, requests);
            Assert.AreEqual(1, consumer.AcceptedCount);
            Assert.AreEqual(1, consumer.LastAcceptedRoute.RouteId);
            Assert.AreNotEqual(roomA, session.CurrentRoomId);

            // 카메라는 방 B 중심(18,4)으로, 플레이어는 무변조.
            Assert.AreEqual(new Vector3(18f, 4f, -10f), cameraGo.transform.position);
            Assert.AreEqual(positionBeforeTransition, playerGo.transform.position);

            Object.Destroy(playerGo);
            Object.Destroy(cameraGo);
            yield return null;
        }
    }
}
