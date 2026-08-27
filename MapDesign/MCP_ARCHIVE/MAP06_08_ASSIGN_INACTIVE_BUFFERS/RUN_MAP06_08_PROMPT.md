# RUN MAP06_08

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP06_08_ASSIGN_INACTIVE_BUFFERS.md`, MAP06_07 PASS Result를 순서대로 읽어라.

Exact gates:

```text
MAP06_07 Result STATUS: PASS
MAP06_07 Result SHA-256: 2815e6b35df71be1477812594435ed4793c3c9a03c60f1ef602267e4a2e12329
MAP06_07 Task SHA-256: 2ab50e5c150bc833395cd9e5f8acb017e8685d90f0b63d5cab394cf0e33b4956
MAP06_08 Task SHA-256: 778d5beb1944ddd01e4541254f6d63d55ce255c3eaeab0f79143ee4de2de9ec7
```

Current Task가 MAP06_08이 아니거나 어느 SHA라도 다르면 `BLOCKED`하고 변경하지 마. MAP06_09 이후 Task body는 읽거나 시작하지 마.

이번 Task는 immutable inactive-buffer assignment와 decorative-boundary classification까지만 구현한다.

Allowed new writes:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/InactiveBufferAssignmentEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/InactiveBufferAssignmentSettings.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/InactiveBufferAssignment.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/InactiveBufferAssignmentDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/InactiveBufferAssignmentResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/InactiveBufferAssigner.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/InactiveBufferAssignerTests.cs
MapDesign/MCP/REPORTS/MAP06_08_ASSIGN_INACTIVE_BUFFERS_RESULT.md
```

Phase-boundary advance를 위해 Current Task의 exact up-to-13 existing test allowlist만 수정할 수 있다. matching existing meta는 수정하지 마.

Approved inputs/settings:

```text
world / dimensions = 169 / 13x13
site reservations / reserved sectors = 7 / 8
biome sectors / assigned / reserved-unassigned = 169 / 165 / 4
mandatory graph nodes/directed/undirected/route cells = 47/96/48/47
optional regions / Type0 cells / return assignments = 12/39/12
returnable/non-returnable = 39/0
Type0 digest = a26e73f25ff7267760a2507ec55b1acda3a6c39a8f3698fc37b190620ceca525
growth digest = 1f00f718bdb8d79fbf88923be0d81e0075987267c024cc624086ee6194998caa
return digest = cff0556a59e66fcc16b886ecf3082779efe9535bb79dcf45b401d12ff0971f6b
RequireFullWorldAccounting = true
RequireClosedInactiveBoundaries = true
ClassifyClaimAdjacentAsDecorativeBoundary = true
Type4 = U+D mandatory, L/R independent, UD/LUD/RUD/LRUD legal
```

Approved accounting:

```text
ReservedSite/Mandatory/Type0 protected = 8/47/39
protected union = 94
InactiveBuffer assignments = 75
world accounting = 169
unassigned/overlap/duplicate = 0/0/0
open mandatory-or-Type0 edge to inactive = 0
RNG/source mutation/partial publication = 0/0/0
```

각 unclaimed sector는 existing `SectorRole.InactiveBuffer`다. protected cardinal neighbor가 있으면 `DecorativeBoundary`, 없으면 `InteriorInactive`로 분류한다. exact split과 edge counters는 independent test oracle과 Result에 기록하되 production에 하드코딩하지 않는다.

boundary profile/recipe/microchunk/tile/socket/edge, validator, overlay, exit, generated CSV를 만들지 않는다. `GeneratedWorldData`, `SectorCell`과 이전 source를 in-place 수정하지 않는다. Authoring CSV/meta는 수정하지 않는다.

Required actual gates:

```text
InactiveBufferAssignerTests >=280 PASS
OptionalReturnPolicyResolverTests 289/289 PASS
OptionalRewardTierCalculatorTests 279/279 PASS
OptionalAccessRuleAssignerTests 289/289 PASS
Type0RouteMaskAssignerTests 257/257 PASS
MAP06 prior combined selection 630/630 PASS
Existing MAP05 aggregate 1959/1959 PASS
Actually executed total >=3983 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3297 -> 3304
new C#/meta 7/7
existing boundary test C# modified <=13
Authoring CSV/meta 50/50 and manifest unchanged
duplicate GUID groups 0
forbidden production/CSV/Scene/Prefab/asmdef changes 0
generated CSV 0
boundary/recipe/microchunk/tile/socket/edge artifacts 0
```

전부 PASS일 때만 MAP06_08 COMPLETE/Current Task NONE으로 finalize한다. `MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR`는 LOCKED로 유지하고 자동 시작하지 않는다.
