# MAP04_08 — Export Biome Patch Results

```yaml
status_control:
  task_key: MAP04_08_EXPORT_BIOME_PATCH_RESULTS
  result_file: REPORTS/MAP04_08_EXPORT_BIOME_PATCH_RESULTS_RESULT.md
```

## Goal

MAP04_07 `Completed` snapshot과 같은 seed의 immutable `GeneratedWorldData`를 결합해 다음을 atomic publish한다.

1. biome/patch assignment가 반영된 새 `GeneratedWorldData`
2. exact `generated_biome_patches.csv` UTF-8 bytes
3. existing serializer가 만든 exact `generated_world_sectors.csv` bytes

filesystem에는 쓰지 않는다. source cleanup/world/P01/P02/P03를 mutate하지 않는다. validator, overlay, pass/root integration은 범위 밖이다.

## Prior Gate / Read

control → Master/Status → 이 Task → MAP04_07 Result 순으로 읽는다.

```text
Prior Result SHA-256 7fbef41a6b6f054e2a8c6270a9cec6d3825143d0291c7c6bf5952e57f46a51dd
STATUS PASS; actual 390/390; output 17/165/4
score 0/0/100; moves 0; RNG 1912->1912
violations/mutation 0; Assets meta 3125; changes conflict 0
```

Read body allowlist:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedSectorRole.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldData.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldDataCsvSerializer.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchRole.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchSeed.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomeSectorOwnership.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchSiteBinding.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatch.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchSnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/PatchCleanupDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/PatchCleanupPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/PatchCleanupResult.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/PatchCleanupTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/BiomePatchModelsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/GeneratedWorldDataTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

Map Package reference는 `generated_biome_patches.csv`, `generated_world_sectors.csv` header/schema와 MAP04 roadmap만 읽는다. matching meta, approved filename inventory, CSV/meta hash/count, 전체 meta GUID, `.APPLIED` 이후 path-only scope도 허용한다. installed Authoring CSV body, unrelated C#, future Task, Legacy, Scene/Prefab YAML은 읽지 않는다.

## Write Allowlist

신규 exact 6 Runtime + 1 test:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchExportError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedBiomePatchRow.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedBiomePatchCsvSerializer.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchExportPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchExportResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchExporter.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/BiomePatchExporterTests.cs
```

matching meta 7 + Result만 생성한다. existing C#/meta/asmdef/CSV/Scene/Prefab를 수정하지 않는다.

Runtime namespace `StarNight.Map.WorldGeneration.Generation`, test namespace `StarNight.Map.Tests.WorldGeneration.Generation`. Unity `6000.3.8f1` current C#; production UnityEditor/UnityEngine.Object/reflection/static mutable state 금지.

## API / Failure

```text
BiomePatchExportResult Export(
    PatchCleanupResult cleanupResult,
    GeneratedWorldData sourceWorld)
