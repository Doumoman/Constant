# TASK RESULT

TASK: CHAR01_02_IMPLEMENT_COLLISION_QUERIES_AND_GROUND_MOTOR
STATUS: PASS

## SUMMARY

CHAR01_01 입력·상태 모델 위에 충돌 질의 추상화(`ICharacterCollisionWorld` + Physics2D 어댑터), 잠금 기준선(캡슐 0.72×0.90, probe 0.08, vy≤0.05) 기반 접지 판정, 걷기/달리기 지상 수평 모터를 구현했다. EditMode 24/24 PASS(신규 12 + CHAR01_01 회귀 12). 점프·중력·공중 제어·착지·2/3셀 코스 검증·MAP 연동은 구현하지 않았다.

## READ

- Mandatory Read Order 18개 문서
- CHAR01_01 runtime/test API(작성 이력 기반 확인), `Game.Map.Tests.EditMode.asmdef` 경계
- 제한적 검색: `Assets/_Game/Character/**` 기존 파일 목록, asmdef 무변경 경계 확인

## CREATED

Runtime (`Assets/_Game/Character/Runtime/Movement/`, namespace `StarNight.Character.Movement`, 10개):

- `CharacterCapsuleGeometry.cs` — 잠금 기준선 0.72×0.90 기본값(BaselineWidth/Height 상수 + Default), 중심·크기·하단 계산, 셀/MAP 상수 미정의
- `CharacterCollisionHit.cs` — hit 여부/point/normal/distance/stable ColliderId 값 객체, fake world가 scene 없이 구성 가능
- `ICharacterCollisionWorld.cs` — capsule swept query 단일 메서드 interface(Tilemap/MAP/scene lookup 비의존)
- `UnityPhysics2DCharacterCollisionWorld.cs` — `Physics2D.CapsuleCast` + LayerMask 어댑터. MAP Tilemap/WorldCoordinateUtility/생성 맵 내부 미접근
- `CharacterGroundProbeSettings.cs` — probe 0.08 / 상승 임계 0.05 기준선 상수+Default, 음수 검증
- `CharacterGroundProbeResult.cs` — grounded/hasHit/normal/distance/SupportId, 명시적 empty(NotGrounded/UngroundedHit/Grounded 팩토리)
- `CharacterGroundProbe.cs` — 하향 CapsuleCast → (거리≤probe) ∧ (normal.y≥0.5 upward) ∧ (vy≤임계) 3게이트 접지 판정. miss/원거리/벽 normal/상승 거부. one-way는 interface 교체 확장 여지만 명시
- `CharacterGroundMotorSettings.cs` — walk/run/accel/decel + `runSpeed > walkSpeed > 0` 검증, Default(2.2/3.75/30/40 — 레거시 선례 기준선)
- `CharacterGroundMotorState.cs` — velocity/facing/locomotion immutable 값 객체(WithVelocity/WithLocomotion)
- `CharacterGroundMotor.cs` — 입력 [-1,1] clamp, walk/run 목표 속도, MoveTowards 가속/감속(overshoot 불가), facing은 비0 입력만 갱신, vertical 보존, 비접지 시 수평 지상 가속 미적용

EditMode Tests (3개):

- `CharacterGroundProbeTests.cs`(FakeCollisionWorld 포함), `CharacterGroundMotorTests.cs`, `CharacterMovementBoundaryTests.cs`

Unity 생성 `.meta`: `Movement` 폴더 1 + 신규 .cs 13개 = 14개(허용 범위, 기록)

Report: 본 파일

## CHANGED

- 기존 파일 수정 0 — `CharacterPlayerState.cs`/`CharacterPlayerStateSnapshot.cs`는 최소 연동 API 추가가 불필요해 미수정(모터가 State enum을 참조만 함). 두 asmdef 미수정. 상태 파일 미수정.

## IMPLEMENTATION

- 충돌 질의를 interface로 추상화해 EditMode에서 Unity scene 없이 fake world로 접지 판정 전체를 검증. Physics2D 어댑터는 query 호출만 수행.
- 접지 3게이트가 잠금 계약과 1:1 대응: probe distance 0.08, upward normal(벽 normal 배제, MinimumUpwardNormalY 0.5), 상승 속도 게이트 0.05(경계값 포함 — vy=0.05는 grounded).
- 모터는 MoveTowards 기반이라 큰 deltaTime에도 목표 속도 overshoot이 구조적으로 불가능. 감속은 0으로만 접근(부호 반전 없음).
- 금지 개념 부재를 reflection 테스트로 고정: Movement namespace에 Jump/Gravity/AirControl/Landing 타입·공개 멤버 없음, 전체 런타임에 WallJump/Dash/DoubleJump/Attack/Melee 없음, ActionId 5개 유지.

