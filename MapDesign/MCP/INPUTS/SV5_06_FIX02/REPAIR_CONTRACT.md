# SV5_06_FIX02 - gate의 상태별 포트 차단과 검증 일치

이번 수정은 FIX01 검토 SG06F1-R1/R2/R3에 한정한다. 상세 근거는 REVIEW_FINDINGS.json과 REVIEW.pdf다.
현재 대표 world는 624×416, seed 1304다. Player 일반 상승 +1, Jump+Grab 최대 +2를 유지한다.
공간 구성/밀도/Family/최종 Bake 작업을 함께 시작하지 않는다.

## C01. 성공한 것과 실패한 것을 각각 고정

FIX01 제출 파일 128개와 focused XML 43 Passed를 확인했다. 여기서 Unity를 다시 실행한 것은 아니다.
전체 접촉 입력 16,222개 / contact 7,188쌍, 누락·추가·중복 0을 독립 재현했다.
새 CorridorCenterline/Clearance/PortAperture와 FixedSolid 또는 기존 support 충돌은 0이다. 이를 회귀로 유지한다.
14개 gate 각각의 두 route envelope만 놓고 한 문을 닫으면 local cut이 되는 결과도 확인했다.
그 검사는 여러 gate가 함께 닫힌 상태, 필수 포트 접근, 정상 귀환을 보장하지 않는다.
기존 FIX01 결과를 수정하거나 실패했던 것으로 소급 변경하지 않는다. 읽기 전용 반례 입력으로 사용한다.
수정 후 plan이 바뀌면 contact 수 7,188을 정답으로 강제하지 않는다. 매번 새 accepted 모델을 독립 열거한다.

## C02. 필수 포트가 자기 선행 조건에 막히는 두 반례

W01: state mask=7, ForgeMade=true, SealOpen=false, BossComplete=false.
FORGE_GATED_SEAL_APPROACH는 ForgeMade를 만족하므로 RMAP15_SITE_SEALBOSS_PORT_ENTRY에 도착해야 한다.
그 포트 (533,311), (533,312), (533,313)을 SV5_GATE_F3608BF3D1EE0923F69A가 모두 막는다.
새 gate 조건은 ForgeMade && SealOpen이다. Seal에 도착해 SEAL|OPEN을 해야 하는 단계에서 이미 SealOpen을 요구한다.

W02: state mask=7, ForgeMade=true, SealOpen=true, BossComplete=false.
원래 SV5_04 OPEN geometry는 RMAP15_SITE_SEALBOSS_PORT_EXIT (560,311~313)을 AIR로 복구한다.
SEAL_GATED_BOSS_APPROACH가 이 포트를 소비한다. SV5_GATE_3699556D982E455C6E7E가 BossComplete까지 요구하며 다시 닫는다.

두 반례는 실제 planned cell/face와 port/조건의 모순이다. Player exploit이나 Unity 런타임 교착을 이미 실행했다고 쓰지 않는다.
현재 gate는 서로 다른 route 조건을 일괄 AND한다. 이를 무조건 OR로 바꾸는 것도 해결이 아니다.
필요한 전이와 차단할 전이를 나누고 gate 경계/방향/상태 소유를 실제 접근 가능한 쪽에 배치한다.
반례 원본의 gate ID와 좌표는 보존하고 수정된 계획의 대응 ID/좌표를 별도로 기록한다.

## C03. 계획 gate의 정본 데이터와 상태별 검증

gate에 실제 predicate 필드(mask/Forge/Seal/Boss), 셀 또는 cardinal face 집합, 양측 anchor, 허용 방향,
OPEN/SEALED 예약, source contact/route/port, 물리 경계 소유권을 명시한다. 설명 문자열은 판정을 대신하지 않는다.
같은 물리 경계의 중복은 정리하고, 조건이 다른 gate를 합칠 때는 허용/금지 전이에 맞는 근거를 검증한다.

