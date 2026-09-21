# SV5_06 공간 그래프 구현·검증 계약

이 파일은 이번 installed Task의 필수 상세 계약이다. 현재 등록된 SV5_06만 실행한다.
목표는 624×416 전체를 큰 장소·핵심 역할·보통 공간·연결 통로의 실제 공간 계획으로 만드는 것이다.
그림만 그리거나 이름과 추상 간선만 나열해 완료하지 않는다. 테스트한 C# plan에서 좌표 데이터와 검토 그림을 만든다.
기존 45개 작업 순서와 사용자 승인 규칙은 유지한다. 아래 제작 기준은 구현·검토 기준이며 새로운 사용자 확정 수치로 기록하지 않는다.

## C01. 선행 검토와 증거의 한계

REVIEW_FINDINGS.json의 원본 ZIP·40파일 SHA, 21개 최종 Passed, 별도 예상 실패 1개를 확인한다.
F1(Boss SealOpen 누락)·F2(candidate 의미 누락 digest)는 고쳐졌다. FIX01 전체 C04 이행이 끝났다는 뜻은 아니다.
CollectContacts는 인접 셀에 공통 route가 있으면 다른 route 쌍도 버린다.
실제 04 데이터의 그런 면은 907곳, 서로 다른 route 쌍·면 조합은 1422개다. 이는 누락 위치 수이지 exploit 수가 아니다.
AddContact의 true와 REVIEW의 routesExist는 predicate/ID 분류만으로 LogicalStateTransitionChecked를 true로 만든다.
이 표시를 새 그래프 승인 근거로 소비하기 전에 C05를 반드시 해결한다. 기존 Result/generated는 수정하지 않는다.
이 보완은 이미 등록된 06의 공간 접촉 책임에 포함한다. 새 FIX ID나 등록 예외를 추가하지 않는다.
FIX01의 RMAP13 ExploreWithAnalysisNodes는 기존 StateTransitions와 CanTraverse를 재사용한다. 이 단일 FSM을 유지한다.

## C02. 정본과 새 공간 계획

단위: 1×1 tile, world 624×416, MicroChunk 12×8 = 52×52개, Pattern 4×4.
좌표는 정수, x=0..623/y=0..415, 경계 half-open [x,x+w) × [y,y+h), bottom-left를 기본으로 문서화한다.
기존 정본 좌표/방향 표기와 다르면 explicit 변환을 한곳에서 수행하고 왕복 검사한다.
Sv5CoreReservationPlan.Source와 RouteSource.Graph의 identity/같은 world를 검증한다.
기존 8 physical sites·2432 core cells·slots·access/state geometry를 보존한다. Seal/Boss의 logical role은 둘이다.
대표 04 좌표/셀 수는 그 fixture의 측정값이지 모든 시드의 새 보편 규칙이 아니다.
새 Sv5SpaceGraphPlan이 core를 읽고 자기 장소/포트/연결/예약을 소유한다. 기존 RMAP16/04 경로를 덮어쓰지 않는다.
새 경로 geometry가 필요하면 06 소유 plan의 명시적 adapter로 분리하고 old→new route mapping을 내보낸다.
04의 route AIR·support 전체를 앞으로 영원히 재배치 불가능한 core 셀로 착각하지 않는다.
반대로 core/슬롯/실제 필수 접근·상태 차단 셀까지 자유 경로로 취급하지 않는다.
고정 core를 옮겨 예시의 장식 좌표에 맞추지 않는다. 정본 Village 24×16을 이번에 36×24로 자동 확장하지 않는다.
핵심과 큰 장소를 먼저 배치하고 연결 공간을 계획한 뒤 청크/패턴 소유로 분해한다. 청크별 독립 랜덤을 먼저 합치지 않는다.

## C03. 장소, 보통 공간, 연결 가능한 영역

