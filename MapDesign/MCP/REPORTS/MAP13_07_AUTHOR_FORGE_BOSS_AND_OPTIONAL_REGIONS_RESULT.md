TASK: MAP13_07_AUTHOR_FORGE_BOSS_AND_OPTIONAL_REGIONS
STATUS: PASS
MAP13_07: COMPLETE ELIGIBLE
MAP13_08_CREATE_SPECIAL_VALIDATOR_AND_PREVIEW: LOCKED / DO NOT START

## User-Facing Implementation Report

MoonSealForge, BossSealArena, WanderingMerchantCave, MaruTimeShrine의 exact-four starter를 additive immutable typed catalog와 atomic compiler로 작성했다. Forge/Boss는 실제 MAP13_01 bridge, MAP13_02 entry-buffer/collision plan, MAP13_03 fixed-slot layer를 요구하는 `PlacedMandatorySite`이며, Merchant/Maru는 world/reservation/bridge/layer 입력을 전부 금지하고 `DeferredToMAP14`를 게시하는 `DeferredOptionalLocal`이다. MAP13_07 patch SHA-256은 `1ddd61aacf2d8e35c03a790ef459f08286e3a4afc526b9194a8a2c456048e20e`이며 installed/archive가 byte-identical하다.

추가한 모든 script와 class/method별 책임·input→output:

| Script | Class/method | 책임 | Input → Output |
|---|---|---|---|
| `Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialLandmarkRegionDefinitions.cs` | `SpecialLandmarkKind`, `SpecialLandmarkTheme`, binding/placement/route/node/state/transition/reset/marker/dependency/variant/resource enums | 네 landmark의 shell·route·state·reset 의미와 placed/deferred 경계를 typed vocabulary로 고정 | authored enum value → canonical semantic identity |
| 같은 파일 | `SpecialLandmarkDesignChunk` | 12×8 logical design-grid offset value | `(x,y)` → comparable immutable chunk offset |
| 같은 파일 | `SpecialLandmarkShellNode`, `SpecialLandmarkShellEdge`, `SpecialLandmarkRouteDefinition` | explicit node coordinate/role과 ordered edge/access/dependency, Low/High/Recovery/Return membership 보존 | authored graph fields → immutable shell graph |
| 같은 파일 | `SpecialLandmarkStateDefinition`, `SpecialLandmarkStateTransitionDefinition`, `SpecialLandmarkResetDefinition` | state identity, ordered trigger, failure/state reset 및 resource-return/seal-preservation/reroll 방지 의미 보존 | explicit state/reset fields → immutable state graph |
| 같은 파일 | `SpecialLandmarkMarkerDefinition`, `SpecialLandmarkForgeLedgerDefinition`, `SpecialLandmarkRewardDefinition` | marker owner/order/persistence, Forge 3-resource ledger, exact MoonSeal Reward binding 보존 | marker/ledger/reward fields → immutable semantic proof data |
| 같은 파일 | `SpecialLandmarkRegionDefinition` | exact starter shell/design/graph/state/reset aggregate를 defensive-copy하고 canonical order로 게시 | explicit starter collections → immutable definition + definition digest |
| 같은 파일 | `SpecialLandmarkCompileRequest` | placed MAP13_01~03 sources, optional null-source boundary, MAP13_06 exact resource identities를 definition과 결합 | definition + source objects/digests → compile input |
| 같은 파일 | `SpecialLandmarkRouteWitness`, `SpecialLandmarkRegionPlan` | ordered route witnesses, binding status, source/component digests와 zero-mutation counters 게시 | validated definition/source chain → immutable downstream plan |
| 같은 파일 | `SpecialLandmarkErrorCode`, `SpecialLandmarkError`, `SpecialLandmarkResult` | accumulated·deduplicated·stable-sorted atomic failure 또는 success plan 게시 | findings → plan or canonical errors, never partial plan |
| 같은 파일 | `SpecialLandmarkCanonicalDigest.ComputeDefinition/Compute/ComputeDesign/ComputeShell/ComputeState/ComputeMarker` | identity/design/shell/state/reset/marker/source semantic material의 SHA-256 생성 | definition 또는 plan component → lowercase 64-hex digest |
| `Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialLandmarkRegionCompiler.cs` | `SpecialLandmarkRegionCompiler.Compile` | identity, canvas/chunk, graph/route/access, state/transition/reset, marker, placed/deferred source, Forge/Boss/Merchant/Maru 계약을 검증하고 witness를 작성 | `SpecialLandmarkCompileRequest` → `SpecialLandmarkResult` |
| `Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialLandmarkRegionStarterCatalog.cs` | `Entries`, `CanonicalDigest`, `TryGetDefinition`, `GetDefinition` | exact-four canonical starter publication과 typed lookup | none 또는 `SpecialLandmarkKind` → read-only catalog/definition |
| `Assets/_Game/Tests/EditMode/Map/WorldGeneration/SpecialRegions/SpecialLandmarkRegionAuthoringTests.cs` | `SpecialLandmarkRegionAuthoringTests`의 12 focused tests | public MAP13 source compiler fixture와 exact catalog/count/proof/invalid atomic failure/determinism/zero mutation 검증 | public sources + catalog → NUnit PASS/diagnostic |

