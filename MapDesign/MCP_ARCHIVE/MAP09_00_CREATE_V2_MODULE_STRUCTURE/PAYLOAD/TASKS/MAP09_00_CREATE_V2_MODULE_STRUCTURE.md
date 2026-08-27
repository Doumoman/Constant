# MAP09_00 - Create V2 Module Structure

```yaml
status_control:
  task_key: MAP09_00_CREATE_V2_MODULE_STRUCTURE
  result_file: REPORTS/MAP09_00_CREATE_V2_MODULE_STRUCTURE_RESULT.md
```

## TASK TYPE

```text
PRE-MAP09 ADDITIVE UNITY MODULE STRUCTURE TRANSITION
```

## Objective

MAP08_14 PASS/finalize 기준선을 보존한 채 새 `Cluster-first → Pattern-second → Chunk-slice-last` 파이프라인이 들어갈 기능별 디렉터리를 만든다.

이 Task는 구조-only 전환이다. 기존 MAP00~08 파일·폴더·Unity GUID를 삭제, 이동, 이름 변경하거나 재배치하지 않는다. 신규 C#, CSV, ScriptableObject, asmdef/asmref, Scene, Prefab, Tile, Generated CSV를 만들지 않는다. `MAP09_01_FREEZE_BASELINE_AND_REGISTER_V2_PASSES`와 이후 Task는 읽거나 시작하지 않는다.

## Mandatory Read Order

1. `00_MCP_ENTRYPOINT.md`
2. `01_PROJECT_LOCKED_RULES.md`
3. `02_MCP_WORK_RULES.md`
4. `03_DATA_CSV_RULES.md`
5. `04_UNITY_MCP_RULES.md`
6. `05_CHANGE_CONTROL_RULES.md`
7. `07_PATCH_APPLY_RULES.md`
8. `08_STATUS_FINALIZE_RULES.md`
9. `MASTER_IMPLEMENTATION_TASK_LIST.md`
10. `06_IMPLEMENTATION_STATUS.md`
11. 이 Task
12. `REPORTS/MAP00_02_FOLDER_AND_ASMDEF_PLAN_RESULT.md`
13. `REPORTS/MAP00_03_CREATE_MAP_MODULE_STRUCTURE_RESULT.md`
14. `REPORTS/MAP00_04_CREATE_TEST_STRUCTURE_RESULT.md`
15. `REPORTS/MAP08_14_MAP08_EXIT_TESTS_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP08_14_MAP08_EXIT_TESTS
STATUS: PASS
MAP08_14: COMPLETE ELIGIBLE
MAP08 PHASE EXIT: APPROVED
SHA-256: 5d0b2f0d478ef8479b93e1b9163445f6e736022b533dee77f81690b8670cf2d1
```

## Approved Input Baseline

```text
Unity: 6000.3.8f1
Runtime assembly: Game.Map.Runtime
Runtime namespace boundary: StarNight.Map.WorldGeneration.*
Runtime EditMode assembly: Game.Map.Tests.EditMode
Editor assembly: MapAuthoring.Editor
Editor EditMode assembly: MapAuthoring.Tests.EditMode
New asmdef/asmref: NO

MAP00 approved directories: 36/36
MAP08_14 Result SHA-256: 5d0b2f0d478ef8479b93e1b9163445f6e736022b533dee77f81690b8670cf2d1
MAP08_14 installed Task SHA-256: 6fffc0ed3f8ca333cf7d74d44c437ab6e4193871ce8b2a7a254405e4bcaa5e8e
MAP08 required union: 9220/9220 PASS
MAP07 required regression: 5422/5422 PASS
MAP06 required regression: 2746/2746 PASS
MAP05 required regression: 1959/1959 PASS
Required subset total: 19347/19347 PASS
MAP08_12 aggregate digest: f7ff1c49f5bc33a4ad57799269bc3915806fe0cb60f347ed76eb16ea26f7fc68
Authoring CSV/meta: 50/50
Authoring manifest SHA-256: f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb
Generated CSV: 0
Global Assets meta after MAP08_14: 3816
Assets/_Game/Map meta after MAP08_14: 596
Duplicate GUID groups: 0
```

Baseline counts are comparison evidence. If unrelated pre-existing worktree changes make the live counts differ, preserve them and report the delta rather than reverting them. The exact 24 target paths and task-owned changes remain authoritative.

## Structure Decision

