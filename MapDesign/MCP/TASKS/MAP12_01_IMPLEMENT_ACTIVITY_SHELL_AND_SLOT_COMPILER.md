```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP12_01_IMPLEMENT_ACTIVITY_SHELL_AND_SLOT_COMPILER
  task_file: TASKS/MAP12_01_IMPLEMENT_ACTIVITY_SHELL_AND_SLOT_COMPILER.md
  requires_current_task: NONE
  requires_completed_task: MAP11_09_MAP11_CLUSTER_EXIT_TESTS
  requires_result:
    path: REPORTS/MAP11_09_MAP11_CLUSTER_EXIT_TESTS_RESULT.md
    status: PASS
    sha256: d407abe3592401416bbf3f8a0f196b0a073a6c706cd6517705dbef8db59e0ed1
  requires_installed_task:
    path: TASKS/MAP11_09_MAP11_CLUSTER_EXIT_TESTS.md
    sha256: bdc273ec52b06fdec8eb6bfdd974bc5fa88acde82eaed22ff5c6853252e4f58b
  sets_current_task: MAP12_01_IMPLEMENT_ACTIVITY_SHELL_AND_SLOT_COMPILER
```

# MAP12_01 — Implement Activity Shell and Slot Compiler

```text
TASK: MAP12_01_IMPLEMENT_ACTIVITY_SHELL_AND_SLOT_COMPILER
PHASE: MAP12 — ActivityStructure / EventOverlay
STATUS: CURRENT
NEXT: MAP12_02_IMPLEMENT_ACTIVITY_REMOVAL_CUE_AND_SAFETY_PROOF
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP09_05의 immutable `ActivityStructureContract`를 MAP11에서 승인한 TerrainCluster Canvas에 처음 연결한다.

```text
Activity slots/phases
→ Cue/Core/Reward/Recovery shell coordinates
→ TerrainCluster Local Canvas projection
→ immutable Activity shell/slot compilation
```

이번 출력은 후속 단계가 Activity 요소를 배치할 좌표 계약이다. Prefab 생성이나 사건 실행은 하지 않는다.

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 반드시 추가/수정 스크립트, 각 책임, 새 기능, 파이프라인 위치, 미구현, Editor/게임 가시성을 실제 파일 단위로 보고한다.

## 1. Scope

| 소유 | 소유하지 않음 |
|---|---|
| 네 Activity shell zone 모델 | Prefab/state machine/physics 실행 |
| existing Activity slot semantic intent | Activity/Event production content와 CSV |
| source local tile → compiled Canvas projection | Sector/world placement/free-space solve |
| underlying geometry/protection read-only evidence | collision/tile/Static Shell 변경 |
| graph node → projected slot binding | frequency/cap/cooldown/RNG/selection |

MAP12_02 소유인 prefab 제거 proof, 시야/오디오 Cue, safe pocket gameplay clearance, 출구/보상 파괴 방지와 PlayMode safety는 구현하지 않는다. EventOverlay assignment는 MAP12_04 소유다.

## 2. Focused-Only Policy

정상 실행 선택:

```text
MAP12_01 EditMode: required
MAP09/MAP10/MAP11 categories: 0
legacy 19347: 0
PlayMode/unfiltered: 0/0
```

test 안에서 기존 public API를 호출하는 것은 과거 category 재실행이 아니다.

upstream defect 발견 시 owner/invariant/원인/최소 검증 범위만 기록하고 기존 production/CSV를 수정하지 않은 채 `BLOCKED`로 STOP한다. Task-owned 신규 파일 문제는 그 파일만 고치고 `MAP12_01`만 재실행한다.

## 3. Preflight

```text
MAP11_09 Result: PASS
MAP11 PHASE EXIT: APPROVED
Result SHA-256: d407abe3592401416bbf3f8a0f196b0a073a6c706cd6517705dbef8db59e0ed1
installed Task SHA-256: bdc273ec52b06fdec8eb6bfdd974bc5fa88acde82eaed22ff5c6853252e4f58b
MAP11_09 COMPLETE / MAP12_01 CURRENT / MAP12_02 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Current authority:

```text
TerrainCluster tables/files: 13/13
catalog/variants: 16/32
catalog digest: 9d26786af477731d57503f16cc899210da6636f48dfb0542791e8fa591bd3bf7
signature-set: 2884a639d9cef923e8b86a7fba2c0430cdfad2de11a63fd138d51dacdce13d8a
Authoring manifest: ff4761537986a4c9433775359d9b62ad806914ef30462a320c97b355126a5b6c
```

Required public inputs:

