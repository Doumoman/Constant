# TASK RESULT

TASK: CHAR02_01_VALIDATE_TWO_CELL_HEIGHT_AND_GAP_RULES
STATUS: PASS

## SUMMARY

CHAR01 이동 코어를 test-only 코스 시뮬레이터(fake 지형 collision world)에 조립해 잠금 이동 문법 2건을 검증했다: 기본 점프 1회로 2셀 높이 발판 도달, 달리기 기반 이동으로 동일 높이 2셀 틈 통과. EditMode 44/44 PASS(기존 36 + 신규 8). 3셀 실패·금지 이동 재검증은 CHAR02_02 소관으로 구현하지 않았다.

## READ

- Mandatory Read Order 19개 문서
- CHAR01 movement runtime API(작성 이력 기반), 기존 Character EditMode convention

## CREATED

Test-only (`Assets/_Game/Tests/EditMode/Character/MovementCourses/`, namespace `StarNight.Character.Tests.MovementCourses`, 5개):

- `CharacterMovementCourseConstants.cs` — 검증 규약 상수: `1 cell = 1 world unit`(fixture 기록, MAP 소스 아님), dt 1/60, 허용 오차 ±0.05, 2셀 높이/틈 폭
- `CharacterMovementCourseResult.cs` — peak bottom(값·시각), final x/bottom/grounded, 감시 구간 최저 bottom, jump 입력/시작 횟수, 틱·경과 시간
- `CharacterMovementCourseSimulator.cs` — CHAR01 코어 7종(GroundProbe+fake world, GroundMotor, AirControlMotor, GravityMotor, JumpController/State, LandingDetector) 조립 고정 틱 시뮬레이션. 바닥 세그먼트 fake collision world(하향 캡슐 캐스트), 하강 관통 스냅, 감시 구간 최저 bottom 기록, 조기 착지 종료. UnityEngine.Object 파생 필드 0(순수 C#)
- `TwoCellHeightCourseTests.cs` — 필수 4개
- `TwoCellGapCourseTests.cs` — 필수 4개

Unity 생성 `.meta`: `MovementCourses` 폴더 1 + .cs 5 = 6개(허용 범위, 기록)

Report: 본 파일

## CHANGED

- 기존 파일 수정 0 — Runtime 무변경, 기존 Character 테스트 9개 파일 무수정, asmdef 무수정, 상태 파일 무수정

## IMPLEMENTATION

- 시뮬레이터는 궤적을 하드코딩하지 않고 CHAR01 코어의 실제 Step/Probe/TryStartJump 호출로만 상태를 진행한다(가변 release 포함). 이를 `TwoCellGapCourse_UsesCharacterMovementCoreNotHardcodedTrajectory`가 행위로 고정: jumpVelocity를 2.0으로 약화하면 같은 코스가 실패한다(정점 저하 + 미착지)
- 지형은 test-only 바닥 세그먼트 목록이며 MAP 좌표·셀 크기 상수를 복제하지 않는다(1u/cell은 fixture 검증 규약으로만 기록)
- 입력은 틱 결정적 스크립트(컨텍스트 기반) — 동일 코스 2회 실행 시 float 단위 동일 결과

## COURSE RESULTS

측정 기준: 고정 틱 1/60s, 캡슐 0.72×0.90(runtime Default), 튜닝 Default(jumpVelocity 10.28, riseGravity 24, fallGravity 30, maxFall 18, runSpeed 3.75, coyote 0.10, buffer 0.12)

```text
[two_cell_height_jump_course]
- 사용 튜닝: jumpVelocity=10.28, riseGravity=24 (release 미적용, Jump 유지)
- 최고 높이(collider bottom): 2.1167 world unit >= 2.0 목표 (이산 틱 기준)
- 최고 높이 도달 시간: ~0.417s, 총 체공: ~0.800s
- Jump 입력 1회 / 점프 시작 1회, 종료 시 착지 복귀(bottom 0 ± 0.05)

[two_cell_same_level_gap_run_course]
- 코스: 시작 플랫폼 [-8,0], 틈 (0,2) 정확히 2.0u, 도착 플랫폼 [2,10], 동일 높이
- 조주 x=-4에서 달리기(runSpeed 3.75) 가속, x≈-0.35에서 단일 점프
- 통과: 틈 구간 위 collider bottom 최저값 >= -0.05 (플랫폼 높이 이상 유지)
- 착지: FinalGrounded=true, FinalX >= 2.6 (수평 2.94u를 0.784s에 통과 < 체공 0.800s)
- 결정성: 2회 실행 결과 완전 동일(FinalX/Peak/MinBottom/Ticks)
```

## TEST

실행: Unity MCP `run_tests`(EditMode, assembly=Game.Character.Tests.EditMode 전체, job `6c08a58c…`)

| 항목 | 값 |
|---|---|
| 전체/성공/실패/스킵 | 44 / 44 / 0 / 0 (resultState=Passed, 1.65s) |
| CHAR02_01 필수 8개 | 전부 Passed |
| 기존 CHAR01 36개 | 전부 Passed |

CHAR02_01 필수 8개: TwoCellHeightCourse_UsesOneWorldUnitCellsAndLockedCapsule, TwoCellHeightCourse_BasicJumpReachesTwoCellPlatformHeight, TwoCellHeightCourse_UsesSingleJumpInputOnly, TwoCellHeightCourse_DoesNotRequireSceneOrTilemap, TwoCellGapCourse_RunSpeedClearsSameLevelTwoCellGap, TwoCellGapCourse_RecordsDeterministicFrameTolerance, TwoCellGapCourse_UsesCharacterMovementCoreNotHardcodedTrajectory, TwoCellGapCourse_DoesNotValidateThreeCellFailureYet

## UNITY

- Unity Version: 6000.3.8f1
- Asset Refresh: PASS (force + compile → 완료)
- Compile Errors: 0 (CS 필터 0건)
- Relevant New Warnings: 0
- Targeted EditMode Tests: PASS 44/44 (최소 44 충족)
- PlayMode Tests: NOT RUN (Task 지정)
- Scene/Prefab Changes: 0

## SCOPE VALIDATION

- `git status -- Assets`: Character 트리 외 변경 0. 신규는 `MovementCourses/` 5개 + .meta 정확히 WRITE ALLOWLIST 일치
- Runtime/asmdef/inputactions/Scene/Prefab/Animator/ScriptableObject 변경 0
- Packages/MapDesign 0, ProjectSettings 기존 사용자 변경 2건 외 0
- 3셀 실패·wall jump·dash·double jump 검증 미구현(ThreeCell 타입 부재를 테스트로 가드). CHAR02_02 미시작

## OUT_OF_SCOPE_FINDINGS

- 기존 관찰 유지: stale Map PlayMode asmdef 레거시 참조

## DONE CONDITIONS

- [x] CHAR01_04 PASS·CHAR02_01 진입 승인 검증(REPORT sha `e9abb9a3…`, required_text 3건)
- [x] test-only MovementCourses 파일만 추가
- [x] 1 world unit cell + 0.72×0.90 캡슐 baseline 검증(runtime Default에서 읽음)
- [x] 2셀 높이 기본 점프 도달 검증(peak 2.1167 ≥ 2.0)
- [x] 동일 높이 2셀 틈 통과 검증(bottom ≥ -0.05 유지 + 착지)
- [x] 시뮬레이션이 CHAR01 코어 사용(약화 튜닝 실패로 행위 증명) — 하드코딩 궤적 아님
- [x] 3셀 실패·wall jump·dash·double jump 검증 미구현
- [x] Runtime/asmdef/inputactions/Scene/Prefab/MAP/Packages/ProjectSettings 변경 0
- [x] 기존 36 + 신규 8 = 44개 전부 PASS
- [x] compile error 0, relevant new warning 0
- [x] 하네스 상태 파일 미수정
- [x] CHAR02_02 미시작

## NEXT

- Current Task after finalize: NONE
- Next Task auto-opened: NO (`CHAR02_02_VALIDATE_THREE_CELL_FAILURE_AND_FORBIDDEN_MOVEMENT`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
