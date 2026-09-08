```yaml
mcp_patch:
  format: single_task_v1
  task_id: RMAP07_PATTERNS
  task_file: TASKS/RMAP07_PATTERNS.md
  requires_current_task: NONE
  requires_completed_task: RMAP06_LOOK
  requires_result:
    path: REPORTS/RMAP06_LOOK_RESULT.md
    status: PASS
    sha256: 6cad2dd5629d05643c3c86449f990a90ad78370d5fda068246fb5f3c108b59e1
  requires_installed_task:
    path: TASKS/RMAP06_LOOK.md
    sha256: 1f11eeb9f775c78e6553d88dd2f49f9243f8d8b6faa71bf3fa707b915f9877aa
  sets_current_task: RMAP07_PATTERNS
```

# RMAP07_PATTERNS - 역할형 패턴과 첫 후보 풀

```text
TASK: RMAP07_PATTERNS
DOCUMENT: v4.2 / RMAP06 PASS 이후 실행 지시서 / 2026-09-08
STATUS: CURRENT
INPUT: MapDesign/MCP_INBOX/RMAP07_PATTERNS.md
EXPECTED_RESULT: MapDesign/MCP/REPORTS/RMAP07_PATTERNS_RESULT.md
NEXT: RMAP08_PORTS
NEXT STATUS: LOCKED / DO NOT START
```

위 CURRENT는 정상 Apply 이후의 실행 상태다. 문서 발행만으로 저장소 상태를 바꾸지 않는다.
약 1~2시간의 기능 책임 단위이며 문서는 300줄 이하로 운영한다.

## 1. User-Facing Goal / 이번 작업의 완료 모습

Unity에서 실제 1x1 Tilemap 지형으로 구성된 4x4 패턴 후보들을 볼 수 있다.
각 후보는 16셀 Base Geometry, Primary Role 하나, 의도 태그, 자동 물리 특성, 원본/Transform 관계를 가진다.
중복 제거 후 40~60개의 첫 후보 풀을 CSV와 조회 가능한 데이터로 제공한다.
대표 패턴에서는 실제 Player로 고체 충돌과 일방향 발판의 상향 통과/상단 착지를 확인한다.
이번 요구 ID는 A01, A06, A07, A08, A09, A10, A11, A12, A15다. 아래에 전체 구현 조건을 포함한다.
최종 500개 확충, Port 연결 계약, 12x8 청크 생성기, 월드 생성은 각각 후속 작업 책임이다.

## 2. Preflight / 선행 완료와 정상 single_task_v1 적용

1. 프로젝트 루트와 적용 AGENTS.md, MapDesign/MCP/00_MCP_ENTRYPOINT.md를 확인한다.
2. 기존 Apply/ChangeControl/Finalize와 RMAP/02_PROTOCOL_V4_2.md를 읽고 지원 필드/경로를 대조한다.
3. YAML 내부 경로는 MapDesign/MCP 기준, 본문 Assets/MapDesign 경로는 프로젝트 루트 기준이다.
4. 이 파일과 함께 전달한 실행 지시문의 SHA-256을 inbox 파일 전체 원본 bytes의 SHA와 대조한다.
   파일 자체의 expected SHA는 자기참조를 피하려고 본문에 넣지 않는다. planned 기획 문서의 해시와 비교하지 않는다.
   외부 값 부재/불일치 또는 현지 별도 manifest/등록 SHA 충돌이면 적용 전에 경로와 expected/actual을 보고하고 중단한다.
   줄바꿈 정규화/재저장으로 해시를 맞추거나 적용기 규칙을 수정하지 않는다.
5. RMAP06 installed Task/Archive의 bytes/SHA, PASS Result의 TASK/STATUS/SHA를 YAML과 대조한다.
6. 첨부 RMAP06 Result는 Phase C Finalize 권한과 Phase D commit 예정만 기록했다. 그 문구를 실제 완료 증거로 간주하지 않는다.
   저장소에서 RMAP06 COMPLETE, Current NONE, RMAP07~19 LOCKED 및 Result 경로의 git log로 실제 RMAP06 commit을 확인한다.
   Finalize/commit 미완료면 남은 단계와 근거를 보고하고 STOP한다. 이 작업 안에서 선행 상태/Result를 임의 수정하지 않는다.
