```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP12_02_IMPLEMENT_ACTIVITY_REMOVAL_CUE_AND_SAFETY_PROOF
  task_file: TASKS/MAP12_02_IMPLEMENT_ACTIVITY_REMOVAL_CUE_AND_SAFETY_PROOF.md
  requires_current_task: NONE
  requires_completed_task: MAP12_01_IMPLEMENT_ACTIVITY_SHELL_AND_SLOT_COMPILER
  requires_result:
    path: REPORTS/MAP12_01_IMPLEMENT_ACTIVITY_SHELL_AND_SLOT_COMPILER_RESULT.md
    status: PASS
    sha256: 6bb0b0b92ff4eca5a75677ff422d3be6e69320edfc2f4aa64c2a2047ae3e7a61
  requires_installed_task:
    path: TASKS/MAP12_01_IMPLEMENT_ACTIVITY_SHELL_AND_SLOT_COMPILER.md
    sha256: 5d6fafcc2efabf7ef571bcf1a08b74f19aa8fcc3445496aad4fd3d78b7abb02b
  sets_current_task: MAP12_02_IMPLEMENT_ACTIVITY_REMOVAL_CUE_AND_SAFETY_PROOF
```

# MAP12_02 — Implement Activity Removal, Cue and Safety Proof

```text
TASK: MAP12_02_IMPLEMENT_ACTIVITY_REMOVAL_CUE_AND_SAFETY_PROOF
PHASE: MAP12 — ActivityStructure / EventOverlay
STATUS: CURRENT
NEXT: MAP12_03_IMPLEMENT_ACTIVITY_COMPATIBILITY_FREQUENCY_AND_CAPS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP12_01이 투영한 Activity shell/slot overlay를 모두 제거해도 MAP11 Static Shell과 필수 경로가 그대로 남는지 증명한다. 동시에 Cue가 Activation 이전에 관측 가능한지, SafePocket/Recovery와 Exit/Reward가 영구 파괴되지 않는지 immutable proof로 게시한다.

```text
Activity shell/slot overlay
→ Cue-before-Activation evidence
→ Active / Removed snapshots
→ Static Shell + route identity comparison
→ SafePocket / Recovery / critical-target proof
```

실제 Prefab·물리·오디오를 실행하지 않는다. MAP12_06 PlayMode fixture가 사용할 정적 proof authority를 만드는 단계다.

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 추가/수정 스크립트, 각 책임, 새 기능, 파이프라인 위치, 미구현, Editor/게임 가시성을 실제 파일 단위로 보고한다.

## 1. Scope

| 소유 | 소유하지 않음 |
|---|---|
| Cue-before-Activation 정적 증거 | 실제 AudioSource/VFX/LOS physics 실행 |
| Active/Removed overlay snapshot | 실제 Prefab 생성·Destroy 호출 |
| Static Shell/route/access identity proof | 경로 solver 또는 geometry repair |
| SafePocket/Recovery 좌표 proof | PlayerController 이동 simulation |
| Exit/Reward 영구 파괴 금지 proof | reward 지급·저장·respawn 실행 |

빈도/cap/candidate/RNG는 MAP12_03, EventOverlay는 MAP12_04, production content는 MAP12_05, 실제 PlayMode 제거/중단/재진입은 MAP12_06 소유다.

## 2. Focused-Only Policy

정상 실행:

```text
MAP12_02 EditMode: required
MAP09/MAP10/MAP11/MAP12_01 categories: 0
legacy 19347: 0
PlayMode/unfiltered: 0/0
```

기존 public API 호출은 과거 category 재실행이 아니다. upstream defect면 owner/invariant/원인/최소 검증 범위를 기록하고 기존 파일을 수정하지 않은 채 `BLOCKED`로 STOP한다. Task-owned 신규 파일 문제는 그 파일과 `MAP12_02`만 수정·재실행한다.

## 3. Preflight

```text
MAP12_01 Result: PASS
Result SHA-256: 6bb0b0b92ff4eca5a75677ff422d3be6e69320edfc2f4aa64c2a2047ae3e7a61
installed Task SHA-256: 5d6fafcc2efabf7ef571bcf1a08b74f19aa8fcc3445496aad4fd3d78b7abb02b
MAP12_01 COMPLETE / MAP12_02 CURRENT / MAP12_03 LOCKED
inbox candidate / unrelated staged: 0/0
Unity compile / relevant Console error: 0/0
```

Approved representative evidence:

```text
cluster/variant: TC_CRATER_BOWL_ASCENT / SPINE_CRATER_BOWL_ASCENT_BASE
Activity shell artifact:
228afb7f7a4b351d9f9706c039cb0681babaa7830de5e48a5061d962f3ebebe9
zones / projected cells / slots: 4 / 10 / 9
cue / mechanism / progression bindings: 1 / 8 / 7
Static Shell / working Canvas: 288 / 288
geometry writes/changes: 0/0
```

Current MAP11 authority remains:

```text
catalog: 9d26786af477731d57503f16cc899210da6636f48dfb0542791e8fa591bd3bf7
signature-set: 2884a639d9cef923e8b86a7fba2c0430cdfad2de11a63fd138d51dacdce13d8a
Authoring manifest: ff4761537986a4c9433775359d9b62ad806914ef30462a320c97b355126a5b6c
```

Required inputs are validated `ActivityStructureContract`/`ActivityRemovalSafety`, successful `ActivityShellCanvas`, and the same MAP11 LocalCanvas/traversal/route/StaticShell/working-Canvas chain. Drift나 missing reference면 `BLOCKED`다.

## 4. Exact Files

정상 범위는 신규 Runtime 2개와 focused test 1개 및 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/Activities/ActivityRemovalSafetyProof.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Activities/ActivityRemovalSafetyCompiler.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Activities/ActivityRemovalSafetyCompilerTests.cs(.meta)
```

