```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP13_06_AUTHOR_THREE_CORE_RESOURCE_REGIONS
  task_file: TASKS/MAP13_06_AUTHOR_THREE_CORE_RESOURCE_REGIONS.md
  requires_current_task: NONE
  requires_completed_task: MAP13_05_IMPLEMENT_VILLAGE_STATE_VARIANTS
  requires_result:
    path: REPORTS/MAP13_05_IMPLEMENT_VILLAGE_STATE_VARIANTS_RESULT.md
    status: PASS
    sha256: 005ad4993c1db449b6199f1e0d2842d10465b8f48b2eec78a37542c5038a9fa6
  requires_installed_task:
    path: TASKS/MAP13_05_IMPLEMENT_VILLAGE_STATE_VARIANTS.md
    sha256: 3bddc96bb417a2a575472b81c8729f2ac2de52804df5f52f02dc97b914505f58
  sets_current_task: MAP13_06_AUTHOR_THREE_CORE_RESOURCE_REGIONS
```

# MAP13_06 — Author Three Core Resource Regions

```text
TASK: MAP13_06_AUTHOR_THREE_CORE_RESOURCE_REGIONS
PHASE: MAP13 — SpecialRegion / Village / Mandatory Landmarks
STATUS: CURRENT
NEXT: MAP13_07_AUTHOR_FORGE_BOSS_AND_OPTIONAL_REGIONS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP13_01~03의 placed CoreResource region authority 위에 MoonCore, CassiaSap, StarNuruk의 starter 환경 해법을 명시적으로 작성하고 immutable plan으로 compile한다.

```text
MAP13_01 site bridge
+ MAP13_02 entry/buffer/collision evidence
+ MAP13_03 fixed/slot layer and required Reward safety proof
+ exact-one starter definition from the additive catalog
→ low route + high route + failure recovery + required reward proof
```

이번 Task는 기존 schema를 변경하거나 이전 category를 재검증하지 않는다. 현재 승인된 SpecialRegion CSV 4-table schema에는 low/high/recovery solution graph를 표현할 authority가 없으므로, 이를 unregistered CSV나 문자열 추론으로 우회하지 않는다. starter content는 신규 Runtime typed catalog에 격리하고 physical CSV migration은 별도 승인 범위로 남긴다.

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 정확한 script 경로, class/method별 input→output, 지역별 실제 node/edge/recovery/reward 수, 새 기능, 파이프라인 위치, 미구현, Editor/게임 가시성을 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| 세 CoreResource starter definition | MAP03 reservation 선택/placement |
| 36×16 design canvas와 five active design chunks | 48×32 SectorCanvas/GeneratedSlice 생성 |
| explicit low/high/recovery solution graph | 자동 pathfinding/carve/physics 판정 |
| 맨몸 환경 해법과 optional mastery route | tool/inventory/item-use 실행 |
| exact required Reward와 persistence proof binding | reward 지급/SaveData I/O |
| immutable plan/catalog/digest/error | Prefab/Scene/Tilemap/visual gameplay |

Village 방문, 상점, 도구, 폭발물, 물뿌리개, Activity/Event는 필수 자원 획득의 dependency가 될 수 없다.

## 2. Focused-Only and No-Regression Policy

정상 실행은 EditMode category `MAP13_06`만 선택한다.

```text
MAP13_06 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13_01~05 selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
```

current public API를 신규 focused test 안에서 호출하는 것은 과거 category 재실행이 아니다.

- 신규 파일 자체 문제는 신규 파일 안에서 수정하고 `MAP13_06`만 재실행한다.
- upstream defect를 발견하면 기존 파일을 수정하거나 회귀를 실행하지 않는다.
- owner/invariant/reason과 필요한 최소 확인 범위를 Result에 기록하고 `BLOCKED`로 STOP한다.
- schema 부족을 이유로 MAP09 registry/test를 수정하거나 승인 없이 schema repair를 만들지 않는다.

## 3. Read-Only Preflight

```text
MAP13_05 Result: PASS
MAP13_05 Result SHA-256:
005ad4993c1db449b6199f1e0d2842d10465b8f48b2eec78a37542c5038a9fa6

MAP13_05 installed Task SHA-256:
3bddc96bb417a2a575472b81c8729f2ac2de52804df5f52f02dc97b914505f58

MAP13_05 COMPLETE / MAP13_06 CURRENT / MAP13_07 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required authority:

```text
MAP09 SpecialRegionKind.CoreResource, 1×1 footprint, slot/persistence/AccessClass contracts
MAP13_01 SpecialRegionSiteBridge and region-wide coordinate projection
MAP13_02 SpecialRegionEntryBufferPlan and CoreResource priority evidence
MAP13_03 SpecialRegionFixedSlotLayerPlan and SpecialRegionRequiredResourceSafetyProof
Sector 48×32; PDF design reference 36×16 / logical 12×8 design chunks 5
```

required public authority가 없거나 기존 source 수정이 필요하면 `BLOCKED`다.

## 4. Exact Write Boundary

정상 범위는 Runtime 3개, focused test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/CoreResourceRegionDefinitions.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/CoreResourceRegionCompiler.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/CoreResourceRegionStarterCatalog.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SpecialRegions/CoreResourceRegionAuthoringTests.cs(.meta)
```

```text
Runtime: Game.Map.Runtime / StarNight.Map.WorldGeneration.SpecialRegions
Tests: Game.Map.Tests.EditMode / StarNight.Map.Tests.EditMode.WorldGeneration.SpecialRegions
Category: MAP13_06
```

수정·생성 금지:

```text
existing C# / test / CSV / meta
V2AuthoringSchemaRegistry and its tests
Authoring / Generated CSV and meta
asmdef / asmref
Scene / Prefab / ScriptableObject / Tilemap / Material / Texture
Settings / Packages
```

Editor importer/window, schema descriptor, helper 파일, serializer는 추가하지 않는다.

## 5. Exact Starter Catalog

catalog은 exact 세 entry를 canonical ID 순서로 게시한다.

| Region ID | Resource | Biome | Kind | Reserved footprint | Design canvas | Active design chunks |
|---|---|---|---|---|---|---:|
| `SR_MOON_CORE_SITE_5` | `MoonCore` | `MoonCrater` | `CoreResource` | `1×1 = 48×32` | `36×16` | 5 |
| `SR_CASSIA_SAP_SITE_5` | `CassiaSap` | `CassiaRoot` | `CoreResource` | `1×1 = 48×32` | `36×16` | 5 |
| `SR_STAR_NURUK_SITE_5` | `StarNuruk` | `MoonDough` | `CoreResource` | `1×1 = 48×32` | `36×16` | 5 |

PDF의 `SM_*` 표기는 design reference일 뿐 MAP09 stable ID 형식이 아니다. Runtime identity에는 위 `SR_*` ID만 사용하고 filename/display text에서 변환하지 않는다.

### 5.1 Design canvas versus placed footprint

- 각 region의 reservation/coordinate authority는 exact `1×1 Sector = 48×32`다.
- `36×16 / active chunks 5`는 내부 환경 해법을 작성하는 design canvas metadata다.
- design canvas는 exact logical `3×2` grid of `12×8`, 그중 unique active offsets 5개를 explicit하게 가진다.
- design canvas의 region-wide origin은 explicit `(6,8)`이며 bounds는 `x=6..41`, `y=8..23`이다.
- Entry/Return connector는 MAP13_02 source evidence가 sector exterior와 design canvas를 연결한다. compiler가 connector를 생성하거나 carve하지 않는다.
- logical design chunks는 MAP09 `GeneratedSlice`가 아니며 Authoring source로 역승격하지 않는다.

각 region의 active offsets는 catalog에 explicit하게 저장하고 exact 5/6 coverage, no duplicate, 4-neighbor connected를 검증한다. inactive cell을 암묵적 active로 채우지 않는다.

## 6. Core Resource Solution Model

프로젝트 naming/style에 맞추되 다음 semantic surface를 제공한다.

```text
CoreResourceKind: MoonCore / CassiaSap / StarNuruk
CoreResourceRouteKind: Low / High / Recovery
CoreResourceMechanismKind: ImpactChain / WaterChannel / FermentationPressure
CoreResourceNodeRole:
  Entry / EnvironmentTrigger / MasteryTrigger / Failure / RecoveryJoin
  RequiredReward / OptionalBenefit / Return

