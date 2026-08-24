# CHAR06_01_INTEGRATE_PLAYER_WITH_GENERATED_MAP_AND_ROUTES_RESULT

## TASK

TASKS/CHAR06_01_INTEGRATE_PLAYER_WITH_GENERATED_MAP_AND_ROUTES.md

## STATUS

STATUS: PASS

## SUMMARY

생성 맵 시작 스냅샷 → 플레이어 스폰 요청, 선언 루트 엣지 → 루트 전환 요청(CHAR03 경계 게이트 재사용), 잠금 역량 기반 루트 역량 판정(기본 이동/폭탄/로프 지원, 잠금 밖 요구 항상 거부), 결정적 통합 요청 배치(스폰 선두·입력 순서·중복 제거·예외 대신 진단)를 순수 값 객체 + 정적 정책으로 구현했다. GameObject/씬/카메라/MAP/Tilemap 어떤 것도 건드리지 않는다. 신규 12개 테스트 포함 Game.Character.Tests.EditMode 170/170 PASS, 컴파일 에러 0건.

## READ

- CharacterDesign/MCP/00_MCP_ENTRYPOINT.md ~ 05_CHANGE_CONTROL_RULES.md, 06_IMPLEMENTATION_STATUS.md, MASTER_IMPLEMENTATION_TASK_LIST.md, INPUTS/CHAR00_SOURCE_REGISTRY.md
- CharacterDesign/MCP/TASKS/CHAR05_05 + REPORTS: CHAR05_05, CHAR03_03, CHAR04_04
- CharacterDesign/01_FIXED_SPEC/01·02·05·06·07, 03_DATA_SCHEMA/ACTION·INVENTORY
- Assets/_Game/Character/Runtime/ 현행 전체 — 특히 재사용 계약: CharacterRoomId.FromWorldTile, CharacterRoomBoundaryGate.Evaluate(NotABoundaryCrossing/Allowed/BlockedMissingRoom/BlockedUnpreparedRoom), ICharacterRoomReadinessSource, CharacterMapCoordinateBridge.GetCellCenter, CharacterRunInventoryState
- MAP 공용 계약(소스 레지스트리): WorldTileCoord(원시 생성자, 검증은 WorldCoordinateUtility.IsValid), SectorCoord/MicroChunkCoord, WorldGenConstants(월드 624×416, Sector 48×32, MicroChunk 12×8) — 레지스트리가 "Room boundary gate 부재(레거시 RoomBounds2D 선례뿐)"로 기록하므로 캐릭터측 스냅샷 DTO 방식 채택(CHAR05_01 확립 선례)
- 레거시 읽기 전용: RunState.cs(시작 데이터 선례), RoomBounds2D 선례 기록

Entry Gate 검증 (Phase A에서 수행):

- Current Task = TASKS/CHAR06_01_INTEGRATE_PLAYER_WITH_GENERATED_MAP_AND_ROUTES.md 확인
- CHAR05_05 REPORT: STATUS: PASS + `CHAR05_EXIT_DECISION: APPROVED` + sha256 `cb7f4d136e6ff09183065754f4a22a1da4deab1311c80c7e205489e7cb0b17a6` 일치 + required_text 확인
- CHAR05_05 Task sha256 `b740d769...` 일치, Source Registry marker + sha256 `be6cadc4...` 일치
- CHAR06_02 이후 전부 LOCKED 확인

## CHANGED

- 없음 (기존 파일 수정 0건 — MapIntegration 허용 경로도 변경 불필요: CHAR03 게이트를 그대로 재사용; asmdef 변경 없이 기존 어셈블리가 신규 Integration 폴더 자동 포함을 컴파일로 확인)

## CREATED

Runtime — `Assets/_Game/Character/Runtime/Integration/` (namespace `StarNight.Character.Integration`, 신규 폴더, 12파일):

