```yaml
mcp_patch:
  format: single_task_v1
  task_id: RMAP10_SMALL_RUN
  task_file: TASKS/RMAP10_SMALL_RUN.md
  requires_current_task: NONE
  requires_completed_task: RMAP09_COMPOSER
  requires_result:
    path: REPORTS/RMAP09_COMPOSER_RESULT.md
    status: PASS
    sha256: b7710e2062ae70adfba9dc138181531e6a1ceba8598ad5a959b15c3c3da2ef07
  requires_installed_task:
    path: TASKS/RMAP09_COMPOSER.md
    sha256: e9819a2dd3962ab32f1c3872fda23251d8be280198b9007f59a6e47729d16384
  sets_current_task: RMAP10_SMALL_RUN
```

# RMAP10_SMALL_RUN - 가변 생성 맵과 실제 플레이 Gate

```text
TASK: RMAP10_SMALL_RUN
DOCUMENT: v4.2 / RMAP09 PASS 이후 실행 지시서 / 2026-09-08
STATUS: CURRENT
INPUT: MapDesign/MCP_INBOX/RMAP10_SMALL_RUN.md
EXPECTED_RESULT: MapDesign/MCP/REPORTS/RMAP10_SMALL_RUN_RESULT.md
NEXT: RMAP11_POOL500
NEXT STATUS: LOCKED / DO NOT START
```

위 CURRENT는 정상 Apply 이후의 상태다. 문서 발행만으로 저장소 상태를 바꾸지 않는다.
약 1~2시간의 기능 책임 단위이며 문서는 300줄 이하로 운영한다.

## 1. User-Facing Goal / 이번 작업의 완료 모습

기존 Seed/Recipe/재생성 도구에서 Seed와 작은 맵 크기를 바꾸어 실제 플레이 씬을 다시 만든다.
가로는 12, 세로는 8의 배수이며 12x8 청크 안에는 RMAP07의 4x4 패턴 6개가 조립된다.
대표 3개 Seed의 생성/필수 연결을 확인하고, 그중 대표 맵에서 Production Player로 Start부터 실제 Exit까지 플레이한다.
현재 Seed/크기/Recipe와 서로 다른 결과를 확인할 수 있다. 실패하면 위치/원인/제한 재선택 이력을 표시한다.
이번 요구 ID는 E03, E04, E05, E06, E07이다. 아래에 전체 구현 조건을 포함한다.
RMAP11의 500개 보강과 대규모 월드/콘텐츠 생성은 시작하지 않는다. RMAP10이 작은 실제 생성 맵 Gate를 소유한다.

## 2. Preflight / 선행 완료와 정상 single_task_v1 적용

1. 프로젝트 루트/적용 AGENTS.md와 MapDesign/MCP/00_MCP_ENTRYPOINT.md를 확인한다.
2. 기존 Apply/ChangeControl/Finalize와 RMAP/02_PROTOCOL_V4_2.md에서 지원 필드/경로를 확인한다.
3. YAML 내부 경로는 MapDesign/MCP 기준, 본문 Assets/MapDesign 경로는 프로젝트 루트 기준이다.
4. 전달 메시지의 외부 SHA-256을 inbox 파일 전체 원본 bytes의 SHA와 대조한다.
   파일 자체의 expected SHA는 자기참조를 피하려고 본문에 넣지 않는다. 이전 planned 기획 문서의 해시와 비교하지 않는다.
   외부 값 부재/불일치 또는 현지 별도 manifest/등록 SHA 충돌이면 적용 전에 경로와 expected/actual을 보고하고 중단한다.
   줄바꿈 정규화/재저장으로 해시를 맞추거나 적용기 규칙을 수정하지 않는다.
5. RMAP09 installed Task/Archive의 bytes/SHA와 PASS Result의 TASK/STATUS/SHA를 YAML과 대조한다.
6. RMAP09 Result는 Phase C Finalize/Phase D commit 전에 작성됐다. 그 문구를 실제 완료 증거로 간주하지 않는다.
   저장소에서 RMAP09 COMPLETE, Current NONE, RMAP10~19 LOCKED 및 Result 경로의 git log로 실제 RMAP09 commit을 확인한다.
   선행 Finalize/commit 미완료면 남은 단계와 근거를 보고하고 STOP한다. 이번 작업에서 선행 상태/Result를 임의 수정하지 않는다.
