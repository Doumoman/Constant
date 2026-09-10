# SV5_06 공간 그래프 계약

## 적용 범위

`SV5_06_SPACE_GRAPH`는 624×416 정수 타일 공간을 장소, 포트, 실제 통로, 접촉 split, 조건 gate, 예약 소유로 계획한다. 좌표는 왼쪽 아래 원점이고 모든 footprint는 half-open `[x,x+w) × [y,y+h)`이다. MicroChunk는 12×8, pattern index는 4×4 단위다.

이 산출물은 `PLANNED_LAYOUT`, `LOGICAL_STATE`, `CONTACT_STATE`까지만 승인한다. `COMPOSED_GEOMETRY`와 `PLAYER`는 거짓이며 Scene, bake, 실제 이동 가능성, 최종 AIR/SOLID를 주장하지 않는다. 미배정 타일은 `INFILL_PENDING`이고 소유자는 `SV5_08_INFILL`이다.

## 입력과 동일성

- `Sv5SpaceGraphPlanner.Plan(Sv5CoreReservationPlan core, ulong seed, Sv5SpaceGraphAuthoringProfile profile)`이 유일한 생성 진입점이다.
- 명시 seed는 `core.RouteSource.Definition.Request.Seed`와 같아야 한다.
- `core.Source`, `core.RouteSource.SpecialPlan`, `core.RouteSource.Definition`, `core.Source.BiomePlan.Definition`은 같은 실제 객체 계보를 유지한다.
- 8개 RMAP15 physical site, 2,432 core cell, slot/access/state geometry와 11개 RMAP16 route는 재생성하거나 옮기지 않는다.
- 승인 예시의 101 places, 166 connections, seed 40921은 정답 상수가 아니다. 대표 profile은 core 밖에 8 large, 6 ordinary places를 분산 배치한다.

## API와 상태 층

| API/형식 | 책임 |
|---|---|
| `Sv5SpaceGraphPlan` | source identity, profile, place/port/connection/gate/reservation/contact/proof와 전체 digest 소유 |
| `Sv5SpaceGraphAuthoringProfile` | versioned family, footprint, 후속 owner를 결정; 입력 순서와 무관하게 canonical 정렬 |
| `Sv5RouteStatePolicy.EnumerateContactPairs` | Passage·Clearance shared-cell 조합과 cardinal-face Cartesian product의 모든 서로 다른 route pair 열거 |
| `Sv5SpaceGraphStateProjection` | 실제 06 connector를 RMAP13 FSM에 투영하고 접촉마다 action-less split node 생성 |
| `Sv5SpaceGraphExport.WriteAll` | 동일 plan에서 JSON, CSV, overview, 16 zoom, HTML 생성 |

기존 `Sv5RouteStateAnalysis.LogicalStateVerified`는 baseline 6순서와 후보집합 논리만 뜻한다. `ContactStateVerified`는 별도다. route/predicate 이름이 존재한다는 이유만으로 contact row를 checked로 표시하지 않는다. 과거 05/FIX01 층의 접촉은 실제 FSM 투영 전까지 `logical_state_transition_checked=false`다.

06 투영에서는 접촉의 두 route가 각자 점유한 위치를 ordinal로 찾고 route 중간에 split node를 삽입한다. 원래 RMAP13 edge의 resource mask, Forge, Seal, Boss 조건을 split 전후 모든 segment에 반복한다. 따라서 끝점 guard를 우회해 중간 접촉으로 들어갈 수 없다. 모든 실제 connector를 결합한 뒤 각 자원 순서에서 reachable state 전체를 열거하고 goal state에서 한 번 만든 reverse adjacency로 역도달 집합을 계산한다.

## 장소와 포트

