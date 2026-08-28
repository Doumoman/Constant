```yaml
mcp_repair:
  format: current_task_repair_v1
  repair_id: MAP11_05R_REPAIR_PATTERN_ZONE_RENDER_TARGET_CONTRACT
  repairs_current_task: MAP11_05_IMPLEMENT_CLUSTER_PATTERN_ZONES_AND_RENDERER
  requires_current_task: MAP11_05_IMPLEMENT_CLUSTER_PATTERN_ZONES_AND_RENDERER
  requires_blocked_result:
    path: REPORTS/MAP11_05_IMPLEMENT_CLUSTER_PATTERN_ZONES_AND_RENDERER_RESULT.md
    status: BLOCKED
    sha256: 7db4cadeb6ec07f60c6e8654cdb47d2f7cda27f0fbb74b9a83ad09d3bc66e076
  requires_installed_task:
    path: TASKS/MAP11_05_IMPLEMENT_CLUSTER_PATTERN_ZONES_AND_RENDERER.md
    sha256: 45bde171c3357c8c9c5f2776566f2e55f4a17cba2d3978323e0a05636a2623b8
  preserves_current_task: MAP11_05_IMPLEMENT_CLUSTER_PATTERN_ZONES_AND_RENDERER
  next_task_remains_locked: MAP11_06_IMPLEMENT_QUIET_BUFFER_CLUSTER_POOL
```

# MAP11_05R — Repair Pattern Zone / Render Target Contract

```text
REPAIR: MAP11_05R_REPAIR_PATTERN_ZONE_RENDER_TARGET_CONTRACT
CURRENT TASK: MAP11_05_IMPLEMENT_CLUSTER_PATTERN_ZONES_AND_RENDERER
STATUS EFFECT: NONE — MAP11_05 stays CURRENT
NEXT: MAP11_06_IMPLEMENT_QUIET_BUFFER_CLUSTER_POOL stays LOCKED
```

## 0. Why This Repair Exists

MAP11_05 preflight correctly found two contradictions in the installed Task.

```text
ShellSolid = MAP11_03 Floor
MAP11_03 Floor ⊆ AbsoluteProtected
ShellSolid ∩ NOT AbsoluteProtected = empty
```

Therefore the original rule “GeometryCarve must begin on an unprotected Static Shell Solid” cannot produce one legal coordinate.

The original Task also required the MAP10 render target to contain the entire active Static Shell. MAP10_03 correctly accepts only the application-plan coordinate union and reports every additional target cell as `ExtraTargetCell`.

This addendum repairs only those two MAP11_05 specification errors. It does not reopen, edit, or reinterpret MAP10, MAP11_03, or MAP11_04 authority.

## 1. Apply / Audit Procedure

This is not a new Master Task and must not run the normal `NONE → CURRENT` single-task open flow.

Preflight must verify:

1. Current Task is exactly `MAP11_05_IMPLEMENT_CLUSTER_PATTERN_ZONES_AND_RENDERER`.
2. MAP11_05 Status row is `CURRENT`; MAP11_06 is `LOCKED`.
3. The BLOCKED Result exists with exact status and SHA from metadata.
4. The installed original MAP11_05 Task has the exact SHA from metadata.
5. No other unapplied inbox candidate exists.
6. No unrelated staged path exists.

Install this repair byte-identically as an audit addendum:

```text
MCP/TASKS/MAP11_05R_REPAIR_PATTERN_ZONE_RENDER_TARGET_CONTRACT.md
MCP_ARCHIVE/MAP11_05R_REPAIR_PATTERN_ZONE_RENDER_TARGET_CONTRACT.md
```

Move/remove the inbox source after both copies match its SHA. Do not edit Master or Status during repair installation. Do not replace the installed original MAP11_05 Task. The original Task plus this repair addendum together are the effective specification.

Any preflight, collision, byte, SHA, or state mismatch is `BLOCKED` with zero implementation change.

## 2. Source-of-Truth Decision A — Carve Substrate

Replace only the original MAP11_05 rule:

```text
OLD: GeometryCarve requires Static Shell Solid and unprotected.
```

with:

```text
NEW: GeometryCarve identifies active, unprotected cells that are seeded Solid
     in the cluster pattern working substrate before MAP10 rendering.
```

Exact semantics:

1. MAP11_04 Static Shell remains immutable and byte/semantic-identical.
2. Begin the full cluster working canvas from the exact MAP11_04 Static Shell active-cell union.
3. For each valid `GeometryCarve` zone cell, set the pre-render working geometry to `Solid=true` with explicit `GeometryCarveSubstrate` provenance.
4. This substrate write is cluster zone initialization, not a MicroPattern renderer write and not a mutation of the Static Shell.
5. `GeometryCarve` cells must be active, inside Local Canvas, and outside `AbsoluteProtected`.
6. A `GeometryCarve` cell may begin as Static Shell Air. That is the expected normal case.
7. A `GeometryCarve` cell overlapping Static Shell Solid is invalid because every current Static Shell Solid is protected and the protection-overlap gate rejects it first.
8. A selected pattern may apply `CarveAir` only to a `GeometryCarve` zone cell. Successful `CarveAir` changes the seeded substrate from Solid to Air.
9. `GeometryAdd` cells keep their Static Shell geometry and must be pre-render Air; `AddSolid` may change them to Solid.
10. Cells outside GeometryAdd/GeometryCarve retain their Static Shell geometry.

The pre-render full working canvas is therefore:

```text
MAP11_04 Static Shell geometry
+ nonprotected GeometryCarve Solid substrate overlay
+ empty/default Surface/Affordance/Material/Hazard/Marker layers
```

Canonical provenance must distinguish at least:

```text
StaticShellAir
StaticShellSolid
GeometryCarveSubstrate
```

The cluster pattern digest includes every substrate coordinate and provenance. Reversed authored zone enumeration produces the same substrate and digest.

## 3. Source-of-Truth Decision B — MAP10 Target Boundary

Replace only the original MAP11_05 rule:

```text
OLD: initial MAP10 render target is the complete Static Shell active-cell union.
```

with:

```text
NEW: MAP10 render target is exactly the canonical union of target coordinates
     published by all successful MAP10_02 application plans in the batch.
```

Exact procedure:

1. Build and retain the full immutable pre-render cluster working canvas from Section 2.
2. Produce all successful MAP10_02 application plans.
3. Compute the exact unique union of their target coordinates.
4. Every plan target coordinate must exist in the full active working canvas; otherwise atomic failure.
5. Construct the MAP10_03 target using only those union coordinates, copying their current six-layer values and provenance from the full pre-render working canvas.
6. Do not create filler, synthetic, or implicit `NoChange` placements.
7. Call the existing MAP10_03 ordered renderer unchanged.
8. Apply only the successful renderer delta to a defensive copy of the full working canvas.
9. Publish the full final working canvas with exact active-cell coverage; coordinates outside the plan union remain byte/semantic-identical to pre-render state.
10. `ExtraTargetCell` behavior in MAP10_03 remains unchanged and must still be covered by MAP10 authority, not bypassed.

The final MAP11_05 report must distinguish:

```text
full working canvas coordinate count
MAP10 plan-union target coordinate count
untouched full-canvas coordinate count
renderer delta coordinate count
```

## 4. Unchanged Absolute Protection Rules

All original protection rules remain binding.

- AbsoluteProtected is the canonical union of MAP11_03 Spine/Envelope and MAP11_04 witness/anchor evidence.
- Authored GeometryAdd/GeometryCarve/Affordance/Marker overlap with AbsoluteProtected is an error.
- `ForceNoChange` produces protected renderer write/change count 0.
- `RejectCandidate` produces atomic failure with partial output 0.
- Witness-only protection without corresponding MAP11_03 evidence remains an error.
- No MAP10 protected enum or authority file may be modified.

The GeometryCarve substrate must never seed or change an AbsoluteProtected coordinate.

## 5. Unchanged Operation Permissions

```text
GeometryAdd:    AddSolid only
GeometryCarve:  CarveAir only
Affordance:     SetAffordance only
Marker:         SetMarker only
AbsoluteProtected / unzoned: non-NoChange forbidden
Surface / Material / Hazard: unsupported in MAP11_05
```

Affordance and Marker may overlap either geometry zone when the coordinate is active and unprotected. GeometryAdd and GeometryCarve remain mutually exclusive.

## 6. Required Implementation Scope

Implement the original MAP11_05 allowlist and responsibilities using this repaired contract.

Expected new files remain:

```text
Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/TerrainClusterPatternZone.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/TerrainClusterPatternRenderer.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/TerrainClusters/TerrainClusterPatternRendererTests.cs(.meta)
```

One additional new Runtime model file remains allowed only when responsibility separation requires it and must be reported.

Required reuse remains:

- MAP10_01 validated definition/catalog
- MAP10_02 transformer/protected-mask/application planner
- MAP10_03 ordered renderer
- MAP11_03 protection evidence
- MAP11_04 Static Shell and route witnesses

No existing MAP10/MAP11_01~04 production/test/meta/CSV file may be modified.

## 7. Repaired Focused Verification

Run category `MAP11_05` only. Minimum cases:

1. GeometryCarve zone on unprotected Static Shell Air seeds Solid substrate.
2. CarveAir changes seeded Solid to Air and publishes renderer delta/provenance.
3. GeometryAdd begins Air and AddSolid changes it to Solid.
4. substrate initialization never mutates MAP11_04 Static Shell.
5. substrate/protection overlap rejects atomically.
6. full working canvas has exact active-cell coverage.
7. MAP10 target equals exact successful application-plan coordinate union.
8. MAP10 target may be a strict subset of full active canvas without `ExtraTargetCell`.
9. coordinates outside plan union remain unchanged in final full canvas.
10. multiple overlapping plan footprints canonicalize target union.
11. out-of-active-canvas plan coordinate rejects atomically.
12. actual MAP10_02 planner and MAP10_03 renderer are used unchanged.
13. ForceNoChange and RejectCandidate protection behavior remains exact.
14. Add/Carve/Affordance/Marker permission and unsupported-layer rejection remain exact.
15. identical write coalescing and conflicting write rejection remain exact.
16. reversed inputs/culture produce same zone/substrate/canvas/digest.
17. semantic substrate/placement change changes digest.
18. accumulated failure exposes no partial zone/plan/delta/canvas/digest.
19. RNG/cleanup/quiet-buffer/starter/sector/Tilemap side effects remain 0.

Normal verification:

```text
MAP11_05 focused selection: required
Prior MAP09/MAP10/MAP11_01~04 selections: 0
Legacy 19347 selections: 0
PlayMode selections: 0
```

This BLOCKED Result is the regression trigger record. The minimum corrected scope is compile/Console plus `MAP11_05` focused only. Do not run a prior authority category merely because its API is consumed.

## 8. Exact Non-Ownership

Still forbidden:

- existing MAP09/MAP10/MAP11_01~04 implementation/test/CSV/meta changes
- MAP10 target validation relaxation or `ExtraTargetCell` change
- MAP11_03 protection reduction
- MAP11_04 Static Shell expansion/mutation
- transform/planner/renderer duplication
- candidate selection, biome weight, RNG
- repetition cleanup, density repair
- quiet buffer pool and starter 16 clusters
- Activity/Event/SpecialRegion/sector/world assembly
- Slice/Tilemap/Scene/Prefab/SO/PlayMode
- asmdef/asmref/Settings/Packages
- unrelated change/stage/commit and Git push

## 9. Required PASS Result

Rewrite the same current-task Result path after the repaired implementation:

```text
REPORTS/MAP11_05_IMPLEMENT_CLUSTER_PATTERN_ZONES_AND_RENDERER_RESULT.md
```

Header:

```text
TASK: MAP11_05_IMPLEMENT_CLUSTER_PATTERN_ZONES_AND_RENDERER
STATUS: PASS | BLOCKED
MAP11_05: COMPLETE ELIGIBLE | NOT COMPLETE
MAP11_06_IMPLEMENT_QUIET_BUFFER_CLUSTER_POOL: LOCKED / DO NOT START
```

The first section must remain Korean `## User-Facing Implementation Report` and explicitly report:

| 필드 | 필수 내용 |
|---|---|
| 추가된 스크립트 | 모든 신규 C#과 각 책임 |
| 새로 가능해진 기능 | zone, carve substrate, protection, MAP10 render, full canvas |
| 실제 파이프라인 위치 | MAP11_04 입력과 MAP11_06~07 소비 관계 |
| 아직 안 된 것 | RNG/cleanup/quiet buffer/content/sector/Tilemap |
| 게임에서 보이는 시점 | 현재 working-canvas 데이터인지 화면 출력인지 |

Then include `## Responsibility and Added Functions` and report actual public types/functions, inputs, outputs, explicit non-ownership, and downstream consumers.

Also report:

```text
Original MAP11_05 Task SHA
MAP11_05R repair SHA
prior BLOCKED Result SHA
Static Shell Solid/Air counts
GeometryCarve substrate coordinate count
full working canvas / MAP10 target union / untouched / delta counts
AbsoluteProtected write/change counts
MAP10 plan/render and MAP11_05 report digests
MAP11_05 focused discovered/executed/pass/fail/skip/inconclusive
REGRESSION TRIGGER owner/reason/minimum scope
PRIOR TASK TEST SELECTIONS
LEGACY TEST SELECTIONS
PLAYMODE TEST SELECTIONS
```

PASS일 때만 기존 MAP11_05 Status를 Finalize하고 original Task, repair addendum, task-owned code/test/meta, PASS Result, Status만 atomic commit한다.

```text
Subject: MAP11_05: implement cluster pattern zones and renderer
Push: NOT PERFORMED
```

PASS여도 MAP11_06은 자동 시작하지 않고 STOP한다.
