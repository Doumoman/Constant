using System.Collections.Generic;
using StarNight.Character.Integration;
using StarNight.Character.Live.Run;
using StarNight.Character.MapIntegration;
using StarNight.Character.RoomTransition;

namespace StarNight.Character.Live.Rooms
{
    /// <summary>
    /// 방 전환 요청 소비자. 카메라 정책의 전환 요청을 받아 선언 루트 검증
    /// (CHAR06_01 정책 위임)을 통과한 경우에만 세션의 현재 방을 갱신한다.
    /// 미선언/미준비/미등록은 진단 기록 + 상태 무변경. 입력·속도·플레이어
    /// 위치는 일절 건드리지 않는다(KEEP 구조 유지).
    /// </summary>
    public sealed class CharacterLiveRouteTransitionConsumer
    {
        public int AcceptedCount { get; private set; }
        public int RejectedCount { get; private set; }
        public bool HasLastDiagnostic { get; private set; }
        public CharacterIntegrationDiagnostic LastDiagnostic { get; private set; }
        public CharacterGeneratedRouteTransitionRequest LastAcceptedRoute { get; private set; }

        /// <summary>
        /// 전환 요청 소비 시도. 수락 시 세션 현재 방을 갱신하고 true를
        /// 반환한다(안정화된 경계 통과 1건당 정확히 1회 — 정책이 보장).
        /// </summary>
        public bool TryConsume(
            in CharacterRoomTransitionRequest transitionRequest,
            IReadOnlyList<CharacterGeneratedRouteEdgeSnapshot> declaredEdges,
            ICharacterRoomReadinessSource readinessSource,
            CharacterLiveRunSession session)
        {
            CharacterGeneratedRouteTransitionRequest routeRequest;
            CharacterIntegrationDiagnostic diagnostic;

            if (!CharacterRouteIntegrationPolicy.TryCreateRouteTransitionRequestForRooms(
                declaredEdges,
                transitionRequest.SourceRoom,
                transitionRequest.TargetRoom,
                readinessSource,
                out routeRequest,
                out diagnostic))
            {
                RejectedCount++;
                HasLastDiagnostic = true;
                LastDiagnostic = diagnostic;
                return false;
            }

            session.UpdateCurrentRoom(routeRequest.TargetRoom);
            AcceptedCount++;
            LastAcceptedRoute = routeRequest;
            HasLastDiagnostic = false;
            return true;
        }
    }
}
