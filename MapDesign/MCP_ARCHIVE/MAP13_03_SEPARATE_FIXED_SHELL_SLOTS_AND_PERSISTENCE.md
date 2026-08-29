```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP13_03_SEPARATE_FIXED_SHELL_SLOTS_AND_PERSISTENCE
  task_file: TASKS/MAP13_03_SEPARATE_FIXED_SHELL_SLOTS_AND_PERSISTENCE.md
  requires_current_task: NONE
  requires_completed_task: MAP13_02_IMPLEMENT_ENTRY_BUFFER_PRIORITY_AND_COLLISION_RULES
  requires_result:
    path: REPORTS/MAP13_02_IMPLEMENT_ENTRY_BUFFER_PRIORITY_AND_COLLISION_RULES_RESULT.md
    status: PASS
    sha256: 2d5e624b5bb1976d5885a93d9a25fa42fe2c31a7193f0baca231c10e9fd74612
  requires_installed_task:
    path: TASKS/MAP13_02_IMPLEMENT_ENTRY_BUFFER_PRIORITY_AND_COLLISION_RULES.md
    sha256: d46b1baa1ba721f78e5c03569e2bc2991c728dc93bb1569336afb5d1b0bfabfa
  sets_current_task: MAP13_03_SEPARATE_FIXED_SHELL_SLOTS_AND_PERSISTENCE
```

# MAP13_03 — Fixed Shell / Replaceable Slots / Persistence Safety

```text
TASK: MAP13_03_SEPARATE_FIXED_SHELL_SLOTS_AND_PERSISTENCE
PHASE: MAP13 — SpecialRegion / Village / Mandatory Landmarks
STATUS: CURRENT
NEXT: MAP13_04_IMPLEMENT_VILLAGE_SHELL_FACILITIES_AND_ACCESS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP09가 정의한 `FixedShell / ReplaceableSlot / SpecialPersistenceKey` 계약을 MAP13의 placed coordinate로 compile하여 다음을 명시적으로 분리한다.

```text
MAP13_01 placed site bridge
+ MAP13_02 entry/apron/buffer + collision evidence
+ validated MAP09 SpecialRegionContract
→ immutable Fixed Collision/Access layer
→ immutable Facility/Npc/Enemy/Event/Reward slot layer
→ required Reward persistence recovery/claim proof
```

이번 Task는 MAP09 타입을 다시 정의하지 않으며 SaveData나 실제 mutable state를 만들지 않는다. Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성하고 정확한 script·class/method 책임·input→output·새 기능·미구현·가시성을 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| placed FixedShell/collision/access layer projection | 새로운 SpecialRegion contract/schema |
| Facility/Npc/Enemy/Event/Reward replaceable slot projection | 실제 Prefab/NPC/Enemy/Event/Reward spawn |
| Entry/Return/apron의 hard-protected access ownership | Village road/facility content |
| slot replacement의 geometry/persistence 불변식 | replacement 실행·inventory·AI |
| required Reward의 loss/reset/claim/revisit proof | SaveData I/O 또는 runtime mutable state machine |
| focused MAP13_03 test | MAP13_04 Village 구현 |

Terrain/Tilemap write, persistence 저장, gameplay payload 실행과 기존 MAP09/MAP13 파일 수정은 하지 않는다.

## 2. Focused-Only Policy

정상 실행은 EditMode category `MAP13_03`만 선택한다.

```text
MAP13_03 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13_01~02 selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
```

current public API 호출은 과거 category 재실행이 아니다. upstream defect면 기존 파일을 고치지 말고 owner/invariant/reason/minimum verification을 기록해 `BLOCKED`로 STOP한다. 신규 파일 자체 문제만 고치고 `MAP13_03`만 재실행한다.

## 3. Read-Only Preflight

```text
MAP13_02 Result: PASS
MAP13_02 Result SHA-256:
2d5e624b5bb1976d5885a93d9a25fa42fe2c31a7193f0baca231c10e9fd74612

MAP13_02 installed Task SHA-256:
d46b1baa1ba721f78e5c03569e2bc2991c728dc93bb1569336afb5d1b0bfabfa

MAP13_02 COMPLETE / MAP13_03 CURRENT / MAP13_04 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required authority:

```text
MAP09 SpecialRegionContract/Validator/CanonicalDigest
SpecialRegionLayerKind: FixedShell / ReplaceableSlot
SpecialRegionSlotKind: Facility/Npc/Enemy/Event/Reward/Entry/Return
SpecialPersistenceKey and scopes Region/Slot/Reward/Encounter
MAP13_01 site/fixed-shell/slot/port placed bindings
MAP13_02 Entry/Return/apron HardProtected and collision-plan evidence
MAP12 Event assignment marker-only/persistence-preservation boundary
```

기존 contract가 invalid하거나 필요한 public authority가 없으면 logic을 복사하지 말고 `BLOCKED`로 보고한다.

## 4. Exact Write Boundary

