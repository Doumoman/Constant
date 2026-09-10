# RMAP11_FIX - 지정 25개 형상 수정 후 다음 TASK로 인계

- MODE: RMAP11_POOL500 CURRENT 재개 / 사용자 변경 지시
- 새 등록 Task가 아니다. 이 파일을 single_task_v1 새 Task로 Apply하지 않는다.
- 기존 RMAP11 Task/Archive는 그대로 두고, 현지 재개·변경 기록 절차를 따른다.
- INPUT ROOT: MapDesign/MCP/INPUTS/R11FIX
- RESULT: MapDesign/MCP/REPORTS/RMAP11_POOL500_RESULT.md
- NEXT: RMAP12_WORLD_DATA. 아래 완료 조건을 만족한 뒤 별도 정상 Apply한다.

## 1. 사용자 결정과 최신 이동 기준

사용자가 기존 500개 도감에서 지정한 번호:
002, 007, 008, 013, 014, 016, 018, 019, 036, 038, 042, 044, 046,
047, 049, 173, 174, 182, 188, 190, 197, 205, 207, 219, 221.
총 25개다. 도감 번호는 역할순 정렬 번호이며 PoolIndex/Candidate ID가 아니다.
사용자 요청: 해당 형상을 수정하고 다음 TASK로 진행한다.
최신 보완 원문: “점프하고 매달려서 플레이어는 최대 2칸을 위로 넘어갈 수 있어”.
따라서 이전의 ‘점프만’은 기본 점프와 모서리 매달림을 포함한 무장비 이동으로 해석한다.

- 1칸 상승은 기본 점프, 2칸 상승은 점프→안전 모서리 Grab→점프 이탈로 통과한다.
- 한 번의 상승에서 3칸 이상을 요구하지 않는다. 중간 지지면으로 분리한다.
- 아이템·파괴·사다리·등반 기둥·벽차기·순간이동을 통과 해법으로 쓰지 않는다.
- 현지 공유 Player profile과 0.4×0.8 발바닥 pivot Collider를 소비한다. 이동 수치 변경 금지.
- 기존 약 1.3칸 점프와 ‘점프+Grab으로 2칸’을 구분한다. 점프 높이를 2칸으로 늘리지 않는다.
- 안전 고체의 노출 상단 모서리만 Grab하며 one-way/hazard/압착 표면은 Grab 지지면이 아니다.
- 2칸이라는 높이 검사만으로 통과 PASS하지 않는다. 실제 접촉·매달림·이탈·상단 착지까지 확인한다.

## 2. 입력과 검증 기준선

| 입력 | 의미 |
|---|---|
| patch25.csv | 원본 번호/ID/16셀과 교체 16셀, 의도 역할, 표면 높이, 단계별 이동/Grab 모서리 |
| preview500.csv | 원본 도감 번호를 유지한 500개 예상 배열. 런타임 catalog가 아니다 |
| review.json | 사용자 실제 요청·보완 원문, 해시, 진행 권한과 로컬 검사 한계 |
| RMAP12_SPEC.md | 기존 RMAP12 설치 전 명세의 원문 복사. 지금 곧바로 Apply할 파일이 아니다 |
| RMAP12_PREP.md | RMAP11 완료 후 실제 근거로 RMAP12 실행 MD를 바인딩하는 절차 |

파일 형식은 UTF-8/LF CSV이며 배열 필드는 JSON 문자열이다. Python/표준 CSV 파서로 읽는다.
BaseCells16은 좌하단 원점, x 우향/y 상향, index=y*4+x, S/A/O의 최종 16셀이다.
원본 CSV SHA-256: e6a251810cdfb800a9c7a0679f4d6b06ca120d1cadfa363a7fb3b10c6be9e912
원본 pool version: RMAP11_POOL500_V1
원본 pool digest: ce713ce3ce985558886032e04685875ff82b547c1bf321a1c63c5122ba6fae3e
RMAP11 설치 Task/Archive SHA: 1f96f5414fb0aa9040017491c9b5f33978822fdcfa0cdd56ea3f78af57d950c5
검수 대기 Result SHA: a10f26ab702e4522c8d0fe08f64615eac9f0c90b2f77cc3e477d1171b976bc0b
patch25.csv SHA: 44f85693f5eecadf364d88aa7ab55aa171953fb4870bcecb7188bf7eb1cd5c0b
이 MD 자체는 동봉 SHA256SUMS.txt의 외부 SHA와 전체 bytes를 비교한다. 자기 해시를 본문에 넣지 않는다.

## 3. Preflight / 같은 RMAP11 재개

