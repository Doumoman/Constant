# RUN MAP07_13

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP07_13_MAP07_STARTER_AND_EXIT_TESTS.md`, MAP07_12 PASS Result를 순서대로 읽어라.

Exact gates:

```text
MAP07_12 Result STATUS: PASS
MAP07_12 Result SHA-256: 869e5e640495e1ec4f7e376133d2525c9e0efe669296e949c7fe7b7d37c92876
MAP07_12 Task SHA-256: 73544122f13653fa3762e87fdc75b7a415482b7336e0ac3fe3a75760d51ec9b0
MAP07_13 Task SHA-256: 698a330dcd7a8ba14ec33cec51b68ea56be9382abd0eefde96eb2a516c81effb
```

Current Task가 MAP07_13이 아니거나 어느 SHA라도 다르면 `BLOCKED`하고 변경하지 마. MAP08_01 이후 Task body는 읽거나 시작하지 마.

이번 Task는 MAP07 starter catalog full round-trip과 MAP07 phase exit tests만 구현한다.

Allowed new writes:

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/MicrochunkStarterCatalogRoundTripTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Microchunks/Map07ExitTests.cs
MapDesign/MCP/REPORTS/MAP07_13_MAP07_STARTER_AND_EXIT_TESTS_RESULT.md
```

Phase-boundary advance를 위해 Current Task의 exact up-to-18 existing test allowlist만 수정할 수 있다. matching existing meta는 수정하지 마.

Required facts:

```text
New production C#: forbidden
Starter catalog rows: full validation
Complete tile chunks: exactly 96 unique in-bounds cells
Transforms: R0 / MIRROR_X / MIRROR_Y / R180
90-degree rotation remains forbidden
Round-trip uses temp Authoring copies only
MAP07 phase exit approval is allowed only on PASS
MAP08_01 remains locked
```

Forbidden this task:

```text
MAP08 boundary pair/content work
sector assembly
world-level traversal
generated CSV writer
Authoring CSV mutation outside temp fixtures
Scene/Prefab/ProjectSettings/asmdef changes
runtime C# changes
editor production C# changes
```

Required actual gates:

```text
MicrochunkStarterCatalogRoundTripTests >=620 PASS
Map07ExitTests >=180 PASS
MicrochunkPreviewAndReportTests 520/520 PASS
MicrochunkCsvExporterTests 460/460 PASS
MicrochunkCsvImporterTests 420/420 PASS
MicrochunkSocketAndSlotEditorTests 380/380 PASS
MicrochunkAuthoringGridTests 320/320 PASS
MicrochunkReachabilityProbeTests 522/522 PASS
Existing MAP07 regression union 2000/2000 PASS
MAP06 required total 2746/2746 PASS
MAP05 required total 1959/1959 PASS
Actually executed total >=10127 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3407 -> 3409
new production C#/meta 0/0
new Editor test C#/meta 2/2
new folder meta 0
new Runtime C#/meta 0/0
Authoring CSV/meta 50/50 and manifest unchanged
Authoring CSV tracked changes 0
Generated CSV 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes 0
Forbidden MAP08+ production symbol hits 0
```

전부 PASS일 때만 MAP07_13 COMPLETE/Current Task NONE으로 finalize하고 `MAP07 PHASE EXIT: APPROVED`를 기록한다. `MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS`는 LOCKED로 유지하고 자동 시작하지 않는다.
