# RMAP05_FALL Result

```text
TASK: RMAP05_FALL
STATUS: PASS
```

## USER-FACING IMPLEMENTATION REPORT

`Assets/_Game/Map/Scenes/MoonPalace/RMAP05/MoonPalaceFallDamage_RMAP05.unity`를 열어 Play하면 0.4×0.8 foot-pivot 실제 Player가 6타일 이상 착지에서 0.5초 경직, 10/15/20/25타일에서 1/2/3/4 피해, 30타일 이상에서 death 상태를 받는다. 최대 체력은 5다. 실제 safe corner Grab 또는 W/S·Up/Down climb 진입은 그 시점까지의 낙하 거리를 초기화한다. one-way top 착지는 `fallDistance -> damage/stun/death -> reset` 순서이며, Down+Jump selected-platform drop-through은 다른 ground collider를 무시하지 않는다.

## RESPONSIBILITY AND FILES

| 실제 경로 | 판단 | RMAP05 책임 | 소유하지 않는 책임 |
| --- | --- | --- | --- |
| `Assets/_Game/Live/Runtime/Movement/CharacterLiveMovementSettings.cs` | ADAPT | 연속 낙하 표, 최대 체력 5, 0.5초 경직의 단일 profile 소유 | RMAP02 run/walk/jump, RMAP04 climb/one-way 수치 재튜닝 |
| `Assets/_Game/Live/Runtime/Movement/CharacterLiveFallDamageState.cs` | NEW | 기존 Health/PlayerState를 소비하는 Player-local fall 결과, stun/death/input gate | 공용 전투 Health, 저장/부활/체크포인트 |
| `Assets/_Game/Live/Runtime/Movement/CharacterLiveMovementDriver.cs` | ADAPT | foot-pivot peak 추적, actual landing 순서, Grab/climb reset, locked snapshot 억제 | Grab/climb 판정 또는 one-way 0.18초 정책 변경 |
| `Assets/_Game/Live/Editor/RMAP05/CharacterLiveFallSceneBuilder.cs` 및 asmdef | NEW | RMAP05 저장 물리 씬·fixture manifest 생성 | 기존 RMAP02~04 씬/Prefab 변경 |
| `Assets/_Game/Map/Scenes/MoonPalace/RMAP05/MoonPalaceFallDamage_RMAP05.unity` | NEW | Player/Capsule, Tilemap ground, ladder/pole trigger, one-way, camera 관찰 fixture | generated world/Full Run |
| `Assets/_Game/Tests/{EditMode,PlayMode}/Character/RMAP05/` | NEW | 낙하 표 경계 및 actual Player/Rigidbody2D/CapsuleCollider2D/Collider evidence | broad/full regression |
| `MapDesign/MCP/GENERATED/RMAP05/` | NEW | fixture manifest와 focused NUnit 결과 | 다른 RMAP 증거 변경 |

## PRECONDITIONS

- 작업 전 HEAD/branch: `d1d0af643ce6efb8267bc8a4b5799e0599fc4bf7` / `main` (`RMAP04 implement climb axes and one-way platforms`).
- RMAP04 PASS Result SHA-256: `148150921891b00928f71a5b78c96ec693dbcdd44005fbbca27a5aae6ac257c3`; 독립 `TASK: RMAP04_CLIMB` / `STATUS: PASS`를 확인했다.
- RMAP04 installed Task/Archive SHA-256: 모두 `d70088c60a5f8167ba0b6fb306cd20168010008f92db9ceb973977cfbff0b18b`이며 byte-identical이다.
- Apply 전: 240 rows, `225 COMPLETE / 0 CURRENT / 15 LOCKED`, Current=`NONE`, RMAP05와 RMAP06은 각각 한 행의 `LOCKED`.
- 단일 RMAP05 inbox 후보를 `TASKS/RMAP05_FALL.md`에 byte-for-byte 설치하고 archive로 이동했다. installed/archive SHA-256은 모두 `056c65cdaf0040e99cb7423a43c9bb8579c4fb80735e89d202911c858db669f9`다. Apply 후 상태는 `225 COMPLETE / 1 CURRENT / 14 LOCKED`, Current=`RMAP05_FALL`이다.

## CHANGED / REUSE DECISIONS

- `CharacterHealthState`와 `CharacterHealthDamagePolicy`를 재사용해 damage/death request를 적용했다. 기존 중앙 `CharacterSurvivalSettings.Default`의 max 4는 변경하지 않고, RMAP05 movement profile의 max 5로 Player-local 상태를 초기화했다.
- `CharacterPlayerState.IsStunned/IsDead/CanAcceptInput`을 재사용해 Rigidbody2D 또는 Collider2D를 끄지 않는 fixed-step 입력 억제를 제공한다.
- RMAP03의 실제 `IsGrabbing` 전환과 RMAP04의 실제 `IsClimbing` 전환에서만 peak를 reset한다. 단순 climb trigger overlap은 reset하지 않는다.
- RMAP04 one-way marker와 selected collider ignore를 소비할 뿐, top-only collision과 0.18초 ignore 수치는 바꾸지 않았다.

## REQUIREMENT EVIDENCE

