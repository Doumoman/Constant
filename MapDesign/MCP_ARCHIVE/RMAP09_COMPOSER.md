```yaml
mcp_patch:
  format: single_task_v1
  task_id: RMAP09_COMPOSER
  task_file: TASKS/RMAP09_COMPOSER.md
  requires_current_task: NONE
  requires_completed_task: RMAP08_PORTS
  requires_result:
    path: REPORTS/RMAP08_PORTS_RESULT.md
    status: PASS
    sha256: b14ae1ded7c4ee62846c03819fa3808188b4938626cda26083d010eb92db5204
  requires_installed_task:
    path: TASKS/RMAP08_PORTS.md
    sha256: e2aa3586d09ff87d18144a24c21100dd9168081d764bdb4b8b5ce72efd95cdac
  sets_current_task: RMAP09_COMPOSER
```

# RMAP09_COMPOSER - 청크 내부 패턴 조립과 보호

```text
TASK: RMAP09_COMPOSER
DOCUMENT: v4.2 / RMAP08 PASS 이후 실행 지시서 / 2026-09-08
STATUS: CURRENT
INPUT: MapDesign/MCP_INBOX/RMAP09_COMPOSER.md
EXPECTED_RESULT: MapDesign/MCP/REPORTS/RMAP09_COMPOSER_RESULT.md
NEXT: RMAP10_SMALL_RUN
NEXT STATUS: LOCKED / DO NOT START
```

위 CURRENT는 정상 Apply 이후의 상태다. 문서 발행만으로 저장소 상태를 바꾸지 않는다.
약 1~2시간의 기능 책임 단위이며 문서는 300줄 이하로 운영한다.

## 1. User-Facing Goal / 이번 작업의 완료 모습

RMAP07의 실제 4x4 후보 6개를 3x2로 골라 12x8 청크를 조립하고 Unity에서 Player로 대표 필수 연결을 통과한다.
큰 형상/주요 경로, Type/Port, Route Spine/이동 보호 영역이 후보 선택보다 먼저 반영되고 등반 Overlay는 Base와 분리된다.
필수 연결 실패는 어느 Pattern/Port/Chunk/이동 조건 때문인지 표시하며 정해진 횟수 안에서 해당 범위만 재선택한다.
빡빡한 점프와 불리한 선택 경로는 허용한다. 알려진 필수 유일 경로 단절을 굴착/검증 완화로 성공처럼 만들지 않는다.
이번 요구 ID는 P04, P19, A22, A23, A24다. 아래에 전체 구현 조건을 포함한다.
작은 조립 fixture와 경계를 공유하는 인접 사례까지만 확인한다. RMAP10 Small Run, 최종 500 보강과 월드 생성은 시작하지 않는다.

## 2. Preflight / 선행 완료와 정상 single_task_v1 적용

1. 프로젝트 루트/적용 AGENTS.md와 MapDesign/MCP/00_MCP_ENTRYPOINT.md를 확인한다.
2. 기존 Apply/ChangeControl/Finalize와 RMAP/02_PROTOCOL_V4_2.md에서 지원 필드/경로를 확인한다.
3. YAML 내부 경로는 MapDesign/MCP 기준, 본문 Assets/MapDesign 경로는 프로젝트 루트 기준이다.
4. 전달 메시지의 외부 SHA-256을 inbox 파일 전체 원본 bytes의 SHA와 대조한다.
   파일 자체의 expected SHA는 자기참조를 피하려고 본문에 넣지 않는다. 이전 planned 기획 문서의 해시와 비교하지 않는다.
   외부 값 부재/불일치 또는 현지 별도 manifest/등록 SHA 충돌이면 적용 전에 경로와 expected/actual을 보고하고 중단한다.
   줄바꿈 정규화/재저장으로 해시를 맞추거나 적용기 규칙을 수정하지 않는다.
5. RMAP08 installed Task/Archive의 bytes/SHA와 PASS Result의 TASK/STATUS/SHA를 YAML과 대조한다.
6. RMAP08 Result는 Phase C Finalize/Phase D commit 전에 작성됐다. 그 문구를 실제 완료 증거로 간주하지 않는다.
   저장소에서 RMAP08 COMPLETE, Current NONE, RMAP09~19 LOCKED 및 Result 경로의 git log로 실제 RMAP08 commit을 확인한다.
   선행 Finalize/commit 미완료면 남은 단계와 근거를 보고하고 STOP한다. 이번 작업에서 선행 상태/Result를 임의 수정하지 않는다.
