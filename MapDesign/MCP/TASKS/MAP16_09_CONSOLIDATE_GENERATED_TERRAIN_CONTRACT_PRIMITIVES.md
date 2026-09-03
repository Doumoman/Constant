# MAP16_09 - Consolidate Generated Terrain Contract Primitives

```text
TASK: MAP16_09_CONSOLIDATE_GENERATED_TERRAIN_CONTRACT_PRIMITIVES
TYPE: DIRECT FORMAL CONTRACT-CHANGE TASK / NOT A single_task_v1 MCP_INBOX PATCH
DO NOT PLACE THIS FILE IN MCP_INBOX
DO NOT RUN MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md FOR THIS FILE
PREVIOUS FORMAL TASK: MAP16_08_MAP16_SLICE_AND_OUTPUT_EXIT_TESTS
PREVIOUS FORMAL TASK SHA-256: 05380053a16120e904da2aa394f9f3d1a5d7ad3e88ffedf1940f1045dc44f06d
PREVIOUS FORMAL RESULT STATUS: PASS
PREVIOUS FORMAL RESULT SHA-256: 838dd5354477efbdaf349800d5fcdba22041fb055ed16c9b868c1283629c0bb6
PRE-MAP17 OBSERVATION RESULT STATUS: PASS
PRE-MAP17 OBSERVATION RESULT SHA-256: a53e38a15f4ba1def081124cc93457eb05b648c640f78ea84992db8da8dda226
NEXT FORMAL TASK STILL LOCKED: MAP17_01_RESOLVE_ASSETS_AND_PLACE_GENERATED_CELLS
```

## 0. Why This Is Direct

`MAP16_09_CONSOLIDATE_GENERATED_TERRAIN_CONTRACT_PRIMITIVES` is not part of the already-installed compact backlog. Current `single_task_v1` rejects unknown Task IDs by design.

Therefore this file is a one-time direct formal contract-change task. It must be run by reading this MD directly in Codex CLI. It may amend MCP Master/Status/TASKS to insert MAP16_09, then execute only this consolidation, write its Result, finalize MAP16_09, and stop. It must not use the normal MCP_INBOX apply flow.

Recommended CLI command:

```text
MAP16_09_CONSOLIDATE_GENERATED_TERRAIN_CONTRACT_PRIMITIVES.md를 읽고 수행해.

이 파일은 MCP_INBOX용 single_task_v1 패치가 아니다.
MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 실행하지 마.

MAP16_09를 Master/Status/TASKS에 선행 contract-change task로 설치한 뒤,
MAP17 전에 geometry authority와 canonical digest primitive만 좁게 정리해.

Result가 PASS일 때만 MAP16_09를 COMPLETE로 finalize하고 atomic commit해.
MAP17_01은 LOCKED로 유지하고 시작하지 마.
관련 없는 worktree 변경은 건드리거나 stage하지 마.
Git push는 하지 마.
```

## 1. Goal and User Report

PRE-MAP17 structure observation found no BLOCKER, but found two HIGH risks:

```text
STR-01: geometry/public serialization authority is duplicated across WorldGenConstants, MAP15 overlay, MAP16 partition/slice/export/replay boundaries.
STR-02: canonical text -> SHA-256 lower-hex primitive is repeated across MAP16 digest classes.
```

This Task consolidates only those two contract primitives before MAP17 starts.

```text
WorldGenConstants
MicroPatternDefinition.RequiredWidth / RequiredHeight
SectorFinalCanvasLayerPlan.RequiredLayerCount
existing BakingCanonicalDigest
MAP15 world overlay serialization
MAP16 partition/slice/slot/export/replay digest boundaries
-> GeneratedTerrainGeometrySnapshot
-> shared canonical LF + UTF-8 no-BOM + SHA-256 lower-hex primitive
-> byte-for-byte identical existing digest and CSV outputs
-> MAP17_01 receives safer placement/hash authority
```

Result first section must be Korean `## User-Facing Implementation Report`. Second section must be `## Responsibility and Added Functions`.

The report must clearly say:

```text
이번 Task는 새 맵 생성 기능이 아니라 MAP17 전 구조 정리다.
중복 geometry literal과 hash primitive만 줄였다.
CSV header/field order, domain canonical line order, Result/Failure 모델, owner/source token, reference fixture 의미는 바꾸지 않았다.
기존 MAP16 digest/manifest/packet/replay 값은 byte-for-byte 동일하다.
Tilemap bake, asset placement, stable spawn id, runtime spawn, streaming/save는 아직 시작하지 않았다.
```

## 2. Formal MCP Amendment

Before source edits, verify:

