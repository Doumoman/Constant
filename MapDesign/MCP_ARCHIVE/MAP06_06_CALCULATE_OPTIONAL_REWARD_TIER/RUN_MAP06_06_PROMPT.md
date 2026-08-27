# RUN MAP06_06

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP06_06_CALCULATE_OPTIONAL_REWARD_TIER.md`, MAP06_05 PASS Result를 순서대로 읽어라.

Exact gates:

```text
MAP06_05 Result STATUS: PASS
MAP06_05 Result SHA-256: 0f8d8ba09d8c6f36cd75a8bdcdc808eb00bcc1d63031981425a580a64d481630
MAP06_05 Task SHA-256: d80cf04261811777b65b6c99ca8b7ae368fc39f4a895d024c6639ada5226c587
MAP06_06 Task SHA-256: 8c8dd6a780b334edf7fb8c1276c1cc5d64332bf26f8c5ab9b69e9dabcb22a542
```

Current Task가 MAP06_06이 아니거나 어느 SHA라도 다르면 `BLOCKED`하고 변경하지 마. MAP06_07 이후 Task body는 읽거나 시작하지 마.

이번 Task는 Type0/access source-chain 위에 logical reward score와 tier reservation까지만 구현한다.

Allowed new writes:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRewardTierCalculationEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRewardTierSettings.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRewardTierAssignment.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRewardTierDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRewardTierResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRewardTierCalculator.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRewardTierCalculatorTests.cs
MapDesign/MCP/REPORTS/MAP06_06_CALCULATE_OPTIONAL_REWARD_TIER_RESULT.md
```

Phase-boundary advance를 위해 Current Task의 exact up-to-11 existing test allowlist만 수정할 수 있다. matching existing meta는 수정하지 마.

Approved inputs/settings:

```text
Type0/access/growth digests:
a26e73f25ff7267760a2507ec55b1acda3a6c39a8f3698fc37b190620ceca525
5268b7ed2342f197fb5717c85dbfdb8e51b3c520c217ecf93ad14dc944ecf74f
1f00f718bdb8d79fbf88923be0d81e0075987267c024cc624086ee6194998caa
regions/cells/Type0 assignments = 12/39/39
access assignments/clues/perceptible = 12/12/12
attachment base-closed / mandatory base-open = 12/0
DepthWeight = 2
ExplosiveFuelDivisor = 10
TierMinimumScores = 0/4/8/12
Type4 = U+D mandatory, L/R independent, UD/LUD/RUD/LRUD legal
```

Exact formula:

```text
DepthScore = MaxDepth * 2
ToolCostScore = ToolCostTier
ExplosiveFuelScore = ExplosiveFuelCost / 10
HiddenClueScore = HiddenClueDifficulty
RewardScore = DepthScore + ToolCostScore + ExplosiveFuelScore + HiddenClueScore
Tier = existing Low/Medium/High/Unique at highest threshold met among 0/4/8/12; None forbidden
```

source access matrix의 unused cost는 반드시 0이고 모든 clue/preview/attachment identity를 보존한다. actual reward ID/item/pool/quantity/slot/spawn, mandatory/core/unique reward, return policy/device, inactive buffer, validator, overlay, generated CSV를 만들지 않는다. Authoring CSV/meta는 수정하지 않는다.

Required actual gates:

```text
OptionalRewardTierCalculatorTests >=260 PASS
OptionalAccessRuleAssignerTests 289/289 PASS
Type0RouteMaskAssignerTests 257/257 PASS
MAP06 prior combined selection 630/630 PASS
Existing MAP05 aggregate 1959/1959 PASS
Actually executed total >=3395 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3283 -> 3290
new C#/meta 7/7
existing boundary test C# modified <=11
Authoring CSV/meta 50/50 and manifest unchanged
duplicate GUID groups 0
forbidden production/CSV/Scene/Prefab/asmdef changes 0
generated CSV 0
actual reward selection 0
```

전부 PASS일 때만 MAP06_06 COMPLETE/Current Task NONE으로 finalize한다. `MAP06_07_IMPLEMENT_RETURN_POLICY`는 LOCKED로 유지하고 자동 시작하지 않는다.




