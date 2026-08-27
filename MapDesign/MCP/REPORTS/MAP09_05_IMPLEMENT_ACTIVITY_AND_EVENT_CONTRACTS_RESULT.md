# MAP09_05 - Implement Activity and Event Contracts Result

```text
TASK: MAP09_05_IMPLEMENT_ACTIVITY_AND_EVENT_CONTRACTS
STATUS: PASS
MAP09_05: COMPLETE ELIGIBLE
MAP09_06_IMPLEMENT_SPECIAL_CANVAS_AND_SLICE_CONTRACTS: LOCKED / DO NOT START
```

## Predecessor, Status, and Dirty Preflight

The sole root inbox candidate passed every `single_task_v1` precondition. The predecessor Result and installed Task matched the patch metadata, the MAP09_05 source was installed and archived byte-identically, the inbox source was removed, and MAP09_05 became the only CURRENT row.

```text
Preflight HEAD: 3990864d8325245bfee6ec60aac4b302bed880bc
MAP09_04 Result status: PASS
MAP09_04 Result SHA-256:
58098e69a185779404bc30163ccf31f1bf9fcc0582f938eb97d4061ac651937a
MAP09_04 installed Task SHA-256:
f2a3e11a802da1faca5c5e0205ce5061596df68cb6d6327fc851a26a8e09c7c3
MAP09_05 inbox/installed/archive SHA-256:
ae54470791006b6e302f00f225ac92657c3e428d0d8f8088854770faca1bc2b5
Installed/archive bytes: 12155/12155, byte-identical
Status before open: 215 rows; COMPLETE 111 / CURRENT 0 / LOCKED 104
Status after open:  215 rows; COMPLETE 111 / CURRENT 1 / LOCKED 103
Root unapplied candidates after apply: 0
Staged paths before task execution: 0
```

No pre-existing unrelated worktree change overlapped the allowlist. No unrelated path was modified, staged, or included.

The compiled live predecessor baselines matched their approved Results exactly:

```text
MAP09_01 pass count/digest:
10 / 90a2614f9a95c29f1546f350190010524672d4b4aa2d1ad1dfe7dbd431be50d5
MAP09_02 layer count/digest:
7 / d0888c865cbdcc0884dc8abab9fac92900addd662a12a1ec30dc930f9cf4c94e
MAP09_03 MicroPattern fixture digest:
42c88cdb30154f098593d0e3be65063111613612fe5e9e1b9b11f2d9f1297a3d
MAP09_04 TerrainCluster fixture digest:
e8c3228e6f9df360637023d68e9c243cb70df4122342a3251740054bbcc8f9f1
Runtime assembly: Game.Map.Runtime
EditMode assembly: Game.Map.Tests.EditMode
Unity: 6000.3.8f1
```

## Implemented File Inventory

New Activity Runtime C# and matching metas:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Activities/ActivityStructureContract.cs
Assets/_Game/Map/Runtime/WorldGeneration/Activities/ActivityStructureContract.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/Activities/ActivityContractValidation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Activities/ActivityContractValidation.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/Activities/ActivityCanonicalDigest.cs
Assets/_Game/Map/Runtime/WorldGeneration/Activities/ActivityCanonicalDigest.cs.meta
```

New Event Runtime C# and matching metas:

```text
Assets/_Game/Map/Runtime/WorldGeneration/EventOverlays/EventOverlayContract.cs
Assets/_Game/Map/Runtime/WorldGeneration/EventOverlays/EventOverlayContract.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/EventOverlays/EventOverlayValidation.cs
Assets/_Game/Map/Runtime/WorldGeneration/EventOverlays/EventOverlayValidation.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/EventOverlays/EventOverlayCanonicalDigest.cs
Assets/_Game/Map/Runtime/WorldGeneration/EventOverlays/EventOverlayCanonicalDigest.cs.meta
```

New focused EditMode tests and matching metas:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Activities/ActivityStructureContractTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Activities/ActivityStructureContractTests.cs.meta
Assets/_Game/Tests/EditMode/Map/WorldGeneration/EventOverlays/EventOverlayContractTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/EventOverlays/EventOverlayContractTests.cs.meta
```

Task/protocol documents:

