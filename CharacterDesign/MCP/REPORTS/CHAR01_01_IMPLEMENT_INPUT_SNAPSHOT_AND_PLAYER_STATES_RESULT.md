# TASK RESULT

TASK: CHAR01_01_IMPLEMENT_INPUT_SNAPSHOT_AND_PLAYER_STATES
STATUS: PASS

## SUMMARY

캐릭터 첫 활성 런타임(Game.Character.Runtime)을 승인된 경로에 생성했다. 논리 입력 스냅샷, 입력 버퍼, 입력 잠금 reason set, 플레이어 상태 스냅샷을 순수 C#으로 구현하고 지정 EditMode 테스트 12/12 PASS를 확인했다. Rigidbody2D 모터, 충돌 질의, 점프 물리, MAP 연동, inputactions 자산은 만들지 않았다.

## READ

- Mandatory Read Order 18개 문서(MCP 00~08, 상태 파일, 본 TASK, CHAR00_03 REPORT, registry, 입력/이동/테스트 규칙, action/tuning 스키마, MOVEMENT_COURSE_SPEC)
- convention 확인: `Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef`(전체 JSON), `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/CoordinateConversionBoundaryTests.cs`, `WorldGenConstantsTests.cs`(Assert 스타일) — NUnit 파일 본문 2개(≤4 제한 준수)
- 제한적 검색: `Assets/_Game/Character/**`, `Assets/_Game/Tests/EditMode/Character/**` 부재 확인(create 모드 검증)

## CREATED

Runtime (`Assets/_Game/Character/Runtime/`, namespace `StarNight.Character.Input` / `StarNight.Character.State`):

- `Game.Character.Runtime.asmdef` — name/rootNamespace/references `[]`/autoReferenced true, Map runtime asmdef 형태 준수
- `Input/CharacterActionId.cs` — enum {Jump, Action, SafeDrop, Bomb, Rope} 정확히 5개, 금지 값 없음
- `Input/CharacterButtonSnapshot.cs` — readonly struct {PressedThisFrame, Held, ReleasedThisFrame, Consumed, Tick} + Idle/Pressed/Released 팩토리
- `Input/CharacterInputSnapshot.cs` — readonly struct: Horizontal([-1,1] 클램프), DownHeld, Jump/Action/Bomb/Rope 버튼, SafeDropPressedThisFrame(Down+Action), PlainActionPressedThisFrame(SafeDrop 우선), IsPressedThisFrame(id)
- `Input/CharacterInputBuffer.cs` — 렌더 프레임 press를 물리 틱까지 보존, action별 window, 같은 틱 중복 소비 금지, 만료 미반환, Down+Action press는 SafeDrop으로만 기록(같은 source의 Action 중복 소비 원천 차단)
- `Input/CharacterInputLockSet.cs` — reason set(HashSet, Ordinal), Add/Remove/Contains/Clear, 부분 제거 시 잠금 유지
- `State/CharacterFacingDirection.cs` — enum {Left, Right}
- `State/CharacterLocomotionState.cs` — enum {Grounded, Airborne}
- `State/CharacterPlayerState.cs` — facing(0 입력 유지)/locomotion/carry/stun/dead/locks 추적, CanAcceptInput=!dead&&!stunned&&!locked, SetCameraRoomTransitionActive는 lock reason을 추가하지 않음, CreateSnapshot(tick)
- `State/CharacterPlayerStateSnapshot.cs` — readonly struct 값 객체(Animator/사운드/렌더 비의존)

EditMode Tests (`Assets/_Game/Tests/EditMode/Character/`, namespace `StarNight.Character.Tests`):

- `Game.Character.Tests.EditMode.asmdef` — references [Game.Character.Runtime, UnityEditor.TestRunner, UnityEngine.TestRunner], Editor 플랫폼, UNITY_INCLUDE_TESTS. stale `Game.Stage.Runtime` 참조 없음
- `CharacterInputSnapshotTests.cs`, `CharacterInputBufferTests.cs`, `CharacterPlayerStateTests.cs` — 필수 12개 test case 정확한 이름으로 구현

Unity 생성 `.meta` (WRITE ALLOWLIST 동일 취급, 기록): 폴더 5개(`Character`, `Character/Runtime`, `Runtime/Input`, `Runtime/State`, `Tests/EditMode/Character`) + 파일 14개(.asmdef 2, .cs 12) = 19개

Report:

- `CharacterDesign/MCP/REPORTS/CHAR01_01_IMPLEMENT_INPUT_SNAPSHOT_AND_PLAYER_STATES_RESULT.md` (본 파일)

## CHANGED

- 기존 파일 수정 0. `06_IMPLEMENTATION_STATUS.md`·`MASTER_IMPLEMENTATION_TASK_LIST.md`는 Task execution 중 미수정.

## IMPLEMENTATION

