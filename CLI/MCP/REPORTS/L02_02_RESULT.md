# L02_02_RESULT

## TASK

CLI/MCP/TASKS/L02_02.md (L02_02_MAP_ADAPTER — LIVE02_02_PROJECT_GENERATED_MAP_OUTPUT_TO_CHARACTER_RUN_SNAPSHOT)

## STATUS

STATUS: PASS

## SUMMARY

생성 MAP 출력을 캐릭터 생성 런 계약으로 투영하는 라이브 어댑터를 `Assets/_Game/Live/Runtime/Adapters/Map/`에 구현했다(9파일). MAP 런타임에 캐릭터 소비용 파사드가 없으므로(레지스트리·CHAR06_01 확립) 허용 조항대로 공용 도메인 값만 담는 좁은 입력 계약을 정의하고, 배치 마이크로청크(정의+변환)→방/마이크로청크/셀 상태, 시작 셀→시작 스냅샷, 선언 루트/아이템/표식→런 스냅샷으로 결정적 투영한다. 준비 소스·루트 소스는 L02_01 소비자와 호환(ICharacterRoomReadinessSource/DeclaredEdges 동일 표면), 월드 질의는 ICharacterMapWorldQuery 호환이며 미생성 셀은 false(생성-빈 셀과 구분). 검증은 CHAR06_02 정책 위임. 인메모리 에디터 스모크 6종 전부 실측 통과(투영/결정성/준비/루트/질의/불량 진단). MAP·캐릭터 런타임·씬·프리팹 무변경, 컴파일 0 에러, 177/177 유지.

## READ

- CLI/MCP/ENTRY.md~MASTER.md, LIVE_SRC/LIVE_LOCK, REPORTS/L00_02·L01_03·L02_01 RESULT
- CharacterDesign REPORTS: CHAR06_01~04, CHAR00_SOURCE_REGISTRY(파사드 부재 기록)
- MAP 런타임 정독: MicrochunkDefinition(19-인자 ctor·검증 규칙), MicrochunkTileCell(코드 8종, 비어있을 수 없음), **MicrochunkTileLayerOccupancy(점유 판정 권위 — "NONE"=부재)**, MicrochunkTransformer/TransformResult, MicrochunkTileLayer 열거 순서, MicrochunkLocalCoord, WorldCoordinateUtility.ToWorld/IsValid, WorldGenConstants
- Character 런타임: Integration(루트 엣지/정책), GeneratedRunValidation(스냅샷 계열+검증 정책), MapIntegration(CharacterMapCellState.FromTileLayer/Combine, RoomId, ICharacterMapWorldQuery), RunState
- Live 런타임: Rooms(소비자 표면 호환 확인), Run

## CHANGED

- 없음 (기존 파일 수정 0건)

## CREATED

`Assets/_Game/Live/Runtime/Adapters/Map/` (namespace `StarNight.Character.Live.Adapters`, 신규 9파일):

- CharacterLivePlacedMicrochunk.cs — 배치 1건 {Sector, Chunk, MicrochunkDefinition, MicrochunkTransform} (MAP 공용 계약 운반)
- CharacterLiveGeneratedMapAdapterInput.cs — 좁은 입력 계약 {RunId, Seed, 시작 셀, 배치 목록, 선언 루트(캐릭터 계약 재사용), 아이템/표식/금지 셀} — 생성 로직 대체물 아님
- CharacterLiveGeneratedMapDiagnosticKind.cs — 어댑터 계층 진단 7종(입력 형태 결함 전용 — 방/루트/아이템 검증 진단은 캐릭터 정책 소유, 중복 없음)
- CharacterLiveGeneratedMapDiagnostic.cs — {Kind, Subject}
- CharacterLiveGeneratedReadinessSource.cs — 투영 성공 방만 준비 보고(불변, ICharacterRoomReadinessSource)
- CharacterLiveGeneratedRouteSource.cs — DeclaredEdges+Readiness — L02_01 소비자와 동일 표면
- CharacterLiveMapWorldQueryAdapter.cs — 생성 셀 사전 기반 ICharacterMapWorldQuery(미생성=false, 생성-빈=Empty로 true — 구분 유지)
- CharacterLiveGeneratedMapProjection.cs — 결과 {Snapshot, ValidationResult, 소스 3종, AdapterDiagnostics, IsUsable}
- CharacterLiveGeneratedMapAdapter.cs — 투영기: 청크별 검증(정의 null/TileDataComplete/12×8 규격/월드 경계/중복 배치 → 진단+스킵) → ToWorld 좌표 → MicrochunkTransformer 변환 적용 → 점유 레이어→FromTileLayer.Combine 셀 상태 → 방/마이크로청크/준비 등록; 시작 셀은 배치된 방 안일 때만 성립(아니면 StartRoomNotPlaced + 시작 없음); CHAR06_02 검증 정책 호출

## TESTS

