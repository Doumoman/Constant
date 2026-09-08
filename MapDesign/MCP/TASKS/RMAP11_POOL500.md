```yaml
mcp_patch:
  format: single_task_v1
  task_id: RMAP11_POOL500
  task_file: TASKS/RMAP11_POOL500.md
  requires_current_task: NONE
  requires_completed_task: RMAP10_SMALL_RUN
  requires_result:
    path: REPORTS/RMAP10_SMALL_RUN_RESULT.md
    status: PASS
    sha256: 18a660d87fed1ec4e273ad4b7a090da1fcf8336c9f7246f45a02304435003113
  requires_installed_task:
    path: TASKS/RMAP10_SMALL_RUN.md
    sha256: 1981435b4790eb84514d04dfe3842864a2be24390d965065f4278250682dc77a
  sets_current_task: RMAP11_POOL500
```

# RMAP11_POOL500 - 최종 역할형 500개 풀

```text
TASK: RMAP11_POOL500
DOCUMENT: v4.2 / RMAP10 PASS 이후 실행 지시서 / 2026-09-08
STATUS: CURRENT
INPUT: MapDesign/MCP_INBOX/RMAP11_POOL500.md
EXPECTED_RESULT: MapDesign/MCP/REPORTS/RMAP11_POOL500_RESULT.md
NEXT: RMAP12_WORLD_DATA
NEXT STATUS: LOCKED / DO NOT START
```

위 CURRENT는 정상 Apply 이후의 상태다. 문서 발행만으로 저장소 상태를 바꾸지 않는다.
약 1~2시간의 기능 책임 단위이며 문서는 300줄 이하로 운영한다. 원본 A14의 대표 샘플 사람 검수도 완료 조건이다.

## 1. User-Facing Goal / 이번 작업의 완료 모습

Transform/중복 제거가 끝난 서로 다른 4x4 Base Geometry 500개를 역할·태그·자동 특성·출처와 함께 조회한다.
10역할의 목표/실제 수량과 조정 이유, 원본 재사용/탈락/보강 근거를 확인할 수 있다.
최종 풀을 기존 Small Run의 후보 입력에 연결하고 실제 생성 지형에 새 후보가 사용되는지 확인한다.
대표 형상과 생성 결과를 사람이 검수한 뒤 500개를 최종 확정한다. 준비/검사 완료와 실제 사람 검수 완료를 구분한다.
이번 요구 ID는 A13, A14다. RMAP07의 Base/역할/태그/Transform 계약과 RMAP10의 실제 플레이 연결을 소비한다.
RMAP12 월드 데이터, 지원 맵 크기/Recipe 확대와 새 콘텐츠 생성은 시작하지 않는다.

## 2. Preflight / 선행 완료와 정상 single_task_v1 적용

1. 프로젝트 루트/적용 AGENTS.md와 MapDesign/MCP/00_MCP_ENTRYPOINT.md를 확인한다.
2. 기존 Apply/ChangeControl/Finalize와 RMAP/02_PROTOCOL_V4_2.md에서 지원 필드/경로를 확인한다.
3. YAML 내부 경로는 MapDesign/MCP 기준, 본문 Assets/MapDesign 경로는 프로젝트 루트 기준이다.
4. 전달 메시지의 외부 SHA-256을 inbox 파일 전체 원본 bytes의 SHA와 대조한다.
   파일 자체의 expected SHA는 자기참조를 피하려고 본문에 넣지 않는다. 이전 planned 기획 문서의 해시와 비교하지 않는다.
   외부 값 부재/불일치 또는 현지 별도 manifest/등록 SHA 충돌이면 적용 전에 경로와 expected/actual을 보고하고 중단한다.
   줄바꿈 정규화/재저장으로 해시를 맞추거나 적용기 규칙을 수정하지 않는다.
5. RMAP10 installed Task/Archive의 bytes/SHA와 PASS Result의 TASK/STATUS/SHA를 YAML과 대조한다.
6. RMAP10 Result는 Phase C Finalize/Phase D commit 전에 작성됐다. 그 문구를 실제 완료 증거로 간주하지 않는다.
   저장소에서 RMAP10 COMPLETE, Current NONE, RMAP11~19 LOCKED 및 Result 경로의 git log로 실제 RMAP10 commit을 확인한다.
   선행 Finalize/commit 미완료면 남은 단계와 근거를 보고하고 STOP한다. 이번 작업에서 선행 상태/Result를 임의 수정하지 않는다.
