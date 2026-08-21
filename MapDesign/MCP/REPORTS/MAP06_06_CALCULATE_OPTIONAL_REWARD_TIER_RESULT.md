TASK: MAP06_06_CALCULATE_OPTIONAL_REWARD_TIER
STATUS: PASS
MAP06_06: COMPLETE ELIGIBLE
MAP06_07_IMPLEMENT_RETURN_POLICY: LOCKED / DO NOT START

# PATCH / RECEIPT / PRECONDITION GATE

- applied patch: `MCP_INBOX/MAP06_06_CALCULATE_OPTIONAL_REWARD_TIER`
- receipt: `.APPLIED`, `STATUS: APPLIED`, `APPLIED_DATE: 2026-08-19`
- manifest SHA-256: `377e235efbd98d11a9dbdae9a3066ab0db8d7d514b6b2034c140933a62533dc9`
- applied Master SHA-256: `bfe3e1b2681694e1af6e96fa474177d0a1853cf7139905c8ca8da06f22fd51af`
- applied Status SHA-256: `c7033220e7009586227a4cf4f3c7773a13e7e0ce97ea4134f1ffabe914160ca4`
- current Task SHA-256: `8c8dd6a780b334edf7fb8c1276c1cc5d64332bf26f8c5ab9b69e9dabcb22a542`
- prior Result SHA-256: `0f8d8ba09d8c6f36cd75a8bdcdc808eb00bcc1d63031981425a580a64d481630`
- prior Task SHA-256: `d80cf04261811777b65b6c99ca8b7ae368fc39f4a895d024c6639ada5226c587`
- prior Result exact gate: `TASK: MAP06_05_ASSIGN_ACCESS_RULES_AND_CLUES`, `STATUS: PASS`, `MAP06_05: COMPLETE ELIGIBLE`
- status gate: `MAP06_06_CALCULATE_OPTIONAL_REWARD_TIER` is the sole `CURRENT`; `MAP06_07_IMPLEMENT_RETURN_POLICY` remains `LOCKED`.
- unapplied inbox patches after application: `0`.

# CREATED / CHANGED FILES

Created Runtime production C# exact `6`:

- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRewardTierCalculationEnums.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRewardTierSettings.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRewardTierAssignment.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRewardTierDiagnostics.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRewardTierResult.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRewardTierCalculator.cs`

Created EditMode test C# exact `1`:

- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRewardTierCalculatorTests.cs`

Unity generated matching `.cs.meta` exact `7`. Existing phase-boundary test C# modified exact `11 <= 11`:

- `HorizontalBackboneRouterTests.cs`
- `MandatoryRouteGraphValidatorTests.cs`
- `MandatoryRouteMaskLookupBuilderTests.cs`
- `Map05ExitTests.cs`
- `UpDownConflictResolverTests.cs`
- `VerticalGatewayPlannerTests.cs`
- `OptionalRegionModelsTests.cs`
- `OptionalAttachmentEnumeratorTests.cs`
- `OptionalRegionGrowerTests.cs`
- `Type0RouteMaskAssignerTests.cs`
- `OptionalAccessRuleAssignerTests.cs`

The boundary changes remove `OptionalRewardTierCalculator` from negative future-symbol lists while retaining MAP06_07+ guards, including `OptionalReturnPolicyResolver`, `OptionalReturnConnection`, `InactiveBufferAssigner`, `OptionalRegionValidator`, `OptionalRegionOverlay`, and `GeneratedOptionalRegionCsvWriter`.

# SCORE / TIER CONTRACT

Approved immutable settings:

```text
DepthWeight = 2
ExplosiveFuelDivisor = 10
TierMinimumScores = 0 / 4 / 8 / 12
```

Implemented exact checked integer formula:

```text
DepthScore         = MaxDepth * 2
ToolCostScore      = ToolCostTier
ExplosiveFuelScore = ExplosiveFuelCost / 10
HiddenClueScore    = HiddenClueDifficulty
RewardScore        = DepthScore + ToolCostScore + ExplosiveFuelScore + HiddenClueScore
```

The highest satisfied threshold assigns existing `Low/Medium/High/Unique`; `Unique` saturates without an upper bound and successful output never assigns `None`. Settings and output collections are copied immutable values. The calculator is stateless and uses canonical RegionId ordering, invariant integer formatting, SHA-256, checked arithmetic, and zero RNG.

# SOURCE CHAIN / APPROVED FIXTURE

