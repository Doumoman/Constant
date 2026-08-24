# TASK RESULT

TASK: CHAR02_02_VALIDATE_THREE_CELL_FAILURE_AND_FORBIDDEN_MOVEMENT
STATUS: PASS

## SUMMARY

동일 높이 3셀 틈이 기본 run + single jump(2셀 코스와 동일 코어 경로·동일 입력)로 통과 불가함을 검증하고, wall jump/dash/double jump/일반 공격의 부재를 런타임 전수 스캔과 행위 테스트로 고정했다. 검증 과정에서 시뮬레이터 지지 모델을 캡슐 접점(중심 직하) 기준으로 정밀화했다 — 발끝 걸침(span) 지지 모델에서는 3셀 규칙 자체가 깨짐을 사전 검산으로 확인했기 때문이다. EditMode 52/52 PASS(기존 44 + 신규 8). 코요테 지연 점프로 3셀이 뚫릴 수 있는 튜닝 여유 부족을 감사 자료로 기록했다.

## READ

- Mandatory Read Order 20개 문서
- CHAR02_01 MovementCourses support 3개 파일(수정 대상), 기존 코스 테스트 convention

## CREATED

- `Assets/_Game/Tests/EditMode/Character/MovementCourses/ThreeCellGapFailureCourseTests.cs` — 필수 4개 test case. 클래스명은 `GapFailureCourseTests`: CHAR02_01의 가드 테스트(`TwoCellGapCourse_DoesNotValidateThreeCellFailureYet`)가 어셈블리 타입명 기준으로 "ThreeCell" 부재를 단언하고 본 Task WRITE ALLOWLIST 밖이라 갱신 불가하므로, 지정된 파일명·메서드명은 그대로 두고 클래스명만 조정(파일 주석으로 사유 명시)
- `Assets/_Game/Tests/EditMode/Character/MovementCourses/ForbiddenMovementRuleTests.cs` — 필수 4개 test case
- Unity 생성 `.meta` 2개(허용 범위, 기록)
- Report: 본 파일

## CHANGED

- `CharacterMovementCourseConstants.cs` — `ThreeCellGapWidth = 3f` 상수 추가(최소 수정)
- `CharacterMovementCourseSimulator.cs` — 지지 판정을 span 겹침에서 **캡슐 바닥 접점(center x) 기준**으로 정밀화(최소 수정). 근거: 캡슐 곡면 바닥의 최저 접점은 중심 직하이며, 모서리 발끝 걸침을 지지로 인정하면 유효 도달 폭이 틈 폭 + 캡슐 폭(0.72)이 되어 3셀 규칙이 기하적으로 성립 불가(사전 검산: span 모델에서 3셀 통과 x=3.42). 이 변경으로 2셀/높이 코스의 기존 결과 값은 완전 동일하게 유지됨(두 모델에서 동일 궤적 — 회귀 44개 전부 PASS로 확인)
- `CharacterMovementCourseResult.cs` — 수정 불필요(실패 reason은 테스트 측 결정적 분류기로 도출)
- Runtime/asmdef/기존 TwoCell 테스트/상태 파일 — 수정 0

## IMPLEMENTATION

- 3셀 코스는 2셀 코스와 동일한 시뮬레이터 구성·동일 입력 스크립트(달리기 조주 + 접지 상태 단일 점프 + 유지)를 공유하고 지형 폭만 3.0u로 다르다 — `ThreeCellGapCourse_UsesSameCorePathAsTwoCellGapCourse`가 같은 경로로 2셀 통과/3셀 실패를 한 테스트에서 증명
- 실패 reason 분류기(결정적): `fell_below_gap_before_opposite_edge` / `landed_short_of_opposite_edge` / `did_not_land_on_target_platform` / `cleared`
- 금지 이동은 런타임 어셈블리 전수 reflection(타입+공개 멤버) + ActionId 5개 고정 + 공중 2단 점프 불가 행위 재검증으로 고정

## COURSE RESULTS

```text
[three_cell_same_level_gap_basic_movement_failure_course]
- 코스: 시작 [-8,0], 틈 (0,3) 정확히 3.0u, 도착 [3,12], 동일 높이
- 입력: 2셀 코스와 동일(기본 run 3.75 + 접지 단일 점프 + 유지)
- 결과: 통과 실패 — 이륙 x≈-0.3, 비행 도달 3.0u로는 반대편 접점(center ≥ 3.0)에 미달,
  틈 위 bottom이 -0.05 미만으로 낙하(fell_below_gap_before_opposite_edge)
- FinalGrounded=false, MinBottomOverWatch < -0.05, JumpStarts=1, 2회 실행 완전 동일
- 실패 margin 주의: 이륙점을 모서리 끝(x≈0)까지 늦춰도 착지 x≈2.94~3.0 < 3.0으로 실패하나
  여유가 0~0.06u에 불과함(이산 틱 의존)

[margin/exploit 기록 — 감사 자료]
- 코요테 지연 점프(모서리 이탈 후 0.03~0.10s 공중 점프, 기본 이동 범위 내 합법 입력)의
  이산 검산 결과 착지 x≈3.06~3.31 ≥ 3.0 — 현행 튜닝(jumpVelocity 10.28, rise 24,
  run 3.75, coyote 0.10)에서 3셀 규칙이 뚫릴 수 있음
- 본 Task의 지정 검증(2셀 동일 경로)은 PASS이나, 잠금 규칙의 완전한 성립을 위해서는
  CHAR02_03 감사에서 튜닝 여유(예: jumpVelocity/coyote 하향 또는 3셀 실패 adversarial
  코스 추가)의 CHANGE CONTROL 검토가 필요함
```