The transition is additive. Existing broad responsibility roots remain authoritative:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/
Assets/_Game/Map/Runtime/WorldGeneration/Data/
Assets/_Game/Map/Runtime/WorldGeneration/Generation/
Assets/_Game/Map/Runtime/WorldGeneration/Validation/
Assets/_Game/Map/Runtime/WorldGeneration/Random/
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/
```

Existing post-MAP00 feature roots such as `Microchunks/` and `Boundaries/` also remain in place. No migration or namespace rewrite is allowed. New folders are ownership boundaries for future Tasks; they do not authorize implementation in this Task.

## WRITE ALLOWLIST

### Runtime feature directories - exact 9

```text
Assets/_Game/Map/Runtime/WorldGeneration/Pipeline/
Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/
Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/
Assets/_Game/Map/Runtime/WorldGeneration/Activities/
Assets/_Game/Map/Runtime/WorldGeneration/EventOverlays/
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/
Assets/_Game/Map/Runtime/WorldGeneration/Baking/
Assets/_Game/Map/Runtime/WorldGeneration/RuntimeState/
```

### Runtime EditMode test directories - exact 9

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Pipeline/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/MicroPatterns/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/TerrainClusters/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Activities/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/EventOverlays/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SpecialRegions/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/RuntimeState/
```

### Authoring and Generated data directories - exact 6

```text
Assets/_Game/Map/Data/WorldGeneration/Authoring/MicroPattern/
Assets/_Game/Map/Data/WorldGeneration/Authoring/TerrainCluster/
Assets/_Game/Map/Data/WorldGeneration/Authoring/Activity/
Assets/_Game/Map/Data/WorldGeneration/Authoring/EventOverlay/
Assets/_Game/Map/Data/WorldGeneration/Authoring/SpecialRegion/
Assets/_Game/Map/Data/WorldGeneration/Generated/
```

Each missing directory and its matching Unity folder `.meta` may be created. A pre-existing target directory and `.meta` must be preserved byte-for-byte.

### Result report - exact 1

```text
MapDesign/MCP/REPORTS/MAP09_00_CREATE_V2_MODULE_STRUCTURE_RESULT.md
```

## READ ALLOWLIST

```text
MapDesign/MCP files in Mandatory Read Order
The exact 24 target paths and their parent directory entries
The 36 MAP00 approved directories and matching folder metas
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/ directory entries
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/ directory entries
The five existing map/runtime/editor/test asmdef files
Git status and diff path inventory
Unity compile and Console state
Project-wide .meta GUID index for duplicate detection
```

Do not read future MAP09+ Task bodies, Legacy/Stage/P6/P11 generator bodies, Scene/Prefab YAML, or unrelated source bodies.

## Collision Handling

1. If a target directory already exists, record `PREEXISTING` and do not touch its `.meta`.
2. If a target path is occupied by a non-directory file, stop as `BLOCKED`.
3. If a matching folder `.meta` exists without its directory, stop as `BLOCKED`; do not replace the GUID.
4. If a target directory exists without a folder `.meta`, refresh through Unity and record the created meta.
5. Unexpected content inside a pre-existing target is inventory-only; do not delete or overwrite it. If it conflicts with structure-only completion, stop as `BLOCKED`.
6. Preserve all unrelated user worktree changes.

## Forbidden Operations

```text
delete, move, or rename any existing file or directory
change any existing .meta or GUID
create .gitkeep, placeholder README, C#, CSV, asset, json, or schema files
modify Runtime, Editor, test, CSV, Scene, Prefab, Tile, ProjectSettings, or Packages content
create or modify asmdef/asmref
create Generated CSV
implement MAP09_01 or any later symbol
reuse StageMapGenerator, P6/P11 generators, GridWorld, RoomTemplate,
RoomGridTransform, or TileMutationService
relax validation or change MAP00~08 contracts
git push, branch, reset, rebase, force, or stage unrelated changes
```

## Implementation Steps

1. Confirm this Task is the only `CURRENT` row and total state is `105 COMPLETE / 1 CURRENT / 108 LOCKED`.
2. Verify MAP08_14 Result and installed Task SHA-256 gates.
3. Record pre-task Git path inventory, Unity state, global Assets meta count, Map meta count, and duplicate GUID count.
4. Verify the 36 MAP00 approved directories and existing `Microchunks`/`Boundaries` roots remain present.
5. Classify the exact 24 targets as `PREEXISTING`, `MISSING`, or `BLOCKED COLLISION`.
6. Create only missing directories. Use Unity/AssetDatabase refresh so Unity creates valid folder metas.
7. Verify exact target presence `24/24` and matching folder metas `24/24`.
8. Verify every new folder meta has `fileFormatVersion: 2`, one 32-hex GUID, `folderAsset: yes`, and no project-wide duplicate GUID.
9. Verify every pre-existing target meta and all existing MAP00~08 files are byte-preserved.
10. Verify task-owned changes contain only new target folder metas, this Result, patch application files, and status finalize.
11. Refresh Unity; wait for idle; confirm compile/Console/relevant warnings `0/0/0`.
12. Run the three existing architecture fixtures only. Do not modify them to force a pass.
13. Write the Result, finalize status only on PASS, and create the required atomic commit.

