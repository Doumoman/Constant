TASK: MAP13_03_SEPARATE_FIXED_SHELL_SLOTS_AND_PERSISTENCE
STATUS: PASS
MAP13_03: COMPLETE ELIGIBLE
MAP13_04_IMPLEMENT_VILLAGE_SHELL_FACILITIES_AND_ACCESS: LOCKED / DO NOT START

## User-Facing Implementation Report

MAP09의 validated `SpecialRegionContract`, MAP13_01의 placed `SpecialRegionSiteBridge`, MAP13_02의 `SpecialRegionEntryBufferPlan` 및 `SpecialRegionPlacementCollisionPlan`을 exact digest identity로 연결하여 고정 collision/access와 교체 가능한 payload slot을 분리했다. 새 구현은 Runtime 데이터 compile/proof만 수행하며 Scene, Tilemap, Prefab, SaveData, inventory 또는 gameplay spawn을 변경하지 않는다.

신규 script와 책임은 다음과 같다.

- `Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionFixedSlotLayers.cs`
  - `SpecialRegionFixedSlotLayerCompiler.Compile`: MAP09 contract publication + MAP13_01 bridge + MAP13_02 entry-buffer/collision plan + optional Clear/Assign intent를 입력받아 immutable FixedCollision, FixedAccess, five-kind ReplaceableSlot, HardProtected claim 및 canonical digest를 출력한다. digest/provenance/coordinate/owner/kind/persistence mismatch는 accumulated stable error와 atomic null plan으로 출력한다.
  - `SpecialRegionSlotReplacementIntent.Clear/Assign`: 실제 spawn/despawn 없이 slot replacement 계획만 표현한다.
  - `SpecialRegionFixedCollisionCell`: placed FixedShell의 source/placed coordinate와 collision ownership을 보존한다.
  - `SpecialRegionFixedAccessBinding`: Entry, Return, internal apron의 HardProtected access ownership을 별도 보존한다.
  - `SpecialRegionReplaceableSlotBinding`: Facility/Npc/Enemy/Event/Reward marker와 source/placed coordinate, required, scope/key, occupant plan을 보존하며 Solid/Collision/Route/Access 및 persistence ownership을 획득하지 않는다.
  - `SpecialRegionFixedSlotLayerCanonicalDigest.Compute*`: fixed collision, fixed access, slot invariant, assignment, immutable aggregate 및 full plan digest를 ordinal/invariant SHA-256으로 계산한다.
- `Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionPersistenceSafety.cs`
  - `SpecialRegionPersistenceSafetyCompiler.Compile`: required Reward slot과 7 checkpoint evidence를 입력받아 recovery/claim/revisit safety proof 또는 stable atomic errors를 출력한다.
  - `SpecialRegionPersistenceCheckpointEvidence`: region/slot/key/Reward scope/source digest/checkpoint/state provenance를 보존한다.
  - `SpecialRegionRequiredResourceSafetyProof`: Initial availability, interrupt/fail/regenerate recovery, claim/revisit stability, permanent-loss 및 duplicate-risk 0을 증명한다.
  - `SpecialRegionPersistenceSafetyCanonicalDigest.Compute/ComputeProof`: proof 및 aggregate safety publication의 canonical SHA-256을 계산한다.
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/SpecialRegions/SpecialRegionFixedSlotPersistenceTests.cs`
  - public in-memory upstream fixture로 layer 분리, exact kind replacement, persistence recovery/claim, atomic failure, order/repeat/culture/immutability 및 mutation authority 0을 검증한다.

성공 fixture의 실제 출력은 다음과 같다.

| Output | Actual |
|---|---:|
| FixedCollision | 2 |
| FixedAccess | 192 (`Entry 1 / Return 1 / Apron 190`) |
| Replaceable Facility / Npc / Enemy / Event / Reward | `1 / 1 / 1 / 1 / 1` |
| HardProtected layer claims | 2 |
| FixedCollision↔FixedAccess overlap | 0 |
| Replaceable↔Fixed/Access overlap | 0 |
| placement write / spawn+despawn / tile mutation | `0 / 0 / 0` |

Canonical evidence:

```text
FixedCollision digest:
d62728bb58a3d008ee1a6a5eb559ca897271d76ba62dce4fe7375bfae48d4815

FixedAccess digest:
71a5011ce05f692e88672b926d00fd9d368223ea577f784f7453f57f89c03089

Replaceable slot invariant digest:
d72f9ee17fd2faf11ca35e7558fd2c8daa4bff40a079c3d347a8ac75955a7639

Clear plan immutable layer digest:
5969b17232e5e7faf9a6eee3df520447308237b944ec64998aa0bae11a68a5ff

Assign plan immutable layer digest:
5969b17232e5e7faf9a6eee3df520447308237b944ec64998aa0bae11a68a5ff

Clear full plan digest:
10dc034868ad2e60a313b4f22ba56e24dbace801b39aac66b2dc3687f2be45a0

