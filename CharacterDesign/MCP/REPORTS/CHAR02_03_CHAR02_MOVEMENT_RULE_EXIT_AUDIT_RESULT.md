# TASK RESULT

TASK: CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT
STATUS: PASS

## SUMMARY

repair revision(CHANGE_CONTROL_REPAIR_AND_EXIT_AUDIT)에 따라 1차 감사의 차단 사유(코요테 지연 점프의 3셀 통과, x=3.171)를 최소 교정으로 해소하고 종료 감사를 재실행했다. 교정은 단일 값 — 공중 수평 상한 `maxAirSpeed` 3.75 → 3.1 (지상 runSpeed·점프 수직 거동 무변경). 교정 후 합법 코요테 지연 표본 전체(0.0~0.1s 스윕)가 3셀 통과에 실패하고, 2셀 높이·2셀 틈·금지 이동 요건이 전부 유지된다. EditMode 57/57 PASS(기존 52 + 교정 회귀 5). CHAR02 EXIT를 승인한다.

## READ

- Entry Gate 검증: 1차 FAIL REPORT sha `e5fac10b…` + "CHAR02 EXIT: REJECTED"/"CHAR03_01 ENTRY: BLOCKED" 문구, registry sha/marker, CHAR03_01 이후 LOCKED
- Mandatory Read Order 23개 항목(1차 FAIL REPORT, 이동 런타임·코스 테스트 코드 포함)

## CHANGED

- `Assets/_Game/Character/Runtime/Movement/CharacterAirControlSettings.cs` — Default `maxAirSpeed` 3.75f → **3.1f** + CHANGE CONTROL 근거 주석(유일한 런타임 변경, 허용 경로 내)
- `Assets/_Game/Tests/EditMode/Character/MovementCourses/ThreeCellGapFailureCourseTests.cs` — 코요테 지연 점프 스크립트 + 필수 테스트 2개 추가
- `Assets/_Game/Tests/EditMode/Character/MovementCourses/TwoCellGapCourseTests.cs` — 교정 후 2셀 통과 유지 테스트 1개 추가
- `Assets/_Game/Tests/EditMode/Character/MovementCourses/TwoCellHeightCourseTests.cs` — 교정 후 2셀 높이 유지 테스트 1개 추가
- `Assets/_Game/Tests/EditMode/Character/MovementCourses/ForbiddenMovementRuleTests.cs` — 교정 후 금지 이동 부재 유지 테스트 1개 추가

기존 테스트의 기대값 완화·Ignore·조건부 은폐 없음. 상태/마스터 파일 무수정.

## CREATED

- 본 REPORT(1차 FAIL REPORT를 지정대로 교체 — 이전 판은 repair manifest의 requires_result 해시로 이력 고정됨)

## TEST

실행: Unity MCP `run_tests`(EditMode, assembly=Game.Character.Tests.EditMode 전체, job `b261d84d…`)

| 항목 | 값 |
|---|---|
| 전체/성공/실패/스킵 | 57 / 57 / 0 / 0 (resultState=Passed, 1.92s — 최소 57 충족) |
| 기존 52개(CHAR01 36 + CHAR02_01 8 + CHAR02_02 8) | 전부 Passed |
| 교정 회귀 5개 | 전부 Passed |

요구 행위 ↔ 실제 테스트 이름 매핑(5/5):

1. coyote 지연 3셀 실패 잠금 → `ThreeCellGapCourse_CoyoteDelayedJumpDoesNotClearSameLevelThreeCellGap`
2. coyote 지연 스윕 전체 실패 → `ThreeCellGapCourse_CoyoteDelaySweepNeverClearsSameLevelThreeCellGap`
3. 교정 후 2셀 틈 통과 유지 → `TwoCellGapCourse_StillPassesAfterCoyoteRepair`
4. 교정 후 2셀 높이 유지 → `TwoCellHeightCourse_StillReachesTwoCellsAfterCoyoteRepair`
5. 교정 후 금지 이동 부재 유지 → `ForbiddenMovement_StillHasNoWallJumpDashDoubleJumpOrBasicAttack`

## UNITY

- Unity Version: 6000.3.8f1
- Asset Refresh: PASS, Compile Errors: 0 (CS 0건 — Console의 MCP 브리지 "disposed object" 재연결 로그 2건은 코드 무관, 기록만), Relevant New Warnings: 0
- EditMode: 57/57, PlayMode: NOT RUN, Scene/Prefab Changes: 0

## CHANGE_CONTROL

- 변경 전: `CharacterAirControlSettings.Default = (airAcceleration 22.5, maxAirSpeed 3.75)`
- 변경 후: `CharacterAirControlSettings.Default = (airAcceleration 22.5, maxAirSpeed 3.1)`
- 사유(증거 기반): 위반의 기하 원인은 "공중 수평 상한 = 지상 runSpeed(3.75)"로 체공(0.81s) 동안 3.0u를 비행 + 코요테 드리프트 최대 +0.375u → 유효 도달 3.17~3.37u ≥ 3셀. 공중 상한만 3.1로 캡하면 코요테 최대 활용 도달 ≈ 2.84u < 3.0(여유 ≈ 0.16u)이며 2셀 틈 착지 ≈ 2.23u ≥ 2.0(여유 ≈ 0.23u)
- 선택 근거: Category A 단일 노브 — 지상 이동 필(runSpeed)·수직 점프(2셀 높이)·코요테 관용(창 0.10 유지) 전부 무영향인 최소 교정. 코요테 제거/축소나 runSpeed 하향 대비 영향 범위 최소
- 영향 Task: CHAR02(본 교정), 이후 CHAR03+ 이동 관련 검증은 교정된 기준선 사용
- 회귀 테스트: 위 5개(스윕 포함)로 재발 방지 고정
- 승인 경로: 본 repair revision 패치(MCP_INBOX, FAIL REPORT 해시 게이트) 자체가 CHANGE CONTROL 승인 문서다

