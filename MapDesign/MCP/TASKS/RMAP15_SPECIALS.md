```yaml
mcp_patch:
  format: single_task_v1
  task_id: RMAP15_SPECIALS
  task_file: TASKS/RMAP15_SPECIALS.md
  requires_current_task: NONE
  requires_completed_task: RMAP14_BIOMES
  requires_result:
    path: REPORTS/RMAP14_BIOMES_RESULT.md
    status: PASS
    sha256: 38b22e7d5414aef3eb56a9b97cd7d44af9ea9af0fa488c3273f577e7a84b8c48
  requires_installed_task:
    path: TASKS/RMAP14_BIOMES.md
    sha256: 9d99d0241eeb904ce0cd523c53349e290707cb9eca242e78719a0905ccc42f0b
  sets_current_task: RMAP15_SPECIALS
```

# RMAP15_SPECIALS - 필수 특수 지역 예약과 실제 고정 지형 입력

```text
TASK: RMAP15_SPECIALS
DOCUMENT: v4.2 / RMAP14 PASS 이후 실행 지시서
STATUS: CURRENT (정상 Apply 이후)
INPUT: MapDesign/MCP_INBOX/RMAP15_SPECIALS.md
EXPECTED_RESULT: MapDesign/MCP/REPORTS/RMAP15_SPECIALS_RESULT.md
NEXT: RMAP16_CLUSTERS
NEXT STATUS: LOCKED / DO NOT START
```

이 MD는 300줄 이하의 한 Task 계약이다. 문서 발행만으로 저장소 상태를 바꾸지 않는다.
이번 요구는 W04(원문 §3/§22/§23)다. 일반 TerrainCluster/Pattern보다 먼저 필수 지역 공간과 고정 지형을 정한다.

## 1. User-Facing Goal / 완료 모습

624×416 월드에서 시작·세 자원·주요 마을·인장 제작·봉인지/보스·최종 출구의 8개 실제 위치를 확인한다.
각 지역의 전체 1×1 고정 셀, 진입/출구, 활동 슬롯, 보호 공간과 충돌 소유권을 조회한다.
RMAP14의 PATCH_SET_ONLY 후보 집합을 실제 위치/footprint로 구체화하고 후속 일반 지형이 이를 침범하지 못하게 한다.
예약 ID 8개나 bounding box만 만드는 것으로 완료하지 않는다. 실제 지형 셀과 접근·슬롯 입력까지 완성한다.
전체 월드 Bake는 RMAP17 책임이다. 이번 단계는 그보다 앞선 고정 지형 입력과 제한된 소비 검증을 소유한다.

## 2. Preflight / 선행 완료와 정상 Apply

1. 프로젝트 루트/AGENTS.md, MapDesign/MCP/00_MCP_ENTRYPOINT.md, RMAP 프로토콜과 현지 Apply/Finalize를 읽는다.
2. 외부 전달 SHA와 inbox 전체 원본 bytes를 비교한다. 자기 해시를 본문에 넣거나 planned 원본 SHA와 혼동하지 않는다.
3. 외부 값 부재/불일치 또는 등록 SHA 충돌이면 경로/expected/actual을 보고한다. 정규화·재저장/운영 검사 수정으로 우회하지 않는다.
4. YAML의 RMAP14 PASS Result와 설치 Task/Archive SHA·bytes 동일성을 대조한다.
5. 제출 Result는 Finalize/commit 전 문구다. 실제 RMAP14 COMPLETE, Current NONE, Result 경로 git log/완료 commit을 확인한다.
6. 아직 RMAP14 CURRENT이며 해당 PASS/증거가 유효하면 기존 RMAP14 규정의 Finalize·소유 commit만 먼저 마무리한다.
7. 선행 마무리는 RMAP14 이력으로 분리한다. RMAP15를 먼저 CURRENT로 열거나 선행 Result를 임의 수정하지 않는다.
8. 실제 상태가 제출 근거와 다르면 이력을 확인한다. 설명 없는 선행 기능/해시 불일치를 PASS로 간주하지 않는다.
9. RMAP 부분집합의 시작 기준은 14 COMPLETE / 0 CURRENT / 5 LOCKED다. 이전의 전체 MASTER 집계와 혼동하지 않는다.
10. 전체 MASTER가 여전히 240행이면 대응값은 235 COMPLETE / 0 CURRENT / 5 LOCKED다. 실제 집계 범위를 함께 기록한다.
11. branch/HEAD/git status, 다른 CURRENT 부재, 단일 inbox 후보/등록 ID를 확인한다.
12. §3의 별칭을 실제 경로로 해석하고 KEEP/ADAPT/NEW·직접 호출자·허용 쓰기 경계를 사전 바인딩 기록에 확정한다.
13. 정상 Apply로 RMAP15만 LOCKED→CURRENT, Current NONE→RMAP15_SPECIALS를 수행하고 동일 bytes Task/Archive를 설치한다.
14. 같은 CURRENT 재개는 입력/설치/Archive 동일성과 기존 결과를 확인한다. 이미 COMPLETE+유효 PASS면 보고하고 STOP한다.
15. 무관한 변경은 보존한다. reset/stash/강제 덮어쓰기, 무관한 stage/unstage, git push를 하지 않는다.

