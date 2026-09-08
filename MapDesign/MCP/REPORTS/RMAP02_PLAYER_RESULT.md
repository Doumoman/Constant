# RMAP02_PLAYER Result

```text
TASK: RMAP02_PLAYER
STATUS: PASS
```

## 사용자 구현 보고

이제 `Assets/_Game/Map/Scenes/MoonPalace/RMAP02/MoonPalacePlayerTilemapRun_RMAP02.unity`를 열어 Play하면, 기존 실제 Player가 60×40 타일 물리 fixture에서 달리기, Shift 걷기, 점프, 가변 점프, 벽/천장 충돌과 출구 도달을 수행한다. 조작은 `A/D` 또는 좌/우 방향키 이동, `Space` 점프, `Left/Right Shift` 유지 걷기다. Game View에는 여분 화면을 pillarbox/letterbox로 처리한 정확한 12×8 월드 viewport가 Player를 연속 추적한다.

## 파일별 책임 및 재사용 판단

| 실제 경로 | 판단 | 이번 책임 | 소유하지 않는 책임 |
|---|---|---|---|
| `Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab` | REUSE | 기존 Rigidbody2D, CapsuleCollider2D, Input Source, motor 조합을 씬 인스턴스로 사용 | 공용 prefab 본체 변경, Grab |
| `Live/Input/CharacterLiveControls.inputactions`, `Runtime/Input/CharacterLiveInputSource.cs` | ADAPT | `Walk` action과 Shift held 상태를 기존 snapshot 흐름에 배선 | 새 전역 입력 체계 |
| `Runtime/Movement/CharacterLiveMovementSettings.cs`, `CharacterLiveMovementDriver.cs` | ADAPT | P01~P03 수치, 발바닥 pivot에 맞는 실제 Capsule cast/MovePosition | Grab/등반/피해 상태 |
| `Map/Runtime/WorldGeneration/Baking/GeneratedUnityTilemapApplier.cs` | NEW | 논리 bake plan을 7개 실제 Tilemap의 `SetTiles`로 소비하고 Terrain만 물리 collider에 연결 | Seed/full-world 생성 |
| `Live/Runtime/Camera/CharacterLiveCameraFollowDriver.cs` | NEW | 12×8, aspect frame, LateUpdate 연속 추적, 60×40 clamp | 기존 Room camera의 책임 변경 |
| `Live/Runtime/Player/CharacterLiveMapRunBootstrap.cs`, `CharacterLiveMapRunExit.cs` | NEW | fixture 전용 spawn/exit 조합 | 캠페인, 저장, 씬 전환 |
| `Live/Editor/RMAP02/CharacterLiveMapRunSceneBuilder.cs`, `Live/Prefabs/RMAP02/` | NEW | 재현 가능한 저장 씬과 7개 Tile asset 생성 | 공용 미술/Prefab 대체 |
| `Tests/EditMode/Map/RMAP02/`, `Tests/PlayMode/Character/RMAP02/` | NEW | 실제 Tilemap 적용과 실제 Player 물리/카메라 focused 검증 | 전체 회귀 테스트 |

## 실제 RMAP02 구현 경로

논리 경로는 `GeneratedTilemapBakePlan` → `GeneratedUnityTilemapApplier.Apply(plan, bindings)` → 각 `Tilemap.SetTiles`이며, 이번 60×40 fixture는 `Rmap02TilemapFixturePlan`의 직접 배치다. 이는 Seed, role-pool, Full Run이 아니다. Terrain binding에만 `TilemapCollider2D + static Rigidbody2D + CompositeCollider2D`를 부여한다.

실행 경로는 `CharacterLiveControls`의 Shift/A-D/Space → `CharacterLiveInputSource` → `ConsumeFixedSnapshot` → `CharacterLiveMovementDriver`의 기존 motor/`UnityPhysics2DCharacterCollisionWorld` capsule cast → 실제 Terrain collider다. Player는 공용 prefab의 씬 인스턴스에서만 `CapsuleCollider2D.size=(0.4,0.8)`, `offset=(0,0.4)`으로 맞춰 발바닥 root가 `(3,1)` spawn에 놓인다. `CharacterLiveCameraFollowDriver`는 이 Player Transform을 LateUpdate에 따라가며 최종 중심을 x=[6,54], y=[4,36]으로 clamp한다.

## PRECONDITIONS

- Branch/implementation start HEAD: `4d8760b980ffa2b3155ea7cc9302ca2a9675c870` (`RMAP01 register v4.2 plan and map reuse bindings`).
- Predecessor Result: `RMAP01_REBASE_RESULT.md`, `STATUS: PASS`, SHA-256 `1d63e4b4812ba3e6aa8d6b398717b13f1b7b6e011d25e7fab924e402e4794b3a`.
- RMAP01 installed Task/Archive SHA-256: `65cb0479876770d1aefabf1d48315744eba49e797433a9cf354018218cd24b8e`.
- Apply 전 상태는 222 COMPLETE / 0 CURRENT / 18 LOCKED, RMAP01 COMPLETE, RMAP02 LOCKED였다. 정상 Apply 후에는 222 / 1 / 17 및 RMAP02 CURRENT였고, RMAP03_GRAB은 계속 LOCKED였다.
- RMAP02 installed Task와 Archive는 모두 SHA-256 `3a8b6602abc775bd18cef93069f32185c6b582ea7a592cd8b54a95462cca61eb`로 byte-identical이다.

