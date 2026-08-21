# MAP06_01 — Implement Optional Region Models

```yaml
status_control:
  task_key: MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS
  result_file: REPORTS/MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS_RESULT.md
```

## TASK TYPE

```text
RUNTIME IMMUTABLE P04 TYPE0 OPTIONAL-REGION VALUE/AGGREGATE MODELS + EDITMODE TESTS
```

## Objective

MAP05에서 승인된 mandatory route graph 위에 MAP06 Type0 선택 영역이 사용할 compile-time typed immutable 데이터 계약을 만든다.

이번 Task의 산출물은 optional region ID, depth `1..4`, access rule, reward tier, return policy, attachment point, optional region cell, optional region aggregate, complete optional region snapshot이다. 자기 일관성, 불변성, deterministic ordering, token codec만 구현한다.

접점 후보 열거, optional region grower, Type0 route mask assignment, access/clue 배정, reward tier 계산 알고리즘, return device 배치, inactive buffer, validator, overlay, generated CSV writer는 구현하지 않는다.

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
12. `REPORTS/MAP05_11_MAP05_BATCH_AND_EXIT_TESTS_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP05_11_MAP05_BATCH_AND_EXIT_TESTS
STATUS: PASS
MAP05 EXIT: APPROVED
MAP06 ENTRY: ELIGIBLE FOR SEPARATE PATCH
MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS: LOCKED / DO NOT START
SHA-256: 5fdd4354d1ceee50376c3a8cd535e391af4db10baa148c682cf70247b19b40ff
```

이 별도 patch가 적용된 뒤에만 MAP06_01을 실행한다. MAP06_02 이후 Task body는 읽거나 시작하지 않는다.

## Map Package Reference

exact installed Map Package v1.0이 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/01_WORLD_FIXED_SPEC.md
01_FIXED_SPEC/02_COORDINATE_AND_ID_RULES.md
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
01_FIXED_SPEC/06_ROUTE_TOPOLOGY_CONSTRAINTS.md
02_PHASE_ROADMAP/MAP06_TYPE0_OPTIONAL_REGION.md
03_CSV_SCHEMA/CSV_RELATIONSHIPS.md
04_CSV_STARTER/sector_route_masks.csv
04_CSV_STARTER/generation_profiles.csv
05_GENERATED_OUTPUT_SCHEMA/generated_world_sectors.csv
05_GENERATED_OUTPUT_SCHEMA/generated_world_edges.csv
```

reference는 Type0/optional/access/return 용어 확인용이다. installed Authoring CSV를 다시 읽거나 파싱하지 않는다. source of truth는 approved typed MAP05 graph and validation publication이다.

## READ ALLOWLIST

### Existing domain / P00~P03

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGridIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldData.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchValidationPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteTerminalSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteMaskFamily.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphNodeId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphEdgeId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphNode.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphEdge.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraph.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteValidationReport.cs
```

### Focused tests / assemblies

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteGraphBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteGraphValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Diagnostics/MandatoryRouteOverlayTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Preview/MandatoryRouteOverlaySceneDrawerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map05ExitTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

위 파일과 matching meta, approved Runtime/Test `Generation` 직계 path-only inventory, Authoring CSV/meta count·hash, 전체 meta GUID, task marker 이후 change-scope path만 확인할 수 있다.

금지:

- installed Authoring CSV body 재파싱·수정
- generated CSV body를 disk에서 읽어 source of truth로 사용
- MAP06_02 이후 Task body
- unrelated production/test C# body
- Legacy/Stage/P6/P11 generator body
- Scene/Prefab YAML

## WRITE ALLOWLIST

### 신규 Runtime production C# — exact 6

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionAttachment.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegion.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionSnapshot.cs
```

### 신규 Runtime EditMode test — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionModelsTests.cs
```

신규 C# 7개와 matching `.cs.meta` 7개, Result 1만 생성한다. existing Assets/CSV/meta/asmdef/Scene/Prefab/Packages/ProjectSettings는 수정하지 않는다. 기존 approved directory를 재사용하고 folder meta를 만들지 않는다.

## Namespace / Assembly

```text
Runtime namespace: StarNight.Map.WorldGeneration.Generation
Test namespace:    StarNight.Map.Tests.WorldGeneration.Generation
Runtime assembly:  Game.Map.Runtime
Test assembly:     Game.Map.Tests.EditMode
```

`UnityEditor`, Unity object/lifecycle, record/record struct, `required`, `init`, nullable-reference directive, reflection factory, singleton/static mutable state를 도입하지 않는다.

## Frozen P04 Boundary

```text
Input artifacts  = MANDATORY_ROUTE_GRAPH + VALIDATION_REPORT
Output artifact  = OPTIONAL_REGION_MODELS
Pass ID          = PASS_OPTIONAL
RNG stream       = RNG_OPTIONAL / WORLD
Grid             = 13 x 13 / 169 sectors / lower-left origin
Depth            = 1..4 from attachment
Type0 invariant  = no L+R simultaneous route mask in later assignment
Mandatory guard  = mandatory route graph unchanged
```

