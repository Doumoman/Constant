---
protocol: direct_visible_map_work_order_v6
task_id: RUN06_TUNE_GENERATED_RUN_SHAPES_AND_REMOVE_BAD_VARIANTS
requires_result:
  path: MapDesign/MCP/REPORTS/RUN05_CREATE_SEED_REGENERATE_AND_VARIANT_SELECTION_TOOL_RESULT.md
  status: PASS
  sha256: 1ac68e5f06c5837f95ef572b5f65c62a59653cc87c20c23feeac6ffb827fe831
requires_completed_task: RUN05_CREATE_SEED_REGENERATE_AND_VARIANT_SELECTION_TOOL
next_task: RUN07_BUILD_PLAYABLE_CAMERA_ROOM_SAMPLE_WITH_GAME_PLAYER
---

# RUN06 - Tune Generated Run Shapes and Remove Bad Variants

```text
TASK: RUN06_TUNE_GENERATED_RUN_SHAPES_AND_REMOVE_BAD_VARIANTS
STATUS: LOCKED
EXPECTED_RESULT: RUN06_TUNE_GENERATED_RUN_SHAPES_AND_REMOVE_BAD_VARIANTS_RESULT.md
NEXT: RUN07_BUILD_PLAYABLE_CAMERA_ROOM_SAMPLE_WITH_GAME_PLAYER
```

## User-Facing Goal

RUN05 lets the user choose a recipe and seed, regenerate a direct 4x4 MicroPattern camera-room run, and compare four generated courses.

RUN06 must make that useful for actual map design by filtering out bad-looking or low-quality generated runs.

```text
RUN05 seed/recipe generator
-> quality rules for visible map shape
-> bounded curation batch
-> accepted and rejected examples
-> tuned recipe constraints
-> Unity curation gallery Scene
```

The final output is not a PNG.
The final output is not a full world.
The final output is not a report-only task.

The user must be able to open Unity and see which generated runs are accepted, which are rejected, and why.

## Required Final Unity Outputs

Create these Unity-facing outputs:

```text
Assets/_Game/Map/Scenes/MoonPalace/RUN06/MoonPalaceCuratedRunGallery_RUN06.unity
Assets/_Game/Map/Editor/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceRunCurationWindow.cs
```

The Scene must contain:

```text
root: MoonPalace_CuratedRunGallery_RUN06
child: AcceptedGallery
child: RejectedGallery
child: QualityLegend
child: RuleOverlay
child: SeedLabels
child: Metadata
```

The Editor Window must be accessible from a Unity menu item:

```text
Tools/MoonPalace/Run Curation
```

## What This Task Adds

RUN06 adds a visible quality gate for generated camera-room runs.

Required behavior:

1. Generate a bounded curation batch from the RUN05 recipes.
2. Score each generated run with explicit visible-shape rules.
3. Accept good generated runs.
4. Reject bad generated runs with a concrete reason.
5. Build one RUN06 Scene showing accepted and rejected examples side by side.
6. Add a small curation window to rebuild the gallery and inspect rejection reasons.
7. Tune recipe constraints only where the batch proves the old range creates visibly poor shapes.
8. Preserve RUN05 generation and RUN04 preview behavior for accepted runs.

This task should make the generated maps feel less arbitrary.
It must not hide bad outputs by silently repairing them.

## Scope Lock

### Required

| Required output | Contract |
| --- | --- |
| Curation rules | Explicit shape-quality rules with reason codes |
| Curation batch | At least 36 generated runs across all recipes |
| Accepted gallery | At least 9 accepted generated runs visible |
| Rejected gallery | At least 6 rejected generated runs visible with reasons |
| Recipe tuning | Small, reported adjustments only when justified by failures |
| Editor Window | Rebuild/inspect curation from Unity |
| Focused tests | RUN06 EditMode category only |

### Explicitly Not Required

