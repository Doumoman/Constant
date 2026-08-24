using System.Collections.Generic;
using StarNight.Character.GeneratedRunValidation;
using StarNight.Character.Integration;
using StarNight.Character.MapIntegration;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.Tests.GeneratedRunValidation
{
    /// <summary>
    /// 테스트 전용 결정적 스냅샷 픽스처 빌더. 시드로 매개변수화된 "고정
    /// 데이터"를 조립할 뿐 난수·생성기를 쓰지 않는다(런타임은 검증만 소유).
    /// 방 A=[0..11]×[0..7], 방 B=[12..23]×[0..7].
    /// </summary>
    internal static class CharacterGeneratedRunFixtures
    {
        internal const int ActorId = 777;

        internal static readonly CharacterRoomId RoomA =
            CharacterRoomId.FromWorldTile(new WorldTileCoord(0, 0));

        internal static readonly CharacterRoomId RoomB =
            CharacterRoomId.FromWorldTile(new WorldTileCoord(12, 0));

        internal static readonly CharacterRoomId RoomMissing =
            CharacterRoomId.FromWorldTile(new WorldTileCoord(0, 8));

        internal sealed class FakeReadinessSource : ICharacterRoomReadinessSource
        {
            private readonly Dictionary<CharacterRoomId, bool> rooms =
                new Dictionary<CharacterRoomId, bool>();

            public int QueryCount { get; private set; }

            public void SetRoom(CharacterRoomId room, bool isReady)
            {
                rooms[room] = isReady;
            }

            public bool TryGetRoomReadiness(CharacterRoomId room, out bool isReady)
            {
                QueryCount++;
                return rooms.TryGetValue(room, out isReady);
            }
        }

        internal static FakeReadinessSource ReadyRooms()
        {
            var source = new FakeReadinessSource();
            source.SetRoom(RoomA, true);
            source.SetRoom(RoomB, true);
            return source;
        }

        internal static List<CharacterGeneratedRoomSnapshot> DefaultRooms()
        {
            return new List<CharacterGeneratedRoomSnapshot>
            {
                new CharacterGeneratedRoomSnapshot(
                    RoomA, new WorldTileCoord(0, 0), new WorldTileCoord(11, 7)),
                new CharacterGeneratedRoomSnapshot(
                    RoomB, new WorldTileCoord(12, 0), new WorldTileCoord(23, 7))
            };
        }

        internal static List<CharacterGeneratedMicrochunkSnapshot> DefaultMicrochunks()
        {
            return new List<CharacterGeneratedMicrochunkSnapshot>
            {
                new CharacterGeneratedMicrochunkSnapshot(
                    RoomA, new WorldTileCoord(0, 0), new WorldTileCoord(11, 7)),
                new CharacterGeneratedMicrochunkSnapshot(
                    RoomB, new WorldTileCoord(12, 0), new WorldTileCoord(23, 7))
            };
        }

        internal static CharacterGeneratedRouteEdgeSnapshot BasicRoute(
            int routeId = 3,
            CharacterRouteRequirement requirement =
                CharacterRouteRequirement.BasicMovement)
        {
            return new CharacterGeneratedRouteEdgeSnapshot(
                routeId, RoomA, RoomB, CharacterRouteBoundarySide.Right,
                new WorldTileCoord(11, 3), new WorldTileCoord(12, 3), requirement);
        }

        internal static CharacterGeneratedMapStartSnapshot Start(int seed)
        {
            // 시드별 결정적 변주: 시작 X = 2 + (seed % 3) ∈ [2..4].
            return new CharacterGeneratedMapStartSnapshot(
                seed, RoomA, true,
                new WorldTileCoord(2 + (seed % 3), 3),
                new WorldTileCoord(0, 0), new WorldTileCoord(11, 7));
        }

        /// <summary>시드로 매개변수화된 유효 런 스냅샷.</summary>
        internal static CharacterGeneratedRunSnapshot ValidRun(int seed)
        {
            return new CharacterGeneratedRunSnapshot(
                seed, seed, Start(seed),
                DefaultRooms(), DefaultMicrochunks(),
                new List<CharacterGeneratedRouteEdgeSnapshot> { BasicRoute() },
                new List<CharacterGeneratedItemPlacementSnapshot>
                {
                    new CharacterGeneratedItemPlacementSnapshot(
                        1, RoomA, new WorldTileCoord(8, 2 + (seed % 2)))
                },
                new List<WorldTileCoord> { new WorldTileCoord(23, 3) },
                new List<WorldTileCoord> { new WorldTileCoord(6, 6) });
        }

        /// <summary>부품 교체용 — 임의 구성 스냅샷.</summary>
        internal static CharacterGeneratedRunSnapshot Custom(
            int seed,
            CharacterGeneratedMapStartSnapshot start,
            List<CharacterGeneratedRoomSnapshot> rooms,
            List<CharacterGeneratedMicrochunkSnapshot> microchunks,
            List<CharacterGeneratedRouteEdgeSnapshot> routes,
            List<CharacterGeneratedItemPlacementSnapshot> items)
        {
            return new CharacterGeneratedRunSnapshot(
                seed, seed, start, rooms, microchunks, routes, items,
                new List<WorldTileCoord> { new WorldTileCoord(23, 3) },
                new List<WorldTileCoord> { new WorldTileCoord(6, 6) });
        }
    }
}