plan 입력은 정본 core + 명시적 seed + versioned authoring profile이다. UnityEngine.Random 전역 상태를 사용하지 않는다.
stable ID/Family/역할/Core binding/footprint/ports/future owner/검증 수준을 가진 장소를 배치한다.
하나의 긴 동굴이 여러 청크를 차지해도 하나의 place ID다. Family별 독립 장소 개수와 청크 수를 구별한다.
큰 내부 약 7×5 이상의 열린 공간을 허용한다. 모든 장소를 4×4 크기 상자로 축소하지 않는다.
승인된 예시의 101장소/166연결/seed40921은 고정 정답이 아니다. 그 모양을 정본 core에 복사하지 않는다.
전체 영역에서 핵심 주변·큰 장소 사이·외곽에 일반 방/동굴 공간/계단참/분기 여유를 계획한다.
이 Task의 기본 연결 일반 공간은 실제 노드와 연결 예약을 가진다. 의미/보상 없는 왕복 가능한 방도 포함한다.
가상의 공중 선만 통과하고 장소 본체는 닿지 않는 배치를 허용하지 않는다.
잔여 영역에는 SV5_08 대상 INFILL_PENDING 소유를 명시한다. 미설계 셀을 AIR 또는 SOLID로 확정하지 않는다.
624×416 전 영역을 몇 장소와 끝없이 긴 통로만으로 덮고 나머지를 완료 처리하지 않는다.
profile에 장소 수/면적·연결 길이/지역별 점유 목표를 명시하고 대표 출력의 quadrant별 분포·미배치 원인을 보여준다.
위 목표는 조정 가능한 제작 입력이다. 희귀 지형을 모든 시드에 강제하거나 미정 확률을 사용자 수치로 invent하지 않는다.
대표 검토 profile은 정본 8 sites 외에 여러 Family와 여러 일반 방이 실제로 나타나도록 정하고 수치를 기록한다.
07은 반복 억제 강화, 08은 잔여 일반 공간 채움, 09는 추가 재합류·지름길, 10은 불규칙 샛길을 담당한다.
06은 그 작업들이 붙을 node/port/candidate extension API와 공간 소유를 제공한다. 그 Task들을 미리 COMPLETE로 표시하지 않는다.
가벼운 기본 충돌 회피와 seed 결정성은 지금 구현한다. 07의 분포 최적화 전체를 대신 구현하지 않는다.

### 장소 footprint 제작 경계

승인 규칙과 reference/v4의 세부 크기/역할을 읽어 profile에 연결한다.
협곡 상부 약10×깊이20·아래로 좁아짐, 도서관18~20×20~25, 회랑 높이15~20, 동굴 약50×상하20 등은 footprint 요구다.
주거지/도서관의 좌우 높이 비대칭, 점프맵 고체+발판/실제 Grab/1~2칸 외곽 굴곡의 후속 여유를 예약한다.
허브12×30·좌우 세 높이대는 4~6개의 실제 구별 포트가 가능한 여유로 잡는다. 같은 이웃 6중복으로 연결 수를 채우지 않는다.
목장/농장/호수 인접 선호는 profile metadata로 보존한다. 기능·배경·스폰 확률 보정은 담당 Task로 남긴다.
호수·핀볼·미검증 점프·열차·엘리베이터를 필수 진행의 유일한 수단으로 사용하지 않는다.
타임캡슐/감옥은 SEALED_SPECIAL로 분리한다. 공개 연결 부재가 정상이며 공개 통로를 뚫어 연결률을 올리지 않는다.
절구 작업장은 정확한36×16 예약이며 내부 미정이다. 전체 AIR/SOLID로 임시 굽지 않는다.
철도 footprint/향후 경로 예약과 도보 통로는 구별한다. 60이동·대각 상하·3칸통로 제약을 후속 소유로 보존한다.
미구현 Family는 PLANNED_SHELL/RESERVED이지 내부 기믹 구현 완료가 아니다.

## C04. 실제 port와 통로 예약

