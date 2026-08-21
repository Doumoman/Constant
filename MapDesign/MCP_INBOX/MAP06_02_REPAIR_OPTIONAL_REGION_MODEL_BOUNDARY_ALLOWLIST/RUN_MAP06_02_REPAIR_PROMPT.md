# RUN MAP06_02 REPAIR

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP06_02_ENUMERATE_OPTIONAL_ATTACHMENTS.md`, 현재 MAP06_02 FAIL Result를 순서대로 읽어라.

Phase A precondition:

```text
Current Task = TASKS/MAP06_02_ENUMERATE_OPTIONAL_ATTACHMENTS.md
Current Task SHA-256 = d1f39196c3897f54611185eb0ccd95d64e60ed60c1a4b96e03671c799e2f68f0
Current Result = MapDesign/MCP/REPORTS/MAP06_02_ENUMERATE_OPTIONAL_ATTACHMENTS_RESULT.md
Current Result STATUS = FAIL
Current Result SHA-256 = a5c93b16d551ce999aebea014d37fc1ac0bbb0e6fea1c790ae97eb83175ee3c2
```

값이 다르면 `BLOCKED`하고 변경하지 마. MAP06_03 이후 Task body는 읽거나 시작하지 마.

Repair only this unresolved boundary failure:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionModelsTests.cs
Failure: RuntimeSurfaceHasNoMutableStaticUnityEditorOrMap06_02PlusSymbols
Rejected present symbol that must now be allowed: OptionalAttachmentEnumerator
```

Preserve these already implemented MAP06_02 files and candidate output:

```text
OptionalAttachmentCandidateId.cs
OptionalAttachmentCandidate.cs
OptionalAttachmentEnumerationSettings.cs
OptionalAttachmentEnumerationDiagnostics.cs
OptionalAttachmentEnumerationResult.cs
OptionalAttachmentEnumerator.cs
OptionalAttachmentEnumeratorTests.cs
AcceptedCount: 51
CanonicalDigest: 68b438c523645c2f6721fa0c104c3cd4c282076292cd2e035cd20a2b272aaee6
```

MAP06_02 allowed symbols now include:

```text
OptionalAttachmentCandidateId
OptionalAttachmentCandidate
OptionalAttachmentEnumerationSettings
OptionalAttachmentEnumerationDiagnostics
OptionalAttachmentEnumerationResult
OptionalAttachmentEnumerator
OptionalAttachmentEnumeratorTests
```

Keep MAP06_03+ future symbols forbidden:

```text
OptionalRegionGrower
Type0RouteMaskAssigner
OptionalAccessRuleAssigner
OptionalRewardTierCalculator
OptionalReturnPolicyResolver
InactiveBufferAssigner
OptionalRegionValidator
OptionalRegionOverlay
GeneratedOptionalRegionCsvWriter
```

Do not modify production implementation, MAP05 production graph/CSV/SectorCell, Authoring CSV, generated CSV, asmdef, Scene, Prefab, Packages, ProjectSettings, Master, or Status.

Required actual gates after repair:

```text
OptionalAttachmentEnumeratorTests 202/202 PASS
OptionalRegionModelsTests 194/194 PASS
Existing MAP05 aggregate 1959/1959 PASS
Actually executed required total >=2355 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3261
new C#/meta preserved 7/7
modified existing boundary test C# total <=7
repair-only additional existing test C# modification: OptionalRegionModelsTests.cs
Authoring CSV/meta 50/50
duplicate GUID groups 0
production graph/CSV/SectorCell/asmdef/Scene/Prefab/Packages/ProjectSettings modifications 0
generated CSV files created by this repair 0
```

전부 PASS일 때만 MAP06_02 COMPLETE/Current Task NONE으로 finalize한다. `MAP06_03_IMPLEMENT_OPTIONAL_REGION_GROWER`는 LOCKED로 유지하고 자동 시작하지 않는다.
