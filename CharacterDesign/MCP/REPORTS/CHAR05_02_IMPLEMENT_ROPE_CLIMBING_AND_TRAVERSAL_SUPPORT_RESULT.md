# CHAR05_02_IMPLEMENT_ROPE_CLIMBING_AND_TRAVERSAL_SUPPORT_RESULT

## TASK

TASKS/CHAR05_02_IMPLEMENT_ROPE_CLIMBING_AND_TRAVERSAL_SUPPORT.md

## STATUS

STATUS: PASS

## SUMMARY

로프 설치/소모 요청, 결정적 수직 세그먼트 생성(경계·고체·중앙 최대 길이 6셀 제한), 로프 겹침+등반 의도 판정, 상/하한 clamp가 있는 등반 모터 요청을 순수 값 객체 + 정적/결정적 정책으로 구현했다. 모든 산출물은 "요청"이며 프리팹 생성·씬 배치·인벤토리·MAP/Tilemap·플레이어 상태 어떤 것도 직접 변조하지 않는다. 신규 12개 테스트 포함 Game.Character.Tests.EditMode 134/134 PASS, 컴파일 에러 0건.

## READ

- CharacterDesign/MCP/00_MCP_ENTRYPOINT.md ~ 05_CHANGE_CONTROL_RULES.md (전역 규칙)
- CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md, MASTER_IMPLEMENTATION_TASK_LIST.md
- CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md
- CharacterDesign/MCP/TASKS/CHAR05_01_IMPLEMENT_BOMB_AND_TERRAIN_MUTATION_REQUEST.md
- CharacterDesign/MCP/REPORTS/CHAR05_01_IMPLEMENT_BOMB_AND_TERRAIN_MUTATION_REQUEST_RESULT.md, CHAR04_04_CHAR04_INTERACTION_COMBAT_EXIT_AUDIT_RESULT.md
- CharacterDesign/01_FIXED_SPEC/01_CHARACTER_GAMEPLAY_RULES.md, 02_CHARACTER_INPUT_RULES.md, 06_CHARACTER_MAP_INTEGRATION_RULES.md, 07_CHARACTER_TEST_RULES.md
- CharacterDesign/03_DATA_SCHEMA/CHARACTER_INVENTORY_SCHEMA.md (ropeCount), CHARACTER_ACTION_SCHEMA.md (Rope=C 잠금)
- CharacterDesign/04_TEST_FIXTURES/COMBAT_COURSE_SPEC.md
- Assets/_Game/Character/Runtime/ 현행 전체(Equipment/MapIntegration/Input/Movement 표면), 기존 EditMode 테스트
- MAP 공용 계약: WorldTileCoord, WorldCoordinateUtility(WorldWidthTiles 624 / WorldHeightTiles 416), ICharacterMapWorldQuery, CharacterMapCoordinateBridge
- 레거시 읽기 전용 선례: Assets/_Legacy/StarNight/Scripts/Runtime/Tools/Rope/RopePlacementModels.cs (DefaultMaximumLength=6), RopeClimber2D.cs (climbSpeed=4f), Assets/_Legacy/_Game/Tools/Runtime/Rope/RopeClimbController.cs, Assets/_Legacy/_Game/Core/State/RunState.cs (초기 ropes=4)

Entry Gate 검증:

- Current Task = TASKS/CHAR05_02_IMPLEMENT_ROPE_CLIMBING_AND_TRAVERSAL_SUPPORT.md 확인
- CHAR05_01 REPORT: STATUS: PASS + sha256 `1c5036404d957cc5ca534d4c0ec89e77995c3d6adfa66306b73915bc42005e7f` 일치 + required_text 3건("Current Task after finalize: NONE" / "CHAR05_02_..." / "LOCKED 유지") 확인
- CHAR05_01 Task sha256 `a12be0d7...` 일치
- Source Registry marker `REGISTRY_STATE: FILLED_BY_CHAR00_01` + sha256 `be6cadc4...` 일치
- CHAR05_03 이후 전부 LOCKED 확인

