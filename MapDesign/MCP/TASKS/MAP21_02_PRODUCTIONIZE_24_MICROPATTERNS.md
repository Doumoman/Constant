```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP21_02_PRODUCTIONIZE_24_MICROPATTERNS
  task_file: TASKS/MAP21_02_PRODUCTIONIZE_24_MICROPATTERNS.md
  requires_current_task: NONE
  requires_completed_task: MAP21_01_LOCK_MOONPALACE_PROFILES_AND_TILE_SHELL
  requires_result:
    path: REPORTS/MAP21_01_LOCK_MOONPALACE_PROFILES_AND_TILE_SHELL_RESULT.md
    status: PASS
    sha256: 37de80ca52aa90a2ccc121f0b10ac2b75f54a8758381cf592a1413ac9e7d2eaf
  requires_installed_task:
    path: TASKS/MAP21_01_LOCK_MOONPALACE_PROFILES_AND_TILE_SHELL.md
    sha256: efd5fedf4551f5427af95253ffe792c0b9d6b3b68a0868df167cee66101b9f89
  requires_handoff:
    name: MAP21_02 handoff digest
    sha256: f7993186e63a73a76e0e54af05c0851a6ca9e9743b836877dfdb931a8dd8e2d2
  sets_current_task: MAP21_02_PRODUCTIONIZE_24_MICROPATTERNS
```

# MAP21_02 - Productionize 24 MicroPatterns

```text
TASK: MAP21_02_PRODUCTIONIZE_24_MICROPATTERNS
PHASE: MAP21 - MoonPalace Vertical Slice
STATUS: CURRENT
NEXT: MAP21_03_EXPAND_CRATER_AND_ROOT_CLUSTER_POOLS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP10의 starter 24 MicroPattern을 월궁 production tile shell 기준으로 옮겨 **실제 제작용 4x4 pattern authoring**을 고정한다.

이번 Task는 24개 패턴의 실루엣, TileCode 매핑, material/affordance/hazard/marker 태그, fallback/MissingData 표현만 소유한다.  
Cluster pool, Sector/World generation, Activity/Region 제작, 실제 sprite/audio/background import는 아직 시작하지 않는다.

| Area | This task owns | This task must not do |
|---|---|---|
| Pattern catalog | 24개 MoonPalace MicroPattern identity, biome, role, transform, source starter mapping | MAP10 starter CSV rewrite |
| 4x4 cells | 각 패턴의 16-cell production cell, TileCode/fallback/collision/source operation | renderer/generator 실행 |
| Surface/material | 월궁 material token과 visual intent tag | actual sprite/material asset import |
| Affordance/hazard/marker | climb/grip/bounce/cue/recovery 등 semantic tag와 runtime binding 필요 여부 | gameplay component, damage, physics binding |
| Digest/handoff | MAP21_03 cluster pool이 소비할 pattern manifest digest | MAP21_03 파일 생성 또는 unlock |

핵심 원칙:

```text
This task productionizes pattern data, not map structure.
Every pattern is 4x4 and deterministic.
All tile references must resolve to MAP21_01 tile shell records or explicit MissingData/fallback.
No generated terrain, Tilemap, Scene, Prefab, runtime object, or MAP10 source CSV is mutated.
```

## 1. 사용자 보고 의무

Result 첫 부분에는 반드시 아래 두 섹션을 둔다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`User-Facing Implementation Report`에는 아래 질문에 답한다.

```text
이번 작업이 어떤 24개 월궁 MicroPattern을 productionized 했는가?
MAP10 starter 24개와 source mapping은 어떻게 보존했는가?
4개 biome마다 몇 개의 pattern이 생겼는가?
Geometry silhouette, material, affordance, hazard, marker는 어떻게 기록됐는가?
TileCode는 MAP21_01 tile shell과 어떻게 연결됐고 MissingData/fallback은 어떻게 표시됐는가?
이번 작업이 renderer/generator/cluster/world/Tilemap/Scene/Prefab/runtime을 건드리지 않았다는 증거는 무엇인가?
legacy regression을 돌리지 않았다는 증거는 무엇인가?
MAP21_03은 왜 아직 시작하지 않았는가?
```

`Responsibility and Added Scripts`는 반드시 표로 작성한다.

```text
| Script or file | Added or changed responsibility | Explicit non-ownership |
```

파일별 책임이 구체적이지 않으면 FAIL이다.

## 2. 선행조건

작업 시작 전에 다음을 확인한다.

```text
MapDesign/MCP/REPORTS/MAP21_01_LOCK_MOONPALACE_PROFILES_AND_TILE_SHELL_RESULT.md exists
MAP21_01 Result STATUS: PASS
MAP21_01 Result SHA-256:
37de80ca52aa90a2ccc121f0b10ac2b75f54a8758381cf592a1413ac9e7d2eaf

