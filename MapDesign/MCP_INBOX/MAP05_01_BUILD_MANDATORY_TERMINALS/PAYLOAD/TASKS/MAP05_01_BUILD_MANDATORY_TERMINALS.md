# MAP05_01 — Build Mandatory Terminals

```yaml
status_control:
  task_key: MAP05_01_BUILD_MANDATORY_TERMINALS
  result_file: REPORTS/MAP05_01_BUILD_MANDATORY_TERMINALS_RESULT.md
```

## TASK TYPE

```text
RUNTIME IMMUTABLE P03 MANDATORY-TERMINAL MODELS + DETERMINISTIC BUILDER + EDITMODE TESTS
```

## Objective

MAP03의 approved `SiteReservationSnapshot`과 MAP04의 approved `BiomePatchValidationPublication`을 받아 P03 mandatory route가 연결할 exact terminal set을 만든다.

```text
StartAnchor 1
Boss entry 1
Forge entry 1
CoreResource entry 3
Village entry 1
total terminals 7
```

Start는 `SiteReservationSnapshot.StartAnchor` 자체를 terminal로 보존한다. 나머지 six required site는 각 `SiteEntryAnchor`의 footprint sector와 `TryGetExteriorSector` 결과를 보존하고, 실제 route approach coordinate는 exterior sector다. builder는 source snapshot/publication을 수정하지 않고 immutable terminal set만 publish한다.

route mask lookup, connector tree, cost/path search, horizontal run, Type2/3 gateway, U/D conflict, loop, final graph, `PASS_ROUTE`, generated CSV, root/retry, overlay는 구현하지 않는다.

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
12. `REPORTS/MAP04_11_MAP04_BATCH_AND_EXIT_TESTS_RESULT.md`

prior Result exact gate:

```text
STATUS: PASS
MAP04 EXIT: APPROVED
MAP PROGRESS TEST SCENE: READY
MAP05 ENTRY: ELIGIBLE FOR SEPARATE PATCH
MAP05_01_BUILD_MANDATORY_TERMINALS: LOCKED / DO NOT START
SHA-256: afa5b5da9eba2a5ea93cd81ca40cfe2ae57ef6febd09190b843e652dc98224db
```

이 별도 patch가 적용된 뒤에만 MAP05_01을 실행한다.

## Map Package Reference

exact installed Map Package v1.0이 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/01_WORLD_FIXED_SPEC.md
01_FIXED_SPEC/02_COORDINATE_AND_ID_RULES.md
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
01_FIXED_SPEC/06_ROUTE_TOPOLOGY_CONSTRAINTS.md
02_PHASE_ROADMAP/MAP05_ROUTE_123_GENERATOR.md
03_CSV_SCHEMA/CSV_RELATIONSHIPS.md
04_CSV_STARTER/special_map_entry_sockets.csv
04_CSV_STARTER/sector_route_masks.csv
04_CSV_STARTER/generation_profiles.csv
```

reference는 terminal/entry/type/domain 확인용이다. installed Authoring CSV를 다시 읽거나 파싱하지 않는다. source of truth는 typed P01/P02 publication이다.

## READ ALLOWLIST

### Existing domain / P01

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGridIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteEntryAnchor.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationPublication.cs
```

### Existing P02 approved publication chain

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomeSectorOwnership.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchSnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/PatchCleanupPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchExportPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchValidationDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchValidationPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchValidationResult.cs
```

### Focused tests / assemblies

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationModelsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/BiomePatchModelsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/BiomePatchValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map04ExitTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

위 파일과 matching meta, approved Runtime/Test `Generation` 직계 path-only inventory, Authoring CSV/meta count/hash, 전체 meta GUID, task marker 이후 change-scope path만 확인할 수 있다.

금지:

- installed Authoring CSV body 재파싱·수정
- MAP05_02 이후 Task body
- unrelated production/test C# body
- Legacy/Stage/P6/P11 generator body
- Scene/Prefab YAML

## WRITE ALLOWLIST

### 신규 Runtime production C# — exact 8

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteTerminalId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteTerminalKind.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteTerminal.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteTerminalSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryTerminalBuildError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryTerminalBuildDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryTerminalBuildResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryTerminalBuilder.cs
```

