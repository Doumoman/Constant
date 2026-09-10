# RMAP16_CLOSE - 완료 근거 확인·필요한 보완 후 RMAP17 순차 진행

- MODE: 기존 RMAP16의 근거 확인/필요한 보완과 다음 Task 실행을 위한 사용자 인계 지시
- 새 Task ID를 등록하는 single_task_v1 문서가 아니다. 이 파일 자체를 새 Task로 Apply하지 않는다.
- INPUT: MapDesign/MCP_INBOX/RMAP16_CLOSE.md
- 먼저 처리할 Task: RMAP16_CLUSTERS
- 다음 Task: RMAP17_WORLD_BAKE. RMAP16 실제 PASS/Finalize/commit 이후 별도 정상 Apply.
- 다음 미실행: RMAP18_WORLD_STATE
- 기존 순차 진행의 연속 작업이다. 이미 허용된 근거 확인·수정·후속 진행에 같은 허락을 다시 묻지 않는다.

## 1. 이번 확인 이유와 고정 근거

제출 보고서는 STATUS PASS, 전체 259,584 typed 셀, 4개 Cluster/13청크/78 Pattern 배치, 11개 경로 Spine을 보고했다.
RMAP15 고정 셀 보존, 2청크 비밀 공간, patch 밀도 달성과 focused EditMode 3/3 PASS도 보고했다.
그러나 최종 지형의 이동 판정/6개 자원 순서, 나머지 청크의 조립 출처와 활성 비율, Cluster 밀도 근거는 생략되어 있다.
이것만으로 구현 실패라고 단정하지 않는다. 현지 코드·CSV·XML·그림에서 이미 있는 근거를 먼저 찾고 부족한 책임만 보완한다.
테스트 3개 또는 Cluster 4개라는 숫자만으로 탈락시키지 않는다. 새 최소 Cluster 수/활성 비율을 임의로 추가하지 않는다.

| 제출 근거 | 값 |
|---|---|
| RMAP16 Result | MapDesign/MCP/REPORTS/RMAP16_CLUSTERS_RESULT.md |
| 제출 Result SHA-256 | 3f75bf5a8c31c2f9e6e9c2f7eabe63168c9489401da8ac3e43770fc7a2f67d43 |
| RMAP16 설치 Task/Archive SHA-256 | 9b7c78e2c8257a5baf1d679b562763a9db4e478f42676e4d94e730ef4b2691ca |
| RMAP15 PASS Result SHA-256 | 734a09ce776e95986b3fca0ee2e8c1ad6323a48d64ae0ba8347779f209c1274c |
| RMAP15 설치 Task/Archive SHA-256 | 96bb700defb9e494563850004455983022b793b9fe21948294403fb9b99afa80 |
| 보고된 RMAP15 완료 commit | cabbd24b94aa3bcbcc9ae852b2f5da5ff4356d3f |
| 현지 검토 ZIP | MapDesign/MCP/GENERATED/RMAP16/RMAP16_REVIEW.zip |

위 SHA는 제출본 기준이다. 현지 Result가 이후 보완됐다면 이력/현재 파일/증거로 설명 가능한 변경인지 확인한다.
RMAP16_REVIEW.zip은 제출 보고서에서 존재가 언급됐으며 이 인계 작성자는 내부 CSV/PNG/코드를 직접 확인하지 못했다.
자료가 이미 충분하면 링크·메서드·실제 수치만 보강한다. 확인되지 않은 실패나 실행 증거를 새 사실로 쓰지 않는다.

## 2. Preflight / 상태·해시·변경 경계

