# RMAP06_LOOK Result

```text
TASK: RMAP06_LOOK
STATUS: PASS
```

## USER-FACING IMPLEMENTATION REPORT

`Assets/_Game/Map/Scenes/MoonPalace/RMAP06/MoonPalaceMovementLab_RMAP06.unity`를 열어 Play한다. 지상 정지, 안전 Grab 정지, ladder/pole 정지에서 `Tab`과 `A/D`, `W/S`(또는 방향키)를 함께 1초 유지하면 8방향으로 3타일 관찰한다. 대각선은 정규화된다. 카메라는 0.24초에 이동하고 Tab 또는 방향을 놓으면 0.18초에 기존 연속 follow로 복귀한다. 관찰 대기와 관찰 중에는 이동·점프·행동·Grab·climb·one-way drop-through 입력이 소비된다.

## RESPONSIBILITY AND FILES

| 실제 경로 | 판단 | RMAP06 책임 | 소유하지 않는 책임 |
| --- | --- | --- | --- |
| `Assets/_Game/Character/Runtime/Input/CharacterInputSnapshot.cs` | ADAPT | 기존 fixed-step snapshot에 `LookHeld` 전달 | 신규 gameplay ActionId/전투 action |
| `Assets/_Game/Live/Input/CharacterLiveControls.inputactions`, `Runtime/Input/{CharacterLiveInputSource,CharacterLiveInputAdapter}.cs` | ADAPT | Tab Input System action과 held 값 배선 | run/walk/jump/climb 수치 변경 |
| `Assets/_Game/Live/Runtime/Camera/CharacterLiveLookModeState.cs` | NEW | 1초 wait, 8방향 정규화, 3타일 target, wait/active input lock 상태 | world/save/room/generation 상태 |
| `Assets/_Game/Live/Runtime/Movement/CharacterLiveMovementDriver.cs` | ADAPT | 실제 grounded/grab/climb/fall gate를 보는 허용 판정과 neutral snapshot 소비 | RMAP03 Grab, RMAP04 climb/one-way, RMAP05 fall 규칙 재작성 |
| `Assets/_Game/Live/Runtime/Camera/CharacterLiveCameraFollowDriver.cs` | ADAPT | runtime-only offset, 0.24/0.18초 이동, 12×8 최종 camera-center clamp | CameraRoomDriver snap 재활성화 |
| `Assets/_Game/Live/Editor/RMAP06/CharacterLiveMovementLabSceneBuilder.cs` 및 asmdef | NEW | RMAP06 물리 실험실과 fixture manifest 생성 | 기존 RMAP02~05 scene/prefab 수정 |
| `Assets/_Game/Map/Scenes/MoonPalace/RMAP06/MoonPalaceMovementLab_RMAP06.unity` | NEW | TilemapCollider2D terrain, Player, camera, safe/moving Grab, ladder/pole, one-way, fall deck | Full Run/world bake |
| `Assets/_Game/Tests/{EditMode,PlayMode}/Character/RMAP06/` | NEW | focused look state 및 actual Player/Camera/Input evidence | broad/full regression |
| `MapDesign/MCP/GENERATED/RMAP06/` | NEW | fixture manifest와 final NUnit artifacts | 이전 RMAP evidence 변경 |

## PRECONDITIONS

- Apply 전 HEAD/branch: `63dad4b7fddc9581beb353b74aadb4ea2c8d5d02` / `main` (`RMAP05 implement fall damage and landing stun`).
- 외부 정정 inbox SHA-256과 실제 전체 bytes: 모두 `1f11eeb9f775c78e6553d88dd2f49f9243f8d8b6faa71bf3fa707b915f9877aa`.
- RMAP05 Result SHA-256: `457ae2a27a2aee29ab9fc8e313dfc92ad6a4c76efb85237515327204c260b94a`; 독립 `TASK: RMAP05_FALL` / `STATUS: PASS` 확인.
- RMAP05 installed/Archive Task SHA-256: 모두 `056c65cdaf0040e99cb7423a43c9bb8579c4fb80735e89d202911c858db669f9`이며 byte-identical.
- Apply 전 `240 rows = 226 COMPLETE / 0 CURRENT / 14 LOCKED`; Apply 후 RMAP06만 `CURRENT`, `226 / 1 / 13`이다. installed/Archive RMAP06 bytes는 모두 외부 SHA와 동일하다.

## CHANGED / REUSE DECISIONS

- RMAP05의 `CanAcceptInput`, `IsStunned`, `IsDead`를 그대로 소비한다. false인 tick은 look wait/active를 취소하고 기존 fall input gate도 유지한다.
- 실제 `IsGroundedNow`, `IsGrabbing`, `IsClimbing`과 internal velocity를 허용 조건으로 사용한다. `MovingSafe` Grab surface는 RMAP03의 physical transport 분류를 소비해 관찰 대상에서 제외한다.
- 방향 변경은 active offset을 즉시 바꾸지 않고 새 1초 wait를 시작한다. 관찰 offset은 `CharacterLiveLookModeState`의 local runtime 값이며 save/generation state가 아니다.
- RMAP02의 follow driver만 적응했다. `CharacterLiveCameraRoomDriver`를 켜거나 경쟁시키지 않았고, run/walk/jump, Grab 거리, climb 속도, one-way 0.18초, fall table은 변경하지 않았다.

