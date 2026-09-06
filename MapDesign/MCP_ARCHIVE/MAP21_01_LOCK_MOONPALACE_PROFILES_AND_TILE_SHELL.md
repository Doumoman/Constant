```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP21_01_LOCK_MOONPALACE_PROFILES_AND_TILE_SHELL
  task_file: TASKS/MAP21_01_LOCK_MOONPALACE_PROFILES_AND_TILE_SHELL.md
  requires_current_task: NONE
  requires_completed_task: MAP20_06_MAP20_TOOLING_EXIT_TESTS
  requires_result:
    path: REPORTS/MAP20_06_MAP20_TOOLING_EXIT_TESTS_RESULT.md
    status: PASS
    sha256: ed73ee40f6f197c9e3d3582acbab7527d25de662a45a8a6eeeb4ae67e52da4bb
  requires_installed_task:
    path: TASKS/MAP20_06_MAP20_TOOLING_EXIT_TESTS.md
    sha256: 1702533bac5ea69e1bcd687092e2ac6ab02268e1341b5d8a39a0677cd1af3494
  requires_handoff:
    name: MAP21_01 handoff digest
    sha256: 828b00646fc8ab5936625244471f717e5e25cbf6edf49f8295d60e8b5fec514c
  sets_current_task: MAP21_01_LOCK_MOONPALACE_PROFILES_AND_TILE_SHELL
```

# MAP21_01 - Lock MoonPalace Profiles and Tile Shell

```text
TASK: MAP21_01_LOCK_MOONPALACE_PROFILES_AND_TILE_SHELL
PHASE: MAP21 - MoonPalace Vertical Slice
STATUS: CURRENT
NEXT: MAP21_02_PRODUCTIONIZE_24_MICROPATTERNS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP21의 첫 작업은 월궁 vertical slice의 실제 콘텐츠 제작 기준을 고정하는 것이다.

이번 Task는 **프로필과 타일 shell 계약**만 소유한다. 풀 맵 생성, 패턴 제작, 클러스터 확장, Activity/Region 제작은 아직 시작하지 않는다.

| Area | This task owns | This task must not do |
|---|---|---|
| Movement profile | 월궁에서 사용할 실제 이동 수치와 traversal envelope 기준 고정 | Player/controller 코드 변경 |
| Biome profile | MoonCrater/CassiaRoot/AbandonedMill/MoonDough density, pacing, difficulty band 고정 | MAP21_10 튜닝, cluster pool 확장 |
| TileCode shell | 월궁 TileCode, collision kind, layer, material/audio/background token 정의 | 실제 sprite/audio/background asset import |
| Presentation shell | material, footstep/audio, ambient/background token과 fallback/MissingData 기록 | Scene/Prefab 배치, Addressables 빌드 변경 |
| Digest/handoff | MAP21_02가 사용할 profile/tile shell digest 생성 | MAP21_02 파일 생성 또는 unlock |

핵심 원칙:

```text
This task locks names, ranges, tokens, references, and deterministic digests.
This task does not create playable MoonPalace maps.
This task does not mutate generated terrain, tilemaps, scenes, prefabs, or runtime objects.
```

## 1. 사용자 보고 의무

Result 첫 부분에는 반드시 아래 두 섹션을 둔다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`User-Facing Implementation Report`에는 아래 질문에 답한다.

```text
이번 작업이 월궁 제작을 위해 무엇을 고정했는가?
실제 이동 수치는 어떤 값/범위로 기록됐고 player code를 바꾸지 않았다는 증거는 무엇인가?
네 바이옴의 density/pacing/difficulty는 어떻게 구분되는가?
TileCode/collision/material/audio/background shell은 어떤 방식으로 연결되는가?
없는 art/audio/background asset은 어떻게 MissingData 또는 fallback으로 표시하는가?
이번 작업이 풀 맵 생성, 패턴 제작, 클러스터 확장, Scene/Prefab 변경을 하지 않았다는 증거는 무엇인가?
legacy regression을 돌리지 않았다는 증거는 무엇인가?
MAP21_02는 왜 아직 시작하지 않았는가?
```

`Responsibility and Added Scripts`는 반드시 표로 작성한다.

```text
| Script or file | Added or changed responsibility | Explicit non-ownership |
```

파일별 책임이 구체적이지 않으면 FAIL이다.

## 2. 선행조건

작업 시작 전에 다음을 확인한다.

```text
MapDesign/MCP/REPORTS/MAP20_06_MAP20_TOOLING_EXIT_TESTS_RESULT.md exists
MAP20_06 Result STATUS: PASS
MAP20_06 Result SHA-256:
ed73ee40f6f197c9e3d3582acbab7527d25de662a45a8a6eeeb4ae67e52da4bb