1. 외부 전달 SHA와 이 MD 전체 원본 bytes를 대조한다. 자기 해시 삽입·정규화·검사 수정으로 맞추지 않는다.
2. 프로젝트 루트/AGENTS.md, MapDesign/MCP/00_MCP_ENTRYPOINT.md, 현행 RMAP 프로토콜/Apply/Finalize를 읽는다.
3. branch/HEAD/git status, Current/MASTER/STATUS, RMAP16 Task와 Archive의 동일 bytes, 선행 체인을 확인한다.
4. RMAP16 CURRENT이면 그대로 재개한다. RMAP 부분집합 기준 15 COMPLETE / 1 CURRENT / 3 LOCKED다.
5. 이미 COMPLETE면 완료 commit과 Result 이력을 확인한다. 정상 부분집합은 16 COMPLETE / 0 CURRENT / 3 LOCKED다.
6. 완료본 수정이 필요하면 현지의 지원되는 수정/재개 절차를 따른다. 새 Task 행/임의 CURRENT 전환/운영 검사 우회 금지.
7. 다른 CURRENT 또는 미설명 상태·해시 충돌이면 STATUS_CONFLICT로 경로·expected·actual을 보고한다.
8. 무관한 변경은 보존한다. reset/stash/강제 덮어쓰기, 무관한 stage/unstage, git push 금지.
9. 설치 RMAP16 전체 요구와 GENERATED/RMAP01/file_bindings.csv의 CLUSTER/SECRET/COMPOSER/DENSITY/PROFILE/PORT/SPINE을 읽는다.
10. RmapClusterAssemblyPlanner.Plan(RmapWorldDefinition, WorldGenerationRngStreams)와 explicit overload를 실제 파일/호출자로 찾는다.
11. RMAP16 manifest/설정·cells/chunks/clusters/patterns/ports/routes/secrets/state geometry/density/shape summary와 기존 검사·그림을 읽는다.
12. RMAP12~15 선행 출력은 이번 소비 경계에 필요한 범위만 읽는다. 과거 전체 Task/seed/500개 물리 증거를 재감사하지 않는다.

쓰기 범위는 설치 RMAP16의 조립/정적 이동/보호/비밀/설정·측정/후속 소비와 직접 영향 검사·생성 자료·Result·상태다.
RMAP15 site·고정 셀, RMAP14 ownership/승인 경계, RMAP13 진행 의미, RMAP11 수정 pool, Player 수치는 보존한다.
메서드별 KEEP/ADAPT/NEW와 실제 경로를 먼저 기록한다. 미확인 클래스 경로를 꾸미거나 별칭마다 새 Service를 만들지 않는다.

## 3. C1 / 13청크 바깥을 포함한 실제 조립과 생성 순서

- 4개 Cluster의 4+4+3+2=13청크와 78=13×6 Pattern 배치는 그 영역의 근거다. 전체 2,704청크 조립의 충분성을 별도 확인한다.
- 2,704청크 전체의 공간 상태/실제 셀 소유/Cluster·특수·비밀·일반 경로·기타 합법적 원본을 중복 없이 집계한다.
- Cluster 수를 억지로 늘릴 필요는 없다. 나머지 영역이 무엇이며 어떤 생성 원본·규칙으로 만들어졌는지 확인해야 한다.
- INACTIVE_SOLID는 내부 플레이 공간 없는 고체 영역이다. 공기를 흩뿌린 청크에 이 라벨만 붙여 완료하지 않는다.
- 일반 Pattern 조립 대상으로 분류한 청크는 3×2 후보/원점/16셀/transform/현재 pool digest를 추적한다.
- 특수·비활성·봉인 등 다른 합법적 source kind/mask는 분리한다. 사후 변경 셀에 원래 후보 ID를 그대로 붙이지 않는다.
- 수정한 25개 형상은 기존 old→new ID 매핑으로 현행 pool 및 composer/fallback 소비를 확인한다. 전수 재배치·재시험은 불필요하다.
- 실제 순서가 예약→큰 형상/경로→Type/Port→Spine·필요 여유/지지→Pattern→Overlay인지 호출·데이터 의존성으로 확인한다.
- 밀도 수치만 맞추는 임의 S/A 산포와 사후 통로 굴착이 전체 조립을 대신하는지 확인한다. 그렇다고 미리 단정하지 않는다.
- 생성 전에 여유/지지 조건을 정해 후보를 고르는 것과, 완성 지형을 지워 경로를 만드는 것을 구분한다.
- 문제가 확인되면 기존 형상/후보 선택과 유한 국소 재선택으로 해당 생성 책임을 보완한다. 마지막 Base 배열의 수작업 수리 금지.
- 최종 259,584 좌표의 단일 typed Base/소유/출처·별도 Overlay, 재현 가능한 설정·ID·digest를 보존한다.