7. 정상 시작 기준은 240행 = 227 COMPLETE / 0 CURRENT / 13 LOCKED다. 차이가 있으면 실제 이력과 프로토콜로 판단한다.
8. 미적용 inbox 후보 본 MD 1개와 RMAP07 등록 ID를 확인한다. 다른 CURRENT/선행 변경/Task-Archive 불일치는 적용 전 중단한다.
9. 정상 Apply로 RMAP07만 LOCKED->CURRENT, Current NONE->RMAP07_PATTERNS를 수행하고 Task/Archive를 바이트 동일하게 설치한다.
10. 같은 RMAP07 CURRENT 재개는 설치 Task/Archive/입력 동일성과 기존 결과를 확인하고 현지 재개 규칙을 따른다.
11. 이미 RMAP07 COMPLETE+유효 PASS라면 기존 결과를 보고하고 STOP한다. RMAP08을 자동으로 열지 않는다.
12. 무관한 변경은 보존한다. 관련 변경을 분리할 수 없으면 근거를 보고하며 reset/stash/강제 덮어쓰기를 하지 않는다.

## 3. Read Allowlist / 실제 경로 바인딩

- MapDesign/MCP/RMAP/{00_BASELINE_V4_2,01_SEQUENCE_V4_2,02_PROTOCOL_V4_2}.md 및 현지 운영 문서.
- MapDesign/MCP/{MASTER_IMPLEMENTATION_TASK_LIST,06_IMPLEMENTATION_STATUS}.md와 RMAP06 Task/Archive/Result.
- MapDesign/MCP/GENERATED/RMAP01/{file_bindings,reuse_decisions}.csv 중 아래 별칭/패턴 책임 행. 파일명은 현지 존재 여부를 확인한다.
- PATTERN_DATA / TRANSFORM / CLASSIFIER / EDITOR 바인딩이 가리키는 코드, CSV, importer/exporter, 직접 호출자와 테스트.
- 기존 500 후보 및 RUN06 선별 기능의 원본 ID/Mask/Transform/Seed/버전/필터/출력 계약을 필요한 범위만 확인한다.
- RMAP02 실제 Tilemap applier/Player prefab, RMAP04 one-way Bake/PlatformEffector2D 증거, RMAP06 실험실 builder와 scene.

| 연결점 | 증거 상태 | 이번 책임 |
|---|---|---|
| PATTERN_DATA / TRANSFORM / CLASSIFIER / EDITOR | RMAP01 바인딩에서 현지 해석 필요 | 기존 모델/변환/분류/표시 재사용 여부와 실제 수정 경계 확정 |
| Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/RmapPatternCatalog.cs | PROPOSED: RMAP06 보고 | 기존 catalog가 책임을 충족하지 못할 때만 필요한 catalog/API 추가 |
| Assets/_Game/Live/Editor/RMAP06/CharacterLiveMovementLabSceneBuilder.cs | EXISTING: RMAP06 보고 | 물리 fixture 구성 방식 참조 |
| Assets/_Game/Map/Scenes/MoonPalace/RMAP06/MoonPalaceMovementLab_RMAP06.unity | EXISTING: RMAP06 보고 | 실제 Player/Tilemap/camera 배선 참조 |
| Assets/_Game/Map/Scenes/MoonPalace/RMAP07/MoonPalacePatternGallery_RMAP07.unity | PROPOSED | 첫 후보 풀 전시와 대표 Player 충돌 확인 씬 |

바인딩 CSV가 가리키는 실제 경로/API를 읽은 뒤 KEEP/ADAPT/NEW와 이유를 Result에 남기고 수정한다.
별칭과 PROPOSED 이름은 기존 구현의 증거가 아니다. 누락이면 관련 디렉터리와 직접 참조만 좁게 찾아 실제 경로를 확정한다.
기존 공개 API/CSV를 적응하고 Task별 새 Service를 만들지 않는다. 긴 파일은 rg로 심볼을 찾고 필요한 구간만 읽는다.
현재 Player/Camera 인스턴스는 패턴 catalog의 의존성이 아니다. 생성 정적 데이터와 플레이 변경 상태를 분리한다.