각 port는 소유 place ID, 경계 셀 집합, 안쪽/바깥쪽 접점, 방향/flow, clearance, 조건 출처를 가진다.
core port는 실제 RMAP15 access ID·셀·조건을 사용한다. 문자열 GATED로 조건을 추정하지 않는다.
Start EXIT는 실제 사용 또는 UNUSED_WITH_REASON을 결정한다. ENTRY/EXIT를 편의상 바꾸거나 모두 무조건 연결하지 않는다.
Village ENTRY/EXIT는 OPTIONAL로 실제 접근과 복귀를 연결한다. Village 방문이 자원/Forge/Boss flags를 바꾸지 않는다.
일반 방의 말단도 들어갔다 되돌아갈 수 있다. 단방향 추락만 가능한 막다른 가지를 허용하지 않는다.
통로는 from/to port, 순서 있는 centerline, corridor envelope/clearance, 교차점, 보호/지원 필요, 조건·방향을 가진다.
좌표열은 연속되어야 한다. 단일 셀 대각 건너뛰기·teleport·port와 떨어진 끝점·중심점만 닿는 경우를 거부한다.
완성 타일 전 단계의 공간 예약이므로 수직 이동은 SUPPORT_LAYOUT_PENDING 등 실제 후속 책임을 붙인다.
예약만으로 점프/Grab/머리 여유를 통과했다고 하지 않는다. 일반 상승+1, Jump+Grab최대+2를 넘는 미지원 필수 구간은 해결 대상으로 남긴다.
교차에는 JOIN / SEPARATED / CONDITIONAL_GATE / PENDING의 명시적 타입과 좌표·소유가 필요하다.
SEPARATED는 2D에서 실제 분리 셀/clearance가 있어야 한다. 선 그림의 위아래 교차 표시만으로 분리되지 않는다.
조건 gate는 차단 경계/셀·predicate·상태별 OPEN/SEALED 계획을 가진다. 횡단 가능한 전체 폭을 막아야 한다.
한 셀에 guard label만 남겨 주변 AIR로 돌아갈 수 있으면 안전한 gate 예약이 아니다.
일반 같은-stage 통로 합류는 허용하되 모든 합류가 명시적 graph 연결로 반영되어야 한다.
최종3~4칸 지형 두께/6×6 SOLID 검사는41/42 소유다. 이번 예약 plan에 그 최종 검사를 했다는 수치를 만들지 않는다.

## C05. 접촉 쌍 전수 열거와 상태 검증 — 이번 필수 보완

고정된 작은 반례: p=(10,10), routes={A,B}; q=(11,10), routes={A,C}.
공통 A가 있어도 서로 다른 (A,B),(A,C),(B,C) 세 쌍을 그 face에서 열거한다. 같은 route 자기 접촉만 제외한다.
shared-cell은 각 다른 route pair를, cardinal face는 두 셀의 Cartesian product에서 다른 route pair를 열거한다.
cell/face 좌표와 route pair를 stable key로 사용한다. 방향별 검사 기록은 따로 보존하고 임의 중복은 제거한다.
Passage와 Clearance 접촉을 모두 다룬다. 조건 문자열이 같다는 이유로 접촉 검사를 생략하지 않는다.
기존 CollectContacts 또는 그 공용 열거기를 고치고 새 06 공간에서도 같은 완전한 쌍 열거기를 사용한다.
기존 AddContact/review rows의 LogicalStateTransitionChecked는 실제 전이를 평가하지 않았다면 false/PENDING이다.
predicate 분류 완료·baseline6순서·candidate집합·contact논리·예약geometry·composedgeometry·Player 상태를 분리한다.
기존 종합 bool의 의미를 정정/분리할 때 API version과 소비자를 함께 갱신한다. 실제 baseline proof의 PASS는 유지한다.
과거 tests의 잘못된 checked=true assertion은 진실한 readiness assertion으로 교정하고 해당 변경 이유를 남긴다.
기존 F1/F2·정상 복귀·반례·결정성 assertions는 제거/완화/skip하지 않는다.
새 geometry의 모든 노출 접촉은 from/to anchor와 허용 방향을 생성한다. 통로 중간 접촉은 split node로 표현한다.
말단 edge의 guard가 중간 접촉에서도 적용된다고 자동 가정하지 않는다.
RMAP13 기존 상태 탐색/CanTraverse를 재사용해 도달 가능한 상태별로 그 접촉이 허용하는 전이를 검사한다.
민감 role 조기 진입, 방향 역전, 필수 action 생략, 복귀 불능을 분리하며 실제 시작 prefix/before/after/action을 남긴다.
UNKNOWN mapping 또는 미실행 접촉을 안전한 것으로 채택하지 않는다. 유효한 분리/재배치/guard 예약으로 계획을 고치거나 후보를 거부한다.
06 접촉 결과는 입력/출력 plan digest, 두 좌표, route/place/port pair, 평가 상태 수, predicate, 검사 수준을 가진다.
04의 3개 label 좌표와109-edge raw AIR witness는 역사 입력으로 재분류하고, 새 plan 대응 또는 해당 경로 대체 사실을 기록한다.
옛 AIR 좌표열을 지워 회귀를 피하지 않는다. 옛 좌표열이 새 경로와 다르면 NEW_GEOMETRY_MAPPING으로 구별한다.
예약 gate 증거는 runtime gate 증거가 아니다. ComposedGeometryReady/PlayerVerified는 false를 유지한다.

