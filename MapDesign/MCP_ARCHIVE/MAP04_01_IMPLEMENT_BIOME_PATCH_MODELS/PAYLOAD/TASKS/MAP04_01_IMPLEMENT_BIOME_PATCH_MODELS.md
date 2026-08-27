# MAP04_01 — Implement Biome Patch Models

```yaml
status_control:
  task_key: MAP04_01_IMPLEMENT_BIOME_PATCH_MODELS
  result_file: REPORTS/MAP04_01_IMPLEMENT_BIOME_PATCH_MODELS_RESULT.md
```

## TASK TYPE

```text
RUNTIME IMMUTABLE P02 BIOME-PATCH VALUE/AGGREGATE MODELS + EDITMODE TESTS
```

## Objective

MAP03에서 승인된 exact P01 `SiteReservationSnapshot` 위에 P02 Biome Patch가 사용할 compile-time typed immutable 데이터 계약을 만든다.

이번 Task는 `BiomePatchId`, Core/Satellite/Intrusion 역할, patch seed, exact 169-sector Primary/SecondaryBiome·Patch 소유권, Core site binding, patch aggregate, partial/complete `BiomePatchSnapshot`의 자기 일관성·불변성·결정적 순서만 구현한다.

`CoreBiomeSeed`를 실제 PatchId/footprint seed로 초기화하는 일, Core growth, Satellite seed 추첨, multi-seed 비용 성장, Intrusion 배치, cleanup, `PASS_BIOME`, generated CSV, validator, overlay는 구현하지 않는다.

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
12. `REPORTS/MAP03_11_MAP03_BATCH_AND_EXIT_TESTS_RESULT.md`

MAP03_11 Result에서 exact 아래를 확인한다.

```text
STATUS: PASS
MAP03 EXIT: APPROVED
MAP04 ENTRY: ELIGIBLE FOR SEPARATE PATCH
MAP04_01: LOCKED / DO NOT START
```

Result SHA-256은 exact 아래다.

```text
6e870129778f62ceb13037b3f4c9f53ee55d37403b92685828f07767ae30df11
```

MAP03_11의 사용자 지정 1/10 validation profile은 승인된 phase baseline이다. 이 Task에서 원래의 100,000/10,000/1,024 profile을 다시 실행하거나 그 수치가 실행됐다고 위조하지 않는다.

## Map Package Reference

Map Package v1.0 exact installed path가 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/01_WORLD_FIXED_SPEC.md
01_FIXED_SPEC/02_COORDINATE_AND_ID_RULES.md
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
01_FIXED_SPEC/04_RUNTIME_ARCHITECTURE.md
01_FIXED_SPEC/05_IMPLEMENTATION_ORDER.md
02_PHASE_ROADMAP/MAP04_BIOME_PATCH_GENERATOR.md
05_GENERATED_OUTPUT_SCHEMA/generated_biome_patches.csv
```

exact 문서가 installed tree에 없으면 이 Task의 frozen contracts를 authoritative fallback으로 사용한다. 대체 GDD, 과거 하네스, Legacy generator를 broad search하지 않는다.

## READ ALLOWLIST

### Existing Domain / Data APIs

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/BiomeDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/BiomeBoundaryDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/BiomeBoundaryDefinitionSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/StaticDataRegistry.cs
```

### Existing P00 / P01 Generation APIs

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedSectorRole.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldData.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGridIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorNeighborIndices.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GridInitializationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteFootprint.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreBiomeSeed.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorReservation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationPublication.cs
```

### Focused Tests / Assemblies

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/BiomeBoundaryDefinitionBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/StaticDataRegistryBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/GeneratedWorldDataTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/GridInitializationPassTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationModelsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map03ExitTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

위 C#/asmdef와 matching meta, 기존 Runtime/Test `Generation` 직계 파일명의 path-only inventory, Authoring CSV/meta count·hash, 전체 meta GUID, task marker 이후 change-scope path만 확인할 수 있다.

금지:

- Authoring CSV body 재해석 또는 수정
- MAP04_02 이후 Task body
- Legacy/Stage/P6/P11 generator body
- unrelated production/test C# body
- Scene/Prefab YAML
- MAP03 exit의 original large batch 재실행을 선행조건으로 추가

## WRITE ALLOWLIST

### 신규 Runtime production C# — exact 7

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchRole.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchSeed.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomeSectorOwnership.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchSiteBinding.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatch.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchSnapshot.cs
```

