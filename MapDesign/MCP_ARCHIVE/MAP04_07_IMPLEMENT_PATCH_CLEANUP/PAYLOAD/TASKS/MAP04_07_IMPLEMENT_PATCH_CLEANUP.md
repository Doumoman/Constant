# MAP04_07 — Implement Patch Cleanup

```yaml
status_control:
  task_key: MAP04_07_IMPLEMENT_PATCH_CLEANUP
  result_file: REPORTS/MAP04_07_IMPLEMENT_PATCH_CLEANUP_RESULT.md
```

## Goal

MAP04_06 `Completed` output의 일반 Core/Satellite 경계에서 exact checkerboard와 1-cell neck만 deterministic cleanup한다. source·reservation·site binding·seed·Intrusion 환경은 보존하고 RNG는 쓰지 않는다.

```text
Input:  17 patches / 165 assigned / 4 reserved-unassigned / source RNG 1912
Output: same patch IDs/count and assigned/unassigned counts
RNG:    0 calls, source DrawCount evidence unchanged
```

이 Task는 cleanup만 한다. export/CSV, final validator, overlay, pass/root/retry adapter는 범위 밖이다.

## Read Order / Prior Gate

control 문서 → Master → Status → 이 Task → `REPORTS/MAP04_06_IMPLEMENT_INTRUSION_PLACEMENT_RESULT.md` 순으로 읽는다.

Prior Result gate:

```text
STATUS PASS
SHA-256 17be290682faf4a69716424bed7eb38fa32049a63f5406c17d0c89af128644ed
focused/regression/actual 156/515/671 PASS
output patches/assigned/reserved-unassigned 17/165/4
RNG 1907->1912
violations/source mutation 0/0
Assets meta 3118; existing/unexpected 0/0
```

## Read / Write Allowlist

Read body only:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/BiomeDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorReservation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchRole.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchSeed.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomeSectorOwnership.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchSiteBinding.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatch.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/BiomePatchSnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/IntrusionPlacementRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/IntrusionPlacementDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/IntrusionPlacementPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/IntrusionPlacementResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/IntrusionPlacer.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/IntrusionPlacerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/BiomePatchModelsTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

matching meta와 approved Generation 폴더 filename inventory, Authoring CSV/meta hash/count, 전체 meta GUID, `.APPLIED` 이후 path-only scope는 읽을 수 있다. installed CSV body, unrelated C#, prior tests, future Task, Legacy, Scene/Prefab YAML은 읽지 않는다.

신규 exact 파일:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/PatchCleanupError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/PatchCleanupMoveRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/PatchCleanupDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/PatchCleanupPublication.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/PatchCleanupResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/PatchCleanup.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/PatchCleanupTests.cs
```

위 7 C# + matching meta 7 + Result만 생성한다. existing 파일/asmdef/CSV를 수정하지 않는다.

Namespace는 Runtime `StarNight.Map.WorldGeneration.Generation`, test `StarNight.Map.Tests.WorldGeneration.Generation`이다. Unity `6000.3.8f1` current C#에 맞추고 UnityEditor/UnityEngine.Object/reflection/static mutable state를 production에 넣지 않는다.

## Public Contract

```text
PatchCleanupResult Clean(
    IntrusionPlacementResult intrusionResult,
    IEnumerable<BiomeTypeDefinition> biomeTypes,
    IEnumerable<BiomePatchRuleDefinition> patchRules)