## 4. C2 / 경로선에서 최종 지형의 이동 판정까지

- 보고된 11개 Spine 각각의 실제 site/port/열린 전체 셀, OUT→IN 방향과 최종 Base·Overlay 기반 내부/경계 연결을 추적한다.
- 좌표선·AIR BFS·높이 차이·profile 문자열은 이동 증명의 대체물이 아니다. 기존 이동 판정 호출과 구체 assertion을 확인한다.
- `PLAYER_0.4x0.8 | STEP_MAX_1 | JUMP_MAX_2 | GRAB_REQUIRED_FOR_VERTICAL`의 코드상 의미를 실제 공유 profile과 대조한다.
- `JUMP_MAX_2`가 합성 이동 상한의 이름일 뿐이면 그 뜻을 명확히 한다. 실제 점프 높이 2칸을 가정했다면 판정을 수정한다.
- 플레이어는 1칸 점프, 최대 2칸 점프+안전 모서리 Grab이다. 2칸 벽은 접근·노출 모서리·Grab 면·머리 공간·상단 이탈/착지를 검증한다.
- 1칸 높이를 WALK의 무조건 step-up으로 통과시키지 않는다. Production Player가 요구하는 점프 동작을 판정에 반영한다.
- 수평 이동·점프 궤적 여유·출발/착지 지지면·낙하/ONE_WAY·등반 문맥은 기존 계약으로 처리한다. 새 만능 물리 시뮬레이터를 만들지 않는다.
- 경사·벽을 장비/사다리 필수로 바꾸거나 Player 수치를 높이지 않는다. 2칸 초과 상승은 중간 지지면으로 나눈다.
- 필수 지형을 정적 판정할 수 없으면 미확인으로 남기고 원인 범위만 해결한다. 전체 Player 주행은 RMAP16 필수 조건이 아니다.
- 실제 최종 지형으로 판정한 방향 간선에 자원/Forge/Seal/Boss 상태 조건을 연결한다. RMAP13의 계획 그래프만 다시 검사하면 부족하다.
- 세 자원의 정확한 6개 획득 순서 각각이 Forge→Seal→Boss→Exit로 이어지는 경로와 복귀 근거를 출력한다.
- 같은 지점의 획득 전후 상태를 구분하고 역방향 간선을 자동 생성하지 않는다. 잘못된 순서로 자원을 미리 얻은 경로를 세지 않는다.
- 현행 Optional/Required 복귀 정책과 실제 사용값을 기록한다. Required이면 해제 조건·좌표·영향 셀·개방 후 복귀 지형이 있어야 한다.
- 공유 SealBoss의 폐쇄 3셀/개방 3셀 상태와 조건을 유지하고 폐쇄 상태의 물리 공간 우회 여부를 확인한다.
- Village는 필수 자원 획득 순서에 강제 삽입하지 않되 실제 일반 경로에서 접근 가능해야 한다.
- 기존 검사에 근거가 부족할 때만 머리 공간/Grab 면/착지면 또는 일방향 복귀를 깨뜨린 직접 사례의 실패 검출을 보강한다.
- 이미 승인된 어려운 선택 배치는 보존한다. 필수 단절·필수 이동 미확인을 PASS로 표시하지 않는다.

## 5. C3 / 보호 gate와 비밀 공간의 최종 결과

