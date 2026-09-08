# RMAP04_CLIMB Result

```text
TASK: RMAP04_CLIMB
STATUS: PASS
```

## USER-FACING IMPLEMENTATION REPORT

`Assets/_Game/Map/Scenes/MoonPalace/RMAP04/MoonPalaceClimbOneWay_RMAP04.unity`를 열어 Play하면 실제 `CharacterLivePlayer`가 사다리와 기둥 trigger에 겹친 상태에서만 `W`/Up 또는 `S`/Down으로 등반한다. 단순 접촉은 등반을 시작하지 않는다. 기본 Up은 빠르고 Shift+Up은 느리며 Down은 빠르게 하강한다. `A`/`D`+Space는 기존 jump/air-control로 이탈한다. 일방향 발판은 아래에서 위로 통과하고 상단에 착지하며, `S`/Down+Space는 선택된 발판 하나만 0.18초 drop-through 한다. 이 발판은 계속 Grab 금지다.

## RESPONSIBILITY AND FILES

| 실제 경로 | 판단 | RMAP04 책임 | 소유하지 않는 책임 |
| --- | --- | --- | --- |
| `Assets/_Game/Character/Runtime/Input/CharacterInputSnapshot.cs` | ADAPT | fixed snapshot에 Up/Shift held 값을 추가 | 새 `CharacterActionId`/기존 action 우선순위 변경 |
| `Assets/_Game/Character/Runtime/Movement/UnityPhysics2DCharacterCollisionWorld.cs` | ADAPT | trigger를 physical solid cast에서 제외 | damage/one-way gameplay 정책 |
| `Live/Input/CharacterLiveControls.inputactions`, `Live/Runtime/Input/CharacterLiveInput{Source,Adapter}.cs` | ADAPT | W/Up, Shift, Down을 Input System→snapshot으로 전달 | legacy polling |
| `Live/Runtime/Movement/CharacterLiveMovementSettings.cs` | ADAPT | climb 4, slow 2, down 5, reentry 0.12s, drop 0.18s 단일 소유 | RMAP02 run/walk/jump 수치 재튜닝 |
| `Live/Runtime/Movement/CharacterLiveMovementDriver.cs` | ADAPT | climb enter/hold/exit, trigger 제외 sweep, one-way top-only/drop filter | wall-kick, fall damage, death |
| `Live/Runtime/Movement/CharacterLiveClimbSurface.cs` | NEW | ladder/pole 공통 trigger axis 분류 | solid cap/전투/animation |
| `Live/Runtime/Movement/CharacterLiveOneWayPlatform.cs` | NEW | 선택된 물리 Collider의 제한적 drop-through 주소화 | moving platform/destruction |
| `Live/Editor/RMAP04/CharacterLiveClimbSceneBuilder.cs` 및 asmdef | NEW | RMAP04 저장 시험 씬·manifest 생성 | RMAP03/RMAP02 scene 변경 |
| `Map/Scenes/MoonPalace/RMAP04/MoonPalaceClimbOneWay_RMAP04.unity` | NEW | 실제 Player, trigger, PlatformEffector2D fixture | Full Run/world assembly |
| `Tests/*/Character/RMAP04/` | NEW | 실제 Player/Rigidbody2D/CapsuleCollider2D/Collider 검증 | broad regression |

## PRECONDITIONS

- 작업 전 HEAD/branch: `e01523557354e325670cb59504edb174eb90402a` / `main`.
- RMAP03 PASS Result SHA-256: `66293f22ce730ed9b8bdd62cc22818509fda44669372399ef91939bc16c045f9` (독립 `TASK: RMAP03_GRAB`, `STATUS: PASS` 확인).
- RMAP03 installed Task/Archive SHA-256: 모두 `87d6c6301c65708b749c2576e06fef2eea6a6b4331cd52ee954602e7db2a9cd0`; predecessor commit은 `e015235…`.
- Apply 전 상태: `224 COMPLETE / 0 CURRENT / 16 LOCKED`, RMAP04는 한 행의 `LOCKED`, RMAP05는 한 행의 `LOCKED`.
- Apply 후 상태: `224 COMPLETE / 1 CURRENT / 15 LOCKED`, Current=`RMAP04_CLIMB`; installed/archive는 현재 inbox bytes와 동일한 `d70088c60a5f8167ba0b6fb306cd20168010008f92db9ceb973977cfbff0b18b`.
- 본문에 기재된 planned SHA `f4ab…`는 실행 지시서의 참고값이며 current inbox bytes를 덮어쓰지 않았다. Apply metadata의 두 필수 predecessor SHA는 위 값과 정확히 일치한다.

## CHANGED