7. 정상 시작 기준은 240행 = 230 COMPLETE / 0 CURRENT / 10 LOCKED다. 차이가 있으면 실제 이력과 현지 프로토콜로 판단한다.
8. 미적용 inbox 후보 본 MD 1개, RMAP10 등록 ID와 다른 CURRENT 부재를 확인한다. 선행 변경/Task-Archive 불일치면 중단한다.
9. 정상 Apply로 RMAP10만 LOCKED->CURRENT, Current NONE->RMAP10_SMALL_RUN을 수행하고 Task/Archive를 바이트 동일하게 설치한다.
10. 같은 RMAP10 CURRENT 재개는 설치 Task/Archive/입력 동일성과 기존 결과를 확인하고 현지 재개 규칙을 따른다.
11. 이미 RMAP10 COMPLETE+유효 PASS면 기존 결과를 보고하고 STOP한다. RMAP11을 자동으로 열지 않는다.
12. 무관한 변경은 보존한다. 관련 변경을 분리할 수 없으면 근거를 보고하며 reset/stash/강제 덮어쓰기를 하지 않는다.

## 3. Read Allowlist / 실제 Run 연결점

- MapDesign/MCP/RMAP/{00_BASELINE_V4_2,01_SEQUENCE_V4_2,02_PROTOCOL_V4_2}.md 및 현지 운영 문서.
- MapDesign/MCP/{MASTER_IMPLEMENTATION_TASK_LIST,06_IMPLEMENTATION_STATUS}.md와 RMAP09 Task/Archive/Result.
- MapDesign/MCP/GENERATED/RMAP01/file_bindings.csv의 RUN_GENERATOR / EDITOR / BAKE / PLAYER / COMPOSER 행과 실제 코드/직접 호출자/테스트.
- 기존 Seed/Recipe/재생성 도구, Run 생성/제한 재선택, 실제 Tilemap Bake/Player 배치/출구 판정의 필요한 구현.
- RMAP06 이동 결과와 RMAP07~09의 catalog/Port/조립 요청·결과·시도 기록 및 실제 profile/Collider/Overlay 증거.
- GeneratedTraversalProfileCatalog와 기존 생성 지형 이동 판정/그래프 입력 계약. 과거 전체 Run/CSV/회귀를 재감사하지 않는다.

| 실제 연결점 | 상태/확인 근거 | 이번 소비/적응 경계 |
|---|---|---|
| Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/RmapComposer.cs | EXISTING: RMAP09 | 실제 request/result/attempt trace API를 확인하고 Small Run에 필요한 입력 범위만 확장 |
| Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/RmapPatternCatalog.cs | EXISTING: RMAP07~09 | 첫 48개 후보/최종 16셀/역할/출처/Transform 조회 |
| Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/RmapPortCatalog.cs | EXISTING: RMAP08/09 | Type/공간 상태/전체 Port 좌표/방향/내부 연결 계약 |
| Assets/_Game/Live/Editor/RMAP09/CharacterLiveComposerLabSceneBuilder.cs | EXISTING: RMAP09 | 실제 Base TilemapCollider2D와 별도 ladder Overlay 표시 방식 참조 |
| Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab 및 Live rig/input/movement | 기존 Player 바인딩으로 현지 확인 | Production Player/입력/Collider 배치. 검사 전용 대체 Player 금지 |
| 기존 Seed/Recipe 도구, Bake와 Exit 판정 | 별칭에서 현지 해석 필요 | 기존 API 우선, 실제 필요한 누락 연결만 추가 |
| Assets/_Game/Map/Scenes/MoonPalace/RMAP10/MoonPalaceSmallRun_RMAP10.unity | PROPOSED | 가변 생성 결과의 저장 플레이 씬 |