## MOVEMENT_GRAMMAR_COVERAGE

```text
Logical cell 1 world unit                : PASS (코스 규약 상수 + 검증)
Capsule 0.72 × 0.90                      : PASS (runtime Default 사용)
2-cell height reachable                  : PASS (peak 2.1167 ≥ 2.0 — 교정 무영향, 유지 테스트 추가)
Same-level 2-cell gap pass               : PASS (교정 후에도 착지·watch bottom ≥ -0.05, 유지 테스트 추가)
Same-level 3-cell gap fail (coyote 포함) : PASS — 기본 접지 점프 실패 + 코요테 지연 스윕
                                           0/1/2/3/4/5/6틱(0.0~0.1s) 전부 실패.
                                           창 만료 표본(0.1s 초과분)은 점프 미발동 명시 기록
Forbidden movement absent                : PASS (전수 스캔 + 유지 테스트)
Basic attack/melee/shoot absent          : PASS
```

## COYOTE_THREE_CELL_REPAIR

교정 전(1차 감사 실코어 증거) → 교정 후(동일 프로브 재실행):

```text
delay 0.033s: [전] 통과 x=3.171 착지  → [후] 실패 — 틈 낙하 (minWatch -1.829)
delay 0.067s: [전] 건너편 접지 성공    → [후] 실패 — 틈 낙하 (minWatch -1.375)
delay 0.083s: [전] 건너편 접지 성공    → [후] 실패 — 틈 낙하 (minWatch -1.173)
delay 0.100s: [전] 창 만료 미발동      → [후] 동일(점프 0회, 낙하)
```

재현→교정→재검증 절차 완료. 스윕은 EditMode 테스트로 영구 고정.

## COURSE_FIXTURE_DETERMINISM

- 1u 셀 규약·runtime 캡슐 사용·2/3셀 코스는 틈 폭만 상이·고정 틱 결정성·반복 실행 동일성 전부 유지
- 하드코딩 궤적/판정 없음(약화 튜닝 실패 테스트 + 이번 교정에서 튜닝 변경만으로 코스 결과가 실제로 뒤집힌 것 자체가 코어 유도의 증거)

## FORBIDDEN_FEATURE_SCAN

- WallJump/Dash/DoubleJump/Attack/BasicAttack/Melee/Shoot: 런타임 타입·공개 멤버 0건(유지 테스트 포함)
- CharacterActionId = 정확히 {Jump, Action, SafeDrop, Bomb, Rope}
- 단일 점프 소비·공중 재점프 불가 유지

## DEPENDENCY_LEDGER

```text
MAP world query / coordinate conversion    : DEFERRED (CHAR03_01 전 MAP 계약 승인 필요)
Room boundary detection and readiness gate : DEFERRED
Camera room transition policy              : DEFERRED (CHAR03_02 소관)
Terrain mutation request API               : DEFERRED
Generated map route integration            : DEFERRED (CHAR06 소관)
```

이동 문법이 교정되었으므로 위 지연 의존성은 CHAR02 EXIT를 차단하지 않는다.

## OUT_OF_SCOPE_FINDINGS

- CHAR02_01 가드 테스트 문구의 시점 명시형 갱신 권고(기존 기록 유지, 비차단)
- stale Map PlayMode asmdef 레거시 참조(기존 관찰 유지)
- 참고: 3셀 실패 여유는 ≈ 0.16u다. 이후 이동 튜닝 변경 시 코요테 스윕 테스트가 회귀 게이트로 동작한다

## CHAR02 EXIT

```text
CHAR02 EXIT: APPROVED
CHAR03_01 ENTRY: ELIGIBLE FOR SEPARATE PATCH
```

## DONE CONDITIONS

- [x] 1차 CHAR02_03 FAIL REPORT를 정확한 SHA·기각 문구로 검증
- [x] 교정 전 합법 코요테 지연 점프 위반 재현(1차 감사 증거 + 본 실행 전/후 대비)
- [x] 교정 전략을 CHANGE_CONTROL에 문서화
- [x] 2셀 높이 도달 유지
- [x] 2셀 틈 통과 유지
- [x] 3셀 틈이 일반·코요테/버퍼 타이밍 모두에서 실패
- [x] 코요테 지연 스윕 회귀 추가
- [x] 금지 이동 부재 유지
- [x] EditMode 57개(≥57) 전부 PASS
- [x] Unity compile error 0
- [x] 범위 검증 완료(런타임 변경은 Movement 1파일, 테스트 변경은 Character 테스트 4파일 한정)
- [x] 의존성 장부 완료
- [x] CHAR02 EXIT 판정을 정확한 지정 문구로 기록
- [x] CHAR03_01 ENTRY 판정을 정확한 지정 문구로 기록
- [x] 상태/마스터 무수정
- [x] CHAR03_01 LOCKED 유지

## NEXT

- Current Task after finalize: NONE
- Next Task auto-opened: NO (`CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
