```yaml
mcp_patch:
  format: single_task_v1
  task_id: RMAP08_PORTS
  task_file: TASKS/RMAP08_PORTS.md
  requires_current_task: NONE
  requires_completed_task: RMAP07_PATTERNS
  requires_result:
    path: REPORTS/RMAP07_PATTERNS_RESULT.md
    status: PASS
    sha256: c25de6ab55df577bc8587ecccdc610b1bef309cfd24fd6f5fb6483a66172c61e
  requires_installed_task:
    path: TASKS/RMAP07_PATTERNS.md
    sha256: b976a386c798d7cd5052f46a0c65e858eaf97ccbd23981874298c1cc58d16d3b
  sets_current_task: RMAP08_PORTS
```

# RMAP08_PORTS - 청크 Type·Port와 내부 방향 연결

```text
TASK: RMAP08_PORTS
DOCUMENT: v4.2 / RMAP07 PASS 이후 실행 지시서 / 2026-09-08
STATUS: CURRENT
INPUT: MapDesign/MCP_INBOX/RMAP08_PORTS.md
EXPECTED_RESULT: MapDesign/MCP/REPORTS/RMAP08_PORTS_RESULT.md
NEXT: RMAP09_COMPOSER
NEXT STATUS: LOCKED / DO NOT START
```

위 CURRENT는 정상 Apply 이후의 상태다. 문서 발행만으로 저장소 상태를 바꾸지 않는다.
약 1~2시간의 기능 책임 단위이며 문서는 300줄 이하로 운영한다.

## 1. User-Facing Goal / 이번 작업의 완료 모습

Unity에서 12x8 청크의 Type 0~4 연결 형태, 실제 입구 좌표 전체, 진입/이탈 방향과 내부 이동 관계를 볼 수 있다.
폭이 다른 입구, 한 셀만 겹치는 이웃, 일방 낙하, 개방형/봉인형 Type 0을 구분하고 대표 사례를 실제 Player로 확인한다.
청크 공간 상태, 외부 입구 형태, 물리 이동 가능성은 각각 별도 자료이며 한 숫자나 태그로 대신하지 않는다.
이번 요구 ID는 A05, A16, A17, A18, A19, A20이다. 아래에 전체 구현 조건을 포함한다.
RMAP07의 48개 패턴/특성 API를 재사용하되 자동 3x2 조립, 최종 500개 보강과 월드 생성은 시작하지 않는다.

## 2. Preflight / 선행 완료와 정상 single_task_v1 적용

1. 프로젝트 루트/적용 AGENTS.md와 MapDesign/MCP/00_MCP_ENTRYPOINT.md를 확인한다.
2. 기존 Apply/ChangeControl/Finalize와 RMAP/02_PROTOCOL_V4_2.md에서 지원 필드/경로를 확인한다.
3. YAML 내부 경로는 MapDesign/MCP 기준, 본문 Assets/MapDesign 경로는 프로젝트 루트 기준이다.
4. 전달 메시지의 외부 SHA-256을 inbox 파일 전체 원본 bytes의 SHA와 대조한다.
   파일 자체의 expected SHA는 자기참조를 피하려고 본문에 넣지 않는다. 이전 planned 기획 문서의 해시와 비교하지 않는다.
   외부 값 부재/불일치 또는 현지 별도 manifest/등록 SHA 충돌이면 적용 전에 경로와 expected/actual을 보고하고 중단한다.
   줄바꿈 정규화/재저장으로 해시를 맞추거나 적용기 규칙을 수정하지 않는다.
5. RMAP07 installed Task/Archive의 bytes/SHA와 PASS Result의 TASK/STATUS/SHA를 YAML과 대조한다.
6. RMAP07 Result는 Phase C Finalize/Phase D commit 전에 작성됐다. 그 문구를 실제 완료 증거로 간주하지 않는다.
   저장소에서 RMAP07 COMPLETE, Current NONE, RMAP08~19 LOCKED 및 Result 경로의 git log로 실제 RMAP07 commit을 확인한다.
   선행 Finalize/commit 미완료면 남은 단계와 근거를 보고하고 STOP한다. 이번 작업에서 선행 상태/Result를 임의 수정하지 않는다.