이번 Task의 RNG consumption은 exact `0`이다. 모델은 optional region data를 표현할 뿐 후보를 만들거나 route mask를 배정하지 않는다.

MAP05 Type4 규칙은 보존한다.

```text
Type4 requires U+D open.
L/R are independent and preserve actual mandatory graph adjacency.
UD, LUD, RUD, LRUD are all legal.
```

## `OptionalRegionId` Contract

`public readonly struct`, `IEquatable<OptionalRegionId>`, `IComparable<OptionalRegionId>`다.

```text
string Value
bool IsValid
OptionalRegionId(string value)
bool TryCreate(string value, out OptionalRegionId result)
```

- grammar exact `^[A-Z0-9_]+$`; default invalid.
- equality/order는 ordinal case-sensitive, hash는 deterministic이다.
- valid `ToString()`은 exact `Value`다.
- empty/whitespace/lowercase/hyphen/non-ASCII를 거부한다.
- ID 자동 생성, seed/order 접두사 조립, random suffix는 이 Task에서 만들지 않는다.

## Enum / Token Contract

`OptionalRegionEnums.cs`는 exact enum/struct와 stateless token codec을 제공한다.

```text
OptionalRegionAccessRule: Basic, Tool, Environment, Explosive, Hidden
OptionalRewardTier: None, Low, Medium, High, Unique
OptionalReturnPolicy: BacktrackToAttachment, ReturnGateToMandatory, SafeExitToMandatory
OptionalRegionDepth: readonly struct int Value, valid 1..4
```

exact case-sensitive token mapping:

| Value | Token |
|---|---|
| Basic | `BASIC` |
| Tool | `TOOL` |
| Environment | `ENVIRONMENT` |
| Explosive | `EXPLOSIVE` |
| Hidden | `HIDDEN` |
| None | `NONE` |
| Low | `LOW` |
| Medium | `MEDIUM` |
| High | `HIGH` |
| Unique | `UNIQUE` |
| BacktrackToAttachment | `BACKTRACK` |
| ReturnGateToMandatory | `RETURN_GATE` |
| SafeExitToMandatory | `SAFE_EXIT` |

각 enum의 `TryParse...`와 `ToToken`을 제공한다. null/empty/space/case variation/numeric/undefined enum을 거부한다. `Enum.Parse`, locale case-fold, `ToString().ToUpper*`를 계약 구현으로 사용하지 않는다.

`OptionalRegionDepth`는 `1..4`만 허용하고 default invalid다.

## `OptionalRegionAttachment` Contract

optional region의 단일 입구 후보를 표현하는 sealed immutable object다.

```text
OptionalRegionId RegionId
int AttachmentOrder
int MandatoryRouteSectorIndex
SectorCoord MandatoryRouteSector
MandatoryRouteGraphNodeId MandatoryRouteNodeId
int EntrySectorIndex
SectorCoord EntrySector
int EntrySideFromMandatoryDx
int EntrySideFromMandatoryDy
OptionalRegionDepth InitialDepth
```

- RegionId, graph node ID는 valid여야 한다.
- order는 `0..9999`.
- mandatory sector와 entry sector는 exact `WorldGridIndex` identity여야 한다.
- entry sector는 mandatory sector의 cardinal neighbor여야 한다.
- direction delta는 exactly one of `(-1,0)`, `(1,0)`, `(0,1)`, `(0,-1)`이며 mandatory→entry 방향과 일치해야 한다.
- initial depth는 exact `1`.
- attachment는 후보 data일 뿐 MAP06_02 enumeration을 수행하지 않는다.

## `OptionalRegionCell` Contract

optional region이 소유할 수 있는 한 sector projection이다.

```text
OptionalRegionId RegionId
int SectorIndex
SectorCoord Sector
OptionalRegionDepth Depth
bool IsAttachmentCell
bool RequiresReturnConnection
```

- RegionId valid, index/coordinate exact identity.
- depth는 `1..4`.
- attachment cell이면 depth `1`.
- return connection requirement는 model flag만 표현하며 device/path를 만들지 않는다.
- route mask ID, generated edge row, reward spawn, clue ID는 이 Task에서 저장하지 않는다.

## `OptionalRegion` Contract

optional region aggregate sealed immutable object다.

```text
OptionalRegionId RegionId
OptionalRegionAttachment Attachment
OptionalRegionAccessRule AccessRule
OptionalRewardTier RewardTier
OptionalReturnPolicy ReturnPolicy
IReadOnlyList<OptionalRegionCell> Cells
OptionalRegionDepth MaxDepth
```

