using System.Collections.Generic;
using StarNight.Character.MapIntegration;

namespace StarNight.Character.Integration
{
    /// <summary>
    /// 생성 루트 전환 판정(순수). 선언된 엣지만 전환 요청이 되며, 도착 방
    /// 준비 여부는 CHAR03 CharacterRoomBoundaryGate를 그대로 재사용해
    /// 판정한다. 미선언·미준비는 예외 없이 진단으로 보고한다.
    /// </summary>
    public static class CharacterRouteIntegrationPolicy
    {
        /// <summary>선언 엣지 목록에서 (출발→도착) 방 쌍이 선언되어 있는지 찾는다.</summary>
        public static bool TryFindDeclaredEdge(
            IReadOnlyList<CharacterGeneratedRouteEdgeSnapshot> declaredEdges,
            CharacterRoomId sourceRoom,
            CharacterRoomId targetRoom,
            out CharacterGeneratedRouteEdgeSnapshot edge)
        {
            edge = default;

            if (declaredEdges == null)
            {
                return false;
            }

            for (int index = 0; index < declaredEdges.Count; index++)
            {
                var candidate = declaredEdges[index];

                if (candidate.SourceRoom.Equals(sourceRoom)
                    && candidate.TargetRoom.Equals(targetRoom))
                {
                    edge = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// (출발→도착) 방 쌍 요청 → 선언 여부 + 게이트 판정. 미선언 엣지는
        /// UndeclaredRouteEdge 진단으로 보고된다.
        /// </summary>
        public static bool TryCreateRouteTransitionRequestForRooms(
            IReadOnlyList<CharacterGeneratedRouteEdgeSnapshot> declaredEdges,
            CharacterRoomId sourceRoom,
            CharacterRoomId targetRoom,
            ICharacterRoomReadinessSource readinessSource,
            out CharacterGeneratedRouteTransitionRequest request,
            out CharacterIntegrationDiagnostic diagnostic)
        {
            request = default;

            CharacterGeneratedRouteEdgeSnapshot edge;
            if (!TryFindDeclaredEdge(declaredEdges, sourceRoom, targetRoom, out edge))
            {
                diagnostic = new CharacterIntegrationDiagnostic(
                    CharacterIntegrationDiagnosticKind.UndeclaredRouteEdge,
                    "rooms:" + sourceRoom.Sector.X + "," + sourceRoom.Sector.Y
                        + "->" + targetRoom.Sector.X + "," + targetRoom.Sector.Y);
                return false;
            }

            return TryCreateRouteTransitionRequest(
                in edge, readinessSource, out request, out diagnostic);
        }

        /// <summary>
        /// 선언 엣지 → 전환 요청. CHAR03 게이트(준비 게이트)를 통과해야만
        /// 요청이 만들어진다.
        /// </summary>
        public static bool TryCreateRouteTransitionRequest(
            in CharacterGeneratedRouteEdgeSnapshot edge,
            ICharacterRoomReadinessSource readinessSource,
            out CharacterGeneratedRouteTransitionRequest request,
            out CharacterIntegrationDiagnostic diagnostic)
        {
            request = default;
            diagnostic = default;
            string subject = "route:" + edge.RouteId;

            if (readinessSource == null)
            {
                diagnostic = new CharacterIntegrationDiagnostic(
                    CharacterIntegrationDiagnosticKind.RouteBlockedMissingRoom,
                    subject);
                return false;
            }

            var gate = new CharacterRoomBoundaryGate(readinessSource);
            var decision = gate.Evaluate(edge.SourceExitCell, edge.TargetEntryCell);

            switch (decision)
            {
                case CharacterBoundaryCrossDecision.BlockedMissingRoom:
                    diagnostic = new CharacterIntegrationDiagnostic(
                        CharacterIntegrationDiagnosticKind.RouteBlockedMissingRoom,
                        subject);
                    return false;

                case CharacterBoundaryCrossDecision.BlockedUnpreparedRoom:
                    diagnostic = new CharacterIntegrationDiagnostic(
                        CharacterIntegrationDiagnosticKind.RouteBlockedUnpreparedRoom,
                        subject);
                    return false;

                default:
                    request = new CharacterGeneratedRouteTransitionRequest(
                        edge.RouteId,
                        edge.SourceRoom,
                        edge.TargetRoom,
                        edge.BoundarySide,
                        edge.TargetEntryCell);
                    return true;
            }
        }
    }
}
