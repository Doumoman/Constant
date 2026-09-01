TASK: MAP15_03_IMPLEMENT_MULTI_SECTOR_SPECIAL_AND_CLUSTER_POLICY
STATUS: PASS
MAP15_03: COMPLETE ELIGIBLE only when PASS
MAP15_04_IMPLEMENT_WORLD_PACING_DENSITY_AND_REPETITION: LOCKED / DO NOT START

## User-Facing Implementation Report

이번 작업은 MAP15_01의 169-sector world plan/solve order, MAP15_02의 312-edge intersector plan, MAP13의 SpecialRegion 권위, MAP11의 TerrainCluster identity, MAP14의 sector-local handoff를 받아 **world-level multi-sector reservation/cluster policy**를 구성한다. 결과는 후속 planner가 소비할 immutable in-memory 계약이며, 실제 624x416 Tilemap 생성, Scene/Prefab/GameObject 변경, gameplay spawn 또는 production seed 승인이 아니다.

- 새 Runtime model `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldMultiSectorReservationPlan.cs`는 Special transaction, sector/edge claim, edge lock, cluster containment/allowlist, conflict, typed atomic result와 canonical digest를 공개한다.
- 새 Runtime planner `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldSpecialClusterPolicyPlanner.cs`는 고정 Special, MAP15_02 mandatory/boundary edge, explicit cross-sector cluster, sector-contained cluster, quiet/filler 순으로 우선순위를 결정하고 silent overwrite 없이 winner/loser/reason을 남긴다.
- 새 focused test `Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/WorldSpecialClusterPolicyPlannerTests.cs`는 명시적인 `REFERENCE MULTI-SECTOR RESERVATION PLAN`만 사용한다. 이 fixture는 production seed, full-world terrain solve, Tilemap 출력 또는 MAP15 phase exit을 승인한다고 주장하지 않는다.
- 관측된 world sector/internal edge는 `169/169`, `312/312`다. Special transaction required/accepted/missing는 `6/6/0`, 그중 fixed `4`, deferred Merchant/Maru `2`, two-sector Village `1`이다.
- two-sector Village `TX_VILLAGE_2S`는 sector `2`와 `3` 사이의 정확한 MAP15_02 adjacent edge, compatible endpoint 2개, `VILLAGE_ENTRY_PORT`/`VILLAGE_RETURN_PORT` evidence 및 전용 edge lock 1개를 가진다. adjacency failure와 entry/return missing은 모두 `0`이다.
- reservation claim `10`, edge lock `4`다. lock은 mandatory route `1`, boundary `1`, fixed Village `1`, accepted cross-sector cluster `1`을 포함하며 Special/mandatory/boundary stolen edge lock은 `0`이다.
- cluster policy는 총 `4`, accepted/rejected `3/1`이다. sector-contained 기본 결정 `3` 중 `2`가 accepted이고, explicit cross-sector allowlist `1/1`이 exact cluster/variant/edge/span reason과 compatible edge를 충족해 accepted됐다. implicit cross-sector 및 missing/invalid allowlist 사례는 partial plan 없이 거절되는 테스트로 확인했다.
- conflict는 `2`건이며 모두 `PriorityOverride`다. `TX_CORE_RESOURCE`가 `POLICY_CLUSTER_SPECIAL_CONFLICT`보다 우선하고 reason은 `Higher-priority reservation retained; TerrainCluster policy rejected atomically.`이다. `TX_VILLAGE_2S`가 `CLAIM_QUIET_VILLAGE_CONFLICT`보다 우선하고 reason은 `Higher-priority reservation retained; quiet/filler claim rejected.`이다. fixed Special overlap conflict는 `0`이다.
- input digest는 `b3969fc71dbb0ae16321b5b2dacf66ad3520b438d13adf27f7345dd3ceb726e3`, output digest는 `00b908dc3e883f1823a51a76cd4511be2d3b260895780920cd57a006b37f3590`이다. repeat, reversed enumeration, `tr-TR` culture replay의 input/output/claim/conflict mismatch는 모두 `0`이다.
- invalid/missing upstream, 잘못된 topology/count/digest, duplicate/non-adjacent transaction sector, missing return evidence, fixed Special overlap, implicit/invalid/protected-edge cross-sector cluster, mutation claim은 typed reason을 모으고 `Plan = null`, empty digest로 원자적으로 종료한다. fallback corridor carve나 sector rerender를 시도하지 않는다.
- 새 RNG draw, fallback carve, sector rerender, generated file write, Tilemap/Scene/Prefab/GameObject mutation, gameplay spawn, SpecialRegion/MAP14 sector planner/MAP15_01 world plan/MAP15_02 intersector plan mutation은 각각 `0`이다.
- prior task category, legacy 19347, PlayMode 및 unfiltered regression test는 실행하지 않았다. 새 task-owned compile/test 문제가 없었으므로 regression trigger는 발생하지 않았다.