검증은 accepted reservation 모델의 통행 공간에서 수행한다. 전체 corridor envelope, aperture,
실제 core AIR/state geometry와 보호 셀, 관련된 다른 gate의 동시 상태를 소비한다.
INFILL_PENDING이나 미설계 장소 shell 전체를 통행 AIR로 간주하지 않는다.
검사 영역을 줄이면 주변 연결을 보존하는 경계 그래프나 독립 교차 검증으로 동등함을 입증한다.
해당 route 두 개만 따로 떼어 문을 닫는 검사는 보조 증거이며 전역 상태 안전성을 대체하지 않는다.

- 닫힘: 금지한 전이가 실제로 차단되고 옆 clearance나 다른 route로 우회해 열리지 않아야 한다.
- 열림: 필요한 정상 전이와 포트 접근, 회복 가능한 복귀 경로가 유지돼야 한다.
- 여러 gate가 동시 적용될 때 필수 도착 포트/복귀를 막거나 먼저 필요한 action을 잠그면 거부한다.
- 방향성이 있으면 실제 허용 방향과 반대 방향을 별도로 검사한다. 임의 ONE_WAY로 문제를 숨기지 않는다.
- gate 정의가 부족하거나 공간 검증을 실행하지 않았으면 planned verification은 false/실패로 남긴다.

보완은 계획 검증 단계다. Player body/점프 물리, GameObject, runtime door 또는 Scene Bake 구현을 요구하지 않는다.
ComposedGeometryReady=false, PlayerVerified=false를 유지하되 이번 계획 검증 책임을 후속 Task로 넘기지 않는다.

## C04. 보호 AIR와 원래 상태 예약도 충돌 검사에 포함

기존 충돌 검사는 새 통로 세 종류만 본다. ConditionalGate의 SEALED/OPEN 의미와 core ProtectedAir도 검사한다.
FIX01 gate 차단은 보호 AIR에 24행/15셀 겹친다. 기존 state geometry의 560,311~313을 제외해도 15행/12셀이 남는다.
좌표 겹침 자체를 일괄 금지하지 않고 그 좌표의 소유와 상태별 허용 의미를 비교한다.
원래 SEALED/OPEN 세 셀은 합법적인 상태 변화다. 새 gate가 OPEN 상태를 Boss 조건으로 다시 막는 것은 별도 위반이다.
ProtectedAir를 allowlist에서 빼거나 지원/clearance를 삭제해서 충돌 0을 만들지 않는다.
같은 의미의 AIR/support 공유, 합법적 상태 변화, 실제 상태 충돌을 구별하는 생산 검증기를 사용한다.
원래 core S/A/O, slot, source identity, state geometry와 action 조건을 바꾸지 않는다.

06 소유 active connector/gate의 재배치가 필요하면 새 계획 안에서 수행할 수 있다.
이전 route geometry를 대체할 경우 old route ID -> active centerline/envelope/support/aperture 매핑을 기록한다.
과거 SV5_04/RMAP16 CSV나 core.Source 객체를 고치지 않는다. contact/projector/export가 낡은 RouteCells를 다시 섞지 않도록 한다.
Seal과 Boss가 같은 core 장소/포트를 공유한다는 이유로 서로의 action을 합치거나 미리 실행시키지 않는다.
별도 계획 전이 anchor가 필요하면 기존 core 내용을 보존하는 명시적 adapter와 원래 port 대응을 둔다.

## C05. 실제 경계와 FSM 증명 연결

현재 projector는 gate를 만들기 전에 원래 edge guard만 segment에 반복하여 6순서를 증명한다.
수정된 증명은 먼저 검증한 상태별 경계/포트/전이 모델을 소비해야 한다.
각 guard는 해당 상태에서 검증된 물리 계획 경계 ID와 이동 방향에 연결한다.
근거 없는 logical segment를 넣거나 baseline edge를 덧붙여 실제 삭제/차단된 통로를 되살리지 않는다.
모든 accepted Passage/Clearance/Aperture의 shared/face 접촉을 다루며 route별 점유 좌표와 방향을 보존한다.
미지 port/node/route/flow, 잘못된 endpoint, 누락된 경계 참조는 명시적으로 거부한다.

