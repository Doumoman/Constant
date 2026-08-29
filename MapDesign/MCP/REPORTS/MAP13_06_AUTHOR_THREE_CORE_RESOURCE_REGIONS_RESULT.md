TASK: MAP13_06_AUTHOR_THREE_CORE_RESOURCE_REGIONS
STATUS: PASS
MAP13_06: COMPLETE ELIGIBLE
MAP13_07_AUTHOR_FORGE_BOSS_AND_OPTIONAL_REGIONS: LOCKED / DO NOT START

## User-Facing Implementation Report

MAP13_01의 placed site bridge, MAP13_02의 Entry/Return·quiet buffer·collision plan, MAP13_03의 fixed-slot layer와 required-resource safety proof 위에 MoonCore, CassiaSap, StarNuruk 세 CoreResource starter 환경 해법을 immutable typed catalog와 atomic compiler로 추가했다. 각 definition은 `1×1 Sector = 48×32` placement authority와 별개의 `36×16` design canvas, explicit five active `12×8` chunks, Low/High/Recovery graph, failure branch, exact required Reward와 optional benefit marker를 가진다.

repair provenance:

```text
original MAP13_06 Task SHA-256: ec7a880f0239819025b9df6f3b9021143523721003c24b026f2e9dce6054ccbb
MAP13_06R repair SHA-256: 57671bf7a031f89e93d9ad830a0a41c9cd229f8cf22dc41ce6a42d977d7bdb0d
prior BLOCKED Result SHA-256: 39faac2bac212de87405d944427bb0ce4c514a2544c48b0aaf84c10976d1c296
```

repair가 supersede한 persistence identity는 다음과 같다.

| Region | Required Reward slot | 잘못된 short key | corrected authoritative key | `ForSlot` exact |
|---|---|---|---|---|
| `SR_MOON_CORE_SITE_5` | `SR_SLOT_MOON_CORE_REWARD` | `SR_STATE_MOON_CORE_REWARD` | `SR_STATE_MOON_CORE_SITE_5_REWARD_MOON_CORE_REWARD` | PASS |
| `SR_CASSIA_SAP_SITE_5` | `SR_SLOT_CASSIA_SAP_REWARD` | `SR_STATE_CASSIA_SAP_REWARD` | `SR_STATE_CASSIA_SAP_SITE_5_REWARD_CASSIA_SAP_REWARD` | PASS |
| `SR_STAR_NURUK_SITE_5` | `SR_SLOT_STAR_NURUK_REWARD` | `SR_STATE_STAR_NURUK_REWARD` | `SR_STATE_STAR_NURUK_SITE_5_REWARD_STAR_NURUK_REWARD` | PASS |

catalog은 key 문자열 조합, alias 또는 mapping dictionary를 가지지 않는다. `CoreResourceRegionStarterCatalog.Reward`가 `SpecialPersistenceKey.ForSlot(regionId, Reward, slotId)`을 직접 호출하고, compiler가 같은 public authority의 결과를 authored definition·MAP13_03 placed Reward slot·safety proof와 exact 비교한다. 세 short key의 신규 Runtime/test source 출현 수는 0이다.

추가한 모든 script와 책임·input→output:

| Script | Class/method | 책임 | Input → Output |
|---|---|---|---|
| `Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/CoreResourceRegionDefinitions.cs` | `CoreResourceKind`, `CoreResourceRouteKind`, `CoreResourceMechanismKind`, `CoreResourceNodeRole`, marker/benefit/dependency enums | resource, route, mechanism, explicit semantic marker와 non-dependency vocabulary | authored enum value → canonical typed identity |
| 같은 파일 | `CoreResourceDesignChunk` | 3×2 design grid offset value | `(x,y)` → comparable immutable chunk offset |
| 같은 파일 | `CoreResourceSolutionNode`, `CoreResourceSolutionEdge` | node role/coordinate/marker/order/Reward slot과 edge endpoint/order/route/access/mechanism/required/dependency 보존 | explicit authored fields → immutable graph element |
| 같은 파일 | `CoreResourceRouteDefinition`, `CoreResourceRecoveryDefinition` | main route edge membership과 mastery→failure→Recovery route→existing Low join binding | explicit IDs → immutable route/recovery definition |
| 같은 파일 | `CoreResourceRewardDefinition`, `CoreResourceOptionalBenefitDefinition` | exact-one required Reward 및 persistence-free optional marker | resource/slot/key/node 또는 benefit/node → immutable reward marker |
| 같은 파일 | `CoreResourceRegionDefinition` | region/biome/footprint/design/graph/reward starter aggregate의 defensive copy와 canonical order | explicit starter collections → immutable definition |
| 같은 파일 | `CoreResourceRegionCompileRequest` | MAP13_01~03 source와 expected digest를 definition에 결합 | definition + bridge + entry/collision + layer + safety proof → compile input |
| 같은 파일 | `CoreResourceRegionPlan`, `CoreResourceRouteWitness` | validated definition, source digests, forward/reverse/failure witnesses와 zero-mutation counters 게시 | validated semantic graph + sources → immutable downstream plan |
| 같은 파일 | `CoreResourceRegionErrorCode`, `CoreResourceRegionError`, `CoreResourceRegionResult` | accumulated/deduplicated/stable-sorted atomic failure 또는 success plan | validation findings → plan or canonical errors, never partial plan |
| 같은 파일 | `CoreResourceRegionCanonicalDigest.ComputeDefinition/Compute/ComputeDesign/ComputeGraph/ComputeReward` | display text, time, object identity와 무관한 SHA-256 material 생성 | definition/plan components → lowercase 64-hex digest |
| `Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/CoreResourceRegionCompiler.cs` | `CoreResourceRegionCompiler.Compile` | identity/digest/footprint/chunk/coordinate/graph/route/environment/recovery/reward/persistence/source-chain을 검증하고 witness를 작성 | `CoreResourceRegionCompileRequest` → `CoreResourceRegionResult` |
| `Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/CoreResourceRegionStarterCatalog.cs` | `Entries`, `CanonicalDigest`, `TryGetDefinition`, `GetDefinition` | exact-three canonical starter publication과 lookup | none 또는 region/resource identity → read-only catalog/definition |
| `Assets/_Game/Tests/EditMode/Map/WorldGeneration/SpecialRegions/CoreResourceRegionAuthoringTests.cs` | `CoreResourceRegionAuthoringTests`의 12 focused tests | public MAP13 source fixture, exact catalog/graph/key/checkpoint, invalid atomic failure, reverse/repeat/culture/immutability/digest/zero mutation 검증 | public MAP13 inputs + catalog → NUnit PASS/diagnostic |