7. 정상 시작 기준은 240행 = 231 COMPLETE / 0 CURRENT / 9 LOCKED다. 차이가 있으면 실제 이력과 현지 프로토콜로 판단한다.
8. 미적용 inbox 후보 본 MD 1개, RMAP11 등록 ID와 다른 CURRENT 부재를 확인한다. 선행 변경/Task-Archive 불일치면 중단한다.
9. 정상 Apply로 RMAP11만 LOCKED->CURRENT, Current NONE->RMAP11_POOL500을 수행하고 Task/Archive를 바이트 동일하게 설치한다.
10. 같은 RMAP11 CURRENT 재개는 설치 Task/Archive/입력 동일성, 기존 결과/검수 기록을 확인하고 현지 재개 규칙을 따른다.
11. 이미 RMAP11 COMPLETE+유효 PASS면 기존 결과를 보고하고 STOP한다. RMAP12를 자동으로 열지 않는다.
12. 무관한 변경은 보존한다. 관련 변경을 분리할 수 없으면 근거를 보고하며 reset/stash/강제 덮어쓰기를 하지 않는다.

## 3. Read Allowlist / 후보 원본과 실제 소비 경로

- MapDesign/MCP/RMAP/{00_BASELINE_V4_2,01_SEQUENCE_V4_2,02_PROTOCOL_V4_2}.md 및 현지 운영 문서.
- MapDesign/MCP/{MASTER_IMPLEMENTATION_TASK_LIST,06_IMPLEMENTATION_STATUS}.md와 RMAP10 Task/Archive/Result.
- MapDesign/MCP/GENERATED/RMAP01/file_bindings.csv의 PATTERN_DATA / CLASSIFIER / EDITOR / RUN06 선별 바인딩과 실제 코드/직접 호출자/테스트.
- 기존 500 Mask/원본 ID/분류/탈락 근거 및 RUN06 선별·형태 비교 API 중 필요한 범위.
- RMAP07 첫 48개 후보, Base/10역할/24태그/Transform/특성/원본 관계와 RMAP10 실제 소비·플레이 자료.
- 최신 공유 Player profile/Collider와 기존 후보의 로컬 검사/분류 규칙. 과거 모든 Task/CSV/Seed를 재감사하지 않는다.

| 실제 연결점 | 상태/확인 근거 | 이번 책임 |
|---|---|---|
| Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/RmapPatternCatalog.cs | EXISTING: RMAP07~10 | 기존 모델/분류/변환/특성/안정 ID를 재사용하고 최종 후보 입력 접점 적응 |
| Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/RmapSmallRunHarness.cs | EXISTING: RMAP10 | 기존 후보 source만 최종 풀에 연결. PortGalleryV1/크기/Port/플레이 책임은 유지 |
| Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/RmapComposer.cs | EXISTING: RMAP09/10 | 필요한 후보 조회/호환 접점만 적응. 조립/보호 규칙 재작성 금지 |
| Assets/_Game/Live/Editor/RMAP10/RmapSmallRunGeneratorWindow.cs / RmapSmallRunSceneBuilder.cs | EXISTING: RMAP10 | 현재 pool 버전/후보 사용 표시와 대표 생성 확인 |
| 기존 RUN06 선별/형태 비교 및 원본 500 | 실제 바인딩에서 현지 확인 | 원본 보존, 재분류/탈락/대표 검수 재사용 |
| Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/RmapPatternPool500.cs | PROPOSED: RMAP10 | 기존 selector로 부족한 최종 500 선정/조회 책임만 추가 |

별칭/PROPOSED 이름을 기존 구현으로 단정하지 않는다. 실제 경로/API를 읽고 KEEP/ADAPT/NEW와 이유를 Result에 남긴다.
RUN06 기능 재사용은 선별/비교 범위다. 과거 65,536-mask 감사/전체 Run publisher를 재실행하거나 다른 SourceOfTruth를 덮어쓰지 않는다.
RMAP07~10 CSV는 파생 자료일 수 있다. 원본/선정 규칙/최종 snapshot의 권위와 재생성 경로를 먼저 구분한다.
같은 Geometry의 기존 Candidate ID/출처는 유지한다. 생성 데이터는 특정 Live Player/Camera 인스턴스에 의존하지 않는다.

