# RUN MAP06_05

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP06_05_ASSIGN_ACCESS_RULES_AND_CLUES.md`, MAP06_04 PASS Result를 순서대로 읽어라.

Exact gates:

```text
MAP06_04 Result STATUS: PASS
MAP06_04 Result SHA-256: 7cfb055bb6cb1df24206b25a1a5f046936c7fbdf58bd4b307d476ead4f28ed7a
MAP06_04 Task SHA-256: 320870304bc61d7414a10473978ae11472adefd88c6f8cd76bb6f909ac136cea
MAP06_05 Task SHA-256: d80cf04261811777b65b6c99ca8b7ae368fc39f4a895d024c6639ada5226c587
```

Current Task가 MAP06_05가 아니거나 어느 SHA라도 다르면 `BLOCKED`하고 변경하지 마. MAP06_06 이후 Task body는 읽거나 시작하지 마.

이번 Task는 MAP06_04 Type0 assignment 위에 logical access/clue reservation과 MAP06_06용 cost inputs까지만 구현한다.

Allowed new writes:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessClueId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessAssignmentEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessClue.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessAssignmentSettings.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessAssignment.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessAssignmentDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessAssignmentResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalAccessRuleAssigner.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalAccessRuleAssignerTests.cs
MapDesign/MCP/REPORTS/MAP06_05_ASSIGN_ACCESS_RULES_AND_CLUES_RESULT.md
```

Phase-boundary advance를 위해 Current Task의 exact up-to-10 existing test allowlist만 수정할 수 있다. matching existing meta는 수정하지 마.

Approved assignment inputs:

```text
AccessRulePattern = Basic / Tool / Environment / Explosive / Hidden
Approved 12-region distribution = 3 / 3 / 2 / 2 / 2
ToolRequirementPattern = Pickaxe / Shovel / Rope
HiddenCluePattern = HiddenCrack / HiddenLight / HiddenSound
ToolCostTierByDepth = 1 / 2 / 3 / 4
ExplosiveFuelCostByDepth = 10 / 20 / 30 / 40
HiddenClueDifficultyByDepth = 1 / 2 / 3 / 4
regions/cells/Type0 assignments = 12/39/39
attachment boundaries base-closed = 12
mandatory boundary base-open = 0
Type0 assignment digest = a26e73f25ff7267760a2507ec55b1acda3a6c39a8f3698fc37b190620ceca525
growth digest = 1f00f718bdb8d79fbf88923be0d81e0075987267c024cc624086ee6194998caa
Type4 = U+D mandatory, L/R independent, UD/LUD/RUD/LRUD legal
```

Rule matrix:

```text
Basic       -> None        / OptionalBreak / BasicOpening             / all costs 0
Tool        -> P/S/R       / OptionalBreak / ToolSurface              / tool tier only
Environment -> Environment / OptionalBreak / EnvironmentDevice        / all costs 0
Explosive   -> Explosive   / OptionalBreak / ExplosiveRewardPreview   / fuel only, preview true
Hidden      -> None        / Hidden        / HiddenCrack/Light/Sound  / clue difficulty only
```

모든 region은 mandatory 쪽에서 perceptible한 clue exact 1개를 가진다. attachment→mandatory base mask는 closed를 유지한다. concrete edge signature, microchunk socket, generated edge/CSV, reward tier/item, return policy/device, inactive buffer, validator, overlay를 만들지 않는다. Authoring CSV/meta는 수정하지 않는다.

Required actual gates:

```text
OptionalAccessRuleAssignerTests >=250 PASS
Type0RouteMaskAssignerTests 257/257 PASS
MAP06 prior combined selection 630/630 PASS
Existing MAP05 aggregate 1959/1959 PASS
Actually executed total >=3096 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3274 -> 3283
new C#/meta 9/9
existing boundary test C# modified <=10
Authoring CSV/meta 50/50 and manifest unchanged
duplicate GUID groups 0
forbidden production/CSV/Scene/Prefab/asmdef changes 0
generated CSV 0
```

전부 PASS일 때만 MAP06_05 COMPLETE/Current Task NONE으로 finalize한다. `MAP06_06_CALCULATE_OPTIONAL_REWARD_TIER`는 LOCKED로 유지하고 자동 시작하지 않는다.

