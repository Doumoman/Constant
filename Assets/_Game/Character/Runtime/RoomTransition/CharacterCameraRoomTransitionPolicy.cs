using System;
using StarNight.Character.MapIntegration;
using StarNight.Map.WorldGeneration.Domain;
using UnityEngine;

namespace StarNight.Character.RoomTransition
{
    /// <summary>
    /// 순수 카메라룸 전환 정책. 위치 샘플만 평가해 전환 판정/요청을 반환한다.
    ///
    /// - 준비 판정은 CHAR03_01의 <see cref="CharacterRoomBoundaryGate"/>에 위임한다(중복 없음).
    /// - 입력·속도를 받지도 반환하지도 않으므로 KEEP이 API 형태로 보장된다.
    ///   전환을 이유로 입력 잠금 사유를 추가하지 않는다.
    /// - 카메라 이동, 플레이어 위치 변조, 텔레포트, 스냅을 수행하지 않는다.
    /// - grounded 여부를 받지 않으므로 지상/공중 경계 진입이 동일 정책이다.
    /// - hysteresis: 목표 방으로의 경계 침투가 margin 이상인 연속 샘플이
    ///   기준 횟수를 채울 때만 전환을 요청한다(경계 핑퐁 방지).
    /// - 스윕 충돌은 지원하지 않는다 — 이전/현재 위치 샘플 평가 방식이며, 한
    ///   평가 스텝에 여러 방을 건너뛰면 최종 위치의 방 하나로만 전환을 요청한다.
    /// </summary>
    public sealed class CharacterCameraRoomTransitionPolicy
    {
        private const float RoomWidthWorld =
            WorldGenConstants.MicroChunkWidthTiles * CharacterMapCoordinateBridge.WorldUnitsPerCell;

        private const float RoomHeightWorld =
            WorldGenConstants.MicroChunkHeightTiles * CharacterMapCoordinateBridge.WorldUnitsPerCell;

        private readonly CharacterRoomBoundaryGate boundaryGate;
        private readonly CharacterRoomTransitionSettings settings;

        private bool hasActiveRoom;
        private CharacterRoomId activeRoom;
        private WorldTileCoord activeAnchorTile;

        private bool hasCandidate;
        private CharacterRoomId candidateRoom;
        private int candidateStableSamples;

        public CharacterCameraRoomTransitionPolicy(
            CharacterRoomBoundaryGate boundaryGate,
            CharacterRoomTransitionSettings settings)
        {
            if (boundaryGate == null)
            {
                throw new ArgumentNullException(nameof(boundaryGate));
            }

            this.boundaryGate = boundaryGate;
            this.settings = settings;
        }

        public CharacterRoomTransitionSettings Settings
        {
            get { return settings; }
        }

        public bool HasActiveRoom
        {
            get { return hasActiveRoom; }
        }

        public CharacterRoomId ActiveRoom
        {
            get { return activeRoom; }
        }

        /// <summary>초기 배치. anchor 타일의 방을 활성 방으로 설정한다.</summary>
        public void SetActiveRoom(WorldTileCoord anchorTile)
        {
            activeRoom = CharacterRoomId.FromWorldTile(anchorTile);
            activeAnchorTile = anchorTile;
            hasActiveRoom = true;
            ResetCandidate();
        }

