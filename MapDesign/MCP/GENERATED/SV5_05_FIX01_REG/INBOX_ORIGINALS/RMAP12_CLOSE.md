# RMAP12_CLOSE - 실제 통합 확인·마무리 후 RMAP13 순차 진행

- MODE: 기존 RMAP12의 근거 확인/필요한 보완과 후속 작업 준비를 위한 사용자 인계 지시
- 새 Task ID를 등록하는 single_task_v1 문서가 아니다. 이 파일 자체를 새 Task로 Apply하지 않는다.
- INPUT: MapDesign/MCP_INBOX/RMAP12_CLOSE.md
- 먼저 처리할 Task: RMAP12_WORLD_DATA
- 다음 Task: RMAP13_WORLD_GRAPH. 실제 선행 PASS/Finalize/commit 확인 후 별도 정상 Apply.
- 다음 미실행: RMAP14_BIOMES
- 사용자는 기존 순차 작업의 계속 진행을 요청했다. 근거 확인·필요한 수정·다음 Task 준비에 같은 허락을 다시 묻지 않는다.

## 1. 현재 근거와 이번 확인 이유

제출 RMAP12 Result는 STATUS PASS이며 Unity 6000.3.8f1의 focused EditMode 2/2 PASS를 보고했다.
확인된 구현 설명은 Unity Object와 분리된 WorldData 계약, 7개 stream seed, 종류별 대표 stable ID, mutation text round-trip이다.
보고서는 실제 생성/저장 소비 경로, 같은 종류 여러 객체의 ID 구분, Finalize/commit 근거를 충분히 제시하지 않았다.
이것만으로 구현 실패라고 단정하지 않는다. 실제 코드를 읽고 이미 있는 증거는 재사용하며 부족한 책임만 보완한다.
테스트 수가 2개라는 이유로 늘리지 않는다. 요구와 연결된 실제 assertion/호출 경로가 충분한지를 본다.

### 고정된 입력 근거

- 제출 RMAP12 Result: MapDesign/MCP/REPORTS/RMAP12_WORLD_DATA_RESULT.md
- 제출 Result SHA-256: a798276303f381f323c0dd9b8492ceff291a34e84cf49c1628bb372708567061
- RMAP12 설치 Task/Archive SHA-256: f6b649abc0e452fb897e7e4175adee3ab94e4cfe051cb7234be8147ee9c184fb
- 제출 Result가 확인한 RMAP11 PASS SHA: 524b1f3aa1ab77cd4ff9c32ff9ffc91e317b613ae6b069cd1be8fa45d1a00865
- RMAP11 설치 Task SHA: 1f96f5414fb0aa9040017491c9b5f33978822fdcfa0cdd56ea3f78af57d950c5
- RMAP12 XML: MapDesign/MCP/GENERATED/RMAP12/rmap12_editmode_results.xml
- 제출 XML SHA: b2b3fbb8a5acc62b0aeff4d49fb4cb2846aea2a91546f5d91f4734100007147b
- 위 값은 제출본 기준이다. 로컬 Result가 이후 보완됐다면 git 이력/현재 파일/검증 자료로 변경 근거를 확인한다.
- 출처 없는 해시 차이를 자동 수용하거나 줄바꿈 정규화/재저장으로 해시를 맞추지 않는다.

## 2. Preflight / 상태와 변경 경계

외부 전달 메시지의 SHA-256과 이 MD 전체 bytes를 먼저 대조한다. 자기 해시를 본문에 넣지 않는다.

1. 프로젝트 루트/AGENTS.md, MapDesign/MCP/00_MCP_ENTRYPOINT.md, 설치 RMAP 프로토콜/Apply/Finalize를 읽는다.
2. branch/HEAD/git status, 실제 Current, MASTER/STATUS의 RMAP12/RMAP13 행, Task/Archive bytes를 확인한다.
3. RMAP12 CURRENT이면 그대로 재개한다. 정상 예상 상태는 232 COMPLETE / 1 CURRENT / 7 LOCKED다.
4. RMAP12 COMPLETE면 실제 Finalize/commit/Result 경로 이력을 확인한다. 문서의 PASS만으로 완료 상태를 추정하지 않는다.
5. 완료된 구현의 보완이 필요하면 현지에서 지원하는 수정/재개 절차를 따른다. 임의 상태 문자열 교체나 새 Task 행 추가 금지.
6. 다른 CURRENT 또는 미설명 상태·해시 충돌은 STATUS_CONFLICT로 경로/expected/actual을 보고한다.
7. 무관한 변경은 보존한다. reset/stash/강제 덮어쓰기, 무관한 stage/unstage, git push는 하지 않는다.
8. GENERATED/RMAP01/file_bindings.csv의 RNG/ID/WORLD_DEFINITION/SAVE/MUTATION을 실제 경로로 바인딩한다.
9. 설치 RMAP12 Task의 W08~W12 전체와 기존 RMAP10/11 요청/결과/저장 접점을 필요한 범위만 읽는다.

