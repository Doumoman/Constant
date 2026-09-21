# SV5_05_ROUTE_STATE — 진행 조건과 지름길 검사

FORMAT: source_spec_v1
TASK_ID: SV5_05_ROUTE_STATE
STATUS: PLANNED_NOT_INSTALLED
PREVIOUS_TASK: SV5_04_CORE_RESERVE
PREVIOUS_RESULT_SHA256: e7f75abeae77693f2190a6cc58dee50fd27cf2ea3bbf589f37140ebc242b073a
PREVIOUS_INSTALLED_ARCHIVE_SHA256: fd1f879e9f3d1ab302a1b2f7eb0b610227d6a7d9d86e7d43d2724978cb05f9a2
실제 single_task_v1 실행 Task는 현지 계약과 Read/Write/API를 바인딩해 별도 발행한다.

## 목표

SV5_04의 core/route 예약에 기존 RMAP13 진행 조건을 정확히 연결하고, 추가 연결이 필수 action·접근 조건·복귀를 우회하는지 검사한다.
실제 자원 6순서의 상태 trace와 지름길 후보집합/접촉 검사 API를 구현한다. 논리 상태 검증과 geometry/Player 준비 수준을 분리한다.
SV5_06 랜덤 공간 배치·전체 지형 수정·runtime Player/전투/저장·Scene Bake는 시작하지 않는다.

## 선행과 바인딩

1. 실제 AGENTS/MCP·02_PROTOCOL·Master/Status/Current 및 선행 Task/Archive/Result를 읽는다.
2. SV5_04 Result/검토 ZIP은 Finalize commit을 담지 않는다. 실제 SV5_04 Finalize·Task 소유 commit을 현지에서 먼저 확인한다.
3. 아직 SV5_04 CURRENT이면 동일 PASS Result/Task/Archive와 실제 focused 결과를 검증하고 기존 승인된 정상 Finalize/commit을 마무리한다. Result를 amend하지 않는다.
4. SV5_04 COMPLETE·Current NONE·SV5_05 LOCKED에서만 이번 바인딩/Apply를 진행한다. 이미 SV5_05 COMPLETE이면 정상 증거를 보고하고 종료한다.
5. SOURCE_LOCK의 MAPDESIGN/PROJECT 기준을 구분해 실제 바이트와 선행 Result를 검사한다. INPUTS/검토 사본은 현지 증거가 아니다.
6. 예상 시작 상태는 285 = 242 COMPLETE / 0 CURRENT / 43 LOCKED이다. 실제 상태를 기록하며 수치/역사적 HEAD에 맞춰 강제하지 않는다.
7. REVIEW_NOTES/REVIEW_FINDINGS를 읽고 실제 04 API와 접촉 사례를 확인한다. 이 패키지의 AIR witness는 의도된 진단 입력이다.
8. RMAP13/Completion 원문은 ZIP에 없었다. 현지 SHA 고정 소스에서 정확한 state/edge/action/Evaluate 시그니처와 test 부작용을 읽어 바인딩한다.
9. source/API 충돌이나 다른 Current는 임의로 덮지 않는다. 실제 범위·기대 입력·거부 원인을 기록한다.

## 필수 Read

- 실제 AGENTS/MCP 계약·상태·Task/Apply/Finalize/commit 절차와 SV5_04 native 증거.
- MCP/SV5/03_RULES_V5.md, 06_BINDINGS_V5.md, 07_FILE_FLOW_V5.md, 08_CORE_RESERVE_V5.md 및 기존 승인 기준.
- MCP/INPUTS/SV5 원문 규칙/계획과 관련 reference/v4 SPECIALS/NETWORK/MOVEMENT 계약.
- MCP/GENERATED/SV5_04의 core/access/route/state geometry/manifest/BINDING/validation/XML.
- MCP/GENERATED/SV5_03의 BINDINGS/CORE_BINDINGS/DATA_SCHEMAS/TASK_COVERAGE와 현재 RMAP13 node/edge/proof 원본.
- Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/Sv5CoreReservationPlan.cs 및 대응 SV5_04 focused test.
- Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/RmapWorldGraphPlanner.cs.
- Assets/_Game/Map/Runtime/WorldGeneration/Validation/GeneratedCompletionSearch.cs 및 실제 RMAP13 직접 test/assembly.
- 실제 WorldData/RMAP15/RMAP16 source 중 graph/port/identity 대응에 필요한 부분. 전체 파일 묶음을 무조건 문맥에 넣지 않는다.
- MCP/INPUTS/SV5_05의 SOURCE_LOCK/REVIEW_NOTES/REVIEW_FINDINGS/STATE_CONTRACT/VALIDATION_CONTRACT/OUTPUT_CONTRACT와 이 명세.

## 확인된 재사용 접점

Sv5CoreReservationPlanner.Plan(Rmap16ClusterAssemblyPlan)은 같은 RMAP15/RMAP16 plan 계보를 보존한다.
Sv5CoreReservationPlan의 RouteSource.Graph, Routes/RouteCells/AccessBindings/GraphBindings/StateGeometry/Digest를 읽는다.
EvaluateTerrainCandidates는 지형 변경 후보 보호 검사이며 state transition checker로 오용하지 않는다.
Route.Condition은 Rmap16Route.Source.Condition을 전달하는 label이다. RouteId로 실제 graph edge를 찾아 조건/action을 대응한다.
RmapWorldGraphPlanner와 기존 상태 key를 재사용한다. API 이름 일부는 확인된 audit 검색 단서이며 임의 시그니처를 만들어 호출하지 않는다.