## TEST

실행: Unity MCP `run_tests`(EditMode, assembly=Game.Character.Tests.EditMode 전체, job `2b841d36…`)

| 항목 | 값 |
|---|---|
| 전체/성공/실패/스킵 | 24 / 24 / 0 / 0 (resultState=Passed, 0.97s) |
| CHAR01_02 필수 12개 | 전부 Passed |
| CHAR01_01 회귀 12개 | 전부 Passed |

CHAR01_02 필수 12개: GroundProbe_UsesLockedCapsuleSize, GroundProbe_ReturnsGroundedForValidDownwardHit, GroundProbe_RejectsMissTooFarWallNormalAndRisingVelocity, GroundProbe_DoesNotRequireMapOrTilemapTypes, GroundMotor_AcceleratesTowardWalkSpeed, GroundMotor_AcceleratesTowardRunSpeed, GroundMotor_DeceleratesTowardZeroWithoutInput, GroundMotor_ClampsHorizontalIntentAndPreventsOvershoot, GroundMotor_PreservesVerticalVelocity, GroundMotor_DoesNotMoveWhenAirborne, MovementRuntime_DoesNotDeclareJumpGravityAirControlOrLandingTypes, MovementRuntime_DoesNotDeclareForbiddenMovementFeatures

## UNITY

- Unity Version: 6000.3.8f1
- Asset Refresh: PASS (force + compile → isCompiling=False, isUpdating=False)
- Compile Errors: 0 (CS 오류 0건. Console의 `MCP-FOR-UNITY: Client handler error: Cannot access a disposed object` 1건은 MCP 브리지 자체의 도메인 리로드 재연결 로그이며 프로젝트 코드 오류가 아님 — 기존 로그 삭제 없이 기록)
- Relevant New Warnings: 0 (테스트 러너 표준 IPrebuildSetup/IPostBuildCleanup 로그만 존재)
- Targeted EditMode Tests: PASS 24/24
- PlayMode Tests: NOT RUN (Task 지정)
- Scene/Prefab Changes: 0

## SCOPE VALIDATION

- 작업 후 `git status -- Assets`: `Assets/_Game/Character/**` + `Assets/_Game/Tests/EditMode/Character/**`(+.meta) 외 변경 0.
- 신규 파일 = WRITE ALLOWLIST의 Movement 10 + 테스트 3 정확히 일치. State 2개 파일은 미수정.
- Packages/MapDesign 0, ProjectSettings 기존 사용자 변경 2건 외 0.
- inputactions/asmdef/Scene/Prefab/Animator/ScriptableObject 변경 0. CHAR01_03(점프·공중·착지) 선행 구현 없음.

## OUT_OF_SCOPE_FINDINGS

- 기존 관찰 유지: stale Map PlayMode asmdef 레거시 참조.
- MCP-for-Unity 브리지의 "disposed object" 오류 로그 1건(도메인 리로드 시 클라이언트 핸들러 노이즈, 프로젝트 코드와 무관).

## DONE CONDITIONS

- [x] CHAR01_01 PASS·finalize 상태 검증(REPORT sha `092ddca2…`, Current Task NONE)
- [x] 승인된 Runtime/Movement 경로와 정확한 테스트 파일만 추가
- [x] Collision query 추상화 + Unity Physics2D 어댑터 구현
- [x] Ground probe가 잠금 캡슐 기준선·probe distance·vertical velocity gate 사용
- [x] Ground motor: walk/run 가속, 감속, facing 갱신, vertical 보존
- [x] 공중 상태에서 지상 수평 가속 미적용
- [x] jump/gravity/air control/landing 미구현
- [x] 일반 공격·wall jump·dash·double jump 관련 없음(reflection 테스트 고정)
- [x] MAP/Tilemap/Scene/Prefab/inputactions/asmdef/Packages/ProjectSettings 변경 0
- [x] CHAR01_02 필수 12개 전부 PASS
- [x] CHAR01_01 기존 12개 전부 PASS
- [x] compile error 0, relevant new warning 0
- [x] 하네스 상태 파일 미수정
- [x] CHAR01_03 미시작

## NEXT

- Current Task after finalize: NONE
- Next Task auto-opened: NO (`CHAR01_03_IMPLEMENT_JUMP_AIR_CONTROL_AND_LANDING`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
