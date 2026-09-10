
# SV5_06_FIX03 - 전역 좌표 접촉과 상태 gate 보완

이번 수정은 FIX02 검토 SG06F2-R1에 한정한다. 정확한 수치와 좌표 전환은 REVIEW_FINDINGS.json,
사람이 읽는 그림은 REVIEW.pdf, 고정된 재현은 REPRODUCE.py에 있다.
대표 world는 624×416 / seed 1304다. Player 일반 상승 +1, Jump+Grab 최대 +2를 유지한다.
장소 Family/밀도/일반 재합류/Scene Bake/Player 구현은 시작하지 않는다.

## C01. FIX02 성공과 현재 실패를 함께 고정

제출 ZIP manifest 265개와 51/51 EditMode 기록, Finalize/commit 메타가 일관됨을 확인했다.
FIX01의 protected AIR/정본 OPEN 포트 자기잠금은 FIX02에서 수정됐다. 이 성공을 회귀로 유지한다.
FIX02가 내보낸 접촉은 7,188쌍이며 Join 6,963 / Separated 225다.
Separated 225는 FACE 164 / SHARED 61이고 전부 boundary_id가 비어 있다.
그중 93쌍은 양 route 모두 Passage를 포함한다.
이 수치는 FIX02 실패 fixture의 고정값이다. 수정된 plan의 접촉 개수를 정답으로 강제하지 않는다.

REPRODUCE.py는 exact FIX02 connections/ports/contact/gate SHA를 먼저 확인한다.
그 뒤 accepted envelope와 gate face는 그대로 두고 모든 exported SHARED/FACE 접촉만 이동에 포함한다.
Unity/Player 실행을 주장하지 않는 독립 계획 그래프 반례다. 수정 후 생산 검증과 Unity tests가 최종 증거다.

## C02. 여섯 개 닫힌 상태 우회

- FORGE_GATED_SEAL_APPROACH: INITIAL에서 조건 false지만 전체 접촉 모델은 252노드/4 route 전환으로 도달한다.
- SEAL_GATED_BOSS_APPROACH: INITIAL 및 FORGE에서 조건 false지만 46노드/2 route 전환으로 도달한다.
- BOSS_GATED_EXIT_APPROACH: INITIAL, FORGE, SEAL에서 조건 false지만 245노드/3 route 전환으로 도달한다.

대표 경로 전환 좌표와 route ID는 REVIEW_FINDINGS.json의 forbidden_reachability_cases를 그대로 소비한다.
FIX02 Explore의 SamePredicate 필터를 켠 모델만 false가 된다. 필터를 제거한 좌표 접촉 모델은 모두 true다.
세 gate의 typed predicate와 W01/W02 수정은 맞다. 문제는 gate 뒤로 재진입하는 다른 route 접촉을 실제로 막지 않은 것이다.

## C03. 전역 물리 통행 모델

생산 검증기의 통행 상태는 world coordinate와 state를 기준으로 하나의 정본 의미를 가져야 한다.
route ID, connection ID, predicate label을 이동 불가 벽처럼 사용할 수 없다.
같은 world cell이 통행 가능하면 어느 route가 없이도 하나의 통행 위치다.
cardinal face 양쪽이 통행 가능하고 실제 solid wall/closed gate가 없으면 그 face는 이동 가능하다.
route 소유 정보는 출처/진단/방향 증거로 보존할 수 있지만 공간 연결을 숨기지 않는다.

Passage와 Clearance의 실제 이동 의미를 명시한다. Clearance가 몸 통행 셀이 아니라면 모든 route에서 같은 좌표 의미를 가져야 하며
Passage로도 소유된 셀을 route별로 비통행 처리하지 않는다. 한 state에서 같은 좌표를 통행/비통행으로 동시에 해석하지 않는다.
INFILL_PENDING과 미설계 shell은 계속 통행 AIR로 간주하지 않는다.

## C04. 서로 다른 predicate 접촉의 해결

모든 exported SHARED/FACE 접촉은 다음 중 하나여야 한다.

1. 실제 accepted route/envelope를 재배치해 접촉을 제거한다.
2. 같은 조건의 정상 join으로 분류하고 전역 상태 이동에 포함한다.
3. predicate가 다르면 nonempty boundary_id와 실제 분리 또는 typed gate geometry를 갖고 전역 상태 검사에 포함한다.

SHARED 접촉은 한 world cell을 공유하므로 face-only boundary로 분리할 수 없다. reroute하거나 합법적인 state cell ownership을 둔다.
그 state cell은 protected AIR, 기존 OPEN/SEALED, port aperture와 충돌하면 안 된다.
FACE 접촉의 분리라면 그 cardinal face를 막는 정본 geometry/owner/state가 있어야 한다.
문자열 boundary_id만 붙이거나 description으로 separated를 선언하면 거부한다.
서로 다른 predicate를 AND/OR로 임의 합치지 않는다. 원래 RMAP13 action/CanTraverse를 보존한다.

## C05. 모든 gate를 동시에 적용한 상태 증명

각 reachable state에서 전역 이동 그래프를 만들고 모든 닫힌/열린 gate를 동시에 적용한다.
blocking cell/face는 world coordinate에 전역 적용하고 route 소유 key로 우회하지 못하게 한다.
각 조건 경로에 대해 source port/anchor, target port, 방향, 정상 복귀를 검사한다.