## 허용 Write

- 실제 기존 WorldGeneration/SectorPlanning 또는 Validation assembly 아래 이번 SV5 route-state 전용 최소 source 파일.
- 제안 이름 Sv5RouteStatePolicy.cs / Sv5RouteStateValidation.cs는 신규 후보이며, 동일 책임이 이미 있으면 재사용하고 bound Write를 확정한다.
- 기존 graph에 변경 없이 후보 분석이 불가능한 경우에만 RmapWorldGraphPlanner.cs의 최소 read-only 분석 접점을 Apply 전에 명시해 확장할 수 있다.
- 그 확장은 기존 default policy/state/action/edge 의미를 바꾸지 않는다. 이전 호출/정적 출력의 동등성을 직접 검사한다.
- 실제 Game.Map.Tests.EditMode assembly 아래 SV5_05 focused test와 필요한 .meta.
- MCP/SV5/09_ROUTE_STATE_V5.md 및 MCP/GENERATED/SV5_05의 명시된 export/시험 증거/검토 ZIP/추적된 임시 _work 파일.
- 정상 이번 Task/Archive/Result·최소 상태 표식과 필요한 기존 SV5_04 완료 마무리 단계.

SV5_04 adapter/test/generated, core identity/셀/port/slot/gate 원본, SV5_03 조사 표와 기존 INPUTS/Task/Archive/Result를 수정하지 않는다.
GeneratedCompletionSearch의 기존 상태 의미를 바꾸거나 별도 거의 같은 상태 FSM을 만들지 않는다.
Scene/Prefab·Player/카메라·NPC/전투/저장·ProjectSettings/Packages/BuildSettings/Addressables·공용 승인/검사 정책은 Write 밖이다.
새 장소/route geometry를 실제 배치하거나 현재 AIR 연결을 편의상 carve/fill하지 않는다. 이 작업은 조건 검사와 후속 해결 의무를 만든다.
Master 45개 계획을 재등록/재정렬하지 않는다. 기존 무관한 dirty 변경을 revert/stage하지 않는다.

## 수행

1. 정상 선행/코드 바인딩을 확인해 별도 BOUND_TASK_SHA를 발행하고 SV5_05만 Apply한다.
2. C01~C04에 따라 실제 graph/route/port 조건을 연결하고 6순서·정식 action·복귀·실패 진단을 구현한다.
3. C05~C06의 후보 및 후보집합/Optional·Required 계약을 실제 checker에 연결한다.
4. C07~C08의 경로 접촉 검사에 실제 세 좌표와 AIR witness를 투입하고 조건별 판정/물리 준비 상태/해결 의무를 기록한다.
5. C09의 불변·결정적 결과에서 OUTPUT_CONTRACT의 CSV/manifest/trace를 생성한다.
6. T01~T10 책임을 focused test로 검사하고 실제 명령/수치/XML을 기록한다.
7. 원본과 허용된 선택적 graph 확장의 변경 범위를 구분해 SHA/diff/입출력 대응/참조/임시 정리를 확인한다.
8. 정상 Result·Finalize·소유 commit 후 검토 ZIP을 만들고 실제 최종 상태/commit/ZIP SHA를 콘솔에 보고한다.

## 완료 기준

- 6가지 자원 순서 각각이 기존 조건과 정상 복귀를 유지하며 정식 Forge→Seal→Boss→Exit trace를 만든다.
- 실제 후보 및 후보집합 검사가 선행 조건 누락/일방향 역행/상태 누락/미지 anchor를 거부하고 합법 지름길은 논리 허용한다.
- route label과 실제 predicate가 구별되고, 실제 geometry/contact가 없으면 물리 통과로 표시하지 않는다.
- 실제 리뷰 접촉/witness를 재현해 처리 결과와 해결 의무를 기록했다. baseline physical readiness가 남으면 false/PENDING으로 유지한다.
- 같은 입력과 열거 순서 변화에 결정적이며 원본 core/route/진행 의미를 보존한다.
- focused 시험을 실제 실행해 통과했다. 기존 XML 복사나 fixture flag 사전 획득으로 통과를 꾸미지 않는다.
- 다음 SV5_06/09/41의 미연결 port·조건 경계 해결/최종 geometry 검사 의무와 실패 gate가 명시되어 있다.
- 보조 파일은 MCP 하위에 있고 이번 임시 자료가 정리됐다.

## 보고와 종료

Result에 독립 TASK/STATUS, 입력/명세/bound/installed/archive 각 SHA, 실제 code/test/export SHA, 검증 명령과 범위를 기록한다.
조건부 graph 확장이 있으면 이전/이후 source SHA와 기존 의미 보존 증거를 기록한다. 선행 snapshot SHA를 변경 후에도 강제하지 않는다.
정상 시작과 같다면 Apply 후 242 COMPLETE / 1 CURRENT / 42 LOCKED, Finalize 후 243 COMPLETE / 0 CURRENT / 42 LOCKED를 기대한다.
Result 작성 시점의 실제 상태를 표시한다. Finalize 이후 상태/commit은 콘솔로 보고하며 Result amend는 하지 않는다.
SV5_06 이후·RMAP18/19·VIS 후속·전체 회귀·PlayMode·build·Scene Bake·push는 진행하지 않고 멈춘다.
이번은 한 Task 실행이다. 묶음 실행 전환은 포함하지 않는다.