7. 정상 시작 기준은 240행 = 229 COMPLETE / 0 CURRENT / 11 LOCKED다. 차이가 있으면 실제 이력과 현지 프로토콜로 판단한다.
8. 미적용 inbox 후보 본 MD 1개, RMAP09 등록 ID와 다른 CURRENT 부재를 확인한다. 선행 변경/Task-Archive 불일치면 중단한다.
9. 정상 Apply로 RMAP09만 LOCKED->CURRENT, Current NONE->RMAP09_COMPOSER를 수행하고 Task/Archive를 바이트 동일하게 설치한다.
10. 같은 RMAP09 CURRENT 재개는 설치 Task/Archive/입력 동일성과 기존 결과를 확인하고 현지 재개 규칙을 따른다.
11. 이미 RMAP09 COMPLETE+유효 PASS면 기존 결과를 보고하고 STOP한다. RMAP10을 자동으로 열지 않는다.
12. 무관한 변경은 보존한다. 관련 변경을 분리할 수 없으면 근거를 보고하며 reset/stash/강제 덮어쓰기를 하지 않는다.

## 3. Read Allowlist / 실제 조립 연결점

- MapDesign/MCP/RMAP/{00_BASELINE_V4_2,01_SEQUENCE_V4_2,02_PROTOCOL_V4_2}.md 및 현지 운영 문서.
- MapDesign/MCP/{MASTER_IMPLEMENTATION_TASK_LIST,06_IMPLEMENTATION_STATUS}.md와 RMAP08 Task/Archive/Result.
- MapDesign/MCP/GENERATED/RMAP01/file_bindings.csv의 COMPOSER / SPINE / ENVELOPE / PATTERN / PORT / OVERLAY 행과 실제 코드/직접 호출자/테스트.
- 공유 Player/이동 profile, RMAP08 Type/Port/내부 방향 조건, RMAP07 첫 풀/출처/특성/충돌 자료.
- 기존 국소 후보 선택/결정적 재선택 경로와 GeneratedTraversalProfileCatalog / GeneratedTileMovementGraphBuilder의 필요한 판정 API.
- RMAP02~06의 실제 점프/Collider/Grab/climb/one-way/fall 의미와 직접 필요한 증거. 과거 전체 결과를 재감사하지 않는다.

| 실제 연결점 | 상태/확인 근거 | 이번 소비 경계 |
|---|---|---|
| Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/RmapPatternCatalog.cs | EXISTING: RMAP07/08 | BuildInitialPool(), TryGetCandidate(), BaseCells/Role/Origins/Characteristics 조회 |
| Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/RmapPortCatalog.cs | EXISTING: RMAP08 | BuildFixture()/RmapPortCatalogSnapshot의 Type/공간 상태/Port/내부 연결 계약 참조 |
| GeneratedTraversalProfileCatalog | EXISTING: RMAP08 | 공유 profile/digest와 실제 이동 조건. 현지 실제 경로/API 확인 |
| GeneratedTileMovementGraphBuilder | EXISTING: RMAP08 | 기존 이동 판정 중 로컬 조립 검증에 재사용 가능한 범위 확인. 전역 Solver 복제 금지 |
| Assets/_Game/Live/Editor/RMAP08/CharacterLivePortLabSceneBuilder.cs | EXISTING: RMAP08 | 실제 TilemapCollider2D/Player/등반 fixture 구성 방식 참조 |
| Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/RmapComposer.cs | PROPOSED: RMAP08 | 기존 composer로 부족한 3x2 조립/보호/국소 재선택 책임만 추가 |
| Assets/_Game/Map/Scenes/MoonPalace/RMAP09/MoonPalaceComposerLab_RMAP09.unity | PROPOSED | 실제 조립 지형, 필수/선택 경로, 실패 위치 표시 |