        /// <summary>
        /// 위치 샘플 평가. 판정만 반환하며 입력·속도·카메라·플레이어 위치를
        /// 일절 변조하지 않는다.
        /// </summary>
        public CharacterRoomTransitionResult Evaluate(Vector2 position)
        {
            WorldTileCoord tile;
            if (!CharacterMapCoordinateBridge.TryGetTileCoordinate(position, out tile))
            {
                // 월드 범위 밖 위치는 평가 불가 — 전환 없음, 안정 추적 초기화.
                ResetCandidate();
                return CharacterRoomTransitionResult.Of(
                    CharacterRoomTransitionDecision.NoTransition);
            }

            CharacterRoomId sampleRoom = CharacterRoomId.FromWorldTile(tile);

            if (!hasActiveRoom)
            {
                // 최초 평가는 현재 방을 활성 방으로 채택한다(전환 아님).
                SetActiveRoom(tile);
                return CharacterRoomTransitionResult.Of(
                    CharacterRoomTransitionDecision.NoTransition);
            }

            if (sampleRoom.Equals(activeRoom))
            {
                activeAnchorTile = tile;
                ResetCandidate();
                return CharacterRoomTransitionResult.Of(
                    CharacterRoomTransitionDecision.NoTransition);
            }

            // 준비 판정은 기존 게이트에 위임한다(중복 구현 금지).
            CharacterBoundaryCrossDecision gateDecision =
                boundaryGate.Evaluate(activeAnchorTile, tile);

            if (gateDecision == CharacterBoundaryCrossDecision.BlockedMissingRoom)
            {
                ResetCandidate();
                return CharacterRoomTransitionResult.Of(
                    CharacterRoomTransitionDecision.BlockedMissingRoom);
            }

            if (gateDecision == CharacterBoundaryCrossDecision.BlockedUnpreparedRoom)
            {
                ResetCandidate();
                return CharacterRoomTransitionResult.Of(
                    CharacterRoomTransitionDecision.BlockedUnpreparedRoom);
            }

            // hysteresis: margin 미만 침투는 안정 샘플로 세지 않는다.
            float penetration = ComputeBoundaryPenetration(position, activeRoom, sampleRoom);

            if (penetration < settings.HysteresisMargin)
            {
                ResetCandidate();
                return CharacterRoomTransitionResult.Of(
                    CharacterRoomTransitionDecision.PendingStabilization);
            }

            if (!hasCandidate || !candidateRoom.Equals(sampleRoom))
            {
                hasCandidate = true;
                candidateRoom = sampleRoom;
                candidateStableSamples = 0;
            }

            candidateStableSamples++;

            if (candidateStableSamples < settings.StableTargetSamples)
            {
                return CharacterRoomTransitionResult.Of(
                    CharacterRoomTransitionDecision.PendingStabilization);
            }

            // 안정 조건 충족 — 전환 요청 발행 후 활성 방 갱신(경계당 요청 1회).
            var request = new CharacterRoomTransitionRequest(activeRoom, sampleRoom);
            activeRoom = sampleRoom;
            activeAnchorTile = tile;
            ResetCandidate();
            return CharacterRoomTransitionResult.Requested(request);
        }

        private void ResetCandidate()
        {
            hasCandidate = false;
            candidateRoom = default(CharacterRoomId);
            candidateStableSamples = 0;
        }

        /// <summary>
        /// 활성 방 → 후보 방으로 넘어간 공유 경계 기준의 침투 깊이.
        /// 축별로 방 원점(MAP ToWorld 위임)을 비교해 통과한 경계 쪽 깊이만 잰다.
        /// </summary>
        private static float ComputeBoundaryPenetration(
            Vector2 position,
            CharacterRoomId fromRoom,
            CharacterRoomId toRoom)
        {
            Vector2 fromMin = GetRoomMinWorld(fromRoom);
            Vector2 toMin = GetRoomMinWorld(toRoom);
            float penetration = float.MaxValue;

            if (toMin.x > fromMin.x)
            {
                penetration = Mathf.Min(penetration, position.x - toMin.x);
            }
            else if (toMin.x < fromMin.x)
            {
                penetration = Mathf.Min(penetration, toMin.x + RoomWidthWorld - position.x);
            }

            if (toMin.y > fromMin.y)
            {
                penetration = Mathf.Min(penetration, position.y - toMin.y);
            }
            else if (toMin.y < fromMin.y)
            {
                penetration = Mathf.Min(penetration, toMin.y + RoomHeightWorld - position.y);
            }

            return penetration == float.MaxValue ? 0f : penetration;
        }

        private static Vector2 GetRoomMinWorld(CharacterRoomId room)
        {
            WorldTileCoord originTile = WorldCoordinateUtility.ToWorld(
                room.Sector, room.MicroChunk, new LocalTileCoord(0, 0));
            return CharacterMapCoordinateBridge.GetCellOrigin(originTile);
        }
    }
}