## 4. Write Allowlist / 변경 경계

- 실제 바인딩에서 확인한 후보 검사/재분류/선별/부족 역할 생성과 최종 풀 CSV/Generated 출력.
- 원본/Transform/탈락/보강/최종 ID 추적, 역할별 목표/실제/조정 이유와 대표 검수 자료.
- 기존 catalog/selector의 최종 풀 조회 접점과 RmapSmallRunHarness의 후보 source 및 필요한 직접 소비부의 최소 변경.
- 기존 RUN06/Editor의 대표 역할 비교·표시 접점. 부족하면 RMAP11 범위의 최소 검수 표시/내보내기만 추가한다.
- 기존 assembly의 focused 데이터/분류/선정/소비 테스트와 후보 입력 변경에 직접 영향받는 대표 실제 플레이 확인.
- MapDesign/MCP/GENERATED/RMAP11의 자료/검수 패키지/검사 증거, 지정 Task/Archive/Result와 RMAP11 상태 기록.
- 새 .meta/asmdef는 필요한 최소 범위로 한정한다. 기존 후보 원본/Player/scene을 임의 교체하지 않는다.

맵 크기/Recipe/Port 계약/기본 장비·체력/이동 수치를 500개 수량에 맞춰 바꾸지 않는다.
원본 500과 첫 풀 파일은 보존한다. 최종 source 연결에 따른 생성 결과 변경은 pool 버전으로 구분하고 과거 증거를 덮어쓰지 않는다.
package/운영 프로토콜/과거 Result/월드 데이터/새 콘텐츠 구현은 범위 밖이다.

## 5. A13 / 역할별 500개 목표 전체

| Primary Role | 초기 목표 |
|---|---:|
| SLOPE_RISE_RIGHT | 55 |
| SLOPE_RISE_LEFT | 55 |
| CEILING_FLAT | 40 |
| CEILING_ROUGH | 50 |
| WALL_LEFT | 35 |
| WALL_RIGHT | 35 |
| VOID_CLEAR | 1 |
| SPARSE_AIR_PLATFORM | 95 |
| STANDABLE_LEDGE | 100 |
| VERTICAL_PASSAGE | 34 |
| 합계 | 500 |

- 역할별 수량은 잠정 목표다. 목표/실제/차이/이유를 기록하고 조정할 수 있으나 고유 Base Geometry 총 500과 VOID_CLEAR 1개는 지킨다.
- 후보당 Primary Role은 정확히 하나다. 실제 셀 형상과 기존 분류 규칙에서 판정하며 인덱스/부족 수량에 맞춰 역할 이름만 붙이지 않는다.
- VOID_CLEAR는 16셀이 모두 AIR인 하나다. 다른 원본/태그/Transform으로 복제해 수를 늘리지 않는다.
- 특정 역할의 고유 유효 형상을 목표만큼 얻기 어렵다면 제약/분류 근거와 조정 수량을 기록한다. 중복/무효 후보로 숫자를 채우지 않는다.
- 10역할의 유효 대표와 전체 합계가 자료/API/표시에 일치해야 한다. 배열·ID 유일성 검사 없이 CSV 행 수만 500이라고 보고하지 않는다.

## 6. A14 / 제작·재사용 순서

다음 순서를 원본/중간 집계/최종 관계로 추적한다. 처음부터 500개를 새로 추첨해 원본 재사용을 생략하지 않는다.

1. 기존 500 Mask와 원본 ID/파일/좌표 규칙을 보존하고 현재 유효한 첫 풀도 읽는다.
2. 최신 공유 Player 규칙으로 로컬 셀/특성/사용 조건을 검사한다.
3. 실제 Geometry에서 역할을 재분류하고 의도 태그/방향 변환/문맥 조건을 구분한다.
4. 무효/중복을 제거하고 원본->변환->동일 후보 병합/탈락 이유를 남긴다.
5. 부족한 역할만 명시적 형상 규칙으로 보강하고 검사/재분류/중복 제거를 다시 적용한다.
6. 500개 선정안과 실제 생성 사용/대표 검수 자료를 완성하고 사람이 대표 샘플을 검수한다.
7. 검수 결과를 반영한 고유 500개를 확정한다. 수정된 범위의 검사/관계/생성 증거만 필요한 만큼 갱신한다.

