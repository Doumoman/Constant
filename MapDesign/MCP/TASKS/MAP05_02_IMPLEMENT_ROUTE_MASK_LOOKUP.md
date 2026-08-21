# MAP05_02 — Implement Route Mask Lookup

```yaml
status_control:
  task_key: MAP05_02_IMPLEMENT_ROUTE_MASK_LOOKUP
  result_file: REPORTS/MAP05_02_IMPLEMENT_ROUTE_MASK_LOOKUP_RESULT.md
```

## TASK TYPE

```text
RUNTIME IMMUTABLE P03 MANDATORY ROUTE MASK LOOKUP + DETERMINISTIC BUILDER + EDITMODE TESTS
```

## Objective

MAP01 typed `SectorRouteMaskDefinition` rows를 받아 MAP05 mandatory route에서 사용할 Type1/2/3 route mask lookup을 만든다.

이번 Task의 output은 "open L/R/U/D 조합 -> registered mandatory RouteMask ID"의 immutable lookup뿐이다.

```text
Type1: ROUTE_T1_LR  = L/R
Type2: ROUTE_T2_LRD = L/R/D
Type3: ROUTE_T3_LRU = L/R/U
```

Type0, optional/dead-end mask, connector tree, route path search, horizontal backbone, Type2/3 gateway placement, U/D conflict resolution, loops, final graph, `SectorCell.RouteMaskId`, generated CSV, validator, overlay, root/retry는 구현하지 않는다.

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
12. `REPORTS/MAP05_01_BUILD_MANDATORY_TERMINALS_RESULT.md`

prior Result exact gate:

```text
TASK: MAP05_01_BUILD_MANDATORY_TERMINALS
STATUS: PASS
TERMINAL IDS: 7 exact
TEST: MandatoryTerminalBuilderTests 120/120 PASS
UNITY: compile/Console/warnings 0/0/0
ASSET META: 3152 -> 3161
DONE CONDITIONS: PASS
SHA-256: a5ea4a2a3e7ac29de825e45e4b75a816ae2d8f5a6d4824fabf6a0676d62b2069
```

이 별도 patch가 적용된 뒤에만 MAP05_02를 실행한다.

## Map Package Reference

exact installed Map Package v1.0이 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
01_FIXED_SPEC/06_ROUTE_TOPOLOGY_CONSTRAINTS.md
02_PHASE_ROADMAP/MAP05_ROUTE_123_GENERATOR.md
03_CSV_SCHEMA/CSV_RELATIONSHIPS.md
04_CSV_STARTER/sector_route_masks.csv
04_CSV_STARTER/generation_profiles.csv
```

reference는 ID/shape/domain 확인용이다. installed Authoring CSV를 다시 읽거나 파싱하지 않는다. source of truth는 MAP01 typed route definition objects다.

## READ ALLOWLIST

### Existing MAP01 route definitions

```text
Assets/_Game/Map/Runtime/WorldGeneration/Data/RouteDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/WorldRouteDefinitionSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/WorldRouteDefinitionBuilder.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/WorldRouteDefinitionBuildResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/StaticDataRegistry.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/StaticDataRegistrySnapshot.cs
```

존재하지 않는 파일명은 실제 MAP01_07/MAP01_12 typed API 이름에 맞춰 allowlisted equivalent를 읽어도 된다. 단, Authoring CSV body를 직접 파싱하거나 registry publish 의미를 바꾸지 않는다.

### Existing P03 terminals

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteTerminalId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteTerminalKind.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteTerminal.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteTerminalSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryTerminalBuildResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryTerminalBuilder.cs
```

### Focused tests / assemblies

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/WorldRouteDefinitionBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryTerminalBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/BiomePatchValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map04ExitTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

위 파일과 matching meta, approved Runtime/Test `Generation`/`Data` 직계 path-only inventory, Authoring CSV/meta count/hash, 전체 meta GUID, task marker 이후 change-scope path만 확인할 수 있다.

금지:

