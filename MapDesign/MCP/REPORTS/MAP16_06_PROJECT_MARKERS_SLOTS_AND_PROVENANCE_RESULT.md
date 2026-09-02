TASK: MAP16_06_PROJECT_MARKERS_SLOTS_AND_PROVENANCE
STATUS: PASS
MAP16_06: COMPLETE ELIGIBLE only when PASS
MAP16_07_EXPORT_REPLAY_AND_DEBUG_GENERATED_TERRAIN: LOCKED / DO NOT START

## User-Facing Implementation Report

MAP16_05가 공개한 immutable `GeneratedMicroChunkSliceSet`의 16개 slice, 1536개 cell,
10752개 layer record를 그대로 읽어 generated 12x8 MicroChunk용 local marker slot packet으로
투영했다. 성공 출력은 24개 slot이며, 각 slot은 chunk index, local x/y, sector tile x/y,
owner/kind/source key, source layer/claim/cell/slice와 socket/signature/traversal provenance를
보존한다.

이 결과는 메모리 내 generated terrain data packet이다. Editor 또는 게임 화면에 새 오브젝트를
표시하지 않으며 stable spawn id, runtime spawn, 파일 export, Tilemap bake, Scene/Prefab/GameObject
변경을 수행하지 않는다. MAP16_07이 이후 소비할 수 있는 결정적 입력만 제공하며 MAP16_07을
열거나 실행하지 않는다.

## Responsibility and Added Functions

| File / symbol | Responsibility | Input -> output |
|---|---|---|
| `GeneratedMicroChunkMarkerSlotSet.cs` | immutable slot id, cell ref, projection, provenance, aggregate counters, failure/result and SHA-256 digest model | MAP16_05 public slice facts -> stable local slot packet/value identities |
| `GeneratedMarkerSlotId` | sector/chunk/local cell/kind/owner/source-key hash/ordinal 기반 local id | deterministic projection facts -> non-spawn local slot id |
| `GeneratedMarkerSlotProvenance` | source owner/task/layer token/claim/provenance/cell/slice/socket/signature/traversal chain | public layer and slice records -> complete provenance token |
| `MarkerSlotProjectionDigest` | UTF-8, LF, invariant, sorted, lower-hex SHA-256 canonicalization | source/projection or slot packet -> input/output digest |
| `GeneratedMicroChunkMarkerSlotProjector.cs` | public owner records scan, deterministic mapping/sort/project, atomic validation | complete MAP16_05 slice set -> slot set or sorted failures with no partial set |
| `Project` / `Scan` / `TryMapOwner` | scan all public layers, select supported ownership, reject null/orphan/duplicate/missing provenance, preserve identities | source set/projections -> `MarkerSlotProjectionResult` |
| `GeneratedMicroChunkMarkerSlotProjectorTests.cs` | exactly 10 MAP16_06 EditMode proofs | reference public authority chain -> focused evidence |

New Unity files and their matching meta files:

- `Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedMicroChunkMarkerSlotSet.cs(.meta)`
- `Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedMicroChunkMarkerSlotProjector.cs(.meta)`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/GeneratedMicroChunkMarkerSlotProjectorTests.cs(.meta)`

No existing C#, test, CSV, scene, prefab, Tilemap, ScriptableObject, asmdef, package, project setting,
or upstream MAP16_05 file was modified.

## Public Owner Mapping

The projector maps the exact public `FinalCanvasSourceOwner` names preserved by MAP16_05:

| Public source owner | Slot kind / owner | Source task token |
|---|---|---|
| `TerrainCluster` | `TerrainCluster` / `TerrainCluster` | `MAP11_TERRAIN_CLUSTER` |
| `Activity` | `Activity` / `Activity` | `MAP12_ACTIVITY` |
| `SpecialRegion` | `SpecialRegion` / `SpecialRegion` | `MAP13_SPECIAL_REGION` |
| `EventOverlay` | `EventOverlay` / `EventOverlay` | `MAP12_EVENT_OVERLAY` |
| `Boundary` | `Boundary` / `Boundary` | `MAP15_02_BOUNDARY` |
| `MandatoryRoute` | `RouteRecovery` / `RouteRecovery` | `MAP16_03_ROUTE_RECOVERY` |

`Boundary` and `RouteRecovery` are the two optional public owner families observed. `Decoration` is
modeled but was not inferred or emitted because the accepted public input exposes no Decoration owner.

## Projection Evidence

```text
source slices observed: 16/16
source cells observed: 1536/1536
source layer records observed: 10752/10752
marker layer records scanned: 10752
marker layer records consumed: 24
required marker owner families required/covered/missing: 4/4/0
optional marker owner families observed: 2 (Boundary, RouteRecovery)
slots projected: 24
slots with stable local id: 24/24
slots with cell refs: 24/24
slots with provenance: 24/24
slots preserving source layer identity: 24/24
slots preserving socket/signature/traversal identity where applicable: socket 23/23; signature/traversal 24/24
compatible multi-marker cells: 1
duplicate slot ids: 0
duplicate same owner/kind/source key failures verified: 1
orphan marker records: 0
orphan cell failures verified: 1
missing cell ref failures verified: 1
missing provenance: 0
missing provenance failures verified: 1
atomic failure partial slot sets: 0
stable spawn ids created: 0
runtime objects spawned: 0
CSV/JSON generated files: 0/0
Tilemap bakes: 0
Tilemap/Scene/Prefab/GameObject mutation: 0/0/0/0
source slice mutation: 0
input digest: 774f5cdbb8ebbb0a5896f50fcc0d9923f41038b27363a6f7c17ed036aec83366
output digest: 80ef198fe6aefb08d61f6c48cdf0c4f93a80e5a5592c42d1ba51605b2899a90f
repeat/reverse/culture digest mismatches: 0/0/0
production seed approvals: 0
```

The focused fixture is explicitly reference-only. It extends the accepted public authority chain with
deterministic TerrainCluster, Activity and EventOverlay claims and retains the already public
SpecialRegion, Boundary and MandatoryRoute facts. It does not approve production seeds or persisted
content.

## Atomic Failure and Determinism Proof

- Null source returns a failure, null `SlotSet`, and empty output digest.
- A repeated projection verifies both duplicate owner/kind/source key and duplicate slot id failures.
- Missing cell reference, cross-slice orphan cell, and missing provenance each fail atomically.
- Different marker kinds on the same cell are retained and receive distinct deterministic ordinals.
- Repeat, reversed projection enumeration and `tr-TR` culture produce identical input/output digests.
- Failure collections and successful slots are sorted by their stable identities.

## Focused Verification

```text
Unity version: 6000.3.8f1
mode: EditMode
category_names: [MAP16_06]
discovered: 10
executed: 10
passed: 10
failed: 0
skipped: 0
inconclusive: 0
duration seconds: 26.76
compile errors: 0
relevant Console errors after final verification/clear: 0
relevant Console warnings after final verification/clear: 0

REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
FULL REGRESSION RUNS: 0
```

All ten required test names passed under the single `MAP16_06` category. No prior category, legacy,
PlayMode, unfiltered, or full regression selection was run.

## Scope and Handoff

Implemented scope ends at deterministic local slot/provenance projection. Stable spawn IDs, runtime
objects, gameplay attachment, CSV/JSON/debug export, generated assets, save/streaming state,
Tilemap/collider/physics bake and production seed approval remain unimplemented and unapproved.

MAP16_07 remains `LOCKED` and was not started. The PASS-only atomic commit subject is:

```text
MAP16_06: project marker slots provenance
```
