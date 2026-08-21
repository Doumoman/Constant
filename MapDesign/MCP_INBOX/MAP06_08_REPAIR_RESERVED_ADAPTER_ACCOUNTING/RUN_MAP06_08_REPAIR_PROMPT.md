# RUN MAP06_08 REPAIR

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP06_08_ASSIGN_INACTIVE_BUFFERS.md`, 현재 MAP06_08 BLOCKED Result, MAP06_07 PASS Result를 순서대로 읽어라.

Phase A precondition:

```text
Current Task = TASKS/MAP06_08_ASSIGN_INACTIVE_BUFFERS.md
Current Task SHA-256 before repair apply = 778d5beb1944ddd01e4541254f6d63d55ce255c3eaeab0f79143ee4de2de9ec7
Revised Task SHA-256 after repair apply = 0e45ed924cd515ca497abca85e0ede2a6efddefa9648c72c21b0d00a93647340
Current Result = MapDesign/MCP/REPORTS/MAP06_08_ASSIGN_INACTIVE_BUFFERS_RESULT.md
Current Result STATUS = BLOCKED
Current Result SHA-256 = 759de495f3e2608fba844e5cca5ab3c6d7cd0479a73c8a3928c1ac4b964045fa
Prior MAP06_07 Result STATUS = PASS
Prior MAP06_07 Result SHA-256 = 2815e6b35df71be1477812594435ed4793c3c9a03c60f1ef602267e4a2e12329
```

값이 다르면 `BLOCKED`하고 변경하지 마. MAP06_09 이후 Task body는 읽거나 시작하지 마.

Repair only this accounting contradiction:

```text
Incorrect old contract:
  ReservedSite source sectors = 8
  Mandatory route cells       = 47
  Type0 cells                  = 39
  pairwise intersections      = 0
  protected union             = 94
  inactive assignments        = 75

Approved fixture reality:
  Site ∩ Mandatory            = 0,28,106
  Site ∩ Type0                = empty
  Mandatory ∩ Type0           = empty
  protected union             = 91
```

Revised accounting:

```text
source counts ReservedSite/Mandatory/Type0 = 8/47/39
approved reserved-adapter overlap          = 3 at 0,28,106
exclusive projected ReservedSite           = 8
exclusive projected MandatoryOnly          = 44
exclusive projected Type0                  = 39
InactiveBuffer assignments                 = 78
full accounting                            = 169 = 8 + 44 + 39 + 78
illegal overlap / duplicate / unassigned   = 0/0/0
```

Keep and repair only the MAP06_08 allowlisted files already created by the BLOCKED attempt:

```text
InactiveBufferAssignmentEnums.cs
InactiveBufferAssignmentSettings.cs
InactiveBufferAssignment.cs
InactiveBufferAssignmentDiagnostics.cs
InactiveBufferAssignmentResult.cs
InactiveBufferAssigner.cs
InactiveBufferAssignerTests.cs
```

Allowed boundary test files remain the exact up-to-13 allowlist in the revised Task. Keep MAP06_08 symbols allowed and MAP06_09+ symbols forbidden.

Required actual gates after repair:

```text
InactiveBufferAssignerTests >=281 PASS
OptionalReturnPolicyResolverTests 289/289 PASS
OptionalRewardTierCalculatorTests 279/279 PASS
OptionalAccessRuleAssignerTests 289/289 PASS
Type0RouteMaskAssignerTests 257/257 PASS
MAP06 prior combined selection 630/630 PASS
Existing MAP05 aggregate 1959/1959 PASS
Actually executed required total >=3984 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3304
MAP06_08 C#/meta preserved 7/7
existing boundary test C# modified <=13
Authoring CSV/meta 50/50 and manifest unchanged
duplicate GUID groups 0
forbidden production/CSV/Scene/Prefab/asmdef changes 0
generated CSV 0
boundary/recipe/microchunk/tile/socket/edge artifacts 0
```

전부 PASS일 때만 MAP06_08 COMPLETE/Current Task NONE으로 finalize한다. `MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR`는 LOCKED로 유지하고 자동 시작하지 않는다.
