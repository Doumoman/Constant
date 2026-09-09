```yaml
mcp_patch:
  format: single_task_v1
  task_id: RMAP16_CLUSTERS
  task_file: TASKS/RMAP16_CLUSTERS.md
  requires_current_task: NONE
  requires_completed_task: RMAP15_SPECIALS
  requires_result:
    path: REPORTS/RMAP15_SPECIALS_RESULT.md
    status: PASS
    sha256: 734a09ce776e95986b3fca0ee2e8c1ad6323a48d64ae0ba8347779f209c1274c
  requires_installed_task:
    path: TASKS/RMAP15_SPECIALS.md
    sha256: 96bb700defb9e494563850004455983022b793b9fe21948294403fb9b99afa80
  sets_current_task: RMAP16_CLUSTERS
```

# RMAP16_CLUSTERS - 연속 지형·이동 경로·비밀 지역의 전체 정적 조립

```text
TASK: RMAP16_CLUSTERS
DOCUMENT: v4.2 / RMAP15 PASS 이후 실행 지시서
STATUS: CURRENT (정상 Apply 이후)
INPUT: MapDesign/MCP_INBOX/RMAP16_CLUSTERS.md
EXPECTED_RESULT: MapDesign/MCP/REPORTS/RMAP16_CLUSTERS_RESULT.md
NEXT: RMAP17_WORLD_BAKE
NEXT STATUS: LOCKED / DO NOT START
```

이 MD는 300줄 이하의 한 Task 계약이다. 문서 발행만으로 저장소 상태를 바꾸지 않는다.
이번 책임은 A02/A21/A26/A27/W06이다. 예약→큰 형상→Port→Spine/보호→Pattern→Overlay의 실제 정적 셀을 완성한다.

## 1. User-Facing Goal / 완료 모습

624×416 월드에서 RMAP15의 특수 지역 8곳을 보존하고, 그 주변에 여러 청크를 잇는 경사·동굴·통로·하프파이프를 만든다.
필수 장소 사이의 실제 지형 경로와 선택 탐험 공간, 봉인 비밀 지역을 전체 지도와 셀 확대도에서 확인한다.
Cluster ID/사각형/경로선만 만드는 것으로 완료하지 않는다. RMAP17이 그대로 받을 전체 Base 셀과 별도 Overlay를 출력한다.
전체 Tilemap/Collider·Production Player/Camera 실현은 RMAP17이다. 정적 지형 검증과 실제 플레이 검증을 구분한다.

## 2. Preflight / 선행 마무리와 정상 Apply

1. 프로젝트 루트/AGENTS.md, MapDesign/MCP/00_MCP_ENTRYPOINT.md, 현재 RMAP 프로토콜과 Apply/Finalize 절차를 읽는다.
2. 외부 전달 SHA와 inbox 전체 원본 bytes를 먼저 비교한다. 본문 자기 해시나 planned 원본 SHA로 대체하지 않는다.
3. 부재/불일치/등록 SHA 충돌이면 경로·expected·actual을 보고한다. 정규화·재저장·검사 수정으로 우회하지 않는다.
4. YAML의 RMAP15 PASS Result SHA와 설치 Task/Archive SHA·bytes 동일성을 확인한다.
5. 제출 Result는 Finalize/commit 전 문서다. 실제 RMAP15 COMPLETE, Current NONE, 상태 이력/완료 commit을 확인한다.
6. 아직 RMAP15 CURRENT이며 PASS/증거가 유효하면 기존 규정의 RMAP15 Finalize·소유 commit만 먼저 마무리한다.
7. 선행 마무리는 RMAP15 이력으로 분리한다. RMAP16을 먼저 열거나 선행 Result를 임의 재작성하지 않는다.
8. 선행 기능·증거·상태가 설명 없이 달라졌다면 차이를 보고한다. 문서의 PASS만으로 실제 완료를 추정하지 않는다.
9. RMAP 부분집합 시작 기준은 15 COMPLETE / 0 CURRENT / 4 LOCKED다. 전체 MASTER 집계와 구분한다.
10. 전체 MASTER가 여전히 240행이면 대응값은 236 COMPLETE / 0 CURRENT / 4 LOCKED다. 실제 집계 범위를 기록한다.
11. branch/HEAD/git status, 다른 CURRENT 부재, 단일 inbox 후보와 등록 ID를 확인한다.
12. §3 별칭을 실제 경로로 바인딩하고 KEEP/ADAPT/NEW·직접 호출자·허용 쓰기 경계를 사전 기록에 확정한다.
13. 정상 Apply로 RMAP16만 LOCKED→CURRENT, Current NONE→RMAP16_CLUSTERS를 수행하고 동일 bytes Task/Archive를 설치한다.
14. 같은 CURRENT 재개는 입력/설치/Archive 동일성과 기존 진행을 확인한다. 이미 COMPLETE+유효 PASS면 보고하고 STOP한다.
15. 무관한 변경은 보존한다. reset/stash/강제 덮어쓰기, 무관한 stage/unstage, push는 하지 않는다.