## 4. Write Allowlist / 변경 경계

- 위 바인딩으로 확인한 패턴 데이터/분류/변환/CSV 입출력과 그 직접 호출부의 최소 호환 변경.
- 필요성이 확인된 MicroPatterns 계층의 모델/catalog/자동 특성 계산 보조 코드. 기존 Seed/버전/ID 계약을 보존한다.
- 기존 authoring 체계의 RMAP07 첫 풀 CSV와 관계/특성 자료. 기존 500 원본, 이진 Mask, 원본 ID를 덮어쓰지 않는다.
- RMAP07 전용 gallery scene/builder/fixture 표시 및 필요한 Tile/prefab/.meta. 기존 씬/Player prefab을 임의 교체하지 않는다.
- 기존 테스트 assembly 안의 focused 패턴/변환/CSV 테스트와 대표 실제 Player/Collider PlayMode 확인.
- MapDesign/MCP/GENERATED/RMAP07의 첫 풀 snapshot, 관계/특성/선정 근거, fixture manifest와 실행 증거.
- 지정 Task/Archive/Result 및 기존 프로토콜이 요구하는 RMAP07 상태 기록. asmdef는 컴파일에 필요한 최소 참조만 수정한다.

원본 authoring과 파생 Generated 출력의 권위/재생성 경로를 구분한다. 같은 catalog를 독립 편집하는 두 원본을 만들지 않는다.
RMAP02~06 이동/Grab/climb/fall/look 수치와 입력을 재튜닝하지 않는다. 새 package, 운영 프로토콜/과거 Result 수정은 범위 밖이다.

## 5. A01 / Tile·Pattern·Chunk 계층

- Tile=1x1 충돌 단위, MicroPattern=4x4 역할형 Base Geometry다. 후보 하나의 Base 셀 수는 정확히 16이다.
- MicroChunk=12x8이며 Pattern 3x2개로 구성된다. 저장·검증·조립 단위이며 화면과 크기가 같아도 카메라 방이 아니다.
- 이번에는 치수/관계 계약을 표현한다. 3x2 자동 조립, Port 계약, CameraRoom snap을 구현하지 않는다.
- 셀 좌표 원점, x/y 방향과 16셀 직렬화 순서는 기존 규격을 우선하고 명시한다. 규격이 없으면 좌하단 원점, x 우향/y 상향, index=y*4+x를 쓴다.
- CSV, Transform, 자동 특성, scene 표시가 동일 좌표 변환을 사용한다. 화면상 위에서부터 읽는 배열과 저장 순서의 차이도 기록한다.

## 6. A06·A07 / Base Geometry와 제외 대상

- 최종 500은 Transform 후 중복 제거까지 끝난 서로 다른 16셀 Base Geometry 배열 500개다. 이번 첫 풀은 그중 40~60개다.
- Base 셀은 SOLID / AIR / ONE_WAY_PLATFORM 세 종류만 허용한다. 3^16 전수 열거는 하지 않는다.
- 기존 이진 Mask의 bit/좌표 의미를 확인하고 SOLID/AIR로 변환하며 원본 파일·Mask·ID와 변환 관계를 보존한다.
- ONE_WAY_PLATFORM은 별도 셀 값으로 보존한다. SOLID와 합치거나 공기로 취급해 중복 계산하지 않는다.
- LADDER / CLIMB_PILLAR는 Overlay로 분리한다.
- HAZARD / MONSTER_SLOT / RESOURCE_SLOT / REWARD_SLOT / MECHANISM_SLOT / MATERIAL / DECORATION은 용도별 Overlay/슬롯/재질·장식이다.
- 이 아홉 종류는 500개 수와 Base Geometry 중복 키에 포함하지 않는다. 타입 구분만으로 미구현 콘텐츠를 생성하지 않는다.
- 역할/태그/Overlay/원본 ID만 다르고 16셀 배열이 같으면 같은 Candidate다. 셀 배열이 다르면 다른 Candidate다.