## CHANGED

- 없음 (기존 파일 수정 0건 — 폭탄/이동/충돌/방 전환 코드 불변; asmdef 변경 없이 기존 어셈블리가 신규 Traversal 폴더를 자동 포함함을 컴파일로 확인)

## CREATED

Runtime — `Assets/_Game/Character/Runtime/Equipment/` (namespace `StarNight.Character.Equipment`):

1. CharacterRopeSettings.cs — 중앙 설정: 최대 길이 6셀 / 등반 속도 4u/s (레거시 선례), 유효성 검증
2. CharacterRopePlacementInput.cs — 설치 판정 입력 스냅샷(actor, 원점 셀, 보유 수량, 설치 가능 여부)
3. CharacterRopePlacementRequest.cs — 설치 요청(값 객체)
4. CharacterRopeSpendRequest.cs — 로프 1개 소모 요청(인벤토리 변조 없음)
5. CharacterRopePlacementPolicy.cs — 보유>0 ∧ 유효 원점 ∧ 설치 가능 → 설치+소모 요청 동시 발행

Runtime — `Assets/_Game/Character/Runtime/Traversal/` (namespace `StarNight.Character.Traversal`, 신규 폴더):

6. CharacterRopeSegmentRequest.cs — 세그먼트 요청(로프 ID + 셀 + 원점 기준 순번)
7. CharacterRopeSegmentPolicy.cs — origin에서 위로 한 열 결정적 생성; 월드 경계/고체 차단/최대 길이에서 중단
8. CharacterRopeExtent.cs — 세그먼트 목록 → 등반 범위(하단/상단 셀·월드 Y) 파생
9. CharacterRopeClimbInput.cs — 등반 판정 입력 스냅샷(겹침, 의도, 수직 축, 현재 Y, 범위)
10. CharacterRopeClimbMotorRequest.cs — 모터 요청(수직 속도 + clamp된 목표 Y만 — 수평 성분 없음)
11. CharacterRopeClimbPolicy.cs — 겹침+의도 → 요청; 축 [-1,1] clamp; 목표 Y를 로프 상/하한으로 clamp

Tests:

12. Assets/_Game/Tests/EditMode/Character/Equipment/CharacterRopePlacementTests.cs (3 tests)
13. Assets/_Game/Tests/EditMode/Character/Traversal/CharacterRopeSegmentTests.cs (3 tests, 테스트 전용 FakeMapWorldQuery)
14. Assets/_Game/Tests/EditMode/Character/Traversal/CharacterRopeClimbTests.cs (4 tests)
15. Assets/_Game/Tests/EditMode/Character/Traversal/CharacterRopeGuardTests.cs (2 tests)

(+ Unity 자동 생성 .meta — 신규 폴더 Traversal 2곳 포함)

## TEST

Unity Version: 6000.3.8f1
EditMode Assembly: Game.Character.Tests.EditMode
Result: 134/134 PASS (failed 0, skipped 0, 0.666s) — 이전 122 + 신규 12, 첫 실행 전건 통과

요구 테스트명 12건 → 실제 테스트명 매핑 (전부 요구명 그대로 사용):

