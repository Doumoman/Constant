```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP13_07_AUTHOR_FORGE_BOSS_AND_OPTIONAL_REGIONS
  task_file: TASKS/MAP13_07_AUTHOR_FORGE_BOSS_AND_OPTIONAL_REGIONS.md
  requires_current_task: NONE
  requires_completed_task: MAP13_06_AUTHOR_THREE_CORE_RESOURCE_REGIONS
  requires_result:
    path: REPORTS/MAP13_06_AUTHOR_THREE_CORE_RESOURCE_REGIONS_RESULT.md
    status: PASS
    sha256: db33f5e46e50153a0d9a340f7726eb09df3cb0cafa6089ac7f42ef7a976587e3
  requires_installed_task:
    path: TASKS/MAP13_06_AUTHOR_THREE_CORE_RESOURCE_REGIONS.md
    sha256: ec7a880f0239819025b9df6f3b9021143523721003c24b026f2e9dce6054ccbb
  sets_current_task: MAP13_07_AUTHOR_FORGE_BOSS_AND_OPTIONAL_REGIONS
```

# MAP13_07 — Author Forge, Boss and Optional Regions

```text
TASK: MAP13_07_AUTHOR_FORGE_BOSS_AND_OPTIONAL_REGIONS
PHASE: MAP13 — SpecialRegion / Village / Mandatory Landmarks
STATUS: CURRENT
NEXT: MAP13_08_CREATE_SPECIAL_VALIDATOR_AND_PREVIEW
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MoonSealForge, BossSealArena, WanderingMerchantCave, MaruTimeShrine 네 starter landmark의 shell·route·state·reset 계약을 additive typed catalog와 atomic compiler로 작성한다.

```text
Forge / Boss:
  MAP13_01~03 placed mandatory source + explicit landmark definition
  → placed immutable landmark plan

Merchant Cave / Maru Shrine:
  explicit local optional definition
  → placement-deferred immutable local plan for MAP14
```

MAP13_01은 MAP03 reservation kind가 있는 Village/CoreResource/Forge/Boss만 placed bridge 대상으로 승인했고 `OptionalLandmark`는 명시적으로 지원하지 않는다. 이번 Task는 이 경계를 바꾸지 않는다. Optional 두 지역은 world/reservation binding을 위조하지 않고 local shell/state만 compile하며 실제 world placement는 `DeferredToMAP14`로 게시한다.

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 정확한 script 경로, class/method별 input→output, 지역별 shell/route/state/reset 실제 수, 새 기능, 파이프라인 위치, 미구현, Editor/게임 가시성을 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| exact-four starter catalog | MAP03/MAP14 reservation·placement 선택 |
| Forge 공정 순서와 실패 시 자원 반환 proof | inventory 소비·아이템 제작 실행 |
| Boss seal gate, encounter reset, fall recovery proof | Boss AI/공격/HP/전투 실행 |
| Merchant safe side-pocket shell와 marker states | NPC 종족 RNG·상점 가격/재고 |
| Maru choice preview/state/revisit proof | 실제 힌트 탐색·Maru AI/관심도 실행 |
| immutable route/state/reset plan/digest/errors | Prefab/Scene/Tilemap/physics/save |

새 이동 규칙, 필수 Village 방문, 강제 optional landmark 방문 또는 synthetic path를 만들지 않는다.

## 2. Focused-Only and No-Regression Policy

정상 실행은 EditMode category `MAP13_07`만 선택한다.

```text
MAP13_07 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13_01~06 selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
```

current public API를 신규 focused test에서 호출하는 것은 과거 category 재실행이 아니다. 신규 파일 자체 문제는 신규 파일 안에서 고치고 `MAP13_07`만 재실행한다.

upstream defect나 contract conflict를 발견하면 기존 파일을 수정하거나 회귀를 실행하지 않는다. owner/invariant/reason/minimum verification을 Result에 기록하고 `BLOCKED`로 STOP한다.

## 3. Read-Only Preflight

```text
MAP13_06 Result: PASS
MAP13_06 Result SHA-256:
db33f5e46e50153a0d9a340f7726eb09df3cb0cafa6089ac7f42ef7a976587e3

MAP13_06 original Task SHA-256:
ec7a880f0239819025b9df6f3b9021143523721003c24b026f2e9dce6054ccbb