RMAP09에서 확인된 것은 단일 Type3 보호 보행/등반 fixture와 3번의 제한 시도다. 임의 Type/Seed/크기/출구 지원으로 확대 해석하지 않는다.
같은 Type3 시험 방을 반복 배치하거나 Seed 라벨만 바꾼 결과를 가변 생성기로 보고하지 않는다.
필요한 Type/Port 요청 처리를 기존 composer/생성 선택 경로에 좁게 적응한다. 고정 fixture 성공 기록은 새 Geometry의 이동 증거가 아니다.
GeneratedTileMovementGraphBuilder는 SectorCanvas/GeneratedSlice 계약을 요구한다고 보고됐다. 실제 필요/입력을 확인하고 계약을 흉내 내거나 전역 Solver를 복제하지 않는다.
별칭/추측 경로를 기존 구현으로 단정하지 않는다. 실제 경로/API와 KEEP/ADAPT/NEW 이유를 Result에 남기고 수정한다.
RMAP07~09 CSV는 builder 소유 파생 자료다. 원본처럼 직접 편집하지 않고 기존 Seed/버전/ID 및 원본 후보를 보존한다.

## 4. Write Allowlist / 변경 경계

- 기존 Seed/Recipe 도구의 작은 Run 설정과 생성->조립->Bake->Player/Exit 배치 연결.
- 가변 크기/필요 Type·Port를 지원하기 위한 기존 RmapComposer/Run 생성/로컬 연결 판정의 최소 적응과 직접 소비부.
- 작은 Run의 실제 Base/Overlay Tilemap·Collider 적용, 기존 Player 배치와 실제 Exit 판정 연결.
- 출구 판정이 없으면 Player의 실제 도착을 확인하는 최소 컴포넌트만 추가한다. 보상/씬 전환/저장 진행 시스템은 만들지 않는다.
- RMAP10 전용 scene/builder/설정/재생성·실패 표시와 필요한 Tile/prefab/.meta.
- 기존 assembly의 focused 생성/조립 경계/Bake/재현성 테스트와 대표 Start->Exit PlayMode 확인.
- MapDesign/MCP/GENERATED/RMAP10의 설정/생성/실패/Seed별 판정/실제 플레이 증거, 지정 Task/Archive/Result와 RMAP10 상태 기록.
- asmdef는 컴파일에 필요한 최소 참조만 수정한다. 기존 씬/Player prefab과 무관한 변경을 임의 덮어쓰지 않는다.

첫 풀을 사용하고 500개 확충을 선행조건으로 두지 않는다. 새 package/운영 프로토콜/과거 Result/전체 설정 재작성은 범위 밖이다.
정적 생성 데이터는 Live Player/Camera 인스턴스에 의존하지 않는다. 생성 상태와 플레이 변경 상태를 분리한다.

## 5. E03 / 실제 생성 지형의 연결

- Type 1~4의 Port 좌표 연결과 일방통행을 실제 조립 지형에 반영한다. 같은 Type 숫자/Side 접촉만으로 통과를 선언하지 않는다.
- Type1=LR, Type2=LD/RD/LRD, Type3=LU/RU/LRU, Type4=UD/LUD/RUD/LRUD다. Type4는 U,D 모두 필수다.
- L/R의 y=0..7, U/D의 x=0..11 좌표 전체와 청크 origin을 사용한다. 실제 교집합과 Collider 여유/착지/방향 조건을 확인한다.
- IN/OUT/BOTH와 내부 입구->출구 관계를 유지한다. Type2 낙하/Type3 상승을 자동 왕복으로 만들지 않는다.
- 개방형 Type0은 일반 입구 하나인 막다른 공간이며 ACTIVE일 수 있다. 폭 3칸 입구도 독립 입구 하나다.
- 봉인형 Type0은 내부 공간+BreakableAccess가 있고 일반 입구가 없다. INACTIVE_SOLID와 구분하며 도구/필수 아이템 금지를 Type0 전체에 일괄 적용하지 않는다.
- 봉인된 공간 접근의 미구현 조건은 명시한다. 작은 Gate를 위해 새 파괴/도구 시스템을 만들거나 Exit를 접근 불가능한 곳에 놓지 않는다.
- 대표 생성 결과 전체에서 Type1~4/일방통행/Type0 두 형태의 적용 위치·데이터를 확인한다. 각 Seed에 모든 형태를 강제하지 않는다.
- 먼저 큰 형상/경로/예약을 정하고 Type/Port->Spine/보호->3x2 후보->별도 Overlay->Bake 순서를 사용한다.
- 청크마다 독립 방 테두리를 만들지 않는다. 이웃이 공유하는 형상/Port/보호 조건을 조립 후 실제 경계에서도 대조한다.
- 새 Geometry/Overlay/profile에서 필수 연결을 판정한다. RMAP08/09의 고정 성공 간선이나 공기 BFS만으로 생성 경로를 PASS하지 않는다.
- 필수 단절은 위치/원인을 남기고 기존 제한 재선택 범위/상한 안에서만 처리한다. 상한 소진은 실패이며 자동 터널을 만들지 않는다.