기존 RUN06의 선별/형태 비교 기능을 활용한다. 모든 후보/Seed의 전수 물리 시뮬레이션은 요구하지 않는다.
사람 검수 이전에도 생성/연결/실제 플레이 검사를 수행해 검토 가능한 선정안을 완성한다. 최종 확정 여부는 별도로 기록한다.

## 7. Base·Transform·원본 관계와 검사 규칙

- 후보는 4x4/16셀, 좌하단 원점/x 우향/y 상향/index=y*4+x다. 기존 이진 Mask bit 규칙을 대조하고 SOLID/AIR로 변환한다.
- Base는 SOLID / AIR / ONE_WAY_PLATFORM 세 종류다. one-way를 SOLID/AIR에 합쳐 중복 계산하지 않는다.
- LADDER/CLIMB_PILLAR와 HAZARD/콘텐츠 슬롯/MATERIAL/DECORATION은 Base의 500개 수에 포함하지 않는다.
- 허용 Transform은 R0/MirrorX/MirrorY/R180이다. 최종 16셀 배열이 같으면 같은 후보, 다르면 별도 후보로 센다.
- 배열 동등성으로 dedup한다. 해시를 사용해도 실제 배열을 대조하고 원본 ID/태그/Overlay만 다른 중복을 세지 않는다.
- Source ID/원본 Mask·파일/Transform->최종 Candidate ID의 모든 관계를 보존한다. 병합된 출처나 탈락 이유를 유실하지 않는다.
- 변환 후 방향 역할/자동 특성을 재계산하고 방향 의존 의도 태그는 기존 명시적 변환표를 사용한다.
- 사람이 부여한 용도 태그 전체를 자동 추정으로 덮지 않는다. UnresolvedDirectionalTags/상반된 의도는 검토 필요로 남긴다.
- ONE_WAY의 충돌면은 MirrorY/R180 이후에도 월드 위쪽이다. 최종 셀 배열에 변환을 중복 적용하지 않는다.
- 최신 profile/Collider로 경계/머리 여유/지지면/Grab·등반 가능 영역 등 기존 로컬 특성을 검사하고 조건/버전을 기록한다.
- 로컬 특성/의도 태그는 실제 배치 통과 증명이 아니다. ContextRequired를 보존하고 최종 조립 문맥에서 해소한다.
- 낮은 천장/복합 형상/1타일 통로/불리한 선택 공간을 전부 무효로 버리지 않는다. 무효 데이터와 문맥에 따라 사용 가능한 난도를 구분한다.
- 알려진 필수 유일 경로 단절은 실제 생성 검증에서 실패다. REQUIRED_OK 태그나 역할만으로 PASS하지 않는다.

## 8. 부족 역할 보강과 결정적 선정

- 기존 후보의 실제 유효/고유 역할 분포에서 부족분을 계산한다. 부족하지 않은 역할까지 무작위로 대량 재생성하지 않는다.
- 보강은 역할별 형상 규칙/파라미터/허용 변환/생성 한도를 명시한다. 단순 비트 난수에 역할을 순번 배정하지 않는다.
- 각 보강 후보의 RuleId/파라미터/원본 관계/분류 이유를 기록하고 기존 검사/자동 특성/dedup를 통과시킨다.
- 3^16 전수 열거나 무한 재시도를 하지 않는다. 유한 후보 평가/시도 상한을 두고 소진/목표 조정 이유를 남긴다.
- 첫 풀의 유효한 실제 소비 후보는 재사용을 우선한다. 최종에서 제외하면 이유와 대체 소비 영향을 기록하고 숨겨진 48개 fallback으로 우회하지 않는다.
- 기존 선택/Seed/ID 계약을 재사용한다. 같은 입력/설정/버전이면 같은 최종 배열·ID·순서·집계가 나온다.
- 최종 풀/catalog 버전을 실제 source에 연결한다. pool 버전 변경으로 생긴 새 맵 digest를 과거 버전의 재현 실패와 혼동하지 않는다.
- 목표 500을 충족하지 못하면 실제 수/원인을 보고하고 실패한다. 전체 고유 수나 VOID_CLEAR 조건을 자동 완화하지 않는다.