MAP13_06 repair SHA-256:
57671bf7a031f89e93d9ad830a0a41c9cd229f8cf22dc41ce6a42d977d7bdb0d

MAP13_06 COMPLETE / MAP13_07 CURRENT / MAP13_08 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required authority:

```text
MAP09 SpecialRegionKind: Forge / Boss / OptionalLandmark
MAP09 layer/slot/persistence/AccessClass contracts
MAP13_01 placed bridge for Forge/Boss; OptionalLandmark unsupported boundary
MAP13_02 entry/buffer/collision and priority evidence
MAP13_03 fixed/slot layer and persistence safety
MAP13_06 MoonCore/CassiaSap/StarNuruk identity and no-loss plans
```

required public authority가 없거나 existing source modification이 필요하면 `BLOCKED`다.

## 4. Exact Write Boundary

정상 범위는 Runtime 3개, focused test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialLandmarkRegionDefinitions.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialLandmarkRegionCompiler.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialLandmarkRegionStarterCatalog.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SpecialRegions/SpecialLandmarkRegionAuthoringTests.cs(.meta)
```

```text
Runtime: Game.Map.Runtime / StarNight.Map.WorldGeneration.SpecialRegions
Tests: Game.Map.Tests.EditMode / StarNight.Map.Tests.EditMode.WorldGeneration.SpecialRegions
Category: MAP13_07
```

수정·생성 금지:

```text
existing C# / test / CSV / meta
V2 schema registry/test and Authoring/Generated CSV/meta
asmdef / asmref
Scene / Prefab / ScriptableObject / Tilemap / Material / Texture
Settings / Packages
```

Editor importer/window, placement solver, serializer, helper 파일은 추가하지 않는다.

## 5. Exact Starter Matrix and Binding Modes

Catalog은 exact 네 entry를 canonical region ID 순서로 게시한다.

| Region ID | Landmark | Kind | Theme | Binding | Placed footprint | Design canvas | Active design chunks |
|---|---|---|---|---|---|---|---:|
| `SR_MOON_SEAL_FORGE_9` | MoonSealForge | Forge | AbandonedMill | `PlacedMandatorySite` | `1×1 = 48×32` | `48×24`, origin `(0,4)` | 9 |
| `SR_MOON_BOSS_SEAL_ARENA_12` | BossSealArena | Boss | MoonPalaceCommon | `PlacedMandatorySite` | `1×1 = 48×32` | `48×32`, origin `(0,0)` | 12 |
| `SR_WANDERING_MERCHANT_CAVE_3` | WanderingMerchantCave | OptionalLandmark | Any | `DeferredOptionalLocal` | none | `24×16`, origin `(0,0)` | 3 |
| `SR_MARU_TIME_SHRINE_5` | MaruTimeShrine | OptionalLandmark | MoonPalaceCommon | `DeferredOptionalLocal` | none | `24×24`, origin `(0,0)` | 5 |

PDF `SM_*` label은 design reference이며 MAP09 stable runtime ID가 아니다. 위 `SR_*`만 runtime identity로 사용하고 문자열 변환·추론하지 않는다.

`SpecialLandmarkTheme`은 이 starter catalog의 typed theme이며 기존 four-biome enum에 `Any`/`MoonPalaceCommon`을 억지로 추가하지 않는다.

### 5.1 Design chunk rules

- Forge: logical `4×3` grid of 12×8, explicit active 9/12
- Boss: logical `4×4` grid of 12×8, explicit active 12/16
- Merchant: logical `2×2` grid of 12×8, explicit active 3/4
- Maru: logical `2×3` grid of 12×8, explicit active 5/6
- active offsets는 explicit unique, in-range, 4-neighbor connected다.
- design chunk는 `GeneratedSlice`가 아니며 final Tilemap/streaming unit을 소유하지 않는다.

### 5.2 Binding separation

`PlacedMandatorySite` request는 MAP13_01 bridge, MAP13_02 plans, MAP13_03 layers와 expected digests가 모두 필요하다. region ID/kind/1×1 footprint가 exact 일치해야 한다.

`DeferredOptionalLocal` request는 다음을 exact 만족한다.

- source kind `OptionalLandmark`
- world origin/reservation/bridge/entry-buffer/collision/fixed-slot input `0`
- all coordinates normalized local design-canvas coordinates
- placement status exact `DeferredToMAP14`
- local entry/return side-pocket connector marker와 shell return witness 존재
- world coordinate, reservation digest, placed ownership 또는 overlap claim 게시 `0`

Optional plan을 Forge/Boss처럼 fake MAP13_01 bridge로 감싸거나 unsupported kind를 alias하지 않는다.

## 6. Shared Landmark Model

프로젝트 naming/style에 맞추되 다음 semantic surface를 제공한다.

```text
SpecialLandmarkKind: MoonSealForge / BossSealArena / WanderingMerchantCave / MaruTimeShrine
SpecialLandmarkTheme: AbandonedMill / MoonPalaceCommon / Any
SpecialLandmarkBindingKind: PlacedMandatorySite / DeferredOptionalLocal
SpecialLandmarkPlacementStatus: Placed / DeferredToMAP14
SpecialLandmarkRouteKind: Low / High / Recovery / Return
SpecialLandmarkNodeRole
SpecialLandmarkStateRole
SpecialLandmarkTransitionTrigger
SpecialLandmarkResetPolicy