- 순수 C#만 사용 — `UnityEngine`/`UnityEngine.InputSystem` compile-time 의존 0(런타임 파일에 using UnityEngine 없음). 시간은 double 인자, 틱은 long 인자로 주입해 테스트 가능.
- SafeDrop 우선순위: 스냅샷 레벨에서 Down+Action이면 PlainAction=false(같은 틱 SafeDrop 우선), 버퍼 레벨에서 해당 press는 SafeDrop으로만 기록되어 같은 물리적 press의 Action 중복 소비가 구조적으로 불가능.
- 입력 잠금: reason set — 부분 제거 시 잠금 유지, 전부 제거 시 해제. 카메라룸 전환은 별도 플래그로만 추적하고 CanAcceptInput에 불개입(전환 중 입력 KEEP 계약).
- 금지 요소 부재: 일반 공격/wall jump/dash/double jump 관련 타입·상태·enum 값 없음(테스트로 고정).

## TEST

실행 명령: Unity MCP `run_tests` (mode=EditMode, assembly_names=Game.Character.Tests.EditMode, job `216f78d6…`)

| 항목 | 값 |
|---|---|
| 전체/성공/실패/스킵 | 12 / 12 / 0 / 0 (resultState=Passed, 0.45s) |

필수 12개 개별 결과 (전부 Passed):

1. CharacterActionId_DoesNotContainBasicAttack
2. Snapshot_ReportsSafeDropWhenDownAndActionPressed
3. Snapshot_PrioritizesSafeDropOverPlainAction
4. Snapshot_KeepsJumpBombAndRopeAsSeparateActions
5. PressedAction_SurvivesUntilFirstPhysicsTick
6. ConsumedAction_IsNotReturnedTwiceInSameTick
7. ExpiredAction_IsNotReturned
8. SafeDropConsumption_DoesNotAlsoReturnPlainAction
9. InputLocks_AreReasonSetAndClearIndependently
10. CameraRoomTransition_DoesNotCreateInputLock
11. StateSnapshot_TracksFacingLocomotionCarryStunAndDeath
12. DeadOrStunnedState_CannotAcceptInput

## UNITY

- Unity Version: 6000.3.8f1
- Asset Refresh: PASS (`refresh_unity` force + compile request → isCompiling=False, isUpdating=False)
- Compile Errors: 0
- Relevant New Warnings: 0 (Warning 필터 조회 0건)
- Targeted EditMode Tests: PASS (12/12, Game.Character.Tests.EditMode만 실행)
- PlayMode Tests: NOT RUN (Task 지정)
- Scene/Prefab Changes: 0

## SCOPE VALIDATION

- 작업 전 변경 파일 기록 완료(CharacterDesign 하네스 문서 churn + ProjectSettings 기존 사용자 변경 2건 — 미접촉).
- 작업 후 `git status -- Assets`: 신규는 `Assets/_Game/Character/**`와 `Assets/_Game/Tests/EditMode/Character/**` + 대응 `.meta`뿐. WRITE ALLOWLIST 외 파일 0.
- Packages/MapDesign 변경 0, ProjectSettings 기존 2건 외 0.
- `.inputactions`/Rigidbody2D/Collider2D/Physics2D/Tilemap/Animator/Scene/Prefab 변경 0.
- CHAR01_02(충돌 질의·모터) 선행 구현 없음.

## OUT_OF_SCOPE_FINDINGS

- 기존 관찰 유지: `Assets/_Game/Tests/PlayMode/Map/Game.Map.Tests.PlayMode.asmdef`의 stale `Game.Stage.Runtime` 참조(미수정).

## DONE CONDITIONS

- [x] CHAR00_03 PASS와 CHAR01_01 진입 승인 검증(`CHAR00 EXIT: APPROVED`, `CHAR01_01 ENTRY: ELIGIBLE FOR SEPARATE PATCH`)
- [x] 승인된 Character runtime/test 경로와 정확한 두 asmdef만 개방
- [x] Required Input Types 구현
- [x] Required State Types 구현
- [x] SafeDrop 우선순위·action 소비 중복 방지 테스트
- [x] 입력 lock reason set 동작·카메라룸 전환 비잠금 테스트
- [x] 일반 공격·wall jump·dash·double jump 관련 action/state 없음
- [x] inputactions/Rigidbody2D/Collider2D/Physics2D/Tilemap/Animator/Scene/Prefab 변경 없음
- [x] 지정 12개 EditMode test case 전부 PASS
- [x] Unity compile error 0, relevant new warning 0
- [x] REPORT 외 하네스 상태 파일 미수정
- [x] CHAR01_02 미시작

## NEXT

- Current Task after finalize: NONE
- Next Task auto-opened: NO (`CHAR01_02_IMPLEMENT_COLLISION_QUERIES_AND_GROUND_MOTOR`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