| Not required | Reason |
| --- | --- |
| Final production art | RUN06 is debug curation, not art pass |
| Final player physics | Accepted runs remain RUN04-style preview compatible |
| Full world generation | Curation is over camera-room runs only |
| Large statistical QA | This is a bounded design batch, not large-scale release QA |
| Automated PlayMode test selection | Not needed for this curation task |
| Android/player build | Not needed for editor inspection |

## Prerequisite Check

Before doing any work, verify:

```text
RUN05 Result exists and STATUS: PASS
RUN05 Result SHA-256:
1ac68e5f06c5837f95ef572b5f65c62a59653cc87c20c23feeac6ffb827fe831
```

Expected RUN05 baseline:

```text
Editor menu item: Tools/MoonPalace/Run Preview Generator
recipes: WIDE_BRANCH_RUN, TALL_LOOP_RUN, COMPACT_SPLIT_RUN
candidate pool count: 500
active generation direct placements: 2304
SeedGallery course count: 4
focused tests: 18 / 18 PASS
legacy 19347 selections: 0
automated PlayMode selections: 0
RUN06 files/runs: 0 / 0
RUN05 commit hash: 7f0b7bbce71491281462d9ffca7829a91abb1cf3
```

If the RUN05 Result is missing, not PASS, or has different bytes, stop as BLOCKED unless the only difference is a file relocation with identical canonical content.

If RUN05 finalize/commit is still pending, finish only RUN05 finalize/commit first.
Then start RUN06.

## Allowed Files

Keep edits scoped to RUN06, plus narrowly justified RUN05 recipe constraint changes.

### Runtime

```text
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceRunQualityRule.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceRunQualityProfile.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceRunQualityAnalyzer.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceRunCurationBatch.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceCuratedRunSet.cs
```

### Existing Runtime That May Be Minimally Changed

```text
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceSeededRunRecipeCatalog.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceSeededRunRecipe.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceSeededRunGenerator.cs
```

Allowed changes to these existing files:

```text
add small quality-related recipe fields
tighten a recipe range that RUN06 proves produces bad shapes
expose read-only data needed by curation
```

Forbidden changes to these existing files:

```text
replace the RUN05 generator architecture
remove determinism guarantees
add silent repair
add full-world generation
start using 48x32 sector as primary dimensions
```

### Editor

```text
Assets/_Game/Map/Editor/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceRunCurationWindow.cs
Assets/_Game/Map/Editor/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceCuratedRunGallerySceneBuilder.cs
```

### Tests

```text
Assets/_Game/Map/Tests/EditMode/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceRunShapeCurationTests.cs
```

### Generated Scene and Artifacts

```text
Assets/_Game/Map/Scenes/MoonPalace/RUN06/**
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN06/**
MapDesign/MCP/GENERATED/RUN06/**
MapDesign/MCP/TASKS/RUN06_TUNE_GENERATED_RUN_SHAPES_AND_REMOVE_BAD_VARIANTS.md
MapDesign/MCP/REPORTS/RUN06_TUNE_GENERATED_RUN_SHAPES_AND_REMOVE_BAD_VARIANTS_RESULT.md
MapDesign/MCP_ARCHIVE/RUN06_TUNE_GENERATED_RUN_SHAPES_AND_REMOVE_BAD_VARIANTS.md
```

### Existing Files That May Be Read

```text
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceSeededRun*.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceRunPreview*.cs
MapDesign/MCP/GENERATED/RUN05/**
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN05/**
```

## Forbidden

Do not do any of the following:

```text
legacy 19347 selection
full or unfiltered test run
automated PlayMode test selection
player build
Build Settings mutation
existing Scene/Prefab mutation outside RUN06
full world generation
48x32 sector generator as primary output
static screenshot/PNG as completion proof
silent carve or fallback tunnel repair
90-degree MicroPattern rotation
reroll-until-valid infinite loop
production player controller replacement
combat/NPC/shop/save integration
RUN07 or later task start
git push
```

Bounded deterministic attempts are allowed only for generation and candidate selection.
Rejected outputs must be visible and reported.
Do not delete evidence of rejected runs.

## Quality Rule Requirements

Create explicit quality rules.

Required rule categories:

| Rule id | Purpose | Reject when |
| --- | --- | --- |
| `ROUTE_TOO_STRAIGHT` | Avoid long flat corridor feeling | main route has too many consecutive same-row rooms or low turn count |
| `ROOM_TOO_EMPTY` | Avoid huge hollow boring rooms | open ratio exceeds recipe threshold without branch/route structure |
| `ROOM_TOO_NOISY` | Avoid unreadable visual clutter | tiny isolated solid/open fragments exceed threshold |
| `BRANCH_TOO_SHORT` | Avoid decorative one-step branches | branch route length below minimum |
| `BRANCH_TOO_DEEP` | Avoid branches that feel like main route by accident | branch route length exceeds max ratio |
| `CONNECTOR_CROWDING` | Avoid many gates stacked too close | connector gates too near each other in one room |
| `VERTICAL_VARIATION_LOW` | Avoid maps that ignore verticality | vertical span below recipe threshold |
| `BACKTRACK_SHAPE_BAD` | Avoid split/rejoin that looks like noise | split path separation or rejoin distance below threshold |
| `OPEN_ISLAND_REQUIRED` | Required paths must not form unreachable islands | any required room/route/connector tile unreachable |
| `REPAIR_POLICY_VIOLATION` | Keep generator honest | fallback carve, silent repair, or rotation count is nonzero |

Each rule must produce:

```text
rule id
severity: info | warning | reject
measured value
threshold
affected room/connector/route ids when available
human-readable reason
```

## Curation Batch Requirements

Generate a bounded deterministic batch.

Minimum:

```text
recipe count: 3
seeds per recipe: 12
total generated runs: at least 36
accepted runs: at least 9
rejected runs: at least 6
```

Required seed range:

```text
1924737067 through 1924737078
```

For each recipe and seed:

```text
generate course
validate basic reachability
run quality analyzer
assign ACCEPTED or REJECTED
record primary reason
record all rule findings
record digests
```

If fewer than 9 runs are accepted, tune recipe constraints conservatively and rerun the same bounded batch.
If fewer than 6 runs are rejected, broaden visible quality checks enough to expose weak shapes, but do not invent fake failures.

The goal is not 100% acceptance.
The goal is to see both good and bad examples and understand why.

## Recipe Tuning Requirements

Only tune recipe constraints that affect visible structure.

Allowed tuning examples:

```text
minimum vertical span
minimum branch length
maximum branch length ratio
minimum split/rejoin separation
connector spacing
room aspect ratio limits
open ratio range
turn count target
```

Forbidden tuning examples:

```text
hardcode accepted seed ids as the generator result
delete candidate masks from the 500 pool without a separate pattern-library task
silently carve route fixes
force all recipes into the same shape
reduce every output into one safe straight route
```

If recipe fields are changed, report:

```text
old value
new value
why it changed
which rejected examples motivated it
how many outputs changed from rejected to accepted
```

## Editor Window Requirements

Create `MoonPalaceRunCurationWindow`.

It must provide:

```text
menu item: Tools/MoonPalace/Run Curation
button: Run Curation Batch
button: Build Curation Gallery Scene
button: Select Next Rejected
button: Select Next Accepted
summary area: generated / accepted / rejected counts
summary area: top rejection reasons
summary area: selected recipe, seed, digest, primary finding
link or ping target for RUN06 Scene if possible
```

It must call the batch, analyzer, and scene builder directly.

It must not:

```text
run broad tests
run player build
modify Build Settings
write outside RUN06 paths
start RUN07
```

## Scene Builder Requirements

Create `MoonPalaceCuratedRunGallerySceneBuilder`.

It must create or replace only:

```text
Assets/_Game/Map/Scenes/MoonPalace/RUN06/MoonPalaceCuratedRunGallery_RUN06.unity
```

Scene requirements:

```text
root GameObject: MoonPalace_CuratedRunGallery_RUN06
AcceptedGallery shows at least 9 accepted courses
RejectedGallery shows at least 6 rejected courses
QualityLegend explains color coding
RuleOverlay marks rejection hotspots when available
SeedLabels show recipe, seed, ACCEPTED/REJECTED, primary reason, digest
Metadata shows batch seed range, rule profile digest, accepted count, rejected count
```