1. 프로젝트 루트/AGENTS.md, MapDesign/MCP/00_MCP_ENTRYPOINT.md와 현지 RMAP 프로토콜을 읽는다.
2. git status/branch/HEAD, Current Task, MASTER/STATUS에서 실제 상태를 확인한다.
3. 신규 적용의 기준선은 RMAP11 CURRENT, RMAP12~19 LOCKED, 231 COMPLETE / 1 CURRENT / 8 LOCKED다.
4. 설치 RMAP11 Task/Archive byte 동일성과 위 해시, 기존 BLOCKED Result 및 현재 pool을 대조한다.
5. 원본 25행은 OldCandidateId+OldBaseCells16+PoolIndex로 정확히 매칭한다. 번호만으로 갱신하지 않는다.
6. 원본 CSV bytes/기준선이 다르면 실제 차이를 보고한다. 정규화·재저장으로 해시를 맞추지 않는다.
7. 재실행이면 기존 적용 매핑/수정 pool/검사 기록을 먼저 확인한다. 이미 반영한 교체를 중복 적용하지 않는다.
8. 이미 RMAP11 COMPLETE이면 이번 patch의 실제 반영, PASS Result, Finalize/commit을 확인한 뒤 §10만 수행한다.
9. 다른 CURRENT, 미설명 해시/셀 불일치, 불완전 중간 상태는 현지 절차로 확인한다. 임의 상태 덮어쓰기 금지.
10. 무관한 변경을 보존한다. reset/stash/강제 덮어쓰기, 프로토콜 수정으로 우회하지 않는다.

## 4. Read / Write 경계와 실제 연결

RMAP01 file_bindings.csv의 PATTERN_DATA/CLASSIFIER/EDITOR/RUN06/PLAYER_MOVE/PROFILE/BAKE를 좁게 확인한다.
아래는 기존 보고서에서 확인된 경로다. 현지 API/직접 호출자를 읽고 실제 변경 책임을 기록한다.

- Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/RmapPatternPool500.cs
- Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/RmapPatternCatalog.cs
- Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/RmapSmallRunHarness.cs
- Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/RmapComposer.cs
- Assets/_Game/Live/Editor/RMAP11/RmapPool500ReviewPublisher.cs
- Assets/_Game/Live/Editor/RMAP10/RmapSmallRunSceneBuilder.cs
- Assets/_Game/Live/Runtime/Movement/CharacterLiveMovementDriver.cs 및 기존 settings/Grab surface 접점
- RMAP11의 기존 EditMode/PlayMode tests, Scene/Generated 자료, 현재 Result와 상태 기록

쓰기: 최종 풀의 명시적 교체 원본/빌더·최소 소비 접점·실제 Grab 표면 연결·해당 검증·검수 자료.
공유 Player 물리/튜닝, 기존 RUN06 원본과 RMAP07 첫 풀, 과거 RMAP02~10 씬·Result는 보존한다.
특정 구현 파일 이름에 맞추어 새 Service를 늘리지 않는다. 기존 책임을 재사용한다.

## 5. 정확한 25개 교체와 재생성 원본

1. patch25.csv를 명시적 authoring override로 읽어 기존 BuildFinalPool의 적절한 소유 지점에 연결한다.
2. 파생 final CSV만 편집하고 다음 Build에서 사라지는 방식으로 종료하지 않는다.
3. 25개는 NewBaseCells16 그대로 교체한다. 나머지 475개의 Geometry/ID를 유지한다.
4. 원본 500/첫 48은 보존하고, 최종 풀에서만 25개 원본을 대체한다. pool version은 현지 규칙대로 증가한다.
5. 변경 Geometry에 옛 Candidate ID를 재사용하지 않는다. 기존 안정 ID API로 새 ID를 산출한다.
6. 동일 Geometry/ID의 불변성을 유지하며 Old ID→ReplacementKey→New ID/셀/RuleId/원본 관계를 출력한다.
7. 첫 풀에서 온 #002/#019의 옛 형상도 최종 풀에서는 교체 대상이다. 새 형상을 ‘첫 48 그대로’라고 세지 않는다.
8. 475개 유지+25개 새 배열이므로 예상 첫 풀 원형 유지 46, 그 밖 454다. 실제 분류/집계 의미를 확인한다.
9. 기존 RMAP07/legacy source 정보는 provenance로 남긴다. 옛 source mask가 새 형상을 직접 나타낸다고 기록하지 않는다.
10. 실제 16셀로 PrimaryRole/특성/방향 태그를 재계산한다. OldPrimaryRole/DesignIntent를 강제 분류값으로 쓰지 않는다.
11. 벽에 디딤면을 추가해 실제 분류가 경사로 바뀌면 그대로 기록한다. 기존 역할 수량 유지를 위해 오분류하지 않는다.
12. 원본 의도 태그/출처는 보존하고 변경 형상과 충돌하는 태그만 이유와 함께 조정한다.
13. 500개 고유 배열/ID, VOID_CLEAR 정확히 1, 역할별 실제 집계와 10역할 대표 존재를 검증한다.
14. 이 수정은 25개 피드백 반영이다. 기존 전체 역할 부족분 보강까지 새로 완료했다고 주장하지 않는다.
15. 원본 500에 이미 있는 배열과 겹치지 않는 25개임을 로컬에서 확인했다. 현지 실제 catalog로도 재확인한다.