## REQUIREMENT EVIDENCE

| ID | 실제 산출물·관찰 | 판정 |
| --- | --- | --- |
| C03 | final PlayMode가 Input System `KeyboardState(Tab + A/D/W/S)` → `CharacterLiveInputSource` → snapshot → actual Player/Camera로 8방향을 확인했다. 1 fixed step에는 미발동, 1초 이후 target magnitude=3이며 대각선은 `3 / sqrt(2)`이다. follow driver의 runtime offset은 0.24초 이동·0.18초 복귀를 사용한다. | PASS |
| C04 | actual 0.4×0.8 Rigidbody2D/CapsuleCollider2D Player의 static ground, physical safe Grab, trigger 기반 static climb에서 관찰을 확인했다. actual airborne Player는 불가했고 RMAP05 stun/death gate는 wait를 보유하지 않고 취소했다. `MovingSafe` 분류는 허용 조건에서 제외하며, 그 physical transport 증거는 RMAP03 P10 PASS를 재사용했다. | PASS |
| C05 | 관찰 wait/active에서 동일 fixed-step snapshot을 neutral화하여 held right+Jump가 Player를 이동/점프시키지 않음을 확인했고, release 뒤 동일 Input System motor path의 수평 이동이 복원됐다. bounds `x=0..40`, 12-tile viewport에서 right look camera center가 `x<=34`로 clamp됨을 actual Camera에서 확인했다. | PASS |
| E02 | 저장 이동 실험실에 physical TilemapCollider2D flat/run, 1-tile step/tunnel, safe+moving Grab, ladder, pole, PlatformEffector2D one-way, 8-tile fall deck, fall state와 12×8 camera를 확인했다. RMAP02~05의 개별 run/jump/Grab/climb/one-way/fall behavior는 각각 기존 PASS Result 및 GENERATED evidence를 재사용했고 재실행하지 않았다. | PASS |

## VALIDATION

Unity `6000.3.8f1` batch만 사용했다. connected live Editor는 없었으며, builder가 저장 scene/manifest를 생성하면서 compile을 PASS했다. broad build, Player build, legacy 19347, full/unfiltered 또는 RMAP02~05 regression은 실행하지 않았다.

| 결과 파일 | filter | total / pass / fail |
| --- | --- | --- |
| `GENERATED/RMAP06/rmap06_editmode_results.xml` | `CharacterLiveLookModeStateTests` | 10 / 10 / 0 |
| `GENERATED/RMAP06/rmap06_playmode_results.xml` | `CharacterLiveLookModePlayModeTests` | 4 / 4 / 0 |

첫 EditMode run은 0.02f 누적의 경계 오차로 1 fixed step 늦게 발동해 실패했으며, threshold를 완화하지 않고 1.00초 표현 오차만 보정했다. 첫 PlayMode injection은 Tab 개별 delta-state를 지원하지 않는 test framework 제약을 만났고, 동일 Keyboard Input System의 full `KeyboardState` event로 바꿨다. 임시 debug XML은 제거했고 위 artifacts가 최종 결과다.

## UNITY VISIBLE OUTPUT

`rmap06_fixture_manifest.json`은 저장 scene, Player foot pivot, `Tab + 8-direction`, `hold=1.00s`, `offset=3`, `enter=0.24s`, `return=0.18s`, `12×8` camera와 `x=0..64/y=0..20` fixture bounds를 기록한다. 실험실에는 사용자 조작 안내 TextMesh와 physical fixture가 모두 저장돼 있다.

## OUT-OF-SCOPE FINDINGS

RMAP07 pattern/role library, seed/full run, world generation, save/replay/minimap/UI, combat knockback/damage source, persistent health/revive/checkpoint는 시작하지 않았다. RMAP02~05의 existing gameplay numbers와 scenes도 수정하지 않았다.

## RMAP07 BINDINGS

- EXISTING: RMAP06은 Player-local input/camera/movement integration만 소유하며 map microchunk API를 새로 만들지 않았다.
- PROPOSED: `GENERATED/RMAP01/file_bindings.csv`의 `Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/RmapPatternCatalog.cs`는 RMAP07의 proposed pattern catalog다. 이번 look state와 직접 연결하지 않았고 RMAP07을 시작하지 않았다.

## FOLLOWUP / FINAL EVIDENCE

`single_task_v1` predecessor metadata의 RMAP05 Result/Task SHA와 RMAP06 installed/Archive byte identity를 적용 전후 검증했다. 필수 C03/C04/C05/E02 미확인은 없다. 이 Result는 Phase C에서 RMAP06만 `CURRENT→COMPLETE`, Current만 `NONE`으로 닫을 권한을 준다.

```text
NEXT: RMAP07_PATTERNS LOCKED / NOT STARTED
COMMIT: Phase D pending after this PASS Result; push is not authorized.
```