CoreResourceDesignChunk
CoreResourceSolutionNode
CoreResourceSolutionEdge
CoreResourceRouteDefinition
CoreResourceRecoveryDefinition
CoreResourceRewardDefinition
CoreResourceRegionDefinition
CoreResourceRegionPlan
CoreResourceRegionCompileRequest
CoreResourceRegionCompiler.Compile
CoreResourceRegionStarterCatalog
CoreResourceRegionCanonicalDigest
CoreResourceRegionErrorCode / Error / Result
```

모든 node/edge/route/reward/slot ID와 region-wide coordinate는 catalog의 explicit data다. ID 이름, 배열 순서, display string 또는 좌표 근접성으로 역할을 추론하지 않는다.

### 6.1 Common graph requirements

각 definition은 다음을 exact 만족한다.

- Low, High, Recovery route definition 각각 최소 1개
- Low와 High는 explicit Entry에서 시작하여 같은 exact required Reward를 거쳐 Return으로 끝남
- Low route의 모든 mandatory edge는 `AccessClass.MandatoryNoTool`
- High route는 optional mastery path이며 실패해도 required Reward를 잃지 않음
- 모든 Failure node는 explicit Recovery route를 통해 Low route의 existing RecoveryJoin으로 복귀
- route edge는 explicit from/to node, order, route kind, access class, mechanism과 required/optional flag를 보존
- node coordinate는 48×32 region 안이고 design canvas 또는 approved Entry/Return connector evidence에 속함
- fixed collision과 겹치지 않고 required Reward node는 exact MAP13_03 required Reward slot을 참조
- synthetic edge, teleport, carve, auto-search, RNG 선택은 0

### 6.2 MoonCore starter

```text
mechanism: ImpactChain
low: surrounding MoonBoulder / Mortar environmental triggers open meteor shell
high: one chained impact also resolves vein + enemy cue + secret pocket benefit
failure: device reset → existing low-route recovery join
required reward: MoonCore exact 1
optional benefit markers: MoonIron, AuxiliaryBattery
forbidden mandatory dependency: Pickaxe / Explosive / Village / Inventory
```

Pickaxe와 Explosive는 향후 optional compatibility가 될 수 있지만 low-route edge나 required proof에는 포함하지 않는다.

### 6.3 CassiaSap starter

```text
mechanism: WaterChannel
low: three explicit root-channel triggers are supplied one by one → central sap pipe
high: one mastery water-flow adjustment opens bonus root + shortcut benefit
failure: wrong connection → manual reset → existing low-route recovery join
required reward: CassiaSap exact 1
optional benefit markers: RecoveryPickup, HiddenSeed
forbidden mandatory dependency: WateringCan / Village / Inventory
```

세 root trigger는 distinct stable ID와 explicit order를 가지며 compiler가 수로나 순서를 추론하지 않는다.

### 6.4 StarNuruk starter

```text
mechanism: FermentationPressure
low: explicit valves + safe platforms in authored order → safe pressure release
high: bounce chain combines pressure release + rare benefit acquisition
failure: overpressure → lower recovery room → existing low-route recovery join
required reward: StarNuruk exact 1
optional benefit markers: Fuel, RareFermentationItem
forbidden mandatory dependency: Explosive / Village / Inventory
```

gas warning cue와 recovery room marker는 required하다. 이 Task는 gas damage, bounce physics 또는 valve MonoBehaviour를 실행하지 않는다.

## 7. Required Reward and Persistence Binding

각 region은 exact 하나의 required Reward definition을 가진다.

```text
MoonCore:   SR_SLOT_MOON_CORE_REWARD   / SR_STATE_MOON_CORE_REWARD
CassiaSap:  SR_SLOT_CASSIA_SAP_REWARD  / SR_STATE_CASSIA_SAP_REWARD
StarNuruk:  SR_SLOT_STAR_NURUK_REWARD  / SR_STATE_STAR_NURUK_REWARD
```

- slot은 source MAP13_03 plan의 `Reward`, `Required=true`와 exact 일치한다.
- persistence key는 stable, non-default, `Reward` scope이고 source safety proof와 exact 일치한다.
- Initial available, interrupt/fail/regenerate available, Claimed/Revisited claimed가 보존된다.
- low/high/failure/recovery 어느 route에서도 permanent loss와 duplicate claim risk는 0이다.
- optional benefit marker는 required Reward로 승격하거나 persistence owner가 될 수 없다.
- compiler는 reward 지급, inventory 변경, save/load를 실행하지 않는다.

## 8. Compilation, Digest and Atomic Failure

Input:

```text
exact CoreResourceRegionDefinition
MAP13_01 bridge + expected digest
MAP13_02 entry-buffer/collision plan + expected digest
MAP13_03 fixed-slot plan + expected digest
MAP13_03 required-resource safety proof + expected digest
```

Success output:

```text
region/resource/biome/design identity
five active design chunks
canonical node/edge/Low/High/Recovery graph
required Reward and optional benefit markers
entry→trigger→reward→return witnesses
failure→recovery→low-route witnesses
source and per-component digests
aggregate canonical digest
zero mutation/solver/tool-dependency counters
```

Collections은 defensive-copy/read-only/canonical order다. same input/reverse enumeration/repeat/`tr-TR`는 same semantic plan/digest를 게시한다. display text, PDF polyline, time, object identity, Unity lifecycle은 digest에서 제외한다.

Any error는 plan/digests `0`; errors는 accumulated, deduped, stable-sorted다. partial route, fallback content, implicit tool edge 또는 reward replacement를 게시하지 않는다.

Minimum error groups:

```text
MissingInput | DigestMismatch | NotCoreResource | RegionIdentityMismatch
UnsupportedFootprint | InvalidDesignCanvas | InvalidActiveChunk
DuplicateNode | InvalidNodeCoordinate | DuplicateEdge | InvalidRoute
MissingLowRoute | MissingHighRoute | MissingRecoveryRoute
MissingEnvironmentSolution | MandatoryToolDependency | UnrecoverableFailure
MissingRequiredReward | RewardSlotMismatch | PersistenceMismatch
RequiredResourcePermanentlyLost | DuplicateRewardRisk | NonCanonicalPublication
```

## 9. Focused Tests

public MAP13 source fixtures와 exact starter catalog로 최소 다음을 검증한다.

1. exact catalog 3개, ID/resource/biome/kind/1×1/36×16/active 5 matrix
2. design origin `(6,8)`, 3×2 logical grid, five active offsets의 bounds/connectivity/uniqueness
3. MoonCore environment low/high/recovery graph와 exact required/optional rewards
4. CassiaSap three ordered root channels, mastery flow, manual reset
5. StarNuruk valve/safe-platform low route, bounce high route, gas cue/recovery room
6. 세 Low route의 `MandatoryNoTool`, Village/tool/inventory dependency 0
7. Entry→trigger→required Reward→Return과 reverse static graph witness
8. 모든 Failure→RecoveryJoin과 required resource preservation
9. MAP13_03 exact required Reward slot/key/checkpoint proof binding
10. invalid ID/biome/footprint/chunk/node/route/tool/recovery/reward/persistence 원자 실패
11. reverse/repeat/culture/immutability/digest 안정성
12. pathfinding/carve/RNG/world/tile/Scene/Prefab/inventory/save mutation 0

PDF의 초록/주황 polyline 좌표를 golden source로 사용하지 않는다. actual physics, item use, device behavior 또는 CSV parser를 test 안에 복제하지 않는다.

## 10. Verification and Required Result

Unity refresh/compile 후 `MAP13_06` EditMode만 실행한다.

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
MapDesign/MCP/REPORTS/MAP13_06_AUTHOR_THREE_CORE_RESOURCE_REGIONS_RESULT.md
```