```text
MAP09_05 ActivityStructureContract, validator/digest, slots/cues,
         MechanismGraph, ProgressionGraph, ActivityRemovalSafety
MAP11    TerrainClusterLocalCanvas, RoleSocketContract,
         TraversalCompilation, RouteWitnessReport, StaticShell,
         PatternRenderReport, PatternWorkingCanvas
```

drift나 missing reference면 기존 authority를 복사·변경하지 말고 `BLOCKED`다.

## 4. Exact Files

정상 범위는 신규 C# 세 개와 matching meta뿐이다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/Activities/ActivityShellProjection.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Activities/ActivityShellCompiler.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Activities/ActivityShellCompilerTests.cs(.meta)
```

```text
Runtime: Game.Map.Runtime / StarNight.Map.WorldGeneration.Activities
Tests: Game.Map.Tests.EditMode / StarNight.Map.Tests.EditMode.WorldGeneration.Activities
Category: MAP12_01
```

기존 C#/test/CSV/meta/asmdef, Authoring/Generated, Scene/Prefab/SO/Tilemap, Settings/Packages 수정은 금지한다. 책임 분리가 불가능할 때만 같은 Runtime folder helper 1개를 허용하며 이유를 Result에 보고한다.

## 5. Public Model

이름은 project style에 맞출 수 있으나 동등한 surface를 제공한다.

```text
ActivityShellZoneKind: Cue, Core, Reward, Recovery
ActivitySlotSemanticKind:
  CueMarker, PressurePlateTrigger, DeviceAnchor, ProjectileEmitter,
  ChaseOrHazardSpawn, RewardAnchor, RecoveryAnchor, ResetAnchor, NpcAnchor
ActivityShellZoneDefinition
ActivitySlotProjectionIntent
ProjectedActivityShellCell / ProjectedActivitySlot
ActivityShellCanvas
ActivityShellCompileRequest / Result / ErrorCode / Error
ActivityShellCompiler.Compile
```

collection은 defensive-copy/read-only/canonical order다. 실패 시 Canvas/zones/slots/bindings/digest는 모두 0/null이다.

## 6. Zone and Slot Contract

caller는 exact 네 non-empty zone을 explicit `LocalTileCoord`로 제공한다.

```text
Cue      시작 전 감지 marker 영역
Core     Trigger/mechanism/hazard 중심 영역
Reward   보상 marker 영역
Recovery 실패 복구/reset marker 영역
```

- 모든 좌표는 referenced active Local Canvas 안이다.
- same-zone duplicate는 실패다.
- cross-zone explicit overlap은 허용하고 membership을 lossless하게 게시한다.
- zone은 overlay metadata이며 underlying occupancy를 변경하지 않는다.
- projected cell은 source/compiled coord, owning chunk, occupancy, AbsoluteProtected flag/provenance를 가진다.

모든 existing Activity slot에는 exact 하나의 intent가 필요하다.

| `ActivitySlotKind` | Semantic | Zone |
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

intent는 semantic만 보강한다. 좌표 authority는 existing `ActivitySlot.LocalTileCoord`다.

- missing/duplicate/unknown/mismatched intent를 거부한다.
- slot은 active tile/owning chunk와 required zone에 resolve돼야 한다.
- Activity cue `(kind,slot)`은 projected CueMarker에 연결한다.
- 모든 Mechanism node는 exact projected slot에 연결한다.
- Progression phase는 corresponding zone 존재만 참조하고 새 coordinate graph를 만들지 않는다.
- Exit phase는 TerrainCluster Exit witness에 남는다.

## 7. Artifact Binding

compiler는 다음 identity/digest chain을 검증한다.

```text
validated Activity + digest
→ referenced TerrainCluster/SpineVariant
→ LocalCanvas → role/socket → traversal
→ StaticShell/routes → final pattern working Canvas
→ Activity shell/slot overlay
```

- 좌표 변환은 `TerrainClusterLocalCanvas` mapping만 사용한다.
- working Canvas는 active tiles exact coverage이며 upstream digest와 일치한다.
- baseline/recovery witness 존재를 확인하되 재계산하지 않는다.
- compile 전후 StaticShell/working Canvas/traversal/routes digest는 같다.
- geometry write/carve/renderer invocation/RNG draw는 0이다.
- AbsoluteProtected overlap은 marker projection만으로 실패시키지 않고 flag/provenance로 게시한다. 실제 prefab 침범 판정은 MAP12_02 책임이다.

Graph ownership은 그대로 유지한다.

```text
TraversalGraph → TerrainCluster
MechanismGraph/ProgressionGraph → ActivityStructure
EventOverlay → 미구현
```

Mechanism node는 compatible semantic slot에 연결한다. Progression Cue/Core/Reward/Recovery는 같은 shell zone을 참조하고 Activation은 Core Trigger를, Exit은 TerrainCluster Exit witness를 참조한다. timing/physics/projectile/chase/reward 실행은 하지 않는다.

## 8. Digest and Errors

digest는 ruleset, Activity/upstream digests, cluster/variant, four zones, every slot semantic/coordinate/occupancy/protection, cue/mechanism/progression bindings를 포함한다. locale/time/object identity/input order/Prefab/RNG/runtime state는 제외한다.

최소 error groups:

```text
MissingInput | InvalidActivityContract | IdentityMismatch | ArtifactDigestMismatch
MissingZone | InvalidZone | DuplicateZoneCoordinate
MissingSlotIntent | DuplicateSlotIntent | UnknownSlot | SlotSemanticMismatch
SlotOutsideActiveCanvas | SlotOutsideRequiredZone | MissingGraphSlotBinding
WorkingCanvasMismatch | ProtectedEvidenceMismatch | NonCanonicalPublication
```

errors는 accumulated/deduplicated/stable-sorted이며 any error는 atomic zero publication이다.

## 9. Focused Fixture and Tests

Activity production CSV는 아직 없으므로 test-owned Activity fixture를 MAP09_05 public validator로 만든다. TerrainCluster는 fabricated Canvas 대신 physical catalog의 실제 non-Quiet cluster를 full MAP11 public chain으로 compile한다.

```text
Representative: TC_CRATER_BOWL_ASCENT
SpineVariant: catalog에서 실제 baseline 조회
Required slots: Cue, Trigger, Device, Hazard, Projectile,
                Reward, Recovery, Reset, Npc