정본 RMAP13 CanTraverse/action/FSM을 재사용한다. 자원 3종의 6순서, 정상 Forge/Seal/Boss/Exit 순서를 유지한다.
모든 reachable state의 정상 복귀 또는 goal 역도달을 검사하며 정상 양방향 말단도 허용한다.
반례에는 실제 시작 prefix, before/after state, action, port/좌표/경계를 남긴다.
ProjectionPassed, PlannedBarrierVerified, ComposedGeometryReady, PlayerVerified를 별개 증거로 유지한다.

## C06. 실제 생산 API로 실행할 집중 시험

| ID | 필수 실패 사례 | 필수 성공 사례 |
|---|---|---|
| G01 | W01의 Seal 미개방 입구 차단 | Forge 완료 후 Seal 입구 진입 및 SEAL action, 원래 복귀 |
| G02 | W02의 기존 OPEN 출구 재차단 | Seal 개방/Boss 미완료 상태의 정상 Boss 접근, Boss 전 Exit 금지 |
| G03 | 옆 clearance 우회, 다른 route 우회, 함께 닫힌 다른 gate의 필수 포트 차단 | 전체 경계의 닫힘 차단과 OPEN 전이/복귀 |
| G04 | ConditionalGate의 무권한 ProtectedAir 점유, 원래 OPEN과 새 SEALED 충돌 | 합법적 상태 세 셀 변화와 동일 의미 예약 공유 |
| G05 | unknown port/node, endpoint 불일치, 잘못된 flow/방향, 가짜 aperture로 단면 검사 회피 | 정본 core 폭에서 전체 corridor 폭으로 이어지는 유효한 adapter |
| G06 | 실제 connector 삭제, 중간 무조건 접촉, 역방향 우회, 도달 가능 복귀 불능 가지 | 6순서/모든 reachable state 회복, 정상 왕복 말단 |
| G07 | 실제 port boundary/flow/envelope/gate OPEN-SEALED/predicate 의미 각각 변경 | 유효한 의미 변경은 plan digest 변경, 집합 순서만 바뀌면 동일 |
| G08 | 검사 plan과 export/preview 불일치, 과거 증거 수정 | 하나의 검증 plan과 모든 출력의 digest·의미 일치 |

테스트 이름이나 JSON 키 포함, true 플래그, 빈 blocking set 확인만으로 위 사례를 대체하지 않는다.
fixture 전용 복제 validator가 아니라 planner가 사용하는 생산 검증 경로에 실제 입력을 전달한다.
잘못된 입력을 거부한 뒤 원본/채택 plan이 변하지 않는지도 확인한다. geometry 형태를 하드코딩한 assertion으로 통과시키지 않는다.
W01/W02의 실제 선행 데이터를 읽는 실패 회귀와 수정 후 성공을 구별한다. 실패 원본을 새 생성기로 재생성하지 않는다.

이전 SV5_04/SV5_05/05_FIX01/SV5_06/06_FIX01 테스트의 export 경로를 먼저 이번
GENERATED/SV5_06_FIX02/legacy_exports/<각 테스트별 고유 폴더>로 옮긴 후 focused tests를 실행한다.
Sv5CoreReservationPlanTests.T08도 기존 SV5_04로 쓰므로 출력 경로만 반드시 분리한다.
기존 입력 참조는 그대로 두며 오래된 generated CSV/XML/Result를 덮어쓰지 않는다.
기존 유효한 assertion을 삭제/완화/skip하지 않는다. 잘못된 계획 readiness나 고정 개수 assertion 정정은 근거와 대체 검사를 기록한다.