- Forge 전에는 Forge→Seal 금지, Forge 후에는 Seal entry와 SEAL action 및 복귀 가능.
- Seal 전에는 Seal→Boss 금지, Seal 후에는 Boss 접근과 action 및 복귀 가능.
- Boss 전에는 Boss→Exit 금지, Boss 후에는 Exit 접근 가능.
- W01: Forge=true/Seal=false/Boss=false에서 Seal entry는 열림.
- W02: Forge=true/Seal=true/Boss=false에서 Boss 접근은 열림, Exit는 닫힘.
- 자원 3종의 6순서와 모든 reachable state의 정상 역도달/dead-end 0 유지.

검사 영역을 줄이면 잘라낸 주변을 보존하는 경계 그래프와 독립 교차 검증으로 동등함을 입증한다.
두 route만 떼거나 서로 다른 predicate 접촉을 제외하는 검사는 보조 증거일 뿐 PASS 근거가 아니다.

## C06. 실제 생산 API 집중 시험

| ID | 필수 실패 사례 | 필수 성공 사례 |
|---|---|---|
| G01 | exact FIX02의 225 separated/빈 boundary와 여섯 상태 우회 | REPRODUCE의 각 우회가 수정된 production validator에서 차단 |
| G02 | 닫힌 gate 중간에 다른 predicate SHARED cell로 뒤쪽 재진입 | 같은 조건의 실제 join은 통과 |
| G03 | 닫힌 gate 전후를 cardinal FACE로 잇는 무조건 route | 실제 solid/gate face가 있는 분리는 차단 |
| G04 | route rename/predicate label/열거 순서 변경으로 reachability 변화 | 좌표/형상 동일 시 같은 상태 결과 |
| G05 | 빈 boundary, 가짜 boundary ID, SHARED에 face-only boundary | reroute 또는 검증 가능한 separation geometry |
| G06 | route-keyed blocked face/cell로 다른 route 우회 허용 | 전역 coordinate blocking과 simultaneous gates |
| G07 | contact/boundary/gate/state geometry 각각 의미 변경 | 의미 변경은 digest 변경, 집합 순서만 변경하면 동일 |
| G08 | export/proof/preview plan 불일치, FIX02 증거 수정 | 하나의 accepted plan digest와 과거 bytes 보존 |

G02/G03은 fixture 전용 복제 함수가 아니라 planner의 생산 이동 검증 API로 실행한다.
반례 거부 뒤 원본/채택 plan이 변하지 않아야 한다. 테스트용 route 이름이나 좌표 하드코딩으로 통과시키지 않는다.
기존 Fix02 G01~G08을 삭제/완화/skip하지 않는다. 잘못된 planned pass assertion은 실패 근거와 대체 검사를 기록해 정정한다.
기본 필터는 StarNight.Map.Tests.EditMode.Sv5다. RMAP13 접점을 바꾸면 직접 RMAP13 필터도 추가한다.
발견/실행/Passed/Failed/Skipped, Unity Editor/CLI, 전체 명령과 XML raw/blob SHA를 기록한다.
무필터 전체, PlayMode, build, Bake, Player 실행은 범위 밖이다.

## C07. 증거와 semantic digest

GENERATED/SV5_06_FIX03에 BINDING.json, space_graph.json, places.csv, ports.csv, connections.csv,
reservation_cells.csv, contact_checks.csv, physical_contact_checks.json, gate_geometry.json,
gate_state_checks.json, physical_gate_state_checks.json, state_proofs.json, validation.json, obligations.csv,
focused_results.xml과 preview/overview·16 확대·FIX02_bypass_before_after를 쓴다.
파일명이 달라지면 같은 정보를 잃지 않고 역할을 BINDING에 명시한다.

physical_contact_checks에는 각 SHARED/FACE의 세계 좌표, 양쪽 cell kind, global traversability,
join/separation/gate 결정, boundary/geometry owner, 적용 state와 결과를 기록한다.
physical_gate_state_checks에는 각 reachable state의 닫힌/열린 gate 집합, source/target, 차단 또는 경로 witness를 남긴다.
단일 true나 개수만으로 대체하지 않는다.

semantic digest는 global movement cell/face 의미, 모든 contact 결정, 실제 separation geometry, typed predicate,
gate cell/face/state, port/aperture/flow와 proof identity를 포함한다. route ID 변경만으로 물리 결과가 바뀌지 않아야 한다.
ordered path는 순서를 보존하고 집합만 정렬한다. JSON/CSV/proof/preview는 같은 accepted plan digest를 소비한다.

활성 설명은 SV5/13_SPACE_CONTACT_FIX03.md에 작성한다. 02_PROTOCOL_V5.md에는 동봉 suffix만 정확히 한 번 append한다.
FIX02 Task/Archive/Result/GENERATED와 SV5/12 문서, 이전 source snapshot은 소급 수정하지 않는다.
ComposedGeometryReady=false, PlayerVerified=false를 유지한다. SG06-F5와 07/08/09 책임을 넘겨받지 않는다.

## C08. 종료

G01~G08과 기존 집중 회귀가 실제 통과하고, 여섯 닫힌 상태 우회 및 다른 미해결 물리 접촉이 없어야 PASS다.
상태별 전역 검증을 실행하지 않았거나 separation geometry가 없으면 planned verification은 false/실패다.
matching PASS Result 뒤 native Finalize하고 이번 소유만 atomic commit한다.
최종 289행 = 248 COMPLETE / 0 CURRENT / 41 LOCKED, Current NONE, SV5_07 LOCKED다.
Review ZIP은 Result/Task/Archive/Status/Master, 변경 source/tests/meta, 모든 새 evidence/preview/XML,
읽기 전용 FIX02 증거와 INPUTS 패키지를 포함한다. self ZIP과 _work는 제외한다.
commit 후 _REVIEW_MANIFEST.json에 raw worktree와 commit blob SHA/bytes를 구별해 작성한다. push하지 않는다.