## 3. Read Allowlist / 실제 바인딩

- MapDesign/MCP/RMAP/{00_BASELINE_V4_2,01_SEQUENCE_V4_2,02_PROTOCOL_V4_2}.md와 필요한 현지 운영 문서.
- MASTER_IMPLEMENTATION_TASK_LIST/06_IMPLEMENTATION_STATUS, RMAP15 Task/Archive/Result와 실제 생성 자료.
- GENERATED/RMAP01/file_bindings.csv의 CLUSTER/SECRET/COMPOSER/DENSITY 및 직접 연결 PORT/SPINE/OVERLAY/PROFILE/EDITOR.
- RMAP12 정의/RNG/ID, RMAP13 그래프·상태·복귀 정책, RMAP14 실제 16 patch/경계/밀도 설정과 출력.
- RMAP15 fixed cells/access/slots/ownership/state geometry, 기존 TerrainCluster/SecretRegion/보호·파괴 상태 계약.
- 현행 RMAP11 수정 완료 pool/25개 old→new 매핑·승계 기록, 실제 RMAP08~10 composer/이동 판정/소규모 소비부.
- 기존 RUN06 형태·중복·밀도 관찰 접점과 이번 변경의 직접 호출자/검사. 과거 전수 감사는 다시 실행하지 않는다.

| 보고서로 확인된 경로/API | 이번 소비·적응 책임 |
|---|---|
| Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/RmapSpecialReservationPlanner.cs | 8 site/고정 셀/포트/슬롯/봉인 상태 입력 |
| RmapSpecialReservationPlan.EvaluateTerrainCells | 일반 지형 후보의 FixedSolid·ProtectedAir 침범 거부 |
| Assets/_Game/Map/Runtime/WorldGeneration/Biomes/RmapWorldBiomePlanner.cs | 실제 16 patch/2704 ownership/승인 경계/밀도 목표 |
| Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/RmapWorldGraphPlanner.cs | 8 논리 역할/조건·방향/6개 자원 순서/복귀 정책 |
| Assets/_Game/Map/Runtime/WorldGeneration/WorldData/RmapWorldDataContract.cs | 정의/버전/기존 stream/안정 ID 재사용 |
| Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/RmapPatternPool500.cs | 현행 BuildFinalPool()의 수정 완료 500개 typed 형상 |
| Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/RmapComposer.cs | 기존 후보 선택/보호/재시도/Overlay의 전체 조립 적응 |
| 같은 RunGeneration의 RmapSmallRunHarness.cs | 검증된 가변 크기·Port/조립 소비 접점 재사용 |
| CLUSTER/SECRET/DENSITY 실제 바인딩 | 기존 형상·봉인·설정·관찰 책임 KEEP/ADAPT 우선 |

경로/API가 바뀌었다면 현지 바인딩으로 적응한다. 별칭마다 새 Service를 만들거나 미확인 API를 기존 구현으로 단정하지 않는다.
RMAP11 최초 BLOCKED 보고서는 수정 전 이력이다. 현지 완료본을 소비하고 이미 유효한 사용자 수정·진행 승인을 다시 요구하지 않는다.

## 4. Write Allowlist / 책임 경계