고정 시험 씬과 생성 씬은 같은 Production Player profile/Collider/발판 의미를 사용한다.
profile digest만 비교하지 말고 사용한 실제 Collider 크기/피벗/이동 조건의 바인딩도 확인한다. 생성기에 맞춰 이동 수치를 재튜닝하지 않는다.

## 6. E04 / 작은 Gate에서 제외할 콘텐츠

- 몬스터, 도박, 달돌 관제 퍼즐, 절구 연쇄, 마루, 마을 경제, 보스, 멀티플레이 실행을 넣지 않는다.
- 위 콘텐츠가 있어야만 통과 가능한 필수 경로를 만들지 않는다. 미구현 능력/장치로 통과를 가정하지 않는다.
- 최소 Start/Exit, 지형/등반 Overlay, Production Player와 기존 camera/input은 실제 Gate에 필요한 범위다.
- 콘텐츠 슬롯/태그가 기존 자료에 있어도 실제 콘텐츠를 생성하지 않는다. 기존 관련 구현을 삭제하는 작업은 하지 않는다.

## 7. E05 / 실제 Bake와 Production Player의 Start->Exit

- 각 청크의 선택된 6개 Candidate/최종 96셀/원점과 전체 맵 Base를 추적한다. 패턴 출처/Transform을 보존하고 중복 변환하지 않는다.
- 실제 선택된 SOLID/AIR/ONE_WAY_PLATFORM과 별도 LADDER/CLIMB_PILLAR 의미를 Bake에서 보존한다.
- RMAP09 fixture의 solid-only Bake를 그대로 일반화해 ONE_WAY를 누락하지 않는다. 기존 RMAP04의 위쪽 충돌면/통과 설정을 재사용한다.
- Overlay/Bake가 Base 셀을 침묵 삭제하지 않아야 한다. 실제 Tilemap 셀/Collider와 생성 Base/Overlay를 대조한다.
- 기존 Production Player prefab/rig/Collider/input을 안전한 Start에 배치한다. 저장된 시작 위치와 실제 foot pivot/몸통 여유/지지면을 확인한다.
- Exit는 실제 씬 객체와 Player 도착 판정을 가진다. 최소 필수 경로를 따라 도달할 수 있는 위치에 두고 생성 결과/manifest에 기록한다.
- 대표 맵에서 Start부터 Exit까지 연속된 입력으로 플레이하고 Exit가 실제 Player의 도착을 인식한 증거를 남긴다.
- 기존 Input System 또는 Live 입력 adapter를 통해 motor/충돌/등반을 실행한다. 순간이동/위치 덮어쓰기/마커를 이동 증거로 쓰지 않는다.
- 초기 배치/새 Run 초기화 외에 체력·낙하 상태를 임의 회복하거나 실패한 중간 구간을 건너뛰지 않는다.
- 기본 장비/체력 조건에서 필수 진행 자체가 성립해야 한다. 알려진 사망/완전 단절을 필수 연결 PASS로 보고하지 않는다.
- 낮은 천장/빡빡한 점프/불리한 선택 경로는 허용한다. 모든 구간의 무피해/복구를 전역 조건으로 강제하지 않는다.
- BFS/논리 경로/검사 마커만으로 실제 플레이 통과를 보고하지 않는다. 실제 Play 미실행은 NOT RUN/BLOCKED다.
- 실패 Seed/크기/Chunk/Pattern/Port/셀 위치/입력 구간과 원인을 기록한다. 실패를 숨기려고 평탄화/자동 굴착/검증 완화를 하지 않는다.
- 기존 연속 12x8 camera와 world bounds clamp를 생성 크기에 연결한다. 청크 경계에서 CameraRoom snap을 다시 켜지 않는다.