```

checked-in constructor/property shape가 illustrative API보다 우선한다. registry/RNG/clock/file/root를 조회하지 않는다.

Status는 `Completed`, `InvalidInput` 두 개다. invalid는 publication null, stable sorted/deduped errors `>=1`, source mutation `0`이다.

Minimum error codes:

```text
MissingCleanupResult
CleanupNotCompleted
MissingCleanupPublication
MissingCleanupDiagnostics
MissingSourceWorld
SeedMismatch
InvalidPatchSnapshot
InvalidSourceWorld
ConflictingExistingBiomeAssignment
SerializationFailure
InternalInvariantViolation
```

validation: source seed/snapshot seed 동일; exact 169 index-coordinate rows; viable `17 patches / 165 assigned / 4 reserved-unassigned`; patch↔ownership↔seed↔binding cross-consistency; source world biome fields는 empty 또는 exact target이며 conflicting 값은 거부한다.

## World Assignment

index `0..168` 순으로 새 `SectorCell`을 만든다.

- assigned ownership: `PrimaryBiomeId`, `SecondaryBiomeId`, `PatchId`를 P03 값으로 설정
- unassigned ownership: 세 필드 exact empty 유지
- source의 `Role`, route/site/boundary/recipe/reservation IDs, distance, mandatory flag는 byte-logically 보존
- 모든 string/token/case를 정규화하지 않음

viable output은 assigned/unassigned `165/4`; SecondaryBiome는 169행 모두 empty다. source `GeneratedWorldData`, its cells, cleanup chain은 mutate하지 않는다.

## Patch Rows

`GeneratedBiomePatchRow`는 schema 13 fields를 immutable typed value로 보존한다.

```text
seed, patch_instance_id, biome_id, patch_role,
seed_sector_x, seed_sector_y, sector_count,
min_x, min_y, max_x, max_y, perimeter_edges,
special_map_instance_ids
```

patch order는 `PatchId.Value` ordinal. row derivation:

- seed coord = patch의 smallest `Seed.SectorIndex` 좌표
- bounds = owned SectorIndices의 min/max x/y
- perimeter = 각 owned cell의 L/R/U/D 중 world outside 또는 foreign PatchId인 edge 수
- role token = exact `CORE|SATELLITE|INTRUSION`
- special IDs = 해당 Core patch SiteBindings의 `SiteReservationId.Value`, ordinal distinct, `|` join; non-Core empty

Core multi-seed라도 CSV row는 schema의 singular seed coordinate 때문에 smallest seed를 canonical representative로 쓴다. patch/stat 계산은 source를 바꾸지 않는다.

## CSV Bytes

`GeneratedBiomePatchCsvSerializer.FileName == "generated_biome_patches.csv"`.

Exact header:

```text
seed,patch_instance_id,biome_id,patch_role,seed_sector_x,seed_sector_y,sector_count,min_x,min_y,max_x,max_y,perimeter_edges,special_map_instance_ids
```

- UTF-8 BOM exactly once; exact CRLF; final CRLF one
- header + PatchId ordinal 17 rows for viable fixture
- invariant decimal seed/int; no leading plus/locale separator
- RFC4180 quote/comma/CR/LF escaping, doubled quote
- pipe list has ordinal values, no extra spaces/empty elements
- return fresh copied `byte[]`; caller mutation cannot alter publication
- no undocumented columns, timestamp/path/GUID/JSON

world CSV는 new world를 existing `GeneratedWorldDataCsvSerializer`로 직렬화한다. filename/header/13 columns/BOM/CRLF/index-order 169 rows를 중복 구현하지 않는다.

## Publication

`BiomePatchExportPublication` minimum immutable properties:

```text
PatchCleanupPublication SourceCleanup
GeneratedWorldData SourceWorld
GeneratedWorldData WorldWithBiomeAssignments
IReadOnlyList<GeneratedBiomePatchRow> PatchRows
byte[] GeneratedBiomePatchesCsv
byte[] GeneratedWorldSectorsCsv
string BiomePatchFileName
string WorldSectorFileName
int PatchRowCount, WorldSectorRowCount, AssignedSectorCount, UnassignedSectorCount
```

byte arrays는 constructor/getter에서 방어 복사한다. success 전에 두 serializers와 cross-check를 전부 완료하고 atomic publish한다.

Cross-check:

- patch CSV PK `(seed,patch_instance_id)` unique; row count = snapshot patch count
- row counts/bounds/perimeter/bindings exact source 재계산값
- world CSV 169 rows and every primary/secondary/patch equals new world/snapshot
- patch row sector sum = assigned count; patch/assigned/unassigned conservation
- repeated/shuffled/culture/time/thread/fresh-reused calls produce identical bytes
- source mutation/RNG draw/file write `0`

## Tests / Gates

`BiomePatchExporterTests.cs` actual NUnit cases `>=120`:

- accumulated input errors, seed/169/cross-link/conflict checks
- source-world field preservation and assignment overlay
- exact 13-column patch header/schema/order/role/seed/bounds/perimeter/site-list
- BOM/CRLF/final newline/RFC4180/invariant/max ulong
- exact existing world serializer reuse and 169 row cross-check
- viable `17 rows / 165 assigned / 4 unassigned`; patch sector sum `165`
- byte-array isolation, source immutability, no filesystem/RNG
- shuffled/culture/fresh-reused determinism and failure atomicity

Actually run:

```text
BiomePatchExporterTests  >=120 PASS
PatchCleanupTests          127/127 PASS
BiomePatchModelsTests      107/107 PASS
GeneratedWorldDataTests     56/56 PASS
Required regressions        290/290 PASS
Actually executed total    >=410 PASS
failed/skipped                0/0
```

large suites는 실행하지 않고 discovery-only: Game.Map `>=4912`, Full EditMode `>=4981`. forced compile/Console/warning `0/0/0`.

Asset gate:

```text
Assets meta 3125->3132
new Runtime/test/meta 6/1/7; exact Assets changes 14
existing/unexpected 0/0; duplicate GUID 0
Authoring CSV/meta 50/50 unchanged; generated CSV files created 0
legacy Editor.meta 6/6; Scene/Prefab/Packages/ProjectSettings changes 0
```

## Compact Result / Finalize

Result `<=140 lines`: STATUS, apply/SHA, created paths+GUID, actual row/byte/hash/schema/count evidence, tests, compile/meta/scope, findings, NEXT만 기록한다. 이전 Task 설명을 복사하지 않는다.

PASS일 때만 MAP04_08 COMPLETE, Current Task NONE, Last Completed/Result를 MAP04_08로 설정하고 MAP04_09는 LOCKED로 유지한다.

금지: existing/CSV 수정, filesystem write, RNG, source mutation, schema 확장, validator/overlay/root, MAP04_09 생성/시작, Git commit/push.