- Cluster 큰 형상/경로·활성 청크 배치·Type0 비밀 지형과 직접 연결된 composer/보호/이웃 계약의 최소 확장.
- Type0 빈도/활성 비율/형상·밀도 가중치 설정, 공개 전체 정적 생성 API와 기존 preview/exporter 연결.
- 비밀 입구·단서·파괴 전후 정적 상태/안정 ID 접점, 후속 MechanismZone 슬롯·공간 제약.
- 직접 관련 focused tests, GENERATED/RMAP16 자료, 지정 Task/Archive/Result와 현지 상태 기록.
- 필요한 최소 파일/assembly/.meta. 기존 구현을 새 RMAP 전용 병렬 정본으로 복제하지 않는다.

RMAP15 site 위치·고정 셀·슬롯, RMAP14 ownership/승인 경계, RMAP13 진행 의미, RMAP11 pool 형상은 입력으로 보존한다.
Player 수치/prefab·Camera, 도구 동작·보스/경제/정식 장치·보상 배치, RMAP17 전체 Bake, RMAP18 저장 런타임은 범위 밖이다.

## 5. W06 / 실제 입력과 생성 순서

1. 공개 생성 경로가 RMAP12 정의+RMAP13+RMAP14+RMAP15의 실제 계획을 소비하게 한다. 테스트 전용 월드를 별도 정본으로 만들지 않는다.
2. Tile 1×1, Pattern 4×4, MicroChunk 12×8(3×2 Pattern), 좌하단 원점/상향 y를 유지한다.
3. FullRun은 624×416=259,584셀, 52×52=2,704청크, 156×104=16,224패턴 위치다. 전부 ACTIVE라는 뜻은 아니다.
4. 기존 가변 테스트 크기(가로 12·세로 8 배수)를 보존한다. 전역 크기를 고정 상수로 바꿔 작은 Run을 깨지 않는다.
5. 그래프→BiomePatch+특수 예약/고정 셀→Cluster 큰 형상/경로→활성·비밀 청크와 Type/Port 순서로 계획한다.
6. 이어서 Spine/비울 공간·필요 지지면→Pattern 선택→별도 등반 Overlay→최종 정적 조립 검증/출력을 수행한다.
7. 이 순서는 실제 호출·자료 의존성이다. 임의 패턴을 채운 뒤 Cluster/보호 라벨을 붙이는 방식은 허용하지 않는다.
8. RMAP15의 8 site/8 논리 연결, 2,432 typed 소유 셀, 45 열린 포트 셀, 10 슬롯을 대표 입력에서 대조한다.
9. 셀 수는 고체 개수가 아니며 45는 입구 개수가 아니다. 역할/port ID/상태별 실제 자료를 소비한다.
10. 모든 일반 지형 후보는 EvaluateTerrainCells를 거치는 실제 생성 경로에 연결한다. 테스트에서만 호출하면 부족하다.
11. FixedSolid 제거·ProtectedAir 채우기·포트 폐쇄·슬롯 지지면/여유 침범을 거부하고 최종 조립에서도 원본 대조를 수행한다.
12. 특수 지역과 겹치는 청크는 셀 소유권/mask로 처리한다. 그 청크 전체를 고체로 채우거나 통째로 비우지 않는다.
13. 일반 패턴 후보가 보호 셀과 충돌하면 후보/접근 경로를 제한 재선택한다. 완성 후 fixed 셀 복사로 충돌을 숨기지 않는다.
14. shared SealBoss의 서로 다른 node/local point/조건과 gate 상태를 유지한다. Village는 별도 site로 접근 가능하게 한다.
15. 대표 입력의 SealBoss가 Dough/Crater 경계를 공유한다면 각 역할 점의 원래 후보 제약과 실제 경계 접속을 함께 확인한다.
16. 상위 예약이 실제로 불가능하면 해당 원인만 보고한다. RMAP16에서 site를 몰래 이동하거나 조건을 느슨하게 하지 않는다.

## 6. A02 / 여러 청크를 잇는 큰 형상

