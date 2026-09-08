```yaml
mcp_patch:
  format: single_task_v1
  task_id: RMAP14_BIOMES
  task_file: TASKS/RMAP14_BIOMES.md
  requires_current_task: NONE
  requires_completed_task: RMAP13_WORLD_GRAPH
  requires_result:
    path: REPORTS/RMAP13_WORLD_GRAPH_RESULT.md
    status: PASS
    sha256: 2aeae5f7b259787b2b70d5ce07f61145e4b40f0c3717cbff079d948cc3e01a01
  requires_installed_task:
    path: TASKS/RMAP13_WORLD_GRAPH.md
    sha256: 00b926dcfcb3fc5ed4e76a5f541fe68064346a2036fe473aeaa7308063685f12
  sets_current_task: RMAP14_BIOMES
```

# RMAP14_BIOMES - 네 바이옴 공간 배치와 밀도 프로필

```text
TASK: RMAP14_BIOMES
DOCUMENT: v4.2 / RMAP13 PASS 이후 실행 지시서
STATUS: CURRENT (정상 Apply 이후)
INPUT: MapDesign/MCP_INBOX/RMAP14_BIOMES.md
EXPECTED_RESULT: MapDesign/MCP/REPORTS/RMAP14_BIOMES_RESULT.md
NEXT: RMAP15_SPECIALS
NEXT STATUS: LOCKED / DO NOT START
```

문서 발행만으로 저장소 상태를 바꾸지 않는다. 이 MD는 300줄 이하의 한 Task 실행 계약이다.
이번 요구 ID는 A25/W05다. RMAP13의 논리 진행·계획 예약과 RMAP12의 결정성/ID/정의를 소비한다.

## 1. User-Facing Goal / 완료 모습

624×416 월드 좌표 위에서 네 바이옴의 위치·범위·경계와 선택된 밀도 프로필을 한눈에 확인한다.
실제 생성 요청에서 바이옴 계획 결과를 만들고, 후속 특수 지역 예약이 소비할 입력으로 제공한다.
프로필 설정 목록이나 작은 예시 객체만 만든 상태로 전체 공간 배치 완료를 주장하지 않는다.
이번 단계는 바이옴 공간 계획이다. 전체 Tilemap Bake/물리 통과/특수 지역 고정 지형은 후속 책임이다.

## 2. Preflight / 선행 완료와 정상 Apply

1. 프로젝트 루트/AGENTS.md, MapDesign/MCP/00_MCP_ENTRYPOINT.md, RMAP 프로토콜과 현지 Apply/Finalize를 읽는다.
2. 외부 전달 SHA-256과 inbox 전체 원본 bytes를 대조한다. 파일 자체의 expected SHA는 본문에 넣지 않는다.
3. 외부 값 부재/불일치나 별도 등록 SHA 충돌이면 경로/expected/actual을 보고한다. 정규화·재저장으로 해시를 맞추지 않는다.
4. 이전 planned 문서 SHA와 이번 실행 MD SHA를 혼동하지 않는다. 적용기/운영 검사를 수정하지 않는다.
5. RMAP13 Result의 TASK/PASS/SHA, 설치 Task/Archive의 bytes 동일성과 SHA를 YAML과 대조한다.
6. 제출 Result는 Finalize/commit 이전 문구다. 실제 RMAP13 COMPLETE, Current NONE 및 Result 경로 git log/commit을 확인한다.
7. 아직 RMAP13 CURRENT이고 해당 PASS/증거가 유효하면 RMAP13 규정에 따른 Finalize·소유 commit만 먼저 마무리한다.
8. 위 선행 마무리는 RMAP13 이력으로 분리한다. RMAP14를 먼저 CURRENT로 열거나 선행 Result 내용을 임의 수정하지 않는다.
9. 선행 상태/증거가 불충분하면 원인을 보고한다. 선행 기능을 RMAP14에서 숨겨 구현하거나 근거 없이 PASS로 바꾸지 않는다.
10. 정상 시작 기준은 240행 = 234 COMPLETE / 0 CURRENT / 6 LOCKED, RMAP14~19 LOCKED다.
11. 실제 branch/HEAD/git status, 다른 CURRENT 부재, inbox 후보/등록 ID와 현지 상태를 확인한다.
12. §3의 기존 바인딩을 읽고 실제 Read/Write 경로·직접 호출자·KEEP/ADAPT/NEW를 사전 바인딩 기록으로 확정한다.
13. 별칭을 새 클래스 생성 목록으로 사용하지 않는다. 경로 해석 때문에 이 MD를 재저장해 입력 해시를 바꾸지 않는다.
14. 정상 Apply로 RMAP14만 LOCKED→CURRENT, Current NONE→RMAP14_BIOMES를 수행하고 Task/Archive를 동일 bytes로 설치한다.
15. 같은 CURRENT 재개는 설치/Archive/입력 동일성과 기존 결과를 확인한다. 이미 COMPLETE+유효 PASS면 보고하고 STOP한다.
16. 무관한 변경은 보존한다. reset/stash/강제 덮어쓰기, 무관한 stage/unstage, git push를 하지 않는다.