## 7. A08 / Primary Role 전체

다음 10개를 실제 enum/데이터 값과 분류 결과로 연결한다. 후보 하나의 Primary Role은 정확히 하나다.

- SLOPE_RISE_RIGHT / SLOPE_RISE_LEFT
- CEILING_FLAT / CEILING_ROUGH
- WALL_LEFT / WALL_RIGHT
- VOID_CLEAR / SPARSE_AIR_PLATFORM
- STANDABLE_LEDGE / VERTICAL_PASSAGE

복합 형상과 1타일 통로를 허용한다. 모호한 형상은 명시적인 분류 규칙/우선순위로 하나를 고르고 이유를 추적한다.
VOID_CLEAR는 모든 셀이 AIR인 정확히 한 후보다. 네 Transform이나 다른 원본 ID로 그 수를 늘리지 않는다.
10역할의 대표 형상과 분류 이유를 첫 풀의 자료/전시에서 확인할 수 있어야 한다.

## 8. A09 / 의도 태그 전체

- FLAT_FLOOR / STEP_FLOOR
- SHORT_LEDGE / LONG_LEDGE
- TAKEOFF / LANDING / RUN_APPROACH
- GRABBABLE_CORNER / LOW_CEILING / ONE_TILE_PASSAGE
- VERTICAL_CLEARANCE / LEFT_ENTRY / RIGHT_EXIT / UP_ENTRY / DOWN_EXIT
- REQUIRED_OK / OPTIONAL_ONLY / SECRET_SHELL / QUIET_FILL
- LADDER_ALLOWED / PILLAR_ALLOWED / HAZARD_ALLOWED / MECHANISM_ALLOWED / REWARD_ALLOWED

이 24개는 사람의 의도 태그다. 실제 배치 문맥의 물리 통과 증명이나 필수 경로 PASS로 간주하지 않는다.
태그를 자동 물리 특성과 다른 필드에 저장한다. 원본 의도를 보존하며 자동 계산으로 전체 태그를 추정/대체하지 않는다.

## 9. A10 / 자동 계산 물리 특성 전체

아래 값은 실제 Base 셀과 명시한 계산 규칙에서 계산하고 Candidate ID로 조회/내보낼 수 있어야 한다.

| 자동 특성 | 기록/검증 기준 |
|---|---|
| L/R/U/D EdgeOpenCellSet | 변별 가능한 경계 셀 좌표 집합. ONE_WAY의 방향별 충돌과 AIR 구분을 기록하며 Port 통과 판정으로 쓰지 않는다. |
| 연속 Standable 길이와 머리 공간 | 서 있을 수 있는 상단면 구간, 길이, 위쪽 빈 셀 여유. 후보 밖을 확인 못한 구간은 unknown/context-required다. |
| 점프 출발 셀과 착지 셀 | 로컬 후보 위치 집합. 출발-착지 사이 점프 가능성의 실제 배치 증명과 구분한다. |
| Grab 가능한 모서리와 낙하 열 | 안전 고체의 로컬 모서리 후보와 세로 공간. RMAP03/04 기준상 one-way는 Grab 대상에서 제외한다. |
| 등반 Overlay 가능 영역 | 사다리/기둥을 배치할 로컬 영역. 실제 Overlay/등반 동작 생성과 분리한다. |
| 고체/공기 비율과 연결된 공기 컴포넌트 | 16셀 분모의 SOLID/AIR/ONE_WAY 별 count/ratio와 명시한 인접 규칙으로 계산한 AIR 연결 집합. |
| Transform 대칭성 | 각 허용 Transform 결과의 16셀 배열 동일 여부와 동등 Transform 집합. |