지역별 실제 게시 수:

| Region | Active chunks | Nodes | Total edges | Low routes / edges | High routes / main edges | Failure branch edges | Recovery routes / edges | Failure nodes | Required Reward | Optional benefits |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| MoonCore | 5 | 14 | 16 | 1 / 5 | 1 / 8 | 1 | 1 / 2 | 1 | 1 | 2 |
| CassiaSap | 5 | 15 | 17 | 1 / 7 | 1 / 7 | 1 | 1 / 2 | 1 | 1 | 2 |
| StarNuruk | 5 | 15 | 18 | 1 / 8 | 1 / 7 | 1 | 1 / 2 | 1 | 1 | 2 |

환경 해법과 route proof:

- MoonCore `ImpactChain`: Low는 RecoveryJoin→MoonBoulder→Mortar→MoonCore Reward, High는 ChainedImpact→Vein→EnemyCue→SecretPocket→MoonIron/AuxiliaryBattery→동일 Reward다. 실패 branch는 DeviceReset을 거쳐 기존 Low RecoveryJoin으로 복구한다.
- CassiaSap `WaterChannel`: Low는 authored order `1→2→3`의 distinct RootChannel과 central SapPipe를 거친다. High는 MasteryWaterFlow→BonusRoot→Shortcut→RecoveryPickup/HiddenSeed→동일 Reward다. wrong connection 실패는 ManualReset을 거쳐 기존 Low RecoveryJoin으로 복구한다.
- StarNuruk `FermentationPressure`: Low는 Valve→SafePlatform→Valve→required GasWarning→PressureRelease→Reward다. High는 BounceChain 두 단계→PressureRelease→Fuel/RareFermentationItem→동일 Reward다. overpressure 실패는 required RecoveryRoom을 거쳐 기존 Low RecoveryJoin으로 복구한다.
- 세 Low route의 모든 edge는 `Required=true`, `AccessClass.MandatoryNoTool`, `Dependency=None`이다. Pickaxe, Explosive, WateringCan, Village, Inventory dependency 수는 모두 0이다.
- Low/High는 같은 explicit Entry에서 시작하고 exact required Reward를 거쳐 Return으로 끝난다. ordered edge chain으로 forward witness를 만들고 같은 static edge를 역방향 조회할 수 있음을 검증했다.
- 각 Failure는 optional mastery branch에서 exact 하나의 Recovery definition으로 연결되며, Recovery route는 `MandatoryNoTool`로 기존 Low `RecoveryJoin`에 끝난다.

required Reward persistence 결과:

```text
required Reward definitions per region: 1
required Reward amount per region: 1
MAP13_03 required Reward slot/key/scope exact match: 3/3
checkpoint evidence per region: 7
Initial available: 3/3
Interrupted/Failed/Regenerated available: 9/9
Claimed/Revisited claimed: 6/6
permanent loss count: 0
duplicate reward risk count: 0
reward grant / inventory mutation / save write count: 0 / 0 / 0
```

새로 가능해진 기능과 파이프라인 위치:

```text
MAP13_01 placed bridge
→ MAP13_02 entry/buffer/collision evidence
→ MAP13_03 fixed-slot layer + persistence safety proof
→ MAP13_06 exact-three starter catalog + solution/recovery/reward proof compile
→ 별도 검수 후 MAP13_07 downstream authoring
```

이제 downstream은 세 핵심 자원 지역의 placement authority를 바꾸지 않고, exact five design chunks, 맨몸 Low, optional mastery High, failure recovery, required Reward 보존 여부를 하나의 deterministic immutable plan/digest/error surface로 검증할 수 있다. invalid input은 accumulated stable errors만 게시하고 plan과 모든 output digest를 비운다.