## 3. Read Allowlist / 실제 소비 경로

- MapDesign/MCP/RMAP/{00_BASELINE_V4_2,01_SEQUENCE_V4_2,02_PROTOCOL_V4_2}.md 및 필요한 현지 운영 문서.
- MapDesign/MCP/{MASTER_IMPLEMENTATION_TASK_LIST,06_IMPLEMENTATION_STATUS}.md와 RMAP13 Task/Archive/Result.
- MapDesign/MCP/GENERATED/RMAP01/file_bindings.csv의 BIOME/BOUNDARY/PROFILE/EDITOR와 필요한 RNG/ID/WORLD_DEFINITION 바인딩.
- 바인딩된 바이옴 배치/경계/설정/Editor 코드와 직접 호출자/검사. 기존 MAP08 경계 Authoring·승인 조합·좌표 의미.
- RMAP13 실제 그래프/예약/6개 증거 경로와 RMAP12 실제 요청/결과/stream/안정 ID API.
- RMAP11 수정 후 최종 catalog/version과 공유 이동 profile. 과거 500개·모든 Seed·모든 Task를 재감사하지 않는다.

| 보고서로 확인된 실제 연결점 | 이번 책임 |
|---|---|
| Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/RmapWorldGraphPlanner.cs | 기존 논리 그래프·계획 예약·ID 관계를 읽고 공간 계획의 입력으로 소비 |
| Assets/_Game/Map/Runtime/WorldGeneration/WorldData/RmapWorldDataContract.cs | 기존 요청/immutable 정의/Biome stream/안정 ID 접점 재사용 |
| Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionFixedSlotLayers.cs | 기존 예약/슬롯 의미를 읽고 다음 단계의 입력 형식과 충돌하지 않게 연결 |
| Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionSiteBridge.cs | 후속 예약 접점 확인. 실제 고정 지형 배치는 RMAP15 책임 |
| Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/RmapPatternPool500.cs | density metric에 실제 셀이 필요한 경우 최신 Base/ID 계약만 소비 |
| BIOME/BOUNDARY/PROFILE/EDITOR 실제 바인딩 파일 | 기존 배치/승인 경계/설정/표시를 KEEP/ADAPT 우선으로 사용 |

위 이름이 변경됐으면 바인딩/직접 호출 근거로 현지 경로를 적응한다. 없는 API를 기존 구현으로 단정하지 않는다.
기존 책임으로 부족할 때만 필요한 최소 파일을 추가하고 PROPOSED→실제 경로/이유를 Result에 남긴다.

## 4. Write Allowlist / 범위

- 확정한 기존 바이옴 영역 생성·밀도 프로필 설정/가중치·경계 연결 및 직접 소비 접점.
- 월드 요청→바이옴 계획 결과→다음 예약 입력의 최소 연결. 특정 Player/Camera 인스턴스에 의존하지 않는다.
- 기존 Editor/내보내기의 전체 배치 표시, 필요한 최소 CSV/이미지 출력.
- RMAP14 focused tests 및 MapDesign/MCP/GENERATED/RMAP14의 계획/경계/프로필/표시/검증 자료.
- 지정 Task/Archive/Result와 현지 프로토콜의 상태 기록. .meta/assembly 참조는 필요한 최소 범위.

MAP08 승인 Authoring 원본과 과거 Scene/Result는 보존한다. 호환은 별도 최소 adapter/선택 입력에서 해결한다.
RMAP11의 25개 수정 형상, Player 점프·Grab·Collider, RMAP13 진행 의미를 밀도나 배치 편의를 위해 바꾸지 않는다.
RMAP15 특수 지역 실예약/고정 지형, RMAP16 TerrainCluster 배치, RMAP17 전체 Bake, 콘텐츠/전투는 이번 범위 밖이다.