## 9. 실제 생성기의 후보 입력 교체

- RmapSmallRunHarness의 실제 후보 source를 500개 선정안/확정 풀에 연결한다. 목록만 내보내고 생성기는 계속 48개를 쓰는 상태로 끝내지 않는다.
- PortGalleryV1, 지원 크기 36x24/48x24, Port/Spine/보호/실제 Bake/Player/Exit 의미를 유지한다.
- 500개 등록 수, 조건별 유효 후보 집합/선택 ID/청크·슬롯 위치와 최종 pool 버전을 추적한다.
- 대표 생성에서 첫 48개 밖의 새 유효 후보가 실제 선택되어 Base/Tilemap에 반영되는 증거를 남긴다. 모든 500개를 한 맵/Seed에 강제 사용하지 않는다.
- 후보는 기존 역할/이웃/보호/Port 필터를 통과해야 한다. 사용 증거를 만들려고 검증 뒤 셀/후보를 교체하지 않는다.
- 같은 Seed/크기/Recipe/pool·profile 버전의 재생성 결과를 비교한다. 원본 후보를 가변 참조로 오염시키지 않는다.
- 후보 입력 교체에 직접 영향받는 대표 Small Run 하나를 생성하고 실제 Production Player Start->Exit를 확인한다.
- 우선 RMAP10의 1107/36x24/PortGalleryV1을 재사용한다. 다른 대상이 필요하면 선택 이유와 기존 대상 결과를 기록한다.
- 필요한 추가 Seed/검사는 구체적인 실패나 역할 사용 확인 범위로 한정한다. 기존 3개/20개/전체 Seed 검사를 습관적으로 반복하지 않는다.
- 필수 단절은 원인/위치를 남기고 기존 제한 재선택으로 처리한다. 자동 터널/평탄화/Player 수치 변경으로 성공시키지 않는다.

## 10. A14의 대표 샘플 사람 검수

- 기존 RUN06의 선별/형태 비교 도구를 활용해 10역할 대표를 고른다. 재사용 후보와 새 보강 후보, 변환/분류 경계 사례를 포함한다.
- 4x4의 모든 16셀과 SOLID/AIR/ONE_WAY 구분이 보이도록 이미지/비교 화면을 제공한다. 실제 데이터에서 그리며 셀을 생략한 장식 그림으로 대체하지 않는다.
- Candidate ID/역할/원본·RuleId/Transform/선정 이유/문맥 필요/태그 충돌을 표와 연결한다. 목표/실제 역할 수량도 함께 제공한다.
- VOID_CLEAR, 좌우 경사·벽, 천장/발판 전환과 one-way 위쪽 면, 조밀/희소/1타일 통로 등 필요한 대표 형상을 확인할 수 있어야 한다.
- 새 후보가 쓰인 실제 생성 결과도 첨부한다. 검수 대상 ID와 pool 버전/Geometry를 고정해 다른 버전의 승인과 혼동하지 않는다.
- 코드/500개 선정안/자동 검사/실제 소비·플레이/검수 패키지를 먼저 완성한 후 사용자에게 실제 검수를 요청한다.
- 같은 검수 대상에 대한 유효한 사용자 검수 기록이 이미 있으면 재사용한다. 변경된 Geometry/역할/대상은 해당 범위의 확인이 필요하다.
- 자동 검사나 AI의 이미지 판독을 사람 검수 완료로 기록하지 않는다. 검수자/판정/근거를 꾸며 쓰지 않는다.
- 유효한 사람 검수가 아직 없으면 Result STATUS는 BLOCKED, REVIEW_STATUS는 HUMAN_REVIEW_PENDING으로 기록한다.
  준비된 자료 경로/검수 대상/확인할 내용을 보고하고 CURRENT를 유지한다. Finalize/완료 commit은 수행하지 않는다.
