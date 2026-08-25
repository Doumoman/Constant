# L03_01_RESULT

## TASK

CLI/MCP/TASKS/L03_01.md (L03_01_TOOLS — LIVE03_01_IMPLEMENT_CARRY_DROP_THROW_BOMB_ROPE_CONSUMERS)

## STATUS

STATUS: PASS

## SUMMARY

휴대(들기/안전 내려놓기/방향 투척)·폭탄·로프의 라이브 소비자 계층을 `Assets/_Game/Live/Runtime/Tools/`에 구현했다(14파일, 순수 클래스 — MonoBehaviour/씬/프리팹/입력 자산 배선 없음). 판정·소모·산출은 전부 기존 캐릭터 계약에 위임한다: 휴대는 CharacterCarryInteraction(단일 슬롯 소유권)+CharacterCarryCandidateQuery(결정적 선택), 폭탄은 CharacterBombPlacementPolicy→ApplyBombSpend(인벤토리 유일 경로)→CharacterBombFuse(정확히 1회 폭발)→CharacterExplosionTerrainPolicy(파괴 가능 셀 한정), 로프는 CharacterRopePlacementPolicy→ApplyRopeSpend→CharacterRopeSegmentPolicy(경계·고체·최대 6셀). 지형/로프 실적용 sink가 없으므로 과제 조항대로 좁은 인터페이스+인메모리 FIFO 큐를 신설했다. 요청 대장(채널×요청 id)이 수락 요청만 기록해 정확히 1회 소비를 보장하고, 거부·중복은 어떤 라이브 상태도 변조하지 않는다(재시도 가능). 인메모리 스모크 전 채널 ALL PASS 실측, 컴파일 0 에러, 177/177 유지. 스모크 임시 파일은 finalize 전 제거 완료.

## READ