기존 Player prefab과 RMAP02 motor/jump/air-control, RMAP03 `CharacterLiveGrabSurface` 분류를 재사용했다. Up/Shift는 action ID를 늘리지 않는 snapshot held 값이다. 기본/Shift 이탈은 새 jump profile 대신 기존 `RunSpeed`/`WalkSpeed` 및 `JumpVelocity`를 사용하고, 이탈 뒤에는 기존 air-control이 계속 입력을 소비한다. RMAP03 `OneWay` 분류는 `IsGrabAllowed=false`로 유지한다.

## REQUIREMENT EVIDENCE

| ID | 실제 Player/Collider 산출물과 관찰 | 판정 |
| --- | --- | --- |
| P11 | 같은 `CharacterLiveClimbSurface` trigger가 ladder와 pole에 사용됨. 실제 Player는 vertical intent 없이는 미진입, W Input System→snapshot→motor로 진입. trigger는 sweep/ground support에서 제외되고 pole top에 solid cap이 없다. | PASS |
| P12 | 실제 Player fixed 10 steps에서 `dt=0.016667`: Up 4u/s=`0.667`, Shift Up 2u/s=`0.333`, Down 5u/s=`0.833`; settings 단일 소유와 ±0.05 tolerance를 검사. | PASS |
| P13 | 실제 좌우+Space 이탈: run 최대 높이 `1.237`, 수평 `3.656`; Shift 최대 높이 `1.237`, 수평 `1.856`. 즉시 Up 재진입 억제와 이후 반대 수평 input의 기존 air-control 반응을 검사. | PASS |
| P15 | 실제 BoxCollider2D+PlatformEffector2D one-way(PlayMode fixture top y=2.125)를 위로 통과해 상단 착지. Down+Jump는 그 collider만 0.18초 ignore하고 일반 ground collision은 유지·복원. outward corner에서 actual Player Grab 불가. | PASS |

## VALIDATION

Unity `6000.3.8f1` focused 실행만 수행했다. build, legacy 19347, full/unfiltered regression, Player build는 실행하지 않았다.

| 결과 파일 | filter | total / pass / fail |
| --- | --- | --- |
| `GENERATED/RMAP04/rmap04_editmode_results.xml` | `CharacterLiveClimbSurfaceTests` | 1 / 1 / 0 |
| `GENERATED/RMAP04/rmap04_playmode_results.xml` | `CharacterLiveClimbOneWayPlayModeTests` | 6 / 6 / 0 |
| `GENERATED/RMAP04/rmap03_direct_regression_results.xml` | PlayMode `RMAP03` | 7 / 7 / 0 |
| `GENERATED/RMAP04/rmap02_input_direct_regression_results.xml` | `CharacterLiveInputPlayModeTests` | 3 / 3 / 0 |

초기 focused run은 climb trigger가 ordinary sweep/ground probe에 들어가는 결함을 드러냈다. trigger를 physical collision world에서 제외한 최소 수정 후 위 최종 결과로 모두 PASS했다.

## UNITY VISIBLE OUTPUT

저장 씬의 actual Player는 0.4×0.8 foot-pivot capsule이다. Ladder trigger는 `(7, 3.5)`, Pole trigger는 `(14, 3.5)`, 각각 `0.7×5`이며 non-solid다. OneWay는 `(23, 4)`의 `5×0.25` BoxCollider2D+PlatformEffector2D이고 `CharacterLiveGrabSurface.OneWay` marker를 가진다. `rmap04_fixture_manifest.json`에 같은 좌표와 component 구성을 기록했다.

## OUT-OF-SCOPE FINDINGS

RMAP05 fall damage/stun/death/reset, RMAP06 look mode, RMAP07 이후 pattern/world generation, destructible/moving platform gameplay, Full Run은 시작하지 않았다.

## RMAP05 BINDINGS

EXISTING: `IsGroundedNow`, RMAP03의 `HasSafeGrabContact`/`LastSafeGrabAnchor`. RMAP04 ADDED: `IsClimbing`, `HasSafeClimbContact`, `LastSafeClimbAnchor`, `IsDroppingThroughOneWay`. PROPOSED: RMAP05가 이 마지막 안전 traversal contact와 grounded state를 reset/fall 판단에 소비할 수 있다. damage/stun/death/reset API 또는 구현은 추가하지 않았다.

## FOLLOWUP / FINAL EVIDENCE

single_task_v1은 metadata의 `TASKS/RMAP03_GRAB.md`/`REPORTS/RMAP03_GRAB_RESULT.md` 상대 경로와 lowercase SHA-256만 수용했다. 설치 Task와 Archive의 current RMAP04 bytes는 동일하다. 필수 P11/P12/P13/P15 미확인은 없다. Finalize는 RMAP04만 `CURRENT→COMPLETE`, Current만 `NONE`으로 바꾸며 RMAP05_FALL은 `LOCKED / NOT STARTED`여야 한다.

COMMIT: 작업 전 `e015235…`; 생성 commit SHA는 Phase D 완료 후 최종 CLI 검증에 기록한다. Push는 수행하지 않는다.