기본 집중 범위는 StarNight.Map.Tests.EditMode.Sv5다. RMAP13 분석 접점을 바꾸면 그 직접 테스트도 명시 필터로 추가한다.
실제 Unity 버전, 전체 명령, 발견/실행/Passed/Failed/Skipped와 XML SHA를 기록한다.
무필터 전체 회귀, PlayMode, build, Bake, Player 실행은 이번 범위가 아니다.
작업트리 raw SHA와 Git blob SHA는 별도 역할이다. 로컬에서 줄바꿈이나 기대 SHA를 보정하지 않는다.

## C07. 증거와 결과

GENERATED/SV5_06_FIX02 아래에만 이번 결과를 쓴다.
필수: BINDING.json, space_graph.json, places.csv, ports.csv, connections.csv, reservation_cells.csv,
contact_checks.csv, gate_geometry.json, gate_state_checks.json, state_proofs.json, validation.json,
obligations.csv, focused_results.xml, preview/overview 및 16개 확대도, preview/W01_before_after와 W02_before_after.
파일이 추가로 필요하면 같은 디렉터리 안에서 역할을 명시한다. _work는 재현 가능한 임시 자료만 두고 정리한다.
gate_state_checks에는 상태/관련 문/anchor·포트/검사한 셀·face 범위/기대·실제 이동/결과/경로 또는 cut 증거를 남긴다.
최소 위 두 반례를 별도 기록하고 상태 전체 검증과 연결한다. 단일 true 값만 내보내지 않는다.
semantic digest는 typed predicate, 각 상태 형상/방향/소유, active route 매핑과 검증 증명 정체성을 포함한다.
기존 FIX01의 port/connection/envelope/contact 필드 결합을 유지한다. 집합 순서는 정렬하되 ordered path는 보존한다.

BINDING에는 선행 Result/Task/Archive, 실제 commit/parent/blob, 등록 전후 SHA, native phase,
실제 read/write/소스 before-after, 기존 증거 보존 검사, 미완 책임을 기록한다.
활성 설명은 SV5/12_SPACE_GATE_FIX02.md에 작성하고 02_PROTOCOL_V5.md에는 동봉 PROTOCOL_APPEND.md만 정확히 1회 append한다.
기존 SV5/11_SPACE_GRAPH_FIX01.md, 과거 보고서, 입력 패키지, native 규칙은 변경하지 않는다.
SG06-F5의 장소 22개/대부분 미설계/단일 순환은 계속 미완이다. 07 Family, 08 일반 밀도, 09 재합류 책임을 유지한다.
101개 장소나 166개 연결을 모든 seed의 고정 정답으로 강제하지 않는다.

## C08. 종료와 전달

G01~08의 필수 사례와 기존 회귀가 실제 통과하고, 채택 계획에 미해결 접촉/상태 예약 충돌/계획 gate 오류가 없어야 PASS다.
실행하지 않은 composed geometry와 Player 증거는 false로 유지한다. 현재 계획 검사를 후속으로 미룬 채 PASS하지 않는다.
작업 중 Status/Master를 수정하지 않는다. matching PASS Result 뒤 native Finalize와 이번 소유만 atomic commit한다.
기존 FIX01은 COMPLETE로 보존한다. 다음 07은 LOCKED로 남기며 push하지 않는다.
Review ZIP은 이번 Result/Task/Archive/Status/Master, 소스/tests/meta, JSON/CSV/XML/preview,
읽기 전용 core adapter/RMAP13과 상태 데이터, 패키지 입력을 포함한다. self ZIP과 _work는 제외한다.
_REVIEW_MANIFEST.json에는 모든 path/raw SHA/bytes와 실제 commit/parent 및 commit blob SHA/bytes를 구별해 기록한다.
커밋 후 Review ZIP을 만들며 ZIP을 자기 commit에 넣어 순환 SHA를 만들지 않는다.
