TASK: MAP06_08_ASSIGN_INACTIVE_BUFFERS
STATUS: PASS
MAP06_08: COMPLETE ELIGIBLE
MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR: LOCKED / DO NOT START

## Contract And Repair Gates

- Applied repair patch: `MAP06_08_REPAIR_RESERVED_ADAPTER_ACCOUNTING / 1.1`
- Repair manifest SHA-256: `a8dd9f6f20fd07c7dfec145d987b9a3e4b9da4fdb294008c5ae4091b7dced2ac`
- Repair receipt: `MCP_INBOX/MAP06_08_REPAIR_RESERVED_ADAPTER_ACCOUNTING/.APPLIED`
- Repair receipt SHA-256: `52f67c10e85faa1859bfc33fc60114a029ca75c237a71abd643becf333f5a005`
- Blocked Task SHA-256 before repair: `778d5beb1944ddd01e4541254f6d63d55ce255c3eaeab0f79143ee4de2de9ec7`
- Revised current Task SHA-256: `0e45ed924cd515ca497abca85e0ede2a6efddefa9648c72c21b0d00a93647340`
- Replaced BLOCKED Result SHA-256: `759de495f3e2608fba844e5cca5ab3c6d7cd0479a73c8a3928c1ac4b964045fa`
- Prior MAP06_07 PASS Result SHA-256: `2815e6b35df71be1477812594435ed4793c3c9a03c60f1ef602267e4a2e12329`
- Original MAP06_08 patch manifest/receipt SHA-256: `a2c377e051f0c0f01b3c97ec70a9cf65dcf9adf3e1f0628f2f918eda90e0ad56` / `f22ab3d6346e3ad825d2d93bf233902dbbd9e9336d843cecf5c6d906aedd5fec`
- Preconditions: status matrix `75 COMPLETE / 1 CURRENT / 129 LOCKED`; MAP06_07 COMPLETE; MAP06_08 sole CURRENT; MAP06_09 and MAP06_10 LOCKED.

## Implementation

The immutable P00/P01/P02/MAP05/MAP06_04~07 source chain is validated before publication. Source membership and exclusive projected ownership are separate:

- Site footprint source: `8`
- Mandatory graph source: `47`
- Type0 source: `39`
- Approved Site ∩ Mandatory overlap: exact `{0,28,106}`
- Site ∩ Type0 / Mandatory ∩ Type0: `0 / 0`
- Approved overlap markers: `3 / 3`
- Exclusive ReservedSite / MandatoryOnly / Type0: `8 / 44 / 39`
- Protected union: `91`
- InactiveBuffer assignments: `78`
- Full-world accounting: `169 = 8 + 44 + 39 + 78`

Approved site+mandatory adapters remain ReservedSite in the exclusive projection while retaining mandatory source membership. They are not duplicate or illegal ownership. Approved reserved-adapter outward sockets are validated by the mandatory source-chain and excluded from ordinary mandatory-to-inactive route-opening rejection; all ordinary mandatory openings and all Type0 base openings retain atomic `OpenEdgeToInactive` rejection.

Every unprotected sector receives exactly one immutable `GeneratedSectorRole.InactiveBuffer` assignment. The logical inactive kind is `DecorativeBoundary` iff a protected cardinal neighbor exists, otherwise `InteriorInactive`. No new final sector role or authored boundary/recipe/socket/edge artifact was introduced.

Changed after the repair receipt, exact four:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/InactiveBufferAssignmentDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/InactiveBufferAssignmentResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/InactiveBufferAssigner.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/InactiveBufferAssignerTests.cs
```

The other three existing MAP06_08 C# files and all seven matching metas were preserved. Eight inherited allowlisted phase-boundary test edits from the original attempt were unchanged by the repair and remain within the `<=13` limit.

## Source Chain And Settings

```text
World sectors / dimensions: 169 / 13x13
Site reservations / reserved sectors / entries / Core seeds: 7 / 8 / 6 / 4
Biome publication sectors / assigned / reserved-unassigned: 169 / 165 / 4
Mandatory graph nodes / directed / undirected / route cells: 47 / 96 / 48 / 47
Optional regions / Type0 cells: 12 / 39
Return assignments / returnable / non-returnable: 12 / 39 / 0
Mandatory graph digest: MAP05_GRAPH_47_96_48_47
Type0 assignment digest: a26e73f25ff7267760a2507ec55b1acda3a6c39a8f3698fc37b190620ceca525
Growth digest: 1f00f718bdb8d79fbf88923be0d81e0075987267c024cc624086ee6194998caa
Access digest: 5268b7ed2342f197fb5717c85dbfdb8e51b3c520c217ecf93ad14dc944ecf74f
Reward digest: c3430c42a27937e143fa89c5839282b9533b62d5fb74fb26fdad490cb545958e
Return-policy digest: cff0556a59e66fcc16b886ecf3082779efe9535bb79dcf45b401d12ff0971f6b
RequireFullWorldAccounting: true
RequireClosedInactiveBoundaries: true
ClassifyClaimAdjacentAsDecorativeBoundary: true
Attachment base-closed / mandatory base-open: 12 / 0
```

MAP05 Type4 `U+D` and independent `L/R`, MAP06_04 Type0 base-closed/`L+R`, MAP06_05 access/clue, MAP06_06 score/tier, and MAP06_07 returnability assertions all passed unchanged.

## Exact Fixture Publication

```text
WorldSectorCount: 169
SiteReservationCount: 7
ReservedSiteSectorCount: 8
MandatoryRouteCellCount: 47
MandatoryExclusiveSectorCount: 44
Type0CellCount: 39
SiteMandatoryOverlapCount: 3
ApprovedReservedAdapterOverlapCount: 3
ProtectedUnionCount: 91
AssignmentCount: 78
DecorativeBoundaryCount: 52
InteriorInactiveCount: 26
WorldEdgeInactiveCount: 19
ProtectedToInactiveCardinalEdgeCount: 112
InactiveToInactiveUndirectedEdgeCount: 90
UnassignedSectorCount: 0
IllegalOwnershipOverlapCount: 0
DuplicateSectorCount: 0
OpenEdgeToInactiveCount: 0
RngDrawCount: 0
SourceMutationCount: 0
Partial publication on failure: 0
Canonical assignment digest: 426f269e39d8a2d75a93020a00c7bb617612c00dd60a663fdbeffc60f8ea9578
```

Digest-backed canonical classification table:

```text
DecorativeBoundary (52):
10,14,15,16,17,18,19,21,23,37,39,41,43,45,49,54,58,60,63,67,71,75,80,82,84,86,
88,91,93,95,96,97,99,100,113,118,122,123,125,131,136,138,144,149,151,152,153,154,
155,158,161,162