## 3. Read Allowlist / 실제 바인딩

- MapDesign/MCP/RMAP/{00_BASELINE_V4_2,01_SEQUENCE_V4_2,02_PROTOCOL_V4_2}.md와 필요한 현지 운영 문서.
- MASTER_IMPLEMENTATION_TASK_LIST/06_IMPLEMENTATION_STATUS 및 RMAP14 Task/Archive/Result, 필요한 RMAP13 실제 출력.
- GENERATED/RMAP01/file_bindings.csv의 SPECIAL_REGION/FOOTPRINT/FIXED_SHELL/SLOT과 필요한 ID/PROFILE/BAKE/EDITOR 접점.
- 바인딩된 기존 SpecialRegion template/footprint/fixed layer/site/slot/protection 자료와 직접 호출자/검사.
- RMAP14 실제 ownership/patch/boundary/reservation inputs와 현재 설정, RMAP13 graph/node/edge/진행 조건.
- RMAP12 정의/안정 ID/SpecialReservation RNG, 최신 공유 Player profile·RMAP11 수정 pool/소비 계약.

| 보고서로 확인된 실제 경로 | 이번 소비·적응 책임 |
|---|---|
| Assets/_Game/Map/Runtime/WorldGeneration/Biomes/RmapWorldBiomePlanner.cs | 실제 16개 patch/ownership/후속 후보 범위 API 소비 |
| Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/RmapWorldGraphPlanner.cs | 8개 논리 node/reservation의 위치·상태 의존성 소비 |
| Assets/_Game/Map/Runtime/WorldGeneration/WorldData/RmapWorldDataContract.cs | immutable 정의/SpecialReservation stream/안정 ID 재사용 |
| Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionFixedSlotLayers.cs | 기존 fixed terrain/slot 의미를 읽고 현재 입력으로 적응 |
| Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionSiteBridge.cs | 실제 site/footprint/접근/후속 소비 연결 재사용 |
| SPECIAL_REGION/FOOTPRINT/FIXED_SHELL/SLOT 실제 바인딩 파일 | 과거 template의 재사용 심사와 필요한 최소 변경 |

경로/API가 다르면 실제 바인딩으로 적응한다. 별칭마다 새 Service를 만들거나 없는 API를 기존 구현으로 단정하지 않는다.
원본/Authoring/파생 Generated의 권위와 재생성 경로를 구분한다. 과거 모든 Scene/Seed/Result를 재감사하지 않는다.

## 4. Write Allowlist / 책임 경계

- 8개 필수 지역의 site 선택/footprint/fixed cell/접근·슬롯·보호·충돌 소유권과 직접 소비 접점.
- 기존 fixed template의 현재 좌표/typed Base/슬롯 변환 adapter 및 부족한 최소 authoring 입력.
- 기존 Editor/exporter의 실제 배치·지역별 셀 표시, 직접 관련 focused tests와 필요한 제한 fixture.
- MapDesign/MCP/GENERATED/RMAP15 자료, 지정 Task/Archive/Result와 현지 상태 기록.
- 새 파일/assembly/.meta는 필요한 최소 범위다. 기존 SpecialRegion 계약을 우선 KEEP/ADAPT한다.

