# MAP04_09 — Implement Biome Patch Validator

```yaml
status_control:
  task_key: MAP04_09_IMPLEMENT_BIOME_PATCH_VALIDATOR
  result_file: REPORTS/MAP04_09_IMPLEMENT_BIOME_PATCH_VALIDATOR_RESULT.md
```

## Goal

MAP04_08 `Completed` export와 typed biome/patch/boundary definitions를 deterministic 15-rule validator로 검사한다. 성공 시 approved export를 immutable publication으로 감싼다. 실패 시 source를 수정하거나 local repair/RNG redraw하지 않고 `ValidationRejected`를 반환한다.

validator만 구현한다. overlay, batch, root/retry adapter, CSV/file write는 범위 밖이다.

## Prior Gate / Read

control → Master/Status → 이 Task → MAP04_08 Result.

```text
Prior SHA-256 a65c8dd370d6b5bc315b1c0d901c7838045f7fc08f8acf596d585388fed0c206
STATUS PASS; actual 431/431; rows 17/169; assigned/unassigned 165/4
patch/world bytes 1956/16380; filesystem/RNG/mutation 0
Assets meta 3132; existing/unexpected/compile conflict 0
```

Read body allowlist:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/BiomeDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldData.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldDataCsvSerializer.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchRole.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchSeed.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomeSectorOwnership.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchSiteBinding.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatch.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchSnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/PatchCleanupPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchExportError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedBiomePatchRow.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedBiomePatchCsvSerializer.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchExportPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchExportResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchExporter.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/BiomePatchExporterTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/BiomePatchModelsTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

Map reference는 MAP04 roadmap과 frozen `biome_types`, `biome_patch_rules`, `biome_boundary_profiles`, `biome_boundary_pair_rules`만 읽는다. matching meta/inventory/hash/scope 읽기 허용. installed CSV body, unrelated C#, future Task, Legacy, Scene/Prefab YAML 금지.

## Write Allowlist

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchValidationRule.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchValidationViolation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchValidationError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchValidationDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchValidationPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchValidationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchValidator.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/BiomePatchValidatorTests.cs
```

exact 8 C# + matching meta 8 + Result만 생성한다. existing Assets/CSV/asmdef 수정 금지. Runtime/test namespace와 assemblies는 existing Game.Map 계약을 따른다. production UnityEditor/reflection/static mutable state 금지.

## API / Result

```text
BiomePatchValidationResult Validate(
    BiomePatchExportResult exportResult,
    IEnumerable<BiomeTypeDefinition> biomeTypes,
    IEnumerable<BiomePatchRuleDefinition> patchRules,
    IEnumerable<BiomeBoundaryProfileDefinition> boundaryProfiles,
    IEnumerable<BiomeBoundaryPairRuleDefinition> boundaryPairRules)