## 8. E06 / 대표 Seed 검사 강도

- 원문의 작은 Seed 20개 전수 Gate는 최신 사용자 수정으로 기본 강제 조건에서 제외한다. 원문을 그대로 충족했다고 표기하지 않는다.
- 기본 3개 대표 Seed의 생성/필수 연결과 그중 대표 맵 하나 이상의 실제 Start->Exit 플레이를 확인한다.
- 검사 전에 사용할 Seed/크기/Recipe를 명시한다. 실패한 Seed를 조용히 바꿔 성공한 결과만 남기지 않는다.
- 세 사례에는 최소 두 유효 크기를 포함한다. 작은 크기 예시는 36x24와 48x24이며 실제 채택값/지원 범위를 기록한다.
- 각 사례에 생성 결과/digest, Type/Port/필수 방향 연결, Start/Exit, 시도 수/실패, 실제 플레이 여부를 각각 기록한다.
- 같은 Seed/크기/Recipe/catalog·profile 버전의 재생성은 같은 배치/출처/결과를 만든다. 재현 확인은 새 Seed 전수 검사로 확대하지 않는다.
- 다른 Seed가 실제 후보 선택/배치에 반영되는지 결과를 비교한다. 모두 같은 결과면 원인을 조사하고 Seed 라벨 변경만으로 가변 생성 PASS를 주장하지 않는다.
- 3개는 실행 기본안이다. 구체적인 실패 유형을 해소할 필요가 있으면 범위를 조정하고 대상/이유/결과를 명시한다.
- 모든 Seed 정밀 시뮬레이션, 기존 전체 회귀, 실패 때마다 Seed를 바꾸는 무한 재생성으로 확대하지 않는다.
- 실제 플레이하지 않은 나머지 맵은 생성/필수 연결 판정만 보고한다. 확인하지 않은 Seed/동작은 미확인이다.

## 9. E07 / Seed·크기·Recipe 도구와 보이는 결과

- 기존 Seed/Recipe/재생성 메뉴 또는 Editor 도구를 우선 확장한다. 기존 기능과 같은 책임의 새 도구를 중복 제작하지 않는다.
- Seed, Width, Height, Recipe를 입력/확인하고 Generate/Regenerate로 RMAP10 플레이 씬을 다시 만들 수 있어야 한다.
- Width는 12의 양의 배수, Height는 8의 양의 배수다. 지원 범위를 명시하고 잘못된/지원 밖 크기는 조용히 보정하지 말고 거절한다.
- 현재 설정, 생성 성공/실패와 이유, 사용한 seed/catalog/profile 버전을 결과와 함께 확인할 수 있게 한다.
- 재생성은 RMAP10 소유 생성 루트만 갱신한다. 이전 Tilemap/Collider/Player/Exit가 중복 남지 않고 무관한 씬 객체는 보존한다.
- 생성 실패 결과와 이전 성공 결과를 혼동하지 않도록 상태/설정/표시를 연결한다. 실패 요청을 이전 맵의 성공으로 표시하지 않는다.
- 대표 Seed별 선택/배치/형태/고체 밀도/반복을 관찰해 기록한다. 의도한 주요 경로/선택 공간도 눈으로 확인할 수 있어야 한다.
- 형태·밀도·반복 관찰을 모든 불리한 지형을 평탄화하는 품질 필터나 고정 다양성 점수 Gate로 바꾸지 않는다.
- 별도의 게임 UI/저장·로드/네트워크 시스템은 만들지 않는다. 도구 여는 경로와 조작 방법을 사용자 안내/Result에 적는다.

## 10. 입력·산출물과 연결 책임

원본/파생 자료의 권위와 재생성 경로를 명시한다. 기존 CSV/ID/Seed 규약을 재사용하고 새 자료는 RMAP10에 한정한다.