```text
MAP16_08 Result exists and PASS
MAP16_08 Result SHA-256:
838dd5354477efbdaf349800d5fcdba22041fb055ed16c9b868c1283629c0bb6

PRE_MAP17_STRUCTURE_OBSERVATION_AUDIT_RESULT exists and PASS
PRE_MAP17_STRUCTURE_OBSERVATION_AUDIT_RESULT SHA-256:
a53e38a15f4ba1def081124cc93457eb05b648c640f78ea84992db8da8dda226

Current Task: NONE
MAP16_08: COMPLETE
MAP17_01: LOCKED
MAP17_01 not started
unrelated staged files: 0
```

Then install this direct task into the formal MCP documents:

```text
MapDesign/MCP/TASKS/MAP16_09_CONSOLIDATE_GENERATED_TERRAIN_CONTRACT_PRIMITIVES.md
MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST_V2.md or current master file
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

Master update:

```text
Insert MAP16_09 immediately after MAP16_08 and before MAP17.
Description:
MAP16_09_CONSOLIDATE_GENERATED_TERRAIN_CONTRACT_PRIMITIVES — MAP17 전 geometry snapshot과 canonical digest primitive를 공통 authority로 정리하고 기존 digest/CSV 호환성을 고정한다.
```

Status update before execution:

```text
MAP16_08: COMPLETE
MAP16_09_CONSOLIDATE_GENERATED_TERRAIN_CONTRACT_PRIMITIVES: CURRENT
MAP17_01_RESOLVE_ASSETS_AND_PLACE_GENERATED_CELLS: LOCKED
Current Task: MAP16_09_CONSOLIDATE_GENERATED_TERRAIN_CONTRACT_PRIMITIVES
```

Status update after PASS:

```text
MAP16_09_CONSOLIDATE_GENERATED_TERRAIN_CONTRACT_PRIMITIVES: COMPLETE
MAP17_01_RESOLVE_ASSETS_AND_PLACE_GENERATED_CELLS: LOCKED
Current Task: NONE
```

If Master/Status already contain MAP16_09 with byte-equivalent installed task content and compatible state, report the idempotent condition and continue. If they contain conflicting content, stop as `BLOCKED`.

## 3. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| immutable generated terrain geometry snapshot | new map generation behavior |
| existing public geometry values derived from authorities | TileCode / Prefab ID resolution |
| final canonical text hash primitive | generated cell placement |
| LF normalization and lower-hex SHA-256 validation | Tilemap bake |
| replacement of duplicated geometry literals in serialization/validation boundaries | collider/physics/player traversal |
| byte-for-byte digest and CSV compatibility proof | runtime streaming/save/load |
| focused EditMode tests for MAP16_09 | stable spawn id |
| formal Master/Status insertion of MAP16_09 | gameplay spawn / object attachment |
| MAP17_01 remains locked | MAP17_01 execution |

This task is intentionally small. It does not split large files, rename domain types, merge Result/Failure classes, rewrite CSV parser/writer logic, or change owner/source task tokens.

## 4. Focused-Only and No-Regression Policy

Normal verification selects only EditMode category `MAP16_09`.

```text
MAP16_09 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16_01/MAP16_02/MAP16_03/MAP16_04/MAP16_05/MAP16_06/MAP16_07/MAP16_08 category selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
full regression runs: 0
```

Compile and Console checks are allowed. Do not run tests just to feel safer.

If a real contradiction is found and broader verification seems necessary, do not run it silently. Record `REGRESSION TRIGGER DETECTED: YES`, explain the owner/invariant/reason, and stop unless the task-owned focused proof can still complete without broader selection.

## 5. Exact Write Boundary

Allowed source additions:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedTerrainGeometrySnapshot.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/GeneratedTerrainContractPrimitivesTests.cs(.meta)
```

