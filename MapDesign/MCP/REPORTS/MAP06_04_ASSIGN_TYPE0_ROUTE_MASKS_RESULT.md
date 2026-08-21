TASK: MAP06_04_ASSIGN_TYPE0_ROUTE_MASKS
STATUS: PASS
MAP06_04: COMPLETE ELIGIBLE
MAP06_05_ASSIGN_ACCESS_RULES_AND_CLUES: LOCKED / DO NOT START

# SUMMARY

- MAP06_03 optional-region topology의 모든 39개 cell에 MAP01 typed `SectorRouteMaskDefinition`에서 검증한 exact registered Type0 mask를 배정했다.
- base open side는 same-region cardinal adjacency만 반영하며 attachment→mandatory와 cross-region boundary는 닫힌 상태로 유지한다.
- assignment는 RegionId/SectorIndex canonical order의 immutable publication이며 unsupported topology는 partial assignment 없이 원자적으로 거부한다.
- access/clue/reward/return/inactive/validator/overlay/generated CSV 동작은 구현하지 않았다.

# PATCH APPLY

- PATCH_ID: `MAP06_04_ASSIGN_TYPE0_ROUTE_MASKS`
- PATCH_VERSION: `1.0`
- `.APPLIED` receipt: PRESENT
- Manifest SHA-256: `5fa69b275c261b9c8db9ace7e5a6f3b4b61dbc462ebe73461474c29a0ec7f6db`
- applied Master SHA-256: `200655ae0bd0986d4eab3d413891bc38eb45c72ccf36cce6d8ccfb7a8f2a674e`
- applied Status SHA-256: `e9e6ce46db36757ef28b59cd37c1946e5c2ed8adc8fbbd9df57a2e9b65197722`
- current Task SHA-256: `320870304bc61d7414a10473978ae11472adefd88c6f8cd76bb6f909ac136cea`

# PRIOR RESULT GATE

- TASK: `MAP06_03_IMPLEMENT_OPTIONAL_REGION_GROWER`
- exact STATUS: `PASS`
- SHA-256: `370a15f504d46492a591d064ee70dbc35d27b5b55ab4b621617aedae95d489b0`
- source growth regions/cells: `12 / 39`
- source growth digest: `1f00f718bdb8d79fbf88923be0d81e0075987267c024cc624086ee6194998caa`
- source attachment digest: `68b438c523645c2f6721fa0c104c3cd4c282076292cd2e035cd20a2b272aaee6`

# CREATED

- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteOpenMask.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskId.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskRecord.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskAssignment.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskAssignmentDiagnostics.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskAssignmentResult.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskAssigner.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Type0RouteMaskAssignerTests.cs`
- matching Unity-generated `.cs.meta`: `8`

# CHANGED

- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/HorizontalBackboneRouterTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteGraphValidatorTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map05ExitTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/UpDownConflictResolverTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/VerticalGatewayPlannerTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionModelsTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalAttachmentEnumeratorTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionGrowerTests.cs`
- boundary negative assertions now allow MAP06_04 symbols while preserving MAP06_05+ forbidden-symbol case counts.

# EXACT REGISTERED TYPE0 CATALOG

| Canonical order | ID | L | R | U | D | route_type | mandatory_allowed | active |
|---:|---|---:|---:|---:|---:|---:|---:|---:|
| 0 | `ROUTE_T0_NONE` | 0 | 0 | 0 | 0 | 0 | 0 | 1 |
| 1 | `ROUTE_T0_L` | 1 | 0 | 0 | 0 | 0 | 0 | 1 |
| 2 | `ROUTE_T0_R` | 0 | 1 | 0 | 0 | 0 | 0 | 1 |
| 3 | `ROUTE_T0_U` | 0 | 0 | 1 | 0 | 0 | 0 | 1 |
| 4 | `ROUTE_T0_D` | 0 | 0 | 0 | 1 | 0 | 0 | 1 |
| 5 | `ROUTE_T0_LU` | 1 | 0 | 1 | 0 | 0 | 0 | 1 |
| 6 | `ROUTE_T0_LD` | 1 | 0 | 0 | 1 | 0 | 0 | 1 |
| 7 | `ROUTE_T0_RU` | 0 | 1 | 1 | 0 | 0 | 0 | 1 |
| 8 | `ROUTE_T0_RD` | 0 | 1 | 0 | 1 | 0 | 0 | 1 |
| 9 | `ROUTE_T0_UD` | 0 | 0 | 1 | 1 | 0 | 0 | 1 |
| 10 | `ROUTE_T0_LUD` | 1 | 0 | 1 | 1 | 0 | 0 | 1 |
| 11 | `ROUTE_T0_RUD` | 0 | 1 | 1 | 1 | 0 | 0 | 1 |

- registered Type0 masks: `12`
- ignored non-Type0 typed definitions in approved test input: `3`
- Type0 source-definition exact reference preservation: `12/12`
- duplicate/missing/inactive/unexpected/wrong-type/wrong-shape/mandatory-allowed/L+R catalog variants: atomic rejection PASS
- source route-mask catalog digest: `a96d0c6860ea0ebf62ac9763efcb7a03fa61df932fde85b30cec76c4b0c50506`

# APPROVED ASSIGNMENT OUTPUT

