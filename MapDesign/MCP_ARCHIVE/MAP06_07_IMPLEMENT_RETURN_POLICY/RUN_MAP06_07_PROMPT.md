# RUN MAP06_07

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP06_07_IMPLEMENT_RETURN_POLICY.md`, MAP06_06 PASS Result를 순서대로 읽어라.

Exact gates:

```text
MAP06_06 Result STATUS: PASS
MAP06_06 Result SHA-256: 0acfcd73b6485e99a56dd4d44bff50f871548e266ed003607466961632ec449c
MAP06_06 Task SHA-256: 8c8dd6a780b334edf7fb8c1276c1cc5d64332bf26f8c5ab9b69e9dabcb22a542
MAP06_07 Task SHA-256: 2ab50e5c150bc833395cd9e5f8acb017e8685d90f0b63d5cab394cf0e33b4956
```

Current Task가 MAP06_07이 아니거나 어느 SHA라도 다르면 `BLOCKED`하고 변경하지 마. MAP06_08 이후 Task body는 읽거나 시작하지 마.

이번 Task는 logical backtrack return policy와 returnability witness까지만 구현한다.

Allowed new writes:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalReturnPolicyResolutionEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalReturnPolicySettings.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalReturnPolicyAssignment.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalReturnPolicyDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalReturnPolicyResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalReturnPolicyResolver.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalReturnPolicyResolverTests.cs
MapDesign/MCP/REPORTS/MAP06_07_IMPLEMENT_RETURN_POLICY_RESULT.md
```

Phase-boundary advance를 위해 Current Task의 exact up-to-12 existing test allowlist만 수정할 수 있다. matching existing meta는 수정하지 마.

Approved inputs/settings:

```text
Type0/access/reward/growth digests:
a26e73f25ff7267760a2507ec55b1acda3a6c39a8f3698fc37b190620ceca525
5268b7ed2342f197fb5717c85dbfdb8e51b3c520c217ecf93ad14dc944ecf74f
c3430c42a27937e143fa89c5839282b9533b62d5fb74fb26fdad490cb545958e
1f00f718bdb8d79fbf88923be0d81e0075987267c024cc624086ee6194998caa
regions / Type0 cells / access / reward = 12/39/12/12
internal reciprocal BaseEdges = 30
attachment base-closed / mandatory base-open = 12/0
MaximumBacktrackSectorCount = 6
RequireAllCellsReturnable = true
Type4 = U+D mandatory, L/R independent, UD/LUD/RUD/LRUD legal
```

Approved output:

```text
returnable/non-returnable cells = 39/0
Backtrack/ReturnGate/SafeExit = 12/0/0
critical witness sector/edge totals = 31/19
maximum witness sector count = 4
same opened attachment returns = 12
return device/extra safe exit/base-open = 0/0/0
```

모든 `RequiresReturnConnection`은 false다. RegionId order로 internal graph를 만들고 fixed `L,R,U,D` BFS를 사용한다. critical source는 depth descending, sector index ascending이며 path는 source와 attachment를 포함한다.

same opened/discovered attachment를 reverse-use하되 base mask를 열지 않는다. concrete ReturnGate/SafeExit/device ID/prefab/socket/edge/recipe/tile marker, inactive buffer, validator, overlay, generated CSV를 만들지 않는다. Authoring CSV/meta는 수정하지 않는다.

Required actual gates:

```text
OptionalReturnPolicyResolverTests >=270 PASS
OptionalRewardTierCalculatorTests 279/279 PASS
OptionalAccessRuleAssignerTests 289/289 PASS
Type0RouteMaskAssignerTests 257/257 PASS
MAP06 prior combined selection 630/630 PASS
Existing MAP05 aggregate 1959/1959 PASS
Actually executed total >=3684 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3290 -> 3297
new C#/meta 7/7
existing boundary test C# modified <=12
Authoring CSV/meta 50/50 and manifest unchanged
duplicate GUID groups 0
forbidden production/CSV/Scene/Prefab/asmdef changes 0
generated CSV 0
synthetic return artifacts 0
```

전부 PASS일 때만 MAP06_07 COMPLETE/Current Task NONE으로 finalize한다. `MAP06_08_ASSIGN_INACTIVE_BUFFERS`는 LOCKED로 유지하고 자동 시작하지 않는다.