## C06. 공간 그래프와 진행 그래프를 실제로 결합

Place adjacency와 directed progression graph를 구분하되 같은 node/port/route mapping에서 투영한다.
Core 논리 node와 action 의미는 그대로 사용한다. 일반 장소/분기/접촉 node는 action-less analysis node다.
SV5_05_FIX01 후보 API의 whole-set 검사와 새 실제 connector 전체 검사를 각각 실행한다.
기존11개 edge가 항상 존재하는 baseline+candidate 분석만으로 새 공간 plan이 완주 가능하다고 선언하지 않는다.
06 projection proof에는 실제06 연결/포트/접촉으로 구현된 edge만 넣는다. 원래 edge는 route mapping이 있을 때만 투영한다.
기존 core action과 자원6순서를 유지한다. Optional에서도 새 지름길 없이 정상 접근·복귀가 된다.
Required는 기존 의미를 유지하고 아직 실현하지 않은 shortcut을 실현 완료로 바꾸지 않는다.
6순서마다 성공 trace 존재와 모든 관련 도달 상태의 goal 역도달성을 따로 검사한다.
reachable non-goal dead-end를 실제 prefix로 거부하고 보상 없는 왕복 말단은 허용한다.
숨겨진 Type0 내부처럼 공개 접근 불가능한 노드는 공개 연결률 분모와 별도 사유로 기록한다.
미래 SV5_09가 추가할 후보를 현재 accepted set에 몰래 포함하지 않는다. 승인 후보/거부 후보/미평가 제안 상태를 분리한다.
RMAP13 public API가 unknown edge endpoints를 drop하는 동작이 있더라도 06 adapter는 호출 전 참조 무결성을 명시적으로 거부한다.
더 많은 일반 node를 위해 상태를 복제하지 않는다. 필요하면 RMAP13 공용 탐색 접점을 최소 확장한다.
대표 크기에서 visited states/transitions/실행시간을 기록한다. goal 역탐색은 reverse adjacency index를 사용해 매 상태마다 모든 edge 재검색을 피한다.
검사를 생략하거나 reachability cache를 새 plan digest 없이 재사용해서 성능을 맞추지 않는다.

## C07. 소유·결정성·출력

place/port/connector/split-contact/gate/clearance/후속 owner를 versioned schema로 정의한다.
footprint와 연결 예약을12×8 chunk와4×4 pattern 인덱스로 투영한다. 음수/경계초과/서로 다른 예약의 충돌을 진단한다.
청크 경계 너머 한 장소/통로를 같은 ID로 유지한다. 아직 최종 S/A/O 셀값이 아니라 reservation semantics다.
identity는 world/core/source graph digest·seed/profile·모든좌표/방향/guard·채택집합·소유/상태를 포함한다.
열거 순서 변화는 digest 불변, 실제 topology/port/guard/footprint/profile 변화는 digest 변경이다.
거부 후보와 반복 생성은 원본 core/graph/RNG 입력을 mutate하지 않는다. candidate failure diagnostic도 deterministic이다.
같은 schema JSON과 CSV/시각화의 ID·좌표·개수·digest를 대조한다. 그림용 별도 가상 배치를 만들지 않는다.