- TerrainCluster는 실제 인접한 2~8 MicroChunk에 걸친 연속 지형이다. ID/청크 목록/origin/형상 mask/경로·경계 조건을 갖는다.
- 형상·주요 경로를 먼저 선택하고 그 제약에 맞는 청크/패턴을 채운다. 독립된 완성 12×8 방의 반복 추첨으로 대신하지 않는다.
- 경사·동굴·통로·하프파이프 등 기존 형상 원본을 재사용한다. 대표 출력에서 서로 다른 실제 형상과 청크 경계 연속성을 보여준다.
- 형상은 매 청크 외곽 벽으로 끊기지 않아야 한다. 경계 양쪽의 지지면/빈 공간/Port·이동 의도를 월드 좌표로 공유한다.
- 실제 mask의 인접·면적·구성 청크 수를 검증한다. bounding box에 빈 청크를 끼워 2~8개로 세지 않는다.
- Cluster/특수 지역/비밀 지역의 소유·겹침 정책을 명시한다. 동일 셀을 여러 지형이 마지막 writer 우선으로 바꾸지 않는다.
- 바이옴은 네 종류/실제 16 patch를 그대로 소비한다. 경계를 넘으면 RMAP14의 해당 pair/방향/승인 후보·무장비 조건을 적용한다.
- 모든 인접 patch 면을 열린 이동로로 만들 필요는 없다. 실제 경로가 건너는 면의 승인 지형·Port/이동 연결을 검증한다.
- 작은 반복 형상에 재질만 바꾼 결과를 새 구조로 세지 않는다. 큰 실루엣/청크 구성/연결 차이를 관찰 자료에 남긴다.

## 7. 필수 경로 / 사용자 점프·매달리기 조건

1. RMAP15의 실제 접근 셀부터 다른 site의 접근 셀까지 경로를 배치한다. 중심점 직선이나 논리 간선만 남기지 않는다.
2. 공유 Player profile/Collider 0.4×0.8·발바닥 pivot과 실제 Jump/Grab/낙하 계약을 소비하고 버전/판정 근거를 기록한다.
3. 사용자가 지정한 경사·벽 통과는 장비 없이 1칸 점프, 최대 2칸 점프+안전 모서리 Grab으로 설계한다.
4. 점프 높이를 2칸으로 올리지 않는다. 2칸 초과 상승은 중간 지지면으로 나누고 매 동작의 접근·머리·착지 여유를 확보한다.
5. 2칸이라는 높이만으로 성공하지 않는다. 모서리 노출/유효 Grab 면/매달림 공간/상단 점프 이탈을 실제 주변 셀로 확인한다.
6. 이 경사·벽을 사다리/아이템 필수로 바꿔 해결하지 않는다. 기존 별도 등반 구간은 명시된 역할과 검증 수준을 유지한다.
7. 필수 경로에 곡괭이·지형 파괴·몬스터 발판·미구현 능력을 가정하지 않는다. 인장/보스의 진행 조건은 그대로 유지한다.
8. Type/Port의 열린 모든 셀, IN/OUT/BOTH, 내부 FromPort→ToPort와 상태 조건을 실제 Base/Overlay로 해소한다.
9. 일반 Type1=LR, Type2=LD/RD/LRD, Type3=LU/RU/LRU, Type4=UD/LUD/RUD/LRUD를 기존 계약대로 적용한다.
10. 일방통행을 자동 왕복으로 만들지 않는다. 한 셀 교집합이나 같은 Side/TraversalKind만으로 이동 성공을 선언하지 않는다.
11. 공기 BFS는 연결 보조 진단이다. 지지면·Collider 여유·이동별 조건을 반영한 기존 판정으로 최종 필수 경로를 확인한다.
12. 실제 조립 지형으로 판정한 방향 간선에 상태 조건을 연결하고 세 자원의 정확한 6개 순서→Forge→Seal→Boss→Exit를 확인한다.
13. 낙하/왕복/복귀 경로를 구분한다. 현행 Optional/Required 복귀 정책과 자원 획득 뒤 해제 조건을 기록하고 보존한다.
14. Required shortcut은 좌표·영향 셀·열림 조건·열린 뒤 복귀 지형까지 제공한다. 선 하나/텔레포트 간선으로 대체하지 않는다.
15. SealBoss는 닫힌 3개 SOLID gate와 열린 같은 3개 AIR 상태를 소비한다. 일반 경로가 폐쇄 상태를 우회하지 않게 확인한다.
16. 봉인/자원/보스 이벤트 실행은 아직 계획 조건이다. 상태별 지형 검사를 실제 콘텐츠 동작 완료로 보고하지 않는다.
17. 낮은 천장·어려운 선택 배치는 허용한다. 확인된 필수 단절이나 필수 이동 미확인을 정적 조립 PASS로 숨기지 않는다.
18. 검증 수준은 논리 계획/정적 이동 판정/실제 Player 통과로 구분한다. 전체 Player 주행을 이번 범위의 필수 증거로 요구하지 않는다.