SpecialLandmarkDesignChunk
SpecialLandmarkShellNode / ShellEdge / RouteDefinition
SpecialLandmarkStateDefinition / StateTransitionDefinition
SpecialLandmarkResetDefinition
SpecialLandmarkMarkerDefinition
SpecialLandmarkRegionDefinition
SpecialLandmarkCompileRequest
SpecialLandmarkRegionPlan
SpecialLandmarkRegionCompiler.Compile
SpecialLandmarkRegionStarterCatalog
SpecialLandmarkCanonicalDigest
SpecialLandmarkErrorCode / Error / Result
```

모든 node/edge/route/state/transition/reset/marker/slot ID와 coordinate는 explicit catalog data다. filename/display/ID prefix 또는 graph 근접성에서 의미를 추론하지 않는다.

공통 요구:

- Low/High/Return witness가 shell 이동을 바꾸지 않음
- 모든 high/failure branch는 Recovery 또는 Return으로 합류
- mandatory route는 `MandatoryNoTool`; optional route는 `OptionalNoTool`
- state/marker 변화가 collision, coordinate, route, slot persistence identity를 변경하지 않음
- any error는 plan/digests `0`, accumulated·deduped·stable-sorted error만 게시
- reverse input/repeat/`tr-TR`에서 동일 plan/digest
- pathfinding/carve/teleport/RNG/world/tile/inventory/save mutation `0`

## 7. MoonSealForge Contract

```text
Region: SR_MOON_SEAL_FORGE_9
Kind: Forge
Route: mandatory L→R
process order: Grind → Mix → Press → MoonlightCure
inputs: MoonCore + CassiaSap + StarNuruk
output: MoonSeal exact 1
```

Requirements:

- Low route는 표시된 네 workstation을 authored order로 통과한다.
- High route는 같은 공정 순서를 보존하고 timing-optimization marker와 Maru-attention-reduction marker만 추가한다.
- 각 공정 사이 explicit ManualReset과 SafeCorridor recovery witness가 있다.
- input resource는 `Available → Reserved → Consumed` semantic ledger를 가진다.
- 어느 공정에서든 failure면 세 input 모두 `Returned`; partial consumption/permanent loss `0`이다.
- success에서만 세 input `Consumed`, MoonSeal Reward `Available`을 게시한다.
- output Reward slot은 `SR_SLOT_MOON_SEAL_REWARD`, `Required=true`, persistence key는 hardcode하지 않고 public `SpecialPersistenceKey.ForSlot` authority와 exact 결합한다.
- 제작 완료 plan은 Boss direction marker를 게시하지만 world direction 탐색이나 UI를 실행하지 않는다.
- actual item removal, workstation MonoBehaviour, timer 또는 SaveData write는 `0`이다.

## 8. BossSealArena Contract

```text
Region: SR_MOON_BOSS_SEAL_ARENA_12
Kind: Boss
Route: mandatory SealGate → Arena
states: GateLocked → GateAccepted → EncounterActive → Defeated
```

Requirements:

- GateAccepted transition은 MoonSeal requirement marker를 참조하지만 inventory를 소비하지 않는다.
- Low route는 broad lower recovery zone에서 observation/return을 보장한다.
- High route는 upper platforms + falling-object marker + pressure-device marker를 연결한 optional mastery witness다.
- 모든 authored fall/failure node는 exact central lower recovery node로 연결된다.
- encounter fail/reset은 `EncounterActive`로 복귀하며 seal acceptance identity를 rollback하지 않는다.
- Defeated state는 encounter persistence marker를 보존하고 duplicate boss/reward claim을 만들지 않는다.
- `IntroducesNewMovementRule=false`가 exact다. MAP13_06과 기존 traversal vocabulary만 참조한다.
- Maru state transition marker는 별도 owner이며 Boss shell/encounter state에 합치지 않는다.
- actual Boss AI, damage, attack, falling physics, collider와 combat reset은 구현하지 않는다.

## 9. WanderingMerchantCave Contract

```text
Region: SR_WANDERING_MERCHANT_CAVE_3
Kind: OptionalLandmark
Route: optional side pocket → same local return
states: Available / Visited / Departed
```

Requirements:

- local shell은 shop safe zone, exact two distinct entrance cue marker와 short return witness를 가진다.
- Low route는 Shop marker를 거쳐 local Return으로 복귀한다.
- High route는 upper storage marker + information marker + optional benefit marker를 거쳐 같은 Return으로 복귀한다.
- allowed merchant presentation variants는 Alien/Rabbit/Spacefarer/Machine exact 네 개다.
- catalog/compiler가 variant를 RNG로 선택하지 않으며 downstream selected marker만 검증한다.
- Visited/Departed에서도 shell/entry/return/safe-zone identity는 동일하다.
- merchant visit, rare inventory와 optional benefit는 mandatory progression dependency `0`이다.
- actual NPC spawn, shop price/stock, purchase 또는 placement는 구현하지 않는다.

## 10. MaruTimeShrine Contract

```text
Region: SR_MARU_TIME_SHRINE_5
Kind: OptionalLandmark
Route: optional side landmark → same local return
states: Offered → Ignored / ShortHint / StrongHint
```

Requirements:

- shrine interior는 exact non-combat safe-zone marker를 가진다.
- choice effect summary는 transition 이전에 preview marker로 게시된다.
- Ignored는 진행 변화 없이 Return, ShortHint는 hint marker만 게시한다.
- StrongHint는 rare-terrain compass marker와 Maru-attention-increase marker를 함께 게시한다.
- StrongHint high route 실패/취소는 safe zone 또는 Return으로 복귀한다.
- choice는 `PersistentChoice` reset policy이며 revisit에서 reroll/duplicate benefit `0`이다.
- shrine visit은 mandatory progression dependency `0`이다.
- actual hint search, rare terrain selection, Maru AI/attention mutation, UI 또는 save write는 구현하지 않는다.

## 11. Output, Digest and Atomic Failure

Success output:

```text
exact-four canonical catalog
design shell/chunk/node/edge/route witnesses
canonical state/transition/reset graph
Forge resource-return and MoonSeal output proof
Boss gate/encounter/fall-recovery proof
Merchant/Maru optional local return and state proof
Placed or DeferredToMAP14 binding status
source/component/aggregate digests
zero mutation/solver/gameplay counters
```

Collections은 defensive-copy/read-only/canonical order다. digest는 semantic IDs, binding, design shell, graph, states, reset, marker, source identity를 포함하고 display text/PDF polyline/time/object identity를 제외한다.

Minimum error groups:

```text
MissingInput | DigestMismatch | RegionIdentityMismatch | KindMismatch
InvalidBindingMode | UnsupportedFootprint | InvalidDesignCanvas | InvalidActiveChunk
DuplicateNode | InvalidEdge | InvalidRoute | MissingReturn | UnrecoverableFailure
InvalidState | InvalidTransition | InvalidResetPolicy | ShellMutation
ForgeProcessOrderMismatch | ResourceLossRisk | InvalidSealReward
InvalidBossGate | MissingFallRecovery | NewMovementRuleIntroduced
OptionalWorldBindingClaim | MissingSafeZone | MandatoryOptionalDependency
MissingChoicePreview | DuplicateBenefitRisk | NonCanonicalPublication
```

## 12. Focused Tests

public source fixtures와 exact starter catalog로 최소 다음을 검증한다.

1. exact-four ID/kind/theme/binding/design/active-chunk matrix
2. four design grids의 bounds/connectivity/uniqueness
3. Forge placed source identity와 Grind→Mix→Press→Cure exact order
4. Forge success consume/output와 every-stage failure all-resource return
5. Boss seal gate/state/reset, low/high route와 all-fall central recovery
6. Boss new movement rule 0, Maru state separate marker
7. Merchant deferred local shell, safe zone, cues 2, low/high/short return
8. merchant variants exact four, RNG/mandatory dependency 0
9. Maru preview before choice, three outcomes, safe return, PersistentChoice/revisit no duplicate
10. Optional plans의 world/reservation/bridge/placed ownership claim 0와 `DeferredToMAP14`
11. invalid identity/binding/chunk/graph/state/reset/resource/seal/recovery/optional dependency atomic failure
12. reverse/repeat/culture/immutability/digest 및 all mutation/solver/gameplay counter 0

PDF polyline 좌표, actual physics, AI/combat/item/shop/hint/save 또는 placement solver를 test에 복제하지 않는다.

## 13. Verification and Required Result

Unity refresh/compile 후 `MAP13_07` EditMode만 실행한다.

```text
discovered = executed = passed
failed / skipped / inconclusive = 0 / 0 / 0
compile / relevant Console error = 0 / 0
prior category / legacy / PlayMode / unfiltered selections = 0 / 0 / 0 / 0
```

Static gate:

```text
new Runtime C#/meta: 3/3
new focused test C#/meta: 1/1
existing C#/test/CSV/meta modifications: 0
new/modified Authoring or Generated CSV/meta: 0
schema registry/test modifications: 0
Scene/Prefab/Tilemap/Settings/Packages changes: 0
duplicate GUID: 0
unapplied candidate/diff-check/unrelated staged: 0/0/0
Git push: NOT PERFORMED
```

Result 경로:

```text
MapDesign/MCP/REPORTS/MAP13_07_AUTHOR_FORGE_BOSS_AND_OPTIONAL_REGIONS_RESULT.md
```

상단 verdict:

```text
TASK: MAP13_07_AUTHOR_FORGE_BOSS_AND_OPTIONAL_REGIONS
STATUS: PASS | BLOCKED
MAP13_07: COMPLETE ELIGIBLE | NOT COMPLETE
MAP13_08_CREATE_SPECIAL_VALIDATOR_AND_PREVIEW: LOCKED / DO NOT START
```

`## User-Facing Implementation Report`에서 다음을 실제 수치로 보고한다.