별칭/PROPOSED 이름을 기존 구현으로 단정하지 않는다. 실제 경로/API를 읽고 KEEP/ADAPT/NEW와 이유를 Result에 남긴다.
RMAP08 BuildFixture()는 고정 시험 지형의 자료다. 그 지형의 성공 기록을 새로 조립한 96셀/Overlay의 성공 기록으로 복사하지 않는다.
새 Geometry에 필요한 범용 입력/판정 접점이 없으면 기존 모델/판정기를 좁게 적응한다. 필요성이 없는 새 전역 Solver/Task별 Service는 만들지 않는다.
RMAP07/08 CSV는 builder 소유 파생 자료다. 원본처럼 직접 수정하지 않으며 Candidate ID/원본/Transform 및 미해결 태그 충돌을 보존한다.
긴 파일은 rg로 심볼을 찾고 필요한 구간만 읽는다. 생성 데이터가 Live Player/Camera 인스턴스에 의존하게 하지 않는다.

## 4. Write Allowlist / 변경 경계

- 바인딩으로 확인한 청크 composer, Spine/이동 보호 영역, 역할/후보 선택, 등반 Overlay와 기존 국소 재선택 경로.
- 실제 조립 Geometry를 입력받는 데 필요한 기존 Port/로컬 이동 판정 API의 최소 적응과 직접 소비부.
- RMAP09 조립 요청/레시피, 선택 결과/출처/실패 자료와 CSV/API. 원본/파생 경로와 재생성 방법을 명시한다.
- RMAP09 전용 scene/builder/표시/fixture manifest 및 필요한 Tile/prefab/.meta. 기존 scene/Player prefab은 임의 교체하지 않는다.
- 기존 assembly 안의 focused 조립/보호/판정/재선택 테스트와 대표 실제 Player/Tilemap PlayMode 확인.
- MapDesign/MCP/GENERATED/RMAP09의 조립 단계/시도/출처/검증 증거, 지정 Task/Archive/Result와 RMAP09 상태 기록.
- asmdef는 컴파일에 필요한 최소 참조만 수정한다. 기존 Seed/버전/ID 체계와 원본 데이터는 보존한다.

이번에는 첫 풀을 소비한다. 500개 확충/무단 후보 원본 변경, 전역 방 추첨/전역 경로 재생성/Small Run 구현은 범위 밖이다.
Player 이동/Collider/체력/fall 규칙을 생성 결과에 맞춰 재튜닝하지 않는다. package/운영 프로토콜/과거 Result 수정은 하지 않는다.

## 5. P04 / 필수 점프 난도와 최소 연결

- 1~2타일 일반 이동, 3타일 드문 이동, 4타일 선택/지름길 중심은 잠정 제작 선호다. 강제 빈도/전역 PASS 규칙으로 만들지 않는다.
- 필수 연결은 실제 profile의 몸통/머리 공간, 이동 궤적과 착지면을 고려해 기본 가능성을 판단한다.
- 천장이 막은 점프를 수평 거리만 보고 통과로 표시하지 않는다. 출발/착지 사이 공기 BFS만으로 점프 가능성을 선언하지 않는다.
- 빡빡한 점프, 불리한 배치, 불편한 우회와 복구 없는 선택 경로를 허용한다. 모든 선택 공간을 평탄화하지 않는다.
- 필수 Port/내부 방향 연결에 대해 실제 선택된 Base+Overlay에서 이동 증거를 만든다. RMAP07 태그/역할은 그 증거가 아니다.
- 알려진 필수 유일 경로의 완전 단절은 실패다. 필수 경로의 미확인 조건은 성공으로 기본값 처리하지 않는다.
- 어려움/선택 경로 실패와 필수 진행 불가능을 분리해 원인/위치를 기록한다. 모든 후보의 쉬운 왕복을 요구하지 않는다.

## 6. P19 / 무피해·복구 보장의 범위

- 필수 진행 무피해 해법과 Grab/등반축/중간 착지/우회/감속 장치 예시는 제작 수단으로 유지한다.
- 모든 구간의 무피해/복구를 전수 PASS 조건으로 강제하지 않는다. 미구현 감속 장치 등을 새로 만들어 통과시키지 않는다.
- 기본 장비와 체력 조건에서 필수 진행 자체가 성립해야 한다. 검증 입력에 사용한 장비/초기 체력/profile 조건을 명시한다.
- 낙하가 있는 대표 필수 경로는 기존 낙하 거리/피해/Grab·등반 reset 의미로 판단한다. 알려진 사망 경로를 기본 진행 성공으로 기록하지 않는다.
- 검사한 경로 범위의 체력 조건을 기록한다. 청크마다 임의 회복시키거나 로컬 확인을 전체 월드 생존 증명으로 확대하지 않는다.
- 선택 경로의 피해/복구 부재는 허용할 수 있으며 그 조건을 표시한다. 선택 난도를 없애려고 Base를 후처리하지 않는다.

