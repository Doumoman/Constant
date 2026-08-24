TASK: MAP08_04_FILTER_MANDATORY_BOUNDARIES
STATUS: PASS
MAP08_04: COMPLETE ELIGIBLE
MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT: LOCKED / DO NOT START

## Patch and prior-gate evidence

The MAP08_04 patch manifest and every declared precondition matched the
finalized MAP08_03 baseline. The payload Master, Status, and Task files were
copied exactly, their destination hashes matched the manifest, and the patch
receipt was created before implementation began.

```text
Prior MAP08_03 Result SHA-256:
43a6d29466996164af4cc8e2d09dd6478a013f95c0b40ad15f132b3bead01445
Prior MAP08_03 Task SHA-256:
1b5efb56faec6a0ea0e5fa6d751efc241f5474bf4403338fbb365b15f69b4b63
Prior MAP08_03 patch receipt SHA-256:
cf65fbb4444f4a08d67129b185f2c567cc767bc5413500edcf2e5f2f5fd60a26
Applied MAP08_04 receipt SHA-256:
11ef33b9315b643f470229dc23547dda3cd5233d4ada73347024bc255bfea3d9
MAP08_04 Task SHA-256:
9e3992c4bcf5ff7f891e236bfddeb00f746c6fee3effd2fb8a18b18da08781b9

Applied destination hashes:
Master  fe876cbb7181380279aabe261dce457a203beb0348f9b22c4e847d87b9807ca5
Status  77bae73c2c817ca142c7b29b9231f610174a425c2ad2ce411407a8cbcb414a08
Task    9e3992c4bcf5ff7f891e236bfddeb00f746c6fee3effd2fb8a18b18da08781b9
```

## Runtime-only mandatory filter contract

`MoonpalaceBoundaryToolRequirement` is an immutable, ordinal value contract.
Its parser accepts exactly:

```text
NONE
Pickaxe
Rope
Bomb
KeyItem
```

Null, empty, whitespace, padded, case-variant, and unknown tokens fail strict
parsing. `MoonpalaceBoundaryCandidateDefinition` now exposes the typed tool
requirement while its existing string constructor delegates to the same strict
parser for source compatibility. Candidate signatures serialize the canonical
tool token.

`MoonpalaceMandatoryBoundaryFilterRequest` carries exactly the existing resolve
request, candidate index, and mandatory-route-boundary flag. The filter derives
the exact MAP08_02 candidate key without invoking MAP08_03 selection.

For a non-mandatory request, every exact-key candidate passes through in index
order regardless of tool requirement or mandatory-route allowance. For a
mandatory request, a candidate is accepted only when both conditions hold:

```text
mandatory_route_allowed = true
tool_requirement        = NONE
```

The result reports original, accepted, and rejected counts; the immutable
accepted list; per-reason rejection counts; terminal issues; and an exact-key
temporary candidate index suitable for the existing resolver. It preserves the
candidate instances, weights, key, route role, orientation, edge signature,
request direction inputs, and deterministic index order.

Candidate rejection uses the documented stable priority:

```text
MandatoryRouteNotAllowed > ToolRequired
```

Therefore a candidate failing both requirements is counted once as
`MandatoryRouteNotAllowed`. If every candidate is removed, the result returns
`NoCandidatesAfterFilter`, preserves the original/rejected counts, and exposes
no temporary index. Null or structurally invalid requests return
`InvalidRequest`.

The filter never resolves a winner. Tests retain two accepted candidates in
the filter result, then separately pass the temporary index to
`MoonpalaceBoundaryChunkResolver` to prove that final weighted selection remains
owned by MAP08_03. Warning marker sufficiency, content authoring, generated CSV,
and sector assembly were not implemented.

## Unity verification

Unity instance: `Constant@ced6e0dfc4a31d45`
Unity version: `6000.3.8f1`
Project root: `C:/Users/user/Documents/GitHub/Optimal-Selection/Constant`
Mode: EditMode

```text
MoonpalaceMandatoryBoundaryFilterTests:      320 / 320
MoonpalaceBoundaryToolRequirementTests:      200 / 200
MoonpalaceBoundaryChunkResolverTests:        420 / 420
MoonpalaceBoundaryTransformPolicyTests:      260 / 260
MoonpalaceBoundaryCandidateIndexTests:       360 / 360
MoonpalaceBoundaryCandidateKeyTests:         220 / 220
MoonpalaceBiomePairCatalogTests:             220 / 220
MoonpalaceBiomePairContractTests:            180 / 180
MAP07 required regression:                 5,422 / 5,422
MAP06 required regression:                 2,746 / 2,746
MAP05 required regression:                 1,959 / 1,959
-------------------------------------------------------
Required total:                           12,307 / 12,307
Failures:                                      0
Skipped:                                       0
```

The first scripts-only compile request observed the modified existing candidate
file before Unity imported the new tool-requirement source, producing two
transient missing-type diagnostics. A full AssetDatabase refresh imported all
new sources and the authoritative compile completed with zero errors before any
required tests ran. An exploratory MAP06 namespace group selected zero tests
and was excluded; the exact eleven MAP06 fixture groups then passed 2,706/2,706
and the drawer group passed 40/40. During MAP05 polling the MCP transport
reconnected; the original Unity job was recovered intact as PASS 1,933/1,933,
then the drawer tests passed 26/26.

```text
Final compile errors:       0
Final Console errors:       0
Final Console warnings:     0
Relevant warnings:          0
```

The final Console gate was taken after recording and clearing the transient
test-runner and MCP reconnect diagnostics, then re-reading errors and warnings
as 0/0.

## Static and ownership gates

```text
Assets meta:                                      3439 -> 3447
New Runtime production C# / matching meta:             6 / 6
New Runtime test C# / matching meta:                   2 / 2
New Runtime folder meta:                                   0
New Editor production C# / matching meta:              0 / 0
New Editor test C# / matching meta:                    0 / 0
Existing MAP08 boundary production/test C# modified:       1 <= 16
Matching existing boundary production/test meta modified: 0
Task-local existing boundary test C# modified:             0 <= 26
Matching existing boundary-test meta modified:             0
Assets duplicate GUID groups:                              0

Authoring CSV / matching meta:                          50 / 50
Authoring manifest SHA-256:
4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Authoring CSV tracked changes:                               0
Generated CSV files created:                                 0

Scene / Prefab tracked changes:                            0 / 0
ProjectSettings / Packages tracked changes:                0 / 0
asmdef / asmref tracked changes:                           0 / 0
MAP08_05+ / MAP09+ forbidden production symbol hits:       0 / 0
Unapplied MCP patches:                                       0
```

The Authoring manifest was recomputed from the sorted 50-file inventory as
`relative/path|file_sha256` LF-separated records and exactly matched the
approved baseline. No ScriptableObject, Scene, Prefab, tilemap, Authoring CSV,
generated CSV, ProjectSettings, Packages, asmdef, or asmref asset was created
or modified.

`Constant.slnx` and the already-applied untracked
`MAP07_13_FINALIZE_MAP07_EXIT_APPROVED` inbox directory were pre-existing
unrelated worktree items. They were not read as implementation inputs, changed,
or included in this Task's ownership set.

## Completion decision

MAP08_04 satisfies its exact typed tool-requirement, mandatory/non-mandatory
filter, deterministic rejection accounting, no-candidate failure, resolver
ownership boundary, Unity test, and static ownership gates. It is eligible to
finalize as `COMPLETE`.
`MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT` remains
`LOCKED / DO NOT START`.