- 신규/수정 script 전체 경로와 class/method별 input→output
- 지역별 design chunk/node/edge/route/state/transition/reset/marker 실제 개수
- Forge 단계별 자원 반환과 MoonSeal output proof
- Boss gate/encounter/fall recovery와 new movement rule 0
- Merchant/Maru safe/local return, optional dependency 0, placement deferred 증명
- 새로 가능해진 기능과 파이프라인 위치
- 아직 미구현한 CSV/placement/physics/device/AI/combat/item/shop/UI/save, MAP13_08+
- Editor/게임 가시성

`## Responsibility and Added Functions`에는 아래를 표로 보고한다.

| Field | Required evidence |
|---|---|
| Task responsibility | four landmark shell/route/state/reset starter compile |
| Added scripts | Runtime 3 + focused test 1 exact paths |
| Added functions | public type/method별 sole responsibility와 input→output |
| Inputs consumed | Forge/Boss MAP13_01~03 source + MAP13_06 identity; optional local definitions |
| Outputs produced | immutable exact-four catalog/plans/digests/errors and binding status |
| Explicit non-ownership | CSV/schema, placement, physics/device/AI/combat/item/shop/UI/save, Prefab/Tilemap |
| Downstream consumer | 별도 검수 후 MAP13_08만 unlock 가능 |

그 뒤 focused test, static scope, regression selections, task-owned files와 commit handoff를 기록한다.

정상 문구:

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

PASS일 때만 Status Finalize 후 task-owned 파일만 atomic commit한다.

```text
Subject: MAP13_07: author Forge Boss and optional regions
Push: NOT PERFORMED
```

Result가 PASS여도 MAP13_08을 자동 시작하지 않고 별도 검수까지 LOCKED로 유지한다.