## 5. W05 / 네 BiomePatch의 실제 공간 배치

반드시 포함할 네 바이옴: 분화구 작업지 / 계수나무 뿌리숲 / 폐방앗간 지대 / 달반죽 지하.
표시 이름과 현지 stable biome key를 명시적으로 매핑한다. 번역/순번 때문에 기존 승인 경계 ID를 바꾸지 않는다.

1. 전체 계획 bounds는 624×416 Tile, MicroChunk는 12×8, 따라서 52×52 = 2,704개 좌표 자리다.
2. Tile/Chunk/Patch 좌표를 구분한다. 좌하단 원점/x 우향/y 상향 변환은 실제 기존 계약을 재사용한다.
3. 계획 대상 2,704개 자리에 바이옴 소유권을 명확히 부여하고 전체 범위를 표시한다.
4. 바이옴 소유권은 ACTIVE/SECRET/INACTIVE_SOLID/SPECIAL_RESERVED 상태와 별개다. 전부 이동 가능한 청크로 만들지 않는다.
5. 네 패치는 각각 비어 있지 않은 공간 영역이어야 한다. 기존 패치 모델의 연결성/겹침 규칙을 확인해 사용한다.
6. 기준 구현에서는 패치별 면 공유 연결성, 월드 내 좌표, 소유권 중복·누락을 검사한다. 이 검사는 Player 통과 증명이 아니다.
7. 기존 결정적 패치 생성기를 우선 사용한다. 부족하면 유한한 규칙/파라미터/시도 상한을 명시해 구현한다.
8. 비율·모양·종자점 선택을 설정/Seed에 연결한다. 고정 사분면 그림 하나를 출력하고 가변 배치 시스템 완료라고 쓰지 않는다.
9. 모든 시드에서 다른 모양이 나와야 한다고 강제하지 않는다. 같은 입력 재현과 설정/Seed가 소비되는 실제 경로를 검증한다.
10. RMAP12의 Biome stream/scope/attempt를 재사용한다. 바이옴 재선택이 RunGraph 등 다른 stream을 소비하지 않게 한다.
11. Patch ID/Chunk owner/경계 ID를 안정적으로 연결한다. 순회 순서/현재 시각/Unity instance ID를 ID 입력으로 쓰지 않는다.
12. static world definition과 runtime mutation은 분리한다. Editor 표시를 데이터 원본으로 삼지 않는다.

## 6. MAP08 경계 Authoring 재사용

- 기존 승인된 실제 Authoring 파일/ID/조합/방향 규칙을 읽고 이번 네 바이옴 사이의 실제 인접 관계에 연결한다.
- 과거 PDF/그림 표를 정본으로 삼거나 이번 모양에 맞추어 승인 원본을 덮어쓰지 않는다.
- 인접한 각 바이옴 면의 좌표 범위/두 owner/방향/승인 source 또는 boundary requirement ID를 기록한다.
- 바이옴 접촉은 Tile/Chunk 경계 좌표에서 산출한다. 명칭 두 개를 나열한 조합 표만으로 경계 연결을 대신하지 않는다.
- 좌표 크기/방향이 이전 체계와 다르면 adapter 변환 근거와 미지원 항목을 명시한다. 숫자 단순 치환으로 호환을 주장하지 않는다.
- 생성된 인접 쌍은 승인 조합으로 해석돼야 한다. 미지원이면 원인 위치/조합을 보고하고 필요한 패치 범위만 제한 재선택한다.
- 모든 과거 경계를 다시 전수 생성/감사하지 않는다. 이번 실제 인접 쌍과 직접 영향 adapter만 확인한다.
- 경계 승인과 최종 Port/Tilemap 통과는 별개다. 이번 경계 결과의 검증 수준과 후속 충족 조건을 보존한다.
- 비밀 영역·일방통행·필수 동선을 경계 편의를 위해 임의 제거/양방향화하지 않는다.

## 7. A25 / 밀도 프로필과 가중치

| Profile | SOLID 비율 초기안 | 용도 |
|---|---|---|
| OPEN | 40~55% | 분화구 표면·대형 관측 공간 |
| BALANCED | 55~65% | 일반 탐험 |
| DENSE | 65~75% | 뿌리숲·방앗간 내부·달반죽 지하 |