```text
MapDesign/MCP/TASKS/MAP09_05_IMPLEMENT_ACTIVITY_AND_EVENT_CONTRACTS.md
MapDesign/MCP_ARCHIVE/MAP09_05_IMPLEMENT_ACTIVITY_AND_EVENT_CONTRACTS.md
MapDesign/MCP/REPORTS/MAP09_05_IMPLEMENT_ACTIVITY_AND_EVENT_CONTRACTS_RESULT.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

No existing MAP00-MAP09_04 production/test source, folder meta, assembly definition, Scene, Prefab, data, Settings, or Package file was changed.

## ActivityStructure Contract

The immutable Activity contract reuses the existing `TerrainClusterId`, `SpineVariantId`, `LocalTileCoord`, integer RouteType compatibility, `PacingRole`, and `AccessClass` authorities. It adds typed Activity and slot IDs, the exact slot/cue enums, marker-backed explicit slots, compatibility-only pacing/access lists, Mechanism nodes/edges, Progression nodes/edges, and removal-safety evidence.

The validator accumulates stable-sorted, deduplicated errors and enforces:

- `ACT_`, `SLOT_`, `MECH_`, and `PROG_` stable identifiers;
- active TerrainCluster footprint ownership for every slot and safety tile;
- required Cue/Trigger/Recovery slots and pre-activation detectable, unique Cue pairs;
- exact Mechanism node-to-slot compatibility, relation compatibility, unique references, exactly one Trigger root, and one Trigger-reachable component;
- exact Progression graph ownership, required phases, Cue start, Exit terminal, an ordered `Cue -> Activation -> Core -> Reward -> Recovery -> Exit` success path, Failure/Reset targets, terminal Exit, and recovery/exit escape from reachable cycles;
- baseline SpineVariant and primary Entry/Exit identity;
- unchanged RouteType, AccessClass, and compiled static-shell digest before/after removal;
- non-empty safe/recovery sets and rejection of permanent writes, mandatory-exit destruction, and protected-envelope writes.

Activity removal neither owns nor mutates the static collision shell or TraversalGraph. Invalid input publishes no contract or digest. The compiled live fixture produced:

```text
ID: ACT_LIVE_BASELINE
TerrainCluster: TC_ACTIVITY_SHELL
SpineVariant: SPINE_ACTIVITY_BASELINE
Slots/Cues: 6/1
Mechanism nodes/edges: 5/4
Progression nodes/edges: 7/7
Validation: PASS
SHA-256: 7a5357320d8e2634ab9416ae7c90fb80a83c1c7f799a8df7689ba37b8a0903bc
```

The Activity digest includes shell references, compatibility, slots, cues, both owned graphs, and every removal-safety semantic. It excludes display text, locale, input/file/reflection order, time, object hash, RNG, and Unity lifecycle state.

## EventOverlay Contract

The immutable Event contract contains only its typed ID/kind, referenced TerrainCluster/optional Activity IDs, and canonical marker assignments. The exact Npc/Reward/State/Cosmetic/Empty kinds and EnableMarker/DisableMarker/SpawnNpc/SpawnReward/SetState operations are published.

The validator enforces explicit stable marker/payload tokens, unique existing marker targets, the exact kind/operation matrix, assignment presence for non-empty variants, assignment absence for Empty, resolved shell/activity references, and unchanged shell/mandatory-path/access/Activity-removal identity evidence. A separate validation evidence object can report a forbidden non-marker declaration without adding collision, route, access, pacing, envelope, MechanismGraph, or ProgressionGraph ownership to `EventOverlayContract`.

The compiled live fixture produced:

```text
ID: EVT_LIVE_BASELINE
Kind: Npc
Assignments: 1
Validation: PASS
SHA-256: 722a490f054e5bfc5a75ac81e03eee4978cd7f51d34e01fa1e01818c9d4ce904
```

The Event digest includes ID, kind, referenced shell/activity, and canonical marker assignments only. Display text and validation evidence do not affect marker semantics.

## Focused Validation and Regression Policy Override

Final authoritative MAP09_05 focused execution:

| Selection | Discovered | Executed | Passed | Failed | Skipped | Inconclusive |
|---|---:|---:|---:|---:|---:|---:|
| MAP09_05 | 67 | 67 | 67 | 0 | 0 | 0 |

Before the user's current regression prohibition, the final code also produced:

```text
MAP09_04: 71/71 PASS
MAP09_03: 62/62 PASS
MAP09_02: 38/38 PASS
MAP09_01: 26/26 PASS
MAP08:    9220/9220 PASS
```

During MAP07, the user issued the overriding instruction: do not perform regression work again unless a problem occurs. The active regression loop was interrupted immediately and no further regression was executed. Therefore the Task text's remaining MAP07/MAP06/MAP05 and aggregate `19347` replay are superseded for this execution and are not used as PASS evidence. The interruption was deliberate policy compliance, not a product failure. No focused, compile, Console, digest, or static problem remained that would authorize another regression run.

## Unity and Static Gates

```text
Unity version: 6000.3.8f1
Compile errors: 0
Console errors: 0
Relevant warnings: 0
Focused EditMode: 67 discovered / 67 executed / 67 passed / 0 failed / 0 skipped / 0 inconclusive
PlayMode: NOT REQUIRED
Scene/Prefab changes: NONE

Runtime C#/matching meta: 6/6
EditMode test C#/matching meta: 2/2
All Assets meta/GUID: 3866/3866
Duplicate GUID groups: 0
Forbidden production symbol hits: 0
Authoring CSV/matching meta: 50/50
Authoring manifest: f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb
Generated CSV: 0
Runtime asmdef unchanged: YES
EditMode asmdef unchanged: YES
Authoring/Generated/Scene/Prefab/Settings/Packages/asmdef task changes: 0
Existing MAP00-MAP09_04 modifications: 0
Other V2 root changes: 0
Unapplied root inbox candidates: 0
Unrelated staged/included: 0
Diff-check errors: 0
```

The Console contained one informational post-test cleanup log and no Error or Warning entry. No RNG, file I/O, prefab/state-machine execution, physics/projectile simulation, tile mutation, frequency/cap/cooldown, CSV, renderer, or Unity lifecycle dependency was added to production scope.

## Change Scope and Out-of-Scope Findings

All changed implementation and test paths are inside the two approved Runtime/Test roots. Existing folder metas and assembly definitions remained byte-unchanged. MAP09_06 was not read as an implementation input and was not started.

```text
OUT_OF_SCOPE_FINDINGS: NONE
```

## Atomic Commit Handoff

```text
Subject: MAP09_05: implement activity and event contracts
Commit: SELF
Push: NOT PERFORMED
```

Only the installed/archived Task, six Runtime C#/meta pairs, two focused test C#/meta pairs, this Result, and finalized Status are eligible for the atomic commit.