Assign full plan digest:
1a637639c26991e2baddb4c134944e712539f4041a1c89ce3d7a2e2926de130e
```

replacement 전후 immutable layer digest는 exact same이고 assignment를 포함하는 full plan digest만 달라진다. Event occupant는 marker-only이며 persistence owner가 되지 않는다. Facility/Npc/Enemy/Event/Reward의 wrong-kind occupant와 Entry/Return replacement는 atomic failure다.

required Reward proof의 실제 결과:

```text
required Reward proofs: 1
checkpoint evidence: 7
Initial Available: PASS
Active TemporarilyUnavailable: PASS
Interrupted / Failed / Regenerated Available: PASS / PASS / PASS
Claimed / Revisited Claimed: PASS / PASS
PermanentlyUnavailable: 0
DuplicateRewardRisk: 0
Reward grant / inventory mutation / Save write: 0 / 0 / 0
proof digest: 2148ea4d7478ef800e3194602aeff76e5856f135d838b04d9304edc9c18488bb
aggregate safety digest: 204feb61e80d92340e968addb865c83462ca2a903bd4570d550368cf03b04aea
```

이제 MAP13 pipeline은 placed SpecialRegion을 immutable collision/access layer와 replaceable gameplay opportunity layer로 전달하고, required Reward가 interrupt/fail/regenerate 후 복구되며 claim/revisit 후 중복 보상으로 rollback하지 않는지를 SaveData 없이 검증할 수 있다.

아직 구현하지 않은 항목은 MAP13_04 village road/facility content, MAP13_05 state variant, authored MAP13_06~07 regions, MAP13_08 preview/validator UI 및 이후 runtime spawn/save integration이다. 이번 결과는 data-only라 Editor window 또는 게임 화면의 신규 시각 요소는 없고, Unity Test Runner 및 downstream debug consumer에서만 관찰 가능하다.

## Responsibility and Added Functions

| Field | Required evidence |
|---|---|
| Task responsibility | placed FixedCollision/FixedAccess/ReplaceableSlot layer + required Reward persistence safety proof |
| Added scripts | Runtime 2: `SpecialRegionFixedSlotLayers.cs`, `SpecialRegionPersistenceSafety.cs`; focused test 1: `SpecialRegionFixedSlotPersistenceTests.cs`; matching meta `2 + 1` |
| Added functions | `SpecialRegionFixedSlotLayerCompiler.Compile`는 layer compile만, `SpecialRegionSlotReplacementIntent.Clear/Assign`은 non-executing replacement intent만, `SpecialRegionFixedSlotLayerCanonicalDigest.Compute*`는 layer digest만, `SpecialRegionPersistenceSafetyCompiler.Compile`은 lifecycle proof만, `SpecialRegionPersistenceSafetyCanonicalDigest.Compute/ComputeProof`는 proof digest만 담당 |
| Inputs consumed | validated MAP09 `SpecialRegionContract` publication + MAP13_01 `SpecialRegionSiteBridge` + MAP13_02 `SpecialRegionEntryBufferPlan`/`SpecialRegionPlacementCollisionPlan`과 각 exact canonical digest |
| Outputs produced | copied/read-only canonical FixedCollision 2, FixedAccess 192, five-kind slot 5, HardProtected claims 2, immutable/assignment/full digests, checkpoint safety proof 1 및 accumulated stable errors |
| Explicit non-ownership | Village content, Prefab/NPC/Enemy/Event/Reward spawn, inventory, gameplay mutable state, SaveData I/O, Terrain/Tilemap write, Scene/Prefab/ScriptableObject, MAP13_04+ |
| Downstream consumer | 별도 검수 후 MAP13_04만 unlock 가능; 본 Task는 MAP13_04를 열거나 시작하지 않음 |

## Focused Verification

```text
Unity: 6000.3.8f1
Mode: EditMode
Category filter: MAP13_03
Successful job: 7c421161943f40fe8ec732421c919282
discovered = executed = passed = 11
failed / skipped / inconclusive = 0 / 0 / 0
duration = 2.2223596 seconds
compile / relevant Console error = 0 / 0
```

최초 동일 category job `e31a7554a9af4fcbb968e47e65d3f58c`는 Test Runner 초기화 15초 timeout으로 test body 실행 0개에서 종료됐다. full asset refresh/compile 후 동일한 `MAP13_03` EditMode filter만 120초 initialization allowance로 재요청했고 위 11개가 모두 PASS했다. 다른 category 또는 mode 선택으로 fallback하지 않았다.

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

## Static Scope Gate

```text
new Runtime C#/meta: 2/2
new focused test C#/meta: 1/1
existing C#/test/CSV/meta modifications: 0
Authoring/Generated/Scene/Prefab/Tilemap/Settings/Packages changes: 0
duplicate GUID: 0
runtime forbidden authority hits: 0
trailing whitespace / conflict marker hits: 0 / 0
installed/archive Task SHA equality: PASS
unapplied candidate/diff-check/unrelated staged: 0/0/0
Git push: NOT PERFORMED
```

기존 unrelated worktree의 `Constant.slnx` 및 TerrainClusters directory meta 3개는 읽기/수정/stage 대상에서 제외했다.

## Task-Owned Files and Commit Handoff

Task-owned commit scope:

```text
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionFixedSlotLayers.cs
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionFixedSlotLayers.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionPersistenceSafety.cs
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionPersistenceSafety.cs.meta
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SpecialRegions/SpecialRegionFixedSlotPersistenceTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SpecialRegions/SpecialRegionFixedSlotPersistenceTests.cs.meta
MapDesign/MCP/TASKS/MAP13_03_SEPARATE_FIXED_SHELL_SLOTS_AND_PERSISTENCE.md
MapDesign/MCP_ARCHIVE/MAP13_03_SEPARATE_FIXED_SHELL_SLOTS_AND_PERSISTENCE.md
MapDesign/MCP/REPORTS/MAP13_03_SEPARATE_FIXED_SHELL_SLOTS_AND_PERSISTENCE_RESULT.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

```text
Subject: MAP13_03: separate SpecialRegion layers and persistence
Push: NOT PERFORMED
Next Task: MAP13_04 remains LOCKED / DO NOT START
```