### 신규 Runtime EditMode test — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryTerminalBuilderTests.cs
```

신규 C# 9개와 matching `.cs.meta` 9개, Result 1만 생성한다. existing Assets/CSV/meta/asmdef/Scene/Prefab를 수정하지 않는다. 기존 approved directory를 재사용하고 folder meta를 만들지 않는다.

## Namespace / Assembly

```text
Runtime namespace: StarNight.Map.WorldGeneration.Generation
Test namespace:    StarNight.Map.Tests.WorldGeneration.Generation
Runtime assembly:  Game.Map.Runtime
Test assembly:     Game.Map.Tests.EditMode
```

`UnityEditor`, Unity object/lifecycle, record/record struct, `required`, `init`, nullable-reference directive, reflection factory, singleton/static mutable state를 도입하지 않는다.

## Frozen P03 Boundary

```text
Input artifacts  = SITE_RESERVATIONS + BIOME_PATCHES
Output artifact  = MANDATORY_TERMINALS
Pass ID          = PASS_ROUTE
RNG stream       = RNG_ROUTE / WORLD
Grid             = 13 x 13 / 169 sectors / lower-left origin
Allowed routes   = 1 | 2 | 3
Terminal count   = 7
```

이번 Task의 RNG consumption은 exact `0`이다. terminal은 approved P01 identity에서 파생되며 후보 추첨이나 route topology 결정을 하지 않는다.

## `MandatoryRouteTerminalId` Contract

`public readonly struct`, `IEquatable<MandatoryRouteTerminalId>`, `IComparable<MandatoryRouteTerminalId>`다.

```text
string Value
bool IsValid
MandatoryRouteTerminalId(string value)
bool TryCreate(string value, out MandatoryRouteTerminalId result)
```

- grammar exact `^[A-Z0-9_]+$`; default invalid.
- equality/order는 ordinal case-sensitive, hash는 deterministic이다.
- culture/time/process randomized hash에 의존하지 않는다.
- builder의 exact IDs:

```text
TERM_00_START
TERM_01_SITE_MOON_BOSS_VAULT_ENTRY_L
TERM_02_SITE_MOON_SEAL_FORGE_ENTRY_L
TERM_03_SITE_CASSIA_SAP_HEART_ENTRY_L
TERM_04_SITE_DEEP_STAR_YEAST_ENTRY_L
TERM_05_SITE_MOON_CORE_METEOR_ENTRY_L
TERM_06_SITE_PRIMARY_VILLAGE_ENTRY_L
```

generic site ID는 `TERM_` + two-digit reservation order + `_` + source definition ID + `_` + entry socket ID다. Start는 exact `TERM_00_START`다. seed/coordinate/random/time을 넣지 않는다.

## Kind / Terminal Contract

`MandatoryRouteTerminalKind` exact order:

```text
Start
SiteEntry
```

`MandatoryRouteTerminal` immutable properties:

```text
MandatoryRouteTerminalId TerminalId
MandatoryRouteTerminalKind Kind
int TerminalOrder
SiteReservationId ReservationId
SiteReservationKind ReservationKind
string SourceDefinitionId
string EntrySocketId
SectorCoord AnchorSector
SectorCoord ApproachSector
SiteEntrySide? EntrySide
IReadOnlyList<int> AllowedRouteTypes
bool Required
bool ReturnPathRequired
```

Start exact semantics:

```text
Kind                 = Start
TerminalOrder        = 0
ReservationKind      = Start
EntrySocketId        = empty
AnchorSector         = snapshot.StartAnchor
ApproachSector       = snapshot.StartAnchor
EntrySide            = null
AllowedRouteTypes    = 1|2|3
Required             = true
ReturnPathRequired   = true
```

SiteEntry exact semantics:

```text
TerminalOrder        = source reservation.ReservationOrder (1..6)
AnchorSector         = SiteEntryAnchor.FootprintSector
ApproachSector       = TryGetExteriorSector(out exterior)
EntrySide/routes/flags/socket/reservation = source anchor exact
```

- undefined kind/enum, invalid IDs/order/coordinates, mismatched kind fields를 거부한다.
- routes는 non-empty unique subset `1|2|3`, ascending copied read-only다.
- SiteEntry approach는 world-bound, source footprint 밖, 모든 reservation footprint 밖의 unreserved sector여야 한다.
- Start에 synthetic side/socket/exterior를 만들지 않는다.
- multiple terminals sharing one approach sector는 source validator가 허용한 identity이므로 표현 가능하다. builder는 merge/drop하지 않는다.

## Builder API

```text
public sealed class MandatoryTerminalBuilder

MandatoryTerminalBuildResult Build(
    SiteReservationSnapshot siteSnapshot,
    BiomePatchValidationPublication biomePublication)