## 8. Pattern / Overlay / 제한 재선택

- 현행 수정 완료 500 pool의 ID/최종 typed 16셀/변환·출처/버전·digest를 실제 composer와 fallback까지 연결한다.
- 사용자 수정 대상은 002,007,008,013,014,016,018,019,036,038,042,044,046,047,049,173,174,182,188,190,197,205,207,219,221이다.
- 번호는 검토 당시 인덱스다. 현재 정렬 인덱스로 재해석하지 말고 완료된 old→new ID 매핑으로 수정 형상의 소비를 확인한다.
- 이미 검증된 25개를 전수 재실행하거나 한 월드에 모두 강제 배치하지 않는다. 구형 pool/첫 48개로 돌아가는 소비 경로는 막는다.
- 일반 조립은 청크당 3×2 슬롯의 선택 후보/원점/16셀/transform을 추적한다. 역할 라벨만으로 실제 셀을 대신하지 않는다.
- 고정 template·비활성 고체 등 별도 소유 영역은 source kind/mask를 명시한다. 그 셀을 pool 후보에서 온 것처럼 보고하지 않는다.
- 부분 특수 청크의 원본 후보와 최종 소유 셀을 구분한다. 후보를 사후 수정한 배열에 기존 Candidate ID를 붙이지 않는다.
- Spine의 비워야 할 공간과 필요한 지지면을 분리해 후보 선택 전에 적용한다. 결과에 맞춰 보호 폭을 줄이지 않는다.
- ContextRequired는 실제 이웃·profile로 해소한다. 등반 Overlay는 Base와 별도이며 ONE_WAY는 월드 위쪽 충돌/Grab 불가를 유지한다.
- Overlay가 고체/보호 공간과 충돌하면 재선택하거나 실패한다. SOLID/O를 AIR로 지워 통로를 만드는 수리는 하지 않는다.
- 후보/슬롯→필요한 이웃 Port/Cluster 범위 순으로 원인을 좁혀 기존 유한 재선택을 사용한다. 상한/시도 수/실패 위치를 기록한다.
- 필수 연결/예약/경계 조건은 유지한다. 무한 seed 변경, 전체 월드 반복 재생성, 자동 터널/침묵 굴착으로 성공시키지 않는다.
- 같은 입력/버전/설정은 같은 셀·선택/실패·안정 ID를 만든다. RMAP12 stream을 재사용하고 재시도가 상위 계획 RNG를 소비하지 않게 한다.

## 9. A21 / 개방형 Type0와 봉인 비밀 지역