### 신규 Runtime EditMode test — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/BiomePatchModelsTests.cs
```

신규 C# 8개와 matching `.cs.meta` 8개, Result 1만 생성한다. 기존 production/tests/meta/asmdef/asmref는 수정하지 않는다. 기존 approved directory를 재사용하며 새 directory/folder meta를 만들지 않는다.

## Namespace / Assembly

```text
Runtime namespace: StarNight.Map.WorldGeneration.Generation
Test namespace:    StarNight.Map.Tests.WorldGeneration.Generation
Runtime assembly:  Game.Map.Runtime
Test assembly:     Game.Map.Tests.EditMode
```

`UnityEditor`, `UnityEngine.Object`, ScriptableObject/MonoBehaviour, serialization callback, reflection factory, service locator, singleton/static mutable state를 도입하지 않는다. Unity 6000.3.8f1의 현재 language level에서 compile되도록 record/record struct, `required`, `init`, nullable-reference directive에 의존하지 않는다.

## Frozen P02 Boundary

```text
Input artifact  = SITE_RESERVATIONS
Output artifact = BIOME_PATCHES
Pass ID         = PASS_BIOME
RNG stream      = RNG_BIOME_PATCH / BIOME_PATCH
Grid            = 13 x 13 / 169 sectors / index y*13+x / lower-left origin
Patch roles     = CORE | SATELLITE | INTRUSION
P02 owns        = PrimaryBiomeId + PatchId
Later boundary  = SecondaryBiomeId only
```

P02 output은 별도 immutable artifact다. 기존 `GeneratedWorldData`, `SectorCell`, `GridInitializationResult`, `SiteReservationSnapshot`, `SiteReservationPublication`을 mutate하지 않는다. model constructor는 Registry/RNG/clock/filesystem/Unity lifecycle을 읽지 않는다.

## `BiomePatchId` Contract

`BiomePatchId`는 `public readonly struct`, `IEquatable<BiomePatchId>`, `IComparable<BiomePatchId>`다.

```text
string Value
bool IsValid
BiomePatchId(string value)
bool TryCreate(string value, out BiomePatchId result)
```

- canonical grammar는 exact ASCII `^[A-Z0-9_]+$`이며 empty/whitespace/lowercase/hyphen/non-ASCII를 거부한다.
- ordinal case-sensitive equality/order와 runtime/process/culture에 독립적인 deterministic hash를 제공한다.
- `ToString()`은 valid instance에서 exact `Value`다.
- default struct는 `IsValid == false`; 다른 biome-patch model constructor는 default ID를 거부한다.
- ID 자동 생성, seed/order/biome 접두사 조립, random suffix는 이 Task에서 만들지 않는다. MAP04_02/04가 caller-supplied canonical PatchId를 결정한다.

## `BiomePatchRole` / Token Contract

`BiomePatchRole.cs`는 exact enum과 stateless `BiomePatchRoleTokenCodec`을 제공한다.

```text
BiomePatchRole: Core, Satellite, Intrusion
```

exact case-sensitive token mapping:

| Enum | Token |
|---|---|
| Core | `CORE` |
| Satellite | `SATELLITE` |
| Intrusion | `INTRUSION` |

`TryParse`와 `ToToken`을 제공한다. null/empty/space/case variation/numeric/undefined enum을 거부한다. `Enum.Parse`, `ToString().ToUpper*`, locale case-fold를 계약 구현으로 사용하지 않는다.

## `BiomePatchSeed` Contract

`BiomePatchSeed`는 patch 내부의 한 seed sector를 나타내는 sealed immutable object다.

```text
int SectorIndex
SectorCoord Sector
BiomePatchRole Role
SiteReservationId? SourceSiteReservationId
```

- `SectorIndex`와 `Sector`는 exact `WorldGridIndex` identity여야 한다.
- `Role == Core`이면 valid `SourceSiteReservationId`가 반드시 있다.
- `Satellite`와 `Intrusion`이면 `SourceSiteReservationId`는 반드시 없다.
- undefined role, invalid nullable ID, out-of-range/mismatched index-coordinate를 거부한다.
- seed는 PatchId/BiomeId/PatchRuleId를 중복 소유하지 않는다. enclosing `BiomePatch`가 그 identity를 소유한다.
- RNG draw, weight, cost, attempt ordinal, timestamp를 저장하지 않는다.

한 Core site footprint가 여러 sector면 동일 source reservation을 가진 Core seed 여러 개가 같은 patch에 들어갈 수 있다. 이 Task는 seed를 자동 생성하거나 footprint를 탐색하지 않는다.

## `BiomeSectorOwnership` Contract

exact 169-row P02 sector ownership의 한 행을 나타내는 sealed immutable object다.

```text
int SectorIndex
SectorCoord Sector
bool IsAssigned
string PrimaryBiomeId
string SecondaryBiomeId
BiomePatchId? PatchId
```

- index/coordinate는 exact `WorldGridIndex` identity다.
- `CreateUnassigned(index, sector)`는 `IsAssigned == false`, Primary/Secondary empty, PatchId null이다.
- assigned row는 canonical non-empty PrimaryBiomeId와 valid PatchId가 필수다.
- SecondaryBiomeId는 canonical ID 또는 empty이며, non-empty이면 PrimaryBiomeId와 ordinal-different여야 한다.
- unassigned row에 Primary/Secondary/Patch 일부만 존재하는 half-state를 금지한다.
- trim/case-fold/Unicode normalization, placeholder biome/patch ID 주입을 하지 않는다.

MAP04는 assigned row의 PrimaryBiomeId/PatchId만 소유한다. SecondaryBiomeId non-empty를 모델이 표현할 수는 있지만 MAP04 algorithm이 설정하지 않는다. boundary 단계만 SecondaryBiomeId를 추가할 수 있다.

## `BiomePatchSiteBinding` Contract

Core site footprint와 Core patch의 강제 소유 관계를 나타내는 sealed immutable object다.

```text
SiteReservationId SiteReservationId
BiomePatchId PatchId
string BiomeId
IReadOnlyList<int> OccupiedSectorIndices
```

- valid site/patch ID, canonical non-empty BiomeId, non-empty occupied sector set을 요구한다.
- sector index는 `0..168`, unique이며 ascending으로 copied read-only 저장한다.
- caller order와 collection mutation이 결과에 영향을 주지 않는다.
- special-map source ID, display name, footprint transform을 복제하지 않는다. source reservation identity가 P01 linkage다.
- 이 Task는 binding을 `SiteReservationSnapshot`에서 생성하지 않는다. MAP04_02가 typed input으로 만든다.

## `BiomePatch` Contract

한 patch의 identity와 현재 owned-sector 집합을 나타내는 sealed immutable aggregate다.

```text
BiomePatchId Id
string BiomeId
string PatchRuleId
BiomePatchRole Role
IReadOnlyList<BiomePatchSeed> Seeds
IReadOnlyList<int> SectorIndices
int SectorCount
bool ContainsSector(int sectorIndex)
```

- BiomeId/PatchRuleId는 canonical non-empty ID다.
- Seeds와 SectorIndices는 null/duplicate 없이 방어 복사한다.
- Seeds는 `SectorIndex`, SectorIndices는 int ascending canonical order다.
- 모든 seed role은 patch role과 같아야 하며 모든 seed sector는 SectorIndices에 포함되어야 한다.
- Core patch는 seed 1개 이상, Satellite/Intrusion patch도 이 모델 시점에는 seed 1개 이상을 요구한다.
- Core patch의 각 seed는 source site가 있고, Satellite/Intrusion seed는 source site가 없어야 한다.
- 서로 다른 source site가 같은 Core patch에 seed를 제공하는 것은 모델 수준에서 표현 가능하다. MAP04_02/09가 실제 정책을 결정한다.
- cardinal connectivity, min/max size, world share, seed distance, edge touch, perimeter, compactness, branchiness는 이 Task에서 계산·강제하지 않는다.

## `BiomePatchSnapshot` Contract

P02의 immutable aggregate snapshot이다.

```text
ulong Seed
IReadOnlyList<BiomePatch> Patches
IReadOnlyList<BiomeSectorOwnership> Sectors
IReadOnlyList<BiomePatchSiteBinding> SiteBindings
int AssignedSectorCount
int UnassignedSectorCount
bool IsComplete
BiomeSectorOwnership GetSector(int index)
bool TryGetSector(SectorCoord sector, out BiomeSectorOwnership ownership)
bool TryGetPatch(BiomePatchId id, out BiomePatch patch)
bool TryGetSiteBinding(SiteReservationId id, out BiomePatchSiteBinding binding)
```

Snapshot은 exact 169 non-null sector rows를 요구한다.

- index set은 exact `0..168`, coordinate set은 exact 13×13이며 각 row의 index-coordinate는 `WorldGridIndex`와 일치한다.
- Patches는 PatchId ordinal, SiteBindings는 SiteReservationId ordinal, Sectors는 index ascending canonical order다.
- patch ID와 site binding ID는 각각 unique다.
- 각 patch SectorIndices와 assigned sector ownership은 exact 양방향 일치한다.
- assigned ownership의 PrimaryBiomeId/PatchId는 enclosing patch와 일치해야 한다.
- orphan ownership, orphan patch cell, patch overlap, wrong biome, duplicate ownership을 거부한다.
- patch seed는 같은 patch의 owned cell이어야 한다.
- 각 SiteBinding의 patch는 존재하고 `Role == Core`, BiomeId가 같아야 한다.
- binding의 모든 occupied sector는 patch cell이자 동일 PrimaryBiome assigned ownership이어야 한다.
- binding의 각 occupied sector에는 동일 `SourceSiteReservationId`를 가진 Core seed가 exact 1개 있어야 한다.
- Core seed에 존재하는 source site는 exact 하나의 binding으로 역참조되어야 한다. binding 없는 orphan Core seed를 거부한다.
- `IsComplete == (UnassignedSectorCount == 0)`이며 partial snapshot은 허용한다. partial은 unassigned row만 허용하며 half-state를 허용하지 않는다.
- SecondaryBiomeId는 ownership 보조값일 뿐 patch membership/identity를 바꾸지 않는다.

Snapshot은 caller collection order와 이후 mutation, current culture, dictionary enumeration order와 무관해야 한다. public setter/mutable field, mutable collection downcast, lazy caller enumeration, public static current snapshot/cache를 만들지 않는다.

## Structural Validation Boundary

이번 Task가 강제하는 것은 typed model 자기 일관성뿐이다.

강제:

- ID/token validity
- exact index-coordinate identity
- defensive copy와 deterministic ordering
- patch↔sector 양방향 membership
- seed⊂patch
- Core site binding↔Core seed↔owned sector consistency
- partial/complete count consistency

금지:

- 필수 biome 4종 또는 CorePatch 4개 정책 검사
- 2~59 sector size 검사
- 일반 1-cell patch 금지 / Intrusion 1-cell 허용 정책
- patch cardinal connectivity/perimeter/compactness 계산
- site kind/source map/required biome를 Registry/P01에서 자동 resolve
- `CoreBiomeSeed`/footprint를 읽어 seed/binding 자동 생성
- ownership winner, growth cost, tie-break, priority queue
- RNG stream 생성·소비
- retry/failure classification
- `GeneratedWorldData`/`SectorCell` mutation 또는 serializer/file I/O

## Required Tests

`BiomePatchModelsTests.cs`에서 actual discovered cases 최소 `72`개를 만든다.

최소 검증 범위:

1. BiomePatchId default/valid/invalid ASCII grammar, equality/order/hash/culture
2. Core/Satellite/Intrusion exact token parse/format, undefined rejection
3. seed index-coordinate identity와 Core source presence / non-Core source absence
4. ownership unassigned/assigned/secondary/half-state 규칙
5. site binding canonical order, duplicate/out-of-range rejection, defensive copy
6. patch seed/cell canonical order, duplicate/missing seed cell/role mismatch rejection
7. exact 169 rows, missing/extra/duplicate index/coordinate rejection
8. patch↔ownership orphan/overlap/wrong-biome/wrong-patch rejection
9. binding↔Core patch↔Core seed exact cross-consistency와 orphan rejection
10. empty all-unassigned partial snapshot과 fully-assigned complete snapshot
11. index/coordinate/patch/site lookup success/failure
12. reversed/shuffled input, `en-US`/`tr-TR`, caller mutation 후 동일 snapshot
13. public setter/mutable field/static mutable state/Unity API/RNG/time/filesystem dependency 0

테스트 fixture는 exact 13×13 ownership을 in-memory typed objects로 만든다. Authoring CSV body나 Registry singleton/file을 읽지 않는다.

## Verification Gates

```text
New focused BiomePatchModelsTests: >=72 PASS
MAP03_01 SiteReservationModels regression: 81/81 PASS
MAP03_09 SiteReservationValidator regression: 268/268 PASS
MAP01 BiomeBoundary definitions regression: 38/38 PASS
MAP01 StaticDataRegistry regression: 53/53 PASS
MAP02 GeneratedWorldData regression: 56/56 PASS
Game.Map targeted EditMode: >=3921 PASS
Full project EditMode: >=3989 PASS
Failed / skipped in required runs: 0 / 0
Compile errors / relevant new warnings / Console errors: 0 / 0 / 0
```

MAP03_11의 원래 대형 profile을 반복할 필요는 없다. assembly/full run이 reduced 1,000-seed exit case를 포함하면 실제 실행 결과를 그대로 기록하고, timeout이나 누락을 PASS로 바꾸지 않는다.

## Asset / Change Gates

```text
Baseline Assets meta: 3071
New Runtime C#: 7
New test C#: 1
New matching meta: 8
Final Assets meta: 3079
Exact Assets changes after .APPLIED: 16
Existing Assets modifications: 0
Unexpected Assets changes: 0
Authoring CSV/meta: 50/50 unchanged
Accepted legacy Editor folder meta: 6/6 unchanged
Duplicate GUID groups: 0
```

각 신규 `.cs.meta`는 `fileFormatVersion: 2`, valid unique non-zero lowercase 32-hex GUID, `MonoImporter`여야 한다. Unity가 unrelated folder meta를 만들면 baseline 여부를 확인하고 이 Task가 만든 unexpected file은 제거한다. 기존 legacy Editor folder meta 6개는 삭제·재생성하지 않는다.

## Result Contract

`REPORTS/MAP04_01_IMPLEMENT_BIOME_PATCH_MODELS_RESULT.md` 첫 부분에 exact 아래를 기록한다.

```text
# MAP04_01 — Implement Biome Patch Models Result

