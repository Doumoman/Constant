# SV5_05 진행 상태·지름길·연결점 계약

목표: SV5_04의 실제 core/route plan과 기존 RMAP13 상태 의미를 결합해 새 연결 후보의 조건 보존을 검사한다.
새 맵 생성이나 게임 runtime을 만드는 단계가 아니다. 실제 동작하는 논리 검사 API와 후속 공간 생성기의 필수 소비 계약을 만든다.
아래 이름은 의미/출력 요구다. 새 runtime 타입명은 기존 소스를 읽고 bound Task에 확정한다.

## C01. 원본과 상태의 단일 소유

Sv5CoreReservationPlan.RouteSource.Graph의 실제 node/edge와 기존 RmapWorldGraphState/GeneratedCompletionStateKey를 확인한다.
RmapWorldGraphPlanner의 Plan/Evaluate 및 실제 상태 action/edge predicate를 재사용한다. 시그니처는 현지 코드에서 읽는다.
위치, 자원 mask, 자원 순서 cursor, Forge-made, Seal-open, Boss-complete를 담는 기존 상태 의미를 유지한다.
WorldDefinition/RNG/site/Player 능력을 바꾸거나 거의 같은 진행 FSM을 따로 복제하지 않는다.
방문 key에는 진행 flags와 자원 순서 cursor를 포함해 자원 획득 후 같은 장소 재방문을 버리지 않는다.
입력의 source/graph/core digest·stable ID 계보를 검사한다. 서로 다른 plan을 이름만 같다고 섞지 않는다.

## C02. 조건을 실제 edge와 연결

11개 현재 route 각각을 RouteId로 실제 graph edge와 연결한다. from/to logical node와 실제 core port/flow도 대조한다.
NORMAL_* / *_GATED_* route label은 predicate가 아니다. RMAP13 실제 edge/action 조건과 access 조건을 명시적으로 연결한다.
기존 Condition 문자열을 substring/접두어/GATED 유무로 참/거짓 판정하지 않는다. 알 수 없는 조건/ID는 구체 진단으로 거부한다.
접근 조건, 장소에서 가능한 action의 조건, 귀환 조건은 역할이 다르다. 장소에 닿았다고 자원/Forge/Boss 상태를 자동 부여하지 않는다.
Village의 OPTIONAL_VILLAGE는 현재 미연결 역할이며 새로운 필수 자원 조건을 만드는 근거가 아니다.
조건부 edge와 실제 gate 소유/차단 셀은 별도 기록한다. 물리 gate가 없으면 없다고 기록한다.

## C03. 자원 여섯 순서와 정식 진행

MooncoreOre/CondensedCoefficientSap/DeepStarYeast의 6개 정확한 순서를 각각 같은 시작 상태로 검사한다.
자원은 해당 실제 resource node/action에서만 획득한다. 방문 또는 shortcut 통과가 남의 resource bit를 바꾸지 않는다.
중복 방문은 이미 가진 자원을 재획득하거나 순서 cursor를 건너뛰지 않는다. 기존 정본의 멱등/거부 규칙을 따른다.
Forge에는 세 자원, Seal에는 Forge, Boss 완료에는 Seal, Exit 완료에는 Boss의 기존 선행 조건이 필요하다.
Seal/Boss가 같은 physical site라고 서로의 logical node/action을 합치지 않는다.
각 순서의 trace에는 state before/after, edge/action ID, 조건 결과, 복귀와 Forge→Seal→Boss→Exit를 기록한다.
Boss 완료는 기존 PLANNED_COMPLETION_EVENT의 논리 시험이다. 전투 구현이나 live boss 처치 증거로 표시하지 않는다.

## C04. 복귀·일방향·실패 상태

각 자원 획득 뒤 다음 자원 또는 정상 복귀 지점으로 돌아갈 수 있고 마지막에는 Forge에 도달해야 한다.
실제 normal return edge를 유지한다. 일방향 edge에 편의를 위한 reverse edge를 자동 추가하지 않는다.
missing return/반대방향/잘못된 port/조건 충돌/최종 goal 실패를 구별하고 최초 실패 state와 edge를 내보낸다.
조건에 따른 일방향을 무조건 왕복으로 바꾸지 않는다. 정본이 요구하지 않는 새 방향을 사용자 규칙인 것처럼 추가하지 않는다.
불가능한 상태 조합과 정상 도달 상태를 구분한다. 정상 trace 전이로 선행 조건이 생략되는지를 검사한다.

## C05. 지름길 후보 API

