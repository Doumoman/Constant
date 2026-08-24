using System.Collections.Generic;
using StarNight.Character.MapIntegration;
using StarNight.Character.RunState;

namespace StarNight.Character.Integration
{
    /// <summary>
    /// 통합 요청 배치(순수·결정적). 같은 입력이면 같은 출력이며, 순서는
    /// 스폰 → 선언 엣지 입력 순서이고, 동등 요청은 한 번만 방출된다.
    /// 복구 가능한 결함은 전부 진단으로 흘러간다.
    /// </summary>
    public static class CharacterIntegrationBatchPolicy
    {
        public static void BuildBatch(
            in CharacterGeneratedMapStartSnapshot startSnapshot,
            int actorId,
            IReadOnlyList<CharacterGeneratedRouteEdgeSnapshot> declaredEdges,
            in CharacterRunInventoryState inventory,
            ICharacterRoomReadinessSource readinessSource,
            List<CharacterPlayerSpawnRequest> spawnRequests,
            List<CharacterGeneratedRouteTransitionRequest> routeRequests,
            List<CharacterIntegrationDiagnostic> diagnostics)
        {
            spawnRequests.Clear();
            routeRequests.Clear();
            diagnostics.Clear();

            // (1) 스폰 — 항상 배치 선두.
            CharacterPlayerSpawnRequest spawnRequest;
            CharacterIntegrationDiagnostic spawnDiagnostic;
            if (CharacterSpawnIntegrationPolicy.TryCreateSpawnRequest(
                in startSnapshot, actorId, out spawnRequest, out spawnDiagnostic))
            {
                spawnRequests.Add(spawnRequest);
            }
            else
            {
                diagnostics.Add(spawnDiagnostic);
            }

            if (declaredEdges == null)
            {
                return;
            }

            // (2) 루트 — 선언 목록 입력 순서 그대로(결정적), 동등 요청 1회.
            for (int index = 0; index < declaredEdges.Count; index++)
            {
                var edge = declaredEdges[index];

                CharacterIntegrationDiagnostic capabilityDiagnostic;
                if (!CharacterRouteCapabilityPolicy.IsRouteSupported(
                    edge.Requirement, in inventory, edge.RouteId,
                    out capabilityDiagnostic))
                {
                    diagnostics.Add(capabilityDiagnostic);
                    continue;
                }

                CharacterGeneratedRouteTransitionRequest routeRequest;
                CharacterIntegrationDiagnostic routeDiagnostic;
                if (!CharacterRouteIntegrationPolicy.TryCreateRouteTransitionRequest(
                    in edge, readinessSource, out routeRequest, out routeDiagnostic))
                {
                    diagnostics.Add(routeDiagnostic);
                    continue;
                }

                if (!ContainsEquivalent(routeRequests, in routeRequest))
                {
                    routeRequests.Add(routeRequest);
                }
            }
        }

        private static bool ContainsEquivalent(
            List<CharacterGeneratedRouteTransitionRequest> requests,
            in CharacterGeneratedRouteTransitionRequest candidate)
        {
            for (int index = 0; index < requests.Count; index++)
            {
                var existing = requests[index];

                if (existing.RouteId == candidate.RouteId
                    && existing.SourceRoom.Equals(candidate.SourceRoom)
                    && existing.TargetRoom.Equals(candidate.TargetRoom)
                    && existing.BoundarySide == candidate.BoundarySide
                    && existing.TargetEntryCell.Equals(candidate.TargetEntryCell))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