RMAP14 바이옴/승인 경계 원본, RMAP13 진행 의미, RMAP11 형상, Player 수치·공용 prefab은 보존한다.
마을 경제·NPC AI·보스 전투·정식 자원 획득 UI를 만들지 않는다. RMAP16 일반 Cluster/Type0, RMAP17 전체 Bake도 시작하지 않는다.

## 5. W04 / 정확한 8개 물리 지역과 그래프 대응

| 물리 지역 | 수 | 연결할 논리 역할 / 이번 고정 입력 |
|---|---:|---|
| Start | 1 | Start / 안전한 spawn 지지면·출발 접근 |
| 월핵 원석 지역 | 1 | Mooncore Ore / 자원 슬롯·접근·복귀 |
| 응축 계수수액 지역 | 1 | Condensed Coefficient Sap / 자원 슬롯·접근·복귀 |
| 심층 별누룩 지역 | 1 | Deep Star Yeast / 자원 슬롯·접근·복귀 |
| 주요 마을 | 1 | 별도 Village site / 접근 가능한 바닥·NPC·상점 슬롯 |
| 인장 제작 시설 | 1 | Forge / 제작 슬롯·접근·복귀 |
| 보스 봉인지/보스 지역 | 1 | Seal와 Boss 두 node / 봉인 접근·보스 공간·이후 출구 연결 |
| 보스 이후 최종 출구 | 1 | Exit / 조건부 접근과 안전한 exit 슬롯 |
| 합계 | 8 | 실제 site 8개, 모든 필수 역할 포함 |

- RMAP13의 8개 논리 node를 8개 물리 site에 무조건 일대일 매핑하지 않는다.
- Seal와 Boss는 하나의 물리 지역을 공유하되 서로 다른 local 위치/진행 역할/상태 조건을 유지한다.
- Village는 별도 stable site로 추가한다. Seal ID를 마을로 재사용하거나 논리 node를 누락하지 않는다.
- 기존 8개 graph node/reservation 참조는 모두 보존하고 Node→Site→LocalPoint의 대응을 출력한다.
- 마을은 일반 진행 경로에서 접근할 입력을 가지되 자원 6개 획득 순서에 방문을 강제 삽입하지 않는다.
- 후속 콘텐츠가 아직 없더라도 필요한 슬롯/바닥/여유 공간은 실제 위치로 만든다. 실행된 경제/전투로 보고하지 않는다.

## 6. 후보 범위에서 실제 위치 선택

1. 현행 공개 생성 경로에서 RMAP12 정의+RMAP13 그래프+RMAP14 계획을 입력으로 받는다. 테스트 전용 별도 정본을 만들지 않는다.
2. RMAP14는 네 biome 종류/16개 patch(각 169청크)를 보고했다. biome 종류와 patch 개수를 분리해 실제 집합을 소비한다.
3. 기존 ‘네 패치’ 표현과 실제 16개 세분화의 차이는 설정/생성 규칙과 함께 ADAPT 근거로 기록한다. 임의로 4개로 합치지 않는다.
4. RMAP14의 planning Active를 실제 이동 가능/예약 가능의 물리 증명으로 사용하지 않는다.
5. 각 역할의 eligible patch set과 기존 template의 footprint/접근 조건에서 배치 후보를 산출한다.
6. patch union의 bounding box 안에 있다는 이유로 적합하다고 판정하지 않는다. 실제 차지할 모든 좌표가 허용 집합에 있어야 한다.
7. 월드 bounds는 624×416 Tile/52×52 MicroChunk다. local Tile→world Tile→Chunk 변환과 원점을 기존 계약대로 검증한다.
8. RMAP13의 작은 Run anchor는 논리 참조로 유지한다. 그 좌표를 Full Run site 위치로 복사하지 않는다.
9. 허용 biome/경계/방향/footprint 크기/보호 조건을 명시한다. patch 경계를 넘는다면 모든 영향 patch와 승인 경계 조건을 기록한다.
10. Village의 eligible 범위는 기존 승인 제약을 우선 사용한다. 없으면 명시적 초안 설정과 이유를 기록하고 stable ID로 구분한다.
11. 같은 입력에서 stable 후보 정렬과 기존 SpecialReservation stream/scope/attempt로 결정적으로 선택한다.
12. 큰/제약 많은 footprint 우선 등 실제 선택 규칙과 유한 시도 상한을 설정으로 명시한다. 전체 월드 무한 재생성을 하지 않는다.
13. 충돌/빈 후보는 해당 site/근거/좌표를 출력하고 필요한 예약 범위만 제한 재선택한다. 고정 좌표 8개를 무근거로 박지 않는다.
14. 선택 결과는 실제 origin/footprint/template version/transform/patch IDs/접근점까지 포함한다. PATCH_SET_ONLY로 끝내지 않는다.
15. 공간 부족을 숨기려고 1×1 마커로 축소하거나 도구를 써야만 들어갈 곳으로 필수 자원을 옮기지 않는다.