## 7. A22 / 생성 순서와 경계 공유

아래 순서를 실제 호출/자료 의존성으로 구현한다. 생성 완료 후 Spine/보호 표시만 덧붙인 결과는 해당하지 않는다.

1. 예약 공간과 큰 형상/주요 경로 입력을 먼저 확정한다.
2. 그 조건에 맞는 청크 Type/전체 Port 좌표/내부 필수 방향 관계를 정한다.
3. 내부 Route Spine과 profile 기반 이동 보호 영역을 만든다.
4. 3x2 Pattern 슬롯의 역할을 정하고 보호/이웃 조건에 맞는 후보를 선택한다.
5. 선택된 Base를 유지하면서 사다리/기둥 등반 Overlay를 배치한다.
6. 실제 조립 Base+Overlay의 필수 이동을 확인하고 Bake/물리 표시로 연결한다.

- 큰 형상/주요 경로는 입력으로 받을 수 있어야 한다. 이번에는 고정 소규모 요청을 사용하며 대규모 형상 생성기는 만들지 않는다.
- 경계를 넘는 큰 형상/예약/주요 경로 조건은 인접 청크가 같은 원점/좌표/연결 약속을 공유한다.
- 청크마다 독립 방 테두리를 둘러 막거나 완성 12x8 방 하나를 독립 추첨하지 않는다.
- 3x2 슬롯은 x=0,4,8과 y=0,4 원점의 4x4 패턴이다. 겹침/누락 없이 6개 후보가 96개 Base 셀을 정확히 채운다.
- 실제 선택된 Candidate ID, 최종 16셀, 슬롯 원점, 원본/Transform을 추적한다. 이미 변환된 후보를 중복 변환하지 않는다.
- RMAP08 Type/Port/방향 계약을 실제 출력에서 재검증한다. 단순 접촉/한 셀 교집합을 자동 왕복/내부 완전 연결로 바꾸지 않는다.
- INACTIVE_SOLID/SPECIAL_RESERVED는 공간 상태 계약을 따른다. 그곳을 ACTIVE 조립 슬롯처럼 채워 예약/봉인을 덮지 않는다.

## 8. Spine / 이동 보호 영역 / 역할·후보 선택

- Spine은 필수 입구->출구의 방향/이동 의도를 나타낸다. 선 하나의 시각화나 거리 합만으로 이동 증명이 되지 않는다.
- 공유 profile로 몸통/머리 여유, 출발/착지 지지면, 이동 종류에 필요한 영역/조건을 구분한다.
- 비워야 할 공간과 필요한 지지면을 별도 제약으로 표현한다. 모든 Spine 주변을 AIR로 밀어 착지면을 없애지 않는다.
- 보호 요구를 4x4 슬롯별 좌표와 이웃 문맥으로 연결한 뒤 역할/후보를 필터링한다. 보호 영역을 후보 결과에 맞춰 사후 축소하지 않는다.
- RMAP07의 로컬 특성은 조기 필터로 재사용하되 ContextRequired는 실제 조립 지형/이웃으로 해소한다.
- 출처의 UnresolvedDirectionalTags/review-required를 그대로 남긴다. 의도 태그만으로 필수 사용 가능성을 확정하지 않는다.
- 선택된 Base+Overlay와 출발/도착/profile/이웃 조건을 입력으로 기존 이동 판정 경로를 재사용한다. 고정 fixture 간선을 복사하지 않는다.
- 필수 성공/실패/미확인과 선택 경로 상태를 구분한다. Geometry나 profile이 바뀌면 이전 성공 증거를 무효화하고 필요한 범위만 재검사한다.

## 9. A23 / Overlay와 보호 강도