1. CharacterIntegrationDiagnosticKind.cs — 진단 10종(시작 4 + 루트 3 + 역량 3)
2. CharacterIntegrationDiagnostic.cs — {Kind, Subject("route:3"/"cell:x,y" 형식)} 데이터 전용
3. CharacterGeneratedMapStartSnapshot.cs — {MapRunId, StartRoomId, HasStartCell, StartCell, RoomMinCell/RoomMaxCell(포함 경계)}
4. CharacterPlayerSpawnRequest.cs — {ActorId, StartCell, WorldCenter, StartRoomId} (GameObject 생성/이동/활성화 없음)
5. CharacterSpawnIntegrationPolicy.cs — 시작 셀 존재→월드 경계(IsValid)→방 경계(min/max)→방 유도 일치(FromWorldTile) 순 검증, 실패 시 예외 대신 진단, 월드 중심은 공용 브리지에서만
6. CharacterRouteBoundarySide.cs — enum {Left, Right, Up, Down}
7. CharacterRouteRequirement.cs — enum {BasicMovement, BombSupport, RopeSupport, UnsupportedAdvancedMovement, UnsupportedCombatAction} — 잠금 밖 요구는 Unsupported 분류로 추상화(금지 기능 명칭을 계약 표면에 들이지 않음)
8. CharacterGeneratedRouteEdgeSnapshot.cs — {RouteId, SourceRoom, TargetRoom, BoundarySide, SourceExitCell, TargetEntryCell, Requirement}
9. CharacterGeneratedRouteTransitionRequest.cs — {RouteId, SourceRoom, TargetRoom, BoundarySide, TargetEntryCell} — 입력/속도/카메라 필드 자체가 없음
10. CharacterRouteCapabilityPolicy.cs — IsRouteSupported: 기본 이동 항상 수용, 폭탄/로프는 보유>0일 때만, Unsupported 항상 거부(진단 전용, 소모 없음)
11. CharacterRouteIntegrationPolicy.cs — TryFindDeclaredEdge / TryCreateRouteTransitionRequestForRooms(미선언→UndeclaredRouteEdge 진단) / TryCreateRouteTransitionRequest(CHAR03 CharacterRoomBoundaryGate.Evaluate를 그대로 호출: 미등록 방→RouteBlockedMissingRoom, 미준비→RouteBlockedUnpreparedRoom, 준비→요청)
12. CharacterIntegrationBatchPolicy.cs — BuildBatch: 스폰 선두 → 선언 엣지 입력 순서(결정적) → 역량/게이트 통과분만 요청, 동등 요청 1회, 결함은 전부 진단 목록으로

Tests — `Assets/_Game/Tests/EditMode/Character/Integration/` (신규 폴더, 4파일):

13. CharacterGeneratedMapStartTests.cs (3 tests)
14. CharacterGeneratedRouteTests.cs (3 tests, 테스트 전용 FakeReadinessSource)
15. CharacterRouteCapabilityAndBatchTests.cs (4 tests)
16. CharacterIntegrationGuardTests.cs (2 tests, 질의 카운팅 CountingReadinessSource)

(+ Unity 자동 생성 .meta)

## TEST

Unity Version: 6000.3.8f1
EditMode Assembly: Game.Character.Tests.EditMode
Result: **170/170 PASS** (failed 0, skipped 0, 0.640s) — 이전 158 + 신규 12

요구 테스트명 12건 → 실제 테스트명 매핑 (전부 요구명 그대로 사용):

