using System.Collections.Generic;
using StarNight.Character.Integration;
using StarNight.Character.MapIntegration;
using StarNight.Map.WorldGeneration.Domain;
using UnityEngine;

namespace StarNight.Character.Live.Rooms
{
    /// <summary>
    /// L02_01 한정 임시 수동 방/루트 소스. 인접한 수동 방 2개를 준비 상태로
    /// 등록하고 그 사이 선언 루트 엣지(양방향)를 제공한다 — 방/마이크로청크/
    /// 아이템/Tilemap을 생성하지 않으며, L02_02 MAP 어댑터가 순수 캐릭터
    /// 런타임 무변경으로 교체하는 동일 계약 표면이다.
    /// </summary>
    public sealed class CharacterLiveManualRouteSource : MonoBehaviour
    {
        [Tooltip("방 A 내부 셀(방 식별용 앵커)")]
        [SerializeField] private int roomACellX = 5;
        [SerializeField] private int roomACellY = 0;

        [Tooltip("방 B 내부 셀(방 식별용 앵커)")]
        [SerializeField] private int roomBCellX = 17;
        [SerializeField] private int roomBCellY = 0;

        [Tooltip("A→B 이탈/진입 셀(경계 양쪽)")]
        [SerializeField] private int exitCellX = 11;
        [SerializeField] private int entryCellX = 12;
        [SerializeField] private int boundaryCellY = 0;

        [Tooltip("두 번째 방 준비 여부(미준비 차단 스모크용 토글)")]
        [SerializeField] private bool roomBReady = true;

        private readonly CharacterLiveRoomReadinessSource readinessSource =
            new CharacterLiveRoomReadinessSource();

        private readonly List<CharacterGeneratedRouteEdgeSnapshot> declaredEdges =
            new List<CharacterGeneratedRouteEdgeSnapshot>();

        private bool built;

        public ICharacterRoomReadinessSource ReadinessSource
        {
            get
            {
                EnsureBuilt();
                return readinessSource;
            }
        }

        public IReadOnlyList<CharacterGeneratedRouteEdgeSnapshot> DeclaredEdges
        {
            get
            {
                EnsureBuilt();
                return declaredEdges;
            }
        }

        public CharacterRoomId RoomA
        {
            get
            {
                EnsureBuilt();
                return CharacterRoomId.FromWorldTile(
                    new WorldTileCoord(roomACellX, roomACellY));
            }
        }

        public CharacterRoomId RoomB
        {
            get
            {
                EnsureBuilt();
                return CharacterRoomId.FromWorldTile(
                    new WorldTileCoord(roomBCellX, roomBCellY));
            }
        }

        private void EnsureBuilt()
        {
            if (built)
            {
                return;
            }

            built = true;

            var roomA = CharacterRoomId.FromWorldTile(
                new WorldTileCoord(roomACellX, roomACellY));
            var roomB = CharacterRoomId.FromWorldTile(
                new WorldTileCoord(roomBCellX, roomBCellY));

            readinessSource.RegisterRoom(roomA, true);
            readinessSource.RegisterRoom(roomB, roomBReady);

            var exitCell = new WorldTileCoord(exitCellX, boundaryCellY);
            var entryCell = new WorldTileCoord(entryCellX, boundaryCellY);

            // 선언 루트: A→B(오른쪽), B→A(왼쪽) — 역방향 통과도 선언 루트만 허용.
            declaredEdges.Add(new CharacterGeneratedRouteEdgeSnapshot(
                1, roomA, roomB, CharacterRouteBoundarySide.Right,
                exitCell, entryCell, CharacterRouteRequirement.BasicMovement));
            declaredEdges.Add(new CharacterGeneratedRouteEdgeSnapshot(
                2, roomB, roomA, CharacterRouteBoundarySide.Left,
                entryCell, exitCell, CharacterRouteRequirement.BasicMovement));
        }
    }
}
