# SV5 Normal Issuing Protocol

SV5_01 is the sole first-registration boundary. From SV5_02 onward, a bound
Task must use the exact existing `single_task_v1` schema and verified SHA-256
values from the committed predecessor Result and installed Task.

- Exactly one new bound `mcp_patch` Task is opened at a time.
- Current must be `NONE`; its predecessor must be COMPLETE and its successor
  must be the exactly-one LOCKED Status/Master row.
- Apply installs and archives the same bytes, opens only the new row, and
  Finalize closes only that row after a matching `STATUS: PASS` Result.
- Every Result distinguishes static/design evidence from Unity and Player
  evidence. No later SV5 task may imply a completed feature before its own
  validation.
- SV5/19 are separate registered plans. This protocol never opens them.
- No push is part of any SV5 Apply, Finalize, or atomic task commit.

<!-- SV5_02_RULES_BEGIN -->
## SV5 활성 규칙 진입점

다음 SV5 Task를 바인딩하기 전에 MCP/SV5/03_RULES_V5.md를 읽는다.
규칙 원문은 MCP/INPUTS/SV5/SPACE_V5_RULES.md이며, 기존 승인 범위와 45개 순서는 유지한다.
MCP/SV5/04_RULE_COVERAGE_V5.csv와 05_RULE_READSET_V5.json으로 해당 Task의 원문·SHA·상세 읽기 범위를 확인한다.
문서 규칙 등록과 Player/Unity 구현 완료를 구분한다.
<!-- SV5_02_RULES_END -->

<!-- SV5_03_BINDINGS_BEGIN -->
## SV5 코드·데이터 접점과 파일 배치

다음 SV5 Task의 실제 Read/Write/API를 바인딩하기 전에 MCP/SV5/06_BINDINGS_V5.md를 읽는다.
MCP/GENERATED/SV5_03의 BINDINGS·CORE_BINDINGS·DATA_SCHEMAS·TASK_COVERAGE를 담당 범위만 찾아 읽는다.
현재 Task가 변경할 소스의 최신 바이트/소유를 다시 확인한다. 과거 조사 SHA를 모든 미래 변경 뒤에도 강제하지 않는다.
새 패키지와 임시파일 정리는 MCP/SV5/07_FILE_FLOW_V5.md를 따른다. 프로젝트 루트에 작업별 보조 파일을 추가하지 않는다.
한 Task 후 종료 범위와 기존 정상 검증 계약은 유지한다.
<!-- SV5_03_BINDINGS_END -->

<!-- SV5_05_FIX01_BEGIN -->
## SV5 진행 상태 보완 결과 소비

SV5_06/09/41을 바인딩하기 전에 MCP/SV5/09_ROUTE_STATE_FIX01.md와 SV5_05_FIX01의 실제 Result/Finalize를 확인한다.
SV5_05의 과거 6순서 PASS만으로 새 후보집합의 접근 조건·복귀 안전성이 확인됐다고 간주하지 않는다.
현재 candidate payload/digest와 보완된 상태 검사 결과를 사용한다. 미해결 geometry와 Player 검증은 별도로 남긴다.
SV5_41은 합성 geometry, SV5_44는 전체 Player 검증을 담당한다. 기존 45개 ID/순서와 한 Task 실행 원칙을 유지한다.
<!-- SV5_05_FIX01_END -->

<!-- SV5_06_SPACE_GRAPH_BEGIN -->
## SV5 공간 그래프 소비

SV5_07~10 및 SV5_41은 MCP/SV5/10_SPACE_GRAPH_V5.md와 MCP/GENERATED/SV5_06의 실제 plan/validation을 읽는다.
장소·통로·접촉·보호 예약은 동일한 공간 plan digest로 연결한다. 과거 FIX01의 contact checked 표시를 물리 증거로 승격하지 않는다.
후속 배치 변경은 06의 접촉 전수검사와 현재 FSM 기반 전체 연결 검사를 다시 호출한다.
PLANNED_LAYOUT / LOGICAL_STATE / CONTACT_STATE / COMPOSED_GEOMETRY / PLAYER 검증을 분리한다.
SV5_06 완료는 세부 지형·기능·Scene Bake 완료가 아니다. 다음 Task 하나씩만 새 정상 입력으로 연다.
<!-- SV5_06_SPACE_GRAPH_END -->

<!-- SV5_06_FIX01_BEGIN -->
## SV5 공간 예약 검증 보완 소비