- LADDER / CLIMB_PILLAR는 Base Geometry와 분리된 등반 Overlay로 배치한다.
- 이번 첫 구현의 Overlay에 의한 Base 변경 범위는 없음이다. 고체를 침묵 삭제하거나 SOLID/ONE_WAY를 AIR로 바꾸지 않는다.
- 고체/예약/필수 공간과 충돌하면 Overlay 위치/후보를 해당 범위 안에서 재선택하거나 실패한다.
- 실제 기존 구현에서 Base 변경 필요가 발견되면 경로/이유를 명시한다. 이 작업에서 예외를 자동 도입하지 않는다.
- 등반축의 진입/이탈/머리 공간과 목표 Port까지의 연결을 기존 RMAP04 이동 의미로 확인한다. Overlay 존재만으로 등반 성공을 선언하지 않는다.
- Pattern 역할/후보는 필수 이동 보호와 이웃 조건에 맞게 선택한다. 장식/선택 공간 전체에 같은 보호 강도를 강제하지 않는다.
- one-way Base의 충돌면은 변환 여부와 무관하게 월드 위쪽이다. 등반 Overlay와 one-way 의미를 혼합하지 않는다.
- Base 조립 직후와 Overlay/Bake 이후의 96셀을 대조해 동일성을 확인한다. 표시 지형도 선택 후보의 셀을 충실히 반영한다.

## 10. A24 / 제한 재선택과 금지된 수리

- 완성 12x8 방의 독립 추첨, 자동 터널, 이유 없는 무한 Run 재생성, 검증 기준 완화는 사용하지 않는다.
- 몬스터/장치로만 성립하는 필수 경로를 기본 연결로 사용하지 않는다. 부재한 능력/아이템을 검증기에서 가정하지 않는다.
- 실패에 ChunkId/Slot 또는 PatternId/PortId/셀 위치/제약/판정 이유를 연결한다. 원인을 좁혀 기존 국소 재선택 경로를 사용한다.
- 후보/슬롯이 원인이면 해당 범위부터 다시 선택한다. Port/Chunk 변경이 필요하면 명시된 허용 범위에서만 수행하고 공유 이웃 조건을 다시 검증한다.
- 필수 Port/공간 상태/큰 형상 제약을 몰래 바꿔 성공시키지 않는다. 공유 조건을 깨는 변경은 거절하거나 이유와 함께 실패한다.
- 유한한 후보 평가/국소 재시도 상한을 요청 또는 기존 설정에 명시한다. 실제 사용한 상한/시도 수를 로그에 남긴다.
- 상한이 없거나 잘못된 입력은 거절한다. 후보가 없거나 상한 소진이면 명시적 실패로 종료한다. 시드를 계속 바꿔 재시작하지 않는다.
- 같은 입력/seed/catalog·profile 버전은 같은 선택/실패와 시도 순서를 만든다. 기존 결정적 선택/Seed 계약을 재사용한다.
- 성공 결과만 보존해 실패를 숨기지 않는다. 재현 가능한 입력과 최소 선택/실패 추적을 남기고 전체 후보 전수 로그는 피한다.
- 입력 배열/원본 후보를 가변 참조로 오염시키지 않는다. 한 실패 시도가 다음 시도의 Base를 침묵 수정하지 않게 한다.

## 11. 데이터/API와 조립 산출물

기존 원본/CSV 규칙을 재사용하고 새 자료가 필요하면 RMAP09 범위에 한정한다. 생성 입력과 파생 snapshot의 권위를 명시한다.

| 자료 | 필수 내용 |
|---|---|
| 조립 요청/레시피 | seed/버전, catalog/profile 참조, ChunkId/origin, 공유 큰 형상/예약/이웃 조건, Type/Port/필수 방향, 유한 시도 상한 |
| Spine/보호 계획 | 필수/선택 구분, 이동 의도, 여유 공간/지지면 제약, 슬롯 좌표와 이웃 조건 |
| Pattern 선택/출처 | 6개 슬롯의 Candidate ID/역할/원점/최종 16셀/원본·Transform 참조, 선택/제외 이유 |
| Base/Overlay | 12x8 Base 96셀과 별도 등반 Overlay, Base 불변 확인 |
| 이동 판정 | 실제 Geometry/Overlay/profile/방향/기본 장비·체력 조건, 필수 성공/실패/미확인과 선택 경로 조건 |
| 제한 재선택 기록 | 재현 입력, 실패 범위/위치/이유, 시도 순서/상한/결과, 허용 범위 내 변경 |
| fixture manifest | scene/bounds, 청크/슬롯/Port 위치, Player 시작점, 대표 필수 연결과 음성 사례 |

CSV 다중 좌표/참조 escaping과 정렬을 명시하고 round-trip한다. 누락/중복 슬롯, 알 수 없는 후보, 불일치 셀/참조는 명시적 오류다.
공개 조립 API가 위 요청을 받아 선택/실패를 반환해야 한다. 미리 정해 둔 완성 방을 돌려주거나 자료 출력만 하는 데모로 끝내지 않는다.
정적 조립과 플레이 변경 상태를 분리한다. Live Player/Camera는 표시/물리 검증 단계에서 연결한다.