## Verification

### V1 - Exact structure

```text
Target directories: 24/24
Target folder metas: 24/24
MAP00 approved directories preserved: 36/36
Existing Microchunks root preserved: YES
Existing Boundaries root preserved: YES
Blocked collisions: 0
```

### V2 - Meta and scope

```text
New folder metas: N, where 0 <= N <= 24
Global Assets meta after = before + N
Map meta after = before + new metas under Assets/_Game/Map
Duplicate GUID groups: 0
Existing meta modified: 0
Existing file moved/deleted/renamed: 0/0/0
New non-meta artifacts inside target folders: 0
```

### V3 - Architecture fixtures

```text
WorldGenerationModuleStructureTests
WorldGenerationRuntimeBoundaryTests
WorldGenerationEditorBoundaryTests
Targeted architecture cases: 10/10 PASS
```

This structure-only Task does not rerun the 19,347-case MAP05~08 suite because production code, tests, CSV, and assembly definitions are immutable here. The MAP08_14 hashes and zero-change gates preserve that baseline.

### V4 - Static gates

```text
New/modified Runtime C#: 0/0
New/modified Editor C#: 0/0
New/modified test C#: 0/0
Authoring CSV/meta: 50/50
Authoring CSV tracked changes: 0
Authoring manifest before/after: f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb / same
Generated CSV created: 0
Scene/Prefab changes: 0/0
ProjectSettings/Packages changes: 0/0
asmdef/asmref changes: 0/0
Obsolete MAP09_01 solver production symbols: 0
V2 MAP09_01+ production symbols: 0
git diff --check errors before Result: 0
Compile/Console/relevant warnings: 0/0/0
```

## Commit Requirement

After all gates pass, create exactly one atomic commit.

Commit subject:

```text
MAP09_00: add V2 module structure
```

Commit body must include:

```text
- Preserve all MAP00-MAP08 files, directories, metas, GUIDs, and approved contracts
- Add the exact 24 V2 Runtime, EditMode test, Authoring, and Generated directory roots
- Keep MicroPattern and 12x8 MicroChunk responsibilities separate
- Keep Cluster-first, Pattern-second, Chunk-slice-last ownership boundaries explicit
- Verify folder metas, GUID uniqueness, Unity compilation, Console, and architecture fixtures
- Keep MAP09_01_FREEZE_BASELINE_AND_REGISTER_V2_PASSES locked / do not start
```

Do not stage unrelated pre-existing worktree changes. Do not push.

## Result Report Requirements

Create `MapDesign/MCP/REPORTS/MAP09_00_CREATE_V2_MODULE_STRUCTURE_RESULT.md` with this exact header:

```text
TASK: MAP09_00_CREATE_V2_MODULE_STRUCTURE
STATUS: PASS | FAIL | BLOCKED
MAP09_00: COMPLETE ELIGIBLE only if PASS
V2 MODULE STRUCTURE: APPROVED only if PASS
MAP09_01_FREEZE_BASELINE_AND_REGISTER_V2_PASSES: LOCKED / DO NOT START
```

The Result must report:

```text
Patch apply and installed payload SHA-256 validation
MAP08_14 Result and installed Task SHA-256
Exact 24 target path inventory with PREEXISTING/CREATED/BLOCKED state
Created folder meta inventory and GUID validation
36 MAP00 directories and Microchunks/Boundaries preservation
Before/after global Assets and Map meta counts
Existing file/meta move/delete/rename/modify counts
Architecture fixture counts and job identifiers
Compile/Console/relevant warning counts
Authoring CSV/meta and manifest preservation
Generated CSV, Scene/Prefab, ProjectSettings/Packages, asmdef/asmref change counts
Obsolete and V2 MAP09+ production symbol hits
git diff --check result
Atomic commit subject and immutable commit hash handoff
```

## Done Condition

MAP09_00 is complete only when:

```text
MAP08_14 SHA gates PASS
Exact target directories/metas 24/24
MAP00 approved directories 36/36 preserved
Microchunks and Boundaries roots preserved
Existing file/meta move/delete/rename/modify counts 0
New C#/CSV/asset/asmdef/Scene/Prefab artifacts 0
Duplicate GUID groups 0
Architecture fixtures 10/10 PASS
Compile/Console/relevant warnings 0/0/0
Authoring manifest unchanged and Generated CSV 0
Atomic commit created
Result report created
V2 MODULE STRUCTURE: APPROVED
MAP09_01 remains LOCKED / DO NOT START
```