MapDesign/MCP/TASKS/MAP21_01_LOCK_MOONPALACE_PROFILES_AND_TILE_SHELL.md exists
MAP21_01 installed Task SHA-256:
efd5fedf4551f5427af95253ffe792c0b9d6b3b68a0868df167cee66101b9f89

MAP21_02 handoff digest:
f7993186e63a73a76e0e54af05c0851a6ca9e9743b836877dfdb931a8dd8e2d2

MAP21_02 source shell digests from MAP21_01 Result:
movement profile digest: f7287b4cbe5609b7f37885e76a96172fe22479b7bcbbdb6264366a610bbb6c71
biome profile digest: 631000d63f0f2de9c829e662dbb9ab05f3ad4ad5ada542307058515569b3a735
tile shell manifest digest: 04eefb76556b4874c4a60838149d523e084e0d315fc2d833a9458c9fc6ad2514

MAP21_03_EXPAND_CRATER_AND_ROOT_CLUSTER_POOLS: LOCKED
```

불일치 시:

```text
STATUS: BLOCKED
reason: precondition mismatch
created/changed code files: 0
MAP21_03 started: NO
STOP
```

## 3. 허용 범위

허용 파일:

```text
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/MoonPalaceMicroPatternProduction.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/MoonPalace/MoonPalaceMicroPatternPublisher.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/MoonPalace/MoonPalaceMicroPatternProductionTests.cs
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_02/**
MapDesign/MCP/GENERATED/MAP21_02/**
MapDesign/MCP/TASKS/MAP21_02_PRODUCTIONIZE_24_MICROPATTERNS.md
MapDesign/MCP/REPORTS/MAP21_02_PRODUCTIONIZE_24_MICROPATTERNS_RESULT.md
MapDesign/MCP_ARCHIVE/MAP21_02_PRODUCTIONIZE_24_MICROPATTERNS.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

조건부 허용:

```text
Existing MAP10 MicroPattern contracts may be referenced read-only.
Existing MAP10 starter pattern ids/templates may be read, copied into MAP21_02 authoring, and source-linked.
Existing MAP21_01 profile/tile shell CSV and generated JSON may be read and hashed.
Authoring CSV writes are allowed only under Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_02.
Generated report JSON writes are allowed only under MapDesign/MCP/GENERATED/MAP21_02.
Missing art/audio/background references from MAP21_01 must remain explicit MissingData/fallback.
```

금지:

```text
MAP10 starter CSV rewrite
MAP21_01 profile/tile shell rewrite
pattern renderer behavior change
cluster pool expansion
sector/world generation
Activity/Event/SpecialRegion production
Tilemap bake or Tilemap mutation
Scene / Prefab / ProjectSettings / Packages changes
sprite/audio/background asset import
Addressables group/build change
runtime GameObject instantiate/enable/disable/destroy during tooling actions
generator solve/reroll/execution
validation runner execution
actual replay execution
rollback execution
CSV authoring edit outside MAP21_02 authoring folder
auto-fix / auto-repair
production seed approval
external process launch
manual gameplay test
automatic MAP21_03 start
broad refactor or optimization rewrite
```

회귀 금지:

```text
MAP19_09 scale audit rerun
MAP20_01 generator run rerun
MAP20_02 overlay sample regeneration
MAP20_03 detail sample regeneration
MAP20_04 navigation sample regeneration
MAP20_05 replay/export sample regeneration
MAP20_06 exit audit regeneration
MAP21_01 profile/tile shell regeneration
legacy 19347 regression
prior task category selection
PlayMode selection
unfiltered test run
full regression run
```

## 4. Production MicroPattern 계약

`MoonPalaceMicroPatternProduction` must define immutable records only.

Required catalog fields:

```text
pattern_id
schema_version
source_starter_pattern_id
source_starter_digest
biome_id
pattern_role
silhouette_family
allowed_transform_tokens
selection_weight
tile_shell_digest
cell_record_digest
tag_record_digest
missing_asset_policy
runtime_binding_state
created_utc_excluded_from_canonical_digest
canonical_digest
```

Required cell fields:

```text
pattern_id
x
y
source_operation
tile_code
tile_role
collision_kind
material_token
fallback_tile_code
asset_reference_kind
missing_reason
protected_mask_policy
canonical_digest
```

Required tag fields:

```text
pattern_id
x
y
tag_kind
tag_token
runtime_binding_state
risk_level
visual_priority
canonical_digest
```

Rules:

```text
There must be exactly 24 pattern catalog records.
Each biome has exactly 6 patterns.
Each pattern has exactly 16 cell records with x/y in 0..3.
Every tile_code must resolve to MAP21_01 tile shell or explicit MissingData/fallback.
Every production pattern keeps a source_starter_pattern_id from the MAP10 starter set.
Geometry/source_operation intent must preserve the starter silhouette family.
Affordance/hazard/marker tags are semantic authoring only; no gameplay binding is created in this Task.
```

## 5. Required 24 Pattern Inventory

The productionized set must contain exactly the MAP10 starter 24 identities.

| Biome | Required pattern ids |
|---|---|
| MoonCrater | `MP_CRATER_BROKEN_SLOPE`, `MP_CRATER_BOWL`, `MP_CRATER_ROCK_SHELF`, `MP_CRATER_GRIP_RIDGE`, `MP_CRATER_DUST_PATCH`, `MP_CRATER_METEOR_CUE` |
| CassiaRoot | `MP_ROOT_ARCH`, `MP_ROOT_VERTICAL_TUNNEL`, `MP_ROOT_HOLLOW_POCKET`, `MP_ROOT_CLIMB_VINES`, `MP_ROOT_SAP_PATCH`, `MP_ROOT_SPROUT_MARK` |
| AbandonedMill | `MP_MILL_BROKEN_PILLAR`, `MP_MILL_BEAM_OVERHANG`, `MP_MILL_ORTHOGONAL_CARVE`, `MP_MILL_BEAM_GRIP`, `MP_MILL_RUST_PATCH`, `MP_MILL_GEAR_SOCKET` |
| MoonDough | `MP_DOUGH_BOUNCE_CUP`, `MP_DOUGH_SOFT_POCKET`, `MP_DOUGH_STICKY_SHELF`, `MP_DOUGH_BOUNCE_STRIP`, `MP_DOUGH_FERMENT_PATCH`, `MP_DOUGH_RECOVERY_PAD` |

Required role counts:

```text
Geometry silhouette patterns: 12
Surface/affordance patterns: 4
Material/hazard/marker patterns: 8
Total: 24
```

Required tag kinds:

```text
Surface
Material
Affordance
Hazard
Marker
```

Required runtime binding state tokens:

```text
AuthoringOnly
NeedsRuntimeBinding
MissingData
```

## 6. Tile Shell 매핑 규칙

MAP21_02 must consume the MAP21_01 tile shell by role/collision/material token.

Rules:

```text
Do not hard-code a tile code if a role/collision lookup can resolve it from MAP21_01 tile shell records.
Ground/Wall/Ceiling/SlopeOrStep/BoundaryBlend geometry cells must resolve to a compatible shell record.
OneWayPlatform cells must use collision kind OneWay.
Hazard tags may reference Hazard shell records, but must not create gameplay damage or physics behavior.
Decor/Background/detail cells must remain non-blocking unless the MAP21_01 shell explicitly says otherwise.
If MAP21_01 asset_reference_kind is MissingData, keep MissingData and fallback in MAP21_02 output.
```

Result must report:

```text
tile shell records read:
tile code lookups resolved:
tile code MissingData/fallback records:
unknown tile code references:
asset imports:
```

## 7. Snapshot / Digest 계약

Required authoring files:

```text
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_02/moonpalace_micropattern_catalog.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_02/moonpalace_micropattern_cells.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_02/moonpalace_micropattern_tags.csv
```

Required generated artifacts:

```text
MapDesign/MCP/GENERATED/MAP21_02/moonpalace_micropattern_catalog_manifest.json
MapDesign/MCP/GENERATED/MAP21_02/moonpalace_micropattern_cell_manifest.json
MapDesign/MCP/GENERATED/MAP21_02/moonpalace_micropattern_digest_manifest.json
```

Digest manifest required fields:

```text
schema_version
task_id
source_MAP21_01_result_digest
source_MAP21_01_task_digest
source_MAP21_02_handoff_digest
source_biome_profile_digest
source_tile_shell_digest
pattern_catalog_digest
pattern_cell_digest
pattern_tag_digest
MAP21_03_handoff_digest
created_utc_excluded_from_canonical_digest
canonical_digest
```

All canonical digests must exclude `created_utc`, use deterministic order, and be lowercase SHA-256.

## 8. 중복과 하드코딩 방지

Result에 아래 카운터를 기록하고 모두 0이어야 한다.

```text
MAP10 source CSV modified:
MAP21_01 source profile/tile shell modified:
generation pipeline C# modified:
renderer C# modified:
Scene/Prefab/ProjectSettings/Packages modified:
duplicated generator logic count:
duplicated validation logic count:
duplicated renderer logic count:
hard-coded tile code references outside MAP21_02 authoring/sample constants:
hard-coded digest string copies outside precondition/handoff constants:
```

## 9. Focused Tests

Focused test category:

```text
MAP21_02
```

Required test names:

```text
MoonPalaceMicroPatternsContainExact24StarterMappedPatternsAndSixPerBiome
MoonPalaceMicroPatternCellsCoverExactly4x4AndPreserveSilhouetteFamilies
MoonPalaceMicroPatternsMapCellsToTileShellRolesCollisionAndFallbacks
MoonPalaceMicroPatternsRecordSurfaceMaterialAffordanceHazardAndMarkerTags
MoonPalaceMicroPatternsRepresentMissingAssetsWithoutImportOrRuntimeBinding
MoonPalaceMicroPatternsRejectDuplicateIdsInvalidCellsUnknownTilesAndBadRanges
MoonPalaceMicroPatternArtifactsSerializeDeterministicallyAcrossRepeatCultureAndInputOrder
MoonPalaceMicroPatternPublisherWritesOnlyMap21_02AuthoringAndGeneratedRoots
MoonPalaceMicroPatternsPublishMap21_03HandoffOnlyAfterFocusedPass
MoonPalaceMicroPatternsDoNotRunGenerationRendererValidationReplayRollbackPlayModeOrLegacyRegression
```

Required count:

```text
discovered: 10
executed: 10
passed: 10
failed: 0
```

Focused validation may create sample artifacts only under `MapDesign/MCP/GENERATED/MAP21_02`.

## 10. 회귀 금지 정책

이번 Task의 검증은 focused MAP21_02 EditMode selection과 compile/console check까지만 허용한다.

문제가 실제로 발생하지 않은 상태에서 더 넓은 검증을 돌리지 않는다. 실제 문제가 발생해 더 넓은 검증이 필요하면 조용히 실행하지 말고, Result에 trigger owner, broken invariant, focused proof insufficiency, requested wider verification을 기록하고 STOP한다.

문제가 없다면 Result에 반드시 아래 블록을 포함한다.

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
FULL REGRESSION RUNS: 0
MAP19_09 SCALE AUDIT RERUNS: 0
MAP20_01 GENERATOR RUN RERUNS: 0
MAP20_02 OVERLAY SAMPLE REGENERATION RUNS: 0
MAP20_03 DETAIL SAMPLE REGENERATION RUNS: 0
MAP20_04 NAVIGATION SAMPLE REGENERATION RUNS: 0
MAP20_05 REPLAY EXPORT SAMPLE REGENERATION RUNS: 0
MAP20_06 EXIT AUDIT REGENERATION RUNS: 0
MAP21_01 PROFILE TILE SHELL REGENERATION RUNS: 0
VALIDATION RUNNER EXECUTIONS: 0
REPLAY EXECUTIONS: 0
GENERATOR EXECUTIONS: 0
RENDERER EXECUTIONS: 0
ROLLBACK EXECUTIONS: 0
CSV AUTHORING WRITES OUTSIDE MAP21_02 AUTHORING FOLDER: 0
RUNTIME OBJECT SPAWNS: 0
SCENE PREFAB CHANGES: 0
```

## 11. Result 필수 기록

Result 파일:

```text
MapDesign/MCP/REPORTS/MAP21_02_PRODUCTIONIZE_24_MICROPATTERNS_RESULT.md
```

Required sections:

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
## Pattern Inventory and Source Mapping Summary
## Tile Shell Material Affordance Hazard Summary
## Snapshot and Digest Summary
## Focused Validation Summary
## No Legacy Regression Boundary Notes
## Final Status Evidence
```

Required summary fields:

```text
MAP21_01 Result SHA-256 required/actual:
MAP21_01 installed Task SHA-256 required/actual:
MAP21_02 handoff digest required/actual:
source biome profile digest:
source tile shell digest:

pattern catalog records:
starter source mappings:
biome pattern counts:
role counts:
cell records:
patterns with exact 4x4 coverage:
silhouette families preserved:

tile shell records read:
tile code lookups resolved:
tile code MissingData/fallback records:
unknown tile code references:
tag kinds:
tag records:
runtime binding states:
asset imports:

authoring files written:
generated sample artifacts created:
moonpalace micropattern catalog manifest digest lower-hex SHA-256:
moonpalace micropattern cell manifest digest lower-hex SHA-256:
moonpalace micropattern digest manifest lower-hex SHA-256:
MAP21_03 handoff digest lower-hex SHA-256:

Unity Version:
Compile Errors:
Relevant Warnings:
Relevant Console Errors:
EditMode category:
Discovered:
Executed:
Passed:
Failed:
Skipped:
Inconclusive:
PlayMode Tests:
Scene/Prefab Changes:
```

## 12. PASS / FAIL / BLOCKED

PASS 조건:

```text
Precondition SHA and handoff digest match
Exactly 24 starter-mapped MicroPattern records exist
Each required biome has exactly 6 patterns
Role counts are exactly 12 geometry / 4 surface-affordance / 8 material-hazard-marker
Every pattern has exact 4x4 cell coverage
TileCode references resolve to MAP21_01 shell or explicit MissingData/fallback
Surface/material/affordance/hazard/marker tags are explicit authoring data
No actual asset import, runtime binding, gameplay damage, physics binding, renderer execution, or generated terrain
No MAP10 or MAP21_01 source rewrite
Focused MAP21_02 EditMode tests are 10/10 PASS
All no-regression and no-execution counters are zero
Responsibility and Added Scripts table is present and specific
MAP21_03 remains LOCKED and not started
```

FAIL 조건:

```text
Pattern count is not 24 or source starter mapping is missing
Biome counts are not exactly 6 each
4x4 coverage is incomplete or coordinates are invalid
TileCode references are unknown and not explicit MissingData/fallback
Hazard/affordance creates runtime gameplay behavior in this Task
MAP10 starter CSV or MAP21_01 profile/tile shell is rewritten
Cluster/sector/world generation, renderer, validation runner, replay, rollback, PlayMode, legacy regression, unfiltered run, or full regression is executed
Sprite/audio/background assets are imported
Scene/Prefab/Tilemap/Addressables/ProjectSettings/Packages are changed
CSV authoring is written outside MAP21_02 authoring folder
MAP21_03 starts
```

BLOCKED 조건:

```text
Precondition SHA mismatch
Compile error unrelated to this task that prevents focused validation
Existing MAP10 starter identities cannot be read or source-mapped
MAP21_01 tile shell cannot resolve required roles and MissingData/fallback cannot represent the gap
Authoring CSV location is unavailable or conflicts with existing authoritative MAP21 files
```

When BLOCKED:

```text
Do not finalize MAP21_02
Do not start MAP21_03
Do not publish MAP21_03 handoff digest
Record exact owner, invariant, and minimum next action
STOP
```

## 13. Finalize / Commit / STOP

PASS일 때만:

```text
MAP21_02_PRODUCTIONIZE_24_MICROPATTERNS: COMPLETE
MAP21_03_EXPAND_CRATER_AND_ROOT_CLUSTER_POOLS: LOCKED
```

FAIL 또는 BLOCKED일 때:

```text
MAP21_02 remains CURRENT
MAP21_03 remains LOCKED
STOP
```

PASS일 때만 atomic commit을 만든다.

Commit subject:

```text
MAP21_02 productionize moonpalace micropatterns
```

Commit body에는 다음을 포함한다.

```text
MAP21_02 responsibilities
focused test count
pattern count and biome counts
role counts and 4x4 cell count
tile shell mapping and MissingData/fallback count
tag kind counts
MAP21_03 handoff digest
legacy regression counters all zero
generation/renderer/replay/rollback/validation counters all zero
Scene/Prefab/Tilemap/Addressables changes zero
```

Git push는 하지 않는다.

Result 작성, 상태 finalize, commit 이후 반드시 멈춘다.

```text
DO NOT START MAP21_03.
DO NOT CREATE MAP21_03 FILES.
DO NOT RERUN MAP19_09 SCALE AUDIT.
DO NOT REGENERATE MAP20_01 THROUGH MAP20_06 ARTIFACTS.
DO NOT REGENERATE MAP21_01 PROFILE TILE SHELL.
DO NOT RUN VALIDATION RUNNER.
DO NOT EXECUTE REPLAY.
DO NOT EXECUTE GENERATOR.
DO NOT EXECUTE RENDERER.
DO NOT EXECUTE ROLLBACK.
DO NOT WRITE CSV AUTHORING FILES OUTSIDE MAP21_02 AUTHORING FOLDER.
DO NOT IMPORT SPRITE AUDIO OR BACKGROUND ASSETS.
DO NOT MUTATE SCENE PREFAB TILEMAP ADDRESSABLES OR RUNTIME OBJECTS.
DO NOT RUN LEGACY REGRESSION.
STOP.
```
