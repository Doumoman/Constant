# MAP12_01 Activity Shell and Slot Compiler Result

TASK: MAP12_01_IMPLEMENT_ACTIVITY_SHELL_AND_SLOT_COMPILER
STATUS: PASS
MAP12_01: COMPLETE ELIGIBLE
MAP12_02_IMPLEMENT_ACTIVITY_REMOVAL_CUE_AND_SAFETY_PROOF: LOCKED / DO NOT START

## User-Facing Implementation Report

추가/수정 스크립트:

- 신규 Runtime 모델: `Assets/_Game/Map/Runtime/WorldGeneration/Activities/ActivityShellProjection.cs`
- 신규 Runtime compiler: `Assets/_Game/Map/Runtime/WorldGeneration/Activities/ActivityShellCompiler.cs`
- 신규 focused test: `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Activities/ActivityShellCompilerTests.cs`
- 위 세 파일의 matching `.meta` 신규 3개
- 기존 production C#, test, CSV, meta 수정: 0

스크립트 책임:

- `ActivityShellProjection.cs`는 Cue/Core/Reward/Recovery 네 shell zone, 9종 slot semantic intent, projected cell/slot, cue/mechanism/progression binding과 immutable `ActivityShellCanvas`를 정의한다.
- `ActivityShellCompiler.cs`는 검증된 Activity 계약을 기존 MAP11 Local Canvas, role/socket, traversal, route witness, Static Shell, final pattern working Canvas에 연결한다. 기존 compiler를 다시 구현하거나 geometry를 수정하지 않는다.
- `ActivityShellCompilerTests.cs`는 Editor-only public importer를 test reflection 경계로 호출해 실제 physical catalog의 `TC_CRATER_BOWL_ASCENT`를 full MAP11 public chain으로 compile하고 MAP12_01 계약을 검증한다. CSV parser, graph solver 또는 renderer를 test 안에 복제하지 않는다.

이번에 새로 가능해진 것:

- Activity의 기존 source-local slot 좌표를 TerrainCluster compiled Canvas 좌표로 결정적으로 투영할 수 있다.
- 네 zone의 cross-zone membership을 손실 없이 게시하고, 각 slot의 semantic/required zone/occupancy/owning chunk/AbsoluteProtected provenance를 조회할 수 있다.
- Activity cue, MechanismGraph node, ProgressionGraph phase를 projected slot 또는 shell zone에 연결하며 Exit phase는 기존 TerrainCluster baseline Exit witness를 참조한다.
- 잘못된 zone, slot intent, identity, digest, working Canvas 또는 graph binding은 partial artifact 없이 atomic failure로 차단한다.

파이프라인 위치:

```text
13 TerrainCluster CSV → physical catalog entry
→ Local Canvas → role/socket → traversal → route/Static Shell
→ PatternFree final working Canvas
→ validated ActivityStructureContract
→ Activity shell/slot overlay
```

아직 미구현:

- MAP12_02의 prefab 제거 proof, cue 시야/오디오, safe-pocket gameplay clearance와 PlayMode safety
- Activity/Event production CSV, frequency/cap/cooldown, RNG selection
- MAP12_04 EventOverlay assignment
- 실제 prefab/state machine/projectile/chase/reward 실행, Sector/world placement, Tilemap/physics 변경

Editor/게임 가시성:

- 신규 gameplay 화면, Scene, Prefab, Tilemap 변경은 0이다.
- 신규 기능은 현재 순수 Runtime data/compiler surface와 Unity Test Runner의 `MAP12_01` EditMode category에서만 보인다.
- 기존 TerrainCluster preview와 게임 화면 동작은 변경하지 않았다.

## Responsibility and Added Functions

| Field | Actual |
|---|---|
| Task responsibility | MAP09_05 Activity contract를 MAP11 Static Shell/working Canvas 좌표 계약에 연결 |
| Added functions | 네 shell zone, 9 semantic intent, projected cell/slot, cue/mechanism/progression binding, atomic compiler |
| Inputs consumed | existing Activity validator/digest와 physical MAP11 importer/compiler/route/pattern authorities |
| Outputs produced | immutable ActivityShellCanvas, canonical digest, stable accumulated error list |
| Explicit non-ownership | upstream repair, content CSV, removal gameplay proof, EventOverlay, world placement, rendering/physics 실행 |
| Downstream consumer | 별도 검수 후 MAP12_02만 다음 patch로 열 수 있음 |

## Added File and Public Surface

### `ActivityShellProjection.cs`

