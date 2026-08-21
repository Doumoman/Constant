TASK: MAP06_05_ASSIGN_ACCESS_RULES_AND_CLUES
STATUS: PASS
MAP06_05: COMPLETE ELIGIBLE
MAP06_06_CALCULATE_OPTIONAL_REWARD_TIER: LOCKED / DO NOT START

# SUMMARY

- MAP06_04의 immutable Type0 assignment를 입력으로 사용해 approved optional region 12개 각각에 exact one access assignment와 mandatory side에서 perceptible한 clue를 배정했다.
- access rule은 `Basic/Tool/Environment/Explosive/Hidden = 3/3/2/2/2`이고 requirement, traversal, clue, depth cost가 frozen matrix와 일치한다.
- attachment identity와 base-closed boundary를 그대로 보존하며 reward tier, return policy, inactive buffer, validator/overlay, generated CSV는 구현하지 않았다.
- 결과는 canonical order의 immutable atomic publication이고 RNG draw와 source mutation은 모두 0이다.

# PATCH APPLY

- PATCH_ID: `MAP06_05_ASSIGN_ACCESS_RULES_AND_CLUES`
- PATCH_VERSION: `1.0`
- `.APPLIED` receipt: PRESENT
- Manifest SHA-256: `c7679ce926a9fb937af3e2ec0e4e686a97e4362e71b6f5d491496f1ecb10d5b6`
- applied Master SHA-256: `3bcb82b47e7048af09fd1138399ea2ffbbb078e9b1e9780daf845695f7095cb6`
- applied Status SHA-256: `f74dd11b830208401c67f2f471eb0aa431bdc46c4ac5c509aeb93f03898eec37`
- current Task SHA-256: `d80cf04261811777b65b6c99ca8b7ae368fc39f4a895d024c6639ada5226c587`

# PRIOR RESULT / TASK GATE

- prior TASK: `MAP06_04_ASSIGN_TYPE0_ROUTE_MASKS`
- prior exact STATUS: `PASS`
- prior Result SHA-256: `7cfb055bb6cb1df24206b25a1a5f046936c7fbdf58bd4b307d476ead4f28ed7a`
- prior Task SHA-256: `320870304bc61d7414a10473978ae11472adefd88c6f8cd76bb6f909ac136cea`
- pre-execution status: MAP06_05 sole `CURRENT`; MAP06_06+ `LOCKED`

# CREATED

Runtime production C# exact 8:

- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessClueId.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessAssignmentEnums.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessClue.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessAssignmentSettings.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessAssignment.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessAssignmentDiagnostics.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessAssignmentResult.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessRuleAssigner.cs`

Runtime EditMode test exact 1:

- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalAccessRuleAssignerTests.cs`

Unity-generated matching `.cs.meta`: `9`

# CHANGED

Phase-boundary test C# exact 9, allowed maximum 10 이내:

- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/HorizontalBackboneRouterTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteGraphValidatorTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map05ExitTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/UpDownConflictResolverTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/VerticalGatewayPlannerTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalAttachmentEnumeratorTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionGrowerTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Type0RouteMaskAssignerTests.cs`

Boundary tests now allow the exact nine MAP06_05 production/test symbols while continuing to forbid MAP06_06+ examples. `OptionalRegionModelsTests.cs` and all matching existing `.cs.meta` remain unchanged.

# IMPLEMENTATION CONTRACT

- `OptionalAccessClueId`: exact grammar `^CLUE_OPT_REGION_[0-9]{4}_[A-Z0-9_]+$`, default invalid, ordinal case-sensitive equality/order, deterministic hash.
- `OptionalAccessAssignmentEnums`: exact requirement/clue/traversal enums and ordinal token codecs; null/empty/space/case/numeric/undefined values are rejected without locale folding or `Enum.Parse`.
- settings: caller lists are copied and validated; no mutable/default static settings instance exists.
- assigner: stateless sealed service, source region canonical sort, exact rule/tool/hidden cycles, depth-table costs, clue ID construction, validate-all-before-publish atomic behavior.
- result/errors: immutable copied/sorted/deduplicated collections, stable error order, lowercase SHA-256 canonical digest.
- forbidden runtime dependencies: reflection/filesystem/Registry/RNG/UnityEditor/lifecycle/static mutable cache = `0`.

# APPROVED SETTINGS

- access pattern: `Basic / Tool / Environment / Explosive / Hidden`
- tool requirement pattern: `Pickaxe / Shovel / Rope`
- hidden clue pattern: `HiddenCrack / HiddenLight / HiddenSound`
- tool cost tier by depth 1..4: `1 / 2 / 3 / 4`
- explosive fuel cost by depth 1..4: `10 / 20 / 30 / 40`
- hidden clue difficulty by depth 1..4: `1 / 2 / 3 / 4`
- caller list mutation, culture, enumeration order, service reuse, thread/time do not change the copied settings or canonical result.

# APPROVED FIXTURE EVIDENCE

Source/accounting:

- optional regions/cells/Type0 assignments: `12 / 39 / 39`
- source Type0 assignment digest: `a26e73f25ff7267760a2507ec55b1acda3a6c39a8f3698fc37b190620ceca525`
- source growth digest: `1f00f718bdb8d79fbf88923be0d81e0075987267c024cc624086ee6194998caa`
- assignment/clue/perceptible clue: `12 / 12 / 12`
- attachment boundary base-closed: `12`
- mandatory boundary base-open: `0`
- RNG draw/source mutation/partial publication: `0 / 0 / 0`

Distribution:

- Basic/Tool/Environment/Explosive/Hidden: `3 / 3 / 2 / 2 / 2`
- Pickaxe/Shovel/Rope: `1 / 1 / 1`
- HiddenCrack/HiddenLight/HiddenSound: `1 / 1 / 0`
- reward preview reservations: `2`; both Explosive assignments only

Canonical digest:

- `5268b7ed2342f197fb5717c85dbfdb8e51b3c520c217ecf93ad14dc944ecf74f`

# ALL ASSIGNMENTS / COST INPUTS

1. `OPT_REGION_0000`: ordinal `0`, attachment `7`, mandatory `29`, entry `42`, dir `0,1`, `BASIC/NONE/OPTIONAL_BREAK`, clue `CLUE_OPT_REGION_0000_BASIC/BASIC_OPENING`, tool/fuel/hidden `0/0/0`, preview `0`.
2. `OPT_REGION_0001`: ordinal `1`, attachment `8`, mandatory `53`, entry `52`, dir `-1,0`, `TOOL/PICKAXE/OPTIONAL_BREAK`, clue `CLUE_OPT_REGION_0001_TOOL/TOOL_SURFACE`, tool/fuel/hidden `1/0/0`, preview `0`.
3. `OPT_REGION_0002`: ordinal `2`, attachment `12`, mandatory `66`, entry `65`, dir `-1,0`, `ENVIRONMENT/ENVIRONMENT/OPTIONAL_BREAK`, clue `CLUE_OPT_REGION_0002_ENVIRONMENT/ENVIRONMENT_DEVICE`, tool/fuel/hidden `0/0/0`, preview `0`.
4. `OPT_REGION_0003`: ordinal `3`, attachment `14`, mandatory `7`, entry `8`, dir `1,0`, `EXPLOSIVE/EXPLOSIVE/OPTIONAL_BREAK`, clue `CLUE_OPT_REGION_0003_EXPLOSIVE/EXPLOSIVE_REWARD_PREVIEW`, tool/fuel/hidden `0/30/0`, preview `1`.
5. `OPT_REGION_0004`: ordinal `4`, attachment `15`, mandatory `31`, entry `44`, dir `0,1`, `HIDDEN/NONE/HIDDEN`, clue `CLUE_OPT_REGION_0004_HIDDEN/HIDDEN_CRACK`, tool/fuel/hidden `0/0/4`, preview `0`.
6. `OPT_REGION_0005`: ordinal `5`, attachment `16`, mandatory `79`, entry `78`, dir `-1,0`, `BASIC/NONE/OPTIONAL_BREAK`, clue `CLUE_OPT_REGION_0005_BASIC/BASIC_OPENING`, tool/fuel/hidden `0/0/0`, preview `0`.
7. `OPT_REGION_0006`: ordinal `6`, attachment `23`, mandatory `46`, entry `47`, dir `1,0`, `TOOL/SHOVEL/OPTIONAL_BREAK`, clue `CLUE_OPT_REGION_0006_TOOL/TOOL_SURFACE`, tool/fuel/hidden `4/0/0`, preview `0`.
8. `OPT_REGION_0007`: ordinal `7`, attachment `24`, mandatory `106`, entry `119`, dir `0,1`, `ENVIRONMENT/ENVIRONMENT/OPTIONAL_BREAK`, clue `CLUE_OPT_REGION_0007_ENVIRONMENT/ENVIRONMENT_DEVICE`, tool/fuel/hidden `0/0/0`, preview `0`.
9. `OPT_REGION_0008`: ordinal `8`, attachment `27`, mandatory `107`, entry `120`, dir `0,1`, `EXPLOSIVE/EXPLOSIVE/OPTIONAL_BREAK`, clue `CLUE_OPT_REGION_0008_EXPLOSIVE/EXPLOSIVE_REWARD_PREVIEW`, tool/fuel/hidden `0/40/0`, preview `1`.
10. `OPT_REGION_0009`: ordinal `9`, attachment `28`, mandatory `107`, entry `94`, dir `0,-1`, `HIDDEN/NONE/HIDDEN`, clue `CLUE_OPT_REGION_0009_HIDDEN/HIDDEN_LIGHT`, tool/fuel/hidden `0/0/1`, preview `0`.
11. `OPT_REGION_0010`: ordinal `10`, attachment `30`, mandatory `72`, entry `73`, dir `1,0`, `BASIC/NONE/OPTIONAL_BREAK`, clue `CLUE_OPT_REGION_0010_BASIC/BASIC_OPENING`, tool/fuel/hidden `0/0/0`, preview `0`.
12. `OPT_REGION_0011`: ordinal `11`, attachment `31`, mandatory `108`, entry `121`, dir `0,1`, `TOOL/ROPE/OPTIONAL_BREAK`, clue `CLUE_OPT_REGION_0011_TOOL/TOOL_SURFACE`, tool/fuel/hidden `4/0/0`, preview `0`.

# ATOMIC FAILURE / IMMUTABILITY

- null source, invalid source status/digest/accounting, invalid settings, non-contiguous/duplicate region identity, invalid direction, open attachment base side, mismatched matrix, duplicate clue ID, and non-perceptible clue paths are rejected atomically.
- every failure publishes assignments/clues `0/0`, canonical digest empty, RNG/mutation `0/0`, partial publication `0`.
- caller order/culture/service reuse determinism and source graph/region/Type0 identity preservation are covered by actual EditMode cases.

# MAP05 / MAP06 PRESERVATION

- MAP05 required aggregate remained `1959/1959` PASS; Type4 mask distribution and phase-boundary assertions were not weakened.
- MAP06 prior combined selection remained `630/630` PASS.
- Type0 assignment gate remained `257/257` PASS with source digest, L+R handling, attachment base-closed `12`, mandatory base-open `0` intact.
- MAP05/MAP06_01~04 production, mandatory graph/mask, OptionalRegion models, Type0 assignment production, runtime/test asmdef, Authoring/generated CSV/meta, Scene, Prefab, Packages, ProjectSettings modifications: `0`.

# ACTUAL UNITY EDITMODE TEST JOBS

| Required job | Job ID | Passed | Failed | Skipped |
|---|---|---:|---:|---:|
| `OptionalAccessRuleAssignerTests` | `955ff9c6256e4e6daeb47e0d445be25d` | 289 | 0 | 0 |
| `Type0RouteMaskAssignerTests` | `ad1bfa3468234746891d5d87f24d6224` | 257 | 0 | 0 |
| MAP06 prior combined selection | `a8ac02a5cea94fad9f14fec08fc54f68` | 630 | 0 | 0 |
| MAP05 category selection | `e9244a08570841a191be4e9e9c314e16` | 1832 | 0 | 0 |
| `MandatoryRouteMaskLookupBuilderTests` MAP05 remainder | `9e5bfe683a3c4aa89f2d18381ef29152` | 127 | 0 | 0 |
| **Actually executed required total** | — | **3135** | **0** | **0** |

- MAP05 aggregate: `1832 + 127 = 1959/1959`.
- required minimum `3096` exceeded by actual `3135/3135` PASS.
- new-test category actual cases: `38 / 38 / 44 / 30 / 34 / 28 / 28 / 24 / 24`, each specified category minimum satisfied; one canonical summary case completes `289`.
- supplemental canonical evidence rerun: job `768b5d8bb64344da811722c2adce08bf`, `1/1` PASS; excluded from the required-total arithmetic above.

# UNITY GATE

- Unity instance: `Constant@ced6e0dfc4a31d45`
- Unity version: `6000.3.8f1`
- forced refresh/import/domain reload: COMPLETE
- compile errors: `0`
- final Console errors/warnings after clearing stale Test Framework/MCP transport messages: `0 / 0`
- relevant code warnings: `0`
- compile/Console/relevant warnings gate: `0 / 0 / 0`

# ASSET / META / CSV / GUID / CHANGE-SCOPE GATE

- Assets meta: `3274 -> 3283`
- new C#/matching meta: `9 / 9`
- duplicate GUID groups: `0`
- existing boundary test C# modified: `9 <= 10`; their existing `.cs.meta` modified `0`
- protected existing production/runtime asmdef hashes unchanged from pre-task snapshot: `19/19`
- test asmdef and unmodified `OptionalRegionModelsTests.cs` hashes unchanged: `2/2`
- Authoring CSV/matching meta: `50 / 50`
- approved Authoring manifest SHA-256 unchanged: `4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3`
- independent pre/post Authoring path+file-hash snapshot SHA-256 unchanged: `58af89c8af9631f9138d4a503925d0886fde99f6f83c0fb1d85a392d76f22b7d`
- generated CSV created by this task: `0`
- new directory/folder meta/asmdef/asmref: `0`
- Task WRITE ALLOWLIST outside modifications: `0`

# DONE CONDITIONS

- Preconditions, receipt, prior Result/Task SHA gates: PASS.
- Every approved optional region has exact one matrix-valid assignment and perceptible clue: PASS.
- Tool/hidden cycles, depth costs, explosive preview reservation: PASS.
- Attachment identity/base-closed preservation and source/canonical digests: PASS.
- Atomic failure, immutable publication, RNG/mutation zero: PASS.
- MAP06_05 symbols allowed and MAP06_06+ symbols forbidden: PASS.
- Required actual Unity tests, Unity gate, asset/static/change-scope gates: PASS.

# NEXT

Finalize only `MAP06_05_ASSIGN_ACCESS_RULES_AND_CLUES` as COMPLETE and set Current Task to `NONE`. Keep `MAP06_06_CALCULATE_OPTIONAL_REWARD_TIER` LOCKED. Do not read or start any MAP06_06+ Task body without a separate valid patch.
