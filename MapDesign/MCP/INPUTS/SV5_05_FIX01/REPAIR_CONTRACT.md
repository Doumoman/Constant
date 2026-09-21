# SV5_05_FIX01 구현·검증·출력 계약

이 작업은 SV5_05의 누락을 보완한다. 624×416 승인 공간 설계·45개 본 작업·Player 능력은 그대로 따른다.
임의로 물리 gate나 terrain을 추가하지 않는다. 기존 RMAP13 FSM을 재사용해 논리 검사부터 고친다.

## C01. 실제 접근 조건과 도달 상태

FIX01-F1의 정확한 candidate/입력 trace를 public checker에 넣어 재현한다.
Seal/Boss가 physical site와 port를 공유해도 logical role과 접근 선행 조건을 합치지 않는다.
새 candidate는 실제 port 조건과 해당 논리 진입의 필수 조건을 모두 보존해야 한다.
Boss 접근의 SealOpen과 Exit 접근의 BossComplete를 최종 action guard만으로 대체하지 않는다.
Guard가 충분한지는 실제 도달 가능한 상태와 기존 상태 의미로 판정한다. label의 GATED 문자열을 파싱하지 않는다.
다중 incoming edge의 조건을 근거 없이 모두 AND하거나 전부 OR해 새로운 규칙으로 만들지 않는다.
baseline NORMAL_FORGE_APPROACH의 무조건 이동과 Forge action/physical port 의미가 다른 기존 경계는 유지한다.
기존 RMAP13 edge나 action을 더 느슨하거나 강하게 바꿔 negative fixture를 통과시키지 않는다.
후보가 추가한 민감 구역 진입을 검사하며, baseline에 없던 접근 규칙을 전체 기존 경로에 소급 적용하지 않는다.

## C02. 성공 경로 존재와 우회/복귀 안전성을 분리

기존 RmapWorldGraphPlanner의 state/action/CanTraverse/visited-key를 재사용한다.
필요하면 기존 탐색 구현을 공유하는 순수 분석 접점을 최소로 추가한다. 별도 복제 FSM을 작성하지 않는다.
6개 자원 순서마다 baseline 성공 및 candidate-set 성공을 구분해 실제 trace를 내보낸다.
candidate-set 전체에서 모든 관련 도달 상태/전이를 검사해 민감 role의 선행 조건 위반을 탐지한다.
goal 한 개를 찾았다는 이유로 안전성 검사를 중단하지 않는다.
새 후보가 정상 진행 상태에서 진입 가능한 복귀/완료 불능 상태를 만드는지도 확인한다.
action/guard가 있는 유한 상태 graph의 goal 역도달성 등 기존 의미를 재사용한 검사를 사용할 수 있다.
일반 node를 거치는 2개 이상의 candidate에도 적용한다. 개별 Allowed 행들을 단순 합쳐 집합 Allowed로 표시하지 않는다.
도달 불가 비정상 flags를 실제 start trace의 반례처럼 쓰지 않는다. failure에는 최초 실제 prefix와 before/after를 남긴다.
후보 거부 때 원래 graph/core/채택 집합을 mutate하지 않는다. 집합 거부와 개별 구조 검사 결과를 명확히 구분한다.
미지 anchor/잘못된 enum/중복 ID/내부 집합 식별자와의 충돌은 명시적 오류로 처리한다.
Optional/Required와 normal-return 의미는 원래 API를 유지한다. 새로운 물리 shortcut 완료를 주장하지 않는다.

## C03. 입력을 완전하게 묶는 결정적 digest