| 요구 동작/테스트명 | 실제 테스트 | 파일 |
|---|---|---|
| GeneratedMapStart_ValidStartCreatesPlayerSpawnRequest | 동일명 | CharacterGeneratedMapStartTests.cs |
| GeneratedMapStart_InvalidOrOutOfBoundsStartCreatesDiagnosticOnly | 동일명 (누락/월드 밖/방 밖/방 불일치 4경로) | CharacterGeneratedMapStartTests.cs |
| GeneratedMapStart_SpawnRequestUsesMapCoordinateBridgeCenter | 동일명 (브리지 값 일치 + 5.5/3.5 수치) | CharacterGeneratedMapStartTests.cs |
| GeneratedRoute_DeclaredRouteCreatesTransitionRequest | 동일명 | CharacterGeneratedRouteTests.cs |
| GeneratedRoute_UndeclaredRouteIsRejected | 동일명 (역방향 미선언 + 빈 목록) | CharacterGeneratedRouteTests.cs |
| GeneratedRoute_RespectsRoomTransitionReadinessContract | 동일명 (Missing/Unprepared/Allowed 3상태 + KEEP 구조 보장) | CharacterGeneratedRouteTests.cs |
| RouteCapability_BasicMovementRouteIsAccepted | 동일명 | CharacterRouteCapabilityAndBatchTests.cs |
| RouteCapability_ForbiddenMovementOrAttackRequirementsAreRejected | 동일명 (Unsupported 2종 항상 거부) | CharacterRouteCapabilityAndBatchTests.cs |
| RouteCapability_BombAndRopeRequirementsRequireAvailableSupport | 동일명 (보유 유/무 + 소모 없음) | CharacterRouteCapabilityAndBatchTests.cs |
| IntegrationBatch_IsDeterministicOrderedAndDeduplicated | 동일명 (중복 엣지·거부 혼재, 반복 호출 동일, 불량 시작 진단 흡수) | CharacterRouteCapabilityAndBatchTests.cs |
| Integration_DoesNotMutateMapTilemapScenePrefabPlayerTransformOrRunState | 동일명 | CharacterIntegrationGuardTests.cs |
| IntegrationRuntime_DoesNotUseAnimatorPhysicsUiAudioSceneSaveOrForbiddenActions | 동일명 | CharacterIntegrationGuardTests.cs |

## UNITY

- refresh_unity(force + compile) 2회: 컴파일 정상, `error CS` 필터 콘솔 에러 0건
- run_tests: 1차 164/170 — 기존 어셈블리 전역 가드 6건이 신규 enum 멤버 `UnsupportedAttack`의 "Attack" 부분 문자열을 거부(가드가 올바르게 작동한 사례). 멤버를 `UnsupportedCombatAction`으로 개명(의미 동일, 잠금 밖 공격류 요구 분류) → 2차 **170/170 PASS**

## GENERATED_MAP_START_AND_SPAWN

- 스냅샷: {맵/런 ID, 시작 방 ID, 시작 셀, 방 경계(포함 min/max 셀)} 값 데이터 — 라이브 생성기에서 스냅샷을 만드는 생산자는 후속 통합 계층 소관
- 검증 순서: HasStartCell → WorldCoordinateUtility.IsValid(월드 624×416) → 방 경계 사각형 포함 → CharacterRoomId.FromWorldTile(셀) == 선언 시작 방 (4중 검증, 각 실패는 고유 진단)
- 유효 시작 → CharacterPlayerSpawnRequest {ActorId, StartCell, WorldCenter=CharacterMapCoordinateBridge.GetCellCenter(셀), StartRoomId} — 공용 브리지 외 좌표 계산 없음(테스트가 브리지 값과 일치 단언)
- GameObject 생성·이동·활성/비활성·변조 없음 — 요청 값 객체뿐

## GENERATED_ROUTE_TRANSITION

- 선언 엣지 스냅샷은 출발 이탈 셀 + 도착 진입 셀을 함께 기록 — CHAR03 CharacterRoomBoundaryGate.Evaluate(fromTile, toTile)를 개조 없이 그대로 재사용해 도착 방 준비 게이트를 판정
- (출발→도착) 방 쌍이 선언 목록에 없으면 UndeclaredRouteEdge 진단만 (요청 없음)
- 게이트 3상태 계약 그대로: 방 정보 없음→RouteBlockedMissingRoom, 미준비→RouteBlockedUnpreparedRoom, 준비→요청 생성
- KEEP/hysteresis 존중은 데이터 계약으로 보장: 전환 요청에 입력/속도/카메라 필드 자체가 없어 재작성이 구조적으로 불가능(리플렉션 단언), 카메라 전환·hysteresis는 CHAR03_02 정책 소유 그대로 — 요청은 카메라 이동/씬 로드/transform 이동/MAP 변조를 하지 않음