## C08. 실제 집중 검증

아래는 필요한 검증 책임이다. 테스트 함수 개수를 맞추는 요구는 아니다.

| Gate | 실제 검증 |
|---|---|
| T01 core/input | world624×416, 같은 source identity, core/slot/state/기존증거 불변; 잘못된 world/digest 거부 |
| T02 layout | 대표 seed+다른2seed에서 결정성/경계/중첩/긴 장소 같은 ID; 원본예시 복사 아님; 알려진 불가능 배치 명시 거부 |
| T03 ports | 실제 core port binding·Village왕복·Start EXIT 결정; 떨어진끝점/미지ID/잘못된방향/Type0공개개방 거부 |
| T04 pair completeness | 공통route tinyfixture의3쌍, shared cell3route=3쌍, passage-clearance/같은조건face, 중복제거; 옛04 누락 반례 |
| T05 contact truth | routeID존재/predicate나열만으로checked=true금지; unknown/missing-anchor PENDING; 실제same-stage허용·조기Boss접촉거부 |
| T06 actual projection | 6순서·정상복귀·candidate집합·모든도달상태역도달; 실제connector삭제하면 실패, baseline11edge로 숨기지 않음 |
| T07 gate/crossing | 통로중간 guard우회/일방향역행/가짜분리/guard옆AIR우회 거부; 계획차단/실제runtime상태 분리 |
| T08 dead ends | 선택왕복말단 허용, 들어갈수있는복귀불능말단 거부; 원래 goal trace가 남아있어도 거부 |
| T09 digest/export | 의미있는좌표/guard/direction/profile 변화에 digest변화, 순서불변; 출력JSON/CSV/SVG 동일plan |
| T10 preservation | 기존04/05/FIX01 export 불변, 모든 구 test export를06 하위로 격리; F1/F2회귀 유지 |

이 Task의 focused EditMode + SV5_05/SV5_05_FIX01 + 실제 변경한 RMAP13 접점의 직접 tests만 실행한다.
구 Sv5RouteStateFix01Tests.T05와 Sv5RouteStatePolicyTests.T10은05_FIX01에 출력한다. 실행 전에06 하위로 redirect한다.
과거결과를 덮고 되돌리는 방식 대신 test output root를 주입/수정한다. legacy_exports 하위 이름으로 두 fixture 출력 충돌도 분리한다.
순수 정적 분석/첨부 XML과 실제 이번 Unity실행을 구별한다. 최종 failed/skipped0, 실행 명령·Unity버전·필터·발견/실행/통과 수·XML SHA를 기록한다.
쉘 파이프가 있는 여러필터 문자열은 하나의 인자로 quoting한다. 전체회귀/PlayMode/build/Bake/Player는 이번 시험이 아니다.

## C09. 생성 파일과 사용자 검토 자료

다음 파일은 모두 MapDesign/MCP/GENERATED/SV5_06 아래에 둔다. 역할이 다른 반복 CHECK/VERIFY 문서를 만들지 않는다.