## 12. Unity Composer Lab / 실제 대표 조합

전용 저장 씬의 전체 가로/세로는 각각 12와 8의 배수다. 컴포저의 실제 출력을 Tilemap/Collider에 적용한다.
6개 슬롯 경계/Candidate ID, 필수·선택 경로, Spine/보호 영역과 실패 위치를 구분해 볼 수 있게 한다.
전시용 바닥/간격은 조립 Base와 구분하고, 검사하는 청크/경계에 지지대를 추가해 실제 실패를 숨기지 않는다.

| 사례 | 확인할 내용 |
|---|---|
| 유효한 3x2 조립 | 첫 풀에서 6개 후보를 실제 선택, 필수 Port/내부 연결, 실제 Player로 대표 통과 |
| 인접 두 청크 | 24x8 등 소규모 공유 형상 입력, 경계 Port/보호 조건 일치, 독립 방 테두리로 막히지 않음 |
| 천장에 막힌 점프 | 거리 조건만 맞는 후보의 거절/실패 위치. 천장을 삭제해 성공시키지 않음 |
| 착지면 누락 | 지지면 없는 후보의 거절, 필수 연결 실패와 가능한 국소 재선택 |
| 등반 Overlay | 실제 진입/이탈 연결, Base 불변. 고체 충돌 Overlay는 거절/재선택 |
| 불가능한 요청 | 정해진 상한 안에 원인과 함께 실패, 같은 입력에서 재현 |
| 불리한 선택 경로 | 필수 연결 성공과 선택 난도를 구분, 무피해/복구를 전역 강제하지 않음 |

대표 유효 연결과 등반은 실제 Player/Tilemap/Collider, 기존 Input System 경로로 확인한다.
논리 Bake/Preview marker/Spine 화살표를 실제 통과 증거로 대체하지 않는다. 씬 경로/조작법/사례 위치를 Result에 적는다.
인접 사례는 로컬 조립 경계 시험이다. 월드 경로 생성/출구까지의 Small Run을 구현하거나 완료로 표시하지 않는다.

## 13. 구현 순서와 Focused Checks

1. 선행 완료/commit/SHA와 정상 Apply 후 실제 바인딩/재사용 경계를 확정한다.
2. 큰 형상/예약/Port 입력과 Spine/보호 영역을 연결하고 역할/후보 선택 전에 반영한다.
3. 첫 풀의 3x2 선택, 별도 Overlay, 실제 Geometry의 로컬 필수 이동 판정과 제한 재선택을 연결한다.
4. 실제 출력/실패를 CSV/API/전용 씬에 표시하고 아래 focused 검사로 재현된 문제만 좁게 고친다.
5. 요구 ID별 근거와 난도 허용/미확인 범위를 보고하고 PASS일 때만 Finalize/commit한다.

- EditMode: 생성 순서의 데이터 의존성, 6개 슬롯/96셀/offset·출처, 공유 큰 형상/Port와 예약 보존.
- EditMode: 몸통/머리 공간/착지면, 낮은 천장과 착지 누락 음성 사례, 필수 단절/선택 난도 구분.
- EditMode: 첫 풀/role/이웃/보호 필터, ContextRequired 해소, 고정 RMAP08 성공 기록의 무조건 재사용 없음.
- EditMode: Overlay 전후/Base-Bake 셀 동일성, 고체 충돌 거절, 기존 체력·장비 조건의 필수 진행과 무피해 전역 강제 없음.
- EditMode: 후보 없음/상한 소진의 종료, 실패 위치/이유, 결정적 재선택, 원본 배열 불변, CSV round-trip/참조 오류.
- PlayMode: 조립 출력의 대표 필수 연결과 등반 Overlay 통과. 실제 머리 충돌/착지면을 포함한 실패 지형은 필요한 최소 범위로 확인.
- 음성 사례의 실패 감지는 검사의 성공 조건이다. 불가능한 필수 경로를 통과 PASS로 기록하지 않는다.
- Compile/refresh와 씬 생성은 수행한다. 직접 영향 회귀만 최소 범위로 정하고 이유를 적는다.
- broad/full/unfiltered regression, legacy 19347, 불필요한 Player build, 모든 생성 seed 전수 실행과 다음 작업 구현은 하지 않는다.
- 테스트 수를 목표로 늘리지 않는다. 초기 배치 외 teleport/검사 marker로 실제 입력/물리 동작을 대신하지 않는다.