## REQUIREMENT EVIDENCE

| ID | 실제 산출물·관찰 | 판정 |
|---|---|---|
| P01 | 실제 Player Capsule 0.4×0.8, offset (0,0.4), Terrain TilemapCollider/CompositeCollider에서 발바닥 spawn 및 벽/계단/천장 충돌 | PASS |
| P02 | 실제 held run-jump 측정: 높이 1.237타일, 수평 3.425타일; 실 Input System에서 Shift walk 거리가 default run보다 0.35 이상 짧음 | PASS |
| P03 | 실측 early-release 높이 0.204타일; 설정 coyote=0.08s, buffer=0.10s; 실제 한 칸 Tilemap gap에서 coyote와 착지 직전 buffer 통과 | PASS |
| C01 | orthographicSize=4, 12×8 viewport. 실제 Camera에서 1500×1000 (3:2), 1600×900 (16:9), 2000×900 (20:9) 모두 pixelRect aspect=1.5 및 가로 투영 12를 확인 | PASS |
| C02 | Player를 LateUpdate로 연속 추적하고 실제 bounds clamp를 `(54,36)`과 `(6,4)`에서 확인; 새 씬에는 Room driver를 배선하지 않음 | PASS |
| E01 | 60×40 / 7 logical layers / 235 applied tiles. Start=(3,1), Exit=(42,1); 실제 Player가 1-tile step, low ceiling을 통과/충돌하며 exit trigger 도달 | PASS |

## VALIDATION

Unity 6000.3.8f1에서 RMAP02 category만 실행했다. broad build, legacy/full/unfiltered regression은 실행하지 않았다.

| Job / 결과 파일 | Filter | total / pass / fail |
|---|---|---|
| `rmap02_editmode_results.xml` | EditMode `RMAP02` | 1 / 1 / 0 |
| `rmap02_playmode_results.xml` | PlayMode `RMAP02` | 5 / 5 / 0 |
| `rmap02_playmode_measurement_results.xml` | physical height/run/release | 1 / 1 / 0 |
| `rmap02_playmode_coyote_results.xml` | physical coyote/buffer gap | 1 / 1 / 0 |
| `rmap02_playmode_camera_results.xml` | actual Camera aspect/clamp | 1 / 1 / 0 |
| `rmap02_playmode_saved_scene_results.xml` | saved scene Player/Tilemap/Camera | 1 / 1 / 0 |

`rmap02_fixture_manifest.json` records `scene_path`, `fixture_size_tiles=60x40`, `viewport_tiles=12x8`, `logical_layers=7`, `applied_tiles=235`, Spawn and Exit. The measurement log records `fullHeight=1.237`, `runDistance=3.425`, `releaseCutHeight=0.204`.

## UNITY VISIBLE OUTPUT

저장 씬에는 Grid 하위의 Terrain, Affordance, Material, Hazard, Marker, Protection, Owner Tilemap이 있고, Terrain만 실제 TilemapCollider2D/CompositeCollider2D/static Rigidbody2D를 갖는다. PlayMode saved-scene 검사가 이 구성과 tile `(12,1)` step, Player collider, CameraFollow를 실제로 로드해 확인했다. Camera 검사는 같은 Camera 컴포넌트에 세 화면비를 적용해 유효 Game viewport를 확인했다.

## OUT-OF-SCOPE FINDINGS

Grab(RMAP03), 등반/one-way(RMAP04), 낙하 피해(RMAP05), 관찰(RMAP06), Seed/role candidates/Full Run(RMAP07~19)은 구현하거나 시작하지 않았다. fixture는 명시적 직접 배치이며 생성 월드 승인 근거가 아니다.

## RMAP03 BINDINGS

`RMAP03_GRAB`은 **LOCKED / NOT STARTED**다. 확인된 EXISTING 접점은 `CharacterLivePlayerRig`의 `Body`/`BodyCollider`/`InputSource`, `CharacterLiveInputSource`의 `Action` 입력, `CharacterLiveMovementDriver`의 물리 contact/snapshot 경로, 그리고 RMAP02 Terrain TilemapCollider다. PROPOSED인 Grab contact/state adapter는 후속 Live Runtime 경로에 별도로 추가되어야 하며, 이번 run/walk/jump 설정이나 `GeneratedUnityTilemapApplier`를 변경하지 않고 이를 소비해야 한다.

## FOLLOWUP / FINAL EVIDENCE

single_task_v1 설치/Archive 규칙을 유지했고, RMAP02 Task와 Archive는 위 SHA로 동일하다. 이 PASS Result 뒤 Finalize는 오직 `Current Task: RMAP02_PLAYER → NONE`, `RMAP02_PLAYER: CURRENT → COMPLETE` 두 상태 field만 변경해야 한다. 예상 최종 토폴로지는 223 COMPLETE / 0 CURRENT / 17 LOCKED이며 RMAP03_GRAB은 LOCKED다.

NEXT: `RMAP03_GRAB` LOCKED / NOT STARTED

COMMIT: 작업 전 HEAD `4d8760b980ffa2b3155ea7cc9302ca2a9675c870`; 생성 commit SHA는 Finalize 후 CLI에서 확인한다. Push는 수행하지 않는다.