- CLI/MCP/ENTRY.md~MASTER.md, INPUTS/LIVE_SRC·LIVE_LOCK, REPORTS/L00_02·L01_01·L01_03·L02_03 RESULT
- CharacterDesign REPORTS: CHAR05_01~05, CHAR06_04 — **파일명 편차 2건**(과제 표기 CHAR05_01_..._TERRAIN_REQUESTS/CHAR05_02_..._TRAVERSAL_REQUESTS → 실제 CHAR05_01_IMPLEMENT_BOMB_AND_TERRAIN_MUTATION_REQUEST / CHAR05_02_IMPLEMENT_ROPE_CLIMBING_AND_TRAVERSAL_SUPPORT; 로컬 실제 파일 정독)
- Character 런타임 정독(로컬 시그니처 권위): **Interaction/**(과제의 `Actions/**`는 부재 — 휴대 계약 실위치, 허용 조항대로 적응): CharacterCarryInteraction(TryPickUp/TryCreateSafeDrop/TryCreateThrow — 슬롯 소유·거부 시 유지), CharacterCarryCandidate(1×1 적격)/Kind/Query(우선순위→거리→id 결정적), Placement/ThrowRequest, ThrowDirection(Resolver), ICharacterPlacementSpaceQuery; Equipment/: Bomb·Rope Placement Input/Request/SpendRequest/Policy(재고→유효 순), BombFuse(주입 시간·1회 폭발), ExplosionRequest, ExplosionTerrainPolicy(파괴 가능 한정·결정적 순서), Bomb/RopeSettings; Traversal/: RopeSegmentRequest/Policy(경계·고체·최대 길이); RunState/: RunInventoryState/Policy(ApplyBombSpend/ApplyRopeSpend — actor 일치·0 하한)/ApplyResult(Changed)/RunState(WithInventory); MapIntegration/: CoordinateBridge(TryGetTileCoordinate)/CellState/ICharacterMapWorldQuery
- Live 런타임: Input/Run(세션 UpdateRunState — L03 소비용 표면 명시)/Movement/Rooms/Adapters/Map(월드 질의 어댑터 재사용), Packages/manifest.json

## CHANGED

- 없음 (기존 파일 수정 0건)

## CREATED

`Assets/_Game/Live/Runtime/Tools/` (namespace `StarNight.Character.Live.Tools`, 신규 14파일 + .meta):

- CharacterLiveToolDiagnosticKind.cs — 진단 15종(None/DuplicateRequest + 휴대 6종 + 폭탄 3종 + 로프 4종)
- CharacterLiveToolUseResult.cs — {Accepted, Diagnostic} 값 객체
- CharacterLiveToolChannel.cs — 대장 채널 5종(Carry/Drop/Throw/Bomb/Rope)
- CharacterLiveToolRequestLedger.cs — 채널×요청 id 소비 대장: **수락만 기록**(중복=기록된 id 재시도 거부, 거부는 미기록 → 같은 id 재시도 가능), 정적 상태 없음
- ICharacterLiveCarryTarget.cs — 라이브 휴대 대상 계약(수락 경로에서만 AttachTo/ReleaseAt)
- CharacterLiveCarryConsumer.cs — 들기/내려놓기/투척 소비자(슬롯 공유로 1파일 응집 — 권장 3파일을 축소, 과제 허용 조항): 활성·범위(ctor 주입 pickupRangeCells)·미휴대 대상만 후보화 → 캐릭터 질의로 정확히 1개 선택 → 소유권은 CharacterCarryInteraction(1운반자=1대상, 대상측 IsCarried로 1대상=1운반자); drop은 배치 요청 지점+속도 0, throw는 **캐릭터 요청의 DirectionVector×Speed 그대로**; 진단 6종 노출
- ICharacterLiveTerrainCommandSink.cs / CharacterLiveTerrainCommand.cs / CharacterLiveTerrainCommandQueue.cs — 지형 명령 계약+값 객체{폭발 요청, 변경 요청 목록}+인메모리 FIFO(실적용 소비자 부재로 신설 — Tilemap/MAP 무접촉)
- ICharacterLiveRopeCommandSink.cs / CharacterLiveRopeCommand.cs / CharacterLiveRopeCommandQueue.cs — 로프 명령 계약+값 객체{설치 요청, 세그먼트 목록}+인메모리 FIFO(프리팹/씬 무접촉)
- CharacterLiveBombConsumer.cs — 설치 소비(sink 부재/재고/무효 셀/중복 → 무변조 거부) → ApplyBombSpend 1회 → 세션 WithInventory 반영 → 퓨즈 점화; TickFuses(주입 시간)가 만료 시 폭발 1회→파괴 가능 셀 한정 변경 요청→sink enqueue; 미생성 셀=설치 불가(어댑터 의미 일치)
- CharacterLiveRopeConsumer.cs — 설치 소비(sink 부재/재고/범위 밖·미생성=Invalid/고체=Blocked/중복 → 무변조 거부) → ApplyRopeSpend 1회 → 세그먼트 생성(캐릭터 정책) → sink enqueue

## TESTS

- Character EditMode 기준선: **177/177 PASS** (6.59s, failed 0/skipped 0) — 감소 없음 (임시 스모크 제거 후 최종 상태에서 실행)
- MAP EditMode: 13,536 앵커 유지(MAP 무접촉)
- 신규 테스트 파일 없음(Tests/** 금지; PlayMode 파일 금지 준수 — 인메모리 스모크는 임시 파일로 수행 후 제거)

## BUILD

- 본 과제 빌드 없음(비요구) — 컴파일 클린(임시 파일 제거 후 재확인 error CS 0)

## LIVE_CONTRACTS_USED

- 휴대: CharacterCarryInteraction/Settings.Default(투척 7 u/s·유예 0.25s 중앙값)/Candidate(1×1 적격 소유)/CandidateQuery/PlacementRequest/ThrowRequest/ThrowDirection/ICharacterPlacementSpaceQuery
- 폭탄: CharacterBombPlacementInput/Policy/PlacementRequest/SpendRequest, CharacterBombFuse/BombSettings.Default(퓨즈 2.5s·반경 1.5셀·피해 2), CharacterExplosionRequest, CharacterExplosionTerrainPolicy, CharacterTerrainMutationRequest
- 로프: CharacterRopePlacementInput/Policy/PlacementRequest/SpendRequest, CharacterRopeSettings.Default(최대 6셀), CharacterRopeSegmentRequest/Policy
- 런 상태: CharacterRunInventoryState/Policy(ApplyBombSpend/ApplyRopeSpend — 소모 유일 경로)/ApplyResult, CharacterRunState.WithInventory, CharacterLiveRunSession.RunState/UpdateRunState(L01_03 표면)
- 좌표/맵: CharacterMapCoordinateBridge.TryGetTileCoordinate(월드→셀 — 수학 복제 없음), CharacterMapCellState(IsSolid), ICharacterMapWorldQuery(미생성=false 의미 유지)

## REQUESTS_CONSUMED

- CharacterCarryPlacementRequest (safe drop — 요청 지점·소유자 충돌 유예 그대로 적용)
- CharacterCarryThrowRequest (방향 투척 — DirectionVector×Speed 초기 속도 그대로 적용)
- CharacterBombPlacementRequest + CharacterBombSpendRequest (설치+소모 쌍 — 소모는 인벤토리 정책 위임)
- CharacterExplosionRequest → CharacterTerrainMutationRequest 목록 (퓨즈 만료 1회 → 지형 명령 sink로 운반)
- CharacterRopePlacementRequest + CharacterRopeSpendRequest (설치+소모 쌍) → CharacterRopeSegmentRequest 목록 (로프 명령 sink로 운반)

route/camera·spawn·damage·death·run failure·HUD·presentation·save·audio·animation·scene 요청 소비 없음.

## ASSETS_WIRED

None. Runtime consumers only; no scene, prefab, HUD, audio, animation, save, or input asset wiring.

## MANUAL_VERIFICATION

인메모리 에디터 스모크(임시 파일 `CharacterLiveToolSmokeTemp.cs` — 실행 후 제거, 아래 전부 실측 `SMOKE: ALL PASS`):

- 공통 픽스처: 12×8 생성 월드(바닥 GroundSolid + 파괴 가능 (5,2)/(6,1) + 생성-빈 셀), L02_02 월드 질의 어댑터 재사용, 스폰 정책→세션 시작(인벤토리 4/4), 공유 대장
- [휴대-들기] 후보 4종(적격 A/2×1 과대 B/이미 휴대됨 C/범위 밖 D) 혼합 → **A만 정확히 1개** 선택·부착(carrier id 기록); 같은 id 재시도 → DuplicateRequest(부착 1회 유지); 휴대 중 새 id → AlreadyCarrying
- [휴대-거부 진단] C만 → TargetAlreadyCarried, B만 → InvalidCarryTarget(과대), D만 → NoCarryTarget(범위 밖) — 전부 무변조
- [투척] Right 투척 → 해제 1회, 초기 속도 **(7,0) = 캐릭터 요청 DirectionVector×Speed**, 유예 0.25s; 같은 id 재시도 → 해제 1회 유지(두 번 던져지지 않음); 빈 슬롯 투척 → NoCarriedTarget
- [내려놓기] 자유 공간 → 배치 요청 지점(발밑+오프셋)·속도 0으로 해제 1회; 중복 → DuplicateRequest; 빈 슬롯 → NoCarriedTarget; 막힌 공간 → BlockedDrop + **슬롯·대상·해제 수 전부 유지**
- [폭탄] 유효 설치 → 소모 4→3 정확히 1회+퓨즈 1(큐 0); 중복 → 무소모; 미생성 셀/고체 셀 → InvalidBombPlacement 무소모; 거부된 요청 id는 이후 유효 설치에 재사용 가능(수락만 대장 기록) 실측; TickFuses 1.0s → 폭발 0, +1.5s → **폭발 정확히 1회**·지형 명령 1건·**변경 요청 2건(파괴 가능 (5,2)/(6,1)만, 비파괴 바닥 제외)**·반경 1.5, 추가 tick → 0; 재고 소진(4회) 후 → NoBombStock(퓨즈·큐 불변); sink null 소비자 → MissingTerrainSink 무소모
- [로프] 유효 설치 (8,1) → 소모 4→3 정확히 1회+명령 1건+**세그먼트 6(중앙 최대 길이)**; 중복 → 무소모·큐 불변; 고체 앵커 → BlockedRopeAnchor; 미생성 앵커 → InvalidRopeAnchor; (5,1) 설치 → **세그먼트 1(위 (5,2) 파괴 가능=고체에서 중단)**; 재고 소진 후 → NoRopeStock(큐 불변); sink null → MissingRopeSink; 큐 payload 검증(placement+세그먼트 (8,1)부터)
- 최종 실측: carry accepted=5/rejected=10, bomb accepted=4/rejected=4/explosions=1, rope accepted=4/rejected=4, 인벤토리 bombs=0/ropes=0(정확 소모)

## REGRESSION_BASELINE

- Character EditMode 177/177 PASS(본 실행), MAP 13,536 앵커 유지, 컴파일 0 에러(임시 파일 제거 후 최종 확인)

## SCOPE_VALIDATION

- 생성: Tools/ 14파일(+.meta) — 허용 경로 `Assets/_Game/Live/Runtime/Tools/**` 내
- 변경 0건: Character/MAP 런타임, Live Input/Prefabs, Scenes, Tests, Packages, ProjectSettings, MapDesign, CharacterDesign, Builds, Temp 전부 무접촉(git status 실측 — Tools/ + 파이프라인 파일뿐)
- 임시 스모크 파일 제거 완료(최종 트리에 부재), 후속 과제 미개방

## FORBIDDEN_AUDIT

- 신규 CharacterActionId 없음(잠금 5종 유지), 신규 입력 바인딩 없음, 대체 게임플레이 규칙 없음(적격·선택·소모·폭발·세그먼트 전부 캐릭터 계약 위임)
- basic attack/melee/shoot/dash/wall jump/double jump 없음
- Tilemap/MAP 런타임 데이터/생성 MAP 데이터/씬 자산 직접 변조 없음 — 지형·로프는 명령 값 객체로 sink 큐에만 적재
- 정적 전역 상태 없음(전 소비자 인스턴스 기반 — 미래 씬 컴포넌트에서 주입 호출 가능), 인메모리 더블로 검증 가능함을 스모크로 실증
- 테스트 어셈블리/에디터 전용 API/씬 조회/UI/오디오/세이브/애니메이션 참조 없음, HUD·피해·사망·런 실패 소비 없음(L03_02·이후 소관)

## NEXT

Current Task after finalize: NONE
Next Task auto-opened: NO (`L03_02_HUD`는 LOCKED 유지, 새 INBOX 패키지로만 개방 — 도구 소비자 5채널이 씬 배선 대기 상태)