| 경로 | 내용 |
|---|---|
| BINDING.json | 선행 live commit/parent, packageManifest·bound/install/archive SHA, 실제 Read/Write/API, source before/after, 기존 evidence 보존 결과 |
| space_graph.json | canonical plan: world/core/seed/profile/digest, places/ports/connectors/gates/상태별 예약/소유 |
| places.csv / ports.csv | 안정 ID·좌표·footprint·역할·Family·core binding·후속 책임 |
| connections.csv | endpoint IDs·방향·ordered centerline/envelope 참조·논리edge/guard mapping·채택상태 |
| reservation_cells.csv | tile 좌표·reservation semantics·owner·chunk/pattern index; 충돌 분리; 최종tile값 아님 |
| state_proofs.json | baseline/candidate-set/actual-projection 구분,6순서 traces,all-state counts/역도달/반례 |
| contact_checks.csv | 실제 전체pair enumeration, 접촉anchor/direction/state/판정근거·준비상태 |
| obligations.csv | 미구현 family/이동support/runtime gate/07~10/41/42/44의 정확한 책임과 미검증 범위 |
| validation.json / focused_results.xml | gate별실제근거, XML수치/SHA, seed별분포, digest/export/불변성 검사 |
| preview/overview.svg | 624×416 viewBox의 전체 배치도; 실제 plan과 같은 번호·형상·port·연결·guard·미정 범례 |
| preview/A1.svg ... D4.svg | 전체도를4×4구획한16개 확대도;각구획156×104 tile; A1은왼쪽위, D4오른쪽아래 |
| preview/index.html | 외부의존없이 전체/확대도·번호표·범례를 열어 볼 수 있는 검토 진입점 |

시각화는 final cell render가 아닌 PLANNED LAYOUT임을 표시한다. 미설계 footprint 내부와 실제 확정core셀을 다른 스타일로 그린다.
core·큰장소·일반공간·연결/보류 후보·gate·숨겨진영역을 구별한다. 배경을 실제 AIR처럼 오해하지 않게 미정영역 범례를 둔다.
전체도에서 번호가 겹치면 확대도에 상세 라벨을 제공한다. 화면용 작은 글자로 수백ID를 쌓지 않는다.
SVG는 geometry export 코드에서 생성한다. 실제 HTML로16확대도링크/좌표방향을 확인하고 최소 전체도/중앙확대 하나를 렌더 점검한다.
검토환경 렌더를 못하면 이유와 누락을 명시한다. 그림 확인을 Unity/Player 검증으로 세지 않는다.

## C10. 완료, 정리, 다음 작업

C01~09의 이번 소유 검증을 마친 뒤에만 Result를 PASS로 쓴다. 후속소유인 기능/Player 미구현은 명시적으로 분리한다.
GEOMETRY_STATE_READY/PLAYER_VERIFIED는 false, layout planning과 actual projected logic/contact state의 검사결과는 실제대로 기록한다.
현재 채택 geometry에 UNKNOWN접촉/논리우회/불가능 port가 남으면 accepted-ready로 내보내지 않는다. 후보재설계 또는 이번Task BLOCKED/FAIL이다.
세부support/장치 구현은 담당 후속Task로 남길 수 있으나 해결되지 않은 현재검사 자체를 후속owner표로 숨기지 않는다.
새 활성 설명 MCP/SV5/10_SPACE_GRAPH_V5.md는 API·schema·샘플·검증수준·소비의무를 담는다.
MCP/SV5/02_PROTOCOL_V5.md는 동봉PROTOCOL_APPEND.md를 기존바이트 뒤에 정확히한번 append한다.
기존 inputs/Task/Archive/Result/generated를 보존하고 이번 _work만 소유확인후 삭제한다. helpers는 이번INPUTS 하위만 사용한다.
정상 Finalize 후 이번소유 변경만 atomic commit, push없음, SV5_07이하 시작없음.
최종값을 불변Result에 다시끼워넣으려고 amend하지 않는다. commit/ZIP SHA는 콘솔 및 review manifest로 보고한다.
SV5_06_REVIEW.zip은 이번GENERATED 아래에 만들고 자기자신을 포함하지 않는다.
ZIP은 이번 Task/Archive/Result/활성설명/Status/Master, 테스트한전체source/test/meta, 새JSON/CSV/XML/preview를 포함한다.
읽기전용 핵심 adapter/RMAP13/current policy·기존04 핵심CSV·FIX01 Result/BINDING·실제소비interface도 포함해 다른환경에서 검토할 수 있게 한다.
REVIEW_SOURCE_MANIFEST.json에는 project상대경로/SHA/bytes, 실제commit/parent, 최종state, focused XML수치, 미실행범위를 기록한다.