| 보고서로 확인된 실제 경로 | 우선 확인 책임 |
|---|---|
| Assets/_Game/Map/Runtime/WorldGeneration/WorldData/RmapWorldDataContract.cs | RmapWorldDataGenerator, RmapWorldDefinition, RmapWorldMutationState의 실제 API/호출자 |
| Assets/_Game/Tests/EditMode/Map/RMAP12/RmapWorldDataContractTests.cs | 기존 assertion, 실제 생성 경로와 scope/ID/저장 경계 검증 여부 |
| Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/RmapPatternPool500.cs | 수정 후 최종 pool API/DataVersion/digest 소비 |
| Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/RmapSmallRunHarness.cs | 현재 생성 요청/결과와 WorldData 계약의 연결 여부 |
| MapDesign/MCP/GENERATED/RMAP12/ | 기존 검사·예시 데이터·연결 근거 재사용 |

쓰기 범위는 설치 RMAP12가 소유한 기존 결정성·ID·정적/변경 상태 연결과 직접 영향 코드/검사/Result/상태다.
새 파일/API 이름은 실제 확인 전 기존 구현으로 단정하지 않는다. 별칭별 새 Service 생성을 피한다.
RMAP11 수정 pool은 소비하며, 이번 작업에서 25개 형상이나 Player 점프·Grab 수치를 다시 바꾸지 않는다.

## 3. RMAP12 완료 조건의 실제 확인

### W08 / 같은 입력이 같은 생성 결과로 이어지는가

- Seed+ContentVersion+GeneratorVersion+현재 pool version/digest와 필요한 크기/Recipe 입력을 명시한다.
- 별도 sample 생성기만 재현되는지, 실제 현행 생성 요청/결과가 같은 계약을 소비하는지 호출 경로로 구분한다.
- 연결이 빠졌으면 기존 요청/결과 접점의 최소 adapter로 통합하고 실제 소비 사례 하나를 검증한다.
- 현재 시각/Editor 순서/Player 위치/Unity instance ID를 결정성 입력으로 사용하지 않는다.
- 624×416 실물 맵이나 아직 없는 콘텐츠를 지금 생성할 필요는 없다. 현행 실제 소비 경로의 통합을 확인한다.

### W09 / 7개 RNG Stream과 재선택 격리

- RunGraph/Biome/SpecialReservation/Port/Pattern/Overlay/Population 7종의 명시적 scope/attempt 규칙을 확인한다.
- 이름만 다른 seed 7개를 만드는 것에 더해, 기존 RNG와 실제 소비 영역에 전달되는 경계를 확인한다.
- 같은 입력 재현, 한 stream의 추가 소비/실패 재선택이 다른 stream 결과를 바꾸지 않는 사례를 확인한다.
- 아직 미구현인 소비 영역은 전달 계약과 지연 책임을 명시한다. 미구현 영역의 실제 사용을 꾸며 쓰지 않는다.
- 기존 RNG를 재사용하며 평행한 별도 RNG 구현을 늘리지 않는다.

### W10 / 정적 정의와 Mutation, 저장 경계

- WorldDefinition의 불변성과 mutation의 분리, 동일 static seed 재생성이 mutation을 지우지 않는 경로를 확인한다.
- 현재 Save/Mutation 모델과 serializer 경계를 읽고 실제 ID를 키로 변경 상태가 오가는지 확인한다.
- 새 text serializer의 독립 테스트만 있다면 기존 저장 경계에 필요한 최소 연결을 추가한다.
- 저장→복원→해당 대상 조회/적용 round-trip에서 static digest가 보존되는지 확인한다.
- 디스크 save-slot UI/I/O나 클라우드 저장을 새로 만들 필요는 없다. 이번 확인은 기존 데이터/직렬화 경계의 연결이다.
- 미구현 장치/NPC 동작은 상태 데이터와 슬롯 참조까지만 다룬다.