연결된 AIR 컴포넌트는 기존 규약을 확인하고, 규약이 없으면 상하좌우 4-neighbor를 사용한다. 대각 접촉을 통로로 오인하지 않는다.
고체 비율을 위해 ONE_WAY를 SOLID에 섞거나 경계 밖 셀을 무조건 AIR로 가정하지 않는다.
특성이 이웃 셀/Player profile에 의존하면 사용 조건과 context-required를 기록한다. A09 의도 태그를 계산 입력의 정답으로 쓰지 않는다.
좌우/상하 경계 일치나 넓은 머리 공간만으로 필수 통과 성공을 주장하지 않는다. 실제 배치 검증은 후속 작업 책임이다.

## 10. A11·A12 / Transform·중복·재분류와 one-way

- 허용 Transform은 R0, MirrorX, MirrorY, R180뿐이다. 90/270도 회전은 추가하지 않는다.
- MirrorX는 좌우, MirrorY는 상하 반사다. 기본 좌표 규격에서 각각 (3-x,y), (x,3-y), R180은 (3-x,3-y)다.
- 결과 16셀 배열이 같으면 동일 Candidate ID로 모으고, 다르면 별도 ID로 센다. Transform 이름만으로 새 후보를 만들지 않는다.
- dedup 키는 순서가 정해진 Base 배열이다. 해시를 쓰더라도 실제 배열 동등성을 확인한다.
- SourcePatternId + Transform -> CandidateId의 모든 출처 관계를 보존한다. 여러 원본/Transform이 합쳐져도 관계를 유실하지 않는다.
- 같은 입력/설정의 재생성은 동일 Candidate ID, 순서와 셀 결과를 만든다. 발견 순서만으로 기존 ID를 재번호화하지 않는다.
- 방향 의존 역할은 변환된 Geometry에서 재분류하고 자동 특성도 다시 계산한다.
- WALL_LEFT + MirrorX -> WALL_RIGHT, SLOPE_RISE_RIGHT + MirrorX -> SLOPE_RISE_LEFT를 확인한다.
- CEILING_FLAT + R180 -> PrimaryRole STANDABLE_LEDGE와 FLAT_FLOOR 태그를 대표 형상으로 확인한다.
- 방향성 태그는 명시적 변환표를 적용한다. 기존 태그 집합으로 표현 못하는 반대 방향은 의미를 지어내지 말고 보류/재검토 근거를 기록한다.
- REQUIRED_OK/OPTIONAL_ONLY/SECRET_SHELL 등 인위적 용도 태그 전체를 자동 재추정하지 않는다.
- 동일 Geometry로 합쳐질 때 출처별 의도 태그를 남기고 충돌을 보고한다. 상반된 태그를 침묵 병합해 필수 사용을 허가하지 않는다.
- 어떤 Transform에서도 ONE_WAY_PLATFORM의 충돌면은 월드 위쪽이다. 셀 위치만 변환하고 의미는 유지한다.
- MirrorY/R180 결과를 뒤집힌 GameObject의 scale/rotation으로 Bake하지 않는다. RMAP04의 top-only collision 설정을 재사용한다.
- 실제 TilemapCollider2D/PlatformEffector2D로 R0/MirrorX/MirrorY/R180 대표 발판의 상향 통과와 상단 착지를 확인한다.

## 11. A15 / 첫 40~60개 풀과 데이터 산출물

- 기존 후보와 RUN06 선별 기능을 재사용해 초기 조립에 필요한 서로 다른 Base 후보 40~60개를 선정한다.
- 10역할과 가능한 경계 개방/수직 공간/solid·one-way 형태를 대표하도록 고른다. VOID_CLEAR는 정확히 하나다.
- 최종 500개 보강을 완료했다고 표시하거나 첫 풀/첫 실제 생성 맵의 선행조건으로 두지 않는다.
- 기존 CSV 규약에 아래 정보를 연결한다. 새 CSV가 필요하면 기존 authoring 위치에 범위를 한정하고 경로/필드 의미를 Result에 명시한다.

| 자료 | 필수 내용 |
|---|---|
| 후보 catalog | CandidateId, 정확한 BaseCells16, PrimaryRole, 의도 태그, 기존 버전/ID 관련 필드 |
| 출처/변환 관계 | SourcePatternId, 원본 파일/row 또는 원본 Mask 참조, Transform, CandidateId, 출처별 의도 태그/충돌 |
| 자동 특성 | CandidateId와 A10의 모든 값, 좌표/인접 규칙, 계산 조건과 context-required |
| 첫 풀 선정 | 선택 CandidateId 40~60개, 역할/형태별 선정 이유와 제외/중복 집계 |
| 표시 manifest | 저장 scene, CandidateId별 원점/16셀 위치, Player 진입점, one-way 검증 fixture와 조작 안내 |