1. Profile ID/범위/가중치는 한 설정 원본으로 관리한다. 코드 여러 곳의 중복 상수를 만들지 않는다.
2. BiomePatch별 OPEN/BALANCED/DENSE 선택 가중치를 설정 데이터로 둔다. 기존 승인 설정이 있으면 우선 재사용한다.
3. 설정이 없으면 임시 초기값과 선정 이유를 명시한다. 특정 가중치/바이옴 비율을 사용자 최종 확정으로 기록하지 않는다.
4. 유효 가중치는 유한한 비음수이며 합이 양수여야 한다. 음수/NaN/Infinity/전부 0은 명시적으로 거부한다.
5. stable profile 순서와 Biome stream의 별도 선택 scope로 결정적으로 선택한다. 재시도 이유/attempt를 추적한다.
6. 입력 가중치/사용한 정규화 또는 누적 구간/선택된 Profile ID/목표 범위/설정 버전을 출력한다.
7. 한 profile만 양수인 설정은 그 profile을 선택해야 한다. 작은 표본의 빈도가 정확히 가중치와 같아야 한다고 시험하지 않는다.
8. OPEN/DENSE를 바이옴 이름 하나만으로 영구 고정하지 않는다. 용도 안내와 실제 가중 선택을 구분한다.
9. 55%/65%처럼 경계가 겹치는 초기 범위는 선택된 Profile ID로 해석한다. 측정 비율만으로 소속을 바꾸지 않는다.

### 밀도 측정 범위와 현재 판정

- 밀도는 주변 MicroChunk 묶음 또는 TerrainCluster 범위에서 측정한다. 개별 4×4 후보의 탈락 기준으로 사용하지 않는다.
- metric은 겹치지 않는 실제 Base cell 좌표의 SOLID 수/전체 S+A+O 셀 수로 정의한다. ONE_WAY를 SOLID로 합치지 않는다.
- 측정 scope ID/포함 좌표/분자/분모/실제 비율/제외 정책을 기록한다. 중복 셀과 분모 0을 숨기지 않는다.
- 특수 지역 고정 지형/INACTIVE_SOLID를 측정에 포함할지는 scope 정책으로 명시한다. 목표를 맞추려고 임의로 제외하지 않는다.
- RMAP14에서 실제 Base가 아직 없는 전체 월드는 목표 Profile만 확정하고 MeasuredDensity=PENDING_GEOMETRY로 둔다.
- 면적 가중치, SOLID 추정치, profile 중간값을 실제 고체 측정값으로 출력하지 않는다.
- 측정 접점은 기존 실제 Base 표본이나 좁은 fixture로 검증한다. 이를 전체 월드 밀도 달성 증거로 확대하지 않는다.
- 이후 실제 생성에서 범위를 벗어나면 해당 scope/원인을 기록해 제한 재선택 입력으로 넘긴다. 평탄화·자동 굴착으로 숨기지 않는다.
- 밀도 목표보다 필수 이동/머리 여유/Grab 접촉/보호 공간이 우선한다. 최신 무장비 1칸 점프·2칸 점프+Grab 조건을 보존한다.

## 8. RMAP13 연결과 RMAP15 예약 입력

- RMAP13의 node/edge/reservation stable ID와 계획 조건을 보존해 바이옴 계획 결과에 참조한다.
- 보고된 8개 RMAP10 MicroChunk anchor는 논리 참조다. 작은 Run의 좌표를 Full Run 확정 좌표로 복사하지 않는다.
- 실제 공간 배치 전 PlannedSpace 간선을 Player 검증 완료로 바꾸지 않는다. RMAP13의 6개 논리 증거와 물리 증거를 구분한다.
- node/reservation별 허용·선호 바이옴 또는 후보 공간 범위를 현지 계약대로 다음 단계에 제공한다.
- 기존 승인 자원→바이옴 제약이 있으면 재사용한다. 없으면 미정/설정 초안으로 남기며 임의 배정을 정본으로 확정하지 않는다.
- 후보 범위의 bounds/biome owner/접근 요구/경계 제약을 노출해 RMAP15가 실제 footprint를 선택할 수 있게 한다.
- 공간 예약 완료 좌표나 footprint를 지금 꾸며 넣지 않는다. 고정 지형/접근/출구/충돌 소유권 확정은 RMAP15 책임이다.
- 네 패치 배치 때 특수 지역 후보 여지를 남긴다. 후속 최소 footprint가 이미 정의된 경우에만 그 크기로 후보 공간을 확인한다.
- 필요한 크기가 아직 미정이면 그 사실과 현재 후보 범위를 출력한다. 장차 특수 지역이 반드시 들어간다고 보증하지 않는다.
- RMAP13의 8개 논리 노드와 RMAP15의 8개 필수 지역을 무조건 일대일로 보지 않는다. 마을 및 봉인지/보스 통합은 RMAP15에서 매핑한다.
- 이번 배치로 확정된 제약 충돌을 숨기지 않는다. 해당 ID/빈 후보 범위/경계 조건을 보고하고 원인 범위만 조정한다.