Allowed existing source edits, only where the current project actually contains these files:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Baking/BakingCanonicalDigest.cs
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorFinalCanvasLayerPlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorCanvasProtectionDensityReport.cs
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorFinalRouteRecoveryReport.cs
Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorPatternChunkPartition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedMicroChunkSliceSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedMicroChunkMarkerSlotSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedTerrainExportPacket.cs
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedTerrainCsvExporter.cs
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedTerrainReplayVerifier.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldAssemblyOverlayExport.cs
```

Allowed MCP edits:

```text
MapDesign/MCP/TASKS/MAP16_09_CONSOLIDATE_GENERATED_TERRAIN_CONTRACT_PRIMITIVES.md
MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST_V2.md or current master file
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
MapDesign/MCP/REPORTS/MAP16_09_CONSOLIDATE_GENERATED_TERRAIN_CONTRACT_PRIMITIVES_RESULT.md
```

Forbidden:

```text
CSV schema/header/field order changes
Authoring CSV edits
Generated CSV commits
Scene / Prefab / Tilemap / ScriptableObject edits
asmdef / asmref edits
ProjectSettings / Packages edits
EditorWindow / Inspector / Scene overlay edits
PlayMode tests
stable spawn id records
runtime spawned objects
MAP17 files
large file split or namespace migration
domain Result/Failure model merge
owner/source token rename
reference fixture relocation
```

If an existing file must be changed outside this list, stop as `BLOCKED`.

## 6. GeneratedTerrainGeometrySnapshot Contract

Create an immutable public geometry snapshot in the Baking namespace.

Suggested namespace:

```text
Runtime assembly: Game.Map.Runtime
Runtime namespace: StarNight.Map.WorldGeneration.Baking
```

Required concept:

```text
GeneratedTerrainGeometrySnapshot
```

It must derive, not invent, these values from existing public authorities:

```text
sector width: 48
sector height: 32
sector cells: 1536
micro chunk width: 12
micro chunk height: 8
micro chunk cells: 96
chunk grid width: 4
chunk grid height: 4
chunk count: 16
micro pattern width: 4
micro pattern height: 4
patterns per chunk x/y: 3/2
world sectors x/y: 13/13
world sector count: 169
world width/height: 624/416
world cells: 259584
world projected slices: 2704
layers per final canvas cell: 7
sector layer records: 10752
chunk rotation allowed: false
```

Required API qualities:

```text
immutable
no UnityEngine object dependency
no filesystem dependency
no runtime static mutable cache
explicit validation result or exception-free TryCreate pattern
stable ordered canonical lines for diagnostics
```

If existing public authorities do not expose one of the required values, derive it from exposed values only. If it cannot be derived without private field access or upstream semantic changes, stop as `BLOCKED`.

## 7. BakingCanonicalDigest Contract

Extend the existing `BakingCanonicalDigest` if present. If it does not exist, create the smallest equivalent under the same Baking namespace and report why.

Required public primitive surface:

```text
NormalizeLineEndingsToLf(string text)
HashCanonicalText(string canonicalText)
HashCanonicalLines(IEnumerable<string> canonicalLines)
IsLowerHexSha256(string value)
Utf8NoBomEncoding or equivalent internal encoding authority
```

Behavior:

```text
null input is rejected deterministically
line endings normalize CRLF and CR to LF
encoding is UTF-8 without BOM
hash output is 64-char lower-hex SHA-256
hex validation is ordinal and culture-invariant
no current time, random, file path, Unity instance id, mutable static state
```

Do not change domain canonical record construction. Each existing digest class still owns what lines are included and how they are sorted. Only the final canonical text/lines -> lower-hex hash primitive should be shared.

## 8. Replacement Rules

Replace duplicated primitive usage only when it is behavior-preserving:

```text
manual SHA256.Create final hash -> BakingCanonicalDigest.HashCanonicalText/Lines
manual lower-hex validator -> BakingCanonicalDigest.IsLowerHexSha256
manual LF normalization at hash boundary -> BakingCanonicalDigest.NormalizeLineEndingsToLf
duplicated sector/chunk/world geometry literals in serialization/validation -> GeneratedTerrainGeometrySnapshot
```

Do not replace:

```text
domain canonical line names
domain field order
CSV header order
CSV escaping/writing implementation
CSV replay parsing implementation
Result/Failure/Report classes
owner/source token mappings
reference labels
test fixture coordinates unless they assert expected public output
```

All replacements must keep the exact same externally reported values.

## 9. Golden Compatibility Values

The focused tests must verify that these values remain byte-for-byte identical after consolidation:

```text
MAP16_01 final canvas output:
450645c1f7ea6f326ffb21c569bdff83b19e2c456de03dbf7770487eb8c9738d

MAP16_02 protection/density output:
549469a22af5f75f64fb14155647d84a66e85c5ad6b6ca260af55d805e88c43b

MAP16_03 route/recovery output:
9fa02be125385fb575331812435dc01f9be316f8c518f16b9e4fc3482c497c25

MAP16_04 partition output:
56352472c3da4777a56e75c1012588c0fbbfa93064559ed134ee8e5d598c45b5

MAP16_05 slice output:
deaf94c9cbb323342911f13bcf2d14f3e8715abbea4f8450b78d35d5a189a882

MAP16_06 marker slot output:
13a0e6733db9266b1e3bddc8d26dee54776ac6eb2d934a19bc2e408eda405737