```text
ActivityShellZoneKind: Cue / Core / Reward / Recovery
ActivitySlotSemanticKind:
  CueMarker / PressurePlateTrigger / DeviceAnchor / ProjectileEmitter /
  ChaseOrHazardSpawn / RewardAnchor / RecoveryAnchor / ResetAnchor / NpcAnchor
ActivityShellZoneDefinition
ActivitySlotProjectionIntent
ProjectedActivityShellCell
ProjectedActivitySlot
ActivityCueSlotBinding
ActivityMechanismSlotBinding
ActivityProgressionShellBinding
ActivityShellCanvas
```

모든 공개 collection은 defensive-copy/read-only이며 output은 canonical order다. projected cell/slot은 source와 compiled coordinate, owning chunk, current/static occupancy, AbsoluteProtected flag와 traversal provenance를 보존한다.

### `ActivityShellCompiler.cs`

```text
ActivityShellCompileRequest
ActivityShellCompileErrorCode / ActivityShellCompileError
ActivityShellCompileResult
ActivityShellCompiler.Compile
Ruleset: MAP12_01_ACTIVITY_SHELL_SLOT_COMPILER_V1
```

실패 시 `Canvas = null`, zones/zone cells/slots/bindings `0`, digest empty다. errors는 accumulated, deduplicated, stable-sorted다.

### `ActivityShellCompilerTests.cs`

- Assembly: `Game.Map.Tests.EditMode`
- Namespace: `StarNight.Map.Tests.EditMode.WorldGeneration.Activities`
- Category: `MAP12_01`
- test-owned Activity fixture는 기존 `ActivityContractValidator`로 검증했다.
- physical catalog는 기존 `MapAuthoring.Editor` importer를 reflection으로 호출해 asmdef 수정 없이 소비했다.

## Physical Preflight and Approved Authority

```text
Unity: 6000.3.8f1
TerrainCluster physical files/tables: 13/13
catalog entries/variants: 16/32
representative present: TC_CRATER_BOWL_ASCENT = YES
TerrainCluster catalog digest:
9d26786af477731d57503f16cc899210da6636f48dfb0542791e8fa591bd3bf7
structural signatures/duplicates: 16/0
signature-set digest:
2884a639d9cef923e8b86a7fba2c0430cdfad2de11a63fd138d51dacdce13d8a
Authoring CSV/meta: 65/65
Generated CSV: 0
Authoring manifest:
ff4761537986a4c9433775359d9b62ad806914ef30462a320c97b355126a5b6c
```

MAP11의 승인 catalog/signature-set/manifest drift는 `0/0/0`이다.

## Representative Artifact and Binding Evidence

```text
cluster: TC_CRATER_BOWL_ASCENT
baseline variant: SPINE_CRATER_BOWL_ASCENT_BASE
zones: 4
projected zone cells: 10
unique projected coordinates: 9
cross-zone overlap memberships: 1
projected slots: 9
cue bindings: 1
mechanism bindings: 8
progression bindings: 7
zone occupancy solid/air: 4/6
AbsoluteProtected zone cells: 4
AbsoluteProtected slots: 3
baseline witness edges: 4
recovery witness routes: 1
Static Shell / final working Canvas tiles: 288/288
geometry writes/changes: 0/0
renderer invocations/RNG draws: 0/0
```

모든 projected cell의 compiled→source round-trip이 원본 source-local coordinate와 일치했다. Cue/Core cross-zone overlap은 두 membership으로 보존됐으며 underlying occupancy는 바뀌지 않았다.

Slot mapping actual:

| ActivitySlotKind | Semantic | Required zone |
|---|---|---|
| Cue | CueMarker | Cue |
| Trigger | PressurePlateTrigger | Core |
| Device | DeviceAnchor | Core |
| Hazard | ChaseOrHazardSpawn | Core |
| Projectile | ProjectileEmitter | Core |
| Reward | RewardAnchor | Reward |
| Recovery | RecoveryAnchor | Recovery |
| Reset | ResetAnchor | Recovery |
| Npc | NpcAnchor | Core |

Progression `Activation`은 `SLOT_TRIGGER`, `Reset`은 `SLOT_RESET`, `Exit`은 기존 baseline witness의 Exit node에 연결됐다.

## Digest and Determinism Evidence

```text
Activity contract:
50acd7023822edf8ba608a044a158b21053839afa4ed181ab357d7dd7cd8160d
TerrainCluster source contract:
f95f5b75c438d37e1e38503216473274e32d4868212f2b271f525bd312207402
Local Canvas:
aa675212ee03e462b4f6d9b5eebfbd6b6975d3567e55e9f5e41a601cb9e968b8
role/socket:
147a3e884e7ef4d63421b773da8dabb32169c60cdee596e506f9956c70c6845c
traversal:
e38a6d158866b07102aef42b0400085b1e5cd960d64a5308809f9bd950ccd84a
route witness:
bf6fd84f020495de2a1ad4669eee37e61932bf76adc9411ed9d4afd7ff378ca2
PatternFree render report:
611737a776c2c7b84f9062694c125901c9d5763e5e9822188262d09fa3cd3689
final working Canvas:
e8b2aa4250fb96b17002eb46d2879fa847f028f21cffde9a5044712f6c8a1c8b
Activity shell artifact:
228afb7f7a4b351d9f9706c039cb0681babaa7830de5e48a5061d962f3ebebe9
```