## 6. 점프·매달림과 배치 문맥

- SurfaceHeightsX0To3는 각 열의 지지 높이다. Moves는 설계 경로이며 실제 물리 trajectory 증거가 아니다.
- 2칸 단계마다 GrabCorners의 안전 고체 상단 모서리 접근/접촉/유지/점프 이탈/상단 착지를 검증한다.
- Grab 자동 진입의 상승 억제, 하강/수평 접근 조건, Down 억제는 기존 RMAP03 의미를 유지한다.
- 실제 baked Tilemap에 유효한 Grab 표면 분류가 연결돼야 한다. S 셀만 있다고 Grab 가능하다고 추정하지 않는다.
- Composite/Tilemap Collider 전체 bounds가 내부 계단 모서리를 숨기는지 확인한다. 각 요구 모서리의 실제 접촉으로 검증한다.
- 문제가 있으면 기존 지형 접촉/표면 adapter를 필요한 범위만 고친다. 수치를 올리거나 모든 bounds를 안전 표면으로 통과시키지 않는다.
- 4×4 위쪽 2타일 여유는 검증 fixture 문맥이다. 실제 생성에서는 해당 이동의 전체 Collider sweep 여유를 판정한다.
- BottomSupportX의 높이 0열은 아래 이웃 고체 지지를 요구한다. AIR 자체를 착지면으로 간주하지 않는다.
- 진입 측에는 EntryFeet 높이의 바닥, 반대편에는 FarSideLandingFeet의 안전 착지를 둔다.
- 마지막 벽을 실제로 넘어 반대편에 착지해야 한다. 상단 근처에 도착한 것만으로 완료하지 않는다.
- 현 수정안의 반대편 낙하는 최대 4타일이며, 기존 낙하 피해 조건을 그대로 검증한다.
- 문맥이 맞지 않는 실제 배치는 부적합 후보로 처리한다. 검증 후 상부를 자동 굴착해 성공시키지 않는다.
- 사용자가 말한 경사·벽 무장비 원칙을 이후 배치에도 적용한다. 미수정 후보의 역할명만으로 통과를 인증하지 않는다.
- 새로 확인된 비대상 결함은 ID/문맥/영향을 보고한다. 무관한 475개를 무작위로 전수 재생성하지 않는다.

## 7. 실제 소비와 좁은 검증

1. 기존 RMAP11 출력과 구분되는 수정 버전의 검수 자료와 작은 물리 gallery를 만든다.
2. Gallery bounds는 가로 12/세로 8 배수다. 25개를 분리된 fixture로 배치하며 실제 16셀을 그대로 bake한다.
3. 각 fixture의 외부 진입 바닥/상부 여유/반대편 착지 좌표를 별도 manifest에 명시한다.
4. 실제 Production Player + TilemapCollider로 25개 수정 후보의 의도 방향 무장비 통과를 focused parameterized 검사한다.
5. 2칸 단계가 있는 후보는 실제 IsGrabbing 진입과 이탈 후 착지 증거를 남긴다. 좌표 강제 설정으로 이동을 대체하지 않는다.
6. 각 사례는 독립 초기화한다. 검사 도중 teleport, 임시 발판, 이동능력 강화로 성공시키지 않는다.
7. 실제 입력/공식 snapshot 경로로 이동하며 모서리/fixture 좌표와 예상·실제 상태, 실패 원인을 출력한다.
8. RMAP11 실제 생성 소비부 전체를 확인한다. non-Type3 detail만 고치고 composer/첫48 fallback이 옛 25개를 재도입하게 두지 않는다.
9. RMAP11 run context의 선택/호환 adapter를 최소 변경해 최종 pool/version을 소비시킨다. 과거 RMAP09/10 증거는 보존한다.
10. 대표 1107 / 36x24 / PortGalleryV1을 재생성해 실제 Player Start→Exit와 새 pool source를 확인한다.
11. 후보별 fixture 검사는 25개 형상 검증이고 대표 Run 검사는 실제 소비 검증이다. 서로 대신할 수 없다.
12. 최종 catalog/CSV/API round-trip, 500개 유일성, 나머지 475개 동일성, 안정 ID/참조/역할/버전 결정성을 확인한다.
13. 같은 입력/새 버전 재생성 digest가 일치해야 한다. 구 버전 맵 digest와 다름은 자동 실패가 아니다.
14. compile/refresh와 직접 영향 tests만 수행한다. 500개 전수 물리/모든 Seed/full regression/build는 요구하지 않는다.

