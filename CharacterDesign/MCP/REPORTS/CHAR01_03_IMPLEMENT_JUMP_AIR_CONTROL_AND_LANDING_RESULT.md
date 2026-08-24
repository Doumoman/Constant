# TASK RESULT

TASK: CHAR01_03_IMPLEMENT_JUMP_AIR_CONTROL_AND_LANDING
STATUS: PASS

## SUMMARY

핵심 이동의 마지막 구현 단계. 점프(버퍼 0.12·코요테 0.10·단일 소비), 가변 점프 release(상승 중 1회 cut), rise/fall 분리 중력 + 최대 낙하 clamp, 공중 수평 제어, 착지 전환(airborne→grounded에서만 발화, 점프 소비 reset)을 구현했다. EditMode 36/36 PASS(신규 12 + CHAR01_01/02 회귀 24). 2셀/3셀 코스 검증은 CHAR02 소관으로 구현하지 않았다.

## READ

- Mandatory Read Order 19개 문서
- CHAR01_01/02 runtime/test API(작성 이력 기반), asmdef 경계 확인

## CREATED

Runtime (`Assets/_Game/Character/Runtime/Movement/`, 8개):

- `CharacterJumpSettings.cs` — jumpVelocity(>0)/coyoteTime(≥0)/jumpBufferTime(≥0)/releaseCutMultiplier([0,1)) 검증, Default(10.28, 0.10, 0.12, 0.5 — 레거시·스키마 기준선)
- `CharacterJumpState.cs` — grounded/press 시각(NegativeInfinity 초기), JumpConsumed, ReleaseCutApplied 추적. NoteGrounded가 소비·release reset. 공중 재점프는 소비 플래그로 차단
- `CharacterJumpController.cs` — TryStartJump: 버퍼 내 press ∧ (grounded ∨ 코요테) ∧ 미소비 → vy=jumpVelocity + press 소비(1회). 조건 미충족 시 press 보존, 만료는 buffer 체크가 처리. ApplyJumpRelease: 상승 중 + release + 점프 소비 상태에서 cut 계수 1회 적용
- `CharacterGravitySettings.cs` — riseGravity/fallGravity/maxFallSpeed 전부 >0 검증, Default(24, 30, 18)
- `CharacterGravityMotor.cs` — 상승 rise/하강 fall 중력, -maxFallSpeed clamp, grounded면 중력 미적용(하강 누적 없음)
- `CharacterAirControlSettings.cs` — airAcceleration(≥0)/maxAirSpeed(>0), Default(22.5, 3.75)
- `CharacterAirControlMotor.cs` — airborne에서만 [-1,1] clamp 입력으로 MoveTowards 수평 제어, vertical 불변, 지상은 ground motor 소관
- `CharacterLandingDetector.cs` — airborne→grounded 전환만 landing, 하강 속도 0 정리, jumpState.NoteGrounded로 소비 reset. Animator/사운드/렌더 비의존

EditMode Tests (신규 3개): `CharacterJumpControllerTests.cs`, `CharacterAirAndGravityMotorTests.cs`, `CharacterLandingAndMovementBoundaryTests.cs`

Unity 생성 `.meta`: 신규 .cs 11개 대응(허용 범위, 기록)

Report: 본 파일

## CHANGED

- `Assets/_Game/Tests/EditMode/Character/CharacterMovementBoundaryTests.cs` — WRITE ALLOWLIST가 명시 허용한 CHAR01_03 기준 업데이트. 테스트 이름 2개는 유지한 채 경계 의미 갱신: `MovementRuntime_DoesNotDeclareJumpGravityAirControlOrLandingTypes`는 이제 해당 개념이 Input/State namespace로 새지 않고 승인된 Movement namespace에만 존재함을 검증. `MovementRuntime_DoesNotDeclareForbiddenMovementFeatures`(WallJump/Dash/DoubleJump/Attack/Melee 금지)는 그대로 유지
- 그 외 기존 파일 수정 0 — `CharacterGroundMotorState`/`CharacterPlayerState`/`CharacterPlayerStateSnapshot`은 최소 연동 API 추가가 불필요해 미수정(신규 모듈이 기존 타입을 참조만 함). asmdef 2개·상태 파일 미수정

## IMPLEMENTATION