## ROUTE_CAPABILITY_CHECK

- BasicMovement(2셀 점프/2셀 틈 문법): 잠금 이동 프로필이 CHAR02 코스로 보증하므로 항상 수용
- BombSupport/RopeSupport: CharacterRunInventoryState 보유량 >0일 때만 수용, 없으면 MissingBombSupport/MissingRopeSupport 진단 — 판정은 진단 전용이며 인벤토리를 소모하지 않음(불변 단언)
- UnsupportedAdvancedMovement/UnsupportedCombatAction: 대시/벽점프/이중점프/사격/일반공격류 요구의 추상 분류 — 보유량과 무관하게 항상 거부(UnsupportedRouteRequirement 진단). 금지 기능 명칭을 enum 멤버로 쓰지 않은 것은 기존 전역 금지 명명 가드와의 정합 설계(UNITY 절의 1차 실패가 이 가드의 정상 작동 증거)

## INTEGRATION_REQUEST_BATCH

- BuildBatch(시작 스냅샷, actorId, 선언 엣지 목록, 인벤토리, 준비 소스 → 스폰/루트/진단 3개 출력 리스트): 같은 입력 → 같은 출력(반복 호출 동일성 테스트)
- 순서: 스폰(0..1건) 선두 → 루트는 선언 목록 입력 순서 그대로(결정적) — 동등 요청(RouteId+방+경계+진입 셀 전 필드 비교)은 1회만 방출
- 역량 거부·게이트 차단·불량 시작은 전부 예외 없이 진단 목록으로 흡수(월드 밖 시작 셀 케이스 테스트)
- null 선언 목록/준비 소스도 진단·빈 결과로 처리(복구 가능 결함 무예외 원칙)

## AUTHORITY_AND_FORBIDDEN_FEATURE_GUARD

- Integration 전 타입(12개 이상): MonoBehaviour/Component 아님 + OnCollision*/OnTrigger* 부재 스캔 — 물리 콜백 권위 불가
- 어셈블리 참조: AnimationModule·TilemapModule·UIModule·UnityEngine.UI·AudioModule·TMP 전부 부재 (Physics2DModule은 CHAR01 승인 질의 어댑터 소관 — 확립 검증 레벨)
- 표면 타입 스캔: Animator/Tilemap/Rigidbody/Collider/RaycastHit/Scene/Canvas/Audio/GameObject/PlayerPrefs 부재; 변조형 명명(Instantiate/Destroy/Teleport/LoadScene/SetTile/Mutate/Apply) 부재; 공개 setter·가변 공개 필드(enum 제외) 0건
- 행동 검증: 배치 실행 후 인벤토리·준비 소스·스냅샷 전부 불변(질의 카운터로 읽기 전용 확인)
- 금지 개념 명명(BasicAttack/Melee/Shoot/Dash/WallJump/DoubleJump) 부재 + ActionId 잠금 5종 동등성 단언

## DEPENDENCY_DIRECTION

- StarNight.Character.Integration → StarNight.Character.MapIntegration (RoomId·게이트·브리지 재사용) + StarNight.Character.RunState (인벤토리 스냅샷 읽기) + StarNight.Map.WorldGeneration.Domain (좌표·경계 검증)
- 역방향 없음: MapIntegration/RunState/기타 모듈은 Integration을 모름(기존 파일 무수정이 구조적 증거); MAP 런타임은 Character를 모름
- asmdef 변경 0건

## SCOPE_VALIDATION