## 7. 실제 fixed shell / 모든 1×1 셀

- 기존 SpecialRegion 자료를 먼저 KEEP/ADAPT/REJECT로 심사하고 template ID/원본/변환/탈락 이유를 남긴다.
- 기존 역할별 유효 template가 없으면 W04 역할을 충족하는 최소 고정 지형을 authoring 설정으로 만든다.
- 크기/형상/방향의 신규값은 초안 설정과 이유를 명시한다. 미확정 치수를 사용자 최종 확정으로 기록하지 않는다.
- template 재사용은 허용한다. 역할별 슬롯·접근·지지면 책임을 충족하지 않는 동일 빈 상자 8개로 완료를 대신하지 않는다.
- 각 footprint 내부의 실제 셀을 SOLID/AIR/ONE_WAY_PLATFORM으로 명시한다. Base와 Marker/Slot/Material/Decoration은 분리한다.
- 비직사각 footprint는 occupied/reserved mask를 명시한다. envelope 내부의 외부 셀과 보호할 AIR를 혼동하지 않는다.
- 모든 소유 셀에 local/world 좌표, Base, site owner, 충돌/보호 의미를 연결한다. 셀을 생략한 윤곽 그림만 내보내지 않는다.
- template가 허용하는 transform만 적용한다. 지형/슬롯/접근/방향/보호 영역을 함께 변환하고 위쪽 지지 의미를 재검증한다.
- ONE_WAY는 월드 위쪽 충돌면이며 Grab 지지면으로 사용하지 않는다. Material 이름을 충돌 종류로 해석하지 않는다.
- Start의 발바닥 위치에는 실제 지지면과 Collider 여유가 있어야 한다. spawn을 고체 내부/낙하 공중에 두지 않는다.
- 세 자원과 Forge는 실제 상호작용 위치·지지면·접근/복귀 공간을 갖는다. 슬롯 이름만 있는 공중 점으로 두지 않는다.
- Village의 NPC/상점 슬롯은 기존 슬롯 책임과 점유 크기를 재사용한다. 실제 NPC/경제 동작 없이도 바닥/이동 여유를 제공한다.
- 보스 지역은 봉인 접근 위치, 내부 활동 공간, 보스 슬롯, 이후 출구 접근의 실제 셀/지역 내 관계를 분리한다.
- 최종 Exit의 지지면/접근 조건은 보스 이후 진행 의미와 일치해야 한다. 실제 전투 완료 이벤트를 발생시켰다고 주장하지 않는다.
- shell의 구조적 수정은 template/version/digest에 반영한다. 파생 CSV만 편집해 재생성 때 사라지게 하지 않는다.

## 8. 접근·출구·진행 조건과 이동 여유