7. 정상 시작 기준은 240행 = 228 COMPLETE / 0 CURRENT / 12 LOCKED다. 차이가 있으면 실제 이력과 현지 프로토콜로 판단한다.
8. 미적용 inbox 후보 본 MD 1개, RMAP08 등록 ID와 다른 CURRENT 부재를 확인한다. 선행 변경/Task-Archive 불일치면 중단한다.
9. 정상 Apply로 RMAP08만 LOCKED->CURRENT, Current NONE->RMAP08_PORTS를 수행하고 Task/Archive를 바이트 동일하게 설치한다.
10. 같은 RMAP08 CURRENT 재개는 설치 Task/Archive/입력 동일성과 기존 결과를 확인하고 현지 재개 규칙을 따른다.
11. 이미 RMAP08 COMPLETE+유효 PASS면 기존 결과를 보고하고 STOP한다. RMAP09를 자동으로 열지 않는다.
12. 무관한 변경은 보존한다. 관련 변경을 분리할 수 없으면 근거를 보고하며 reset/stash/강제 덮어쓰기를 하지 않는다.

## 3. Read Allowlist / RMAP07 데이터와 실제 이동 연결점

- MapDesign/MCP/RMAP/{00_BASELINE_V4_2,01_SEQUENCE_V4_2,02_PROTOCOL_V4_2}.md 및 현지 운영 문서.
- MapDesign/MCP/{MASTER_IMPLEMENTATION_TASK_LIST,06_IMPLEMENTATION_STATUS}.md와 RMAP07 Task/Archive/Result.
- MapDesign/MCP/GENERATED/RMAP01/file_bindings.csv의 CHUNK / PORT / PROFILE / GRAPH 행 및 해당 실제 코드/직접 호출자/테스트.
- RMAP07 catalog, first-pool/relations/characteristics/selection/placement/manifest 중 이번 연결 판정에 필요한 자료.
- 기존 GeneratedTraversalProfile / GeneratedTileMovementGraphBuilder 및 Player Collider/이동 profile의 실제 바인딩.
- RMAP02~06의 실제 run/jump/Grab/climb/one-way/fall 입력 의미와 직접 필요한 증거. 과거 전체 결과를 재감사하지 않는다.