### W11 / 종류별 ID가 아니라 대상별 Stable ID

- MicroChunk/BreakableTile/Mechanism/RewardChest/SpecialRegionTrigger/MonsterSpawnSlot/NPCShopSlot을 모두 확인한다.
- kind만 달리한 ID 7개는 여러 실제 객체를 구분한다는 증거가 아니다.
- 같은 종류의 서로 다른 위치/owner/slot에 다른 ID, 같은 대상을 재생성하면 같은 ID가 나와야 한다.
- identity key에 실제 안정 좌표/owner/slot을 포함하는 현지 규칙을 명시한다. 전역 순회 순서/Unity instance ID를 쓰지 않는다.
- 생성 순서가 바뀌거나 무관한 슬롯이 추가돼도 기존 대상 ID가 불필요하게 흔들리지 않는지 확인한다.
- 실제 MicroChunk 소비와 나머지 지연 슬롯의 ID/저장 참조를 구분한다. 예시 슬롯을 배치 완료한 콘텐츠로 보고하지 않는다.
- 최소한 같은 종류의 서로 다른 두 대상, 같은 대상 재생성, 다른 종류의 동일 로컬 key 사례를 의미 있게 확인한다.

### W12 / Player와 Camera 분리

- 순수 모델뿐 아니라 실제 생성 호출 경계에서 특정 live Player/Camera를 요구하지 않는지 확인한다.
- 기존 Scene/Runtime 연결 계층의 Player 배치는 유지한다. 새 Player 배치 시스템을 만들 필요는 없다.
- Camera/Input은 로컬 표현이며 생성/저장 상태의 소유자가 아니다. 전역 mutable static run singleton을 도입하지 않는다.

## 4. 필요한 보완·검증·Finalize

1. W08~W12를 실제 경로/메서드/입력/출력/검증 증거 표로 정리한다.
2. 구현이 충분하면 근거를 보완한다. 실제 기능이 빠진 경우 해당 책임만 수정하고 직접 영향 검사만 수행한다.
3. 기존 2개 테스트와 XML을 재사용한다. 구체적인 미검증 동작에만 focused assertion/검사를 보강한다.
4. broad/full/unfiltered regression, legacy 전수 검사, 불필요한 Player build, 모든 Seed 재실행 금지.
5. RMAP11의 25개 수정 완료는 실제 최신 선행 Result/매핑을 소비해 확인한다. 이 보고서의 해시 한 줄을 물리 검증 증거로 확대 해석하지 않는다.
6. 필수 미확인이 남으면 FAIL/BLOCKED와 해당 항목을 기록하고 RMAP13을 열지 않는다.
7. 충분하면 RMAP12 Result에 사용자 관점 변화·파일별 책임·W08~W12 증거·재사용/수정·실제 상태·미실행을 기록한다.
8. Result 내용이 바뀌면 새 SHA를 계산한다. 아래 RMAP13의 선행 SHA로 이전 제출 SHA를 재사용하지 않는다.
9. RMAP12 CURRENT일 때 기존 Finalize로 COMPLETE, Current NONE 처리한다. 이미 정상 완료면 중복 Finalize하지 않는다.
10. 필요한 RMAP12 소유 변경만 commit하고 실제 SHA를 확인한다. 보고서 작성 시점과 commit 증거를 구분한다.
11. 정상 완료 상태는 233 COMPLETE / 0 CURRENT / 7 LOCKED이며 RMAP13은 아직 LOCKED다.
12. 실제 RMAP12 Result SHA, 설치 Task/Archive SHA, 완료 commit을 외부 CLI 보고에 남긴다.

## 5. RMAP13 실행 MD 발행 규칙

RMAP12의 실제 완료가 확인되면 아래 RMAP13 전체 요구를 바탕으로 별도 정상 Task를 발행·실행한다.
본 지시서의 제출 Result SHA를 무조건 복사하지 말고 §4 이후 실제 최종 SHA를 사용한다.