## 9. 실제 결과 생성·표시

1. 기존 생성 요청/결과의 공개 진입점에서 RMAP12 정의+RMAP13 그래프+RMAP14 설정으로 바이옴 계획을 생성한다.
2. 테스트만 호출하는 샘플 생성기/수동 CSV를 별도 정본으로 만들지 않는다. Editor와 다음 소비부가 같은 API를 사용한다.
3. 기존 Editor/preview/exporter를 재사용해 전체 52×52 계획을 실제 좌표로 표시한다. 부족한 표시 기능만 최소 추가한다.
4. 전체 바이옴 배치도에 네 색상/이름 범례, 월드 bounds/좌표, patch ID/범위, 승인 경계 source 연결을 표시한다.
5. Tile 기준 624:416 = 3:2 비율을 유지한다. 12×8 청크를 정사각 타일로 오해하게 그리지 않는다.
6. 프로필 배치도 또는 같은 뷰의 overlay에서 OPEN/BALANCED/DENSE와 선택 목표를 확인하게 한다.
7. 확정된 바이옴 소유권과 아직 계획 중인 예약/물리 조건을 구분한다. 이미지에 실제 생성 Seed/버전을 표시한다.
8. 대표 설정은 기존 RMAP13의 1304/CONTENT_V1/GENERATOR_V1을 우선 재사용하되 실제 최신 버전과 정책을 기록한다.
9. Editor 실행/내보내기에서 읽은 결과와 CSV/API digest를 연결한다. API 출력 그림을 수작업으로 고쳐 꾸미지 않는다.
10. Scene/Player/Camera를 새로 배치할 필요는 없다. 이번 Unity-visible output은 실제 데이터의 전체 배치 표시/설정이다.

## 10. Required Outputs

아래는 GENERATED/RMAP14 산출물의 제안 이름이다. 기존 exporter 이름을 재사용하면 실제 경로와 대응을 보고한다.

| 자료 | 필수 내용 |
|---|---|
| rmap14_manifest.json | 요청/Seed/버전/pool/그래프 digest/설정/실제 API/52×52 bounds/검증 수준 |
| rmap14_patch_ownership.csv | 전체 2,704 좌표의 patch/biome 소유권과 stable 참조 |
| rmap14_patches.csv | 네 Patch ID/biome key/명칭/실제 셀 집합 참조/bounds/면적/선택 Profile |
| rmap14_boundaries.csv | 실제 인접 면 좌표/방향/양측 owner/승인 source 또는 requirement ID/호환 수준 |
| rmap14_profiles.csv | 설정 가중치/선택 결과/목표 범위/측정 scope/실제 측정 또는 pending |
| rmap14_reservation_inputs.csv | RMAP13 참조/후속 허용 후보 범위/접근·경계 제약/미정 항목 |
| rmap14_biome_layout.png | 전체 실제 바이옴 계획·경계·범례·Seed/버전 |
| rmap14_density_profiles.png | 선택 프로필과 목표/측정 상태를 구분한 전체 표시 |
| focused XML/검사 기록 | 실제 Unity filter/job/count와 새 확인/재사용 근거 |

CSV는 UTF-8/RFC4180, stable 정렬, 다중 값 escaping/참조 유효성과 round-trip을 확인한다.
이미지/CSV는 실제 생성 데이터의 파생 결과다. Authoring 원본/설정/재생성 경로를 명확히 하나로 둔다.

## 11. Focused Checks / PASS

