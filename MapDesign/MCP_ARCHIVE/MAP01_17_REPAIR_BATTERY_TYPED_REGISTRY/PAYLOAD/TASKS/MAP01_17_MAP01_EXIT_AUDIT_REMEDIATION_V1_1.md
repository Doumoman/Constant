# MAP01_17 Remediation v1.1 — Battery Typed Registry

이 문서는 Current Task `MAP01_17_MAP01_EXIT_AUDIT`의 승인된 보완 계약이다. 원 Task와 함께 읽고 실행한다. Current Task identity와 Result path는 변경하지 않는다.

## Root Cause

`battery_profiles.csv`는 catalog/parse/FK/generic Registry record membership까지 성공하지만 MAP01_10의 exact 16-source definition contract에서 누락됐다. 따라서 `BatteryProfileDefinition`, `MicrochunkPopulationItemDefinitionSet.BatteryProfiles`, Registry typed identity가 존재하지 않아 audit 40건 중 Battery set 1 + individual ID 5가 실패했다.

CSV/dictionary를 변경하거나 generic record만으로 typed gate를 통과시키지 않는다.

## Expanded WRITE ALLOWLIST

Production existing files:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Data/ItemResourceDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/MicrochunkPopulationItemDefinitionSource.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/MicrochunkPopulationItemDefinitionSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/MicrochunkPopulationItemDefinitionBuilder.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/StaticDataRegistryBuilder.cs
```

Existing tests:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/MicrochunkPopulationItemDefinitionBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/StaticDataRegistryBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/Map01ExitAuditTests.cs
```

`CsvImportPipeline.cs`는 Microchunk source subset이 public `ExpectedFileNames`를 사용하지 않고 exact 16을 별도 hard-code한 경우에만 `battery_profiles.csv` 전달 한정으로 수정할 수 있다:

```text
Assets/_Game/Map/Editor/WorldGeneration/Data/CsvImportPipeline.cs
```

다른 production/test/asmdef/CSV/meta/Scene/Prefab은 변경 금지. compile reference inventory에서 추가 consumer 수정이 필요하면 임의 확장하지 말고 BLOCKED한다.

## BatteryProfileDefinition — Exact 17 Columns

```text
battery_id ID required PK1
display_name_ko STRING required
fuel_cost INT required
battery_item_cost INT required
delivery_mode ENUM required PLACE|THROW|BLAST_CONE
blast_radius_tiles FLOAT required
damage INT required
knockback FLOAT required
destroys_soft_soil BOOL required
destroys_cracked_terrain BOOL required
destroys_hard_terrain BOOL required
destroys_starstone BOOL required
terrain_damage_enabled BOOL required
fuse_seconds FLOAT required
prefab_id ID required FK prefab_registry.csv.prefab_id
active BOOL required default 1
notes STRING optional
```

PascalCase typed immutable properties와 exact `CsvParsedRecord SourceRecord` identity를 제공한다. numeric은 existing invariant typed values, delivery mode는 ordinal validated token, PrefabId는 unresolved string ID, Notes optional empty contract를 따른다. domain validation이나 enum C# 변환을 추가하지 않는다.

## Definition Set / Builder Contract

- `MicrochunkPopulationItemDefinitionSource.ExpectedFileNames`: exact 16 → exact 17, `battery_profiles.csv`를 ordinal inventory 위치에 포함한다.
- builder는 battery schema inventory/order/type/required/default/PK/FK를 exact 검증하고 모든 successful row를 materialize한다.
- `MicrochunkPopulationItemDefinitionSet.BatteryProfiles`: ordinal sorted immutable `IReadOnlyDictionary<string, BatteryProfileDefinition>`.
- source row shuffle에도 membership/order가 결정적이고 five definitions의 SourceRecord identity가 원 parsed record와 동일하다.
- missing/duplicate/unexpected/unsuccessful/schema mismatch/field mapping error는 기존 deterministic failure policy를 사용하고 partial set을 publish하지 않는다.
- 기존 16 family output과 ordering/API는 변경하지 않는다.

## Registry Typed Identity Contract

- `StaticDataRegistryBuilder`가 BatteryProfiles 5개를 typed-definition identity map에 정확히 한 번 포함한다.
- identity key는 `battery_profiles.csv` + exact record number이며 FK record index의 동일 SourceRecord instance와 일치한다.
- `Registry.MicrochunkPopulationItemDefinitions.BatteryProfiles`에서 exact ordinal lookup 가능해야 한다.
- typed map count는 정확히 +5; generic AllRecords/FK/reverse indexes는 중복·손실·재정렬 없이 유지한다.
- `prefab_id` outgoing FK와 target Prefab SourceRecord가 기존 resolver 결과와 동일해야 한다.

## Required IDs

```text
BAT_MINI
BAT_AIR_CANNON
BAT_STANDARD
BAT_MEGA
BAT_GRENADE
```

source PK set, definition-set typed key set, Registry typed lookup set이 exact equality여야 한다.

## Tests / Verification

- Battery definition 17 fields/type/default/optional/source identity 최소 8 cases.
- exact 17 source inventory, missing/duplicate/schema near-miss/order stability 최소 6 cases.
- Registry typed map inclusion/count/identity/FK/no-regression 최소 6 cases.
- remediation 신규/갱신 focused 최소 `20/20 PASS`.
- 기존 MAP01 exit audit `40/40 PASS`로 전환.
- Microchunk builder 전체 최소 `97 PASS`, Registry 전체 최소 `53 PASS`.
- MAP01_16 fixture `37/37`, window `48/48`, world route `73/73`, atomic/hash/Registry/FK 기존 회귀 PASS.
- targeted total 최소 `861 PASS`, full EditMode 최소 `881 PASS`.
- actual fixed 50 reimport: ERROR/WARNING/FK 0, published true, required Battery `5/5`, ContentVersionHash stable.
- Unity compile/relevant warning 0/0, visual published 50/0 issues PASS.
- Authoring CSV/meta 50/50, non-allowlisted production/tests, asmdef, Scene/Prefab unchanged.

## Completion

기존 `REPORTS/MAP01_17_MAP01_EXIT_AUDIT_RESULT.md`에 `REMEDIATION v1.1`을 추가하고 exact changed files/SHA, Battery 17-field API, typed set/Registry identity/FK, tests, actual report/hash/visual evidence를 기록한다.

모든 원 Task gate와 위 조건이 PASS일 때만 Result top-level `STATUS: PASS`, MAP01_17 COMPLETE, Current Task NONE, `MAP01 PHASE GATE APPROVED`로 finalize한다. 실패하면 MAP01_17 CURRENT/BLOCKED 유지. MAP02_01은 계속 LOCKED이며 자동 시작하지 않는다.

Recommended Commit: `fix(map): publish battery profiles in typed registry`
