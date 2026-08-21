# MAP05_06 — Resolve Up/Down Conflicts

```yaml
status_control:
  task_key: MAP05_06_RESOLVE_UP_DOWN_CONFLICTS
  result_file: REPORTS/MAP05_06_RESOLVE_UP_DOWN_CONFLICTS_RESULT.md
```

## TASK TYPE

```text
RUNTIME IMMUTABLE U/D CONFLICT RESOLUTION PLAN + DETERMINISTIC RESOLVER + EDITMODE TESTS
```

## Objective

`MAP05_05`의 immutable `VerticalGatewayPlan`을 읽고, Type4로 표현할 수 없는 필수 U/D 충돌만 별도 resolution pair로 분리한다. Type4는 `U+D`를 항상 보장하며 L/R은 실제 수평 인접 상태를 그대로 보존한다. 따라서 네 가지 수평 조합(`U+D`, `L+U+D`, `R+U+D`, `L+R+U+D`)은 모두 Type4로 유효하고 충돌로 세지 않는다.

이번 Task는 충돌 계획과 진단만 만든다. Type4 mask family의 최종 등록, loop, graph, `SectorCell.RouteMaskId`, generated CSV, validator, overlay는 후속 Task다.

```text
input vertical gateway pairs = 4
starter Type4-expressible conflicts = 0
starter resolution pairs = 0
Type4 U+D rule = mandatory
Type4 L/R rule = independently preserved, never forced
```

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
11. this Task
12. `REPORTS/MAP05_05_IMPLEMENT_VERTICAL_GATEWAY_PLANNER_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP05_05_IMPLEMENT_VERTICAL_GATEWAY_PLANNER
STATUS: PASS
HORIZONTAL / PENDING / PAIRS: 6 / 4 / 4
TYPE4 JUNCTIONS: 11 exact; all interior cells U+D true, L/R preserved
CONFLICT-PENDING: 0
TEST EXECUTED TOTAL: 1248/1248 PASS
ASSET META: 3197
SHA-256: 016cf5cdd79887252c60b2504cc8ba3f69e037e9af12589e2d3b9b40d038647e
DONE CONDITIONS: PASS
```

## Map Package Reference

```text
01_FIXED_SPEC/06_ROUTE_TOPOLOGY_CONSTRAINTS.md
02_PHASE_ROADMAP/MAP05_ROUTE_123_GENERATOR.md
04_CSV_STARTER/generation_profiles.csv
```

Authoring CSV body는 다시 읽거나 파싱하지 않는다. source of truth는 MAP05_01~05 typed artifacts와 이번 immutable resolution plan이다. MAP05_02 Type1/2/3 lookup은 수정하지 않는다.

## READ ALLOWLIST

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/HorizontalBackbonePlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteMaskLookup.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VerticalGatewayId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VerticalGatewayAnchor.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VerticalGatewayPair.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/VerticalGatewayPlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchValidationPublication.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/VerticalGatewayPlannerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/HorizontalBackboneRouterTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryConnectorTreeBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryTerminalBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/BiomePatchValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map04ExitTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

matching meta, approved `Generation` 직계 path-only inventory, Authoring CSV/meta count/hash, 전체 meta GUID와 task-marker 이후 change-scope만 확인한다. MAP05_07+ Task body와 unrelated production/Scene/Prefab YAML은 읽지 않는다.

## WRITE ALLOWLIST

신규 Runtime production C# exact 8:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/UpDownConflictId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/UpDownConflictCandidate.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/UpDownConflictResolution.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/UpDownConflictResolutionPlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/UpDownConflictBuildError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/UpDownConflictDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/UpDownConflictBuildResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/UpDownConflictResolver.cs
```

신규 Runtime EditMode test exact 1:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/UpDownConflictResolverTests.cs
```

기존 negative symbol audit 전환 exact 4:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryConnectorTreeBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/HorizontalBackboneRouterTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/VerticalGatewayPlannerTests.cs
```

기존 4개는 MAP05_06 output symbols를 허용하도록 negative audit만 전환하고 MAP05_07+ symbols는 계속 금지한다. 신규 C# 9개와 matching `.cs.meta` 9개, Result 1개만 생성한다. 기존 production/CSV/meta/asmdef/Scene/Prefab는 수정하지 않는다.

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
Input artifacts  = VERTICAL_GATEWAY_PLAN + MANDATORY_ROUTE_MASK_LOOKUP + SITE_RESERVATIONS + BIOME_PATCHES
Output artifact  = UP_DOWN_CONFLICT_RESOLUTION_PLAN
Pass ID          = PASS_ROUTE
RNG stream       = none consumed in this Task
Type4-expressible U/D conflicts = 0 on starter
```

## Type4 and Conflict Contract