## FORBIDDEN MOVEMENT SCAN

- WallJump / Dash / DoubleJump: 런타임 타입·공개 멤버 0건
- Attack / BasicAttack / Melee / Shoot: 런타임 타입·공개 멤버 0건, ActionId에 없음
- CharacterActionId = {Jump, Action, SafeDrop, Bomb, Rope} 정확히 5개(EquivalentTo 고정)
- 공중 2단 점프: 코요테 창 내부/외부 재입력 모두 거부, grounded 재획득 후에만 신규 점프(행위 테스트)

## TEST

실행: Unity MCP `run_tests`(EditMode, assembly=Game.Character.Tests.EditMode 전체, job `accf3dbe…`)

| 항목 | 값 |
|---|---|
| 전체/성공/실패/스킵 | 52 / 52 / 0 / 0 (resultState=Passed, 1.89s) |
| CHAR02_02 필수 8개 | 전부 Passed |
| 기존 CHAR01 36개 + CHAR02_01 8개 | 전부 Passed(2셀 결과 완화 없음) |

CHAR02_02 필수 8개: ThreeCellGapCourse_BasicMovementDoesNotClearSameLevelThreeCellGap, ThreeCellGapCourse_UsesSameCorePathAsTwoCellGapCourse, ThreeCellGapCourse_RecordsDeterministicFailureReason, ThreeCellGapCourse_DoesNotChangeTwoCellPassResult, ForbiddenMovement_NoWallJumpDashOrDoubleJumpTypesOrMembers, ForbiddenMovement_NoBasicAttackMeleeOrShootActions, ForbiddenMovement_CharacterActionIdRemainsLockedToFiveValues, ForbiddenMovement_SecondJumpStillFailsBeforeGroundedAgain

## UNITY

- Unity Version: 6000.3.8f1
- Asset Refresh: PASS
- Compile Errors: 0 (CS 필터 0건)
- Relevant New Warnings: 0
- Targeted EditMode Tests: PASS 52/52 (최소 52 충족)
- PlayMode Tests: NOT RUN (Task 지정)
- Scene/Prefab Changes: 0

## SCOPE VALIDATION

- `git status -- Assets`: Character 트리 외 변경 0. 수정 = 허용된 support 2개(Constants/Simulator), 신규 = 지정 테스트 파일 2개 + .meta
- Runtime/asmdef/inputactions/Scene/Prefab 변경 0. 기존 TwoCell 테스트 파일 무수정(결과 완화 없음 — 동일 값 PASS)
- Packages/MapDesign 0, ProjectSettings 기존 사용자 변경 2건 외 0. CHAR02_03 미시작

## OUT_OF_SCOPE_FINDINGS

1. **코요테 지연 점프의 3셀 통과 가능성**(상세는 COURSE RESULTS) — 현행 튜닝 여유 부족. CHAR02_03 감사에서 CHANGE CONTROL 검토 권고.
2. CHAR02_01 가드 테스트가 타입명 기준이라 본 Task 클래스명을 `GapFailureCourseTests`로 조정함 — CHAR02_03 감사에서 가드 문구를 시점 명시형으로 갱신 권고.
3. 기존 관찰 유지: stale Map PlayMode asmdef 레거시 참조.

## DONE CONDITIONS

- [x] CHAR02_01 PASS·진입 상태 검증(REPORT sha `71154757…`, required_text 3건)
- [x] test-only MovementCourses 파일만 추가·최소 수정
- [x] 동일 높이 3셀 틈 기본 이동 실패 검증
- [x] 실패 reason + deterministic result 기록
- [x] 2셀 높이/2셀 틈 기존 결과 유지(44개 회귀 PASS)
- [x] wall jump/dash/double jump 관련 없음
- [x] basic attack/melee/shoot 관련 없음
- [x] Runtime/asmdef/inputactions/Scene/Prefab/MAP/Packages/ProjectSettings 변경 0
- [x] 기존 44 + 신규 8 = 52개 전부 PASS
- [x] compile error 0, relevant new warning 0
- [x] 하네스 상태 파일 미수정
- [x] CHAR02_03 미시작

## NEXT

- Current Task after finalize: NONE
- Next Task auto-opened: NO (`CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