1. 각 지역 접근은 Side/열린 셀 전체 목록/TraversalKind/IN·OUT·BOTH/Required/조건/source graph 연결을 기록한다.
2. 입구 중심점 하나로 포트 폭과 좌표를 축약하지 않는다. 실제 열리는 모든 local/world 셀과 소속 청크를 보존한다.
3. 열린 포트 셀과 내부 fixed cell/protection/slot 점유가 충돌하지 않는지 확인한다. 외부 연결 요구도 별도로 출력한다.
4. 공유 Player Collider/profile을 소비한다. 1칸은 점프, 2칸은 점프+안전 모서리 Grab, 더 높은 상승은 중간 지지면으로 나눈다.
5. 2칸 높이 숫자만으로 통과를 인증하지 않는다. 모서리 노출/Grab 표면/접근·머리 여유/상단 착지 조건을 확인한다.
6. 핵심 자원 접근에 이동 장비·지형 파괴·Type0 봉인 해제를 요구하지 않는다. 인장으로 보스 봉인을 여는 진행 조건은 유지한다.
7. 과거 승인 접근이 있으면 재사용한다. 낮은 천장/난도를 모두 무효로 지우지 않되 확인된 필수 단절은 실패로 남긴다.
8. RMAP13의 자원→Forge→Seal→Boss→Exit 조건/방향을 보존한다. 실제 site 매핑 후 6개 순서의 논리 연결을 다시 확인한다.
9. Seal/Boss의 같은 site 공유를 같은 graph 위치/상태로 합쳐 우회하지 않는다. 봉인 전후 접근과 보스 이후 출구 조건을 명시한다.
10. 봉인 개폐에 따라 충돌이 달라지면 고정 shell과 별도의 상태별 collision/protection 입력을 제공한다.
11. 이번 단계에서 문/전투 컨트롤러를 새로 구현하지 않는다. 상태별 지형 입력과 실제 런타임 상태 전환의 미구현 범위를 구분한다.
12. 일반 지형으로 이어질 외부 경로는 RMAP16이 충족할 요구다. 지역 간 직선/열린 셀 BFS를 전체 Player 통과 증명으로 쓰지 않는다.
13. 후속 경로가 없으면 텔레포트 간선/자동 터널로 성공시키지 않는다. 실패 site/포트/조건을 추적 가능하게 남긴다.

## 9. 공간·충돌 소유권과 후속 입력

- biome owner는 RMAP14가 소유한다. region footprint 예약을 이유로 biome 소유권을 임의 변경하지 않는다.
- site별 ReservedCells/FixedSolid/ProtectedAir/필수 접근/콘텐츠 슬롯의 구분과 기존 owner/protection layer를 연결한다.
- 보호할 AIR도 예약 공간이다. 일반 Cluster/Pattern이 채워도 되는 미할당 공간으로 간주하지 않는다.
- 다른 site의 실제 footprint/fixed cell/보호 공간 침범을 검출한다. 외곽선이 닿는 것과 내부 소유 셀 중복을 구분한다.
- shared access가 필요한 경우 기존 공유 정책과 owner/허용 layer를 명시한다. 마지막 writer 승리로 충돌을 숨기지 않는다.
- 향후 일반 지형 입력이 FixedSolid를 제거하거나 ProtectedAir/필수 포트를 막는 구체 사례를 거부하도록 보호 접점을 연결한다.
- RMAP16은 이 예약/고정 지형을 먼저 소비하고 남은 영역을 배치해야 한다. RMAP17의 Bake 입력에도 같은 fixed cell을 포함한다.
- 모델 타입만 만들지 말고 현행 site/slot/고정 셀 소비 경계의 API 또는 최소 adapter에 실제로 연결한다.
- 새 완성형 RMAP16/RMAP17 구현을 만들지 않는다. 소비 준비 여부는 기존 입력 API의 제한된 fixture/round-trip으로 확인한다.
- 뒤에 배치할 콘텐츠가 Base/Collider를 바꾸면 변경 owner/상태/영향 셀·영역을 알려 해당 구간 재검증 입력을 만들 수 있어야 한다.
- 같은 world/template/version/slot identity면 같은 site/slot ID를 재생성한다. 순회 인덱스/Unity instance ID로 대상 ID를 만들지 않는다.
- RMAP12 static 정의와 mutation은 분리한다. 예약 계획/표시가 Player/Camera 인스턴스를 요구하지 않는다.
- 실제 fixed cell의 S/A/O 집계는 보고할 수 있으나, 이를 전체 월드의 RMAP14 목표 밀도 달성으로 확대하지 않는다.

## 10. 실제 생성·시각 출력

