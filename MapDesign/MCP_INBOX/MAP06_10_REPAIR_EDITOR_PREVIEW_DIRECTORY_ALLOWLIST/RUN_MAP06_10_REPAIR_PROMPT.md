# RUN MAP06_10 REPAIR

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS.md`, 현재 MAP06_10 BLOCKED Result, MAP06_09 PASS Result를 순서대로 읽어라.

Phase A precondition:

```text
Current Task = TASKS/MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS.md
Current Task SHA-256 before repair apply = 205ce60e1e591036a80bc7dc10a939ea95d0237d09babe106e86c09b78e70605
Revised Task SHA-256 after repair apply = 623da5aaf2f8c72dd830fb5f859c4b05a631a93b7f4fa2a3aa67adc823f95cdb
Current Result = MapDesign/MCP/REPORTS/MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS_RESULT.md
Current Result STATUS = BLOCKED
Current Result SHA-256 = d02204b7515e4818052f6e5e8dad0fc0740803f3af5f0753f652b5c715e3119e
Prior MAP06_09 Result STATUS = PASS
Prior MAP06_09 Result SHA-256 = 51a6f0dd621db698628ceef6ba7e7f2f18988b213ad564e7b35e00c52041d62a
Prior MAP06_09 Task SHA-256 = e5f430c29dcba4344feb1ba12fff73fc9052c3f3a386d672a7e8a3b016a2c97e
```

값이 다르면 `BLOCKED`하고 변경하지 마. MAP07 이후 Task body는 읽거나 시작하지 마.

Repair only this allowlist contradiction:

```text
Blocked v1.0 target:
  Assets/_Game/Editor/MapAuthoring/Preview/OptionalRegionOverlaySceneDrawer.cs

Blocked v1.0 problem:
  Assets/_Game/Editor/MapAuthoring/Preview/ directory absent
  Assets/_Game/Editor/MapAuthoring/Preview.meta absent
  Assets/_Game/Editor/MapAuthoring/Preview/MandatoryRouteOverlaySceneDrawer.cs absent
  v1.0 forbade new directory/folder meta
  v1.0 required Assets meta 3311 -> 3322

Repaired v1.1 contract:
  canonical drawer target remains unchanged
  create Assets/_Game/Editor/MapAuthoring/Preview/
  create Assets/_Game/Editor/MapAuthoring/Preview.meta
  no predecessor production drawer is required
  Assets meta 3311 -> 3323
```

Allowed new writes after repair:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/OptionalRegionOverlayEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/OptionalRegionOverlaySettings.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/OptionalRegionOverlayCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/OptionalRegionOverlayConnection.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/OptionalRegionOverlayLegendEntry.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/OptionalRegionOverlaySnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/OptionalRegionOverlayBuilder.cs
Assets/_Game/Editor/MapAuthoring/Preview/
Assets/_Game/Editor/MapAuthoring/Preview.meta
Assets/_Game/Editor/MapAuthoring/Preview/OptionalRegionOverlaySceneDrawer.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Diagnostics/OptionalRegionOverlayTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Preview/OptionalRegionOverlaySceneDrawerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map06ExitTests.cs
MapDesign/MCP/REPORTS/MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS_RESULT.md
```

Phase-boundary advance를 위해 Current Task의 exact up-to-15 existing test allowlist만 수정할 수 있다. matching existing meta는 수정하지 마.

Required actual gates after repair:

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
Actually executed required total >=4705 PASS
Visual checklist Game/Scene >=24/24 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3311 -> 3323
new C#/meta 11/11
new Editor preview folder meta 1/1
other new directory/folder meta 0
existing boundary test C# modified <=15
Authoring CSV/meta 50/50 and manifest unchanged
duplicate GUID groups 0
forbidden production/CSV/Scene/Prefab/asmdef changes 0
generated CSV 0
boundary/recipe/microchunk/tile/socket/edge artifacts 0
```

전부 PASS일 때만 MAP06_10 COMPLETE/Current Task NONE 및 `MAP06 PHASE EXIT: APPROVED`로 finalize한다. `MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION`는 LOCKED로 유지하고 자동 시작하지 않는다.