Required visual encoding:

```text
accepted run frame: green
rejected run frame: red
warning finding: yellow marker
reject finding: red marker
main route: green
branch route: blue
split/rejoin route: gold
connector gates: magenta or cyan
```

The gallery does not need to be manually traversable.
At least one accepted run must be marked as the recommended active preview for RUN07.

## Generated Artifacts

Create these JSON files:

```text
MapDesign/MCP/GENERATED/RUN06/moonpalace_run06_quality_profile.json
MapDesign/MCP/GENERATED/RUN06/moonpalace_run06_curation_batch.json
MapDesign/MCP/GENERATED/RUN06/moonpalace_run06_accepted_runs.json
MapDesign/MCP/GENERATED/RUN06/moonpalace_run06_rejected_runs.json
MapDesign/MCP/GENERATED/RUN06/moonpalace_run06_rule_findings.json
MapDesign/MCP/GENERATED/RUN06/moonpalace_run06_recipe_tuning_delta.json
MapDesign/MCP/GENERATED/RUN06/moonpalace_run06_gallery_manifest.json
MapDesign/MCP/GENERATED/RUN06/moonpalace_run06_digest_manifest.json
```

Create these CSV files:

```text
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN06/moonpalace_run06_curation_summary.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN06/moonpalace_run06_accepted_runs.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN06/moonpalace_run06_rejected_runs.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN06/moonpalace_run06_rule_findings.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN06/moonpalace_run06_recipe_tuning_delta.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN06/moonpalace_run06_gallery_layout.csv
```

All generated JSON/CSV must be deterministic:

```text
UTF-8 without BOM
LF line endings
culture-invariant numbers
stable ordering
exactly one final LF
created_utc excluded from canonical digests
```

## Focused Tests

Create `MoonPalaceRunShapeCurationTests` with category:

```text
RUN06
```

Run only focused EditMode tests:

```text
filter type/value: category / RUN06
```

Expected:

```text
Discovered: 20
Executed: 20
Passed: 20
Failed: 0
Skipped: 0
Inconclusive: 0
```

Required test coverage:

1. RUN05 prerequisite result SHA/status is recognized.
2. Quality profile contains all required rule ids.
3. Each quality finding records severity, measured value, threshold, and reason.
4. Curation batch generates at least 36 runs.
5. Batch covers all three RUN05 recipes.
6. Batch covers seeds 1924737067 through 1924737078.
7. At least 9 accepted runs exist.
8. At least 6 rejected runs exist.
9. Accepted runs pass tile-level start-to-exit BFS.
10. Accepted runs have reciprocal/open connectors.
11. Rejected runs keep their concrete rejection reasons.
12. Rejected runs are not silently repaired.
13. Recipe tuning delta is empty or fully justified with old/new values.
14. Same batch input produces identical curation digest.
15. Scene contains AcceptedGallery and RejectedGallery groups.
16. Scene contains at least 9 accepted visual entries.
17. Scene contains at least 6 rejected visual entries.
18. Editor window exposes curation batch, gallery build, next accepted, and next rejected controls.
19. JSON/CSV artifact counts and digests are stable.
20. No forbidden legacy 19347, automated PlayMode selection, full/unfiltered run, fallback carve, silent repair, Build Settings mutation, or RUN07 start is recorded.

Do not run broad regression unless RUN06 causes a compile error outside focused tests.
If that happens, report the reason and run only the smallest additional check needed to prove the repair.

## Result Report Required Format

Write:

```text
MapDesign/MCP/REPORTS/RUN06_TUNE_GENERATED_RUN_SHAPES_AND_REMOVE_BAD_VARIANTS_RESULT.md
```

The report must include this exact top block:

```text
# RUN06 - Tune Generated Run Shapes and Remove Bad Variants Result

TASK: RUN06_TUNE_GENERATED_RUN_SHAPES_AND_REMOVE_BAD_VARIANTS
STATUS: PASS|FAIL|BLOCKED
```