- 현지 single_task_v1 지원 필드/경로를 확인한다. 운영 검사/적용기를 수정하지 않는다.
- task_id/sets_current_task: RMAP13_WORLD_GRAPH
- task_file: TASKS/RMAP13_WORLD_GRAPH.md
- requires_current_task: NONE
- requires_completed_task: RMAP12_WORLD_DATA
- requires_result: REPORTS/RMAP12_WORLD_DATA_RESULT.md / PASS / 실제 최종 SHA
- requires_installed_task: TASKS/RMAP12_WORLD_DATA.md / 실제 Task·Archive 공통 SHA
- 아래 책임을 실제 경로에 바인딩하여 Read/Write Allowlist를 확정하고 총 300줄 이하로 발행한다.
- 발행 위치: MapDesign/MCP_INBOX/RMAP13_WORLD_GRAPH.md
- 완성된 MD 전체 bytes SHA를 외부 보고/현지 지원 등록 경로에 남긴다. 자기 해시를 본문에 넣지 않는다.
- planned 원본의 오래된 SHA와 새 실행 MD를 혼동하지 않는다. 미확인 값/TODO/가짜 SHA를 남기지 않는다.
- 정상 Apply로 RMAP13만 CURRENT로 만든다. 두 Task를 동시에 CURRENT로 만들지 않는다.
- RMAP12에서 현지 Task별 STOP가 요구되면 여기까지 실행 MD와 외부 SHA를 준비해 다음 호출에서 바로 실행할 수 있게 보고한다.

## 6. RMAP13 전체 구현 요구

### Goal / Scope / 실제 바인딩

사용자가 세 핵심 자원을 어떤 순서로 획득해도 제작·봉인지·보스·출구로 이어지는 방향 관계를 확인할 수 있게 한다.
RMAP13은 W01/W02/W07의 위치·상태·방향 의존성, 지름길 정책, 후속 공간 예약 입력을 소유한다.
보스 AI/전투, 인장 제작 UI, 경제, 바이옴 배치, 624×416 Tilemap 조립·실제 플레이 완성을 주장하지 않는다.

- Read: RMAP01의 WORLD_GRAPH/COMPLETION/REGION_SLOT 실제 바인딩, 기존 CompletionSearch와 직접 호출자/검사.
- Read: RMAP12의 검증된 ID/정의/RNG/Mutation, 공유 이동 profile, 필요한 RMAP08 Port/RMAP09 조립 계약.
- Write: 기존 진행 그래프·상태 탐색의 위치/방향 관계, 후속 공간 예약 요청, 진행 표시/내보내기.
- Write: 직접 관련 focused tests, MapDesign/MCP/GENERATED/RMAP13 자료, Task/Archive/Result와 정상 상태 기록.
- 실제 경로/API/KEEP·ADAPT·NEW 판단을 실행 MD에 기록한다. 기존 CompletionSearch를 먼저 활용한다.
- RMAP12 데이터/RunGraph stream을 소비한다. 별도 RNG/ID/WorldDefinition이나 전역 mutable run singleton을 만들지 않는다.
- 생성기는 live Player/Camera를 참조하지 않는다. 그래프 데이터와 현재 플레이 진행 상태를 분리한다.

### W01 / 필수 진행 목표 (원문 §23)

세 핵심 자원은 월핵 원석, 응축 계수수액, 심층 별누룩이다.
세 자원을 자유 순서로 획득한 뒤 인장 제작→보스 봉인지 개방→보스→최종 출구 순서를 지킨다.

1. Start/세 자원/Forge/봉인지/보스/Exit 역할과 각 실제 위치 또는 예약 슬롯의 stable ID 관계를 표현한다.
2. 봉인지가 기존 모델에서 노드인지 간선 조건인지 확인해 재사용한다. 개념마다 새 Scene Object/클래스를 만들지 않는다.
3. 진행 상태는 현재 위치, 세 자원 획득 상태, 인장 제작 여부, 봉인지 개방, 보스 완료 여부를 구분한다.
4. 자원은 해당 위치/기존 상호작용 조건을 만족할 때만 획득한다. 테스트 편의로 다른 위치에서 상태를 켜지 않는다.
5. Forge의 제작 조건은 세 자원 획득이며, 인장 제작 전 봉인지 개방·보스 진행·최종 출구 성공을 허용하지 않는다.
6. 자원 소비/중복 획득/중복 제작은 기존 의미를 확인해 명시한다. 자원 상태만 초기화해 방문 순서 증거를 잃지 않는다.
7. 보스 완료는 후속 전투가 전달할 명시적 조건/이벤트로 분리한다. 탐색에서의 완료 가정과 실제 전투 완료를 구분한다.
8. 최종 성공은 올바른 진행 조건을 만족한 상태로 Exit에 도착한 것이다. Exit 노드 존재만으로 완료하지 않는다.