고정 schema version을 바꾸고 이전 digest와 의미를 구분한다.
정규화된 입력에 world/core/graph identity, 모든 candidate의 ID, from/to ID와 kind,
direction, resourceMask, Forge/Seal/Boss 요구, provenance를 포함한다.
판정 결과·실제 candidate-set proof/trace·검사 수준도 그 동일 입력과 연결한다.
ReviewContact의 좌표/route IDs와 AIR witness의 순서·전체 좌표도 입력 identity에 포함한다.
set 열거는 안정 ID 순으로 정렬하되 witness의 경로 순서를 정렬로 지우지 않는다.
구분자/escaping이 모호하지 않은 고정 스키마 또는 length-prefix를 사용한다.
동일 ID의 target/방향/조건/provenance 변화는 digest를 바꾸고, 동일 입력의 열거 순서 변화는 바꾸지 않는다.
후보 canonical payload를 결과/export에 남겨 digest만 보고 내용을 추정하지 않게 한다.

## C04. 접촉과 증명 수준을 과장하지 않기

contact row에서 실제로 검사한 상태 전이·요구 predicate·판정 근거를 분리한다.
route ID가 존재하거나 어느 한 edge에 guard flag가 있다는 것만으로 상태별 검증 완료를 선언하지 않는다.
같은 셀 공유 및 cardinal face 접촉의 route 쌍을 검사한다. 두 셀이 공통 route 하나를 갖더라도 다른 쌍의 접촉까지 누락하지 않는다.
기존 graph 전이로 표현 가능한 접촉에는 C02의 같은 checker를 재사용한다.
미정인 crossing anchor/방향/실제 gate는 UNKNOWN/PENDING과 owner를 남긴다. string label로 안전성을 꾸미지 않는다.
AIR witness는 좌표 연속성/양 끝뿐 아니라 실제 고정 AIR 집합·SEALED 제외 조건까지 확인한다.
잘못된 witness/미지 review route/필수 분석 미실행을 LOGICAL_STATE_VERIFIED=true로 요약하지 않는다.
6순서 검증, 후보집합 안전성, 논리 contact 검사, geometry, Player 상태를 각각 표현한다.
범위 미정 접촉은 후속 issue와 준비 불가로 보존한다. 이미 검증한 순서의 PASS까지 거짓으로 바꾸지 않는다.
GEOMETRY_STATE_READY=false, PLAYER_VERIFIED=false를 유지한다.
과거 3개 label 좌표/109-edge AIR witness와 Start/Village/loop 의무를 삭제하지 않는다.
합성 geometry owner는 SV5_41, 실제 전체 Player owner는 SV5_44로 새 보완 의무 표에 정확히 연결한다.

## C05. 최소 집중 검증

다음은 책임 목록이다. 형식적으로 같은 수의 테스트 함수를 만드는 요구는 아니다.

| ID | 필요한 실제 검증 |
|---|---|
| T01 | 고정 Forge→Boss 후보 mask7/Forge=true/Seal=false를 실제 checker가 거부하고 누락 SealOpen과 실제 trace를 제시 |
| T02 | 동일 진입에 충분한 Seal guard를 둔 정상 후보와 기존 same-stage 연결을 허용; baseline 6순서/Required 정상 복귀 유지 |
| T03 | 일반 node를 거친 조기 Boss 진입 및 새 막다른 가지를 후보 전체 검사에서 거부; 기존 goal이 여전히 존재해도 실패 |
| T04 | 동일 candidate ID로 target·방향·resource/Forge/Seal/Boss 조건·provenance를 각각 변경하면 digest 변화; 동일 입력 역순은 같음 |
| T05 | 후보집합 proof가 실제 augmented graph에서 생성되고 base proof와 구분; export와 digest가 동일 결과를 가리킴 |
| T06 | invalid review route/가짜 AIR witness는 미검증으로 거부; 세 원래 contact와 AIR witness의 진단 수준/후속 owner 보존 |
| T07 | source/core/기존 generated 불변, old test export 부작용 차단, 후보 거부의 순수성·재실행 결정성 |