- Core place는 RMAP15 site ID, origin, width, height, template을 그대로 사용한다.
- Large/ordinary place는 stable ID, family, footprint, distribution sector, future owner, `PLANNED_SHELL` 상태를 갖는다.
- 일반 footprint는 최종 지형이 아니다. 7×5보다 작지 않으며 60-tile long cave band를 포함한다.
- Core port는 실제 RMAP15 access ID, 전체 boundary cell set, side, flow, condition, source node를 그대로 기록한다.
- `RMAP15_SITE_START_PORT_EXIT`는 보호된 기존 접근면과 겹치지 않는 별도 안전 분기를 만들 수 없어 `UNUSED_WITH_REASON:RMAP16_PROTECTED_APPROACH_HAS_NO_DISTINCT_SAFE_BRANCH`로 유지한다.
- Start ENTRY의 `Both` 흐름에서 선택 회로가 시작하고 같은 ENTRY로 복귀한다.
- Village ENTRY/EXIT와 village 내부 protected-AIR 경로를 실제로 연결한다. 이 회로는 resource/Forge/Seal/Boss action을 만들지 않는다.

## 통로, 접촉, gate

Core progression connection은 `SourceGraphEdgeId`가 있는 11개 실제 RMAP16 route다. 포트 boundary cell을 route의 외부 첫/끝 셀에 cardinally 붙여 centerline을 완성한다. 선택 회로는 명시적인 from/to port와 ordered cardinal centerline, 한 타일 clearance envelope를 가진다.

모든 connection centerline 및 기존 Passage·Clearance를 같은 pair 열거기에 입력한다. 공통 route가 있는 face도 나머지 `(A,B)`, `(A,C)`, `(B,C)`를 버리지 않는다. 같은 물리 접촉의 중복은 stable contact key로 제거하고 face 방향은 보존한다.

- 조건 없는 안전 접촉: `JOIN`, 공유 split node.
- 조건이 있는 접촉: `CONDITIONAL_GATE`, 공유 split node와 조건을 반복한 segment, 계획 SEALED/OPEN 상태.
- unknown route, missing ordinal, 전체 연결 실패: `PENDING`으로 거부하며 plan은 FAIL이다.

계획 gate는 runtime gate 증거가 아니다. `RuntimeVerified=false`, `GeometryStateReady=false`, `PlayerVerified=false`를 유지한다.

## 산출물 스키마

| 파일 | 기준 |
|---|---|
| `space_graph.json` | source/profile/digest, place/port/connection/gate, readiness, `INFILL_PENDING` |
| `places.csv` | stable place ID, family/kind, half-open bounds, core binding, future owner |
| `ports.csv` | boundary set, anchor, direction/flow/condition, actual access/node binding |
| `connections.csv` | endpoint IDs, ordered centerline/envelope, source graph edge, selection state |
| `reservation_cells.csv` | world tile, reservation semantics/owner, 12×8 chunk 및 4×4 pattern index, `is_final_tile=false` |
| `state_proofs.json` | baseline, candidate-set 분리, actual projection 6순서, state/transition/reverse counts, traces |
| `contact_checks.csv` | complete pair, 두 좌표/방향/route, split node, predicate, crossing, checked/readiness |
| `obligations.csv` | 후속 family, 07~10, 41/42/44 owner와 검증 층 |
| `preview/*` | 624×416 overview, A1~D4 156×104 zoom, HTML 진입점 |

모든 JSON/CSV/SVG는 같은 plan digest에서 생성한다. reservation 중복 행은 서로 다른 소유 의미를 보존할 수 있지만, 좌표 범위 초과나 place footprint 중첩은 진단 실패다. digest는 place/port/좌표/방향/guard/profile/선택 집합/소유/proof를 포함하고 단순 입력 열거 순서에는 불변이다.

## 이번 대표 결과와 후속 책임

대표 seed 1304 결과는 22 places(8 core, 8 large, 6 ordinary), 43 ports, 28 connections다. 5,347개 complete contact pair를 투영했고 이 중 103개가 planned conditional gate다. 여섯 자원 순서 모두 goal proof와 전체 reachable-state 역도달 검사를 통과했다.

`SV5_07`은 분포 다양화, `SV5_08`은 infill, `SV5_09`는 loop 후 contact 재검사, `SV5_10`은 side-path, `SV5_41`은 합성 geometry, `SV5_42`는 최종 간격/solid scan, `SV5_44`는 실제 Player 검증을 담당한다. 어느 후속 Task도 이 문서의 계획 상태를 실제 geometry/Player 완료로 승격해 해석할 수 없다.