```

checked-in public API shape가 다르면 existing typed property name을 사용하되 의미를 바꾸지 않는다. builder는 Registry/root/RNG/clock/filesystem/CSV/Unity lifecycle에서 자체 조회하지 않는다.

### Structural Preflight

output allocation 전에 가능한 오류를 accumulated, ordinal sorted, deduped한다.

- inputs non-null and completed/approved publication chain
- world seed exact same
- P02 publication의 source site snapshot과 input site snapshot identity가 exact compatible
- site snapshot exact `7 reservations / 169 sectors / 6 entries / 4 Core seeds`
- reservation order exact `0..6`, kinds exact `Start/Boss/Forge/CoreResource×3/Village`
- exact source/reservation IDs from MAP03_09
- Start exact one, entry zero, StartAnchor world-bound and own occupied sector
- six non-Start sites each required entry exact one; no unexpected optional second entry in P03 input
- each entry required/return true, allowed routes exact `1|2|3`
- entry footprint/side/exterior identity valid; exterior unreserved and world-bound
- duplicate terminal ID/order/reservation+socket identity 거부
- P02 rules `15/15`, violations/errors `0`, patches/assigned/unassigned `17/165/4`
- terminal builder does not require every approach sector to have the same PrimaryBiome as its site

invalid input은 RNG/file/mutation `0`, terminal set null, diagnostics null-or-empty, sorted errors `>=1`, retry false다. constructor exception text/stack/path/culture를 message에 넣지 않는다.

## Exact Terminal Build Order

```text
1. preflight (RNG 0)
2. create Start terminal from StartReservation/StartAnchor
3. enumerate non-Start reservations by ReservationOrder then ID ordinal
4. enumerate each entry by EntrySocketId ordinal
5. create one SiteEntry terminal per required entry
6. validate cross-terminal ID/order/source/coordinate identity
7. atomically create MandatoryRouteTerminalSet
```

No shuffle, weight, random tie-break, coordinate sort, nearest-neighbor ordering을 사용하지 않는다.

## Terminal Set / Diagnostics / Result

`MandatoryRouteTerminalSet` immutable properties/API:

```text
ulong WorldSeed
SiteReservationSnapshot SourceSiteSnapshot
BiomePatchValidationPublication SourceBiomePublication
MandatoryRouteTerminal StartTerminal
IReadOnlyList<MandatoryRouteTerminal> Terminals
int TerminalCount
int SiteEntryTerminalCount
bool TryGet(MandatoryRouteTerminalId id, out MandatoryRouteTerminal terminal)
bool TryGetByReservation(SiteReservationId id, out MandatoryRouteTerminal terminal)
```

- source reference identities를 보존하고 mutate/clone하지 않는다.
- exact 7 terminals, one Start, six SiteEntry, order `0..6`.
- lookup은 ordinal ID이고 mutable dictionary를 노출하지 않는다.
- constructor는 duplicate ID/order/reservation, missing Start, source mismatch, invalid approach를 거부한다.

`MandatoryTerminalBuildError` fields:

```text
MandatoryTerminalBuildErrorCode Code
string FirstId
string SecondId
int SectorIndex
string Message
```

codes exact stable order:

```text
MissingInput
InvalidSiteSnapshot
InvalidBiomePublication
WorldSeedMismatch
SourceSnapshotMismatch
ReservationCountMismatch
ReservationIdentityMismatch
EntryCountMismatch
EntryIdentityMismatch
EntryOutsideWorld
EntryExteriorReserved
DuplicateTerminalIdentity
```

errors sort/dedupe: code, first/second ID ordinal, sector index, message ordinal.

diagnostics immutable fields:

```text
ulong WorldSeed
int ReservationCount
int ReservedSectorCount
int BiomePatchCount
int BiomeAssignedSectorCount
int BiomeUnassignedSectorCount
int TerminalCount
int StartTerminalCount
int SiteEntryTerminalCount
int RequiredTerminalCount
int ReturnPathRequiredTerminalCount
int SharedApproachSectorCount
int RngDrawCount
int SourceMutationCount
```

starter Completed expected:

```text
reservations/reserved = 7/8
patches/assigned/unassigned = 17/165/4
terminals = 7 = 1 Start + 6 SiteEntry
required/return = 7/7
RNG/mutation = 0/0
```

Result status:

```text
Completed    terminal set + diagnostics, errors 0, retry false
InvalidInput terminal set null, errors >=1, retry false
```

이 Task에는 route-generation rejection/retry status가 없다. routing 실패와 `route_retry_max=200`은 MAP05_03 이후 P03 전체 재시도 범위다.

## Determinism / Immutability

- same logical input, shuffled caller-visible collection exposure, fresh/reused builder, `en-US`/`tr-TR`, thread/time 변화에서 exact same terminal IDs/order/coordinates/diagnostics.
- source collections와 nested lists defensive immutable observable state를 유지한다.
- RNG method calls/raw draws exact `0`.
- static cache/current set, filesystem, Unity object state, current culture ordering을 사용하지 않는다.
- site/biome snapshots, sector role/ownership, entry anchor를 수정하지 않는다.

## Scope Boundary / DO NOT

- `sector_route_masks` lookup/validation 구현 금지 — MAP05_02
- connector cost/tree/edge 구현 금지 — MAP05_03
- horizontal router/Type1 assignment 금지 — MAP05_04
- Type2/3 gateway/U-D conflict/loop 금지 — MAP05_05~07
- `MandatoryRouteGraph`, `SectorCell.RouteMaskId`, generated edges/CSV 금지 — MAP05_08
- final validator/overlay/batch/root/adapter 금지 — MAP05_09~11
- Type0, microchunk, tile reachability, SpecialMap assembly 금지
- synthetic cap/dead-end/Type0 terminal/extra beacon 금지
- existing production/test/meta/asmdef/CSV/Scene/Prefab 수정 금지
- test skip/ignore/assertion 완화, Git operation 금지

## Required Tests

`MandatoryTerminalBuilderTests.cs` actual NUnit cases 최소 `96`개다.

minimum groups:

- terminal ID valid/invalid/default/equality/order/hash/culture
- kind enum undefined rejection
- Start exact ID/order/source/anchor/approach/null side/routes/flags
- six exact SiteEntry IDs/order/source/anchor/exterior/side/routes/flags
- exact seven ordering and lookups
- missing/null/duplicate/unexpected reservation and entry errors
- Start entry present, non-Start entry missing/extra/optional/return false rejection
- exterior four directions, world edge, occupied exterior, wrong footprint/side rejection
- seed/source publication mismatch and non-approved P02 rejection
- source mutation isolation/public mutable surface audit
- shuffled/culture/thread/fresh-reused determinism
- RNG/file/time/UnityEditor/static mutable dependency audit
- no route mask/tree/edge/graph/CSV/root/later-task production symbol

Actually run:

```text
MandatoryTerminalBuilderTests >=96 PASS
SiteReservationValidatorTests 268/268 PASS
BiomePatchValidatorTests       196/196 PASS
Map04ExitTests                 110/110 PASS
Actually executed total       >=670 PASS
failed/skipped                   0/0
```

large suites discovery-only under reduced profile:

```text
Game.Map targeted discovery >=5461
Full EditMode discovery      >=5573
```

forced refresh/compile/Console/relevant warning `0/0/0`.

## Asset / Meta / Change Gate

clean baseline:

```text
Authoring CSV/meta = 50/50
Assets meta = 3152
accepted legacy Editor folder meta = 6/6
duplicate GUID groups = 0
```

completion:

```text
new Runtime production C# = 8
new Runtime test C# = 1
new matching cs.meta = 9
final Assets meta = 3161
task-marker 이후 exact Assets changes = 18
existing Assets modifications = 0
unexpected Assets changes = 0
new directory/folder meta = 0
```

new meta는 `fileFormatVersion: 2`, unique lowercase 32-hex GUID다. Authoring CSV/meta, progress test Scene, accepted legacy meta를 바이트 보존한다.

## Failure Policy

- contract/test/compile/meta/change-scope 한 조건이라도 불일치하면 `STATUS: FAIL`.
- Unity/Test Runner 접근이 없어 actual compile/tests를 실행하지 못하면 `STATUS: BLOCKED`.
- FAIL/BLOCKED를 source 수정, local repair, assertion 완화, later Task 구현으로 해결하지 않는다.
- PASS가 아니면 finalize하지 않고 MAP05_02를 열지 않는다.

## Result / Completion

Result: `REPORTS/MAP05_01_BUILD_MANDATORY_TERMINALS_RESULT.md`.

Result는 `<=150 lines`로 아래를 기록한다.

```text
TASK / STATUS / SUMMARY
PATCH APPLY / READ / CREATED / MODIFIED / PREEXISTING_IDENTICAL
TERMINAL IDS / START TERMINAL / SITE ENTRY TERMINALS / TERMINAL SET
SOURCE IDENTITY / DETERMINISM / IMMUTABILITY
TEST / UNITY / ASSET META / CHANGE SCOPE / OWNERSHIP AUDIT
OUT_OF_SCOPE_FINDINGS / DONE CONDITIONS / NEXT / Recommended Commit
```

PASS일 때만 MAP05_01 COMPLETE, Current Task NONE으로 finalize한다. `MAP05_02_IMPLEMENT_ROUTE_MASK_LOOKUP`은 LOCKED로 유지하고 자동 시작하지 않는다.

Recommended Commit: `feat(map): build immutable mandatory route terminals`