- W05: 전체 좌표 coverage/단일 owner, 네 비어 있지 않은 patch, 계획 연결성·bounds·안정 ID·경계 참조.
- 경계: 실제 인접 면과 승인 조합/방향의 대응, 미지원 조합의 진단, 필요한 adapter 좌표 사례.
- A25: 세 정확한 초기 범위, 바이옴별 유효 가중치, 단일 양수 profile 선택, 동일 입력 결정성.
- 격리: profile/biome의 제한 재선택이 RunGraph 등 관계없는 stream/기존 ID에 영향을 주지 않음.
- 밀도: 복수 Chunk/Cluster scope의 typed 셀 합산/분모/중복 처리, 단일 4×4 탈락 기준으로 오용되지 않음.
- 경계 사례는 측정 정의를 검증할 만큼만 둔다. 무의미한 확률 빈도 시험·테스트 수 채우기 금지.
- 소비: 실제 공개 요청→계획→Editor 출력/다음 예약 입력이 연결되고 CSV/API/이미지가 일치함.
- 선행: RMAP13 논리 ID/조건 보존, 작은 Run anchor의 Full Run 좌표 오용 부재, Player/Camera 분리.
- Compile/refresh, 대표 전체 배치 생성과 표시를 확인한다. 바뀐 코드와 직접 영향 검사만 수행한다.
- 전체 바이옴 ownership 2,704행 검사는 데이터 범위 확인이다. 2,704개 청크의 물리/플레이 전수 검사를 요구하지 않는다.
- broad/full/unfiltered regression, legacy 19347/65536-mask 감사, 불필요한 build, 모든 Seed sweep은 하지 않는다.

바이옴 공간 배치/프로필 선택/승인 경계 연결/실제 소비·표시가 확인되면 이번 범위 PASS다.
전체 실제 SOLID 밀도/예약 배치/Tilemap·Player 통과는 이 Task의 PASS로 인증하지 않는다.
상태·해시 충돌은 STATUS_CONFLICT, 필수 자료/도구 부재는 BLOCKED, 구현 기준 불충족은 FAIL이다.

## 12. Required Result / Commit / STOP

```text
TASK: RMAP14_BIOMES
STATUS: PASS / FAIL / BLOCKED / STATUS_CONFLICT
USER-FACING REPORT: 실제 네 바이옴 위치·밀도 선택·표시, 아직 미구현인 예약/Tilemap/통과
RESPONSIBILITY: 실제 경로 | 추가/수정 | KEEP/ADAPT/NEW 이유 | 소유/비소유 책임
PRECONDITIONS: branch/HEAD, inbox 외부/실제 SHA, 선행 Task/Archive/Result SHA, RMAP13 실제 Finalize/commit
CHANGED: 실제 바인딩/API/설정 원본·재생성 경로/호출자/소비 연결
W05 EVIDENCE: bounds/2704 ownership/네 패치/연결·경계·승인 source/공간 출력
A25 EVIDENCE: 초기 범위/가중치·선택/profile 버전/metric scope/실제 측정 또는 PENDING_GEOMETRY
RMAP13/15 HANDOFF: 논리 ID/예약 후보 범위·제약/검증 수준/미정 및 충돌
VALIDATION: 실제 Unity version/filter/job/count/XML, 직접 영향/재사용/실패·수리/미실행
UNITY VISIBLE OUTPUT: 실제 Editor 경로·설정/배치도/프로필 그림/API·CSV digest 일치
OUT-OF-SCOPE: RMAP15 예약·고정 지형, RMAP16 Cluster, RMAP17 Bake와 Player 통과 등
FINAL EVIDENCE: 실제 상태, Task/Archive bytes 동일, 필수 미확인 유무
COMMIT: 시작 HEAD, 실제 commit SHA 또는 아직 미생성인 단계/이유
NEXT: RMAP15_SPECIALS LOCKED / NOT STARTED
```

PASS Result 후 기존 Finalize로 RMAP14 CURRENT→COMPLETE, Current→NONE만 수행한다.
예상 적용 후 234 COMPLETE / 1 CURRENT / 5 LOCKED, 완료 후 235 COMPLETE / 0 CURRENT / 5 LOCKED다.
실제 이력이 다르면 이유를 보고하고 상태를 숫자에 억지로 맞추지 않는다.
Task/Archive/Result/허용 코드·설정·자료·상태 변경만 commit한다. 무관한 staged 변경을 섞거나 임의 unstage하지 않는다.
최종 CLI에 Result/설치 Task SHA, 실제 commit SHA, Current와 RMAP15 LOCKED를 보고한다.
Result가 commit 전에 작성됐다면 실제 commit 근거는 최종 CLI 보고로 남긴다. 자기참조 해시 때문에 문서를 다시 쓰지 않는다.
RMAP15 실행과 git push 없이 STOP한다.