- EvaluateTerrainCells가 실제 일반 생성 경로에 연결된다는 기존 근거를 재사용하고 최종 RMAP15 고정 2,432셀/포트 45셀/슬롯 10개를 대조한다.
- 보고된 10,096 gate 호출의 의미와 거부 후 처리를 확인한다. 거부된 셀 쓰기를 건너뛰었다는 사실만으로 전체 후보가 유효해지지 않는다.
- 후보 일부가 거부되면 최종 패턴 출처·경로·지지면의 성립을 확인하고 필요하면 해당 후보를 재선택한다.
- protected-safe는 특수 예약 비침범을 뜻한다. 경로 전체의 Collider 여유·이동 가능성까지 자동 증명하지 않는다.
- 2청크 비밀 공간의 실제 내부 연결, 지역 외부 일반 입구 0개, BreakableAccess와 파괴 전후 영향을 최종 주변 지형에서 확인한다.
- 파괴 전 주변 우회 일반 진입 부재와 파괴 후 같은 입구 복귀 또는 안전 출구를 확인한다. 내부 통로를 외부 입구로 세지 않는다.
- 위치가 있는 2종 단서가 일반 탐험 공간 쪽에서 발견 가능하며 필수 역할/유일 경로가 봉인 내부에 없음을 확인한다.
- 위 상태는 정적 파괴·봉인 입력이다. 실제 도구/효과/전투/저장 런타임은 아직 구현 완료로 주장하지 않는다.

## 6. C4 / Cluster 밀도·활성 비율·후속 소비

- patch 측정 474/599/699 permille은 해당 patch 집계의 근거다. 설치 Task가 요구한 Cluster/인접 MicroChunk 묶음 측정도 확인한다.
- 실제 scope mask마다 S/A/O 개수와 S/(S+A+O), 포함된 고정/비활성 셀 정책·목표/편차를 기록한다. O/Overlay를 S에 더하지 않는다.
- 첫 생성 전 설정된 Type0 빈도(개방/봉인), 실제 활성 비율 목표, biome/형상 가중치·시도 상한과 소비 코드를 확인한다.
- ACTIVE·SECRET·INACTIVE_SOLID·SPECIAL_RESERVED의 실제 수/분모 2704와 중복 없는 부분 특수 청크 정책을 제시한다.
- 개방 Type0 청크 수와 봉인 지역 수/구성 청크 수는 서로 다른 분모로 보고한다. 설정값을 측정값처럼 출력하지 않는다.
- RMAP14 planning Active 복사나 생성 후 목표값 역산으로 설정했다면 실제 사전 설정·측정 책임을 보완한다.
- 밀도는 튜닝 목표이며 4×4 전수 탈락 기준이 아니다. 수치를 맞추려고 이동 경로·보호·형상을 훼손하지 않는다.
- 기존 RUN06 관찰 접점으로 최종 형상 중복/유사성 요약을 재사용한다. 재질만 바뀐 구조를 다양성으로 세지 않는다.
- RMAP17이 받을 공개 snapshot/API 또는 기존 Bake 입력 adapter에서 대표 셀·소유·Overlay·상태 입력의 동일 전달을 확인한다.
- 공개 API/exporter/Editor 진입점과 실제 파일 경로를 남긴다. 시험 코드만 가진 독립 데이터 정본으로 끝내지 않는다.

## 7. RMAP16 최소 보완·결과·Finalize