```

checked-in API shape가 illustrative signature보다 우선한다. file/registry/singleton/RNG/root를 자체 조회하지 않는다.

Status:

```text
Completed    publication+diagnostics, errors 0
InvalidInput publication/diagnostics null, stable errors >=1
RetryRequired publication null, diagnostics+errors >=1, atomic rollback
```

`PatchCleanupErrorCode` minimum set:

```text
MissingIntrusionResult
IntrusionNotCompleted
MissingPublication
MissingDiagnostics
MissingBiomeTypes
MissingPatchRules
InvalidSourceSnapshot
InvalidDefinition
NoSafeCleanupMove
CleanupStepLimitExceeded
InternalInvariantViolation
```

Structural errors는 sorted/deduped하고 source를 바꾸지 않는다.

## Frozen Protection Mask

다음 sector는 이동 source/target이 될 수 없다.

1. P01에서 reserved인 모든 sector
2. 모든 Core/Satellite/Intrusion seed sector
3. 모든 Core site binding footprint sector
4. 모든 Intrusion sector
5. 각 Intrusion sector의 in-bounds cardinal neighbor 전부

5번은 MAP04_06에서 확정한 host/pair/anchor 환경을 byte-logically 보존하기 위한 guard다. protected cell을 포함하는 anomaly는 `ProtectedAnomalyCount`로만 기록하고 actionable cleanup 대상에서 제외한다.

## Exact Anomalies

cardinal order는 existing `L,R,U,D`, index는 `y*13+x`다. Core/Satellite만 normal patch다. Intrusion은 anomaly source/target이 아니다.

### Checkerboard

interior center `c`가 아래를 모두 만족하면 actionable checkerboard다.

- c는 unprotected normal patch 소유
- L/R/U/D 네 neighbor가 모두 동일한 foreign normal `PatchId`
- target `PatchId != c.PatchId`

legal이면 c를 target patch로 transfer한다.

### One-cell neck

interior unprotected normal center c에 대해 두 pattern 중 하나다.

```text
vertical:   U,D = c donor PatchId; L,R = same foreign normal target PatchId
horizontal: L,R = c donor PatchId; U,D = same foreign normal target PatchId
```

해결 action 순서:

1. `Collapse`: c를 foreign target으로 transfer.
2. Collapse가 donor connectivity/site preservation 때문에 불가하면 `Widen`: 두 foreign flank 중 legal한 smallest SectorIndex 한 칸을 c donor로 transfer.

모든 legal alternative는 global selection에 함께 넣는다. protected flank, Intrusion, reservation, seed, binding은 Widen 대상이 아니다.

## Legal Transfer Gate

simulate 후 모두 만족해야 한다.

- source/target normal patch이고 cardinal adjacent
- moved cell unprotected
- donor/target patch는 non-empty cardinal connected
- donor/target size가 각 rule min/max 및 hard max `59` 통과
- 일반 patch size `>=2`; patch/seed/binding ID와 role/rule/biome identity 보존
- every seed/binding sector가 원 patch에 유지
- exact 169 ownership rows; ownership↔patch bidirectional
- P01 reserved rows, all protected ownership, all Intrusion patches/neighbors unchanged
- patch IDs/count와 assigned/unassigned는 source와 동일(viable `17/165/4`), SecondaryBiome empty
- biome normal share와 Intrusion share cap 통과
- overlap/orphan/disconnected/site misownership/source mutation `0`

transfer는 기존 두 patch만 새 immutable value로 rebuild한다. moved ownership은 target primary biome/PatchId로 바꾼다. patch 생성/삭제/병합/ID rename은 금지한다.

## Deterministic Global Algorithm

현재 working snapshot의 score:

```text
(ActionableCheckerboardCount, ActionableNeckCount, CrossPatchUndirectedEdgeCount)
```

loop:

1. actionable anomaly를 center SectorIndex 순으로 열거한다.
2. 각 Collapse/Widen을 simulate하고 Legal Transfer Gate를 적용한다.
3. after score가 before score보다 lexicographically 작은 action만 남긴다.
4. `(after score, center index, action kind Collapse<Widen, moved index, donor PatchId, target PatchId)` 순 smallest를 적용한다.
5. 다시 전체를 계산한다.

완료 조건은 actionable checkerboard/neck `0/0`이다. accepted action마다 score가 strict 감소해야 한다. step limit은 `169*4 = 676`이다.

- 처음부터 anomaly 0이면 valid no-op Completed/new immutable publication이다.
- anomaly가 있으나 improving legal action이 없으면 `RetryRequired` + `NoSafeCleanupMove`, publication null, records empty, source counts로 rollback한다.
- limit 도달도 RetryRequired/rollback이다.
- RNG API를 받거나 호출하지 않는다. diagnostics는 input의 actual source DrawCount를 그대로 before/after에 기록한다(viable `1912/1912`); method/raw draws `0/0`이다. production에 `1912`를 hard-code하지 않는다.

## Immutable Output

`PatchCleanupMoveRecord` minimum fields:

```text
Sequence, Kind(CheckerboardCollapse/NeckCollapse/NeckWiden)
CenterSectorIndex, MovedSectorIndex
DonorPatchId, TargetPatchId, Donor/TargetBiomeId
DonorSizeBefore/After, TargetSizeBefore/After
ScoreBefore/After (three components)
```

`PatchCleanupDiagnostics` minimum:

```text
WorldSeed, SourceRngDrawCount, FinalRngDrawCount, RngMethodCallCount
Initial/FinalPatchCount, AssignedCount, UnassignedCount
Initial/FinalActionableCheckerboardCount
Initial/FinalActionableNeckCount
ProtectedAnomalyCount, MoveCount, StepLimit
Moves, violation counters
```

`PatchCleanupPublication`은 `SourceIntrusion`, final `Snapshot`, `Moves`, Core/Satellite/Intrusion/total patch counts와 assigned/unassigned를 보존한다. collections는 copied read-only다.

same input + shuffled definitions + culture/time/thread + fresh/reused instance는 logical byte-equivalent output을 만든다. source result/publication/P01/P02/P03, definitions, lists를 mutate하지 않는다.

## Focused Tests / Verification

`PatchCleanupTests.cs` actual NUnit case `>=120`:

- structural accumulated errors/source immutability
- exact protection mask와 protected anomaly exemption
- checkerboard four-neighbor exactness; edge/near-match rejection
- vertical/horizontal neck, Collapse/Widen order
- donor/target min/max/connectivity/site/seed/binding gates
- Intrusion sector+neighbors immutable
- global score/order/strict decrease/no cycle/676 bound
- no-op, multi-step, no-safe-move rollback
- ownership/patch/count/share conservation
- source RNG `1912->1912`, RNG dependency scan 0
- shuffled/culture/fresh-reused determinism
- viable MAP04_06 integration actual cleanup counts/moves/final score

실제 실행:

```text
PatchCleanupTests          >=120 PASS
IntrusionPlacerTests         156/156 PASS
BiomePatchModelsTests        107/107 PASS
Required regressions         263/263 PASS
Actually executed total     >=383 PASS
failed/skipped                 0/0
```

large suite는 실행하지 않는다. discovery-only:

```text
Game.Map targeted >=4785
Full EditMode      >=4853
```

forced compile/Console/relevant warning `0/0/0`.

Asset gate:

```text
baseline/final Assets meta 3118/3125
new Runtime/test/meta 6/1/7
exact Assets changes 14
existing/unexpected changes 0/0
Authoring CSV/meta 50/50 unchanged
legacy Editor.meta 6/6; duplicate GUID 0
```

## Compact Result / Finalize

Result는 `140 lines` 이하의 고정 요약만 쓴다: `STATUS`, apply/SHA, created paths+GUID, actual anomaly/move/score/count/RNG, tests, compile/meta/scope, out-of-scope, NEXT. 이전 Task 설명을 복사하지 않는다.

PASS일 때만:

```text
MAP04_07_IMPLEMENT_PATCH_CLEANUP: COMPLETE
Current Task: NONE
Last Completed Task: MAP04_07_IMPLEMENT_PATCH_CLEANUP
Last Result: REPORTS/MAP04_07_IMPLEMENT_PATCH_CLEANUP_RESULT.md / STATUS: PASS
MAP04_08_EXPORT_BIOME_PATCH_RESULTS: LOCKED
```

## Do Not

- existing Assets/CSV/asmdef/Scene/Prefab 수정
- protected/site/seed/Intrusion 환경 이동
- patch 생성/삭제/ID 변경
- RNG/clock/file/Unity RNG/System.Random
- score 비감소 action, iteration-order tie break, in-place mutation
- cleanup 외 export/validator/overlay/root/retry 구현
- MAP04_08 생성/시작, Git commit/push