- source regions/cells: `12 / 39`
- assignments: `39`
- internal undirected reciprocal BaseEdges: `30`
- attachment boundaries base-closed: `12`
- mandatory boundary base-open: `0`
- closed cross-region undirected adjacencies: `13`
- horizontal-through / unsupported / RNG / source mutation: `0 / 0 / 0 / 0`
- canonical assignment digest: `a26e73f25ff7267760a2507ec55b1acda3a6c39a8f3698fc37b190620ceca525`

Per-mask usage:

| ID | assignments |
|---|---:|
| `ROUTE_T0_NONE` | 5 |
| `ROUTE_T0_L` | 2 |
| `ROUTE_T0_R` | 3 |
| `ROUTE_T0_U` | 4 |
| `ROUTE_T0_D` | 6 |
| `ROUTE_T0_LU` | 4 |
| `ROUTE_T0_LD` | 2 |
| `ROUTE_T0_RU` | 2 |
| `ROUTE_T0_RD` | 2 |
| `ROUTE_T0_UD` | 2 |
| `ROUTE_T0_LUD` | 3 |
| `ROUTE_T0_RUD` | 4 |

# ATOMIC FAILURE EVIDENCE

- synthetic same-region L+R through topology: `UnsupportedTopology`
- horizontal-through cells / unsupported required masks: `1 / 1`
- failure assignments / canonical output digest / RNG / mutation: `0 / empty / 0 / 0`
- invalid input and invalid catalog results likewise publish no partial assignments.

# MANDATORY BASELINE

- graph nodes/directed/undirected/route cells: `47 / 96 / 48 / 47`
- masks T1/T2/T3/T4_UD/T4_LUD/T4_RUD/T4_LRUD: `20 / 4 / 4 / 17 / 0 / 0 / 2`
- Type4 U+D mandatory: preserved
- Type4 L/R independent: preserved
- legal Type4 combinations UD/LUD/RUD/LRUD: preserved
- MAP05 production graph/mask source modifications: `0`

# TEST

- `Type0RouteMaskAssignerTests`: `257/257 PASS`
  - Job: `34992603a0b3432894e5e929ef916ac3`
  - failed/skipped: `0/0`
- MAP06 required existing combined selection: `630/630 PASS`
  - `OptionalRegionGrowerTests`: `234`
  - `OptionalAttachmentEnumeratorTests`: `202`
  - `OptionalRegionModelsTests`: `194`
  - Job: `62b1dcd3fc4a4937bfa84228ecfb9776`
  - failed/skipped: `0/0`
- Existing MAP05 aggregate: `1959/1959 PASS`
  - MAP05 category aggregate: `1832/1832`, Job `75e6eebe6912434cb0fc642ff14fbe49`
  - uncategorized `MandatoryRouteMaskLookupBuilderTests`: `127/127`, Job `e40958d30081461f9dd6086d9ab4946c`
  - failed/skipped: `0/0`
- Actually executed required total: `2846/2846 PASS`
- failed/skipped: `0/0`
- approved summary evidence: `1/1 PASS`, Job `b5a513306a904c1196144b1534335be4`
- one earlier MAP05 discovery attempt timed out before test execution; the successful jobs above are the counted actual gates.

# UNITY

- Unity: `6000.3.8f1`
- instance: `Constant@ced6e0dfc4a31d45`
- forced refresh/import/domain reload: COMPLETE
- final editor phase: idle
- ready_for_tools: true
- tests running: false
- Compile Errors: `0`
- final Console Errors after clear/check: `0`
- relevant Warnings after final clear/check: `0`
- Scene/Prefab changes by this Task: NONE

# ASSET / STATIC GATES

- Assets meta: `3266 -> 3274`
- new C#/matching meta: `8/8`
- existing boundary test C# modified: `9`
- Authoring CSV/meta: `50/50`
- Authoring manifest SHA-256: `4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3`
- duplicate GUID groups: `0`
- generated CSV paths: `0`
- MAP05/MAP06_01~03 production source modifications by this Task: `0`
- graph/mask/OptionalRegionCell/SectorCell/CSV/asmdef/Scene/Prefab/Packages/ProjectSettings modifications by this Task: `0`
- Status modification during Task execution: `0`

# DONE CONDITIONS

- [PASS] Preconditions, prior Result SHA, current Task SHA, sole CURRENT gate verified.
- [PASS] Exact 12 active registered Type0 typed rows validated without Authoring CSV modification.
- [PASS] Every source optional cell has exactly one registered assignment.
- [PASS] Internal BaseEdges reciprocal; attachment/mandatory/cross-region base boundaries closed; every assignment `!(L&&R)`.
- [PASS] Unsupported topology and invalid catalogs are atomic with no partial publication.
- [PASS] MAP06_04 symbols allowed; MAP06_05+ symbols remain forbidden.
- [PASS] No later access/clue/reward/return/inactive/validator/overlay/generated CSV behavior.
- [PASS] Mandatory graph/masks and Type4 rules unchanged.
- [PASS] Required Unity EditMode gates actually executed: `2846/2846`.
- [PASS] Compile/Console/relevant warning gate: `0/0/0`.
- [PASS] Asset/meta/CSV/GUID/change-scope gate.

# NEXT

- Finalize MAP06_04 only.
- Change MAP06_04 CURRENT -> COMPLETE and Current Task -> NONE.
- Keep MAP06_05_ASSIGN_ACCESS_RULES_AND_CLUES LOCKED / DO NOT START.
- Do not auto-start the next task.
