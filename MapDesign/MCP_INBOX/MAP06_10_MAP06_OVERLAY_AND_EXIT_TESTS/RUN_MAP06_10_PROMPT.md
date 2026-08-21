# RUN MAP06_10

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS.md`, MAP06_09 PASS Result를 순서대로 읽어라.

Exact gates:

```text
MAP06_09 Result STATUS: PASS
MAP06_09 Result SHA-256: 51a6f0dd621db698628ceef6ba7e7f2f18988b213ad564e7b35e00c52041d62a
MAP06_09 Task SHA-256: e5f430c29dcba4344feb1ba12fff73fc9052c3f3a386d672a7e8a3b016a2c97e
MAP06_10 Task SHA-256: 205ce60e1e591036a80bc7dc10a939ea95d0237d09babe106e86c09b78e70605
```

Current Task가 MAP06_10이 아니거나 어느 SHA라도 다르면 `BLOCKED`하고 변경하지 마. MAP07 이후 Task body는 읽거나 시작하지 마.

이번 Task는 optional region overlay snapshot, editor scene drawer command model, MAP06 phase exit tests까지만 구현한다.

Allowed new writes:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/OptionalRegionOverlayEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/OptionalRegionOverlaySettings.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/OptionalRegionOverlayCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/OptionalRegionOverlayConnection.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/OptionalRegionOverlayLegendEntry.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/OptionalRegionOverlaySnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/OptionalRegionOverlayBuilder.cs
Assets/_Game/Editor/MapAuthoring/Preview/OptionalRegionOverlaySceneDrawer.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Diagnostics/OptionalRegionOverlayTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Preview/OptionalRegionOverlaySceneDrawerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map06ExitTests.cs
MapDesign/MCP/REPORTS/MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS_RESULT.md
```

Phase-boundary advance를 위해 Current Task의 exact up-to-15 existing test allowlist만 수정할 수 있다. matching existing meta는 수정하지 마.

Approved overlay facts:

```text
overlay cells = 169
exclusive Mandatory/ReservedSite/Type0/InactiveInterior/InactiveDecorative = 44/8/39/26/52
attachment contact connections = 12
return witness connections = 19
validation status / issues = Valid / 0
approved adapter overlap = 0,28,106
validation digest = 1180f6a784b29739a2ca640d2c45398066ec7e636a8cb69ee307315cc20cc84e
inactive digest = 426f269e39d8a2d75a93020a00c7bb617612c00dd60a663fdbeffc60f8ea9578
return digest = cff0556a59e66fcc16b886ecf3082779efe9535bb79dcf45b401d12ff0971f6b
Type0 digest = a26e73f25ff7267760a2507ec55b1acda3a6c39a8f3698fc37b190620ceca525
Type4 = U+D mandatory, L/R independent, UD/LUD/RUD/LRUD legal
```

Required actual gates:

```text
OptionalRegionOverlayTests >=180 PASS
OptionalRegionOverlaySceneDrawerTests >=40 PASS
Map06ExitTests >=180 PASS
OptionalRegionValidatorTests 321/321 PASS
InactiveBufferAssignerTests 281/281 PASS
OptionalReturnPolicyResolverTests 289/289 PASS
OptionalRewardTierCalculatorTests 279/279 PASS
OptionalAccessRuleAssignerTests 289/289 PASS
Type0RouteMaskAssignerTests 257/257 PASS
OptionalRegionGrowerTests 234/234 PASS
OptionalAttachmentEnumeratorTests 202/202 PASS
OptionalRegionModelsTests 194/194 PASS
Existing MAP05 aggregate 1959/1959 PASS
Actually executed total >=4705 PASS
Visual checklist Game/Scene >=24/24 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3311 -> 3322
new C#/meta 11/11
existing boundary test C# modified <=15
Authoring CSV/meta 50/50 and manifest unchanged
duplicate GUID groups 0
forbidden production/CSV/Scene/Prefab/asmdef changes 0
generated CSV 0
boundary/recipe/microchunk/tile/socket/edge artifacts 0
```

전부 PASS일 때만 MAP06_10 COMPLETE/Current Task NONE 및 `MAP06 PHASE EXIT: APPROVED`로 finalize한다. `MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION`는 LOCKED로 유지하고 자동 시작하지 않는다.