1. ACTIVE/SECRET/INACTIVE_SOLID/SPECIAL_RESERVED와 Type을 구분한다. 전부 고체인 비활성 청크를 비밀 지역으로 세지 않는다.
2. 개방형 Type0은 일반 입구 하나인 막다른 공간이다. 3셀 폭 Port도 입구 하나이며 접근/복귀 가능성은 별도 판정한다.
3. 봉인 비밀 지역은 실제 내부 공간을 가진 1~6청크를 초기 범위로 사용한다. 일반 외부 입구는 닫고 BreakableAccess를 명시한다.
4. 여러 청크의 내부 연결과 지역 외부 입구를 별도 기록한다. 내부 통로가 있다는 이유로 외부 봉인이 해제됐다고 분류하지 않는다.
5. 청크 Type/Port와 지역 단위 봉인 상태의 기존 매핑을 설명한다. 모든 내부 청크의 연결을 가짜 0개로 지워 맞추지 않는다.
6. 실제 접근 면/파괴 대상 셀/안정 ID, 파괴 전·후 Base와 충돌·보호 의미, 곡괭이/지형 파괴 전지 접점을 기록한다.
7. 주변 일반 지형을 포함해 파괴 전 우회 일반 진입 부재, 파괴 후 입구 접근과 내부 공간 연결을 확인한다.
8. 같은 파괴 입구로 복귀하거나 안전 출구를 제공한다. 출구가 있으면 폐쇄 상태의 외부 봉인을 깨지 않는 방향/상태 조건을 명시한다.
9. 필수 자원·Forge/Seal/Boss/Exit와 그 유일 경로를 봉인 비밀 공간에 두지 않는다. 선택 보상은 위치/허용 역할 접점까지만 남긴다.
10. 각 비밀 지역에 금·빛·소리·먼지 중 최소 2종의 실제 위치/방향/표현 슬롯을 둔다. 목록에 단어 두 개만 적지 않는다.
11. 단서는 일반 탐험 쪽에서 발견할 배치여야 한다. 정식 시청각 효과/도구 동작이 없으면 미구현으로 명시한다.
12. 파괴 전후 상태 자료는 정적 정의와 후속 mutation 입력으로 분리한다. 런타임 저장/실제 도구 구현을 선행하지 않는다.

## 10. A26/A27 / 설정과 실제 밀도·빈도

- 첫 전체 생성 전에 Type0 빈도(개방/봉인 구분), 활성 비율 목표, biome별 밀도/형상 가중치와 재시도 상한을 설정으로 확정한다.
- 원문에 없는 수치는 조정 가능한 초안 튜닝값과 근거로 적는다. 기존 값을 우선 사용하고 사용자 최종 확정으로 꾸미지 않는다.
- RMAP14의 OPEN 40~55%, BALANCED 55~65%, DENSE 65~75% 목표와 실제 선택 profile/가중치를 소비한다.
- 실제 Base 셀을 Cluster 또는 명시된 주변 MicroChunk 묶음에서 측정한다. 개별 4×4 밀도로 후보 전수를 탈락시키지 않는다.
- 기본 집계는 S/(S+A+O)다. O는 별도 개수이고 S에 합치지 않는다. Overlay/재질/장식은 Base 밀도를 바꾸지 않는다.
- 밀도 scope/mask/셀 수/상태와 고정 특수 셀·비활성 영역 포함 여부를 기록한다. bbox의 외부 셀을 몰래 분모에 넣지 않는다.
- RMAP14 patch 목표와 비교할 실제 patch 집계도 제공한다. 일반 지형만의 집계와 전체 고정 셀 포함 집계를 명확히 구분한다.
- 목표와 측정값·편차를 함께 보고하고 이제 생성된 범위를 PENDING_GEOMETRY로 남기지 않는다. 큰 편차는 형상/설정 원인을 설명한다.
- ACTIVE 수/2704, SECRET·INACTIVE_SOLID·SPECIAL_RESERVED 수, 탐험 가능 공간을 포함한 별도 비율을 실제 기준과 함께 출력한다.
- 부분 특수 청크를 중복 집계하지 않도록 기존 청크 상태 우선순위/부분 mask를 명시한다. RMAP14 planning Active를 복사하지 않는다.
- 개방 Type0 청크 수와 봉인 지역 수/구성 청크 수/외부 입구 수를 따로 센다. 서로 다른 분모의 빈도를 합치지 않는다.
- 기존 RUN06 관찰 접점을 재사용해 최종 typed 구조·Cluster 형상 중복/유사성을 요약한다. 65,536/legacy 전수 재감사는 하지 않는다.
- 초기 밀도는 형태 조정 목표다. 사용자 허용 난도를 일괄 삭제하거나 목표 달성을 위해 보호/필수 경로를 훼손하지 않는다.

## 11. 실제 생성·시각 출력 / RMAP17 입력