```

`MAP12_01` category에서 검증:

1. real TerrainCluster chain + valid Activity compile
2. four zones/all slot semantics/source→compiled round-trip
3. Canvas/StaticShell/traversal/route immutability
4. cue/mechanism/progression binding completeness
5. protected overlap provenance와 writes/changes 0
6. reverse input/repeat/culture (`tr-TR`) determinism
7. each slot-semantic-zone mapping
8. missing/duplicate/unknown intent atomic failure
9. out-of-active coordinate atomic failure
10. Activity/cluster/variant/digest mismatch atomic failure
11. working Canvas mismatch atomic failure
12. missing mechanism binding atomic failure
13. RNG/file/prefab/physics/Tilemap side effect 0

## 10. Static Gates

```text
Unity compile / Console error / warning: 0/0/0
MAP12_01 discovered = executed = passed; fail/skip/inconclusive 0
new Runtime C#/meta: 2/2
new focused test C#/meta: 1/1
existing C#/test/CSV/meta changes: 0
Authoring/Generated changes: 0/0
asmdef/Scene/Prefab/Tilemap/Settings/Packages changes: 0
approved MAP11 digest drift: 0/0/0
duplicate GUID: 0
inbox/diff-check/unrelated staged: 0/0/0
prior/legacy/PlayMode/unfiltered selections: 0/0/0/0
Git push: NOT PERFORMED
```

initialization/import timeout으로 executed 0이면 PASS로 세지 않는다. refresh 후 같은 category만 재시도한다.

## 11. Required Result

```text
MapDesign/MCP/REPORTS/MAP12_01_IMPLEMENT_ACTIVITY_SHELL_AND_SLOT_COMPILER_RESULT.md
```

상단:

```text
TASK: MAP12_01_IMPLEMENT_ACTIVITY_SHELL_AND_SLOT_COMPILER
STATUS: PASS | BLOCKED
MAP12_01: COMPLETE ELIGIBLE | NOT COMPLETE
MAP12_02_IMPLEMENT_ACTIVITY_REMOVAL_CUE_AND_SAFETY_PROOF: LOCKED / DO NOT START
```

`## User-Facing Implementation Report`에서 파일/책임/새 기능/파이프라인/미구현/가시성을 한국어로 먼저 보고한다. `## Responsibility and Added Functions`에서는 inputs/outputs/non-ownership/downstream을 명시한다.

이후 다음 actual evidence를 기록한다.

- file/class/public surface
- representative cluster/variant
- zone/slot/semantic/projected-cell counts
- mechanism/progression binding counts
- occupancy/protected-overlap counts
- digest/determinism/culture
- negative atomic-failure matrix
- upstream writes/changes 0
- focused counts와 regression selections
- static/change scope와 commit

정상 경로:

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY TEST SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

PASS일 때만 MAP12_01을 Finalize하고 task-owned 파일만 atomic commit한다.

```text
Subject: MAP12_01: implement activity shell and slot compiler
Push: NOT PERFORMED
```

Result가 PASS여도 MAP12_02를 자동 시작하지 않는다. 별도 검수 전까지 계속 LOCKED다.