- Character EditMode 기준선: **177/177 PASS** (2.27s) — 감소 없음
- MAP EditMode: 13,536 앵커 유지(MAP 무접촉)
- 신규 테스트 파일 없음(Tests/** 금지; 인메모리 에디터 스모크로 검증 — 아래 실측, PlayMode 파일 금지 준수)

## BUILD

- 본 과제 빌드 검증 없음(비요구) — 컴파일 클린(error CS 0)

## LIVE_CONTRACTS_USED

- MAP 공용: MicrochunkDefinition/TileCell/**TileLayerOccupancy(점유 권위)**/Transformer/TileLayer/LocalCoord, WorldCoordinateUtility.ToWorld·IsValid, WorldGenConstants, SectorCoord/MicroChunkCoord/WorldTileCoord — 좌표·레이어 수학 복제 0
- Character: CharacterGeneratedRunSnapshot 계열(Room/Microchunk/Item/Start), CharacterGeneratedRouteEdgeSnapshot, CharacterGeneratedRunValidationPolicy(검증 위임), CharacterMapCellState.FromTileLayer/Combine/Empty, CharacterRoomId.FromWorldTile, ICharacterMapWorldQuery/ICharacterRoomReadinessSource, CharacterRunInventoryState

## REQUESTS_CONSUMED

None. Generated MAP adapter produces Character snapshots/readiness/routes/world query only.
No spawn, route/camera, carry, drop, throw, bomb, rope, damage, death, run failure, HUD, or presentation requests consumed.

## ASSETS_WIRED

- Generated MAP public output -> CharacterGeneratedRunSnapshot adapter
- Generated MAP projected rooms -> generated readiness source
- Generated MAP projected routes -> generated route source
- Generated MAP projected tiles/cells -> live map world query (배치 청크의 96셀 전수 — 소스 데이터 존재)
- No scene or prefab wiring

## MANUAL_VERIFICATION

인메모리 에디터 스모크(임시 샘플 — 12×8 정의 2개를 공용 ctor로 조립, 바닥 1줄 G1/나머지 NONE, 청크 (0,0)/(1,0) 배치, A→B 루트 1, 시작 (5,1)):

- [투영] rooms=2, microchunks=2, routes=1, 어댑터 진단 0
- [검증] CHAR06_02 정책 Passed=True, spawn 1 + 루트 요청 1, digest `1ca57b1a-d0-s1-r1`
- [결정성] 같은 입력 2회 투영 → digest 완전 동일, IsUsable=True
- [준비] 배치 방 A ready=True 등록, 미생성 방(셀 24,0) 미등록(False → 게이트 BlockedMissingRoom 경로)
- [루트] RouteSource.DeclaredEdges + Readiness를 CharacterRouteIntegrationPolicy에 직접 공급 → A→B 전환 요청 생성(route 1) — 수동 소스 없이 L02_01 소비 가능 입증
- [질의] 바닥 셀 (5,0)=true·IsSolid, 생성-빈 셀 (5,3)=true·IsEmpty, **미생성 셀 (30,3)=false**(통과 공간 취급 안 함), 생성 셀 수 192(2×96 전수)
- [불량] null 정의 + 미배치 시작 → MissingDefinition + StartRoomNotPlaced 진단 2건, IsUsable=False, 검증도 Fail — 예외 없이 진단으로 흡수

## REGRESSION_BASELINE

- Character EditMode 177/177 PASS (본 실행 재확인), MAP 13,536 앵커 유지, 컴파일 0 에러

## SCOPE_VALIDATION

- 생성: Adapters/Map 9파일(+.meta) — 허용 경로 `Assets/_Game/Live/Runtime/Adapters/**` 내
- 변경 0건: MAP 런타임/캐릭터 런타임/씬/프리팹/입력 자산/Tests/Packages/ProjectSettings/MapDesign/CharacterDesign/Builds/Temp 전부 무접촉 (git status 확인)
- 씬 배선 없음(과제 명시 금지 — L02_03 소관), 후속 과제 미개방

## FORBIDDEN_AUDIT

- MAP 파사드 신설/생성기 재작성/MAP 데이터·Tilemap 편집/생성 로직 복제 없음 — 읽기 전용 공용 계약만
- MAP 테스트 어셈블리/테스트 픽스처/에디터 전용 API/씬 조회 참조 없음(런타임 코드는 Game.Map.Runtime 공용 타입만)
- 좌표 수학 복제 없음(ToWorld/IsValid/상수 위임), 레이어 의미 복제 없음(TileLayerOccupancy+FromTileLayer 위임), 검증 중복 없음(CHAR06_02 위임)
- 미지 셀을 빈 공간으로 취급하지 않음(실측), 신규 ActionId/금지 기능 없음

## NEXT

Current Task after finalize: NONE
Next Task auto-opened: NO (`L02_03_ROOM_AUDIT`은 LOCKED 유지, 새 INBOX 패키지로만 개방 — 어댑터 3소스(준비/루트/질의)가 수동 소스 교체 준비 완료)