```text
Runtime: Game.Map.Runtime / StarNight.Map.WorldGeneration.Activities
Tests: Game.Map.Tests.EditMode / StarNight.Map.Tests.EditMode.WorldGeneration.Activities
Category: MAP12_02
```

기존 C#/test/CSV/meta/asmdef, Authoring/Generated, Scene/Prefab/SO/Tilemap/Audio/Settings/Packages는 수정하지 않는다. 책임 분리가 불가능할 때만 같은 Runtime folder helper 1개를 허용하고 이유를 Result에 보고한다.

## 5. Public Model

이름은 style에 맞출 수 있으나 동등한 surface를 제공한다.

```text
ActivityCueObservationEvidence
ActivityCueObservationProof
ActivityOverlaySnapshotKind: Active / Removed
ActivityOverlayRemovalIntent
ActivityOverlaySnapshot
ActivitySafePocketProof
ActivityRecoverySafetyProof
ActivityCriticalTargetKind: MandatoryExit / Reward
ActivityCriticalTargetEvidence
ActivityCriticalPreservationProof
ActivityRemovalSafetyProof
ActivityRemovalSafetyCompileRequest / Result / ErrorCode / Error
ActivityRemovalSafetyCompiler.Compile
```

모든 collection은 defensive-copy/read-only/canonical order다. 실패 시 proof/snapshots/cue/safe/recovery/critical collections/digest는 모두 0/null이다.

## 6. Cue-Before-Activation Proof

caller는 validated Activity cue마다 observation evidence를 제공한다.

```text
Cue ID/kind/slot
baseline witness observation edge ID
activation boundary edge ID
observation source-local coordinate
maximum observation distance in tiles (>0)
```

compiler는 traversal/path solver나 physics raycast를 만들지 않는다. MAP11의 ordered baseline witness와 final working Canvas를 사용하며 Visual/Motion에만 task-owned deterministic grid supercover 검사를 적용한다.