상단 verdict:

```text
TASK: MAP13_06_AUTHOR_THREE_CORE_RESOURCE_REGIONS
STATUS: PASS | BLOCKED
MAP13_06: COMPLETE ELIGIBLE | NOT COMPLETE
MAP13_07_AUTHOR_FORGE_BOSS_AND_OPTIONAL_REGIONS: LOCKED / DO NOT START
```

`## User-Facing Implementation Report`에서 다음을 실제 수치로 보고한다.

- 신규/수정 script 전체 경로와 class/method별 input→output
- 세 region별 active chunk/node/edge/Low/High/Recovery/failure/reward 실제 개수
- 각 환경 해법과 MandatoryNoTool 결과
- Entry→trigger→reward→return 및 failure→recovery 증명
- required Reward slot/key/checkpoint와 permanent loss/duplicate risk 결과
- 새로 가능해진 것과 파이프라인 위치
- 아직 미구현한 physical CSV migration, device/physics/reward/save, Prefab/Tilemap, MAP13_07+
- Editor/게임 가시성

`## Responsibility and Added Functions`에는 아래를 표로 보고한다.

| Field | Required evidence |
|---|---|
| Task responsibility | 세 CoreResource starter definition + solution/recovery/reward proof compile |
| Added scripts | Runtime 3 + focused test 1 exact paths |
| Added functions | public type/method별 sole responsibility와 input→output |
| Inputs consumed | MAP13_01 bridge + MAP13_02 plan + MAP13_03 layers/safety + starter definition |
| Outputs produced | immutable three-entry catalog and compiled solution plans/digests/errors |
| Explicit non-ownership | CSV/schema, pathfinding/physics, tool/item/device execution, reward/save, Prefab/Tilemap |
| Downstream consumer | 별도 검수 후 MAP13_07만 unlock 가능 |

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
Subject: MAP13_06: author three core resource regions
Push: NOT PERFORMED
```

Result가 PASS여도 MAP13_07을 자동 시작하지 않고 별도 검수까지 LOCKED로 유지한다.