```text
Type0 assignment digest = a26e73f25ff7267760a2507ec55b1acda3a6c39a8f3698fc37b190620ceca525
Access assignment digest = 5268b7ed2342f197fb5717c85dbfdb8e51b3c520c217ecf93ad14dc944ecf74f
Growth digest = 1f00f718bdb8d79fbf88923be0d81e0075987267c024cc624086ee6194998caa
Reward-tier canonical digest = c3430c42a27937e143fa89c5839282b9533b62d5fb74fb26fdad490cb545958e
```

The Type0 snapshot, per-cell Type0 assignments, access assignments, and clues joined one-to-one by RegionId. Exact per-region evidence from the canonical summary test:

| Region | Ordinal | Attachment | Rule | Depth | Tool | Fuel | Hidden | Depth score | Tool score | Fuel score | Hidden score | Reward score | Tier | Preview |
|---|---:|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---:|
| OPT_REGION_0000 | 0 | 7 | BASIC | 4 | 0 | 0 | 0 | 8 | 0 | 0 | 0 | 8 | HIGH | 0 |
| OPT_REGION_0001 | 1 | 8 | TOOL | 1 | 1 | 0 | 0 | 2 | 1 | 0 | 0 | 3 | LOW | 0 |
| OPT_REGION_0002 | 2 | 12 | ENVIRONMENT | 1 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | 2 | LOW | 0 |
| OPT_REGION_0003 | 3 | 14 | EXPLOSIVE | 3 | 0 | 30 | 0 | 6 | 0 | 3 | 0 | 9 | HIGH | 1 |
| OPT_REGION_0004 | 4 | 15 | HIDDEN | 4 | 0 | 0 | 4 | 8 | 0 | 0 | 4 | 12 | UNIQUE | 0 |
| OPT_REGION_0005 | 5 | 16 | BASIC | 1 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | 2 | LOW | 0 |
| OPT_REGION_0006 | 6 | 23 | TOOL | 4 | 4 | 0 | 0 | 8 | 4 | 0 | 0 | 12 | UNIQUE | 0 |
| OPT_REGION_0007 | 7 | 24 | ENVIRONMENT | 1 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | 2 | LOW | 0 |
| OPT_REGION_0008 | 8 | 27 | EXPLOSIVE | 4 | 0 | 40 | 0 | 8 | 0 | 4 | 0 | 12 | UNIQUE | 1 |
| OPT_REGION_0009 | 9 | 28 | HIDDEN | 1 | 0 | 0 | 1 | 2 | 0 | 0 | 1 | 3 | LOW | 0 |
| OPT_REGION_0010 | 10 | 30 | BASIC | 3 | 0 | 0 | 0 | 6 | 0 | 0 | 0 | 6 | MEDIUM | 0 |
| OPT_REGION_0011 | 11 | 31 | TOOL | 4 | 4 | 0 | 0 | 8 | 4 | 0 | 0 | 12 | UNIQUE | 0 |

Canonical diagnostics:

```text
source regions / Type0 cells / access assignments = 12 / 39 / 12
tier distribution Low / Medium / High / Unique = 5 / 1 / 2 / 4
contribution totals depth / tool / fuel / hidden = 62 / 9 / 7 / 5
reward score minimum / maximum = 2 / 12
explosive reward-preview reservations = 2
mandatory reward selections = 0
attachment base-open = 0
RNG draws / source mutation / partial publication = 0 / 0 / 0
```

The access distribution remains Basic/Tool/Environment/Explosive/Hidden `3/3/2/2/2`; all `12` clues remain perceptible, all `12` attachment boundaries remain base-closed, and mandatory boundary base-open remains `0`.

# ATOMIC FAILURE / PHASE BOUNDARY

- null input/settings, invalid accounting, source digest mismatch, growth mismatch, and open attachment evidence produce atomic failure: assignments `0`, canonical digest empty, RNG/mutation/mandatory reward selection `0/0/0`.
- checked overflow is exercised and remains trapped by the calculator's checked arithmetic boundary; no partial result is published.
- source signatures before/after calculation are identical; caller order, `tr-TR`/`fr-FR` culture, and service reuse produce the same canonical digest.
- actual reward IDs/items/pools/quantities/spawn slots selected: `0`.
- return policy, inactive buffer, validator, overlay, exit, and generated optional CSV behavior implemented: `0`.
- MAP05 Type4 remains exact: U+D required; L/R independently preserve graph adjacency; UD/LUD/RUD/LRUD remain legal.
- MAP06_04 Type0 L+R-through prohibition and base-closed evidence, and MAP06_05 access/clue/cost/preview assignments are preserved.