### W02 / 방향성 Completion Graph (원문 §20, §23)

필수 검증은 방향성 이동 간선과 위치/자원/제작/보스 상태를 입력으로 탐색한다.
BFS를 사용할 수 있으나 무방향 연결성 또는 열린 셀 BFS만으로 플랫폼 진행 완료를 판정하지 않는다.

1. 공간 이동 간선과 자원 획득/제작/봉인/보스 상태 전이를 구분하고 각 전이의 근거를 보존한다.
2. L/R/U/D 공간 연결과 실제 이동 방향/조건을 가진 간선을 사용한다. 역방향 간선을 자동 추가하지 않는다.
3. 각 간선에는 source/target ID, 방향, 필요한 상태/TraversalKind, 원본 연결 또는 후속 공간 요구 근거를 남긴다.
4. 전역 그래프 단계의 계획 간선은 후속 Port/공간 배치에서 만족시켜야 할 요구다. 물리 검증 완료 간선으로 표시하지 않는다.
5. 기존 실제 간선을 소비할 때는 그 검증 수준을 보존한다. 로컬 태그/역할명만으로 Required_OK를 붙이지 않는다.
6. 탐색 visited key에 위치뿐 아니라 진행 상태를 포함한다. 자원을 얻고 같은 장소로 돌아오는 상태를 잘못 제거하지 않는다.
7. 3개 자원의 6개 순서 각각에서 실제 방향 이동·상태 전이 증거 경로를 출력한다.
8. 검사 중 목표 순서와 다르게 자원을 미리 획득한 경로를 그 순서의 성공으로 세지 않는다.
9. 모든 자원 획득 후 Forge/Boss/Exit로 이어지는 경로를 검증한다. 성공하는 한 가지 순서만 보고 자유 순서를 주장하지 않는다.
10. 막힌 순서에는 실패 위치/진행 상태/간선 조건/부족한 공간 예약 요구를 출력한다.
11. 검증을 통과시키려고 원인 기록 없이 edge를 추가하거나 one-way를 양방향으로 바꾸지 않는다.
12. 설계 단계의 제한 재선택이 필요하면 실패 노드/요구/attempt를 기록하고 같은 입력으로 재현되게 한다.

### W07 / 자원 복귀와 지름길 정책 (원문 §42)

핵심 자원 완료 뒤 복귀 지름길을 필수로 할지는 미정이므로 설정으로 분리한다.
지름길 유무와 별개로 6개 자유 순서의 필수 연결은 유지한다. 모든 선택 가지의 왕복 가능성을 전역 강제하지 않는다.

- 기존 설정 접점에 ‘자원 복귀 지름길 필수 여부’를 둔다. 이름과 직렬화 형식은 현지 모델에 맞춘다.
- 임시 기본값은 ‘지름길 필수 아님’으로 둘 수 있으며, 채택한 값/버전/이유를 기록한다. 사용자 최종 확정이라고 쓰지 않는다.
- ‘필수 아님’도 Forge 이후 필수 진행에 도달할 일반 경로는 필요하다. 복귀 불가능을 허용하는 설정으로 해석하지 않는다.
- ‘필수’에서는 어떤 자원 획득 상태로 언제 열리고 어디로 이어지는지 명시한 방향 간선/예약 요청이 있어야 한다.
- 임의 지름길 빈도/지형 자동 굴착을 확정하지 않는다. 미구현 장치 동작은 후속 배치가 충족할 의존성으로 출력한다.
- 두 설정 모두 6개 자원 순서가 유지되는지 확인한다. 선택 가지 전체의 왕복을 새 전역 규칙으로 강제하지 않는다.

### 후속 공간 예약과 확인 가능한 출력