1. C1~C4를 구현/기존 증거 충분, 보고만 보완, 기능 보완 필요, 미확인으로 나누고 실제 경로·근거를 기록한다.
2. 현지 ZIP/CSV/XML을 먼저 읽는다. 필요한 자료가 없으면 기존 공개 API로 생성하며 손으로 PASS 표나 CSV를 만들어 채우지 않는다.
3. 구현이 충분하면 기능 수정/추가 시험 없이 근거를 연결한다. 빠진 책임이 확인될 때만 해당 부분을 수정·재검증한다.
4. 기존 focused 3개 및 선행 검사를 재사용한다. broad/full/unfiltered regression, 테스트 수 채우기, 전수 seed/500개 물리, 불필요한 build 금지.
5. 변경 후 전체 지형과 경로/경사·Grab/특수 접근/비밀 전후 확대도를 같은 최종 데이터에서 생성하고 직접 연다.
6. Result에 C1~C4 수치·공개 API·정적 이동/6개 순서 근거·실제 수정/재사용·검사 범위를 적는다. 기존 제출 이력은 보존한다.
7. XML/manifest/CSV/주요 그림의 실제 SHA를 기록하고 RMAP16_REVIEW.zip을 최신 근거로 갱신한다. ZIP 자체 해시를 ZIP 내부에 넣지 않는다.
8. 필수 미확인/불충족이면 FAIL/BLOCKED와 정확한 원인을 보고하고 RMAP17을 열지 않는다. PASS 기준을 줄여 다음으로 넘기지 않는다.
9. 충분하면 PASS Result의 실제 최종 SHA를 계산한다. 다음 Task에 제출 당시 SHA를 자동 재사용하지 않는다.
10. RMAP16 CURRENT이면 기존 Finalize로 COMPLETE, Current NONE 처리한다. 이미 정상 완료면 중복 Finalize하지 않는다.
11. RMAP16 소유 변경만 commit하고 최종 CLI에 실제 commit/Result SHA/설치 Task·Archive SHA와 상태를 남긴다.
12. RMAP 부분집합 완료값은 16 COMPLETE / 0 CURRENT / 3 LOCKED다. 전체 MASTER 240행이면 237/0/3이다.
13. 실제 집계를 명시하며 숫자에 맞춰 상태를 임의 변경하지 않는다. RMAP17은 아직 LOCKED여야 한다.

## 8. RMAP17 실행 MD 발행·정상 Apply

RMAP16 완료가 확인되면 기존 RMAP17_WORLD_BAKE 명세와 아래 전체 요구를 현지 경로에 바인딩해 300줄 이하 실행 MD로 발행한다.

- task_id/sets_current_task: RMAP17_WORLD_BAKE
- task_file: TASKS/RMAP17_WORLD_BAKE.md
- requires_current_task: NONE
- requires_completed_task: RMAP16_CLUSTERS
- requires_result: REPORTS/RMAP16_CLUSTERS_RESULT.md / PASS / §7 이후 실제 최종 SHA
- requires_installed_task: TASKS/RMAP16_CLUSTERS.md / 실제 Task·Archive 공통 SHA
- 발행 위치: MapDesign/MCP_INBOX/RMAP17_WORLD_BAKE.md
- 현행 single_task_v1 지원 필드를 사용하고 미확인 값/TODO/가짜 SHA·자기 해시를 넣지 않는다.
- 완성 MD 전체 bytes SHA를 외부 보고/현지 지원 등록 경로에 남긴다. planned 원본 SHA와 혼동하지 않는다.
- 다음 구현의 실제 Read/Write 경계·KEEP/ADAPT/NEW를 명시하고 정상 Apply로 RMAP17만 CURRENT로 연다.
- 현지 운영 규정이 Task별 별도 호출을 요구하면 여기서 실행 MD와 외부 SHA를 제공하고 STOP한다. 규정을 우회하지 않는다.

## 9. RMAP17 전체 요구 / A04·E08

### 목표와 실제 바인딩

624×416 전체 정적 월드를 실제 Unity Tilemap·Collider에 적용하고 Production Player 시작 위치·연속 카메라를 연결한다.
순수 명령 목록/PNG/작은 fixture만으로 완료하지 않는다. 실제 전체 월드 Scene을 저장하고 열어 조작 가능한 결과를 제공한다.