| 자료 | 필수 내용 |
|---|---|
| Run 요청 | Seed/Width/Height/Recipe, catalog/profile/설정 버전, 기존 유한 재선택 상한 |
| 생성 계획 | 큰 형상/필수·선택 경로, 청크 좌표/공간 상태/Type, 전체 Port와 내부 방향 연결 |
| 실제 조립/Bake | 청크별 6개 후보/원점/출처/96셀, 별도 Overlay, 실제 Tilemap 적용 결과 |
| Start/Exit | 생성 좌표, Production Player 배치/Collider/profile 조건, 실제 Exit 판정 접점 |
| Seed별 판정 | 생성/필수 연결 결과, 재현 digest, 실패 위치/이유/시도, 실제 플레이 여부 |
| 실제 플레이 증거 | 대표 Seed/설정, 입력 경로/기록, Start 초기 상태, Exit 도착 이벤트/관찰, 실패·미확인 |
| 화면/형태 기록 | 저장 scene/설정/재생성 방법, 대표 결과의 형태·밀도·반복과 비교 근거 |

내보낸 CSV/manifest는 참조/좌표/행 구조를 확인한다. 생성 데이터만 저장하고 실제 플레이 씬/Exit 연결을 빠뜨리지 않는다.
공개 Run 생성 입력이 Seed/크기를 실제 선택 경로로 전달해야 한다. 준비된 완성 맵을 조건별로 바꿔 반환하는 데모로 끝내지 않는다.

## 11. 구현 순서 / 실제 파이프라인 완성

1. 선행 완료/commit/SHA와 정상 Apply 후 기존 Seed 도구/Run 생성/composer/Bake/Player/Exit 바인딩을 확인한다.
2. 단일 Type3 fixture에 한정된 composer 입력 중 작은 Run에 필요한 Type/Port/이웃 처리만 확장하고 재사용 이유를 기록한다.
3. 큰 형상/경로 계획에서 실제 청크 조립/방향 연결/제한 재선택으로 이어지게 한다. 후보 셀을 굴착하지 않는다.
4. 가변 맵 크기로 Base/Overlay를 Bake하고 같은 Production Player/profile와 Start/실제 Exit를 연결한다.
5. 기존 Seed/Recipe 도구에 작은 맵 재생성/현재 설정/실패 표시를 연결한다.
6. 기본 3개 대표 Seed의 생성·필수 연결과 대표 맵의 실제 Start->Exit를 확인하고 재현된 실패만 좁게 고친다.
7. 요구 ID별 증거/사용자 수정된 Seed 검사 강도/미확인 범위를 보고하고 PASS일 때만 Finalize/commit한다.

## 12. Focused Checks / 필요한 실제 실행 증거

- EditMode: Seed/크기 입력 전달, 같은 입력 재현, 지원 밖 크기 거절, 대표 3개 Seed의 생성/필수 연결.
- EditMode: 실제 출력의 Type1~4/Type0 형태/전체 Port 좌표/일방향·내부 연결, 조립 출처와 이웃 조건.
- EditMode: 선택 Base/별도 Overlay와 Bake 일치, 유한 재선택/실패 이유, 재생성 시 잔여 데이터와 stale 성공 표시 방지.
- profile/Collider 확인: 같은 Production Player 설정을 소비하고 천장/착지/낙하 조건을 알려진 필수 실패에서 무시하지 않음.
- PlayMode 또는 실제 Play: 대표 Seed의 생성 씬을 실제 Player가 Start부터 Exit까지 입력으로 진행하고 Exit 판정이 발생함.
- 대표 생성 결과에 포함된 one-way/등반의 실제 Bake 설정을 확인한다. 기존 이동 전 항목을 모든 Seed에서 반복 재시험하지 않는다.
- 대표 결과의 형태/밀도/반복과 Seed 차이를 scene/manifest/필요한 화면 증거로 기록한다. 자동 품질 평탄화는 추가하지 않는다.
- Compile/refresh와 저장 씬 생성을 수행한다. 직접 영향 회귀만 필요한 최소 범위로 선택하고 이유를 적는다.
- broad/full/unfiltered regression, legacy 19347, 불필요한 Player build, 20개 Seed 전수 Gate, 모든 Seed 정밀 시뮬레이션은 실행하지 않는다.
- 테스트 수를 목표로 늘리지 않는다. 음성 사례의 실패 감지를 실제 필수 경로 통과 PASS로 혼동하지 않는다.

Unity 미실행/도구 부재는 NOT RUN 또는 BLOCKED다. 기본 3개 생성/연결과 대표 실제 플레이의 미확인을 이전 Task PASS로 대체하지 않는다.