| 요구 동작/테스트명 | 실제 테스트 | 파일 |
|---|---|---|
| RopePlacement_AvailableRopeCreatesPlacementAndSpendRequest | 동일명 | CharacterRopePlacementTests.cs |
| RopePlacement_NoRopeCreatesNoPlacement | 동일명 (0개·음수) | CharacterRopePlacementTests.cs |
| RopePlacement_BlockedOrOutOfBoundsOriginRefusesPlacement | 동일명 | CharacterRopePlacementTests.cs |
| RopeSegments_GenerateVerticalCellsUntilBlockedOrMaxLength | 동일명 (최대 6·차단 3·경계 3·원점 고체 0) | CharacterRopeSegmentTests.cs |
| RopeSegments_AreDeterministicOrderedAndDeduplicated | 동일명 (재호출 동일, 오름차순, 중복 0) | CharacterRopeSegmentTests.cs |
| RopeSegments_DoNotMutateMapOrTilemap | 동일명 (생성 후 맵 상태 불변) | CharacterRopeSegmentTests.cs |
| RopeClimb_OverlapAndClimbIntentCreatesMotorRequest | 동일명 | CharacterRopeClimbTests.cs |
| RopeClimb_NoOverlapOrNoIntentCreatesNoMotorRequest | 동일명 (겹침 없음·의도 없음 각각) | CharacterRopeClimbTests.cs |
| RopeClimb_UpDownInputProducesVerticalVelocity | 동일명 (+4/-4/0 hold/과대축 clamp) | CharacterRopeClimbTests.cs |
| RopeClimb_TopAndBottomBoundsClampTraversal | 동일명 (상/하한·월드 상단 경계) | CharacterRopeClimbTests.cs |
| RopeRuntime_DoesNotUseAnimatorPhysicsTilemapOrForbiddenActions | 동일명 | CharacterRopeGuardTests.cs |
| RopeRuntime_DoesNotIntroduceDashWallJumpDoubleJumpOrBasicAttack | 동일명 | CharacterRopeGuardTests.cs |

## UNITY

- refresh_unity(force + compile) 1회: 컴파일 정상, `error CS` 필터 콘솔 에러 0건
- run_tests(EditMode, Game.Character.Tests.EditMode): 134/134 PASS (수정 반복 없이 1차 통과)

## ROPE_PLACEMENT

- 판정식: `AvailableRopeCount > 0 && HasValidOriginCell && IsOriginPlaceable` 전부 참일 때만 설치 요청 생성
- 설치 요청과 소모 요청(Amount=1)은 항상 쌍으로 발행 — 소모는 요청일 뿐 ropeCount를 어디서도 변조하지 않음(CHARACTER_INVENTORY_SCHEMA 계약 유지)
- 보유 0/음수, 막힘·점유 원점, 월드 밖 원점 → 설치·소모 요청 모두 없음
- 입력은 Rope=C 논리 행동의 하류 스냅샷이며 ActionId 잠금 5종의 기존 `Rope` 슬롯 사용(신규 ActionId·라이브 입력 배선 없음); 프리팹 생성/씬 배치 없음

## ROPE_SEGMENT_GENERATION

- origin 셀부터 위로(offset 0..MaxRopeLengthCells-1) 한 열을 따라 생성 — 아래→위 고정 순서, IndexFromOrigin 0..n-1, 구조상 중복 없음, 재호출 시 완전 동일(결정적)
- 중앙 최대 길이: CharacterRopeSettings.MaxRopeLengthCells = 6 (레거시 RopePlacementSolver.DefaultMaximumLength 선례) — 빈 열에서 정확히 6개로 캡
- 월드 경계: WorldCoordinateUtility.TryCreateWorldTile 실패 시 중단 — y=413 시작이면 415까지 3개 후 중단(WorldHeightTiles=416)
- 고체 차단: ICharacterMapWorldQuery 조회로 `IsSolid` 셀 진입 전에 중단 — (10,8) 고체면 (10,5)~(10,7) 3개; 원점 자체가 고체면 0개(방어적)
- 데이터 없는 셀은 CharacterMapCellState.Empty 의미와 동일하게 통과 가능으로 해석
- 세그먼트 요청은 셀 좌표·로프 ID·순번만 기술 — 생성 후 맵 상태 불변을 테스트로 확인, MAP/Tilemap/씬/프리팹/물리 에셋 변조 없음

## ROPE_CLIMB_TRAVERSAL