- RegionId와 Attachment.RegionId는 일치해야 한다.
- Cells는 non-empty copied read-only list다.
- 모든 cell RegionId가 일치해야 한다.
- duplicate sector index를 거부한다.
- cell은 `SectorIndex`, then `Depth`, then coordinate order로 deterministic 정렬한다.
- `MaxDepth`는 cells의 최대 depth와 일치해야 한다.
- hidden access는 reward tier `None`일 수 있지만 mandatory reward를 표현하지 않는다.
- return policy는 enum data만 보존한다. 실제 복귀 path/device 생성은 MAP06_07 책임이다.

## `OptionalRegionSnapshot` Contract

complete P04 optional model publication이다.

```text
IReadOnlyList<OptionalRegion> Regions
IReadOnlyList<OptionalRegionCell> Cells
IReadOnlyList<int> MandatoryRouteSectorIndices
int SourceMandatoryNodeCount
int SourceMandatoryDirectedEdgeCount
int SourceMandatoryRouteCellCount
string SourceMandatoryGraphDigest
bool IsEmpty
```

- Regions and Cells are copied read-only deterministic snapshots.
- duplicate RegionId, duplicate optional sector index를 거부한다.
- optional cell은 `MandatoryRouteSectorIndices`와 겹칠 수 없다.
- mandatory counts must match MAP05 known vector `47/96/47`.
- Source graph digest is caller-supplied canonical non-empty hex/string identity; this model does not compute graph digest.
- empty snapshot은 allowed only when Regions/Cells empty and mandatory baseline identity is still present.
- snapshot은 `GeneratedWorldData`, `SectorCell`, `MandatoryRouteGraph`, `MandatoryRouteValidationReport`를 mutate하지 않는다.

## Tests

`OptionalRegionModelsTests.cs` actual NUnit cases minimum `120`.

Required coverage:

- ID grammar/default/equality/order/deterministic hash/culture independence
- Access/reward/return token exact parse and reject invalid values
- depth `1..4` accepts and `0/5/default` rejects
- attachment cardinal neighbor and direction identity
- cell depth/attachment/return flag invariants
- region constructor copies input, sorts deterministically, rejects duplicate sector/ID mismatch
- snapshot rejects duplicate region/cell and mandatory overlap
- empty snapshot allowed only with source mandatory identity
- MAP05 known vector counts `47/96/47`, route graph digest identity preserved
- Type4 rule documented and not canonicalized in any optional model
- no Registry/RNG/clock/filesystem/Unity lifecycle access
- public runtime surface has no mutable static, UnityEditor, MAP06_02+ grower/mask/validator/overlay symbols
- culture `en-US`/`tr-TR`, caller order shuffled, repeated construction deterministic

No `[Ignore]`, `[Explicit]`, inconclusive/assumption skip, broad try/catch, sample reduction, or production fake.

## Required Runs

```text
OptionalRegionModelsTests          >=120 PASS
Existing MAP05 phase aggregate      1959/1959 PASS
Actually executed total             >=2079 PASS
failed/skipped                      0/0
```

Recommended focused existing regression:

```text
Map05ExitTests                         132/132 PASS
MandatoryRouteGraphBuilderTests        281/281 PASS
MandatoryRouteGraphValidatorTests      298/298 PASS
MandatoryRouteOverlayTests             142/142 PASS
MandatoryRouteOverlaySceneDrawerTests   26/26 PASS
```

Forced import/domain reload/compile/Console/relevant warning:

```text
0/0/0
```

Unity/Test Runner 접근 불가능으로 actual gate를 완료하지 못하면 `BLOCKED`.

## Asset / Scope Gate

```text
Assets meta 3247 -> 3254
new Runtime production C#/meta = 6/6
new Runtime EditMode test C#/meta = 1/1
exact Assets changes = 14
existing production/test modifications = 0
unexpected existing/folder meta = 0
Authoring CSV/meta = 50/50
duplicate GUID groups = 0
generated CSV files = 0
Scene/Prefab/asmdef/Packages/ProjectSettings = 0
```

The approved regenerated Diagnostics folder meta from MAP05_11 remains part of the baseline and must not be deleted/recreated by this Task.

## Result / Finalize

Result `<=170 lines`.

Required sections:

```text
TASK / STATUS / SUMMARY
PATCH APPLY / PRIOR GATE
CREATED / MODIFIED / PRESERVED
MODEL CONTRACT / TEST / UNITY / ASSET META / CHANGE SCOPE
DONE CONDITIONS / NEXT / Recommended Commit
```

PASS Result exact lines:

```text
STATUS: PASS
MAP06_01: COMPLETE ELIGIBLE
MAP06_02_ENUMERATE_OPTIONAL_ATTACHMENTS: LOCKED / DO NOT START
```

PASS일 때만 MAP06_01 COMPLETE, Current Task NONE으로 finalize한다. MAP06_02는 LOCKED로 유지하고 자동 생성/시작하지 않는다.

Recommended Commit:

```text
feat(map): add optional region model contracts
```