- Read: RMAP01 BAKE/COLLIDER/WORLD/PLAYER/CAMERA 바인딩, RMAP12 정의와 검증 완료 RMAP16 snapshot/API·전체 셀/상태 입력.
- Read: RMAP15 실제 Start/고정 셀/슬롯, 현행 Player·Grab/ONE_WAY·Overlay 의미와 작은 Run 실제 Bake/카메라 소비 경로.
- 기존 접점: Assets/_Game/Live/Editor/RMAP10/RmapSmallRunSceneBuilder.cs와 직접 호출자. 현재 경로가 다르면 실제 구현에 적응한다.
- Write: 전체 Bake adapter/생성·정리 소유 handle, 전용 전체 Scene/필요한 부분 갱신 연결, Player 시작/카메라 bounds 배선.
- Write: 직접 관련 focused tests, GENERATED/RMAP17 자료, Task/Archive/Result와 정상 상태 기록.
- 게임 이동 수치/prefab·RMAP16 지형·pool·특수 위치를 Bake 편의로 변경하지 않는다. 새 RNG/정의/저장 시스템을 만들지 않는다.

### A04 / 실제 크기·원점·격자

1. FullRun은 624×416=259,584셀, Pattern 156×104=16,224자리, Chunk 52×52=2,704자리다.
2. FullRun 설정으로 전달한다. 가로 12·세로 8 배수인 기존 가변 시험 맵을 전역 상수 변경으로 깨지 않는다.
3. Tile 1×1, 좌하단 원점, local→world Tile/Chunk 변환을 보존한다. 빈 AIR 영역도 명시적인 world bounds 안에 포함한다.
4. RMAP16 최종 Base/Overlay/소유/상태 자료를 그대로 소비한다. 같은 seed로 다른 별도 지형을 생성하지 않는다.
5. INACTIVE_SOLID·부분 특수 청크·고정 보호 셀·봉인 상태를 보존하며 4×4/12×8 경계에 틈/중복 충돌/추가 벽을 만들지 않는다.

### E08 / 실제 Tilemap·Collider·Player·Camera

1. 작은 Run의 검증된 Bake 경로를 확장한다. 한 타일마다 GameObject를 만드는 방식을 기본으로 사용하지 않는다.
2. SOLID는 실제 고체 충돌, AIR는 비충돌 빈 셀, ONE_WAY는 기존 위쪽 단방향 충돌 layer/effector로 구현한다.
3. LADDER/CLIMB_PILLAR Overlay는 Base와 별도이며 기존 trigger/소비 의미를 유지한다. 표시 타일만 놓고 동작 완료로 보고하지 않는다.
4. 실제 Tilemap/Collider 구성·layer·필요한 갱신 완료를 확인한다. 사용 API는 현지 Unity 버전·기존 구현으로 검증한다.
5. 결과를 다시 Bake할 때 자신이 생성한 root/layer/handle만 정리한다. 역사 Scene/사용자 객체·무관한 Tilemap을 삭제하지 않는다.
6. RMAP15 Start 슬롯의 발바닥 world 위치/지지면/Collider 여유에 기존 Production Player를 배치한다. 임의 안전 플랫폼을 덧붙이지 않는다.
7. 기존 입력/이동을 연결하고 실제 입력으로 시작 지지면·이동·대표 점프/Grab·청크 경계 통과를 확인한다.
8. 1칸 점프와 2칸 점프+Grab은 실제 최종 지형의 해당 사례에서 확인한다. Player를 목표 위치로 옮긴 것을 통과 증거로 세지 않는다.
9. 기존 연속 12×8 camera와 world bounds clamp를 연결한다. 청크마다 방 전환/snap을 다시 켜지 않는다.
10. 카메라 중심뿐 아니라 viewport 반크기를 고려한 clamp를 기존 구현대로 적용하고 가장자리/모서리에서 월드 밖 노출을 확인한다.
11. 이동 중 청크 경계에서 카메라가 불필요하게 튀지 않는지 확인한다. 카메라 검사용 위치 설정은 실제 Player 주행 증거와 구분한다.
12. RMAP16 봉인/비밀 초기 상태를 정확히 Bake한다. 부분 상태 갱신이 필요하면 기존 영향 셀/영역 접점을 연결하되 RMAP18 런타임 저장을 선행하지 않는다.
13. 생성·Tilemap 적용·Collider 준비 비용을 실제 범위와 환경에서 관찰한다. 임의 성능 목표/불필요한 전면 최적화를 추가하지 않는다.
14. 지형 모순이 재현되면 원본 책임/좌표를 보고하고 필요한 현지 수정 절차를 따른다. Bake에서 AIR로 굴착하거나 검증 기준을 낮추지 않는다.

