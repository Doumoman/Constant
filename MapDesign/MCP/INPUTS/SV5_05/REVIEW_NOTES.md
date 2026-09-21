# SV5_04 실제 ZIP 검토와 SV5_05 인계

검토 대상: 사용자가 제공한 SV5_04_REVIEW.zip. source/test/CSV/XML의 SHA를 Result 및 SV5_03 보고와 대조했다.
검토 ZIP SHA: d57b7dfc6725a0c3fc732b7893f2b820b2e7a99b872df131dd84eadc397fc6d6
보고된 주요 산출물·코드·XML 12개와 SV5_03 CSV 4개의 SHA가 일치한다. Result 사본도 이전 업로드와 동일하다.
동봉 XML은 8/8 Passed, failed/skipped 0/0이다. 이 검토 환경에서 Unity를 다시 실행한 것은 아니다.
대표 셀 2,432개(S483/A1919/O30), 경로 11개, Passage3019/Clearance3019/Support1037행과 셀 연속성을 재구성했다.
같은 예약 좌표에서 required_base가 모순되는 경우는 0개다.

## 확인한 기존 API

- namespace: StarNight.Map.WorldGeneration.SpecialRegions
- source: Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/Sv5CoreReservationPlan.cs
- Sv5CoreReservationPlanner.Plan(Rmap16ClusterAssemblyPlan)
- Sv5CoreReservationPlanner.Plan(RmapSpecialReservationPlan, Rmap16ClusterAssemblyPlan)
- Sv5CoreReservationPlan.EvaluateTerrainCandidates(IEnumerable<Sv5CoreTerrainCandidate>)
- 계획에서 Source, RouteSource.Graph, Routes, RouteCells, AccessBindings, GraphBindings, StateGeometry, Digest를 읽을 수 있다.
- Routes의 RouteId/FromPortId/ToPortId/Condition/Cells는 기존 Rmap16Route를 전달한다.
- 기존 gate는 core/통행 AIR/지지/상태 셀의 일반 지형 변경을 거부하며 진행 상태를 전이하지 않는다.

## 진행 조건 검토 대상

Condition은 NORMAL_RESOURCE_RETURN, FORGE_GATED_SEAL_APPROACH 같은 서술 문자열이다.
문자열에 GATED가 있다는 이유로 상태 predicate가 평가되거나 실제 차단물이 생성되지 않는다.
SV5_05는 RouteId로 실제 RMAP13 edge와 연결하고 기존 상태 전이/조건을 읽어 명시적 대응표를 만들어야 한다.
core access의 condition과 route label을 같은 enum으로 간주하지 않는다. 의미가 불명확하면 NONE으로 처리하지 않는다.

| 좌표 | 겹친 route label |
|---|---|
| 523,134 | FORGE_GATED_SEAL_APPROACH / NORMAL_FORGE_RETURN |
| 415,301 | BOSS_GATED_EXIT_APPROACH / NORMAL_FORGE_RETURN / NORMAL_RESOURCE_RETURN |
| 491,301 | BOSS_GATED_EXIT_APPROACH / NORMAL_FORGE_APPROACH |

위 세 좌표는 label 비교로 찾은 검토 후보다. 겹침 자체가 모두 결함이라는 뜻은 아니며 실제 조건과 허용 전이를 평가해야 한다.
별도로 route의 required_base=A와 core base=A를 합치고 SEALED S 셀을 제외한 4-neighbour BFS에서
Start ENTRY 첫 셀에서 Exit ENTRY까지 109개 간선의 AIR 연결을 확인했다. 좌표열/입력 SHA/알고리즘은 REVIEW_FINDINGS.json에 있다.
이 연결에는 몸체·중력·점프·Grab·출구 상호작용·실제 진행 상태 검사가 없다. 실제 무아이템 완주나 확정 exploit 증거가 아니다.
따라서 현재 예약 AIR를 물리적인 진행 차단까지 검증된 자료로 승격할 수 없다.
SV5_05는 이 후보/좌표열을 실제 조건/접촉 검사에 투입하고 결과와 남은 물리적 차단 책임을 내보내야 한다.
논리 predicate를 덧붙였다는 이유만으로 원래 AIR가 닫혔다고 보고하지 않는다.

## 이후에 남길 책임

- Village ENTRY/EXIT는 실제 graph route가 없고 PRESERVED_NO_GRAPH_RESERVATION이다. SV5_06에서 접근과 복귀를 연결해야 한다.
- Start EXIT는 PRESERVED_NOT_SELECTED_BY_CURRENT_ROUTE_PLAN이다. SV5_06에서 사용/미사용 책임을 명시한다.
- 현재 adapter는 8site/2432cell 및 같은 RMAP15 객체/RMAP16 입력을 요구한다. 새로운 geometry를 지원할 때 SV5_06/41의 명시적 adapter 변경이 필요할 수 있다.
- 새 geometry에서 교차점/필수 차단을 해결하기 전에는 GEOMETRY_STATE_READY를 true로 표시하지 않는다.
- 과거 RMAP/SV5_04 CSV는 보존한다. 후속 계획은 같은 core 정본/조건을 유지하면서 자기 책임의 새 route geometry를 생산할 수 있다.
- SV5_04 Finalize/commit SHA는 ZIP에 없다. 다음 시작 시 현지에서 확인한다.