1. 기존 대표 1304/현행 CONTENT·GENERATOR·pool/graph/biome 버전을 우선 재사용하고 선택 이유를 기록한다.
2. 같은 공개 API로 실제 site/fixed cell/slot/접근 자료를 생성한다. 테스트가 손으로 다른 데이터를 만들어 통과시키지 않는다.
3. 전체 624×416 바이옴 배치도 위에 8개 footprint/역할/ID/접근 방향을 겹쳐 표시한다. Tile 기준 3:2 비율을 유지한다.
4. 각 지역의 확대도에는 모든 1×1 셀, 예약 mask, S/A/O 범례, entry/exit/기능 슬롯/보호 영역/local·world 원점을 표시한다.
5. 큰 그림에서 읽기 어렵다면 8개 개별 이미지로 내보낸다. 매 셀이 확인 가능한 크기를 확보한다.
6. 봉인지/보스의 상태별 접근/충돌이 있으면 같은 site의 두 상태를 비교할 수 있게 표시한다.
7. 기존 Editor/exporter를 우선 재사용한다. 실제 데이터와 연결된 메뉴/API/출력 경로를 Result에 적는다.
8. 최종 전체 Tilemap Scene은 생성하지 않는다. 제한된 소비 검증용 fixture를 만들면 그 범위/검증 수준을 명시한다.

## 11. Required Outputs

아래는 GENERATED/RMAP15의 제안 파일명이다. 기존 exporter를 재사용하면 대응하는 실제 경로를 보고한다.