Unity 미실행/도구 부재는 NOT RUN 또는 BLOCKED다. 필수 이동의 미확인을 이전 Task PASS로 대체하지 않는다.

## 14. Required Result / 완료 보고

```text
TASK: RMAP09_COMPOSER
STATUS: PASS 또는 FAIL 또는 BLOCKED 또는 STATUS_CONFLICT
USER-FACING IMPLEMENTATION REPORT: 조립 가능한 청크/입력, scene/조작/CSV 경로, 남은 Small Run 책임
RESPONSIBILITY AND FILES: 실제 경로 | 추가/수정 | 책임 | 소유하지 않는 책임
PRECONDITIONS: HEAD/branch, 외부 expected/inbox 실제 SHA, 선행 Task/Archive/Result SHA와 실제 commit, 적용 전후 상태
CHANGED: 바인딩과 KEEP/ADAPT/NEW 이유, 원본/파생 경로, 생성 순서, profile/Seed/재선택 접점
REQUIREMENT EVIDENCE: P04/P19/A22/A23/A24 각각 산출물, 검사/관찰, 판정
COMPOSITION EVIDENCE: 큰 형상/Port/Spine/보호->6개 후보->Overlay->Bake, 출처/96셀 불변, 실제 이동/난도·체력 조건
RESELECTION EVIDENCE: 입력/상한/시도 수, 실패 범위/이유, 결정성/비굴착, 종료 결과
VALIDATION: 실제 filter/job/count, CSV/API 확인, Play 증거, 실패/최소 수정, 미실행과 이유
UNITY VISIBLE OUTPUT: scene/bounds/청크·후보·Port/실패 위치, 실제 Player/Tilemap/등반 결과
OUT-OF-SCOPE FINDINGS: Small Run/500 보강/전역 생성/전수 무피해·복구 검증의 남은 책임
RMAP10 BINDINGS: 조립 요청/결과/실패/API, pattern/port/profile/실제 Bake 접점의 경로와 EXISTING/PROPOSED 구분
FOLLOWUP: 현지 single_task_v1 경로/필드 규칙, 설치 Task 실제 SHA-256
FINAL EVIDENCE: 필수 미확인 유무, 현재 상태, Task와 Archive bytes 동일 여부
NEXT: RMAP10_SMALL_RUN LOCKED / NOT STARTED
COMMIT: 작업 전 HEAD, 실제 생성 commit SHA 또는 아직 미생성인 단계/이유
```

## 15. PASS / Finalize / STOP

5개 요구 ID의 실제 조립/API/보호/재선택과 필수 검사가 모두 확인돼야 PASS다. 실행하지 않은 기능/검사를 PASS로 적지 않는다.
선행 상태/해시 충돌은 STATUS_CONFLICT, 필수 자료/도구 부재는 BLOCKED, 기능/확인 기준 불충족은 FAIL로 보고한다.
낮은 천장/불리한 배치 등 난도는 허용한다. 확인된 필수 유일 경로 단절을 자동 굴착/침묵 수리/검증 완화로 PASS 처리하지 않는다.
PASS Result 후 기존 Finalize로 RMAP09 CURRENT->COMPLETE, Current->NONE만 수행한다. RMAP10~19는 LOCKED다.
기준선 상태는 적용 후 229 COMPLETE / 1 CURRENT / 10 LOCKED, 완료 후 230 COMPLETE / 0 CURRENT / 10 LOCKED다.
Task/Archive/Result/허용 코드/asset/CSV/증거/상태 변경만 commit한다. 무관한 staged 변경을 포함하거나 임의 unstage하지 않는다.
commit 메시지: RMAP09 implement protected chunk composition and bounded reselection
최종 CLI 보고에 Result 경로, 실제 생성 commit SHA, Result SHA-256, 설치 Task SHA-256, Current NONE, RMAP10 LOCKED를 적는다.
Result를 commit 전에 작성했다면 실제 commit 증거는 최종 CLI 보고로 남긴다. 해시 기입을 위해 선행/설치 문서를 다시 쓰지 않는다.
Git push와 RMAP10 실행 없이 STOP한다.
