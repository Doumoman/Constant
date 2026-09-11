# SV5_06_FIX04 - 접촉 국소 경계와 물리-FSM product 보완

이번 수정은 FIX03 독립 검토 `SG06F3-R1/R2`에 한정한다. 정확한 수치와 원인은 `REVIEW_FINDINGS.json`,
고정된 반례는 `REPRODUCE.py`에 있다. 대표 world는 624×416 / seed 1304다.
Player 일반 상승 +1, Jump+Grab 최대 +2를 유지한다. Family/밀도/일반 재합류/Scene Bake/Player 구현은 시작하지 않는다.

## C01. FIX03 성공과 현재 실패를 함께 고정

제출 manifest 315개, 59/59 focused EditMode 기록, Finalize/commit 메타는 일관된다.
FIX03의 전역 coordinate movement와 기존 여섯 guarded bypass 차단, FIX02 W01/W02, protected AIR는 회귀로 유지한다.

exact FIX03 export의 ConditionalGate는 225개다. FACE 164 중 해당 contact face가 owner gate에 있는 것은 7개뿐이며 157개는 없다.
SHARED 61은 모두 owner gate의 blocking cell이 없다. 즉 218/225가 실제 접촉 좌표에서 분리되지 않았다.
`REPRODUCE.py`는 exact export SHA를 확인한 뒤 이 수치와 DeepStarYeast hard-lock을 재현한다.
이는 Unity/Player 실행을 주장하지 않는 독립 exported-plan 반례이며 수정 후 production validator와 Unity tests가 최종 증거다.

## C02. 모든 ConditionalGate의 국소 barrier

accepted plan의 각 ConditionalGate contact에 production `ValidateBarrierFixture` 또는 동등한 검사를 반드시 적용한다.

- SHARED: 해당 shared world cell 자체가 owner의 blocking cell이어야 한다. face-only gate는 실패다.
- FACE: contact의 두 world cell이 만드는 정확한 cardinal face가 owner의 blocking face여야 한다.
- boundary_id, ContactIds membership, source route 일치, 같은 gate의 다른 위치 face는 geometry 증명이 아니다.
- barrier cell/face는 typed predicate와 closed/open 상태를 가져 전역 coordinate movement에 같은 의미로 적용된다.
- protected AIR, OPEN/SEALED aperture, port clearance 및 core reservation과 충돌하지 않는다.

각 미지원 접촉은 accepted geometry reroute로 제거하거나, 합법적인 같은 상태 global join으로 바꾸거나,
permanent solid separation 또는 그 좌표의 typed local boundary를 만든다. 원격 route gate에 일괄 귀속하지 않는다.
같은 좌표에서 서로 모순되는 여러 state ownership을 만들거나 predicate를 임의 AND/OR 합성하지 않는다.

## C03. 전역 물리 이동과 정상 connection 평가

FIX03의 이동 노드 의미를 유지한다. 모든 connection의 Centerline/ApertureCells 합집합이 route ID 없는 world-coordinate graph다.
Clearance와 INFILL_PENDING은 통행 AIR가 아니다. 닫힌 gate의 blocking cell/face는 route label과 무관하게 전역 적용한다.

생산 evaluator는 guarded connection뿐 아니라 gate가 없는 NORMAL/OPTIONAL connection도 직접 평가할 수 있어야 한다.
FIX03 `Explore`의 `gates.Single(SourceConnectionId == connection.Id)` 가정은 제거한다. target gate가 없는 connection을 예외로 숨기지 않는다.
flow/direction 및 canonical predicate가 허용하는 방향만 expected-open transition으로 판정한다.

## C04. 물리 그래프 × canonical FSM product

RMAP13 canonical action/CanTraverse/FSM을 별도 논리 PASS로 두지 말고 동일 accepted physical plan과 product로 검증한다.
자원 세 종류의 6개 순서를 각각 따라 reachable FSM state를 열거한다. 각 state에서 모든 gate를 동시에 적용하고,
그 state에서 canonical predicate가 열린 모든 required core connection의 source-to-target 물리 도달을 확인한다.

- NORMAL_RESOURCE_APPROACH/RETURN 세 쌍은 해당 자원을 얻기 전후에 정상이어야 한다.
- NORMAL_FORGE_APPROACH/RETURN은 Forge 동작 전후의 정상 회수와 복귀를 보존한다.
- Forge 전 Forge→Seal, Seal 전 Seal→Boss, Boss 전 Boss→Exit는 차단한다.
- 조건 충족 뒤 각 guarded approach, action, 정상 복귀는 열린다.
- 6순서 모두 완료 가능, reachable-state reverse reachability 유지, unintended dead-end 0이어야 한다.
- actionless/BIDIRECTIONAL optional connection은 명시된 별도 predicate가 없으면 닫힌 core gate의 부수 피해로 끊기지 않는다.

FIX03 exact INITIAL에서 DeepStarYeast approach `SV5_CORE_CONN_93f752cf7b8f813c`와 return
`SV5_CORE_CONN_be0aa26aa7cc3b03`은 gate 없이는 true지만 Boss+Forge가 함께 닫히면 false다.
그런데 논리 proof는 `DeepStarYeast>CondensedCoefficientSap>MooncoreOre`를 성공 처리한다. 이 모순을 고정 before fixture로 두고 수정 후 true로 만든다.
`SV5_OPTIONAL_01`, `SV5_OPTIONAL_RETURN_TO_START`, `SV5_OPTIONAL_TO_VILLAGE`의 같은 초기 collateral lock도 제거한다.