후속 SV5_06/09가 호출할 실제 후보 검사 API를 제공한다. 후보는 stable ID·출발/도착 anchor·허용 방향·필수 조건·source provenance를 가진다.
anchor는 기존 graph node/port 또는 명시적으로 선언된 일반 연결 node를 참조한다. 모르는 anchor/중복 ID/다른 world는 거부한다.
일반 연결 node는 자원/Forge/Seal/Boss/Exit action을 가지지 않는다. 아직 생성하지 않은 장소에 실제 좌표나 scene object를 꾸며 넣지 않는다.
후보를 원본에 mutation하기 전에 순수한 분석 graph로 결합한다. rejected 후보가 원본/채택 집합을 바꾸지 않아야 한다.
지름길은 이동 거리/간선 수를 줄일 수 있으나 기존 필수 action·접근/전이 조건·정상 복귀를 생략할 수 없다.
성공 여부를 단일 start→exit 도달 bool만으로 판단하지 않는다. 새로 도달한 민감 node/action의 선행 조건 위반도 검사한다.
한 후보씩 뿐 아니라 동시 채택되는 후보 전체를 합쳐 다시 검사한다. 개별 후보 통과 결과를 합집합 통과로 간주하지 않는다.
NONE 같은 느슨한 조건으로 바뀐 edge에 결과 action의 guard만 남겨 우회를 숨기지 않는다. 실제 정본이 요구하는 접근 조건도 유지한다.
검사 결과는 논리 허용 여부/진단/증명 수준/물리 검사 필요를 분리한다. 논리 통과만으로 최종 맵에 무조건 배치 가능한 IsAllowed를 반환하지 않는다.

## C06. Optional/Required 정책

기존 RMAP13 정책의 기본 Optional과 Required의 실제 정의를 읽어 재사용한다.
Optional은 지름길 없이도 6순서와 정식 복귀가 가능해야 한다.
Required는 기존에 명시된 해방 후 자원 복귀 shortcut 요구를 검사하되 normal return을 삭제하지 않는다.
기존 RMAP13 planned shortcut 요구와 이번 후보의 ID·release 조건·책임을 연결한다. 존재하지 않는 물리 지름길을 완료로 내보내지 않는다.
SV5_05에서 임의 스폰 확률·좌표·연결 수를 새 사용자 확정치로 추가하지 않는다.

## C07. 경로 접촉과 서술 조건의 틈

같은 셀 공유뿐 아니라 통행 AIR가 맞닿는 연결점도 검사 대상으로 삼는다. cardinal face contact, passage/clearance 접촉을 다루고 같은 route의 연속 셀은 제외한다.
일반적인 같은 조건 경로의 합류와 서로 다른 gate 단계로 넘어가는 접촉을 구별한다. 모든 overlap을 기계적으로 금지하지 않는다.
접촉을 통해 가능한 전이를 상태별로 평가한다. stage가 다른 route끼리 이어지면 요구 조건/소유/실제 차단 근거를 확인한다.
입력은 SV5_04 RouteCells/Routes/AccessBindings/StateGeometry를 재사용한다. 실제 RMAP13 상태 guard와 대응이 없는 string은 기본 허용하지 않는다.
REVIEW_FINDINGS의 세 label 접촉 후보와 109-edge AIR 좌표열을 regression input으로 읽어 재현/분류한다.
AIR 좌표열은 Player jump/Grab/몸체/상호작용 증거가 아니다. 이를 실제 완료 exploit으로 단정하지 않는다.
반대로 논리 label을 붙였다는 이유로 같은 AIR 연결이 물리적으로 차단됐다고 판정하지 않는다.
실제 조건상 허용된 접촉이면 근거를 내보내고, 차단/재배치/후속 runtime action 확인이 필요한 접촉이면 해결 의무와 geometry 준비 불가를 내보낸다.
원래 raw AIR sample에 경고가 남는 것은 이 검사의 실제 출력이다. 원본 CSV를 고치거나 expected witness를 삭제해 PASS로 만들지 않는다.

## C08. 두 가지 준비 상태

LOGICAL_STATE_VERIFIED는 기존 상태 의미/6순서/후보 집합/접촉 predicate 검사의 증거 수준이다.
GEOMETRY_STATE_READY와 PLAYER_VERIFIED는 별도다. SV5_05의 논리 시험만으로 둘을 true로 바꾸지 않는다.
대표 baseline의 미해결 접촉은 GEOMETRY_STATE_READY=false와 정확한 issue ID·상태·좌표·후속 owner로 기록한다.
필요한 물리 guard가 없거나 player 이동 확인이 남으면 그 수준을 PENDING으로 보존한다.
SV5_06/09/41 소비자는 논리 통과 후보라도 자기 최종 geometry/contact 검사를 통과하기 전 ready로 승격하지 않는다.
후속 plan은 기존 RMAP15 core/ports/state를 보존하며 자기 책임의 route geometry를 재생성할 수 있다. 과거 SV5_04 CSV를 덮지 않는다.

## C09. 입력 보호·결정성·진단

기존 Source/Graph/Core/route/export는 불변이다. 새 결과와 trace는 이번 task 폴더로 export한다.
같은 graph/candidate set/state 요청에 대해 입력 열거 순서를 바꿔도 판정/정렬/digest가 같다. BFS 동률은 안정 ID 순으로 처리한다.
진단에는 code·candidate/edge/route ID·state before·요구/관측 조건·최초 counterexample trace를 포함한다.
원래 API가 제공하는 의미/거부를 재사용하고, 새로운 후보/접촉 책임만 추가한다. 불필요한 일반 규칙 엔진이나 상태 저장 시스템을 만들지 않는다.