canonical, repeat, reverse-enumerated zone/intent/Activity input과 `tr-TR` culture compile이 모두 같은 Activity shell artifact digest를 게시했다. locale, input order, time, object identity, runtime state와 RNG는 digest에 영향을 주지 않는다.

## Negative Atomic-Failure Matrix

| Fixture | Required error evidence | Publication |
|---|---|---|
| missing slot intent | `MissingSlotIntent` | Canvas/digest/bindings 0 |
| duplicate slot intent | `DuplicateSlotIntent` | Canvas/digest/bindings 0 |
| unknown slot intent | `UnknownSlot` | Canvas/digest/bindings 0 |
| semantic mismatch | `SlotSemanticMismatch` | Canvas/digest/bindings 0 |
| out-of-active zone/slot | `InvalidZone` + `SlotOutsideActiveCanvas` | Canvas/digest/bindings 0 |
| missing mechanism slot binding | `MissingGraphSlotBinding` | Canvas/digest/bindings 0 |
| Activity cluster/variant mismatch | `IdentityMismatch` | Canvas/digest/bindings 0 |
| Activity/source artifact digest mismatch | `ArtifactDigestMismatch` | Canvas/digest/bindings 0 |
| final working Canvas digest mismatch | `ArtifactDigestMismatch` | Canvas/digest/bindings 0 |

모든 failure result의 errors는 repeatable stable order였고 partial publication은 없었다.

## Focused Verification

최종 선택:

```text
Mode: EditMode
Assembly: Game.Map.Tests.EditMode
Category: MAP12_01
Job: 3170ce147cdc46729f418abd206f62f0
Discovered / executed / passed: 7 / 7 / 7
Failed / skipped / inconclusive: 0 / 0 / 0
Duration: 1.5659809 s
```

통과한 test:

1. `ActivityIdentityArtifactDigestAndWorkingCanvasMismatchFailAtomically`
2. `EverySlotKindMapsToTheExactSemanticAndRequiredZone`
3. `MissingDuplicateUnknownAndMismatchedIntentsFailAtomically`
4. `OutOfActiveZoneAndSlotPlusMissingMechanismBindingFailAtomically`
5. `PublicSurfaceIsImmutableAndProductionHasNoExecutionOrSideEffectDependencies`
6. `RealTerrainClusterChainCompilesFourZonesAndAllSlotBindingsWithoutMutation`
7. `ReverseRepeatAndTurkishCultureProduceTheSameCanonicalArtifact`

Unity 상태:

```text
Unity version: 6000.3.8f1
compile errors: 0
task-owned warnings: 0
final Console errors/warnings: 0/0
PlayMode: not entered
active Scene changed/dirty mutation: 0/0
```

Test Runner가 기록한 자체 IPrebuild/IPostBuild 안내와 result-save 메시지는 task code diagnostic이 아니며 최종 Console 확인 전에 제거했다.

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY TEST SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

## Static and Change Scope

| Gate | Actual |
|---|---|
| new Runtime C#/meta | 2/2 |
| new focused test C#/meta | 1/1 |
| helper file | 0 |
| existing C#/test/CSV/meta modifications | 0 |
| Authoring/Generated changes | 0/0 |
| asmdef/asmref changes | 0/0 |
| Scene/Prefab/Tilemap/SO changes | 0/0/0/0 |
| Settings/Packages changes | 0/0 |
| upstream geometry writes/changes | 0/0 |
| approved MAP11 digest drift | 0/0/0 |
| duplicate GUID groups | 0 |
| unapplied inbox candidate | 0 |
| unrelated staged paths before Result/Finalize | 0 |
| Git push | NOT PERFORMED |

Unity refresh가 만든 unrelated solution/folder-meta side effects는 exact 제거했고 Result/commit 범위에 포함하지 않는다.

## Commit Handoff

```text
Subject: MAP12_01: implement activity shell and slot compiler
Scope: 2 Runtime C#/meta + 1 focused test C#/meta + installed/archive protocol + Result + finalized Status
Push: NOT PERFORMED
Commit SHA: reported after atomic commit
```

MAP12_01은 `COMPLETE ELIGIBLE`이다. MAP12_02는 자동 시작하지 않았고 별도 검수 전까지 `LOCKED / DO NOT START`다.