- installed Authoring CSV body 재파싱·수정
- MAP05_03 이후 Task body
- unrelated production/test C# body
- Legacy/Stage/P6/P11 generator body
- Scene/Prefab YAML

## WRITE ALLOWLIST

### 신규 Runtime production C# — exact 8

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteOpenMask.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteMaskId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteMaskKind.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteMaskRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteMaskLookup.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteMaskLookupBuildError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteMaskLookupDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilder.cs
```

### 신규 Runtime EditMode test — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
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
Input artifact   = WORLD_ROUTE_DEFINITIONS.RouteMasks
Input context    = MANDATORY_TERMINALS for regression only
Output artifact  = MANDATORY_ROUTE_MASK_LOOKUP
Pass ID          = PASS_ROUTE
RNG stream       = none consumed in this Task
Allowed types    = 1 | 2 | 3
Allowed masks    = exactly 3
```

이번 Task의 RNG consumption은 exact `0`이다. lookup은 typed route definition에서 파생되며 후보 연결이나 route topology 결정을 하지 않는다.

## `MandatoryRouteOpenMask` Contract

`public readonly struct`, `IEquatable<MandatoryRouteOpenMask>`, `IComparable<MandatoryRouteOpenMask>`다.

```text
bool OpenLeft
bool OpenRight
bool OpenUp
bool OpenDown
int OpenCount
bool HasHorizontalRun
bool HasVerticalPairConflict
static MandatoryRouteOpenMask Type1Horizontal
static MandatoryRouteOpenMask Type2Down
static MandatoryRouteOpenMask Type3Up
```

- Type1 exact `L=true, R=true, U=false, D=false`
- Type2 exact `L=true, R=true, U=false, D=true`
- Type3 exact `L=true, R=true, U=true, D=false`
- `U && D`는 mandatory lookup에서 항상 invalid다.
- equality/order/hash는 deterministic이며 culture/time/process randomized hash에 의존하지 않는다.

## ID / Kind / Record Contract

`MandatoryRouteMaskId`는 `public readonly struct`, `IEquatable<MandatoryRouteMaskId>`, `IComparable<MandatoryRouteMaskId>`다.

```text
string Value
bool IsValid
MandatoryRouteMaskId(string value)
bool TryCreate(string value, out MandatoryRouteMaskId result)
```

- grammar exact `^[A-Z0-9_]+$`; default invalid.
- exact required IDs:

```text
ROUTE_T1_LR
ROUTE_T2_LRD
ROUTE_T3_LRU
```

`MandatoryRouteMaskKind` exact order:

```text
Type1
Type2
Type3
```

`MandatoryRouteMaskRecord` immutable properties:

```text
MandatoryRouteMaskId MaskId
MandatoryRouteMaskKind Kind
int RouteType
MandatoryRouteOpenMask OpenMask
bool MandatoryAllowed
bool Active
string DescriptionKo
object SourceDefinition
```

`SourceDefinition`은 실제 typed `SectorRouteMaskDefinition` reference를 보존한다. 구현상 강한 타입을 사용할 수 있으면 `SectorRouteMaskDefinition`으로 노출한다. source row를 clone/mutate하지 않는다.

## Builder API

```text
public sealed class MandatoryRouteMaskLookupBuilder

MandatoryRouteMaskLookupBuildResult Build(WorldRouteDefinitionSet definitionSet)
MandatoryRouteMaskLookupBuildResult Build(IEnumerable<SectorRouteMaskDefinition> routeMasks)
```

checked-in public API shape가 다르면 existing typed property name을 사용하되 의미를 바꾸지 않는다. builder는 Registry/root/RNG/clock/filesystem/CSV/Unity lifecycle에서 자체 조회하지 않는다.

## Structural Preflight

output allocation 전에 가능한 오류를 accumulated, ordinal sorted, deduped한다.