정상 범위는 Runtime 2개, focused test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionFixedSlotLayers.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionPersistenceSafety.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SpecialRegions/SpecialRegionFixedSlotPersistenceTests.cs(.meta)
```

```text
Runtime: Game.Map.Runtime / StarNight.Map.WorldGeneration.SpecialRegions
Tests: Game.Map.Tests.EditMode / StarNight.Map.Tests.EditMode.WorldGeneration.SpecialRegions
Category: MAP13_03
```

수정 금지:

```text
existing C# / test / CSV / meta
asmdef / asmref
Authoring / Generated
Scene / Prefab / ScriptableObject / Tilemap / Material / Texture
Settings / Packages
```

helper 파일, Editor window, asset, serializer, CSV는 추가하지 않는다.

## 5. Fixed and Replaceable Layer Compilation

프로젝트 naming/style에 맞추되 다음 semantic surface를 제공한다.

```text
SpecialRegionFixedCollisionCell
SpecialRegionFixedAccessBinding
SpecialRegionReplaceableSlotBinding
SpecialRegionFixedSlotLayerPlan
SpecialRegionFixedSlotLayerCompileRequest
SpecialRegionFixedSlotLayerCompiler.Compile
SpecialRegionFixedSlotLayerCanonicalDigest
SpecialRegionFixedSlotLayerErrorCode / Error / Result
```

### 5.1 Fixed collision/access ownership

- input contract, MAP13_01 bridge와 MAP13_02 plan/digest identity가 exact 같아야 한다.
- 모든 MAP09 FixedShell cell을 MAP13_01 placed/world coordinate로 exact once projection한다.
- FixedShell collision/source ID와 placed coordinate는 immutable하다.
- Entry/Return port, matching slot, internal apron은 `FixedAccess`로 별도 표시하되 모두 `HardProtected`다.
- FixedCollision과 FixedAccess는 downstream replacement 대상이 아니다.
- FixedShell과 port/apron access cell의 prohibited overlap, duplicate coordinate/owner, bridge 밖 coordinate는 atomic failure다.
- Before/After Quiet placement는 source TerrainCluster ownership을 유지하고 FixedShell layer로 흡수하지 않는다.

### 5.2 Replaceable slot ownership

replaceable exact kinds:

```text
Facility / Npc / Enemy / Event / Reward
```

- Entry/Return은 replaceable collection에 들어갈 수 없다.
- 각 slot은 MAP13_01 source/placed coordinate, slot ID/kind, required flag, persistence scope/key를 보존한다.
- slot coordinate는 FixedCollision, FixedAccess, apron, 다른 slot과 겹치지 않는다.
- replaceable slot은 marker/payload opportunity이며 Solid/Collision/Route/Access를 소유하지 않는다.
- Event replacement는 MAP12 marker-only 규칙을 유지하고 persistence owner를 Event로 이전하지 않는다.
- Facility/Npc/Enemy/Event/Reward kind가 맞지 않는 occupant는 거부한다.
- replacement 전후 coordinate/kind/required/persistence key와 underlying fixed/access digest가 같아야 한다.
- Clear/Assign 계획은 가능하지만 실행·spawn·despawn·filesystem write는 하지 않는다.

### 5.3 Layer output and atomicity

Success output:

```text
canonical FixedCollision cells
canonical FixedAccess Entry/Return/apron bindings
canonical replaceable slots by kind
hard-protected occupancy claims
underlying bridge/entry-buffer/collision/contract digests
fixed/access/slot/aggregate canonical digests
```

Collections은 copied/read-only/canonical order다. any error는 layer plan/digests `0`; errors는 accumulated, deduped, stable-sorted다. reverse input/repeat/`tr-TR`에서 동일하고 RNG/time/filesystem/Unity lifecycle/static mutable cache는 `0`이다.

## 6. Required Reward Persistence Safety

MAP09 key/scope를 재사용하고 mutable SaveData를 만들지 않는다.

```text
SpecialRegionPersistenceCheckpoint:
Initial / Active / Interrupted / Failed / Regenerated / Claimed / Revisited

SpecialRegionRequiredResourceState:
Available / TemporarilyUnavailable / Claimed / PermanentlyUnavailable