다중 값의 구분/escaping을 정하고 UTF-8 CSV를 round-trip한다. 누락/중복 ID, 잘못된 셀 수/값, 미해결 출처 참조는 명시적으로 실패한다.
필드가 같은 의미의 기존 타입/열이면 재사용한다. Generated snapshot만 있고 읽는 API/재생성 경로가 없는 출력으로 끝내지 않는다.

## 12. Unity Gallery / 실제 지형 표시

- RMAP07 전용 저장 씬에서 첫 풀을 Candidate ID/역할 라벨과 함께 실제 1x1 Tilemap 지형으로 펼쳐 볼 수 있게 한다.
- 개별 후보는 4x4다. 시험 씬의 전체 가로/세로는 각각 12와 8의 배수로 정하고 bounds/배치를 manifest에 기록한다.
- 전시용 간격/접근 바닥과 후보 Base 셀을 구분한다. 지지대가 필요해도 Candidate 배열을 수정해 통과시키지 않는다.
- 대표 역할과 네 Transform의 one-way는 기존 실제 Player, Rigidbody2D/Collider, Input System 경로로 확인한다.
- RMAP02 applier/RMAP04 one-way 배선을 재사용한다. 논리 Bake/Preview marker만으로 물리 충돌 PASS를 대신하지 않는다.
- 생성 catalog는 Live Player/Camera를 참조하지 않는다. 씬 builder만 데이터와 런타임 표시/Player를 연결한다.
- 씬 경로, 후보 찾는 법, 키 조작, 대표 fixture 위치를 사용자 안내와 Result에 적는다.

## 13. 구현 순서와 Focused Checks

1. 선행 체인/실제 완료 상태 확인과 정상 Apply 후 별칭을 실제 파일에 연결하고 재사용 경계를 정한다.
2. Base 셀/역할/태그/출처 자료를 연결하고 이진 원본을 보존하는 변환 경로를 만든다.
3. 네 Transform, 배열 dedup/안정 ID, 재분류/방향 태그 표와 A10 특성 계산을 구현한다.
4. 첫 풀 40~60개를 선정하고 CSV round-trip/관계 참조/결정적 재생성을 확인한다.
5. 실제 gallery/one-way fixture를 만들고 아래 필요한 검사를 실행해 재현된 실패만 좁게 고친다.
6. 요구 ID별 실제 증거와 남은 배치 문맥 책임을 보고하고 PASS일 때만 Finalize/commit한다.

- EditMode: 이진 Mask 좌표 매핑, 16셀/3종 Base 유효성, 같은 배열 dedup/서로 다른 배열 분리, 원본/Transform 추적.
- EditMode: VOID_CLEAR 하나, 네 Transform 좌표/대칭성, 역할 재분류 예제, 방향성 태그 변환/충돌 기록.
- EditMode: 손으로 판별 가능한 작은 형상으로 A10 모든 특성 및 후보 밖 unknown 구분을 확인한다.
- EditMode: Overlay/태그 차이로 Base 수가 늘지 않음, CSV round-trip/참조 오류, 첫 풀 범위/10역할 대표, 동일 입력 재생성.
- PlayMode: 실제 Player/Tilemap의 대표 고체 충돌, 네 Transform one-way의 아래->위 통과/상단 착지/월드 위쪽 충돌면.
- Compile/refresh와 필요한 씬 생성은 수행한다. 직접 영향 회귀만 최소 범위로 선택하고 이유를 적는다.
- broad/full/unfiltered regression, legacy 19347, 불필요한 Player build, 3^16 열거와 500 최종 보강은 실행하지 않는다.
- 테스트 수를 목표로 늘리지 않는다. 초기 배치 외 teleport/검사 marker로 실제 입력/물리 동작을 대신하지 않는다.