- 판정식: `IsOverlappingRope && HasClimbIntent`일 때만 모터 요청 — 겹침 없음 또는 의도 없음이면 요청 자체가 없음
- 수직 축 [-1,1] clamp × 등반 속도 4u/s: 위 입력 → +4, 아래 입력 → -4, 축 0 → 속도 0·목표 Y=현재 Y(제자리 유지 hold, 고정 규칙 허용 형태), 과대 축(+3)도 +4로 제한
- 모터 요청은 {ActorId, VerticalVelocity, TargetWorldY}만 기술하는 값 객체 — 플레이어 상태/속도 직접 변조 없음
- 기존 이동 상태 기계/점프/공중 제어/충돌 질의/방 전환 코드는 일절 재작성하지 않음; 요청 소비자가 없는 현 단계에서 브리지 어댑터도 불필요해 조건부 Movement 쓰기 경로는 사용하지 않음(후속 통합 단계 소관)

## ROPE_BOUNDARY_RULES

- 목표 Y = clamp(현재 Y + 속도×dt, 로프 하단 월드 Y, 로프 상단 월드 Y) — CharacterRopeExtent가 세그먼트 목록에서 최하단/최상단 셀 중심 Y를 파생
- 6셀 로프 (10,5)~(10,10) → 범위 [5.5, 10.5]: 상단 근처 위 입력 → 10.5에서 정지, 이미 상단이면 dt가 커도 10.5 유지, 하단 근처 아래 입력 → 5.5에서 정지
- 생성된 로프 범위 밖으로는 못 오르며, 세그먼트가 경계 안에서만 생성되므로 로프 clamp가 곧 월드 경계 보호를 함의 — 월드 상단(셀 415, 중심 415.5) 로프에서 dt=5s 위 입력에도 415.5 초과 불가를 테스트로 확인
- 로프 등반은 수직 성분만 존재 — 벽점프/대시/더블점프/추가 공중 제어를 부여할 표면 자체가 없음(가드로 검증)

## AUTHORITY_AND_FORBIDDEN_FEATURE_GUARD

- 로프 계약 표면(Traversal 전체 + Equipment의 Rope 타입, 11개 이상): MonoBehaviour/Component 아님(리플렉션) → 물리 콜백 호출 자체가 불가능, 추가로 OnCollision*/OnTrigger* 메서드 부재 직접 스캔
- 어셈블리 참조: UnityEngine.AnimationModule·UnityEngine.TilemapModule 부재 — Animator 이벤트/Tilemap 직접 접근이 권위가 될 수 없음 (Physics2DModule 참조는 CHAR01 승인 충돌 질의 어댑터 소관으로 CHAR05_01 REPORT에서 확립한 검증 레벨 유지)
- 표면 타입 스캔: Animator/Tilemap/Rigidbody/Collider/RaycastHit 부재
- 금지 개념: 어셈블리 전체 타입명 + 로프 표면 멤버명에서 Dash/WallJump/DoubleJump/BasicAttack/Melee/Shoot 부재, AirControl/AirAccel 부재
- 모터 요청 공개 속성이 정확히 {ActorId, VerticalVelocity, TargetWorldY}임을 동등성 단언 — 수평 속도 멤버가 없어 구조적으로 추가 공중 제어 불가
- ActionId 잠금 5종 {Jump, Action, SafeDrop, Bomb, Rope} 그대로 (Enum.GetNames 동등성 단언)

## DEPENDENCY_DIRECTION

- StarNight.Character.Traversal → StarNight.Character.Equipment (CharacterRopeSettings) + StarNight.Character.MapIntegration (브리지·셀 질의) → StarNight.Map.WorldGeneration.Domain (WorldTileCoord, WorldCoordinateUtility)
- 역방향 없음: MAP 런타임은 Character를 모름; Movement/State/Combat은 로프를 모름(불변); 테스트만 Input의 ActionId 잠금 확인에 참조
- asmdef 변경 0건 — 기존 Game.Character.Runtime이 Traversal 폴더 자동 포함

## SCOPE_VALIDATION