네 지역별 실제 게시 수:

| Region | Chunks | Nodes | Edges | Routes | Low / High / Recovery / Return | States | Transitions | Resets | Markers |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| MoonSealForge | 9 | 13 | 22 | 6 | 1 / 1 / 3 / 1 | 14 | 9 | 3 | 12 |
| BossSealArena | 12 | 12 | 16 | 5 | 1 / 1 / 2 / 1 | 4 | 4 | 3 | 7 |
| WanderingMerchantCave | 3 | 7 | 8 | 3 | 1 / 1 / 0 / 1 | 3 | 2 | 1 | 7 |
| MaruTimeShrine | 5 | 7 | 12 | 4 | 1 / 1 / 1 / 1 | 4 | 3 | 2 | 5 |

Forge proof:

- Low와 High witness 모두 `Grind → Mix → Press → MoonlightCure` authored order를 보존한다. High는 TimingOptimization과 MaruAttentionReduction marker만 추가한다.
- MoonCore/CassiaSap/StarNuruk 각 ledger는 `Available → Reserved → Consumed` success와 `Reserved → Returned` failure transition을 갖는다. resource ledger 3개, ledger state 12개, ledger transition 9개다.
- Grind/Mix/Press failure 3개는 각각 `ManualReset → SafeCorridor → 기존 Low Grind node` recovery witness를 가지며 `ReturnsAllForgeInputs=true`다. partial/permanent loss는 0이다.
- MoonSeal output은 exact `SR_SLOT_MOON_SEAL_REWARD`, amount 1, `Required=true`다. key는 hardcode/alias 없이 `SpecialPersistenceKey.ForSlot(SR_MOON_SEAL_FORGE_9, Reward, SR_SLOT_MOON_SEAL_REWARD)`의 `SR_STATE_MOON_SEAL_FORGE_9_REWARD_MOON_SEAL_REWARD`와 exact 결합한다.
- MAP13_03 fixed-slot binding과 7-checkpoint safety proof가 같은 slot/key를 보존하고, BossDirection marker만 게시한다. inventory removal, crafting execution, reward grant, save write는 0이다.

Boss proof:

- state/transition은 `GateLocked → GateAccepted → EncounterActive → Defeated` exact 4/4이며, GateAccepted는 MoonSeal requirement marker만 참조하고 inventory를 소비하지 않는다.
- Low는 lower observation/central recovery를, High는 upper platform/falling-object/pressure-device를 지나 동일 Arena/Return으로 끝난다.
- fall/pressure failure 2개는 exact `SL_NODE_BOSS_CENTRAL_RECOVERY`로 복구한다. EncounterReset은 `EncounterActive → EncounterActive`, `PreservesSealAcceptance=true`다.
- EncounterPersistence와 SeparateMaruStateOwner marker가 분리되어 있고 `IntroducesNewMovementRule=false`다. Boss AI/combat/physics/reset 실행과 duplicate benefit/reward risk는 0이다.

Optional landmark proof:

- Merchant는 24×16 local canvas에 ShopSafeZone 1, distinct EntranceCue 2, Low/High/short Return witness를 게시한다. variants는 Alien/Rabbit/Spacefarer/Machine exact 4개이며 RNG selection과 mandatory dependency는 0이다.
- Maru는 ChoicePreview order 0 이후 Ignored/ShortHint/StrongHint transition 3개를 게시한다. StrongHint는 RareTerrainCompass와 MaruAttentionIncrease를 함께 게시하고, failure는 SafeZone으로 복구한다. `PersistentChoice + PreventsReroll`로 revisit reroll/duplicate benefit는 0이다.
- 두 optional plan 모두 `DeferredToMAP14`, placed footprint 없음, world origin/reservation/bridge/fixed-slot/placed ownership claim `0/0/0/0/0`이다. shell/entry/return/safe-zone identity는 state가 바뀌어도 동일하다.

새로 가능해진 기능과 파이프라인 위치:

```text
MAP13_01 placed site bridge
→ MAP13_02 entry/buffer/collision evidence
→ MAP13_03 fixed/slot/persistence evidence
→ MAP13_06 exact core-resource identities
→ MAP13_07 exact-four landmark catalog + shell/route/state/reset proof compile
→ 별도 검수 후 MAP13_08 validator/preview

optional local definition
→ MAP13_07 DeferredToMAP14 immutable local plan
→ 이후 승인된 MAP14 placement consumer
```

이제 downstream은 Forge/Boss의 placed source identity를 바꾸지 않고 공정·gate·encounter·recovery를 검증할 수 있고, Merchant/Maru는 가짜 world binding 없이 local shell/state만 안전하게 소비할 수 있다. any validation error는 plan과 digest를 게시하지 않고 stable canonical errors만 반환한다.