SV5_07~10/41의 다음 바인딩은 SV5_06_FIX01 실제 Result/Finalize와 SV5/11_SPACE_GRAPH_FIX01.md를 읽는다.
SV5_06의 과거 contact PASS는 새 clearance를 모두 포함한 검증이 아니다. 보완 결과의 정본 reservation/projection을 소비한다.
미확정 gate 또는 보호 셀 충돌을 state segment의 guard만으로 안전하다고 바꾸지 않는다.
공간 밀도·단일 순환 구조의 미완 항목은 승인된 전체 예시와 대조해07~09와최종REVIEW에서 계속 검토한다.
이 규칙은 다음 Task를 자동으로 열거나45개 본 작업의범위를임의로교체하지않는다.
<!-- SV5_06_FIX01_END -->

<!-- SV5_06_FIX02_BEGIN -->
## SV5 계획 gate 보완의 후속 소비

SV5_07 이후 공간 작업은 SV5_06_FIX02의 실제 PASS Result/Finalize와 SV5/12_SPACE_GATE_FIX02.md를 선행으로 확인한다.
FIX01의 contact coverage와 corridor 충돌 수정은 유지한다. FIX01의 계획 gate PASS는 상태별 포트 검증 증거를 대체하지 않는다.
ConditionalGate를 포함한 상태별 예약, 보호 AIR/정본 OPEN-SEALED, 실제 경계에 연결된 FSM 증명을 함께 소비한다.
접촉 목록 포함 여부나 segment guard 반복만으로 PlannedBarrierVerified를 참으로 만들지 않는다.
Family 분포/일반 공간 밀도/재합류의 남은 책임은 SV5_07/08/09에 유지하며 다음 Task를 자동으로 열지 않는다.
<!-- SV5_06_FIX02_END -->

<!-- SV5_06_FIX03_BEGIN -->
## SV5 물리 접촉과 상태 gate 증명의 후속 소비

SV5_07 이후 공간 작업은 SV5_06_FIX03의 실제 PASS Result/Finalize와 SV5/13_SPACE_CONTACT_FIX03.md를 선행으로 확인한다.
route ID 또는 predicate 차이만으로 동일 world cell이나 cardinal face 접촉을 분리하지 않는다.
이동 검증은 accepted geometry의 전역 좌표 통행 의미와 실제 boundary/gate를 사용한다.
서로 다른 predicate 접촉은 reroute로 제거하거나 명시적인 분리 형상과 상태 의미를 가져야 한다.
FIX02의 W01/W02 포트 수정과 protected AIR 검사는 유지한다.
Family 분포/일반 공간 밀도/재합류의 남은 책임은 SV5_07/08/09에 유지하며 다음 Task를 자동으로 열지 않는다.
<!-- SV5_06_FIX03_END -->
<!-- SV5_06_FIX04_BEGIN -->
## SV5 물리 접촉과 FSM product 증명의 후속 소비

SV5_07 이후 공간 작업은 SV5_06_FIX04의 실제 PASS Result/Finalize와 `SV5/14_SPACE_PRODUCT_FIX04.md`를 선행으로 확인한다.
ConditionalGate 접촉은 해당 SHARED cell 또는 해당 FACE에 실제 barrier geometry가 있어야 하며, route 소유권이나 원격 gate만으로 분리를 선언하지 않는다.
모든 닫힌 gate를 동시에 적용한 물리 이동과 canonical SV5 FSM의 product에서 현재 열려야 하는 required approach/return/recovery를 보존한다.
gate cut 합성은 선택된 guarded endpoint뿐 아니라 모든 reachable state의 open transition 보존 검사를 통과해야 한다.
FIX03의 전역 좌표 이동, FIX02의 W01/W02 포트 수정과 protected AIR 검사를 유지한다.
Family 분포/일반 공간 밀도/재합류의 남은 책임은 SV5_07/08/09에 유지하며 다음 Task를 자동으로 열지 않는다.
<!-- SV5_06_FIX04_END -->

<!-- SV5_07_DIVERSITY_BEGIN -->
## SV5 독립 지형 반복 억제 소비

SV5_08 이후 배치는 SV5_07의 실제 PASS Result/Finalize와 `SV5/15_DIVERSITY_V5.md`를 읽는다.
같은 종류의 독립 장소를 가까이 배치하는 선택 확률을 낮추고 연속 지형의 청크/패턴은 formation 하나로 센다.
Core와 일반 연결 공간의 밀도를 유지하며 장소 삭제·크기 축소·Family 이름 변경으로 반복 지표를 개선하지 않는다.
정책/formation/실제 좌표/포트/통로/검증/그림은 동일 plan을 소비한다. 변경 배치는 FIX04의 local gate와 전체 physical-FSM product를 재검증한다.
새 방 밀도·loop·지형 조립·Player 기능은 각 후속 Task의 책임이다. 다음 Task는 정상 INBOX로 하나씩 연다.
<!-- SV5_07_DIVERSITY_END -->

<!-- SV5_08_INFILL_BEGIN -->
## SV5 일반 공간 채움 소비