아직 미구현한 범위는 physical CSV migration/registry, 실제 boulder·mortar·water channel·valve·gas damage·bounce physics·device MonoBehaviour, tool/item 사용, Reward 지급, inventory/save-load, Prefab/Scene/Tilemap/visual effect, runtime spawn/population, MAP13_07 Forge/Boss/optional region이다.

Editor/게임 가시성: 신규 API는 `Game.Map.Runtime`의 data/compiler와 `Game.Map.Tests.EditMode`의 focused test에만 존재한다. Editor window/importer/Inspector asset, Scene object, Prefab, Tilemap 또는 게임 화면 변화는 없으므로 Editor와 게임 플레이에서 시각적으로 보이는 변경은 0이다.

## Responsibility and Added Functions

| Field | Evidence |
|---|---|
| Task responsibility | 세 CoreResource starter definition과 explicit Low/High/Recovery, failure recovery, exact required Reward proof를 MAP13_01~03 source 위에서 compile한다. |
| Added scripts | Runtime 3: `CoreResourceRegionDefinitions.cs`, `CoreResourceRegionCompiler.cs`, `CoreResourceRegionStarterCatalog.cs`; focused test 1: `CoreResourceRegionAuthoringTests.cs`; matching meta 각각 3/1. |
| Added functions | immutable model constructors는 authored field→canonical model, catalog lookup은 region/resource→exact definition, digest methods는 semantic component→SHA-256, `Compile`은 definition+MAP13_01~03 source→plan 또는 stable atomic errors를 담당한다. |
| Inputs consumed | MAP13_01 `SpecialRegionSiteBridge`; MAP13_02 `SpecialRegionEntryBufferPlan`과 `SpecialRegionPlacementCollisionPlan`; MAP13_03 `SpecialRegionFixedSlotLayerPlan`과 `SpecialRegionRequiredResourceSafetyProof`; exact starter definition과 각 expected digest. |
| Outputs produced | read-only exact-three catalog, compiled identity/design/graph/reward plan, Low/High/Recovery/failure witnesses, source/component/aggregate digests, stable errors, zero mutation/solver/tool counters. |
| Explicit non-ownership | CSV/schema, reservation/pathfinding/carve/physics, tool/item/device 실행, Reward/inventory/save, Prefab/Scene/Tilemap/visual gameplay를 생성·수정하지 않는다. |
| Downstream consumer | MAP13_07은 별도 검수와 patch 후에만 이 immutable plan을 소비할 수 있으며 현재는 `LOCKED`다. |

## Focused Verification

최종 authoritative Unity selection:

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP13_06]
job_id: 6f3983468991435593107eb6ffc47ac2
discovered: 12
executed: 12
passed: 12
failed: 0
skipped: 0
inconclusive: 0
resultState: Passed
durationSeconds: 2.4418657
compile errors: 0
relevant Console errors after final verification: 0
```

동일 `MAP13_06` filter의 task-owned 수정 이력은 투명하게 다음과 같다. 첫 실행은 신규 fixture의 분리 apron 때문에 OneTimeSetUp 12건이 실패했고, 두 번째 시작은 import domain reload와 겹쳐 executed 0의 initialization timeout이었다. 세 번째 실행은 11/12 PASS 후 missing-Low invalid case의 `First` 예외를 발견했다. 신규 test/catalog connector와 신규 compiler atomic failure guard만 수정한 뒤 최종 실행이 12/12 PASS했다. 다른 category나 test mode는 선택하지 않았다.

Focused cases는 exact catalog matrix, 36×16/3×2/five-chunk connectivity, 세 환경 해법, mandatory no-tool Low, Entry→Reward→Return 및 reverse witness, failure→RecoveryJoin, authoritative key/7 checkpoints, invalid identity/footprint/canvas/chunk/node/edge/route/tool/recovery/reward/persistence atomic failure, reverse/repeat/`tr-TR`/caller mutation/digest 안정성, 모든 mutation/solver counter 0을 포함한다.

## Static Scope and Handoff

```text
new Runtime C#/meta: 3/3
new focused test C#/meta: 1/1
existing C#/test/CSV/meta modifications: 0
new/modified Authoring or Generated CSV/meta: 0
schema registry/test modifications: 0
MAP09/MAP13_01~05 production/test modifications: 0
Scene/Prefab/Tilemap/Settings/Packages changes: 0
duplicate GUID: 0
short-key alias/mapping/fallback/surrogate/reflection proof: 0
unapplied inbox candidate/diff-check/unrelated staged: 0/0/0
unrelated included paths: 0
Git push: NOT PERFORMED
```

기존 dirty `Constant.slnx`와 untracked `TerrainClusters.meta` 3개는 읽거나 수정하거나 stage하지 않았다. atomic commit에는 original Task, installed/archive repair, Runtime/test/meta 8개, rewritten Result, finalized Status만 포함한다.

REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0

Commit subject: `MAP13_06: author three core resource regions`

Push: NOT PERFORMED