Unity 미실행/도구 부재는 NOT RUN 또는 BLOCKED다. 과거 PASS를 새 Transform one-way 물리 확인의 대체 증거로 쓰지 않는다.

## 14. Required Result / 완료 보고

```text
TASK: RMAP07_PATTERNS
STATUS: PASS 또는 FAIL 또는 BLOCKED 또는 STATUS_CONFLICT
USER-FACING IMPLEMENTATION REPORT: 후보 수, gallery 여는 법/조작, 데이터 찾는 법, 아직 없는 생성 기능
RESPONSIBILITY AND FILES: 실제 경로 | 추가/수정 | 책임 | 소유하지 않는 책임
PRECONDITIONS: HEAD/branch, 외부 expected/inbox 실제 SHA, 선행 Task/Archive/Result SHA와 실제 commit, 적용 전후 상태
CHANGED: 바인딩 해석과 KEEP/ADAPT/NEW 이유, CSV 원본/파생 경로, ID/좌표/분류/변환 규칙
REQUIREMENT EVIDENCE: A01/A06/A07/A08/A09/A10/A11/A12/A15 각각 산출물, 검사/관찰, 판정
CATALOG EVIDENCE: 원본/변환/중복 제거/선정 수, 10역할 분포, VOID_CLEAR, 출처 관계와 태그 충돌, context-required
VALIDATION: 실제 filter/job/count, CSV 재생성/round-trip, Play 확인, 실패/최소 수정, 미실행과 이유
UNITY VISIBLE OUTPUT: scene, 전체 bounds, 후보/fixture 위치, 실제 Player/Tilemap/one-way 증거
OUT-OF-SCOPE FINDINGS: Port 계약/청크 조립/500 확충/월드 생성/배치 문맥 통과 판정의 남은 책임
RMAP08 BINDINGS: catalog/BaseCells/EdgeOpenCellSet/변환/특성 조회의 실제 경로와 API, EXISTING/PROPOSED 구분
FOLLOWUP: 현지 single_task_v1 경로/필드 규칙, 설치 Task 실제 SHA-256
FINAL EVIDENCE: 필수 미확인 유무, 현재 상태, Task와 Archive bytes 동일 여부
NEXT: RMAP08_PORTS LOCKED / NOT STARTED
COMMIT: 작업 전 HEAD, 실제 생성 commit SHA 또는 아직 미생성인 단계/이유
```

## 15. PASS / Finalize / STOP

9개 요구 ID의 자료/API/첫 풀/실제 지형과 필수 검사가 모두 확인돼야 PASS다. 실행하지 않은 기능/검사를 PASS로 적지 않는다.
선행 상태/해시 충돌은 STATUS_CONFLICT, 필수 자료/도구 부재는 BLOCKED, 기능/확인 기준 불충족은 FAIL로 보고한다.
낮은 천장/복합 형상 등 난도는 허용한다. 확인된 필수 유일 경로 단절을 자동 굴착/침묵 수리/검증 완화로 PASS 처리하지 않는다.
PASS Result 후 기존 Finalize로 RMAP07 CURRENT->COMPLETE, Current->NONE만 수행한다. RMAP08~19는 LOCKED다.
기준선 상태는 적용 후 227 COMPLETE / 1 CURRENT / 12 LOCKED, 완료 후 228 COMPLETE / 0 CURRENT / 12 LOCKED다.
Task/Archive/Result/허용 코드/asset/CSV/증거/상태 변경만 commit한다. 무관한 staged 변경을 포함하거나 임의 unstage하지 않는다.
commit 메시지: RMAP07 implement role patterns and initial candidate pool
최종 CLI 보고에 Result 경로, 실제 생성 commit SHA, Result SHA-256, 설치 Task SHA-256, Current NONE, RMAP08 LOCKED를 적는다.
Result를 commit 전에 작성했다면 실제 commit 증거는 최종 CLI 보고로 남기며, SHA를 넣기 위해 선행/설치 문서를 다시 쓰지 않는다.
Git push와 RMAP08 실행 없이 STOP한다.
