# RUN MAP06_01

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS.md`, MAP05_11 PASS Result를 순서대로 읽어라.

Prior result gate:

```text
TASK: MAP05_11_MAP05_BATCH_AND_EXIT_TESTS
STATUS: PASS
SHA-256: 5fdd4354d1ceee50376c3a8cd535e391af4db10baa148c682cf70247b19b40ff
MAP05 EXIT: APPROVED
MAP06 ENTRY: ELIGIBLE FOR SEPARATE PATCH
```

Current Task가 MAP06_01이 아니거나 Prior Result SHA가 다르면 `BLOCKED`하고 변경하지 마. MAP06_02 이후 Task body는 읽거나 시작하지 마.

이번 Task는 optional region model 계약만 구현한다.

Allowed writes:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionAttachment.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegion.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionSnapshot.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionModelsTests.cs
MapDesign/MCP/REPORTS/MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS_RESULT.md
```

Do not implement MAP06_02+ behavior: attachment enumeration, grower, Type0 mask assignment, access/clue placement, reward calculation, return device, inactive buffer, validator, overlay, generated CSV writer.

Mandatory route baseline must remain unchanged:

```text
graph nodes/directed/undirected/route cells = 47/96/48/47
mask T1/T2/T3/T4_UD/T4_LUD/T4_RUD/T4_LRUD = 20/4/4/17/0/0/2
Type4 = U+D mandatory, L/R actual adjacency preserved
UD/LUD/RUD/LRUD = legal
```

Required actual gates:

```text
OptionalRegionModelsTests >=120 PASS
Existing MAP05 phase aggregate 1959/1959 PASS
Actually executed total >=2079 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3247 -> 3254
exact Assets changes 14
Authoring CSV/meta 50/50
duplicate GUID groups 0
production/test existing modifications 0
generated CSV/Scene/Prefab/asmdef/Packages/ProjectSettings 0
```

Approved MAP05_11 `Diagnostics.meta` baseline은 보존하고 delete/recreate하지 마.

전부 PASS일 때만 MAP06_01 COMPLETE/Current Task NONE으로 finalize한다. `MAP06_02_ENUMERATE_OPTIONAL_ATTACHMENTS`는 LOCKED로 유지하고 자동 시작하지 않는다.