## 8. 검수 기록과 사용자 승인 범위

review.json의 두 실제 사용자 발언을 검수 기록에 보존한다. 이름·판정 시각을 꾸며 넣지 않는다.
기존 HUMAN_REVIEW_PENDING만 되풀이하지 말고 CHANGES_REQUESTED와 반영 내용을 후보 ID별로 기록한다.
사용자는 이 25개 수정과 이후 TASK 진행을 요청했다. 수정 요청을 충족하고 실제 검증이 통과하면 같은 진행 허락을 재요청하지 않는다.
이는 새 형상 PDF를 사람이 다시 보았다는 뜻이 아니다. POST_CHANGE_VISUAL_REVIEW는 NOT_RECORDED로 정확히 남긴다.
검증 통과 후 종료 근거는 USER_AUTHORIZED_CONTINUATION_AFTER_REQUESTED_FIXES다. AI 이미지 검사를 사람 승인으로 쓰지 않는다.
현지 절차에 필요한 기존 review 필드는 실제 사용자 결정/조건부 진행 근거를 함께 기록해 의미를 보존한다.
기능 미충족/실제 Unity 미실행은 이 권한으로 덮을 수 없다. FAIL/NOT_RUN 사유를 보고하고 RMAP11 CURRENT를 유지한다.

## 9. Required Result / RMAP11 Finalize

- 사용자 관점: 수정된 25개, 무장비 점프+Grab으로 새로 통과 가능한 동작, 아직 미확인인 항목.
- 기준선: HEAD/branch/Current, 원본 Task/Archive/Result/CSV와 patch 해시.
- 변경 책임: 파일별 KEEP/ADAPT/NEW, authoritative source와 재생성 경로.
- 매핑: 25개 도감 번호/원본 ID/원본 셀→새 ID/새 셀/실제 역할/수정 이유/Grab 모서리.
- 풀: 500 고유, VOID_CLEAR 1, 475개 유지, 46/454 원형 재사용 집계, 실제 역할/태그/문맥 변화.
- 검증: 실제 Unity version/filter/count/XML, 25개 통과와 2칸 Grab 증거, 실패/수리, 대표 Run 소비/Exit.
- 검수: 실제 사용자 요청과 보완, 반영 결과, 후속 진행 권한, post-change 사람 재검수 미기록 구분.
- 산출물: 수정 pool/usage/provenance/ID map, 전후 16셀 그림, gallery scene/fixture manifest.
- RMAP12 bindings: 실제 최종 pool/API/version/ID/RNG/Save 연결 접점과 EXISTING/PROPOSED 구분.

필수 항목을 확인한 경우만 PASS Result를 작성하고 기존 Finalize로 RMAP11 CURRENT→COMPLETE, Current→NONE 처리한다.
이 시점 상태는 232 COMPLETE / 0 CURRENT / 8 LOCKED이며 RMAP12는 아직 LOCKED다.
RMAP11 소유 변경만 commit한다. 무관한 staged 파일을 포함하거나 임의 unstage하지 않는다. push하지 않는다.
실제 commit SHA, 최종 Result SHA, 설치 Task/Archive SHA를 CLI 보고에 남긴다. 자기참조 해시 때문에 Result를 다시 쓰지 않는다.

## 10. 다음 TASK로 진행

사용자의 후속 진행 요청에 따라 RMAP11 PASS/Finalize/commit 확인 후 RMAP12_PREP.md를 실행한다.
RMAP12는 별도 Task/별도 CURRENT/별도 commit으로 진행한다. 동시에 두 Task를 CURRENT로 만들지 않는다.
RMAP11 검증 전에 RMAP12를 시작하거나 기존 BLOCKED Result를 PASS 선행 증거로 사용하지 않는다.
현지 실행 세션이 Task별 STOP를 강제하면 RMAP12의 바인딩된 MD/해시까지 준비해 다음 호출에 바로 실행할 수 있게 보고한다.
이 문서는 저장소 구현 완료를 주장하지 않는다. 실제 작업 결과와 증거를 생성한 범위만 완료로 표시한다.