## 13. Required Result / 완료 보고

```text
TASK: RMAP10_SMALL_RUN
STATUS: PASS 또는 FAIL 또는 BLOCKED 또는 STATUS_CONFLICT
USER-FACING IMPLEMENTATION REPORT: 도구 여는 법, Seed/크기/Recipe 변경·재생성, scene/조작/Exit, 남은 확장 책임
RESPONSIBILITY AND FILES: 실제 경로 | 추가/수정 | 책임 | 소유하지 않는 책임
PRECONDITIONS: HEAD/branch, 외부 expected/inbox 실제 SHA, 선행 Task/Archive/Result SHA와 실제 commit, 적용 전후 상태
CHANGED: 바인딩과 KEEP/ADAPT/NEW 이유, Type3 fixture에서 확장한 입력/연결, 원본/파생 경로, 실제 Bake/Player/Exit API
REQUIREMENT EVIDENCE: E03/E04/E05/E06/E07 각각 산출물, 검사/관찰, 판정
SEED EVIDENCE: 대표 3개 Seed/크기/Recipe, 결과/digest/필수 연결/실제 Play 여부, 재현/차이, 실패/시도/미확인
USER MODIFICATION: 20개 전수 대신 기본 3개 생성·연결 + 대표 실제 Play, 조정한 범위와 이유
PLAY EVIDENCE: 대표 Seed/실제 Production Player·Collider/profile·입력, Start부터 Exit 도착까지의 근거
VALIDATION: 실제 filter/job/count, 생성/CSV/Bake/재현 확인, Play 증거, 실패/최소 수정, 미실행과 이유
UNITY VISIBLE OUTPUT: 저장 scene/설정/현재 결과, Start/Exit/경계/실패 위치, 형태·밀도·반복 관찰
OUT-OF-SCOPE FINDINGS: 제외 콘텐츠/500 보강/대규모 생성/미검사 Seed·동작의 남은 책임
RMAP11 BINDINGS: catalog/후보 소비/Recipe/선택/재생성·검증 API의 실제 경로, EXISTING/PROPOSED 구분
FOLLOWUP: 현지 single_task_v1 경로/필드 규칙, 설치 Task 실제 SHA-256
FINAL EVIDENCE: 필수 미확인 유무, 현재 상태, Task와 Archive bytes 동일 여부
NEXT: RMAP11_POOL500 LOCKED / NOT STARTED
COMMIT: 작업 전 HEAD, 실제 생성 commit SHA 또는 아직 미생성인 단계/이유
```

## 14. PASS / Finalize / STOP

5개 요구 ID의 생성 도구/실제 맵/대표 Seed 연결/Production Player Start->Exit가 확인돼야 PASS다. 실행하지 않은 기능/검사를 PASS로 적지 않는다.
선행 상태/해시 충돌은 STATUS_CONFLICT, 필수 자료/도구 부재는 BLOCKED, 기능/확인 기준 불충족은 FAIL로 보고한다.
필수 유일 경로의 알려진 완전 단절은 실패다. 제한 재선택으로 원인을 해결하고 자동 굴착/침묵 수리/검증 완화로 숨기지 않는다.
PASS Result 후 기존 Finalize로 RMAP10 CURRENT->COMPLETE, Current->NONE만 수행한다. RMAP11~19는 LOCKED다.
기준선 상태는 적용 후 230 COMPLETE / 1 CURRENT / 9 LOCKED, 완료 후 231 COMPLETE / 0 CURRENT / 9 LOCKED다.
Task/Archive/Result/허용 코드/asset/CSV/증거/상태 변경만 commit한다. 무관한 staged 변경을 포함하거나 임의 unstage하지 않는다.
commit 메시지: RMAP10 implement seeded small run and real player exit gate
최종 CLI 보고에 Result 경로, 실제 생성 commit SHA, Result SHA-256, 설치 Task SHA-256, Current NONE, RMAP11 LOCKED를 적는다.
Result를 commit 전에 작성했다면 실제 commit 증거는 최종 CLI 보고로 남긴다. 해시 기입을 위해 선행/설치 문서를 다시 쓰지 않는다.
Git push와 RMAP11 실행 없이 STOP한다.