MapDesign/MCP/TASKS/MAP20_06_MAP20_TOOLING_EXIT_TESTS.md exists
MAP20_06 installed Task SHA-256:
1702533bac5ea69e1bcd687092e2ac6ab02268e1341b5d8a39a0677cd1af3494

MAP21_01 handoff digest:
828b00646fc8ab5936625244471f717e5e25cbf6edf49f8295d60e8b5fec514c

MAP20 PHASE EXIT: APPROVED
MAP21_02_PRODUCTIONIZE_24_MICROPATTERNS: LOCKED
```

불일치 시:

```text
STATUS: BLOCKED
reason: precondition mismatch
created/changed code files: 0
MAP21_02 started: NO
STOP
```

## 3. 허용 범위

허용 파일:

```text
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/MoonPalaceProductionProfile.cs
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/MoonPalaceTileShell.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/MoonPalace/MoonPalaceProfilePublisher.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/MoonPalace/MoonPalaceProfileTests.cs
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_01/**
MapDesign/MCP/GENERATED/MAP21_01/**
MapDesign/MCP/TASKS/MAP21_01_LOCK_MOONPALACE_PROFILES_AND_TILE_SHELL.md
MapDesign/MCP/REPORTS/MAP21_01_LOCK_MOONPALACE_PROFILES_AND_TILE_SHELL_RESULT.md
MapDesign/MCP_ARCHIVE/MAP21_01_LOCK_MOONPALACE_PROFILES_AND_TILE_SHELL.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

조건부 허용:

```text
Existing MAP19 traversal/rule profile may be read for value compatibility.
Existing MAP20 exit audit JSON may be read and hashed.
Existing asset references may be recorded as stable paths/tokens if they already exist.
Missing art/audio/background references must be explicit MissingData or fallback tokens.
Authoring CSV writes are allowed only under Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_01.
Generated report JSON writes are allowed only under MapDesign/MCP/GENERATED/MAP21_01.
```

금지:

```text
Player/controller movement code change
physics/collider behavior change
pattern/cluster/activity/special/sector/world generation
MAP21_02 micropattern production
cluster pool expansion
Activity/Event/Region production
Tilemap bake or Tilemap mutation
Scene / Prefab / ProjectSettings / Packages changes
sprite/audio/background asset import
Addressables group/build change
runtime GameObject instantiate/enable/disable/destroy during tooling actions
generator solve/reroll/execution
validation runner execution
actual replay execution
rollback execution
CSV authoring edit outside MAP21_01 authoring folder
auto-fix / auto-repair
production seed approval
external process launch
manual gameplay test
automatic MAP21_02 start
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
legacy 19347 regression
prior task category selection
PlayMode selection
unfiltered test run
full regression run
```

## 4. Profile 계약

`MoonPalaceProductionProfile` must define immutable records only.

Required movement fields:

```text
profile_id
schema_version
source_MAP20_phase_exit_digest
walk_speed_tiles_per_second
run_speed_tiles_per_second
jump_height_tiles
jump_distance_tiles
max_safe_drop_tiles
player_width_cells
player_height_cells
head_clearance_cells
landing_clearance_cells
recovery_time_seconds_min
recovery_time_seconds_max
traversal_profile_digest
created_utc_excluded_from_canonical_digest
canonical_digest
```

Required biome profile fields:

```text
biome_id
density_min
density_max
quiet_ratio_min
quiet_ratio_max
cluster_ratio_min
cluster_ratio_max
activity_ratio_min
activity_ratio_max
overlay_ratio_min
overlay_ratio_max
verticality_band
difficulty_band
primary_material_token
ambient_audio_token
background_token
canonical_digest
```

Required biome ids:

```text
MoonCrater
CassiaRoot
AbandonedMill
MoonDough
```

Rules:

```text
Movement values must be explicit numbers, not comments or TODO text.
Biome ranges must be ordered min <= max and remain inside 0.0..1.0.
Ratios must be initial production anchors, not final MAP21_10 tuning claims.
No player/controller source file may be modified.
```

## 5. Tile Shell 계약

`MoonPalaceTileShell` must define immutable TileCode records.

Required tile shell fields:

```text
tile_code
tile_role
layer_token
collision_kind
material_token
footstep_audio_token
impact_audio_token
background_token
biome_allowlist
asset_reference_kind
asset_reference
fallback_tile_code
missing_reason
render_priority
canonical_digest
```

Allowed collision kinds:

```text
Solid
OneWay
PassThrough
Hazard
Decor
Background
MissingData
```

Minimum required tile roles:

```text
Ground
Wall
Ceiling
OneWayPlatform
SlopeOrStep
Hazard
Decor
Background
BoundaryBlend
DebugMissing
```

Rules:

```text
tile_code values must be unique and stable.
Every non-MissingData record must have collision_kind, material_token, and fallback_tile_code.
Missing asset references must be explicit; do not import assets in this Task.
No tilemap, scene, prefab, package, or addressable asset is changed.
```

## 6. Snapshot / Digest 계약

Required generated artifacts:

```text
MapDesign/MCP/GENERATED/MAP21_01/moonpalace_profile_manifest.json
MapDesign/MCP/GENERATED/MAP21_01/moonpalace_tile_shell_manifest.json
MapDesign/MCP/GENERATED/MAP21_01/moonpalace_profile_digest_manifest.json
```

Digest manifest required fields:

```text
schema_version
task_id
source_MAP20_06_result_digest
source_MAP20_phase_exit_digest
MAP21_01_handoff_digest
movement_profile_digest
biome_profile_digest
tile_shell_digest
MAP21_02_handoff_digest
created_utc_excluded_from_canonical_digest
canonical_digest
```

All canonical digests must exclude `created_utc`, use deterministic order, and be lowercase SHA-256.

## 7. 중복과 하드코딩 방지

Result에 아래 카운터를 기록하고 모두 0이어야 한다.

```text
player/controller C# modified:
generation pipeline C# modified:
MAP20 production/test C# modified:
Scene/Prefab/ProjectSettings/Packages modified:
duplicated generator logic count:
duplicated validation logic count:
duplicated replay/rollback logic count:
hard-coded asset path copies outside MAP21_01 authoring/sample constants:
hard-coded digest string copies outside precondition/handoff constants:
```

## 8. Focused Tests

Focused test category:

```text
MAP21_01
```

Required test names:

```text
MoonPalaceProfileLocksExplicitMovementNumbersWithoutPlayerCodeChanges
MoonPalaceBiomeProfilesDefineFourBiomesWithValidDensityPacingAndDifficulty
MoonPalaceTileShellDefinesUniqueTileCodesCollisionMaterialAudioAndBackgroundTokens
MoonPalaceTileShellRepresentsMissingAssetsAsMissingDataOrFallbackWithoutImport
MoonPalaceProfileRejectsDuplicateBiomeIdsTileCodesAndInvalidRanges
MoonPalaceProfileArtifactsSerializeDeterministicallyAcrossRepeatCultureAndInputOrder
MoonPalaceProfilePublishesMap21_02HandoffOnlyAfterFocusedPass
MoonPalaceProfileDoesNotRunGenerationValidationReplayRollbackPlayModeOrLegacyRegression
```

Required count:

```text
discovered: 8
executed: 8
passed: 8
failed: 0
```

Focused validation may create sample artifacts only under `MapDesign/MCP/GENERATED/MAP21_01`.

## 9. 회귀 금지 정책

이번 Task의 검증은 focused MAP21_01 EditMode selection과 compile/console check까지만 허용한다.

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
VALIDATION RUNNER EXECUTIONS: 0
REPLAY EXECUTIONS: 0
GENERATOR EXECUTIONS: 0
ROLLBACK EXECUTIONS: 0
CSV AUTHORING WRITES OUTSIDE MAP21_01 AUTHORING FOLDER: 0
RUNTIME OBJECT SPAWNS: 0
SCENE PREFAB CHANGES: 0
```

## 10. Result 필수 기록

Result 파일:

```text
MapDesign/MCP/REPORTS/MAP21_01_LOCK_MOONPALACE_PROFILES_AND_TILE_SHELL_RESULT.md
```

Required sections:

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
## Movement and Biome Profile Summary
## Tile Shell and Presentation Summary
## Snapshot and Digest Summary
## Focused Validation Summary
## No Legacy Regression Boundary Notes
## Final Status Evidence
```

Required summary fields:

```text
MAP20_06 Result SHA-256 required/actual:
MAP20_06 installed Task SHA-256 required/actual:
MAP21_01 handoff digest required/actual:
MAP20 phase exit approved:
MAP20 phase exit digest:

movement profile required fields present/required:
movement numbers explicit:
player/controller C# modified:
biome ids:
biome profile records:
biome density/pacing ranges valid:

tile shell required fields present/required:
tile shell records:
unique tile codes:
collision kind tokens:
minimum tile roles covered:
missing asset references represented explicitly:
sprite/audio/background asset imports:
Scene/Prefab/Addressables changes:

authoring files written:
generated sample artifacts created:
moonpalace profile manifest digest lower-hex SHA-256:
moonpalace tile shell manifest digest lower-hex SHA-256:
moonpalace profile digest manifest lower-hex SHA-256:
MAP21_02 handoff digest lower-hex SHA-256:

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

## 11. PASS / FAIL / BLOCKED

PASS 조건:

```text
Precondition SHA and handoff digest match
MAP20 phase exit is approved
Movement profile contains explicit numeric values and does not modify player/controller code
Four MoonPalace biome ids are present with valid density/pacing/difficulty records
Tile shell has unique tile codes, allowed collision kinds, required tile roles, and explicit fallback/MissingData
No sprite/audio/background import, Scene/Prefab/Addressables change, or runtime object mutation
No generation, validation runner, replay, rollback, Tilemap bake, or production seed approval
Focused MAP21_01 EditMode tests are 8/8 PASS
All no-regression and no-execution counters are zero
Responsibility and Added Scripts table is present and specific
MAP21_02 remains LOCKED and not started
```

FAIL 조건:

```text
Movement values are TODO/comment-only or player/controller code is modified
Biome density/pacing ranges are invalid or required biome ids are missing
TileCode duplicates exist or required tile roles/collision kinds are missing
Missing art/audio/background references are silently treated as valid assets
Assets are imported, Addressables are changed, or Scene/Prefab/Tilemap is mutated
Generator, validation runner, replay, rollback, PlayMode, legacy regression, unfiltered run, or full regression is executed
CSV authoring is written outside MAP21_01 authoring folder
MAP21_02 starts
```

BLOCKED 조건:

```text
Precondition SHA mismatch
Compile error unrelated to this task that prevents focused validation
Existing movement/traversal data cannot be referenced without changing player/controller code
Existing asset system cannot represent MissingData/fallback without importing assets
Authoring CSV location is unavailable or conflicts with existing authoritative MAP21 files
```

When BLOCKED:

```text
Do not finalize MAP21_01
Do not start MAP21_02
Do not publish MAP21_02 handoff digest
Record exact owner, invariant, and minimum next action
STOP
```

## 12. Finalize / Commit / STOP

PASS일 때만:

```text
MAP21_01_LOCK_MOONPALACE_PROFILES_AND_TILE_SHELL: COMPLETE
MAP21_02_PRODUCTIONIZE_24_MICROPATTERNS: LOCKED
```

FAIL 또는 BLOCKED일 때:

```text
MAP21_01 remains CURRENT
MAP21_02 remains LOCKED
STOP
```

PASS일 때만 atomic commit을 만든다.

Commit subject:

```text
MAP21_01 lock moonpalace profiles tile shell
```

Commit body에는 다음을 포함한다.

```text
MAP21_01 responsibilities
focused test count
movement profile digest
biome profile count and ids
tile shell count and collision tokens
MissingData/fallback asset count
MAP21_02 handoff digest
legacy regression counters all zero
generation/replay/rollback/validation counters all zero
Scene/Prefab/Tilemap/Addressables changes zero
```

Git push는 하지 않는다.

Result 작성, 상태 finalize, commit 이후 반드시 멈춘다.

```text
DO NOT START MAP21_02.
DO NOT CREATE MAP21_02 FILES.
DO NOT RERUN MAP19_09 SCALE AUDIT.
DO NOT REGENERATE MAP20_01 THROUGH MAP20_06 ARTIFACTS.
DO NOT RUN VALIDATION RUNNER.
DO NOT EXECUTE REPLAY.
DO NOT EXECUTE GENERATOR.
DO NOT EXECUTE ROLLBACK.
DO NOT WRITE CSV AUTHORING FILES OUTSIDE MAP21_01 AUTHORING FOLDER.
DO NOT IMPORT SPRITE AUDIO OR BACKGROUND ASSETS.
DO NOT MUTATE SCENE PREFAB TILEMAP ADDRESSABLES OR RUNTIME OBJECTS.
DO NOT RUN LEGACY REGRESSION.
STOP.
```
