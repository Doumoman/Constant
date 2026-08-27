# MAP06_01 — Optional Region Models Unity Gate Resume v1.2

```yaml
status_control:
  task_key: MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS
  result_file: REPORTS/MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS_RESULT.md
  repair_contract: UNITY_GATE_RERUN_RESUME
```

## TASK TYPE

```text
NO-CODE-CHANGE RESUME / COMPILE + TEST RUNNER GATE
```

## Goal

MAP06_01 model implementation and boundary-test repair are already applied. The latest result is `BLOCKED` only because Unity/Test Runner could not be reached:

```text
open Editor process: RUNNING
MCP-visible Unity instance: 0
separate batch Unity: blocked by existing open project lock
```

This resume does not authorize more production/test/source changes. It only permits reconnecting the existing Unity Editor or closing the conflicting Editor normally, then rerunning the required compile/Test Runner gates and replacing the current Result.

MAP06_02 remains locked.

## Preconditions / Current Evidence

Read order:

1. MCP entry/rules
2. Master/Status
3. this Task
4. current `MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS_RESULT.md`
5. the six already-repaired boundary tests path-only/body only if needed for static confirmation

Required current state:

```text
Current Task:
MapDesign/MCP/TASKS/MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS.md

Current Task SHA-256:
c97006b76f8b2c55debc1cb2ef586c9af841de1abe25cbf2ad77aff76d0910b6

Current BLOCKED Result:
MapDesign/MCP/REPORTS/MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS_RESULT.md

Current BLOCKED Result SHA-256:
48d979155de5a7aa9bb239fee137590fd54b61f99c56cdc367f273dce99a0b27
```

If Current Task SHA or BLOCKED Result SHA differs, stop with `BLOCKED`. Do not read or start MAP06_02+.

## Already Completed Repair — Preserve

Do not redo or broaden the previous repair. It already changed exactly six existing boundary test C# files:

```text
HorizontalBackboneRouterTests.cs
MandatoryRouteGraphValidatorTests.cs
MandatoryRouteMaskLookupBuilderTests.cs
Map05ExitTests.cs
UpDownConflictResolverTests.cs
VerticalGatewayPlannerTests.cs
```

The intended boundary after repair:

- MAP06_01 `OptionalRegion*` model symbols are allowed.
- MAP06_02+ implementation symbols remain forbidden.
- safety audits for `UnityEditor`, mutable static state, filesystem, RNG/cache, root/pass adapter, batch runner, and generated writer remain active.

## Allowed Actions

Allowed environment actions:

- reconnect this task session to the already-open Unity Editor through the configured Unity MCP
- or ask the user to close the conflicting Editor normally, then run the same project once
- rerun forced import/domain reload/compile
- rerun required EditMode test selections
- replace only the current Result
- finalize Status only after all gates PASS

No force-kill, no destructive process cleanup, no project lock deletion, no hidden skip.

## Write Allowlist

Only this file may be written during execution before finalize:

```text
MapDesign/MCP/REPORTS/MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS_RESULT.md
```

If and only if every gate PASSes, status finalize may update:

```text
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

Do not modify production C#, test C#, `.cs.meta`, CSV, generated CSV, asmdef, Scene, Prefab, Packages, ProjectSettings, Master, or any MAP06_02+ Task.

## Static Baseline to Confirm

```text
OptionalRegion model/test C# = 7/7 preserved
OptionalRegion model/test matching meta = 7/7 preserved
Assets meta = 3254
Authoring CSV/meta = 50/50
duplicate GUID groups = 0
generated CSV files = 0
Scene/Prefab/asmdef/Packages/ProjectSettings changes = 0
```

Mandatory route baseline must remain unchanged:

```text
graph nodes/directed/undirected/route cells = 47/96/48/47
masks T1/T2/T3/T4_UD/T4_LUD/T4_RUD/T4_LRUD = 20/4/4/17/0/0/2
Type4 = U+D mandatory, L/R actual adjacency preserved
UD/LUD/RUD/LRUD = legal
```

## Required Actual Gates

Run actual Unity gates after MCP connection or normal editor-lock resolution:

```text
OptionalRegionModelsTests            194/194 PASS
HorizontalBackboneRouterTests        142/142 PASS
MandatoryRouteGraphValidatorTests    298/298 PASS
MandatoryRouteMaskLookupBuilderTests 127/127 PASS
Map05ExitTests                       132/132 PASS
UpDownConflictResolverTests          194/194 PASS
VerticalGatewayPlannerTests          156/156 PASS
Existing MAP05 aggregate             1959/1959 PASS
Actually executed total              2153/2153 PASS or higher if discovery count legitimately increases
failed/skipped                       0/0
```

Forced import/domain reload/compile/Console/relevant warnings:

```text
0/0/0
```

Unity/Test Runner gate를 실제 실행하지 못하면 다시 `BLOCKED`로 기록한다. Static checks alone cannot produce PASS.

## Result / Finalize

Replace the current Result in `<=150 lines`.

Required sections:

```text
TASK / STATUS / SUMMARY
PATCH APPLY / RESUME BASIS
TEST / UNITY / ASSET META / CHANGE SCOPE
DONE CONDITIONS / NEXT / Recommended Commit
```

PASS Result exact lines:

```text
STATUS: PASS
MAP06_01: COMPLETE ELIGIBLE
MAP06_02_ENUMERATE_OPTIONAL_ATTACHMENTS: LOCKED / DO NOT START
```

PASS일 때만 MAP06_01 COMPLETE, Current Task NONE으로 finalize한다. MAP06_02는 LOCKED로 유지하고 자동 생성/시작하지 않는다.

If Unity remains unavailable:

```text
STATUS: BLOCKED
MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS: CURRENT
MAP06_02_ENUMERATE_OPTIONAL_ATTACHMENTS: LOCKED / DO NOT START
```

Recommended Commit:

```text
test(map): allow optional region model symbols after MAP06_01
```