- 점프 소비 모델: press는 CharacterJumpState가 시각으로 추적, TryStartJump 성공 시 press 제거+JumpConsumed 설정 → 같은 press 재소비와 공중 2단 점프가 모두 구조적으로 불가(코요테 창 안이라도 차단, 테스트 고정)
- 가변 release는 속도 cut 방식(레거시의 release 중력 배수 선례를 velocity cut으로 재해석) — 상승 중 + 점프 소비 상태에서만 1회
- 중력은 grounded에서 미적용이라 접지 중 하강 누적이 없고, 착지 시 잔여 하강 속도는 LandingDetector가 0으로 정리(상승 속도는 유지)
- 모든 시간·틱 주입식 순수 로직 — scene 없이 EditMode 검증

## TEST

실행: Unity MCP `run_tests`(EditMode, assembly=Game.Character.Tests.EditMode 전체, job `d378bd0e…`)

| 항목 | 값 |
|---|---|
| 전체/성공/실패/스킵 | 36 / 36 / 0 / 0 (resultState=Passed, 1.36s) |
| CHAR01_03 필수 12개 | 전부 Passed |
| CHAR01_01 회귀 12개 | 전부 Passed |
| CHAR01_02 회귀 12개 | 전부 Passed (boundary 2개는 허용된 CHAR01_03 기준 갱신본) |

CHAR01_03 필수 12개: JumpBuffer_PressBeforeGroundedTriggersOnGroundedTick, CoyoteTime_AllowsJumpShortlyAfterLeavingGround, Jump_IsConsumedOnceAndSetsUpwardVelocity, Jump_DoesNotAllowSecondJumpBeforeGroundedAgain, AirControl_AcceleratesHorizontallyOnlyWhileAirborne, AirControl_ClampsHorizontalIntentAndPreservesVerticalVelocity, Gravity_UsesRiseGravityWhenAscendingAndFallGravityWhenDescending, Gravity_ClampsToMaxFallSpeed, VariableJumpRelease_ReducesUpwardVelocityOnlyWhileAscending, LandingDetector_FiresOnlyOnAirborneToGroundedTransition, LandingDetector_ResetsJumpConsumedState, MovementRuntime_DoesNotDeclareForbiddenMovementOrBasicAttackFeatures

## UNITY

- Unity Version: 6000.3.8f1
- Asset Refresh: PASS
- Compile Errors: 0 (CS 필터 0건)
- Relevant New Warnings: 0
- Targeted EditMode Tests: PASS 36/36
- PlayMode Tests: NOT RUN (Task 지정)
- Scene/Prefab Changes: 0

## SCOPE VALIDATION

- `git status -- Assets`: `Assets/_Game/Character/**` + `Assets/_Game/Tests/EditMode/Character/**`(+.meta) 외 변경 0
- Movement .cs 18개(기존 10 + 신규 8 — WRITE ALLOWLIST 정확 일치), 테스트 .cs 9개(기존 6 + 신규 3, boundary 1개 갱신)
- Packages/MapDesign 0, ProjectSettings 기존 사용자 변경 2건 외 0
- inputactions/asmdef/Scene/Prefab/Animator/ScriptableObject 변경 0. 2셀/3셀 코스 검증 미구현. CHAR01_04 미시작

## OUT_OF_SCOPE_FINDINGS

- 기존 관찰 유지: stale Map PlayMode asmdef 레거시 참조

## DONE CONDITIONS

- [x] CHAR01_02 PASS·finalize 검증(REPORT sha `bc637e31…`, required_text 3건)
- [x] 승인된 Movement runtime/test 파일만 추가·수정
- [x] Jump buffer, coyote time, single jump consumption 구현
- [x] 가변 점프 release는 상승 중에만 1회 적용
- [x] rise/fall gravity + max fall speed clamp 구현
- [x] airborne 전용 air control + vertical 보존
- [x] landing 전환 + jump consumed reset 구현
- [x] 2셀/3셀 코스 검증 미구현
- [x] 일반 공격·wall jump·dash·double jump 없음(reflection 테스트 유지)
- [x] MAP/Tilemap/Scene/Prefab/inputactions/asmdef/Packages/ProjectSettings 변경 0
- [x] CHAR01_03 필수 12개 전부 PASS
- [x] CHAR01_01 12개 + CHAR01_02 12개 전부 PASS
- [x] compile error 0, relevant new warning 0
- [x] 하네스 상태 파일 미수정
- [x] CHAR01_04 미시작

## NEXT

- Current Task after finalize: NONE
- Next Task auto-opened: NO (`CHAR01_04_CHAR01_CORE_MOVEMENT_EXIT_AUDIT`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
