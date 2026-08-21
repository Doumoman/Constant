# RUN MAP06_04

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP06_04_ASSIGN_TYPE0_ROUTE_MASKS.md`, MAP06_03 PASS Result를 순서대로 읽어라.

Exact gates:

```text
MAP06_03 Result STATUS: PASS
MAP06_03 Result SHA-256: 370a15f504d46492a591d064ee70dbc35d27b5b55ab4b621617aedae95d489b0
MAP06_03 Task SHA-256: dbdde1bc53b615649c377c700a9c9d35f8de81baa2fcf79253f0e7d35974eb88
MAP06_04 Task SHA-256: 320870304bc61d7414a10473978ae11472adefd88c6f8cd76bb6f909ac136cea
```

Current Task가 MAP06_04가 아니거나 어느 SHA라도 다르면 `BLOCKED`하고 변경하지 마. MAP06_05 이후 Task body는 읽거나 시작하지 마.

이번 Task는 exact registered Type0 route-mask catalog와 per-cell assignment까지만 구현한다.

Allowed new writes:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteOpenMask.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskAssignment.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskAssignmentDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskAssignmentResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/Type0RouteMaskAssigner.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Type0RouteMaskAssignerTests.cs
MapDesign/MCP/REPORTS/MAP06_04_ASSIGN_TYPE0_ROUTE_MASKS_RESULT.md
```

Phase-boundary advance를 위해 Current Task의 exact up-to-9 existing test allowlist만 수정할 수 있다. matching existing meta는 수정하지 마.

Exact Type0 IDs:

```text
ROUTE_T0_NONE, ROUTE_T0_L, ROUTE_T0_R, ROUTE_T0_U, ROUTE_T0_D,
ROUTE_T0_LU, ROUTE_T0_LD, ROUTE_T0_RU, ROUTE_T0_RD,
ROUTE_T0_UD, ROUTE_T0_LUD, ROUTE_T0_RUD
```

모두 route_type 0 / active true / mandatory_allowed false다. `ROUTE_T0_LR`, `ROUTE_T0_LRUD`, 임의 bool 조합은 금지다. 각 cell mask는 같은 region neighbor의 internal BaseEdge required sides와 exact match여야 한다. attachment→mandatory boundary는 base mask에서 closed이고 MAP06_05+ OptionalOverlayEdge로 남겨야 한다.

Approved inputs:

```text
regions/cells = 12/39
growth digest = 1f00f718bdb8d79fbf88923be0d81e0075987267c024cc624086ee6194998caa
attachment digest = 68b438c523645c2f6721fa0c104c3cd4c282076292cd2e035cd20a2b272aaee6
graph nodes/directed/undirected/route cells = 47/96/48/47
mask T1/T2/T3/T4_UD/T4_LUD/T4_RUD/T4_LRUD = 20/4/4/17/0/0/2
Type4 = U+D mandatory, L/R independent, UD/LUD/RUD/LRUD legal
```

Required actual gates:

```text
Type0RouteMaskAssignerTests >=220 PASS
OptionalRegionGrowerTests 234/234 PASS
OptionalAttachmentEnumeratorTests 202/202 PASS
OptionalRegionModelsTests 194/194 PASS
Existing MAP05 aggregate 1959/1959 PASS
Actually executed total >=2809 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3266 -> 3274
new C#/meta 8/8
Authoring CSV/meta 50/50 and manifest unchanged
duplicate GUID groups 0
forbidden production/CSV/Scene/Prefab/asmdef changes 0
generated CSV 0
```

전부 PASS일 때만 MAP06_04 COMPLETE/Current Task NONE으로 finalize한다. `MAP06_05_ASSIGN_ACCESS_RULES_AND_CLUES`는 LOCKED로 유지하고 자동 시작하지 않는다.