```

checked-in API shape가 illustrative signature보다 우선한다. Registry/RNG/clock/file/root를 조회하지 않는다.

Status:

```text
Completed          publication+diagnostics, violations/errors 0
ValidationRejected publication null, diagnostics+violations >=1, retry true
InvalidInput       publication/diagnostics null, errors >=1, retry false
```

structural errors는 missing/null/duplicate/unexpected definition, invalid export/source chain/seed/169 rows/bytes를 accumulated sorted/deduped한다. rule violation은 `(Rule, BiomeId, PatchId, SectorIndex, expected, actual, message)` ordinal로 정렬·중복 제거한다.

## Exact 15 Rules

`BiomePatchValidationRule` order:

```text
RequiredBiomeCoverage
PatchDefinitionIdentity
PatchSizeLimits
PatchConnectivity
PatchSeedContract
NormalPatchCountRange
PatchRuleCountRange
SameRuleSeedDistance
WorldEdgePolicy
WorldShareLimits
CoreSiteOwnership
ReservationAssignment
OwnershipExclusivity
IntrusionBoundaryContract
ExportReproducibility
```

각 rule은 독립 실행되며 applicable entity 전체 violation을 수집한다. 한 rule 실패로 다음 rule을 생략하지 않는다.

### Coverage / Definition / Counts

- exact active required four biome definitions, exact ten patch rules, six boundary profiles, six pair rules
- every required biome: Core patch count `>= MinCorePatchCount`
- normal count = Core+Satellite only; `MinPatchCount..MaxPatchCount` (`1..4`) 적용; Intrusion은 별도
- patch count per PatchRuleId는 `SeedCountMin..SeedCountMax`; current Core rules exact `1`, Satellite/Intrusion declared ranges
- patch BiomeId/Role/PatchRuleId는 definition과 exact 일치

### Size / Connectivity / Seed / Edge

- every Core/Satellite: rule min/max, general `2..59`
- every Intrusion: exact 1, `AllowSingleSector=true`, rule min/max 포함
- every patch cardinal connected; every seed contained and index-coordinate/role valid
- Core seed source site non-null; Satellite/Intrusion source null
- two distinct patches of same rule: minimum Manhattan distance between any seed pair `>= MinSeedDistance`
- `CanTouchWorldEdge=false` rule patch는 x/y `0/12` cell 0

### Share

- primary biome total(all roles)은 해당 biome의 normal rules가 합의한 `MaxWorldShare`; cap `floor(169*share)`
- Intrusion role total per intrusion rule/biome은 its own share cap도 별도 적용
- non-finite/out-of-range/inconsistent share definitions은 structural invalid

### Core Site / Reservation / Ownership

- every SiteBinding points to one Core patch, same biome, exact occupied cells
- every binding cell has matching Core seed/source reservation and ownership
- every Core seed has exactly one binding; no orphan
- all P01 unreserved sectors assigned
- unassigned sector는 non-Core reserved footprint만 허용; Core reservation footprint는 matching Core assigned
- exact 169 ownership rows, no overlap/orphan/wrong PatchId/BiomeId; patch sector sum = assigned count
- SecondaryBiomeId는 MAP04에서 전부 empty

### Intrusion Boundary

each Intrusion one-cell sector:

- cardinal neighbor에 same intruder biome Core/Satellite anchor `>=1`
- cardinal neighbor의 foreign host biome 중 active pair가 `BOUND_TUNNEL`을 포함하는 relation `>=1`
- `BOUND_TUNNEL` active/type exact `TUNNEL_INTRUSION`
- allowed directed relations: ROOT→CRATER/MILL/DOUGH, MILL→ROOT/DOUGH; MILL→CRATER/same biome 거부
- Intrusion을 anchor/host로 세지 않음

### Export Reproducibility

- export world seed/snapshot/source chain 동일; world 169 biome fields와 snapshot exact 일치
- patch rows `17`, world rows `169`, assigned/unassigned `165/4` viable conservation
- typed patch row count/bounds/perimeter/seed/site list exact source 재계산값
- `GeneratedBiomePatchCsvSerializer` 및 existing world serializer 재실행 bytes가 publication bytes와 exact 동일
- patch/world filename/header/BOM/CRLF/row count/PK unique
- viable known SHA: patch `7ccf1fc1e6ebd298cc97bed3914395170fc38fe85b2d2392c80c9f30ec000543`, world `07daa96fe5f6ea985aa9e32aa0609d65b95c620a0b05a99426d3093275f8ee1d`
- known SHA는 viable integration evidence이며 production lookup/hard-code gate가 아님

## Diagnostics / Publication

each immutable rule result: rule, passed, checked count, violation count. Diagnostics:

```text
WorldSeed, 15 RuleResults, Violations
Patch/Core/Satellite/Intrusion counts
Assigned/Unassigned counts, PatchSectorSum
RequiredBiome/CoreBinding counts
MaxPatchSize, Disconnected/Overlap/Orphan counts
UnassignedNonReserved/SiteMisownership/IntrusionInvalid counts
Patch/WorldCsvRowCount and byte count
RngDrawCount, SourceMutationCount
```

success expected viable: rules `15/15`, violations `0`, patches `17 = 4/10/3`, assigned/unassigned `165/4`, RNG/source mutation `0/0`.

`BiomePatchValidationPublication` preserves `SourceExport`, approved snapshot/world/rows/CSV byte identity via defensive copies and diagnostics. No repair or new serialization state is published on rejection.

same input + shuffled definitions + culture/time/thread + fresh/reused validator yields same rule/violation/publication result.

## Tests / Gates

`BiomePatchValidatorTests.cs` actual NUnit cases `>=150` covering every rule pass/failure/boundary, multi-violation accumulation/order/dedupe, structural errors, viable 15/15, shuffled/culture determinism, source immutability, no RNG/file/repair.

Actually run:

```text
BiomePatchValidatorTests >=150 PASS
BiomePatchExporterTests    141/141 PASS
BiomePatchModelsTests      107/107 PASS
Required regressions       248/248 PASS
Actually executed total   >=398 PASS
failed/skipped               0/0
```

large suites discovery-only: Game.Map `>=5083`, Full EditMode `>=5152`. forced compile/Console/warning `0/0/0`.

Asset gate:

```text
Assets meta 3132->3140
new Runtime/test/meta 7/1/8; exact Assets changes 16
existing/unexpected 0/0; duplicate GUID 0
Authoring CSV/meta 50/50 unchanged; generated CSV files 0
legacy Editor.meta 6/6; Scene/Prefab/Packages/ProjectSettings 0
```

## Compact Result / Finalize

Result `<=140 lines`: STATUS, apply/SHA, paths+GUID, rule `15/15` 및 actual counters, tests, compile/meta/scope, findings, NEXT만 기록한다.

PASS일 때만 MAP04_09 COMPLETE, Current Task NONE, Last Completed/Result를 MAP04_09로 설정하고 MAP04_10은 LOCKED 유지.

금지: source/definitions 수정, local repair/RNG/file write, test 완화, validator 외 overlay/batch/root, MAP04_10 생성/시작, Git commit/push.