`UpDownConflictId`는 `public readonly struct`, `IEquatable<UpDownConflictId>`, `IComparable<UpDownConflictId>`이며 exact grammar `^UDC_[0-9]{2}_[A-Z0-9_]+$`를 사용한다. equality/order/hash는 ordinal deterministic이다.

`UpDownConflictCandidate`는 source `VerticalGatewayId`, coordinate, required U/D, actual L/R, reservation/biome identity, checked cost와 `CanBeType4`를 immutable하게 보존한다. `CanBeType4 == true`이면 U+D가 성립하는 즉시 conflict가 아니며 resolution을 만들지 않는다.

`UpDownConflictResolution`은 Type4로 표현할 수 없는 후보에 대해서만 upper/lower adapter pair, inclusive span, source identity, checked cost와 reason을 immutable하게 보존한다. Type4 후보의 L/R을 열거나 닫아서 resolution을 만드는 것은 금지한다.

`UpDownConflictResolutionPlan`은 source `VerticalGatewayPlan` identity, candidate/resolution 목록, `ConflictCount`, `ResolvedCount`, `Type4ExpressibleCount`, `UnresolvedCount`, `TotalCost`와 lookup API를 제공한다. starter에서는 `ConflictCount == 0`, `ResolvedCount == 0`, `UnresolvedCount == 0`이어야 한다.

Resolver는 Type4 family를 다음처럼 판정한다:

```text
isType4 = requiresUp && requiresDown
          && cell is eligible and not forbidden by bounds/reservation/role
```

`isType4`인 모든 조합은 `U+D`, `L+U+D`, `R+U+D`, `L+R+U+D`로 보존한다. L/R을 canonicalize하거나 Type4 단일 ID로 축약하지 않는다. Type4로 불가능한 경우에만 deterministic adjacent gateway candidate를 checked total cost, shorter span, lower X, source ID ordinal 순으로 선택한다. diagonal detour, loop, graph edge, CSV row, `SectorCell.RouteMaskId` write는 금지한다.

## Required Tests

`UpDownConflictResolverTests.cs` actual NUnit cases minimum `150`:

- ID validation/equality/order/hash/culture and immutable candidate/resolution values
- all four Type4 horizontal combinations are accepted with mandatory U+D
- L/R preservation: no forced open/close and no false-invalid rejection
- starter `4` gateway pairs produce zero Type4-expressible conflicts and zero resolutions
- synthetic forbidden-boundary/reservation cases produce stable candidate ordering and adjacent resolution pairs
- unresolved conflict, duplicate ID, source mutation, bounds and reservation rejection
- source identity, lookup, shuffled/culture/thread/fresh-reused determinism
- no RNG/filesystem/clock/UnityEditor/static mutable state; prior negative audit transition

Actually run:

```text
UpDownConflictResolverTests             >=150 PASS
VerticalGatewayPlannerTests             156/156 PASS
HorizontalBackboneRouterTests           142/142 PASS
MandatoryConnectorTreeBuilderTests      129/129 PASS
MandatoryRouteMaskLookupBuilderTests    127/127 PASS
MandatoryTerminalBuilderTests           120/120 PASS
SiteReservationValidatorTests           268/268 PASS
BiomePatchValidatorTests                196/196 PASS
Map04ExitTests                          110/110 PASS
Actually executed total                 >=1398 PASS
failed/skipped                            0/0
Game.Map targeted discovery            >=6189
Full EditMode discovery                 >=6301
forced refresh/compile/Console/warnings  0/0/0
```

## Asset / Meta / Change Gate

```text
baseline Authoring CSV/meta = 50/50; Assets meta = 3197; legacy Editor folder meta = 6/6; duplicate GUID = 0
new Runtime production C# = 8; new Runtime test C# = 1; new matching cs.meta = 9
modified existing test C# = 4; final Assets meta = 3206; task-marker Assets changes = 22
existing production modifications = 0; unexpected Assets changes = 0; new folder meta = 0
```

New meta uses `fileFormatVersion: 2` and unique lowercase 32-hex GUIDs. Existing test `.meta`, Authoring CSV/meta, progress Scene and accepted legacy meta are byte-preserved.

## Failure Policy / Result

Contract/test/compile/meta/change-scope mismatch is `FAIL`; Unity/Test Runner unavailable is `BLOCKED`. Do not finalize or open MAP05_07 unless PASS. Result must be `REPORTS/MAP05_06_RESOLVE_UP_DOWN_CONFLICTS_RESULT.md` within 150 lines and record task/status, patch/read/create/modify, Type4/conflict/resolution counts, diagnostics, deterministic/immutability, test/Unity/meta/change/ownership, out-of-scope, done, next and commit.

PASS일 때만 MAP05_06 COMPLETE, Current Task NONE으로 finalize하고 `MAP05_07_ADD_MANDATORY_ROUTE_LOOPS`는 LOCKED로 유지한다.

Recommended Commit: `feat(map): resolve mandatory up-down conflicts`