- input non-null
- route mask definitions collection non-null
- required exact IDs present once each
- required rows active and `mandatory_allowed == true`
- required route types exact `1/2/3`
- required open sides exact T1/T2/T3 combinations
- no active mandatory Type1/2/3 row with an unregistered ID
- no duplicate route type among mandatory Type1/2/3 rows
- no duplicate open mask among mandatory Type1/2/3 rows
- no mandatory Type1/2/3 row with missing L/R horizontal run
- no mandatory Type1/2/3 row with both U and D open
- Type0 rows remain ignored and never accepted for mandatory lookup

invalid input은 RNG/file/mutation `0`, lookup null, diagnostics null-or-empty, sorted errors `>=1`, retry false다. constructor exception text/stack/path/culture를 message에 넣지 않는다.

## Exact Lookup Build Order

```text
1. preflight all route mask definitions (RNG 0)
2. select active mandatory_allowed route_type 1/2/3 rows only
3. validate exact ID/type/open-side matrix
4. create records in route type order 1 -> 2 -> 3
5. build ID, route type, and open-mask lookup dictionaries
6. atomically create MandatoryRouteMaskLookup
```

No shuffle, weight, random tie-break, coordinate sort, path search, nearest-neighbor ordering을 사용하지 않는다.

## Lookup / Diagnostics / Result

`MandatoryRouteMaskLookup` immutable properties/API:

```text
IReadOnlyList<MandatoryRouteMaskRecord> Records
int Count
MandatoryRouteMaskRecord Type1
MandatoryRouteMaskRecord Type2
MandatoryRouteMaskRecord Type3
bool TryGetById(MandatoryRouteMaskId id, out MandatoryRouteMaskRecord record)
bool TryGetByRouteType(int routeType, out MandatoryRouteMaskRecord record)
bool TryGetByOpenMask(MandatoryRouteOpenMask openMask, out MandatoryRouteMaskRecord record)
MandatoryRouteMaskRecord GetRequired(MandatoryRouteMaskKind kind)
```

- exact 3 records, order Type1/Type2/Type3.
- lookup은 ordinal ID이고 mutable dictionary를 노출하지 않는다.
- constructor는 duplicate ID/type/open-mask, missing required, unsupported mask를 거부한다.
- records expose copied read-only state; source definition reference는 read-only로만 보존한다.

`MandatoryRouteMaskLookupBuildError` fields:

```text
MandatoryRouteMaskLookupBuildErrorCode Code
string FirstId
string SecondId
int RouteType
string Message
```

codes exact stable order:

```text
MissingInput
MissingRequiredMask
DuplicateMaskId
DuplicateRouteType
DuplicateOpenMask
InactiveRequiredMask
MandatoryNotAllowed
UnexpectedMandatoryMask
InvalidRouteType
InvalidOpenMask
UnsupportedVerticalPair
```

errors sort/dedupe: code, route type, first/second ID ordinal, message ordinal.

diagnostics immutable fields:

```text
int SourceRouteMaskCount
int ActiveRouteMaskCount
int MandatoryAllowedRouteMaskCount
int AcceptedMandatoryMaskCount
int Type1Count
int Type2Count
int Type3Count
int IgnoredType0Count
int RejectedMandatoryCandidateCount
int RngDrawCount
int SourceMutationCount
```

starter expected:

```text
source rows = 15
active rows = 15
mandatory allowed rows = 3
accepted masks = 3 = Type1 1 + Type2 1 + Type3 1
ignored Type0 = 12
RNG/mutation = 0/0
```

Result status:

```text
Completed    lookup + diagnostics, errors 0, retry false
InvalidInput lookup null, errors >=1, retry false
```

이 Task에는 route-generation rejection/retry status가 없다. routing 실패와 `route_retry_max=200`은 MAP05_03 이후 P03 전체 재시도 범위다.

## Determinism / Immutability

- same logical input, shuffled route mask order, fresh/reused builder, `en-US`/`tr-TR`, thread/time 변화에서 exact same records/order/diagnostics.
- source definitions and nested lists defensive immutable observable state를 유지한다.
- RNG method calls/raw draws exact `0`.
- static cache/current set, filesystem, Unity object state, current culture ordering을 사용하지 않는다.
- MAP05_01 terminal models/source P01/P02 snapshots를 수정하지 않는다.

