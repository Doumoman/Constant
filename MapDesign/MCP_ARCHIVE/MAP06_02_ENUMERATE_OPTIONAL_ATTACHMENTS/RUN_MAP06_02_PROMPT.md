# RUN MAP06_02

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP06_02_ENUMERATE_OPTIONAL_ATTACHMENTS.md`, MAP06_01 PASS Result를 순서대로 읽어라.

Prior result gate:

```text
TASK: MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS
STATUS: PASS
SHA-256: 8d8f2b8bae5b08c9bf5fd258a225db89d16bffa5ca8faa058ef78ac02334442e
MAP06_01: COMPLETE ELIGIBLE
MAP06_02_ENUMERATE_OPTIONAL_ATTACHMENTS: LOCKED / DO NOT START
```

Current Task가 MAP06_02가 아니거나 Prior Result SHA가 다르면 `BLOCKED`하고 변경하지 마. MAP06_03 이후 Task body는 읽거나 시작하지 마.

이번 Task는 optional attachment candidate enumeration까지만 구현한다.

Allowed writes:

```text
<same runtime directory as existing OptionalRegionId.cs>/OptionalAttachmentCandidateId.cs
<same runtime directory as existing OptionalRegionId.cs>/OptionalAttachmentCandidate.cs
<same runtime directory as existing OptionalRegionId.cs>/OptionalAttachmentEnumerationSettings.cs
<same runtime directory as existing OptionalRegionId.cs>/OptionalAttachmentEnumerationDiagnostics.cs
<same runtime directory as existing OptionalRegionId.cs>/OptionalAttachmentEnumerationResult.cs
<same runtime directory as existing OptionalRegionId.cs>/OptionalAttachmentEnumerator.cs
<same test directory as existing OptionalRegionModelsTests.cs>/OptionalAttachmentEnumeratorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/HorizontalBackboneRouterTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteGraphValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map05ExitTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/UpDownConflictResolverTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/VerticalGatewayPlannerTests.cs
MapDesign/MCP/REPORTS/MAP06_02_ENUMERATE_OPTIONAL_ATTACHMENTS_RESULT.md
```

Do not implement MAP06_03+ behavior: optional grower, Type0 mask assignment, access/clue placement, reward calculation, return device, inactive buffer, validator, overlay, generated CSV writer.

Mandatory route baseline must remain unchanged:

```text
graph nodes/directed/undirected/route cells = 47/96/48/47
mask T1/T2/T3/T4_UD/T4_LUD/T4_RUD/T4_LRUD = 20/4/4/17/0/0/2
Type4 = U+D mandatory, L/R actual adjacency preserved
UD/LUD/RUD/LRUD = legal
```

Required actual gates:

```text
OptionalAttachmentEnumeratorTests >=160 PASS
OptionalRegionModelsTests 194/194 PASS
Existing MAP05 phase aggregate 1959/1959 PASS
Actually executed total >=2313 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3254 -> 3261
new C#/meta 7/7
existing boundary test C# modified <=6
Authoring CSV/meta 50/50
duplicate GUID groups 0
production graph/CSV/SectorCell/asmdef/Scene/Prefab/Packages/ProjectSettings modifications 0
generated CSV files created by this task 0
```

전부 PASS일 때만 MAP06_02 COMPLETE/Current Task NONE으로 finalize한다. `MAP06_03_IMPLEMENT_OPTIONAL_REGION_GROWER`는 LOCKED로 유지하고 자동 시작하지 않는다.