- git status 확인: 신규 파일 전부 허용 경로 내 — Integration 런타임 12 + Integration 테스트 4 (+ .meta). 허용된 MapIntegration 경로는 변경 불필요(게이트 무수정 재사용)
- 조건부 브리지 쓰기(Movement/RunState/Presentation) 미사용
- 기존 파일 수정 0건; Scene/prefab/physics asset/inputactions/Packages/MapDesign/MAP 런타임/Tilemap/카메라/애니메이션/오디오/UI/세이브/레거시 변경 0건; 랜덤 시드 스윕·마이크로청크/아이템 검증 없음(CHAR06_02 소관 준수)
- ProjectSettings 추적 변경 2건은 과제 이전부터 존재한 사용자 수정으로 계속 미접촉

## DEPENDENCY_LEDGER

- 사용(기존 승인): CharacterRoomId(.FromWorldTile/.Equals), CharacterRoomBoundaryGate/ICharacterRoomReadinessSource/CharacterBoundaryCrossDecision(CHAR03), CharacterMapCoordinateBridge.GetCellCenter, CharacterRunInventoryState(CHAR05_04), WorldTileCoord/WorldCoordinateUtility.IsValid/WorldGenConstants(MAP 공용), CharacterActionId(테스트 잠금 확인)
- 신규 공개 계약(후속 소비 예정): CharacterPlayerSpawnRequest(실 스폰 적용 — CHAR06_03/라이브 배선), CharacterGeneratedRouteTransitionRequest(카메라/이동 통합 소비), CharacterGeneratedMapStartSnapshot·CharacterGeneratedRouteEdgeSnapshot(생성기→스냅샷 생산자 — CHAR06_02+), CharacterIntegrationDiagnostic(검증/디버그 리포트 — CHAR06_02)
- 미사용: Tilemap, Animator, Physics2D(Integration 범위), UI/Audio/Scene/Save, Stage/레거시, 에디터 API

## OUT_OF_SCOPE_FINDINGS

- 소스 레지스트리 기록대로 MAP에는 캐릭터 소비용 공용 루트 그래프/시작점 계약이 아직 없음(MoonPalace 생성 내부 타입뿐) — 본 과제는 캐릭터측 스냅샷 DTO로 계약을 정의했고, 생성 결과→스냅샷 생산자는 CHAR06_02+ 소관. MAP측 공용 계약이 생기면 필드 호환 브리지로 연결 가능
- 기존 전역 금지 명명 가드 6건이 "Attack" 부분 문자열까지 잡아내 1차 실행을 막았음 — 가드가 설계 의도대로 작동한 증거로 기록(개명으로 해소, 기능 의미 불변)
- Assets/_Game/Tests/PlayMode/Map asmdef의 stale `Game.Stage.Runtime` 참조 — MAP 하니스 소관, 계속 미수정 유지

## DONE CONDITIONS

- [x] CHAR05 EXIT approved and CHAR05_05 PASS/hash verified.
- [x] Source registry marker/hash verified.
- [x] Valid generated map start creates player spawn request.
- [x] Invalid or out-of-bounds start creates diagnostic only.
- [x] Spawn request uses public map coordinate bridge.
- [x] Declared route creates transition request.
- [x] Undeclared route is rejected.
- [x] Route transition respects CHAR03 readiness/input/velocity contract by data.
- [x] Basic movement route is accepted.
- [x] Forbidden movement/attack route requirements are rejected.
- [x] Bomb and rope route requirements require available support.
- [x] Integration batch is deterministic, ordered, and deduplicated.
- [x] Integration output does not mutate MAP, Tilemap, scene, prefab, player transform, run state, UI, audio, or save data.
- [x] Animator events and physics callbacks are not authority.
- [x] Forbidden basic attack/movement features remain absent.
- [x] ActionId locked set remains unchanged.
- [x] Character EditMode tests pass with at least 170 tests. (170/170)
- [x] Unity compile errors 0.
- [x] Scope validation completed.
- [x] CHAR06_02 remains locked.

## NEXT

Current Task after finalize: NONE
Next Task auto-opened: NO (`CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