## Scope Boundary / DO NOT

- connector cost/tree/edge 구현 금지 — MAP05_03
- horizontal router/Type1 assignment 금지 — MAP05_04
- Type2/3 gateway/U-D conflict/loop 금지 — MAP05_05~07
- `MandatoryRouteGraph`, `SectorCell.RouteMaskId`, generated edges/CSV 금지 — MAP05_08
- final route validator/overlay/batch/root/adapter 금지 — MAP05_09~11
- Type0 optional region, microchunk, tile reachability, SpecialMap assembly 금지
- synthetic cap/dead-end/Type0 terminal/extra beacon 금지
- existing production/test/meta/asmdef/CSV/Scene/Prefab 수정 금지
- test skip/ignore/assertion 완화, Git operation 금지

## Required Tests

`MandatoryRouteMaskLookupBuilderTests.cs` actual NUnit cases 최소 `112`개다.

minimum groups:

- open mask equality/order/hash/default and exact T1/T2/T3 static masks
- ID valid/invalid/default/equality/order/hash/culture
- kind enum undefined rejection
- exact starter success: 15 source rows, 12 ignored Type0, 3 accepted mandatory masks
- exact IDs/types/open sides for `ROUTE_T1_LR`, `ROUTE_T2_LRD`, `ROUTE_T3_LRU`
- lookup by ID, route type, open mask, and required kind
- shuffled input order stable output
- null/missing/duplicate ID/type/open-mask errors
- inactive required row and mandatory_allowed false rejection
- unexpected active mandatory Type1/2/3 row rejection
- Type0 rows never accepted even if open mask resembles mandatory
- L/R missing, U+D simultaneous, unsupported open combination rejection
- diagnostics counts and sorted/deduped error ordering
- source mutation isolation/public mutable surface audit
- shuffled/culture/thread/fresh-reused determinism
- RNG/file/time/UnityEditor/static mutable dependency audit
- no connector tree/router/gateway/graph/CSV/root/later-task production symbol

Actually run:

```text
MandatoryRouteMaskLookupBuilderTests >=112 PASS
MandatoryTerminalBuilderTests        120/120 PASS
SiteReservationValidatorTests        268/268 PASS
BiomePatchValidatorTests             196/196 PASS
Map04ExitTests                       110/110 PASS
Actually executed total              >=806 PASS
failed/skipped                         0/0
```

large suites discovery-only under reduced profile:

```text
Game.Map targeted discovery >=5597
Full EditMode discovery      >=5708
```

forced refresh/compile/Console/relevant warning `0/0/0`.

## Asset / Meta / Change Gate

clean baseline:

```text
Authoring CSV/meta = 50/50
Assets meta = 3161
accepted legacy Editor folder meta = 6/6
duplicate GUID groups = 0
```

completion:

```text
new Runtime production C# = 8
new Runtime test C# = 1
new matching cs.meta = 9
final Assets meta = 3170
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
- PASS가 아니면 finalize하지 않고 MAP05_03을 열지 않는다.

## Result / Completion

Result: `REPORTS/MAP05_02_IMPLEMENT_ROUTE_MASK_LOOKUP_RESULT.md`.

Result는 `<=150 lines`로 아래를 기록한다.

```text
TASK / STATUS / SUMMARY
PATCH APPLY / READ / CREATED / MODIFIED / PREEXISTING_IDENTICAL
ROUTE MASK IDS / OPEN MASK MATRIX / LOOKUP API / SOURCE IDENTITY
DETERMINISM / IMMUTABILITY
TEST / UNITY / ASSET META / CHANGE SCOPE / OWNERSHIP AUDIT
OUT_OF_SCOPE_FINDINGS / DONE CONDITIONS / NEXT / Recommended Commit
```

PASS일 때만 MAP05_02 COMPLETE, Current Task NONE으로 finalize한다. `MAP05_03_IMPLEMENT_MINIMUM_CONNECTOR_TREE`은 LOCKED로 유지하고 자동 시작하지 않는다.

Recommended Commit: `feat(map): add mandatory route mask lookup`