아직 미구현한 범위는 physical CSV migration/schema/serializer, MAP03/MAP14 placement 선택, boulder/workstation/device/physics, Boss AI/combat/damage/falling collider, item consume/crafting/reward, NPC spawn/shop price/stock/purchase, hint search/rare-terrain selection/Maru AI·attention, UI/save-load, Prefab/Scene/Tilemap/visual effect, MAP13_08+다.

Editor/게임 가시성: 신규 API는 `Game.Map.Runtime`의 pure data/catalog/compiler와 `Game.Map.Tests.EditMode` focused test에만 존재한다. EditorWindow/importer/Inspector asset, Scene object, Prefab, Tilemap 또는 게임 화면 변화는 없으므로 Editor와 게임 플레이에서 보이는 변경은 0이다.

## Responsibility and Added Functions

| Field | Evidence |
|---|---|
| Task responsibility | four landmark shell/route/state/reset starter compile; Forge resource-return/MoonSeal proof; Boss gate/reset/fall proof; Merchant/Maru deferred local-return/state proof |
| Added scripts | Runtime 3: `SpecialLandmarkRegionDefinitions.cs`, `SpecialLandmarkRegionCompiler.cs`, `SpecialLandmarkRegionStarterCatalog.cs`; focused test 1: `SpecialLandmarkRegionAuthoringTests.cs`; matching meta 각각 3/1 |
| Added functions | model constructors는 authored field→canonical immutable model, catalog lookup은 kind→exact definition, digest methods는 semantic component→SHA-256, `Compile`은 definition+allowed sources→placed/deferred plan 또는 stable atomic errors를 담당한다. |
| Inputs consumed | Forge/Boss MAP13_01 `SpecialRegionSiteBridge`, MAP13_02 entry-buffer/collision, MAP13_03 fixed-slot/persistence safety, MAP13_06 exact-three resource definitions; Merchant/Maru local definitions과 null world source |
| Outputs produced | read-only exact-four catalog, compiled design/shell/route/state/reset/marker plan, binding status, witnesses, source/component/aggregate digests, stable errors, zero mutation/solver/gameplay counters |
| Explicit non-ownership | CSV/schema, reservation/placement selection, pathfinding/physics/device/AI/combat/item/shop/UI/save, Prefab/Scene/Tilemap/visual gameplay |
| Downstream consumer | MAP13_08은 별도 검수와 patch 후에만 이 immutable plan을 소비할 수 있으며 현재 `LOCKED`다. |

## Focused Verification

최종 authoritative Unity selection:

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP13_07]
job_id: c2ad199e13984a3a92fe99b1fc80301c
discovered: 12
executed: 12
passed: 12
failed: 0
skipped: 0
inconclusive: 0
resultState: Passed
durationSeconds: 2.5082195
compile errors: 0
relevant Console errors after final verification: 0
relevant Console warnings after final verification: 0
```

동일 `MAP13_07` filter의 첫 실행 job `fe98567e962042dcb74906899b9fafc5`는 신규 compiler가 Forge reset의 1차 SafeCorridor를 Low route node로 직접 요구하여 OneTimeSetUp 12건이 실패했다. Task 계약의 explicit `Failure → SafeCorridor → 기존 Low node` recovery witness 전체를 검증하도록 신규 compiler만 수정했다. 최종 실행은 12/12 PASS이며 다른 category나 test mode는 선택하지 않았다. Test Framework가 `Saving results to: ...TestResults.xml`을 Exception type으로 남긴 알림 1건은 compile/test 오류가 아니었고, 이를 기록한 뒤 Console clear 및 relevant error 0을 재확인했다.

Focused cases는 exact-four matrix/count, four grid bounds/connectivity, Forge source/process/ledger/reset/reward key, Boss gate/encounter/fall/new-movement 0, Merchant safe/cues/variants/RNG 0, Maru preview/choice/persistence, optional world claims 0, invalid identity/binding/chunk/graph/state/reset/resource/seal/recovery/dependency atomic failure, reverse/repeat/`tr-TR`/caller mutation/digest, all mutation/solver/gameplay counters 0을 포함한다.

## Static Scope and Handoff

```text
new Runtime C#/meta: 3/3
new focused test C#/meta: 1/1
focused [Test] / Category attributes: 12 / 1
existing C#/test/CSV/meta modifications: 0
new/modified Authoring or Generated CSV/meta: 0
schema registry/test modifications: 0
MAP09/MAP13_01~06 production/test modifications: 0
Scene/Prefab/Tilemap/Settings/Packages changes: 0
duplicate GUID groups: 0
unapplied inbox candidate/diff-check/unrelated staged: 0/0/0
unrelated included paths: 0
Git push: NOT PERFORMED
```

기존 dirty `Constant.slnx`와 untracked `TerrainClusters.meta` 3개는 읽거나 수정하거나 stage하지 않았다. atomic commit에는 installed/archive Task, Runtime/test/meta 8개, 이 Result, finalized Status만 포함한다.

REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0

Commit subject: `MAP13_07: author Forge Boss and optional regions`

Push: NOT PERFORMED