가능한 경우 수정 전 새 T01/T04 반례를 기존 public API로 실행해 예상 실패를 별도 FIX01 출력에 남긴다.
이것은 EXPECTED_PRE_FIX_FAILURE이고 최종 test pass 수에 섞지 않는다. 수정 후 실제 집중 실행은 failed/skipped 0이어야 한다.
기존 Sv5RouteStatePolicyTests.T10은 MCP/GENERATED/SV5_05에 export한다. 그대로 재실행하면 과거 증거를 덮는다.
이번 허용 범위에서 test export를 명시적으로 주입한 FIX01 경로나 통제된 이번 _work로 분리한다.
실제 assertions는 보존한다. 구 테스트를 skip하거나 이전 CSV를 삭제해 부작용을 숨기지 않는다.
SV5 정책의 기존 집중 검사와 FIX01 regression을 실행하고, RMAP13 분석 접점을 바꾸면 RMAP13 직접 검사도 포함한다.
다중 필터를 CLI에서 쓸 때 세미콜론을 쉘 명령 구분자로 해석하지 않게 해당 도구의 인자 규칙을 확인한다.
실제 tool/Unity version/category/filter/발견·실행·통과·실패·스킵 수/XML SHA를 기록한다.
전체 회귀·무필터·PlayMode·build·Scene Bake는 이번 범위가 아니다.

## C06. 출력과 다음 입력

새 활성 설명: MCP/SV5/09_ROUTE_STATE_FIX01.md. 실제 API/검증 수준/원래 결과와 차이/소비자 read 의무를 적는다.
MCP/SV5/02_PROTOCOL_V5.md에는 동봉 PROTOCOL_APPEND.md 블록만 정상 append한다. 기존 바이트 영역을 고치지 않는다.
모든 보완 증거는 MCP/GENERATED/SV5_05_FIX01/에 둔다.

| 파일 | 내용 |
|---|---|
| BINDING.json | native 등록/Task/bound/installed/archive 역할 SHA, 실제 Read/Write, 선행 Finalize commit, before/after source SHA |
| analysis.json | versioned canonical inputs, baseline/candidate-set proofs와 실제 trace, state safety 검사 결과/수치/diagnostics |
| contact_checks.csv | 검토 좌표/route pair/state/요구 guard/실제 검사 여부와 남은 geometry 책임 |
| obligations.csv | SV5_06/09/41/44의 남은 해결 의무와 검증 gate |
| validation.json | T01~T07 근거, 실제 focused 명령/XML SHA/수치, immutable bytes 보존, 정리, 미실행 범위 |
| focused_results.xml | 실제 최종 focused 시험 결과; 도구 고유 이름이면 실제 경로와 SHA를 정확히 매핑 |

필요하면 기존 CSV export를 같은 FIX01 폴더에 추가한다. 위 JSON/CSV는 실제 같은 분석 객체에서 생성한다.
일반 helper는 이번 INPUTS 아래, 생성 임시물은 이번 _work 안에 소유 목록을 두고 완료 전에 정리한다.
프로젝트 루트의 CHECK/FILES/VERIFY/README/HANDOFF 같은 새 보조 파일은 만들지 않는다.
기존 INPUTS, Task, Archive, Result, SV5_03/04/05 generated 전체를 보존한다.
Result는 MCP/REPORTS/SV5_05_FIX01_RESULT.md에 작성하며 과거 PASS를 소급 변경하지 않는다.
정상 Finalize와 이번 소유 commit 후 MCP/GENERATED/SV5_05_FIX01/SV5_05_FIX01_REVIEW.zip을 만든다.
ZIP에는 이 Result, 새 활성 설명, 실제 새 JSON/CSV/XML, 변경된 source/test/.meta와 읽기 전용 core adapter를 넣는다.
원문 RMAP13도 포함하고 source manifest에 project 상대 경로/현재 SHA/실제 보완 commit을 기록한다.
후속 검토에 필요한 original SV5_05 BINDING/condition_bindings 및 FIX01 REVIEW_FINDINGS도 원래 경로로 담을 수 있다.
ZIP 자체 또는 최종 commit SHA를 Result에 뒤늦게 넣으려고 amend하지 않는다. 최종 값은 콘솔에 보고한다.