- 수정 요청이 있으면 해당 후보/관계/사용 영향을 좁게 고친다. 검사된 나머지를 전수 재생성하지 않고 변경된 검수 자료를 갱신한다.
- 실제 승인 기록과 최종 500개/자동 검사/대표 사용 증거가 모두 일치하면 PASS/Finalize로 진행한다.

## 11. 데이터/API와 검수 산출물

원본/선정 규칙/최종 CSV/Generated snapshot의 권위와 재생성 경로를 명시하고 같은 자료를 독립 편집하는 두 원본으로 만들지 않는다.

| 자료 | 필수 내용 |
|---|---|
| 최종 후보 | 500개 Candidate ID/16셀/Primary Role/의도 태그/자동 특성/문맥 조건/pool 버전 |
| 역할 집계 | 10역할 목표/실제/차이/조정 이유, 합계 500, VOID_CLEAR 1 |
| 원본 관계 | 기존 파일/Mask/Source ID/Transform/최종 ID, 재사용/병합/탈락 근거 |
| 부족분 보강 | 역할별 검사 전후 고유 수/부족분, RuleId/파라미터/시도 상한/검사·탈락 결과 |
| 실제 사용 | Run 설정/pool source/선택 ID/슬롯/실제 Bake, 새 후보 사용과 대표 Player Exit 증거 |
| 검수 패키지 | 대표 16셀 그림/비교 화면, 대상 ID/pool 버전/목표·실제 표, 실제 생성 결과 |
| 사람 검수 기록 | 상태/실제 사용자 판정 근거/대상/수정 요청과 반영 내용. 미검수는 명시적으로 pending |

UTF-8 CSV의 셀/다중 태그/관계 escaping과 정렬을 명시하고 round-trip한다. 중복/누락 ID·셀/알 수 없는 역할·태그/미해결 참조는 오류로 보고한다.
파생 CSV 행 수만 검사하지 말고 실제 catalog 조회/생성기 소비와 일치하는지 확인한다. 선택 배열과 원본이 바뀌지 않게 한다.

## 12. Focused Checks / 필요한 실제 검증

- 데이터 전체: 최종 500개 배열/ID 유일성, 16셀/3종 Base, VOID_CLEAR 정확히 1개, 후보당 유효한 Primary Role 하나.
- 역할/태그: A13 목표/실제/조정 이유, 기존 24태그 계약, 방향 재분류/Transform/태그 충돌·ContextRequired 보존.
- 재사용/보강: 원본 보존/ID 추적/중복 병합/탈락 이유, 부족 역할 생성 규칙, 유한 상한과 결정적 재생성.
- 대표 형상: 알려진 경사/벽/천장/발판/빈 공간 예제로 역할·특성 계산을 확인하고 수량 맞추기용 분류를 허용하지 않음.
- CSV/API: round-trip/참조 유효성, catalog와 최종 출력 일치, 동일 Geometry ID 안정성, pool 버전별 재현.
- 생성 소비: 후보 source 500개 연결, 필터/선택/실제 Base 출처, 새 후보 사용과 대표 Small Run의 필수 연결.
- PlayMode 또는 실제 Play: 입력 교체 후 대표 Production Player가 같은 실제 Tilemap/Collider에서 Start->Exit 도착.
- 사람 검수: 대표 자료와 실제 사용자 판정/대상이 일치함. AI 자동 검사로 대체하지 않음.
- Compile/refresh와 필요한 대표 생성/표시를 수행한다. 직접 영향 회귀만 최소 범위로 정하고 이유를 적는다.
- broad/full/unfiltered regression, legacy 19347, 불필요한 Player build, 모든 후보/Seed의 전수 물리 시뮬레이션은 하지 않는다.

500개 자료 자체의 유일성/분류/참조 확인은 필요하다. 이를 500개 전부의 실제 플레이 또는 모든 배치 문맥의 통과 증명이라고 보고하지 않는다.
Unity 미실행/도구 부재는 NOT RUN/BLOCKED다. 실패/미확인을 과거 PASS 또는 검수 예정으로 대신하지 않는다.

## 13. Required Result / 완료 또는 검수 대기 보고

