# RMAP03_GRAB Result

```text
TASK: RMAP03_GRAB
STATUS: PASS
```

## 사용자 구현 보고

`Assets/_Game/Map/Scenes/MoonPalace/RMAP03/MoonPalaceCornerGrab_RMAP03.unity`를 열어 Play하면 실제 Player가 안전한 모서리에 자동으로 매달린다. `A/D` 또는 좌/우 이동으로 접근하거나 Grab 중 일반 공중 제어로 이탈하고, `S`/아래는 Grab을 억제·낙하시키며, `Space`는 수직 점프 이탈이다. static, destructible-classified, moving-safe 표면만 Grab하며 one-way/hazard/crushing/decoration marker는 잡지 않는다.

## 파일별 책임·재사용 판단

| 실제 경로 | 판단 | RMAP03 책임 | 소유하지 않는 책임 |
|---|---|---|---|
| `Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab` | REUSE | 공용 Rigidbody2D/CapsuleCollider2D/Input/motor 조합을 씬·test 인스턴스로 사용 | 공용 prefab 변경 |
| `Runtime/Movement/CharacterLiveMovementSettings.cs` | ADAPT | Grab probe/window/anchor/reentry 값을 한 settings 소유자에 둠 | RMAP02 run/walk/jump 튜닝 재정의 |
| `Runtime/Movement/CharacterLiveMovementDriver.cs` | ADAPT | 실제 Collider2D corner query, enter/hold/exit, safe-contact 공개 상태 | wall-kick/등반/피해 상태 |
| `Runtime/Movement/CharacterLiveGrabSurface.cs` | NEW | safe/destructible-classified/moving 및 forbidden fixture surface 분류 | 파괴, one-way 통과, hazard damage gameplay |
| `Runtime/Movement/CharacterLiveGrabMovingSolid.cs` | NEW | Kinematic Rigidbody2D fixture의 제한된 왕복 이동 | 플랫폼/피스톤 콘텐츠 풀 |
| `Live/Editor/RMAP03/CharacterLiveGrabSceneBuilder.cs` | NEW | RMAP03 전용 저장 씬·manifest 재현 | RMAP02 씬 변경 |
| `Map/Scenes/MoonPalace/RMAP03/MoonPalaceCornerGrab_RMAP03.unity` | NEW | 실제 Player/TilemapCollider/BoxCollider/moving fixture 관찰 씬 | Full Run/world assembly |
| `Tests/EditMode/Character/RMAP03/`, `Tests/PlayMode/Character/RMAP03/` | NEW | 분류와 실제 Player/Collider fixed-step 검증 | broad regression |
| `Tests/EditMode/Character/Game.Character.Tests.EditMode.asmdef` | ADAPT | RMAP03 EditMode test에 필요한 `Game.Character.Live` 참조 하나 추가 | runtime assembly 의존성 확대 |

## PRECONDITIONS

- Branch/작업 전 HEAD: `d72ee65568f1477127fb06512cef669aa9cab630` (`RMAP02 connect physical tilemap player and continuous camera`).
- RMAP02 PASS Result SHA-256: `3c277b7973ef3afac4c5e2ed28138dc7446798f8b4818123a0d32a5f08ee5443`.
- RMAP02 installed Task/Archive SHA-256: `3a8b6602abc775bd18cef93069f32185c6b582ea7a592cd8b54a95462cca61eb`.
- 정상 Apply 전 상태: 223 COMPLETE / 0 CURRENT / 17 LOCKED. Apply 후: 223 / 1 / 16, `Current Task=RMAP03_GRAB`, `RMAP04_CLIMB=LOCKED`.
- RMAP03 installed Task/Archive는 SHA-256 `87d6c6301c65708b749c2576e06fef2eea6a6b4331cd52ee954602e7db2a9cd0`로 byte-identical이다.

## CHANGED

`CharacterLiveMovementDriver`는 안전 표면 marker가 달린 실제 solid `Collider2D`만 world-up bounds의 노출 상단 모서리로 후보화한다. 기본 RMAP02 Terrain에는 marker가 없으므로 기존 RMAP02 grab 동작을 새로 열지 않는다. Grab anchor는 surface Transform-local로 보관해 moving solid의 실제 Rigidbody2D 이동을 따라간다.