### 실제 산출물과 focused PASS

- 전용 전체 월드 Scene, 생성 메뉴/API/조작법, seed/버전/원본 digest·world bounds/타일·Collider/layer·Player/Camera manifest.
- 전체 입력 좌표의 S/A/O→실제 Tilemap 변환 일치와 예약 셀·포트·슬롯·상태 보존을 확인한다. 물리 query는 대표 위험 구간에 집중한다.
- 실제 Player 시작/대표 경사·2칸 Grab·청크 및 biome 경계, O/등반이 사용된 해당 구간의 직접 물리 검증을 수행한다.
- 원래 전체 지형에서 대표 구간만 격리해 시험하면 원본 좌표/동일 셀 근거를 남긴다. 전체 Scene Bake 확인은 별도로 유지한다.
- 카메라 연속 추적·경계 clamp, 재Bake의 소유 객체 정리/중복 부재, 변경한 부분 갱신이 있다면 그 실제 동작을 확인한다.
- compile/refresh, 실제 filter/job/count/XML·검사 범위·미실행, 전체 Scene과 Player/경계 확대 화면을 기록한다.
- 정상 선행 검사 재실행·전체 seed/500개 물리·broad/full/unfiltered regression·불필요한 build 금지. 새 연결의 구체 위험만 검증한다.
- 정적 생성기가 live Player/Camera를 요구하지 않게 유지한다. Scene 표현 계층에서 생성 결과에 Player/Camera를 연결한다.
- 전체 지형·충돌·Player 시작·연속 카메라의 실제 결과가 확인되면 A04/E08 PASS다. 전체 6순서 실주행·보스/경제 완료까지 확대 주장하지 않는다.

### Result / Finalize / Commit / STOP

- Result: MapDesign/MCP/REPORTS/RMAP17_WORLD_BAKE_RESULT.md
- TASK/STATUS, 사용자 관점 실제 가능한 조작, 파일별 책임·KEEP/ADAPT/NEW·비소유 범위, preflight/해시를 기록한다.
- A04/E08 실제 Scene/API/크기·원점/전체 셀 일치/예약·상태/Player·Camera 증거와 비용 관찰을 연결한다.
- 실제 Unity version/filter/job/count/XML/미확인·실패·수리·재사용을 구분한다. Scene/주요 자료 SHA와 원본 digest를 남긴다.
- 상태·해시 충돌은 STATUS_CONFLICT, 필수 자료/도구 부재는 BLOCKED, 구현 불충족은 FAIL이다. 실제 필수 동작 미확인으로 PASS하지 않는다.
- PASS 후 기존 Finalize로 RMAP17 COMPLETE, Current NONE 처리하고 RMAP17 소유 변경만 별도 commit한다.
- 부분집합은 적용 후 16 COMPLETE / 1 CURRENT / 2 LOCKED, 완료 후 17 COMPLETE / 0 CURRENT / 2 LOCKED다.
- 전체 MASTER 240행이면 완료값은 238/0/2다. 실제 상태/집계 범위를 함께 보고하고 억지로 숫자를 맞추지 않는다.
- 최종 CLI에 RMAP16 보완/완료 근거와 RMAP17 Result/설치 Task SHA·실제 commit·Current NONE을 구분해 보고한다.
- 보고서 작성 뒤 생긴 commit SHA는 최종 CLI에 남긴다. 자기참조 해시 때문에 보고서를 다시 쓰지 않는다.
- 사용자에게 RMAP16_REVIEW.zip과 RMAP17 결과/실제 Scene 경로를 제공한다. RMAP18_WORLD_STATE LOCKED, git push 없이 STOP한다.