```text
TASK: RMAP11_POOL500
STATUS: PASS 또는 FAIL 또는 BLOCKED 또는 STATUS_CONFLICT
USER-FACING IMPLEMENTATION REPORT: 후보/역할 조회·검수 도구, 최종 source 연결과 실제 사용, 남은 검수/확장 책임
RESPONSIBILITY AND FILES: 실제 경로 | 추가/수정 | 책임 | 소유하지 않는 책임
PRECONDITIONS: HEAD/branch, 외부 expected/inbox 실제 SHA, 선행 Task/Archive/Result SHA와 실제 commit, 적용 전후 상태
CHANGED: 바인딩과 KEEP/ADAPT/NEW 이유, 원본/파생 경로, 검사·선정·보강·소비 API
REQUIREMENT EVIDENCE: A13/A14 각각 산출물, 검사/관찰/실제 검수, 판정
POOL EVIDENCE: 500 유일 배열/ID, VOID_CLEAR 1, 10역할 목표/실제/조정 이유, 원본/탈락/보강 집계
CONSUMER EVIDENCE: Run 설정/pool 버전/후보 source, 선택 ID·위치/새 후보 사용/실제 Bake·Exit
REVIEW_STATUS: HUMAN_REVIEW_PENDING 또는 CHANGES_REQUESTED 또는 APPROVED
HUMAN REVIEW EVIDENCE: 패키지 경로, 대상 ID/버전, 실제 사용자 판정 근거와 반영 내용 또는 미검수 이유
VALIDATION: 실제 filter/job/count, CSV/결정성/대표 Play, 실패/최소 수정, 미실행과 이유
UNITY VISIBLE OUTPUT: 대표 형상/생성 결과/현재 설정, 실제 Player/Tilemap/Exit 증거
OUT-OF-SCOPE FINDINGS: 월드 데이터/크기·Recipe 확장/전수 물리 검증의 남은 책임
RMAP12 BINDINGS: 최종 catalog/ID/특성/소비 API·버전의 실제 경로와 EXISTING/PROPOSED 구분
FOLLOWUP: 현지 single_task_v1 경로/필드 규칙, 설치 Task 실제 SHA-256
FINAL EVIDENCE: 필수 미확인/검수 대기 유무, 현재 상태, Task와 Archive bytes 동일 여부
NEXT: RMAP12_WORLD_DATA LOCKED / NOT STARTED
COMMIT: 작업 전 HEAD, 실제 생성 commit SHA 또는 아직 미생성인 단계/이유
```

## 14. PASS / Finalize / STOP

A13/A14의 유효한 고유 500개/VOID_CLEAR 1/관계·분류·보강 근거/실제 생성 사용과 대표 사람 검수가 확인돼야 PASS다.
선행 상태/해시 충돌은 STATUS_CONFLICT, 필수 자료/도구 부재나 사람 검수 대기는 BLOCKED, 기능/확인 기준 불충족은 FAIL로 보고한다.
검수 대기 때에는 RMAP11 CURRENT를 유지하고 자료를 먼저 제공한다. 같은 Task를 정상 재개하며 다음 Task를 열지 않는다.
PASS Result 후 기존 Finalize로 RMAP11 CURRENT->COMPLETE, Current->NONE만 수행한다. RMAP12~19는 LOCKED다.
기준선 상태는 적용 후 231 COMPLETE / 1 CURRENT / 8 LOCKED, 완료 후 232 COMPLETE / 0 CURRENT / 8 LOCKED다.
Task/Archive/Result/허용 코드/asset/CSV/증거/상태 변경만 commit한다. 무관한 staged 변경을 포함하거나 임의 unstage하지 않는다.
commit 메시지: RMAP11 implement reviewed unique role pattern pool of 500
최종 CLI 보고에 Result/검수 자료 경로, 실제 commit SHA 또는 미생성 이유, Result/설치 Task SHA, Current와 RMAP12 LOCKED를 적는다.
Result를 commit 전에 작성했다면 실제 commit 증거는 최종 CLI 보고로 남긴다. 해시 기입을 위해 선행/설치 문서를 다시 쓰지 않는다.
Git push와 RMAP12 실행 없이 STOP한다.