1. 선행 대표 seed=1304와 현행 CONTENT/GENERATOR/pool·profile 버전을 우선 사용하고 실제 설정·선행 digest를 기록한다.
2. 공개 API가 모든 259,584 world 좌표에 정확히 하나의 최종 S/A/O Base를 결정한다. 누락·중복·bounds 밖 셀을 거부한다.
3. RMAP15 고정/일반 후보/비활성 고체/비밀 봉인과 상태별 변경 셀의 소유·출처를 추적한다. Overlay/슬롯은 별도다.
4. RMAP17이 사용할 공개 snapshot/API 또는 기존 Bake 입력 adapter에 연결하고 대표 범위 round-trip으로 동일 셀 전달을 확인한다.
5. 전체 Tilemap/Collider는 만들지 않는다. 제한 소비 fixture는 검사한 범위와 물리 미검증 여부를 기록한다.
6. 같은 최종 데이터에서 전체 Base 지형 지도, biome/Cluster/활성·비밀 상태도, 필수 경로·특수 site 표시를 출력한다.
7. 월드 영역은 Tile 기준 3:2 비율을 유지한다. 단색 청크 상자만으로 실제 1×1 지형 그림을 대체하지 않는다.
8. 경사·동굴·통로·하프파이프/특수 접근/biome 경계/봉인 비밀 사례를 셀 크기가 읽히는 확대도로 보여준다.
9. 확대도에는 좌표/Chunk·Cluster 경계/S·A·O/실제 Port·지지면·보호·경로를 구분하고 비밀 공간은 파괴 전후를 비교한다.
10. 기존 Editor/exporter에 생성·조회 진입점을 제공한다. 테스트가 만든 CSV만 있고 다음 소비 경로가 없으면 완료가 아니다.
11. 전체 지도와 확대도를 직접 열어 지형 연속성·특수 접근·봉인을 확인한다. AI 확인을 사용자 시각 승인으로 기록하지 않는다.

## 12. Required Outputs

아래는 GENERATED/RMAP16 제안 이름이다. 기존 exporter를 쓰면 실제 대응 경로를 Result에 기록한다.