설정은 probe=0.35, vertical window=0.45, side offset=0.21, hang offset=0.62, reentry delay=0.12s, upward suppression velocity=0.01을 `CharacterLiveMovementSettings` 한 곳에 둔다. `IsGrabbing`, `HasSafeGrabContact`, `LastSafeGrabAnchor`는 RMAP05가 소비 가능한 최소 안전 접점이다.

## REQUIREMENT EVIDENCE

| ID | 실제 산출물·관찰 | 판정 |
|---|---|---|
| P05 | 0.4×0.8 foot-pivot Player가 static `TilemapCollider2D+CompositeCollider2D` wall (saved scene x=8, y=1..2), destructible-classified BoxCollider, moving-safe Kinematic BoxCollider에 각각 Grab | PASS |
| P06 | one-way/hazard/crushing BoxCollider는 enabled 상태로 Grab 불가, decoration은 Collider 없이 Grab 불가. 이는 분류 fixture 검증이며 one-way 통과·피해 장치 구현이 아님 | PASS |
| P07 | 수평 접근/하강 후보는 Grab, 상승 Jump 중은 Grab 억제, 실제 Input System `S + D → InputSource → snapshot → motor`와 direct fixed snapshot Down 모두 Grab 억제 | PASS |
| P08 | 같은 anchor에서 90 fixed steps 무제한 유지, Space 이탈은 기존 jump velocity=7.2로 약 1.3타일 상승하고 좌우는 기존 air control, Down 이탈 뒤 0.12s 재진입 억제 | PASS |
| P10 | Kinematic Rigidbody2D moving solid가 x=8.5→11.5 (test), x=20.5→23.5 (saved scene), speed=1.5로 이동할 때 Transform-local anchor를 유지해 Player를 운반; danger 전환 시 즉시 Grab 해제 | PASS |

## VALIDATION

Unity 6000.3.8f1에서 RMAP03 전용 filtered 검사만 실행했다. broad build, legacy/full/unfiltered regression은 실행하지 않았다.

| Job / 결과 파일 | Filter | total / pass / fail |
|---|---|---|
| `rmap03_editmode_results.xml` | `SurfaceClassification_OnlySafeSolidKindsAllowGrab` | 1 / 1 / 0 |
| `rmap03_playmode_results.xml` | PlayMode `RMAP03` | 7 / 7 / 0 |
| `rmap03_playmode_input_results.xml` | actual Input System Down suppression | 1 / 1 / 0 |
| `rmap02_direct_regression_results.xml` | PlayMode `RMAP02` (direct movement-driver impact) | 5 / 5 / 0 |

## UNITY VISIBLE OUTPUT

`rmap03_fixture_manifest.json` records the saved scene, 0.4×0.8 Player, static Tilemap wall, destructible BoxCollider, Kinematic moving-safe BoxCollider path/speed, and the four forbidden markers. Saved-scene PlayMode test loads Player, TilemapCollider2D/CompositeCollider2D, `CharacterLiveGrabSurface`, moving Rigidbody2D/mover, and 12×8 CameraFollow from the actual scene.

## OUT-OF-SCOPE FINDINGS

RMAP04 climb/ladder/pole/one-way traversal, RMAP05 damage/stun/death, RMAP06 observation, destruction gameplay, doors/pistons, generated world/Full Run are not started. Hazard, one-way and crushing here are current-state classification fixtures only.

## RMAP04 BINDINGS

EXISTING: `CharacterLiveMovementDriver.IsGrabbing`, `HasSafeGrabContact`, `LastSafeGrabAnchor`, `CharacterLiveGrabSurface`, Player `Body`/`BodyCollider`/Input snapshot, and physical Collider2D surfaces. PROPOSED: an RMAP04-specific climb/ladder/pole/one-way traversal state that may consume the safe anchor/contact but must not reinterpret RMAP03's forbidden `OneWay` marker as implemented pass-through. No climb API, ladder/pole fixture, or one-way collision behavior was added here.

## FOLLOWUP / FINAL EVIDENCE

RMAP03 single_task_v1 installation and archive bytes are identical at the SHA above. This PASS Result permits only `Current Task: RMAP03_GRAB → NONE` and `RMAP03_GRAB: CURRENT → COMPLETE`; expected final topology is 224 COMPLETE / 0 CURRENT / 16 LOCKED. `RMAP04_CLIMB` remains LOCKED / NOT STARTED.

COMMIT: pre-task HEAD `d72ee65568f1477127fb06512cef669aa9cab630`; generated commit SHA and this Result SHA are reported by final CLI verification. Push is not performed.