- cue/slot/binding은 MAP12_01 output과 exact 일치한다.
- observation/activation edge는 baseline witness source edge다.
- observation edge ordinal은 activation boundary보다 앞이다.
- observation coordinate는 해당 edge의 published centerline/clearance/landing evidence이며 active Air다.
- cue slot까지 Manhattan distance는 caller의 positive maximum 이하다.
- `Visual`/`Motion` cue는 existing working-Canvas cell을 따라 deterministic grid supercover를 검사하고 중간 Solid가 0이어야 한다.
- `Audio`/`Environment` cue는 distance proof만 사용하며 physics/audio attenuation을 추정하지 않는다.
- Cue가 Activation과 같은/뒤 edge이거나 blocked/out-of-range이면 실패한다.

고정 gameplay range나 balance threshold는 만들지 않는다. 실제 시야·음향 재생은 MAP12_06 소유다.

## 7. Active and Removed Snapshots

Active snapshot은 MAP12_01의 zone/slot/binding overlay ID와 coordinate를 canonical하게 포함한다. Caller의 `ActivityOverlayRemovalIntent`는 제거할 모든 overlay identity, declared permanent tile mutation과 critical-target destruction 여부를 명시한다. exact 전체 set만 허용하며 missing/extra/duplicate identity와 어떤 destructive declaration도 거부한다. Removed snapshot은 검증된 intent를 적용해 Activity-owned overlay를 exact 0개로 만든 상태다.

두 snapshot 모두 같은 immutable MAP11 working Canvas/Static Shell/traversal/route/access digests를 참조한다.

필수 proof:

```text
active overlay count = zones + slots + bindings의 canonical identities
removed overlay count = 0
residual Activity overlay = 0
underlying tile/occupancy delta = 0
Static Shell digest before/removed = equal
working Canvas digest before/removed = equal
Traversal/route/access identity before/removed = equal
renderer invocation / geometry write / carve = 0/0/0
```

이는 실제 `Destroy(prefab)` 호출이 아니라 제거 가능한 data overlay proof다. Scene/Prefab lifecycle은 만들지 않는다.

## 8. SafePocket and Recovery Proof

existing `ActivityRemovalSafety.SafePocket`와 `Recovery` coordinates를 MAP11/MAP12_01 artifacts에 대조한다.

- 두 set은 non-empty/unique/active bounds다.
- 모든 coordinate는 final working Canvas에서 Air이며 Activity 제거 후에도 Air다.
- SafePocket은 Core hazard/device/projectile slot coordinate와 겹치지 않는다.
- Recovery set은 MAP11 traversal의 authored Recovery envelope 또는 recovery witness coordinate/provenance로 설명돼야 한다.
- 최소 한 SafePocket은 baseline 또는 recovery witness의 published open evidence에 연결된다.
- recovery witness는 source edge only, synthetic/teleport 0, duration `2000..5000 ms`다.
- proof는 좌표와 source provenance를 게시하고 새 경로를 추론하거나 carve하지 않는다.

## 9. Exit and Reward Preservation

caller의 `ActivityCriticalTargetEvidence`와 current artifacts를 검증해 exact 두 kind proof를 게시한다.

```text
MandatoryExit: TerrainCluster primary Exit port/role/node/witness
Reward: Activity Reward slot/progression binding
```

- Exit target은 removed snapshot에서도 same identity/coordinate/digest다.
- Reward target은 underlying tile과 binding identity가 보존되고 permanent-destruction declaration이 없다.
- `PermanentSolidMutationAllowed = false`, `MandatoryExitDestructionAllowed = false`, `PreserveStaticTraversal = true`, `PreserveAccessClass = true`를 existing contract에서 확인한다.
- 어떤 Activity-owned overlay removal도 Exit/Reward underlying coordinate를 삭제·Solid화·carve하지 않는다.
- reward 지급/획득/respawn persistence는 구현하지 않는다.

## 10. Digest and Atomic Errors

digest는 ruleset, Activity/MAP12_01/MAP11 digests, cue observations, active/removed snapshots, safe/recovery coordinates/provenance, Exit/Reward proof와 before/removed identities를 포함한다. locale/time/input order/object identity/Prefab/runtime/audio/RNG state는 제외한다.

최소 error groups:

```text
MissingInput | IdentityMismatch | ArtifactDigestMismatch
MissingCueEvidence | InvalidCueEvidence | CueNotBeforeActivation
CueOutOfRange | CueOccluded | InvalidObservationCoordinate
InvalidActiveSnapshot | ResidualOverlay | StaticShellChanged
TraversalChanged | AccessChanged | WorkingCanvasChanged
InvalidSafePocket | UnsafePocketOverlap | InvalidRecoveryEvidence
RecoveryDurationOutOfRange | MissingCriticalTarget
ExitDestructionDeclared | RewardDestructionDeclared
PermanentMutationDeclared | NonCanonicalPublication
```

errors는 accumulated/deduplicated/stable-sorted이며 any error는 atomic zero publication이다.

## 11. Focused Fixture and Tests

MAP12_01과 같은 real physical chain을 사용한다.

```text
TC_CRATER_BOWL_ASCENT / actual baseline variant
validated test-owned Activity contract
successful MAP12_01 ActivityShellCanvas
```

test-owned evidence만 새로 만들고 기존 production/CSV/test fixture를 수정하지 않는다.

`MAP12_02` category에서 검증:

1. valid Cue-before-Activation observation
2. Visual/Motion clear supercover와 Audio/Environment distance proof
3. cue same/after activation, occluded, out-of-range atomic failure
4. Active→Removed overlay exact zero와 upstream digest equality
5. residual overlay/permanent mutation/static/traversal/access drift atomic failure
6. SafePocket active-Air/non-Core overlap와 witness connection
7. Recovery provenance/source-edge/duration `2000..5000`
8. Exit/Reward identity와 underlying coordinate preservation
9. destructive Exit/Reward declarations atomic failure
10. repeat/reversed input/`tr-TR` digest determinism
11. public immutability와 failure zero publication
12. Prefab/Scene/physics/audio/RNG/file-write side effect 0

## 12. Static Gates

```text
Unity compile / Console error / warning: 0/0/0
MAP12_02 discovered = executed = passed; fail/skip/inconclusive 0
new Runtime C#/meta: 2/2
new focused test C#/meta: 1/1
existing C#/test/CSV/meta changes: 0
Authoring/Generated changes: 0/0
asmdef/Scene/Prefab/Tilemap/Audio/Settings/Packages changes: 0
MAP11 approved digest drift: 0/0/0
MAP12_01 artifact/source modification: 0/0
duplicate GUID: 0
inbox/diff-check/unrelated staged: 0/0/0
prior/legacy/PlayMode/unfiltered selections: 0/0/0/0
Git push: NOT PERFORMED
```

initialization/import timeout으로 executed 0이면 PASS로 세지 않고 같은 category만 재시도한다.

## 13. Required Result

```text
MapDesign/MCP/REPORTS/MAP12_02_IMPLEMENT_ACTIVITY_REMOVAL_CUE_AND_SAFETY_PROOF_RESULT.md
```

상단:

```text
TASK: MAP12_02_IMPLEMENT_ACTIVITY_REMOVAL_CUE_AND_SAFETY_PROOF
STATUS: PASS | BLOCKED
MAP12_02: COMPLETE ELIGIBLE | NOT COMPLETE
MAP12_03_IMPLEMENT_ACTIVITY_COMPATIBILITY_FREQUENCY_AND_CAPS: LOCKED / DO NOT START
```

`## User-Facing Implementation Report`에서 파일/책임/새 기능/파이프라인/미구현/가시성을 먼저 보고하고, `## Responsibility and Added Functions`에서 inputs/outputs/non-ownership/downstream을 명시한다.

이후 actual evidence:

- file/class/public surface
- representative cluster/activity/shell identity
- cue kind/observation/activation/distance/occlusion counts
- Active/Removed overlay와 residual/delta counts
- SafePocket/Recovery coordinates와 witness/duration
- Exit/Reward preservation
- digest/determinism/negative matrix
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

PASS일 때만 MAP12_02를 Finalize하고 task-owned 파일만 atomic commit한다.

```text
Subject: MAP12_02: prove activity removal cue and safety
Push: NOT PERFORMED
```

Result가 PASS여도 MAP12_03을 자동 시작하지 않는다. 별도 검수 전까지 계속 LOCKED다.
