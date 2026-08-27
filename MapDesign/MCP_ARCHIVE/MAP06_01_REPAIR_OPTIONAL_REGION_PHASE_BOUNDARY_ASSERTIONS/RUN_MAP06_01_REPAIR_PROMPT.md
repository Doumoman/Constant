# RUN MAP06_01 REPAIR

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS.md`, 현재 MAP06_01 FAIL Result를 순서대로 읽어라.

Phase A precondition:

```text
Current Task = TASKS/MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS.md
Current Task SHA-256 = 79b806802dab4a86f3cdc0b6193be4c8f5c97a2e6a9cc8bcc023259752b49a62
Current Result = MapDesign/MCP/REPORTS/MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS_RESULT.md
Current Result STATUS = FAIL
Current Result SHA-256 = 254092c80abdec87d20c9276854539ca7225e33738dfbe2419384a48710fb553
```

값이 다르면 `BLOCKED`하고 변경하지 마. MAP06_02 이후 Task body는 읽거나 시작하지 마.

Repair only the six existing boundary tests that failed:

```text
HorizontalBackboneRouterTests
MandatoryRouteGraphValidatorTests
MandatoryRouteMaskLookupBuilderTests
Map05ExitTests
UpDownConflictResolverTests
VerticalGatewayPlannerTests
```

MAP06_01 allowed symbols:

```text
OptionalRegionId
OptionalRegionEnums
OptionalRegionAccessRule
OptionalRewardTier
OptionalReturnPolicy
OptionalRegionDepth
OptionalRegionAttachment
OptionalRegionCell
OptionalRegion
OptionalRegionSnapshot
OptionalRegionTokenCodec
```

Keep MAP06_02+ symbols forbidden:

```text
OptionalAttachmentEnumerator
OptionalRegionGrower
Type0RouteMaskAssigner
OptionalAccessRuleAssigner
OptionalRewardTierCalculator
OptionalReturnPolicyResolver
InactiveBufferAssigner
OptionalRegionValidator
OptionalRegionOverlay
```

Do not modify optional region production model files, OptionalRegionModelsTests, MAP05 production graph/CSV/SectorCell, Authoring CSV, generated CSV, asmdef, Scene, Prefab, Packages, ProjectSettings, Master, or Status.

Required actual gates:

```text
OptionalRegionModelsTests 194/194 PASS
Existing MAP05 aggregate 1959/1959 PASS
Actually executed total 2153/2153 PASS or higher if repair adds tests
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3254
modified existing test C# 6
modified existing test meta 0
Authoring CSV/meta 50/50
duplicate GUID groups 0
```

전부 PASS일 때만 MAP06_01 COMPLETE/Current Task NONE으로 finalize한다. `MAP06_02_ENUMERATE_OPTIONAL_ATTACHMENTS`는 LOCKED로 유지하고 자동 시작하지 않는다.