SV5_09 이후는 SV5_08의 실제 PASS Result/Finalize와 `SV5/16_INFILL_V5.md`를 읽는다.
현재 생산 진입점은 Sv5SpaceGraphPlanner.PlanWithInfill이며 기존 Plan은 07 기준선 생성/회귀용으로 유지한다.
새 방·굴·계단참의 실제 셀, 4×4 write mask, 출입구·부모 연결·왕복 증거와 같은 accepted plan을 소비한다.
미소유 잔여는 INFILL_PENDING이고 AIR/SOLID가 아니다. 방 수/예약률을 완성 지형 또는 Player 통과로 승격하지 않는다.
배경은 미조립, 확정한 infill 셀은 SOLID/AIR로 구분한다. SV5_41은 이 payload를 실제 합성하고 SV5_44는 Player를 검증한다.
08의 추가 공간은 기존 actionless 통로에 붙인 구조물이다. 별개 지역의 재합류/지름길은 SV5_09에서 현행 gate/product로 검증한다.
기존 4×4 패턴/12×8 청크, +1/+2 이동 한계, 3~4칸 구조와 6×6 검사, SV5_07 반복 억제를 유지한다.
이전 증거를 재생성하지 않고 다음 Task 소유 출력으로 격리한다. 45개 본 작업 순서를 바꾸지 않는다.
<!-- SV5_08_INFILL_END -->


## SV5_08_FIX01 — inclusive connector length and report handoff

Active infill generation keeps the SV5_08 production API and actual 1×1/4×4 payloads.
A new ordinary connector includes both host/parent and child entry endpoints, at most24 ordered cardinal AIR cell visits.
Root HostAccess joins Path at exactly one equal endpoint; child Path starts at its parent boundary.
Ownership-filtered ExternalCenterline is a separate diagnostic. +1 diagonal movement consumes two cardinal moves.
The new length policy/digest and exports are defined by INPUTS/SV5_08_FIX01/LENGTH_RULE.json and REPAIR_CONTRACT.md.
Historical08 artifacts are immutable. Current tests/exports write only GENERATED/SV5_08_FIX01.
Legacy inventory is read-only: actual consumers determine ACTIVE/COMPAT/HISTORICAL/CANDIDATE/UNKNOWN; no retirement here.
Report handoff preference: when the user uploads a Result/Review, review it and provide the next concrete normal/repair package
and inline execution instruction in the same response when evidence suffices. Do not wait for another “go”.
If an exact required input is missing, state that specific blocker; do not invent hashes or permissions.
This preference does not open SV5_09 locally or authorize future Task execution/push without its own bound handoff.

## SV5 loop evidence amendment

- SV5_09의 LOOP/RANDOM_SHORTCUT은 실제 1×1 cell payload와 약 2칸 통행 공간을 가져야 한다. 공간 id 사이의 추상 edge만으로는 완료할 수 없다.
- 길이는 양 endpoint room-side AIR 셀을 포함한 상하좌우 중심선 셀 수이며 최대 24다. +1 지지 계단만 이번 단계에 포함한다.
- LOOP는 baseline alternate path와 cycle-rank +1을, RANDOM_SHORTCUT은 같은 상태·끝점에서 엄격한 실제 비용 감소를 증명한다.
- 새 연결은 모든 합법 FSM state와 6 resource order에서 진행 gate를 우회하지 않아야 한다.
- 20~50칸 불규칙 샛길은 SV5_10 책임이며 SV5_09 수치를 부풀리는 데 사용할 수 없다.

## SV5 loop topology evidence correction

- 재합류 loop의 alternate path, cycle rank, bridge 변화는 실제 baseline/final graph에서 계산한다. link 수를 더하거나 빼는 산술값과 고정 boolean은 증거가 아니다.
- loop의 내부 중심선은 baseline supported path와 분리되고 양 endpoint 두 곳에서만 접촉하며 최소 2개의 새 AIR 셀을 조각한다.
- shortcut 비용은 같은 endpoint의 baseline/final supported foot graph BFS로 비교한다. centerline 길이를 양쪽 비용으로 대입하지 않는다.
- 독립 검사는 occupancy와 edge CSV를 읽어 graph를 재구성해야 하며 production export의 판정 boolean을 신뢰하지 않는다.

## SV5_11 hub-shell rule

SV5_11 reserves and writes one six-way hub shell in actual 1×1 world cells. The hub has six sockets,
three per side across three height bands, and accepts four to six connections to distinct external spaces.
An unused socket is not a connection. A candidate with fewer than four real external connections is rejected.

The central tree is a reservation only; SV5_12 owns actual Grab surfaces and traversal. No 48×32 Sector,
SectorId, sector packing, or sector-derived RNG may return. SV5_10 dead-end sidepaths require forward traversal
only and may remain non-returnable.
