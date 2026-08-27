# RUN MAP06_09

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR.md`, MAP06_08 PASS Result를 순서대로 읽어라.

Exact gates:

```text
MAP06_08 Result STATUS: PASS
MAP06_08 Result SHA-256: 43dd272802bfe6094ac5f1dff91ddb30229acf0c5a0885742509945a496bf58b
MAP06_08 Task SHA-256: 0e45ed924cd515ca497abca85e0ede2a6efddefa9648c72c21b0d00a93647340
MAP06_09 Task SHA-256: e5f430c29dcba4344feb1ba12fff73fc9052c3f3a386d672a7e8a3b016a2c97e
```

Current Task가 MAP06_09이 아니거나 어느 SHA라도 다르면 `BLOCKED`하고 변경하지 마. MAP06_10 이후 Task body는 읽거나 시작하지 마.

이번 Task는 optional region validation report까지만 구현한다.

Allowed new writes:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionValidationEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionValidationSettings.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionValidationIssue.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionValidationDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionValidationReport.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionValidator.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionValidatorTests.cs
MapDesign/MCP/REPORTS/MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR_RESULT.md
```

Phase-boundary advance를 위해 Current Task의 exact up-to-14 existing test allowlist만 수정할 수 있다. matching existing meta는 수정하지 마.

Approved source-chain:

```text
world / dimensions = 169 / 13x13
mandatory graph nodes/directed/undirected/route cells = 47/96/48/47
optional regions / Type0 cells = 12/39
access assignments / clues / perceptible clues = 12/12/12
reward-tier assignments = 12
return assignments / returnable / non-returnable = 12/39/0
inactive assignments / DecorativeBoundary / InteriorInactive = 78/52/26
approved Site-Mandatory adapter overlap = 0,28,106
protected union = 91
full accounting = 169 = 8 + 44 + 39 + 78
Type0 digest = a26e73f25ff7267760a2507ec55b1acda3a6c39a8f3698fc37b190620ceca525
growth digest = 1f00f718bdb8d79fbf88923be0d81e0075987267c024cc624086ee6194998caa
access digest = 5268b7ed2342f197fb5717c85dbfdb8e51b3c520c217ecf93ad14dc944ecf74f
reward digest = c3430c42a27937e143fa89c5839282b9533b62d5fb74fb26fdad490cb545958e
return digest = cff0556a59e66fcc16b886ecf3082779efe9535bb79dcf45b401d12ff0971f6b
inactive digest = 426f269e39d8a2d75a93020a00c7bb617612c00dd60a663fdbeffc60f8ea9578
Type4 = U+D mandatory, L/R independent, UD/LUD/RUD/LRUD legal
```

Required actual gates:

```text
OptionalRegionValidatorTests >=320 PASS
InactiveBufferAssignerTests 281/281 PASS
OptionalReturnPolicyResolverTests 289/289 PASS
OptionalRewardTierCalculatorTests 279/279 PASS
OptionalAccessRuleAssignerTests 289/289 PASS
Type0RouteMaskAssignerTests 257/257 PASS
MAP06 prior combined selection 630/630 PASS
Existing MAP05 aggregate 1959/1959 PASS
Actually executed total >=4304 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3304 -> 3311
new C#/meta 7/7
existing boundary test C# modified <=14
Authoring CSV/meta 50/50 and manifest unchanged
duplicate GUID groups 0
forbidden production/CSV/Scene/Prefab/asmdef changes 0
generated CSV 0
boundary/recipe/microchunk/tile/socket/edge/overlay artifacts 0
```

전부 PASS일 때만 MAP06_09 COMPLETE/Current Task NONE으로 finalize한다. `MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS`는 LOCKED로 유지하고 자동 시작하지 않는다.