| 실제 연결점 | 상태/확인 근거 | 이번 소비 경계 |
|---|---|---|
| Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/RmapPatternCatalog.cs | EXISTING: RMAP07 | BuildInitialPool(), snapshot.TryGetCandidate()로 안정 Candidate ID 조회 |
| RmapPatternCandidate.BaseCells / PrimaryRole / Origins / Characteristics | EXISTING: RMAP07 | 4x4 Base Geometry/출처/특성을 읽고 Port로 자동 간주하지 않음 |
| RmapPatternAutomaticCharacteristics.EdgeOpenCellSets / ContextRequired / SymmetricTransforms | EXISTING: RMAP07 | 로컬 경계 증거와 문맥 필요성을 보존 |
| RmapPatternCatalog.TransformCells(...) | EXISTING: RMAP07 | 변환 완료 셀 좌표를 소비. 원본 ID/방향 의미 보존 |
| Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RMAP07/*.csv | DERIVED: RMAP07 | 조회용 파생 자료. 독립 원본처럼 직접 수정하지 않음 |
| Assets/_Game/Live/Editor/RMAP07/CharacterLivePatternGallerySceneBuilder.cs | EXISTING: RMAP07 | 실제 TilemapCollider2D/Player/one-way fixture 구성 방식 참조 |
| Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/RmapPortCatalog.cs | PROPOSED: RMAP07 | 기존 CHUNK/PORT 모델로 부족한 계약만 최소 추가 |
| Assets/_Game/Map/Scenes/MoonPalace/RMAP08/MoonPalacePortLab_RMAP08.unity | PROPOSED | Type/좌표/방향/내부 연결 표시와 실제 Player 확인 |

별칭/PROPOSED 이름을 기존 구현으로 단정하지 않는다. 현지 실제 경로/API를 읽고 KEEP/ADAPT/NEW와 이유를 Result에 남긴다.
누락이면 해당 디렉터리와 직접 참조만 좁게 찾아 바인딩한다. 기존 공개 API/Seed/버전/ID 계약을 보존하고 Task별 새 Service를 만들지 않는다.
RMAP07의 UnresolvedDirectionalTags와 review-required 출처 충돌은 그대로 남긴다. 의도 태그를 Required 또는 통과 증명으로 자동 승격하지 않는다.
긴 파일은 rg로 심볼을 찾고 필요한 구간만 읽는다. 정적 Port 데이터가 Live Player/Camera 인스턴스를 참조하게 하지 않는다.

## 4. Write Allowlist / 변경 경계

- 위 바인딩에서 확인한 기존 Type/공간 상태/Port 모델, 인접 연결 분류, 청크 내부 방향 연결 자료와 직접 소비부의 최소 변경.
- 필요성이 확인된 RmapPortCatalog 또는 동등 보조 타입. 기존 profile/local movement graph API를 재사용한다.
- RMAP08의 고정 12x8 청크 fixture와 연결 사례/CSV/조회 API. 기존 authoring 체계를 우선하고 원본/파생 경로를 명시한다.
- RMAP08 전용 저장 scene/builder/라벨/방향 표시/manifest 및 필요한 Tile/prefab/.meta.
- 기존 assembly 내 focused 계약/좌표/방향/clearance 테스트와 대표 실제 Player/Tilemap PlayMode 확인.
- MapDesign/MCP/GENERATED/RMAP08의 데이터 snapshot, 연결 판정 근거, profile 값, fixture manifest와 실행 증거.
- 지정 Task/Archive/Result 및 기존 프로토콜의 RMAP08 상태 기록. asmdef는 컴파일에 필요한 최소 참조만 수정한다.

이번 fixture는 수동 확정한 소규모 청크다. 자동 3x2 조립/후보 검색/보호 공간 배치/전역 경로 탐색기를 새로 만들지 않는다.
원본과 Generated 복제본을 독립 편집하는 두 권위로 만들지 않는다. RMAP07 후보 셀/ID/첫 풀 및 기존 CSV 원본을 임의 재선정하지 않는다.
기존 Player 이동/Collider/fall/look 수치를 포트에 맞춰 재튜닝하지 않는다. package/운영 프로토콜/과거 Result 수정은 범위 밖이다.

## 5. A05 / 공간 상태와 Type 분리

- ACTIVE / SECRET / INACTIVE_SOLID / SPECIAL_RESERVED를 기존 필드/enum에 명시적으로 연결한다.
- Type 숫자는 외부 연결 형태다. SECRET 여부를 Type 0 또는 일반 입구 수만으로 자동 결정하지 않는다.
- 열린 Type 0은 ACTIVE가 될 수 있다. 공간 상태와 Type/Type 0 형태를 별도 값으로 직렬화/조회한다.
- INACTIVE_SOLID는 내부 플레이 공간이 없는 고체 영역이다. 내부 공간을 가진 봉인형 Type 0과 구분하고 가짜 입구를 만들지 않는다.
- SPECIAL_RESERVED는 일반 지형보다 먼저 예약한다. 기존 예약 계약에서 우선순위/충돌을 표현하고 fixture에서 일반 지형이 예약을 덮지 않음을 확인한다.
- 이 우선순위 확인을 위해 월드 예약 생성기를 구현하지 않는다. 실제 SPECIAL 콘텐츠 배치는 후속 소유 작업에 남긴다.

## 6. A16 / Type 1~4 연결 표

연결 집합은 일반 EdgePort가 존재하는 Side 집합이다. 아래 조합만 허용하고 누락/금지 방향을 명시적 오류로 반환한다.

| Type | 허용 Side 집합 | 필수 | 금지 |
|---|---|---|---|
| 1 | LR | L,R | U,D |
| 2 | LD / RD / LRD | D + L/R 최소 하나 | U |
| 3 | LU / RU / LRU | U + L/R 최소 하나 | D |
| 4 | UD / LUD / RUD / LRUD | U,D | 없음 |

Type 4는 U와 D가 모두 필수다. 좌우는 선택이며 상하 중 하나가 없으면 Type 4로 통과시키지 않는다.
Type은 기믹/난도/보상/바이옴 또는 이동 방향을 뜻하지 않는다. 형태가 맞아도 실제 이동 간선 검증은 별도로 수행한다.
한 Side에 여러 독립 입구가 있어도 Type 1~4의 Side 집합과 실제 입구 수/ID를 각각 보존한다.

## 7. A17 / Type 0의 봉인형과 개방형

- Type 0은 일반 입구가 0개 또는 1개인 청크다. 기존 데이터 필드로 두 형태를 구분하고 새 Type 번호를 늘리지 않는다.
- 0개는 봉인형이다. 내부 공간과 BreakableAccess 최소 하나를 기록한다. 전부 고체인 INACTIVE_SOLID를 봉인형으로 오인하지 않는다.
- BreakableAccess는 일반 EdgePort 수에 포함하지 않는다. 접근 조건/좌표/근거를 기록하되 미구현 파괴·도구 사용을 통과 증거로 꾸미지 않는다.
- 1개는 개방형 막다른 공간이다. ACTIVE도 가능하며 도구 요구/필수 아이템 금지를 Type 0 전체에 일괄 적용하지 않는다.
- 입구 하나는 독립된 연결부 하나다. 폭 3타일 입구는 Port ID 하나와 OpenCells 3개다.
- 같은 Edge의 분리된 두 입구는 둘이다. Side 하나라는 이유로 합쳐 Type 0으로 분류하지 않는다.
- 동일 입구의 WALK/JUMP 등 이동 선택지가 여러 개여도 물리 입구 수를 부풀리지 않는다. 입구 ID와 traversal 선택지를 구분한다.
- 독립 입구의 그룹화/ID 규칙을 기존 authoring과 정합되게 명시한다. 중심점/전체 폭으로 합쳐 중간 고체 틈을 지우지 않는다.
- 개방형 입구의 실제 접근/복귀 방향을 A19/A20으로 기록한다. 입구 하나라는 이유로 왕복이나 도구 필요 여부를 추정하지 않는다.

## 8. A18 / EdgePort 전체 필드와 좌표

아래 필드를 기존 모델/CSV/API에서 모두 보존한다. 내부 참조에는 청크 ID와 안정 Port ID를 사용한다.

| 필드 | 값/의미 |
|---|---|
| Side | L / R / U / D |
| OpenCells | 해당 입구가 차지하는 Edge의 열린 좌표 전체. 집합의 모든 셀을 보존 |
| TraversalKind | WALK / JUMP / DROP / CLIMB / HANG / ONE_WAY |
| FlowDirection | IN / OUT / BOTH. 해당 Port를 소유한 청크 기준 |
| Required | bool. 연결 요구이며 검증 성공 여부와 다른 값 |

- 청크는 12x8이다. 좌하단 원점, x 우향/y 상향이며 L/R의 OpenCells는 y=0..7, U/D는 x=0..11이다.
- RMAP07의 4x4 로컬 Edge 좌표를 청크 좌표로 그대로 쓰지 않는다. 패턴을 배치했다면 offset을 적용하고 원본 Candidate ID/위치를 추적한다.
- 표시/인접 판정은 청크 origin과 Side를 반영해 실제 월드 경계 위치로 변환한다. U/D와 L/R 좌표축을 혼동하지 않는다.
- 중복 좌표는 집합으로 정규화하되 누락 셀을 채우지 않는다. 범위 밖/빈 일반 Port/알 수 없는 enum/중복 ID/미해결 참조는 명시적 오류다.
- 중심점, 폭 하나 또는 min/max 구간으로만 저장하지 않는다. 비연속 OpenCells의 틈은 그대로 유지한다.
- ONE_WAY 위치가 변환돼도 충돌면은 월드 위쪽이다. FlowDirection과 콜라이더 방향은 같은 개념이 아니다.

## 9. A19 / 인접 Port와 내부 입구->출구 연결

- 인접 후보는 실제로 접한 반대 Side(L-R, U-D)만 비교한다. 좌표계를 맞춘 OpenCells 교집합이 비어 있지 않으면 1차 기하 후보가 된다.
- 2칸 이상 입구도 열린 좌표 전체를 보존한다. 한 셀 교집합은 후보가 될 수 있지만 곧바로 확정 이동 간선이 되지 않는다.
- PROFILE에서 실제 Collider 크기/피벗/여유, 이동 종류별 조건, jump/climb/Grab/one-way/fall 의미를 읽고 값/버전을 근거에 기록한다.
- 후보의 실제 Base/Overlay 배치, 머리 공간, 진행 중 Collider 여유, 이탈/착지 면과 traversal 지원을 확인한다.
- 출발 Port의 OUT 또는 BOTH와 도착 Port의 IN 또는 BOTH가 맞아야 A->B를 허용할 수 있다. 역방향은 별도로 판정한다.
- WALK/JUMP/DROP/CLIMB/HANG/ONE_WAY의 호환 조건은 기존 profile와 구체 지형으로 정해 표로 기록한다. 종류 이름 일치만으로 통과시키지 않는다.
- 청크 내부는 FromPortId -> ToPortId 방향 관계를 별도로 기록한다. 외부 포트가 여러 개라는 이유로 내부를 완전 연결하지 않는다.
- 내부 관계에 traversal/조건/profile/근거를 연결한다. 고체 벽으로 나뉜 공기 공간, 착지면 부재, 등반 Overlay 부재를 성공으로 처리하지 않는다.
- 외부 A->B 경계 통과 증거와 B 내부 입구->출구 증거를 별도로 조회할 수 있어야 한다. 쌍방향 관계는 각각의 방향을 기록한다.
- 판정은 최소한 기하 후보 / 이동 검증 성공 / 실패 / 미확인을 구분한다. 기존 enum이 있으면 매핑하고 이유를 남긴다.
- ContextRequired나 필요한 profile/Overlay가 미확정이면 미확인이다. bool 성공으로 기본값 처리하거나 실패와 혼동하지 않는다.
- 통과 증거를 지정된 Geometry/배치/방향/profile에 결부한다. 입력 조건이 바뀌면 이전 증거를 그대로 재사용하지 않는다.
- OpenCells 겹침, 공기 BFS, RMAP07 의도 태그만으로 실제 통과를 선언하지 않는다. 전체 월드 도달성 검증은 후속 작업 책임이다.

## 10. A20 / 일방통행 Port와 Type 2·3 방향 프로필

- 접한 청크를 자동 왕복으로 연결하지 않는다. IN/OUT/BOTH는 소유 청크 기준이며 실제 외부/내부 간선과 일치해야 한다.
- Type 2 낙하는 일방통행일 수 있다. 측면 진입->아래 이탈을 구현한 fixture에서는 역방향 상승 증거 없이 아래->측면을 생성하지 않는다.
- Type 3 상승도 전역 왕복 보장을 강제하지 않는다. 위쪽 도달 수단과 진입/이탈 방향을 해당 지형/profile에서 확인한다.
- Type 2/3 기본 방향 프로필을 기록하되 어떤 지형/이동 조건에 적용되는지 명시한다. Type 숫자만 보고 고정 방향을 부여하지 않는다.
- 동일 Type의 다른 지형/명시 방향이 다른 결과를 낼 수 있도록 모델링하고 사례로 확인한다.
- Required=true 포트/연결에 필수 이동 실패가 확인되면 그 fixture의 요구 충족은 실패다. 불가능한 역방향은 명시적 음성 사례로 남길 수 있다.
- Required=false라고 실패/미확인 상태를 숨기지 않는다. 새 파괴/도구/전투/부활 시스템을 만들어 연결을 성립시키지 않는다.

## 11. 데이터/API 산출물

기존 원본 체계와 CSV 규칙을 재사용한다. 새 파일이 필요하면 RMAP08 범위에 한정하고 경로/권위/재생성 방법을 Result에 적는다.

| 자료 | 필수 내용 |
|---|---|
| 청크 정의 | ChunkId, 12x8 Geometry/참조, SpaceState, Type, Type 0 형태, 일반 입구 ID, BreakableAccess, reservation 정보 |
| EdgePort 목록 | ChunkId/PortId와 A18 전체 필드, 독립 입구 grouping/좌표 규칙 |
| 인접 연결 판정 | 양쪽 ChunkId/PortId, 실제 경계 좌표/교집합, 방향, profile/traversal, 판정 단계와 근거 |
| 내부 연결 목록 | ChunkId, FromPortId, ToPortId, traversal/조건/profile, 방향별 판정과 근거 |
| 방향 프로필 | Type 2/3 대표 지형, 기본 흐름, 적용 조건, 역방향 결과 |
| fixture manifest | scene/bounds, 청크 원점/배치, Player 시작점, 포트/내부 관계 표시, 검증 사례 위치 |

UTF-8 CSV의 다중 좌표/참조 escaping과 정렬을 명시하고 round-trip한다. 같은 입력은 같은 ID/Port/연결 결과를 만든다.
자료를 실제 조회/판정 API에 연결한다. 결과 파일만 내보내거나 미검증 간선을 성공으로 고정한 데모로 끝내지 않는다.

## 12. Unity Port Lab / 대표 사례

전용 저장 씬의 전체 가로/세로를 각각 12와 8의 배수로 구성하고 청크별 12x8 경계와 origin을 표시한다.
Type/공간 상태/Port ID/열린 좌표/IN·OUT 방향, 내부 From->To와 검증 상태를 구분해서 볼 수 있게 한다.
고정 fixture만 사용한다. 표시선/화살표는 설명용이며 실제 Collider/입력 증거를 대체하지 않는다.

| 사례 | 확인할 내용 |
|---|---|
| Type 1~4 대표 | 정확한 허용 Side 집합, 실제 입구 위치. Type 4의 U,D 필수와 좌우 선택 |
| Type 0 개방/봉인 + INACTIVE_SOLID | 폭 3칸 입구=1개, 내부 공간+BreakableAccess, 내부 공간 없는 고체의 구분 |
| 서로 다른 입구 폭 | 좌표 전체와 실제 교집합을 표시하며 중심/폭만으로 계산하지 않음 |
| 한 셀 교집합 | profile상 유효한 통과 사례와 머리 공간/착지 조건 때문에 실패하는 사례를 구분 |
| 외부는 열렸지만 내부 분리 | 입구/출구가 고체로 분리되어 내부 간선이 생기지 않는 음성 사례 |
| Type 2 일방 낙하 / Type 3 상승 | 실제 Player로 의도 방향을 확인하고 역방향 결과/조건을 별도 기록 |
| 같은 Edge의 독립된 입구 2개 | 하나로 합쳐 Type 0으로 분류하지 않음 |

대표 유효 통과와 일방 이동은 실제 Player/Tilemap/Collider와 기존 Input System 경로로 확인한다.
RMAP07의 물리 Tilemap 표시와 RMAP04 one-way/등반 배선을 재사용한다. Player가 지나가도록 후보 셀을 자동 굴착하지 않는다.
씬 경로, 조작법, 사례 찾는 위치, 미확인 조건을 사용자 안내와 Result에 적는다.

## 13. 구현 순서와 Focused Checks

1. 선행 완료/commit/SHA와 정상 Apply를 확인하고 CHUNK/PORT/PROFILE/GRAPH 실제 바인딩/재사용 경계를 확정한다.
2. 공간 상태/Type 표/Type 0 독립 입구 규칙과 A18 전 필드를 기존 데이터에 연결한다.
3. 좌표 기반 인접 후보와 profile/traversal/방향 검증, 별도 내부 입구->출구 관계를 구현한다.
4. Type 2/3 방향 프로필과 고정 fixture/CSV/API를 연결하고 대표 사례를 실제 scene에 표시한다.
5. 아래 focused 검사를 실행하고 재현된 실패만 최소 수정한 뒤 Result/Finalize/commit한다.

- EditMode: Type 표의 모든 허용 조합과 누락/금지 방향, Type 4 U/D 누락, 공간 상태 독립성/예약 우선순위.
- EditMode: Type 0 폭 3칸 입구 하나/분리된 둘, 봉인형 내부 공간+BreakableAccess, INACTIVE_SOLID와 구분.
- EditMode: A18 모든 필드/범위, L/R y와 U/D x 좌표, 4x4->12x8 offset, 비연속 OpenCells/서로 다른 폭/한 셀 교집합.
- EditMode: IN/OUT/BOTH 방향 조합, traversal/profile/clearance 조건, 내부 단절, 후보/성공/실패/미확인 구분.
- EditMode: Type 2/3 지형별 방향 프로필, 자동 역방향 간선 없음, CSV round-trip/참조 오류/동일 입력 안정성.
- PlayMode: 실제 Player의 대표 경계 통과, 한 셀 겹침의 유효/차단 지형, Type 2 낙하와 Type 3 상승의 의도 방향.
- 판정 실패가 기대되는 음성 사례는 그 실패 감지가 테스트의 성공 조건이다. 이를 필수 경로 통과 PASS와 혼동하지 않는다.
- Compile/refresh와 씬 생성은 수행한다. 직접 영향 회귀만 최소 범위로 정하고 이유를 적는다.
- broad/full/unfiltered regression, legacy 19347, 불필요한 Player build와 다음 작업 구현은 실행하지 않는다.
- 테스트 수를 목표로 늘리지 않는다. 초기 배치 외 teleport/검사 marker로 실제 입력/물리 동작을 대신하지 않는다.

Unity 미실행/도구 부재는 NOT RUN 또는 BLOCKED다. 이전 PASS를 새 포트/내부 연결의 실제 물리 증거로 대체하지 않는다.

## 14. Required Result / 완료 보고

```text
TASK: RMAP08_PORTS
STATUS: PASS 또는 FAIL 또는 BLOCKED 또는 STATUS_CONFLICT
USER-FACING IMPLEMENTATION REPORT: Type/포트/방향으로 확인 가능한 일, scene/조작/CSV 경로, 아직 없는 자동 조립
RESPONSIBILITY AND FILES: 실제 경로 | 추가/수정 | 책임 | 소유하지 않는 책임
PRECONDITIONS: HEAD/branch, 외부 expected/inbox 실제 SHA, 선행 Task/Archive/Result SHA와 실제 commit, 적용 전후 상태
CHANGED: 별칭 바인딩과 KEEP/ADAPT/NEW 이유, 원본/파생 경로, 입구 grouping/좌표/방향/profile 규칙
REQUIREMENT EVIDENCE: A05/A16/A17/A18/A19/A20 각각 산출물, 검사/관찰, 판정
PORT EVIDENCE: Type 표/Type 0 사례, 전체 OpenCells, 외부/내부 방향 관계, profile 값, 기하 후보/성공/실패/미확인 근거
VALIDATION: 실제 filter/job/count, CSV 확인, Play 증거, 실패/최소 수정, 미실행과 이유
UNITY VISIBLE OUTPUT: scene/bounds/fixture 위치, 실제 Player/Tilemap/포트 표시와 이동 결과
OUT-OF-SCOPE FINDINGS: 자동 3x2 조립/보호 공간/후보 재선택/500 보강/전역 이동 검증의 남은 책임
RMAP09 BINDINGS: Type/공간 상태/Port/내부 연결/판정 조회와 pattern catalog의 실제 경로/API, EXISTING/PROPOSED 구분
FOLLOWUP: 현지 single_task_v1 경로/필드 규칙, 설치 Task 실제 SHA-256
FINAL EVIDENCE: 필수 미확인 유무, 현재 상태, Task와 Archive bytes 동일 여부
NEXT: RMAP09_COMPOSER LOCKED / NOT STARTED
COMMIT: 작업 전 HEAD, 실제 생성 commit SHA 또는 아직 미생성인 단계/이유
```

## 15. PASS / Finalize / STOP

6개 요구 ID의 모델/API/자료/실제 표시와 필수 검사가 모두 확인돼야 PASS다. 실행하지 않은 기능/검사를 PASS로 적지 않는다.
선행 상태/해시 충돌은 STATUS_CONFLICT, 필수 자료/도구 부재는 BLOCKED, 기능/확인 기준 불충족은 FAIL로 보고한다.
낮은 천장/불리한 형상 등 난도는 허용한다. 확인된 필수 유일 경로 단절을 자동 굴착/침묵 수리/검증 완화로 PASS 처리하지 않는다.
PASS Result 후 기존 Finalize로 RMAP08 CURRENT->COMPLETE, Current->NONE만 수행한다. RMAP09~19는 LOCKED다.
기준선 상태는 적용 후 228 COMPLETE / 1 CURRENT / 11 LOCKED, 완료 후 229 COMPLETE / 0 CURRENT / 11 LOCKED다.
Task/Archive/Result/허용 코드/asset/CSV/증거/상태 변경만 commit한다. 무관한 staged 변경을 포함하거나 임의 unstage하지 않는다.
commit 메시지: RMAP08 implement chunk types ports and directed connections
최종 CLI 보고에 Result 경로, 실제 생성 commit SHA, Result SHA-256, 설치 Task SHA-256, Current NONE, RMAP09 LOCKED를 적는다.
Result를 commit 전에 작성했다면 실제 commit 증거는 최종 CLI 보고로 남긴다. 해시 기입을 위해 선행/설치 문서를 다시 쓰지 않는다.
Git push와 RMAP09 실행 없이 STOP한다.