Then include:

```text
# User-Facing Implementation Report
```

Explain in plain Korean or English:

```text
what can now be seen in Unity
how bad generated runs are rejected
how accepted runs differ from rejected runs
what recipe tuning changed, if anything
which accepted run is recommended for RUN07
what remains outside this task
```

Then include:

```text
# Responsibility and Added Scripts
```

Use this table shape:

| Script or file | Added or changed responsibility | Explicit non-ownership |
| --- | --- | --- |
| MoonPalaceRunQualityRule.cs | ... | ... |
| MoonPalaceRunQualityProfile.cs | ... | ... |
| MoonPalaceRunQualityAnalyzer.cs | ... | ... |
| MoonPalaceRunCurationBatch.cs | ... | ... |
| MoonPalaceCuratedRunSet.cs | ... | ... |
| MoonPalaceRunCurationWindow.cs | ... | ... |
| MoonPalaceCuratedRunGallerySceneBuilder.cs | ... | ... |
| MoonPalaceRunShapeCurationTests.cs | ... | ... |
| RUN06 CSV/JSON/Scene artifacts | ... | ... |

If a RUN05 file was tuned, add rows for those files too.

Then include these sections:

```text
# Quality Profile Summary
# Curation Batch Summary
# Accepted Run Summary
# Rejected Run Summary
# Recipe Tuning Summary
# Unity Scene Output Summary
# Artifact and Digest Summary
# Focused Validation Summary
# Explicitly Not Selected
# Final Status Evidence
```

The report must explicitly state:

```text
source RUN05 Result SHA-256
Editor menu item
batch recipe count
batch seed range
total generated count
accepted count
rejected count
top rejection reasons
recommended RUN07 accepted recipe/seed
recommended RUN07 course digest
scene path
scene root
focused test discovered/executed/passed count
automated PlayMode selections: 0
legacy 19347 selections: 0
full/unfiltered selections: 0
Build Settings mutations: 0
RUN07 files/runs: 0 / 0
commit hash
```

## PASS Criteria

PASS only if all are true:

```text
RUN05 prerequisite PASS verified
Quality rules exist with required rule ids
Curation batch generates at least 36 runs
Batch includes all three RUN05 recipes
Batch includes seeds 1924737067 through 1924737078
At least 9 accepted runs are recorded
At least 6 rejected runs are recorded
Rejected runs include concrete reasons and remain visible
Accepted runs pass BFS and connector validation
Recipe tuning changes are either absent or justified with old/new values
RUN06 Scene exists at the required path
Scene has AcceptedGallery, RejectedGallery, QualityLegend, RuleOverlay, SeedLabels, Metadata
At least one accepted run is recommended for RUN07
Editor curation window exists at the required menu path
RUN06 focused EditMode tests pass 20/20
automated PlayMode tests are not selected
legacy 19347 is not selected
full/unfiltered tests are not selected
Build Settings are not mutated
no existing Scene/Prefab outside RUN06 is changed
RUN07 is not started
Result is written
RUN06-related files only are committed
commit hash is included in the Result
```

## Failure Handling

If blocked, write the Result with:

```text
STATUS: BLOCKED
```

and include:

```text
blocking file
blocking reason
what was not changed
smallest next repair action
```

If failed after edits, write:

```text
STATUS: FAIL
```

and include:

```text
files changed
tests run
failure output summary
smallest repair plan
```

Do not broaden scope to make the task pass.
Do not start RUN07.

## Finalization

When PASS:

```text
record RUN06 as COMPLETE in MapDesign/MCP/06_IMPLEMENTATION_STATUS.md using the smallest clear status addition
commit only RUN06-owned source/test/artifact/task/report/status files and explicitly justified RUN05 recipe-field tuning files
include the commit hash inside the Result before handing off
```

Commit message:

```text
RUN06 tune generated run shapes and curation gallery
```

Do not push.
Stop after reporting the Result path and commit hash.

