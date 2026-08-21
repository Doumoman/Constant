# RUN MAP06_03

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP06_03_IMPLEMENT_OPTIONAL_REGION_GROWER.md`, MAP06_02 PASS Result를 순서대로 읽어라.

Exact gates:

```text
MAP06_02 Result STATUS: PASS
MAP06_02 Result SHA-256: 69b6dbc5b379de297805ba8d9b3523779e26486a9244b3f2306523e70c9c123c
MAP06_02 Task SHA-256: e87e9d55254243eea6ff590b84fb68225077890d454fde978b330a0f4ad805da
MAP06_03 Task SHA-256: dbdde1bc53b615649c377c700a9c9d35f8de81baa2fcf79253f0e7d35974eb88
```

Current Task가 MAP06_03이 아니거나 어느 SHA라도 다르면 `BLOCKED`하고 변경하지 마. MAP06_04 이후 Task body는 읽거나 시작하지 마.

이번 Task는 optional region topology growth까지만 구현한다.

Allowed new writes:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionGrowthSettings.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionGrowthDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionGrowthResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionGrower.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionGrowerTests.cs
MapDesign/MCP/REPORTS/MAP06_03_IMPLEMENT_OPTIONAL_REGION_GROWER_RESULT.md
```

Phase-boundary advance를 위해 Current Task의 exact up-to-8 existing test allowlist만 수정할 수 있다. matching existing meta는 수정하지 마.

Do not implement MAP06_04+ behavior: Type0 mask/edge assignment, access/clue, reward calculation, return path/device, inactive buffer, validator, overlay, generated CSV writer.

Approved inputs:

```text
candidate raw/accepted = 188/51
candidate digest = 68b438c523645c2f6721fa0c104c3cd4c282076292cd2e035cd20a2b272aaee6
graph nodes/directed/undirected/route cells = 47/96/48/47
mask T1/T2/T3/T4_UD/T4_LUD/T4_RUD/T4_LRUD = 20/4/4/17/0/0/2
Type4 = U+D mandatory, L/R independent, UD/LUD/RUD/LRUD legal
```

Required actual gates:

```text
OptionalRegionGrowerTests >=200 PASS
OptionalAttachmentEnumeratorTests 202/202 PASS
OptionalRegionModelsTests 194/194 PASS
Existing MAP05 aggregate 1959/1959 PASS
Actually executed total >=2555 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3261 -> 3266
new C#/meta 5/5
Authoring CSV/meta 50/50 and manifest unchanged
duplicate GUID groups 0
forbidden production/CSV/Scene/Prefab/asmdef changes 0
generated CSV 0
```

전부 PASS일 때만 MAP06_03 COMPLETE/Current Task NONE으로 finalize한다. `MAP06_04_ASSIGN_TYPE0_ROUTE_MASKS`는 LOCKED로 유지하고 자동 시작하지 않는다.