STATUS: PASS
```

Result는 최소 아래를 포함한다.

- PATCH APPLY와 `.APPLIED`
- READ/WRITE allowlist 준수
- CREATED/MODIFIED/PREEXISTING_IDENTICAL
- 7 runtime + 1 test exact path와 matching meta/GUID
- model별 frozen contract evidence
- partial/complete snapshot cross-consistency evidence
- actual focused/targeted/full counts와 job ID
- compile/Console/meta/GUID/Authoring/change-scope evidence
- OUT_OF_SCOPE_FINDINGS
- NEXT: MAP04_01만 COMPLETE/Current Task NONE, MAP04_02 LOCKED

어느 gate든 실패하거나 필요한 기존 파일이 payload/status와 불일치하면 `STATUS: BLOCKED` 또는 `STATUS: FAIL`을 정확히 기록하고 Assets를 부분 성공으로 finalize하지 않는다.

## STATUS FINALIZE

전부 PASS일 때만:

```text
MAP04_01_IMPLEMENT_BIOME_PATCH_MODELS: COMPLETE
Current Task: NONE
Last Completed Task: MAP04_01_IMPLEMENT_BIOME_PATCH_MODELS
Last Result: REPORTS/MAP04_01_IMPLEMENT_BIOME_PATCH_MODELS_RESULT.md / STATUS: PASS
MAP04_02_INITIALIZE_CORE_PATCH_SEEDS: LOCKED
```

MAP04_02를 자동 시작하거나 Task 파일을 만들지 않는다.

## DO NOT

- 기존 C#/meta/asmdef/asmref/CSV/Scene/Prefab 수정 금지
- MAP04_02의 Core seed initializer 또는 PatchId 생성 규칙 구현 금지
- MAP04_03~07 growth/Satellite/Intrusion/cleanup 구현 금지
- MAP04_08 generated CSV serializer/export/file I/O 금지
- MAP04_09 validator와 2~59/Core count/world-share 정책 구현 금지
- MAP04_10 overlay/EditorWindow/Gizmo/menu 금지
- `PASS_BIOME` adapter/root/artifact transaction/retry 구현 금지
- Legacy/Stage/P6/P11 generator 재사용 금지
- Git commit/push 금지

## Recommended Commit

```text
feat(map): add immutable biome patch models
```