아직 구현하지 않은 범위는 실제 full-world terrain solve/bake, 624x416 Tilemap, MicroChunk 12x8 slice/streaming, collider/physics/player traversal, Activity/Event/NPC/reward/combat/crafting/inventory runtime, production seed 승인, MAP15_04 pacing/density/repetition, MAP15 phase exit이다. Editor 가시성은 Unity Test Runner focused evidence만 제공하며 새 EditorWindow/overlay/inspector/debug asset은 없다. 게임 가시성은 없다.

## Responsibility and Added Functions

### `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldMultiSectorReservationPlan.cs`

- `WorldReservationOwnerKind`, `WorldReservationSpanKind`, `WorldReservationTransactionState`, `WorldClusterSpanKind`, `WorldReservationLockKind`, `WorldReservationConflictType`, `WorldReservationPolicyFailureCode`: priority/span/state/lock/conflict/atomic-error의 stable enum 계약을 정의한다.
- `WorldReservationClaim`: claim id/owner priority/sector/optional edge/reason/source owner 입력을 immutable comparable claim으로 변환한다.
- `WorldReservationEdgeLock`: exact edge/lock kind/owner/reason 입력을 immutable comparable edge lock으로 변환한다.
- `WorldSpecialReservationTransaction`: transaction/Special identity, fixed/deferred state, span, sectors, edges, entry/return, protected-edge ownership 및 merge reason 입력을 sorted read-only transaction으로 변환한다.
- `WorldClusterContainmentPolicy`: cluster/variant, sector span, optional edge 및 reason 입력을 sector-contained 또는 explicit cross-sector 결정으로 표현하고 accepted/rejection evidence를 보존한다.
- `WorldClusterCrossSectorAllowance`: exact cluster/variant/edge/owner/span/reason 입력을 sorted allowlist identity로 고정한다.
- `WorldReservationConflict`: subject와 winner/loser kind/id/reason을 deterministic comparable evidence로 보존한다.
- `WorldReservationPolicyRequest`: MAP15_01 plan/result, MAP15_02 edge plan, MAP13/MAP14 digests, transaction/policy/allowance/quiet claim과 no-mutation counters를 defensive immutable input으로 묶는다.
- `WorldMultiSectorReservationPlan`: transaction/claim/lock/policy/allowance/conflict, `169/312` topology, accepted/rejected counts, input/output digest, mutation proof, downstream owner `MAP15_04`와 automatic-open false를 공개한다.
- `WorldReservationPolicyFailure` / `WorldReservationPolicyResult`: 누적 typed issue 입력을 성공 시에만 plan을 갖는 atomic 결과로 반환한다.
- `WorldReservationPolicyDigest.ComputeInput`: upstream digests, public authority, publication label, counters와 sorted requests를 UTF-8/LF/InvariantCulture canonical input digest로 변환한다.
- `WorldReservationPolicyDigest.ComputeOutput`: sorted transaction/claim/lock/policy/allowance/conflict를 lower-hex SHA-256 output digest로 변환한다.
- `WorldReservationPolicyDigest.HashCanonicalText`: canonical text 입력을 lower-hex SHA-256으로 변환한다.

### `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldSpecialClusterPolicyPlanner.cs`

- `Plan`: immutable request를 받아 upstream/count/digest/no-mutation 검증, transaction/allowlist/policy 검증, priority resolution, digest 생성을 수행하고 완전한 plan 또는 원자적 typed result를 반환한다.
- `ValidateUpstream`: MAP15_01/02 success, `169/312`, digest chain, MAP13/MAP14 authority 및 모든 zero-mutation counter를 검증한다.
- `ValidateTransactions` / `ValidateFixedSpan` / `ValidateFixedSpecialOverlaps`: fixed/deferred span, sector/edge identity, two-sector adjacency, endpoint compatibility, entry/return evidence, protected edge 및 overlap merge reason을 검증한다.
- `IndexAllowances` / `ValidateClusterPolicies` / `ValidateAllowanceCoverage`: exact cluster/variant/edge/span reason allowlist, default sector containment, MAP15_02 edge compatibility와 Special/mandatory/boundary edge 보호를 검증한다.
- `BuildInheritedAndSpecialLocks` / `AddSpecialClaims`: mandatory route/boundary obligation과 모든 non-deferred Special sector/edge를 fixed 우선순위로 claim/lock한다.
- `ResolveClusterPolicies` / `ResolveQuietClaims` / `PriorityConflict`: cross-sector, contained, quiet/filler 순으로 claim을 적용하고 winner/loser/reason을 기록하며 silent overwrite를 막는다.
- public authority consumed: MAP15_01 `WorldPlanInput`/`WorldSolveOrderResult`, MAP15_02 `WorldIntersectorEdgePlan`, MAP13 `SpecialRegionKind`와 public starter catalog digests, MAP11 `TerrainClusterId`/`SpineVariantId`, MAP14 phase-exit handoff digest다.

### `Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/WorldSpecialClusterPolicyPlannerTests.cs`

- 필수 gate 10개는 public authority로 조립한 reference request를 `Plan`에 넣고 output counts/digests, two-sector Village evidence, priority conflicts, default containment, exact allowlist, atomic invalid cases, determinism, no-mutation 및 MAP15_04 lock을 검증한다.
- `ReferenceReservationFixture`: public MAP15_01 planner와 MAP15_02 integrator를 통해 169-sector/312-edge test-only input을 만들고 MAP13/MAP11/MAP14 identity를 명시적으로 연결한다.
- production Runtime C#/meta 추가 `2/2`, Runtime EditMode test C#/meta 추가 `1/1`이다. 기존 production/test/meta 수정 `0`, Editor production `0`, CSV/schema/cache/generated output `0`, Scene/Prefab/Tilemap/ScriptableObject `0`, asmdef/asmref/ProjectSettings/Packages `0`, upstream 수정 `0`이다.
- downstream owner는 `MAP15_04_IMPLEMENT_WORLD_PACING_DENSITY_AND_REPETITION`이며 이번 작업은 이를 열거나 시작하지 않는다.

## Focused Verification

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP15_03]
discovered: 10
executed: 10
passed: 10
failed: 0
skipped: 0
inconclusive: 0
durationSeconds: 4.4505776
compile errors: 0
relevant Console errors after final verification/clear: 0
relevant Console warnings after final verification/clear: 0

REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

Unity MCP test job `8c7bac83868a4d8da7b75a245e02764b`는 category `MAP15_03`에서 10개를 발견·실행했고 summary `Passed`, passed `10`, failed/skipped `0/0`을 반환했다. 세 task-owned script의 Unity standard validation diagnostics는 error/warning `0/0`이며, 최종 Console clear 후 error/warning은 `0/0`이다.

## Static and Workflow Verification

- 단일 inbox candidate `MAP15_03_IMPLEMENT_MULTI_SECTOR_SPECIAL_AND_CLUSTER_POLICY.md`만 적용했으며 installed Task와 archive SHA-256은 모두 `32a553a012dfc8b795ad879939246b7780b784013db3fd0882723e34a095c782`로 byte-identical이다.
- predecessor MAP15_02 Result PASS SHA-256 `d7dfcef717d29f05ee1c66f4e9afe6c0b7a55716410680bf9e7bf482a6722660`와 installed Task SHA-256 `116b056de902f7d429186e301ce15327192bf2ada5c82b9a1fc8bb4a4b976eb2`는 patch metadata와 일치했다.
- task 시작 조건은 MAP15_02 COMPLETE, MAP15_03 CURRENT, MAP15_04 LOCKED, unrelated staged `0`이었다.
- Runtime/test source에는 UnityEngine, UnityEditor, System.IO, filesystem write, random/time API 의존성이 없다. Scene/Prefab/Tilemap/GameObject 문자열은 mutation counter/property/assertion에만 존재한다.
- 관련 없는 기존 worktree 변경은 수정하거나 stage하지 않았다.

Commit subject: `MAP15_03: implement multi-sector special cluster policy`

Push: NOT PERFORMED