MAP16_07 manifest:
557ee873aaea69efccde5cddcf3cc1bc84ba2c77522e65f0aa75bf0e0e0fa202

MAP16_07 packet/replay:
fed5b33ad83e7577998f9c3f7b604653ecb380f5d469f66c69570f72fd454189

MAP16_08 exit audit:
78d3046d62608494fb1306ff4e57a0b2d4b36eafc3a5e7e19cb8f399c3ca29f0
```

If a digest changes, do not update the golden value to match the new output. Stop as `FAIL` or `BLOCKED` and report which change altered the public byte contract.

## 10. Focused Tests

Create focused EditMode tests with category `MAP16_09`.

Suggested file:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/GeneratedTerrainContractPrimitivesTests.cs
```

Required test names:

```text
GeometrySnapshotDerivesSectorChunkPatternWorldLayerAndRecordCounts
BakingCanonicalDigestHashesLfUtf8NoBomLowerHexAndValidatesHex
GeneratedTerrainGeometrySnapshotReplacesDuplicatedSerializationLiterals
Map16DigestClassesDelegateOnlyFinalHashPrimitiveAndKeepGoldenOutputs
CsvExportReplayHeadersRowsManifestsAndDigestsRemainByteCompatible
ContractPrimitiveChangesAreDeterministicAcrossRepeatReverseCultureAndLineEndings
ObservationHighFindingsAreCoveredWithoutChangingCsvOwnerTokensOrFailures
NoTilemapScenePrefabGameObjectSpawnGeneratedAssetOrAuthoringMutation
BacklogAmendmentInstallsMap16_09AndKeepsMap17_01Locked
```

The tests must exercise:

```text
geometry snapshot required values
STR-01 covered
STR-02 covered
all nine golden compatibility digests
CSV logical file order and headers unchanged
CSV replay still PASS
owner/source token mapping unchanged
Result/Failure atomic pattern unchanged
no permanent Generated CSV committed
no Tilemap/Scene/Prefab/GameObject mutation
MAP17_01 locked handoff
```

Do not add PlayMode tests.

## 11. Minimum Result Evidence

Result must include these fields with actual values:

```text
TASK: MAP16_09_CONSOLIDATE_GENERATED_TERRAIN_CONTRACT_PRIMITIVES
STATUS: PASS | FAIL | BLOCKED
MAP16_09 installed into Master/Status/TASKS: YES
MAP17_01 remains LOCKED / NOT STARTED

geometry snapshot values covered: actual/actual
geometry literal replacements completed: actual
remaining production geometry duplicate authorities requiring MAP17 action: actual
canonical digest primitive added/extended: YES
digest classes delegating final hash primitive: actual/actual
LF normalization primitive covered: YES
UTF-8 no BOM primitive covered: YES
lower-hex validator covered: YES
golden MAP16 digest values unchanged: 9/9
CSV logical files unchanged: 6/6
CSV headers unchanged: 6/6
CSV replay verification after consolidation: PASS
owner/source token renames: 0
Result/Failure model merges: 0
reference fixture relocations: 0
Authoring reverse import attempts: 0
permanent generated CSV/assets committed: 0
stable spawn ids created: 0
runtime objects spawned: 0
Tilemap bakes: 0
Tilemap/Scene/Prefab/GameObject mutation: 0/0/0/0
production seed approvals: 0
repeat/reverse/culture/line-ending digest mismatches: 0/0/0/0
```

Focused verification block:

```text
Unity version: actual
mode: EditMode
category_names: [MAP16_09]
discovered: actual
executed: actual
passed: actual
failed: 0
skipped: actual
inconclusive: 0
compile errors: 0
relevant Console errors after final verification/clear: 0
relevant Console warnings after final verification/clear: 0

REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
FULL REGRESSION RUNS: 0
```

## 12. Commit and Stop

On PASS:

```text
write MapDesign/MCP/REPORTS/MAP16_09_CONSOLIDATE_GENERATED_TERRAIN_CONTRACT_PRIMITIVES_RESULT.md
finalize MAP16_09 COMPLETE
set Current Task NONE
keep MAP17_01 LOCKED
atomic commit only:
  - MAP16_09 source/test/meta files
  - allowed modified existing source files
  - MapDesign/MCP/TASKS/MAP16_09_CONSOLIDATE_GENERATED_TERRAIN_CONTRACT_PRIMITIVES.md
  - Master/Status documents amended for MAP16_09
  - MAP16_09 Result
commit subject: MAP16_09: consolidate generated terrain primitives
STOP
```

Do not start MAP17_01.

Git push is forbidden.