| 자료 | 필수 내용 |
|---|---|
| rmap16_manifest.json | seed/버전·pool/profile/선행 digest/사전 설정/공개 생성·소비 API/검증 수준 |
| rmap16_clusters.csv / rmap16_chunks.csv | 실제 형상·mask/2~8 구성/좌표·소유/공간 상태/Type/생성 순서 참조 |
| rmap16_cells.csv / rmap16_patterns.csv / rmap16_overlays.csv | 전체 typed 셀/소유·출처, 후보·변환/슬롯, 별도 Overlay |
| rmap16_ports.csv / rmap16_routes.csv | 전체 열린 셀/방향·내부 연결/실제 경로·이동 판정·조건/6순서 근거 |
| rmap16_secrets.csv / rmap16_state_geometry.csv | 실제 1~6청크 공간/외부·내부 연결/단서 슬롯/파괴·봉인·shortcut 상태 입력 |
| rmap16_density.csv / rmap16_shape_summary.csv | 실제 scope별 S/A/O·목표/측정·편차/활성·Type0 빈도/구조 중복 |
| rmap16_layout.png / review/*.png | 실제 전체 지형/상태·경로 표시와 읽을 수 있는 사례별 셀 확대도 |
| focused XML/검사 기록 | 실제 filter/job/count/생성·소비 증거/실패·국소 재선택/재사용·미실행 구분 |

JSON/CSV/그림은 하나의 최종 계획에서 파생한다. CSV UTF-8/RFC4180/stable 정렬/ID 참조·좌표 round-trip을 확인한다.
사용자가 지형을 이어 검토할 수 있도록 위 생성 자료와 review 이미지를 RMAP16_REVIEW.zip으로 묶어 실제 경로를 보고한다.

## 13. Focused Checks / PASS

- 실제 624×416 정적 조립, 2~8청크 큰 형상·경계 연속성, 1~6청크 비밀 내부 공간·단서·외부 봉인과 파괴 후 복귀.
- 실제 일반 생성 경로에서 RMAP15 보호 gate 사용/최종 고정 셀 보존, 포트·슬롯·지지면 보존과 침범 후보 거부.
- 최종 Base/Overlay의 필수 경로·방향·2칸 Jump+Grab 조건, 상태별 gate/복귀/6개 순서와 미확인·실패의 구분.
- 새 연결에서 벽/머리 공간/착지면을 깨뜨린 사례와 봉인 우회/보호 침범 사례가 명시적 실패로 검출되는지 필요한 범위만 확인.
- 실제 typed pool·후보/최종 셀 출처, 설정→측정 집계, 같은 입력 재현/국소 재시도 상한·원본 불변·stream 격리.
- 공개 RMAP17 입력 소비 fixture의 셀/소유/Overlay·상태 전달 일치, compile/refresh와 전체·확대 이미지/API 일치.
- 실제 Player 통과를 완료 근거로 쓰는 사례만 Production Player/Tilemap/실제 입력으로 확인하고 범위를 명시한다.
- 변경 책임과 직접 영향 검사만 실행한다. 테스트 수 채우기, broad/full/unfiltered regression, 전수 seed/500개 물리, 불필요한 build 금지.

위 정적 지형·필수 이동 판정·보호·비밀·설정/측정·후속 소비가 확인되면 A02/A21/A26/A27/W06 범위 PASS다.
전체 Player 완주/Tilemap·Collider/실제 도구·보스 동작은 포함하지 않는다. 필수 정적 결과를 후속으로 미뤄 PASS하지 않는다.
상태·해시 체인 충돌은 STATUS_CONFLICT, 필수 자료/도구 부재는 BLOCKED, 구현 기준 불충족은 FAIL이다. 설계 적응은 근거를 기록한다.

## 14. Required Result / Commit / STOP

```text
TASK: RMAP16_CLUSTERS
STATUS: PASS / FAIL / BLOCKED / STATUS_CONFLICT
USER-FACING REPORT: 큰 지형/필수 경로/비밀 공간/실측 밀도, 아직 미실행인 전체 Bake·실제 플레이
RESPONSIBILITY / CHANGED: 실제 경로·KEEP/ADAPT/NEW·원본/소비 경계·비소유 책임
PRECONDITIONS: branch/HEAD, inbox 외부/실제 SHA, RMAP15 Task/Archive/Result SHA와 실제 Finalize/commit
A02/A21/A26/A27/W06 EVIDENCE: 실제 셀·순서/Cluster/비밀·상태/설정·실측/공개 API
ROUTE/PROTECTION: site/포트·슬롯/2칸 Jump+Grab/6개 순서/봉인·복귀/검증 수준·실패·국소 재선택
VALIDATION: 실제 Unity version/filter/job/count/XML, 직접 영향·재사용·미확인/실패·수리
UNITY VISIBLE OUTPUT: 생성 진입점/전체 지도·확대도/CSV·snapshot digest/RMAP16_REVIEW.zip 경로
RMAP17 HANDOFF: 전체 Base·Overlay·소유/상태 입력/API·버전/추가 물리 검증 대상
FINAL EVIDENCE / COMMIT: 상태·집계 범위, Task/Archive 동일, 시작 HEAD와 실제 commit 또는 미생성 이유
NEXT: RMAP17_WORLD_BAKE LOCKED / NOT STARTED
```

PASS Result 후 기존 Finalize로 RMAP16 CURRENT→COMPLETE, Current→NONE만 수행한다.
RMAP 부분집합은 적용 후 15 COMPLETE / 1 CURRENT / 3 LOCKED, 완료 후 16 COMPLETE / 0 CURRENT / 3 LOCKED다.
전체 MASTER가 240행이면 대응 완료값은 237 COMPLETE / 0 CURRENT / 3 LOCKED다. 실제 이력을 숫자에 억지로 맞추지 않는다.
Task/Archive/Result/허용 코드·설정·자료·상태 변경만 commit한다. 무관한 staged 변경을 섞거나 임의 unstage하지 않는다.
최종 CLI에 Result/설치 Task SHA, 실제 commit SHA, Current NONE과 RMAP17 LOCKED를 보고한다.
Result가 commit 전 작성됐다면 최종 CLI에 실제 commit 근거를 남긴다. 자기참조 해시 때문에 Result를 다시 쓰지 않는다.
RMAP17 실행과 git push 없이 STOP한다.