SpecialRegionPersistenceCheckpointEvidence
SpecialRegionRequiredResourceSafetyProof
SpecialRegionPersistenceSafetyCompileRequest
SpecialRegionPersistenceSafetyCompiler.Compile
SpecialRegionPersistenceSafetyCanonicalDigest
SpecialRegionPersistenceSafetyErrorCode / Error / Result
```

대상은 `Required=true`인 Reward slot이다.

- `CoreResource` region은 required Reward slot을 최소 하나 가져야 한다. 다른 region은 authored required Reward가 있을 때만 같은 proof를 요구한다.
- required Reward마다 stable non-default key와 exact `Reward` scope가 있어야 한다.
- 모든 checkpoint는 같은 region/slot/key/source digest를 참조한다.
- Initial은 `Available`이어야 한다.
- Active는 `Available` 또는 `TemporarilyUnavailable`일 수 있다.
- claim 이전 Interrupted/Failed/Regenerated는 다시 `Available`이어야 한다.
- `PermanentlyUnavailable`은 claim 이전 어느 branch에서도 금지한다.
- Claimed와 Revisited는 같은 key의 `Claimed` 상태여야 한다.
- Claimed 이후 Available로 되돌아가 duplicate reward를 만들 수 없다.
- optional Reward는 required proof 대상이 아니지만 key가 있으면 provenance를 보존한다.
- required Reward가 하나라도 있으면 모든 대상 proof가 성공해야 aggregate safety를 publish한다.
- proof는 lifecycle 상태 계약이며 실제 reward 지급, inventory, save/load를 실행하지 않는다.

Minimum error groups:

```text
MissingInput | ContractDigestMismatch | BridgeDigestMismatch | EntryBufferDigestMismatch
InvalidFixedCell | InvalidAccessBinding | FixedAccessOverlap | DuplicateFixedOwner
InvalidReplaceableSlot | ReplaceableKindMismatch | SlotLayerOverlap
PersistenceKeyMismatch | PersistenceScopeMismatch | MissingRequiredReward
MissingCheckpoint | InvalidCheckpointState | RequiredResourcePermanentlyLost
ClaimRollback | DuplicateRewardRisk | NonCanonicalPublication
```

## 7. Focused Tests

in-memory public fixtures로 최소 다음을 검증한다.

1. FixedShell exact projection과 FixedCollision immutable ownership
2. Entry/Return/apron FixedAccess + HardProtected projection
3. Facility/Npc/Enemy/Event/Reward five-kind replaceable layer와 Entry/Return exclusion
4. replacement 전후 geometry/access/fixed/persistence identity 보존
5. Event marker-only, Facility/Npc/Enemy/Reward kind compatibility
6. required Reward의 Initial→Active 및 interrupt/fail→regenerate→Available branch
7. claim→revisit Claimed 안정성과 duplicate reward 0
8. permanent loss, missing checkpoint/key, scope/key drift, claim rollback atomic failure
9. fixed/access/slot overlap, duplicate owner, invalid occupant atomic failure
10. reverse/repeat/culture/immutability/digest와 RNG/world/tile mutation 0

SaveData, inventory, gameplay state machine 또는 MAP09 validator를 test 안에 복제하지 않는다.

## 8. Verification and Required Result

Unity refresh/compile 후 `MAP13_03` EditMode만 실행한다.

```text
discovered = executed = passed
failed / skipped / inconclusive = 0 / 0 / 0
compile / relevant Console error = 0 / 0
prior category / legacy / PlayMode / unfiltered selections = 0 / 0 / 0 / 0
```

Static gate:

```text
new Runtime C#/meta: 2/2
new focused test C#/meta: 1/1
existing C#/test/CSV/meta modifications: 0
Authoring/Generated/Scene/Prefab/Tilemap/Settings/Packages changes: 0
duplicate GUID: 0
unapplied candidate/diff-check/unrelated staged: 0/0/0
Git push: NOT PERFORMED
```

Result 경로:

```text
MapDesign/MCP/REPORTS/MAP13_03_SEPARATE_FIXED_SHELL_SLOTS_AND_PERSISTENCE_RESULT.md
```

상단 verdict:

```text
TASK: MAP13_03_SEPARATE_FIXED_SHELL_SLOTS_AND_PERSISTENCE
STATUS: PASS | BLOCKED
MAP13_03: COMPLETE ELIGIBLE | NOT COMPLETE
MAP13_04_IMPLEMENT_VILLAGE_SHELL_FACILITIES_AND_ACCESS: LOCKED / DO NOT START
```

`## User-Facing Implementation Report`에서 다음을 실제 수치로 보고한다.

- 신규/수정 script 전체 경로와 class/method별 input→output
- FixedCollision/FixedAccess/5종 slot 개수와 overlap 결과
- required Reward checkpoint/state/proof 결과
- replacement 전후 불변 digest
- 새로 가능해진 것과 파이프라인 위치
- 아직 미구현한 MAP13_04+ 기능
- Editor/게임 가시성

`## Responsibility and Added Functions`에는 아래를 표로 보고한다.

| Field | Required evidence |
|---|---|
| Task responsibility | placed fixed/access/replaceable layer + required Reward safety proof |
| Added scripts | Runtime 2 + test 1 exact paths |
| Added functions | public type/method별 sole responsibility |
| Inputs consumed | MAP09 contract + MAP13_01 bridge + MAP13_02 plan |
| Outputs produced | immutable layers/digests + checkpoint safety proof/errors |
| Explicit non-ownership | content/spawn/gameplay/SaveData/Tilemap/MAP13_04 |
| Downstream consumer | 별도 검수 후 MAP13_04만 unlock 가능 |

그 뒤 focused test, static scope, regression selections, task-owned files와 commit handoff를 기록한다.

정상 문구:

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

PASS일 때만 Status Finalize 후 task-owned 파일만 atomic commit한다.

```text
Subject: MAP13_03: separate SpecialRegion layers and persistence
Push: NOT PERFORMED
```

Result가 PASS여도 MAP13_04를 자동 시작하지 않고 별도 검수까지 LOCKED로 유지한다.