InteriorInactive (26):
11,12,24,25,38,50,51,64,76,77,89,90,101,102,103,114,115,116,126,127,128,129,
139,140,141,142
```

The focused summary job emitted all 78 ordered assignments with coordinate, role, kind, protected/inactive neighbor lists, and world-edge flag. Independent test oracles verified the exact split, cardinal lists, topology counters, canonical order, culture/order/service-reuse determinism, immutable collections, and source identity.

Atomic invalid-input coverage verifies empty assignments, empty digest, zero RNG/source mutation/partial publication, and stable `OpenEdgeToInactive` errors when a Type0 opening is redirected to an inactive sector.

## Required Unity EditMode Gates

All acceptance jobs used the connected Unity Test Runner on Unity `6000.3.8f1`.

```text
3979fceb9d7943df8ad89181c543fe77  InactiveBufferAssignerTests                     281/281 PASS
cff6810aab454d5c8c960199d4930002  OptionalReturnPolicyResolverTests               289/289 PASS
85480df2df244ee7a6183ce0b0c33967  OptionalRewardTierCalculatorTests               279/279 PASS
1da8b037e10f4b94a36c8d64444f90f6  OptionalAccessRuleAssignerTests                 289/289 PASS
2ebcb7398cfc4778babf71c4df61e6ea  Type0RouteMaskAssignerTests                     257/257 PASS
5d017ebd41e34a4a9f23f336f60f8cca  MAP06 prior combined selection                  630/630 PASS
1ff94f2cf7d5448cadd9d5825d3aae96  MAP05_01..MAP05_11 category aggregate          1832/1832 PASS
6c14feeeee554a4ca9db71480096aeef  MandatoryRouteMaskLookupBuilderTests            127/127 PASS
------------------------------------------------------------------------------------------------
Required actual total                                                    3984/3984 PASS
Failed / skipped: 0 / 0
```

Focused non-aggregate evidence job:

```text
705233d417fc41a3a14ebc6e48f28917  ApprovedFixturePublishesCanonicalInactiveSummary  1/1 PASS
```

Final forced script refresh/compile completed with editor idle and ready. Final Console errors/warnings/relevant warnings: `0 / 0 / 0`.

## Static And Scope Gates

- Assets meta: `3304`
- Existing MAP06_08 C# / matching meta: `7 / 7`
- Duplicate Assets GUID groups: `0`
- Existing boundary test C# modified by original task: `8 <= 13`; repair-time boundary modifications: `0`
- Authoring CSV / matching meta: `50 / 50`; newer than repair receipt: `0`
- Authoring manifest SHA-256 unchanged: `4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3`
- Repair-time Assets/Packages/ProjectSettings change inventory: exact four allowlisted C# files listed above
- Generated CSV files created: `0`
- Boundary profile/recipe/microchunk/tile/socket/edge artifacts created: `0`
- asmdef/asmref/directory/folder meta created: `0`
- Scene, Prefab, Packages, ProjectSettings, MAP05/MAP06_01~07 production, Authoring CSV/meta modifications: `0`

Final MAP06_08 source SHA-256:

```text
InactiveBufferAssignmentEnums.cs        6ac5ae0cbdfbccd465d99d61003da64e783708823dde66e771b9f258e05bf2f9
InactiveBufferAssignmentSettings.cs     8cb76720d07707fc5a2bda3676fd7bc984547b50a27c447004322d49cb51be14
InactiveBufferAssignment.cs             096886ae23211fa2797c3bdd2dba663906f2978c744af46a6287d8b583a699d5
InactiveBufferAssignmentDiagnostics.cs  09f1bde7f7c8037a4c1efeac30bd9e57e054bb6d471206807b6a4492630a3ff3
InactiveBufferAssignmentResult.cs       e69f4fa1f143e469523ec3d66a84ab7ae926643a51217210290cae12c1d6d6c3
InactiveBufferAssigner.cs               5baf88a2ca78bd8e065468ba847bdfd70a0df268ee0fd65a89bae3689818e999
InactiveBufferAssignerTests.cs          d7837aa92c4201c549952090c86091de2308354c2adc3f14a523431168032aab
```

## NEXT

Finalize only `MAP06_08_ASSIGN_INACTIVE_BUFFERS`. Keep `MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR` LOCKED and do not start it.