- git status 확인: 신규 파일 전부 허용 경로 내 — Equipment 런타임 5 + Traversal 런타임 6 + Equipment 테스트 1 + Traversal 테스트 3 (+ .meta)
- 조건부 Movement 쓰기 경로 미사용(어댑터 불필요 판단, ROPE_CLIMB_TRAVERSAL 절에 문서화)
- 기존 파일 수정 0건(폭탄 동작 불변 포함); Scene/prefab/physics asset/inputactions/Packages/MapDesign/MAP 런타임/Tilemap/카메라/애니메이션/레거시 변경 0건
- ProjectSettings 추적 변경 2건(dev.yarnspinner json, ShaderGraphSettings.asset)은 과제 이전부터 존재한 사용자 수정으로 본 실행에서 건드리지 않음

## DEPENDENCY_LEDGER

- 사용(기존 승인): WorldTileCoord, WorldCoordinateUtility.TryCreateWorldTile(경계), CharacterMapCoordinateBridge.GetCellCenter, ICharacterMapWorldQuery.TryGetCellState, CharacterMapCellState.IsSolid, CharacterActionId(테스트 잠금 확인)
- 신규 공개 계약(후속 과제 소비 예정): CharacterRopePlacementRequest/CharacterRopeSpendRequest(인벤토리 적용 — CHAR05_03+), CharacterRopeSegmentRequest/CharacterRopeExtent(라이브 로프 표현·겹침 판정 소스 — CHAR06 통합), CharacterRopeClimbMotorRequest(이동 통합 소비 — 후속 단계)
- 레거시 수치 선례 채택: 최대 길이 6, 등반 속도 4f (읽기 전용 참조, 코드 미변경)
- 미사용: Tilemap, Animator, Physics2D(로프 범위), Stage/레거시, 에디터 API

## OUT_OF_SCOPE_FINDINGS

- 레거시 로프는 앵커(링/천장) 탐색·보호 경로 검사 등 더 복잡한 설치 모델을 가짐 — 현행 과제 계약(origin 위로 단순 수직 생성)과 의도적으로 다르며, 앵커류 개념이 필요해지면 별도 과제/CHANGE CONTROL 소관
- 겹침 판정(IsOverlappingRope)은 스냅샷 입력으로 받음 — 실제 위치×세그먼트 겹침 계산은 라이브 통합(CHAR06) 소관
- Assets/_Game/Tests/PlayMode/Map asmdef의 stale `Game.Stage.Runtime` 참조 — MAP 하니스 소관, 계속 미수정 유지

## DONE CONDITIONS

- [x] CHAR05_01 PASS/hash verified.
- [x] Source registry marker/hash verified.
- [x] Available rope and valid origin produce placement request.
- [x] No rope count or invalid origin refuses placement.
- [x] Placement emits spend request but does not mutate inventory.
- [x] Rope segment generation is vertical, deterministic, and deduplicated.
- [x] Max rope length caps generated segments.
- [x] Bounds and solid blockers stop segment generation.
- [x] Rope segment request does not mutate MAP, Tilemap, scene, prefab, or physics assets.
- [x] Rope overlap plus climb intent creates climb motor request.
- [x] No overlap or no intent creates no climb motor request.
- [x] Up/down input produces clamped vertical climb request.
- [x] Top/bottom rope bounds clamp traversal.
- [x] Rope traversal does not grant wall jump, dash, double jump, or extra air control.
- [x] Animator events and physics callbacks are not authority.
- [x] Forbidden basic attack/movement features remain absent.
- [x] ActionId locked set remains unchanged.
- [x] Character EditMode tests pass with at least 134 tests. (134/134)
- [x] Unity compile errors 0.
- [x] Scope validation completed.
- [x] CHAR05_03 remains locked.

## NEXT

Current Task after finalize: NONE
Next Task auto-opened: NO (`CHAR05_03_IMPLEMENT_HEALTH_HAZARDS_DEATH_AND_RUN_FAILURE`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