- 필수 역할/노드 stable ID→RegionSlot/예약 요청을 일대일 또는 명시적 다대일 관계로 연결한다.
- 기존 슬롯/좌표 모델을 먼저 재사용한다. 위치가 미정이면 미정 상태와 배치 제약을 기록하고 가짜 확정 좌표를 만들지 않는다.
- 후속 배치가 만족시킬 입출 방향/필수 접근/해제 조건/참조 ID를 내보낸다. 숫자 요약만으로 관계를 잃지 않는다.
- 노드/간선/상태 조건/6개 증거 경로/지름길 정책/예약 입력을 기존 Editor 표시 또는 읽을 수 있는 자료로 제공한다.
- CSV를 쓴다면 stable 정렬, 다중 값 escaping, ID 참조와 round-trip을 확인한다.
- 같은 Seed/버전/정책이면 같은 노드·간선·예약 ID·digest를 생성한다. 현재 시각/Editor 순서를 사용하지 않는다.
- 자료에는 논리 진행 검증과 실제 공간/Player 검증 수준을 구분한다. 이 단계에서 전체 월드 통과를 주장하지 않는다.

### RMAP13 Focused Checks / PASS

1. 실제 생성 경로의 대표 그래프에서 6개 획득 순서와 Forge→봉인지→Boss 완료 조건→Exit의 증거 경로.
2. 자원 부족 상태 제작, 인장 없이 봉인지 개방, 보스 미완료 Exit 성공이 거부되는 구체 사례.
3. one-way 방향 오류 또는 필수 복귀 단절이 있는 그래프의 실패 탐지와 원인 위치/상태 보고.
4. 자원 획득 전후 같은 위치를 다른 상태로 탐색하며, 순회 순서가 visited 오류로 경로를 지우지 않는지 확인.
5. 지름길 필수/비필수 두 정책에서도 자유 순서·필수 진행 조건 유지.
6. 생성 노드/간선/예약 참조 유효성, stable ID/RunGraph stream 결정성, 출력과 실제 API 일치.
7. 기존 CompletionSearch 및 직접 영향 검사만 실행한다. fixture를 구체적인 위험에 맞추고 테스트 수를 목표로 삼지 않는다.
8. compile/refresh와 사용자에게 보여줄 실제 그래프 자료 생성을 확인한다. 생성하지 않은 Scene/이미지를 결과에 쓰지 않는다.

### RMAP13 실패 처리 / Result / Commit / STOP

- 실제 상태·해시 충돌은 STATUS_CONFLICT, 필수 자료/도구 부재는 BLOCKED, 기능/조건 불충족은 FAIL이다.
- 기존 설계와의 차이는 KEEP/ADAPT/NEW 이유로 설명한다. 자동 터널/침묵 수리/검증 완화/대규모 리팩터링 금지.
- 이번 Result 경로: MapDesign/MCP/REPORTS/RMAP13_WORLD_GRAPH_RESULT.md
- Result 앞부분: 사용자 관점으로 새로 확인 가능한 일, 아직 미구현인 공간 배치/전투/Player 통과를 명시한다.
- 파일별 실제 경로/추가·수정/책임/소유하지 않는 책임, preflight/해시/실제 적용 전후 상태를 기록한다.
- W01/W02/W07 각각 실제 API·출력·검증·판정, 6개 경로, 실패 사례, 지름길 정책과 예약 입력을 연결한다.
- 실제 filter/job/count/XML, 재사용 증거, 새 검사 이유, 미실행/실패/수리 내용을 보고한다.
- RMAP14 바인딩으로 그래프/RegionSlot/예약 요청의 실제 경로·ID·버전·미충족 배치 요구를 남긴다.
- 필수 확인을 마친 경우에만 PASS Result와 기존 Finalize를 수행한다. RMAP13 소유 변경만 commit한다.
- 예상 적용 후 233 COMPLETE / 1 CURRENT / 6 LOCKED, 완료 후 234 COMPLETE / 0 CURRENT / 6 LOCKED.
- 실제 상태가 다르면 이 숫자에 억지로 맞추지 않는다. 현지 이력과 현재 상태를 보고한다.
- 실제 commit SHA, Result SHA, 설치 Task/Archive SHA와 bytes 동일성, Current NONE을 최종 CLI에 남긴다.
- RMAP14_BIOMES는 LOCKED / NOT STARTED로 유지하고 git push 없이 STOP한다.