## C05. gate cut 합성 안전성

새 blocking face/cell 후보는 선택된 세 open witness만 보존해서는 안 된다.
후보를 임시 적용한 뒤 C04 product 전체를 검사하여 현재 열려야 하는 required transition을 하나라도 끊으면 거부한다.
Boss/Forge/Seal 각각 단독뿐 아니라 동시에 닫힌 조합의 비국소 cut 상호작용을 검사한다.
distributed cut이 필요하면 owner/predicate/국소 contact 의미와 전체 보존 proof를 잃지 않는다.
단순히 hard-lock된 normal route를 state-gated route로 재분류하거나 RMAP13 논리를 완화해서 통과시키지 않는다.

## C06. 실제 생산 API 집중 시험

| ID | 필수 실패 fixture | 필수 성공 조건 |
|---|---|---|
| F01 | exact FIX03 accepted plan을 full local barrier validator에 넣으면 218 오류 | 수정 plan의 모든 ConditionalGate 국소 검사 0 오류 |
| F02 | SHARED no-cell, FACE remote-face, 가짜 boundary | exact blocking cell/face 또는 합법적인 reroute/join |
| F03 | FIX03 INITIAL DeepStar approach/return false | FIX04 same state와 첫 자원 순서에서 둘 다 true |
| F04 | Boss+Forge combined cut collateral lock | 개별/복합 gate 모두 expected-open required transition 보존 |
| F05 | 9개 guarded case만 검사 | 6 resource orders × 모든 reachable FSM states × open required connections |
| F06 | optional 세 경로 collateral lock | actionless/BIDIRECTIONAL expected-open 연결 보존 |
| F07 | route rename, set order, digest 의미 누락 | 같은 형상은 같은 결과; 의미 변경은 digest 변경 |
| F08 | export/proof/preview 불일치, 과거 bytes 수정 | 하나의 accepted digest와 FIX03 이전 evidence 보존 |

F01/F03/F04/F05는 fixture 전용 복제 함수가 아니라 planner가 실제 사용하는 production validation API를 호출한다.
반례 거부 뒤 source plan mutation이 없어야 한다. 테스트 좌표/connection ID만 예외 처리하지 않는다.
기존 FIX03/FIX02 집중 회귀를 삭제·완화·skip하지 않는다. 잘못된 assertion은 실패 이유와 대체 검사를 기록해 정정한다.

기본 필터는 `StarNight.Map.Tests.EditMode.Sv5`다. RMAP13 생산 접점을 바꾸면 직접 RMAP13 필터도 추가한다.
발견/실행/Passed/Failed/Skipped, Unity Editor/CLI, 전체 명령과 XML raw/blob SHA를 기록한다.
무필터 전체, PlayMode, build, Bake, Player 실행은 범위 밖이다.

## C07. 증거와 semantic digest

`GENERATED/SV5_06_FIX04`에 기존 FIX03 역할의 산출물과 다음을 추가한다.

- `local_barrier_checks.json`: 모든 ConditionalGate contact의 kind/좌표/owner/exact cell-or-face/support/result.
- `physical_transition_matrix.json`: resource order, reachable FSM state, connection, direction, expected predicate,
  closed/open gates, physical result, witness 또는 차단 이유. 단일 true/개수만으로 대체하지 않는다.
- before/after preview: 218 local barrier 실패와 DeepStar Boss+Forge hard-lock을 사람이 검토할 수 있게 표시한다.

JSON/CSV/proof/preview는 한 accepted plan digest를 소비한다. semantic digest는 movement cells/faces, 모든 contact 결정,
local separation geometry, typed predicates, simultaneous gate state, ports/flow/direction 및 product proof identity를 포함한다.
ordered path는 순서를 보존하고 집합만 정렬한다.

활성 설명은 `SV5/14_SPACE_PRODUCT_FIX04.md`에 작성한다. `02_PROTOCOL_V5.md`에는 동봉 suffix만 정확히 한 번 append한다.
FIX03와 이전 Task/Archive/Result/GENERATED/활성 문서는 소급 수정하지 않는다.
`ComposedGeometryReady=false`, `PlayerVerified=false`를 유지하고 SV5_07/08/09 책임을 넘겨받지 않는다.

## C08. 종료

F01~F08과 기존 집중 회귀가 실제 통과하고 local barrier 오류, expected-open 물리 transition 오류,
unintended dead-end가 0일 때만 PASS다. product를 실행하지 않았거나 normal connection을 예외로 건너뛰면 실패다.
matching PASS Result 뒤 native Finalize하고 이번 소유만 atomic commit한다.
최종 290행 = 249 COMPLETE / 0 CURRENT / 41 LOCKED, Current NONE, SV5_07 LOCKED다.

Review ZIP은 Result/Task/Archive/Status/Master, 변경 source/tests/meta, 모든 새 evidence/preview/XML,
읽기 전용 FIX03 증거와 INPUTS 패키지를 포함한다. self ZIP과 `_work`는 제외한다.
commit 후 `_REVIEW_MANIFEST.json`에 raw worktree와 commit blob SHA/bytes를 구별해 작성한다. push하지 않는다.