| ID | 실제 Player/Collider 산출물과 관찰 | 판정 |
| --- | --- | --- |
| P09 | 실제 0.4×0.8 Player가 safe BoxCollider2D corner를 Grab하기 전 `7.375`타일을 관측했고, Grab에서 health=5/stun 없음으로 reset했다. Down 이탈 뒤 다른 corner 재획득 없이 실제 ground에 착지한 새 낙하는 `7.375`타일이었다. | PASS |
| P14 | ladder/pole trigger 단순 접촉은 climb reset이 아니며, Up 입력으로 각각 실제 climb이 성립한 reset 값은 `8.141`타일이었다. 두 경우 health=5/stun 없음이다. | PASS |
| P16 | actual BoxCollider2D+PlatformEffector2D one-way top landing은 `15.870`타일, damage=2, sequence=`fallDistance -> damage/stun/death -> reset`을 기록했다. 별도 actual drop-through은 selected one-way만 ignore하고 unrelated ground collision 및 6+ 새 fall tracking을 유지했다. | PASS |
| P17 | EditMode가 5.999/6/9.999/10/14.999/15/20/25/29.999/30의 연속 경계를 검증했다. actual 31+ tile physical fall은 health=0/death 및 input 불가를 확인했다. | PASS |
| P18 | actual foot-pivot 6~10 fall은 damage=0과 stun을 만들고, 0.5초 fixed-step 동안 Input System→snapshot 수평 입력을 억제한 뒤 기존 motor를 재개했다. Rigidbody2D/CapsuleCollider2D와 CameraFollow는 계속 enabled/simulated다. | PASS |

## VALIDATION

Unity `6000.3.8f1` batch focused 실행만 수행했다. connected live Editor는 없었으므로 저장 Scene은 RMAP05 builder로 생성하고 batch PlayMode에서 실제 물리를 확인했다. broad build, legacy 19347, full/unfiltered regression, Player build는 실행하지 않았다.

| 결과 파일 | filter | total / pass / fail |
| --- | --- | --- |
| `GENERATED/RMAP05/rmap05_editmode_results.xml` | `CharacterLiveFallDamageStateTests` | 11 / 11 / 0 |
| `GENERATED/RMAP05/rmap05_playmode_results.xml` | `CharacterLiveFallDamagePlayModeTests` | 6 / 6 / 0 |
| `GENERATED/RMAP05/rmap02_direct_regression_results.xml` | `GeneratedTilemapPlayerRunPlayModeTests` | 5 / 5 / 0 |
| `GENERATED/RMAP05/rmap03_direct_regression_results.xml` | `CharacterLiveGrabPlayModeTests` | 7 / 7 / 0 |
| `GENERATED/RMAP05/rmap04_direct_regression_results.xml` | `CharacterLiveClimbOneWayPlayModeTests` | 6 / 6 / 0 |

초기 RMAP05 PlayMode는 safe wall에서 Down 이탈 뒤 기존 Grab reentry가 다시 성립해 P09의 새 착지를 만들지 못했다. 구현을 바꾸지 않고, fixture 입력을 실제 반대 수평 이탈로 제한해 동일 safe corner의 재획득을 피했다. 이후 final focused run은 위 결과로 모두 PASS다.

## UNITY VISIBLE OUTPUT

`rmap05_fixture_manifest.json`은 저장 씬, Player foot pivot, `(7,14)` ladder trigger, `(14,14)` pole trigger, `(23,10)` PlatformEffector2D one-way, max health 5와 table을 기록한다. scene saved-scene PlayMode는 실제 Player capsule, fall state, 두 trigger, one-way effector 및 camera를 load하여 검증했다.

## OUT-OF-SCOPE FINDINGS

RMAP06 Tab observation/input precedence, enemy/hazard combat damage, persistent combat health, save/revive/checkpoint, generation/world assembly/Full Run은 시작하지 않았다. RMAP02 jump/run/camera 수치, RMAP03 grab 허용 규칙, RMAP04 climb 속도 및 one-way ignore 시간도 변경하지 않았다.

## RMAP06 BINDINGS

- EXISTING: `CharacterLiveInputSource`→`CharacterInputSnapshot`→`CharacterLiveMovementDriver` fixed-step path, `CharacterLiveFallDamageState.CanAcceptInput`, `IsStunned`, `IsDead`.
- PROPOSED: RMAP06 look mode는 이 gate를 소비해 관찰 입력 우선순위를 정할 수 있다. Tab action, look state 또는 우선순위 변경은 추가하지 않았다.

## FOLLOWUP / FINAL EVIDENCE

`single_task_v1` metadata의 RMAP04 Result/Task SHA와 installed/archive byte equality를 모두 검증했다. 현재 PASS Result는 Phase C에서 RMAP05만 `CURRENT→COMPLETE`, Current만 `NONE`으로 닫을 권한을 준다. RMAP06_LOOK은 `LOCKED / NOT STARTED`여야 한다.

COMMIT: 작업 전 `d1d0af643ce6efb8267bc8a4b5799e0599fc4bf7`; Phase D 생성 commit SHA는 최종 CLI 검증에서 보고한다. Push는 수행하지 않는다.