# ACTUAL UNITY EDITMODE TEST JOBS

| Required job | Job ID | Passed | Failed | Skipped |
|---|---|---:|---:|---:|
| `OptionalRewardTierCalculatorTests` | `14ef0aeacf454ba093394445603f5604` | 279 | 0 | 0 |
| `OptionalAccessRuleAssignerTests` | `4d6c0e3853264bb59bc6fb7978087694` | 289 | 0 | 0 |
| `Type0RouteMaskAssignerTests` | `2246e6f28dfb4206b2c6ba640361dd92` | 257 | 0 | 0 |
| MAP06 prior combined selection | `2eb111d773ab4b28900d38b6582f4251` | 630 | 0 | 0 |
| MAP05 category selection | `977d76f7920645e39eb8ad5ec0030429` | 1832 | 0 | 0 |
| `MandatoryRouteMaskLookupBuilderTests` MAP05 remainder | `9cdb76e0c9fe47299d76e4e74ac636fd` | 127 | 0 | 0 |
| **Actually executed required total** | — | **3414** | **0** | **0** |

- required minimum `3395` exceeded by actual `3414/3414` PASS.
- MAP05 aggregate: `1832 + 127 = 1959/1959` PASS.
- new-test category actual cases: `34 / 38 / 34 / 36 / 32 / 32 / 26 / 24 / 22`, each specified minimum satisfied; one canonical summary case completes `279`.
- supplemental canonical evidence rerun: job `7952f319380344c9864749ef8a20a96c`, `1/1` PASS; excluded from required-total arithmetic.

# UNITY GATE

- Unity instance: `Constant@ced6e0dfc4a31d45`
- Unity version: `6000.3.8f1`
- forced asset refresh/import/domain reload: COMPLETE
- compile errors: `0`
- final Console errors/warnings after clearing Test Framework result-save and MCP reconnect messages: `0 / 0`
- relevant code warnings: `0`
- compile/Console/relevant warnings gate: `0 / 0 / 0`

# ASSET / META / CSV / GUID / CHANGE-SCOPE GATE

- Assets meta: `3283 -> 3290`
- new C#/matching meta: `7 / 7`
- duplicate GUID groups: `0`
- existing boundary test C# modified: `11 <= 11`; their matching `.cs.meta` modified: `0`
- protected existing allowlisted production/runtime and both asmdef hashes unchanged: `22/22`
- Assets files newer than the patch receipt: exact `25`, comprising only new C#/meta `14` plus allowed boundary test C# `11`
- Authoring CSV/matching meta: `50 / 50`
- approved Authoring manifest SHA-256 unchanged: `4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3`
- independent Authoring snapshot SHA-256 unchanged from pre-task evidence: `58af89c8af9631f9138d4a503925d0886fde99f6f83c0fb1d85a392d76f22b7d`; Authoring files newer than receipt: `0`
- generated Map CSV files created by this task: `0`
- new directory/folder meta/asmdef/asmref: `0`
- Scene/Prefab/Packages/ProjectSettings modifications: `0`
- MAP05/MAP06_01~05 production, graph/mask/models/assignments/CSV modifications: `0`
- Task WRITE ALLOWLIST outside modifications: `0`

# DONE CONDITIONS

- Preconditions, receipt, prior Result/Task SHA, and sole Current Task gates: PASS.
- Type0/access/growth digest chain and one-to-one identity: PASS.
- Exact score formula, component evidence, thresholds, and immutable tier reservation for every region: PASS.
- Access/clue/cost/preview matrix and attachment base-closed state: PASS.
- Atomic failure, checked arithmetic, stable digest, RNG/mutation zero: PASS.
- Actual reward selection and MAP06_07+ behavior absent: PASS.
- MAP05 Type4 and MAP06_04/05 artifacts preserved: PASS.
- Required actual Unity tests, compile, Console, assets, CSV, GUID, and change-scope gates: PASS.

# NEXT

Finalize only `MAP06_06_CALCULATE_OPTIONAL_REWARD_TIER` as COMPLETE and set Current Task to `NONE`. Keep `MAP06_07_IMPLEMENT_RETURN_POLICY` LOCKED. Do not read, create, or start MAP06_07 or any later Task without a separate valid patch.