| 자료 | 필수 내용 |
|---|---|
| rmap15_manifest.json | Seed/버전/선행 digest/설정/실제 생성·소비 API/검증 수준 |
| rmap15_sites.csv | 8개 site/역할/ID/origin/footprint mask 참조/template·변환/소속 patch |
| rmap15_graph_bindings.csv | 기존 8개 node/reservation→site/local point, Seal+Boss 공유, Village 별도 접근 |
| rmap15_fixed_cells.csv | 소유한 모든 1×1 local/world 셀의 S/A/O/site/충돌·보호 의미 |
| rmap15_access.csv | 포트 열린 셀 전체/방향/TraversalKind/Required/상태/외부 연결 요구 |
| rmap15_slots.csv | spawn/resource/NPC/shop/forge/seal/boss/exit 슬롯의 안정 ID·좌표·점유·지지 |
| rmap15_ownership.csv | site별 예약/고정/보호 AIR/공유 정책과 후속 침범 거부 조건 |
| rmap15_state_geometry.csv | 상태별 충돌 변경 입력/영향 범위. 해당 없으면 그 이유를 명시 |
| rmap15_layout.png / review/*.png | 전체 실제 예약도와 8개 지역의 모든 셀 확대도 |
| focused XML/검사 기록 | 실제 filter/job/count와 fixture 소비/재사용/미실행 구분 |

CSV는 UTF-8/RFC4180/stable 정렬/다중 값 escaping과 round-trip/참조 유효성을 확인한다.
JSON/CSV/이미지는 같은 실제 계획에서 파생한다. Authoring 원본과 Generated를 두 개의 독립 정본으로 만들지 않는다.

## 12. Focused Checks / PASS

- 실제 site 8개/역할 수, source node 8개 보존, Village 별도, Seal/Boss 하나의 물리 지역 안에서 구분.
- 좌표 변환/bounds/실제 patch 집합 포함/footprint 및 보호 공간 충돌, 빈 후보와 제한 재선택 진단.
- 모든 소유 셀의 typed Base/원본·변환/충돌 소유권, 슬롯 지지면/Collider 여유/포트 열린 셀과의 일치.
- 침범하는 일반 지형 입력의 거부와 허용 외부 입력의 통과를 기존 보호 소비 접점에서 확인.
- site 매핑 이후 6개 자원 순서/Forge·봉인·Boss·Exit 조건, 외부 공간 검증의 pending 상태 보존.
- 상태별 봉인 충돌이 있다면 폐쇄/개방 입력과 접근 조건이 일치하는지 확인. 실제 전투/장치 완료와 구분.
- 같은 입력의 site/slot ID·fixed 셀·정렬/digest 재현과 SpecialReservation stream 격리.
- 대표 fixed shell 하나는 기존 Bake 입력 소비 fixture로 정확한 셀/충돌 전달을 확인한다. 전체 월드 Bake를 대신하지 않는다.
- 실제 점프/Grab 통과를 완료 근거로 사용할 경우 그 지역만 실제 Production Player/Tilemap/입력으로 확인하고 증거를 남긴다.
- 데이터상 여유/계획 경로 검사만 수행한 지역을 실제 플레이 PASS라고 보고하지 않는다.
- compile/refresh, 실제 생성 및 전체/지역별 시각 출력, CSV/API/그림 일치를 확인한다.
- 변경된 책임과 직접 영향 검사만 수행한다. broad/full/unfiltered regression, 전수 Seed/500개 물리, 불필요한 build 금지.

W04의 실제 8개 예약/고정 셀/접근·슬롯/충돌·보호 소유권/후속 소비 입력이 확인되면 이번 범위 PASS다.
상태·해시 충돌은 STATUS_CONFLICT, 필수 자료/도구 부재는 BLOCKED, 구현 기준 불충족은 FAIL이다.
전체 외부 경로·Tilemap/Collider·Player 통과나 실제 보스/경제 완료는 이 PASS에 포함하지 않는다.

## 13. Required Result / Commit / STOP

```text
TASK: RMAP15_SPECIALS
STATUS: PASS / FAIL / BLOCKED / STATUS_CONFLICT
USER-FACING REPORT: 8개 실제 지역 위치/고정 지형/접근·슬롯, 아직 미구현인 일반 지형·전체 Bake·콘텐츠
RESPONSIBILITY: 실제 경로 | 추가/수정 | KEEP/ADAPT/NEW 근거 | 소유/비소유 책임
PRECONDITIONS: branch/HEAD, inbox 외부/실제 SHA, 선행 Task/Archive/Result SHA, RMAP14 실제 Finalize/commit
CHANGED: 실제 원본/설정/template/선정 규칙/후속 소비 API와 직접 호출자
W04 EVIDENCE: 8개 site/역할·graph 대응/실제 footprint·fixed 셀/접근·슬롯·상태·보호 소유권
PLACEMENT: 실제 16 patch 소비/후보와 충돌/시도 상한·결정성/설계 ADAPT·초안값
VALIDATION: 실제 Unity version/filter/job/count/XML, 대표 소비 fixture/직접 영향/재사용/실패·수리/미실행
UNITY VISIBLE OUTPUT: 실제 생성·표시 경로, 전체 예약도/모든 지역 1×1 셀 확대도/API·CSV digest
RMAP16/17 HANDOFF: 실제 예약·보호·fixed cell·접근 API/버전과 후속 충족·재검증 조건
FINAL EVIDENCE: 실제 상태와 집계 범위, Task/Archive bytes 동일, 필수 미확인 유무
COMMIT: 시작 HEAD, 실제 commit SHA 또는 아직 미생성인 단계/이유
NEXT: RMAP16_CLUSTERS LOCKED / NOT STARTED
```

PASS Result 후 기존 Finalize로 RMAP15 CURRENT→COMPLETE, Current→NONE만 수행한다.
RMAP 부분집합 예상 적용 후 14 COMPLETE / 1 CURRENT / 4 LOCKED, 완료 후 15 COMPLETE / 0 CURRENT / 4 LOCKED.
전체 MASTER가 240행일 때 대응 완료값은 236 COMPLETE / 0 CURRENT / 4 LOCKED다. 실제 이력을 숫자에 억지로 맞추지 않는다.
Task/Archive/Result/허용 코드·설정·자료·상태 변경만 commit한다. 무관한 staged 변경을 섞거나 임의 unstage하지 않는다.
최종 CLI에 Result/설치 Task SHA, 실제 commit SHA, Current와 RMAP16 LOCKED를 보고한다.
Result가 commit 전 작성됐다면 실제 commit 근거를 최종 CLI로 남긴다. 자기참조 해시 때문에 문서를 다시 쓰지 않는다.
RMAP16 실행과 git push 없이 STOP한다.
