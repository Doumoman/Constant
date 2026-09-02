TASK: MAP16_03_VALIDATE_FINAL_ROUTE_AND_RECOVERY
STATUS: PASS
MAP16_03: COMPLETE ELIGIBLE only when PASS
MAP16_04_IMPLEMENT_PATTERN_CHUNK_COORDINATES_AND_PARTITION: LOCKED / DO NOT START

## User-Facing Implementation Report

이번 Task는 MAP16_01의 48x32 final canvas와 MAP16_02의 protection/cleanup/density PASS report를 읽어, final canvas 위의 정적 route/recovery witness를 만드는 검증 계약을 추가했다. 이것은 실제 플레이어 이동, collider/physics, jump simulation, Tilemap bake, 12x8 slice, Scene 또는 gameplay 구현이 아니다.

추가한 script는 다음과 같다.

- `Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorFinalRouteRecoveryReport.cs`: route anchor/node/edge, base/socket/boundary/Special/high/recovery witness, typed failure, atomic result와 canonical digest를 immutable public value로 발행한다.
- `Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorFinalRouteRecoveryValidator.cs`: 공개 final cell layer winner와 MAP16_02 gate를 읽어 passability graph를 구성하고, deterministic base route 및 recovery witness를 검증한다. canvas/report를 변경하지 않으며 실패 시 partial report 없이 typed failure만 반환한다.
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/SectorFinalRouteRecoveryValidatorTests.cs`: `REFERENCE FINAL ROUTE RECOVERY REPORT` fixture로 10개 MAP16_03 focused gate를 검증한다.

새로 가능해진 기능은 final canvas의 AIR/traversable/protected-open cell을 기준으로 한 deterministic static graph, 기본 entry-to-exit witness, external socket/boundary aperture/Special entrance-to-base witness, high failure-to-base recovery witness, blocked crossing과 anchor-isolation softlock 검출, stable lower-hex SHA-256 handoff이다. public final canvas에 high/failure anchor가 별도 API로 노출되지 않은 범위는 Task가 허용한 명시적 reference fixture label로만 투영했으며 production seed 또는 실제 player traversal 승인을 주장하지 않는다.

검증된 reference 수치는 다음과 같다.

```text
sector size observed: 48x32
cells per sector observed: 1536/1536
unique cell coordinates: 1536/1536
route nodes / edges: 768 / 1457
base entry anchors required/covered/missing: 1/1/0
base exit anchors required/covered/missing: 1/1/0
base route witnesses required/covered/missing: 1/1/0
external socket witnesses required/covered/missing: 2/2/0
boundary aperture witnesses required/covered/missing: 1/1/0
special entrance witnesses required/covered/missing: 1/1/0
high failure samples required/covered/missing: 1/1/0
recovery witnesses required/covered/missing: 1/1/0
route cells crossing Solid/Hazard/blocked cells: 0
static softlock candidates: 0
MAP16_02 protection intrusions: 0
MAP16_02 density violations: 0
MAP16_02 unowned AIR violations: 0
MAP16_02 cleanup protected/fixed/boundary/Special changes: 0/0/0/0
fallback carve actions: 0
silent widening actions: 0
sector rerender actions: 0
whole-world rerandom actions: 0
player physics simulations: 0
PlayMode runs: 0
Tilemap bakes: 0
12x8 slices created: 0
production generated file writes: 0
Tilemap/Scene/Prefab/GameObject mutation: 0/0/0/0
gameplay spawn mutations: 0
production seed approvals: 0
full regression runs: 0
```

Digest와 replay evidence:

```text
input digest: 617ace36663f33e8acafa6b1be3dad8c5f89b866e5c84d458375e6484978290b
output digest: 0b071982ca66e90221043a8513175c5bc2cff3d99b05cee9dc960663f57f82be
input/output digest format: 64 lowercase hex / 64 lowercase hex
repeat digest mismatches: 0
reverse upstream claims digest mismatches: 0
reverse route evidence digest mismatches: 0
tr-TR culture digest mismatches: 0
```

실제 Editor/게임 가시성은 없다. Inspector overlay, Scene object, Tilemap, Prefab, GameObject 또는 runtime UI를 추가하지 않았으므로 이 결과는 C# public report와 Unity Test Runner에서만 보인다. 아직 구현하지 않은 범위는 player controller traversal, collider/physics/jump proof, production world seed validation, actual 624x416 world solve, Tilemap bake, 12x8 partition/slice, export, WorldGenerationRoot wiring, Activity/Event/NPC/reward spawn, MAP16 phase exit 및 MAP16_04 작업이다.

## Responsibility and Added Functions

### `Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorFinalRouteRecoveryReport.cs`

- `FinalRouteWitnessKind`, `FinalRouteNodeKind`, `FinalRouteEdgeKind`, `FinalRouteFailureKind`, `FinalRouteRecoveryKind`, `FinalRouteWitnessVerdict`: witness/node/edge/failure/recovery/verdict를 stable enum 이름으로 고정한다.
- `FinalRouteAnchor`: stable id + node kind + final coordinate + public source/protection authority -> immutable anchor와 canonical token.
- `FinalRouteNode`: passable coordinate + role + source owner -> immutable graph node.
- `FinalRouteEdge`: from/to + static edge kind + source owner + directionality -> immutable static edge. Declared edge는 authoring affordance이며 runtime physics proof가 아니다.
- `FinalRouteWitness`: start/end anchor + ordered path -> immutable base/socket/boundary/Special/high witness.
- `FinalRecoveryWitness`: failure anchor + target base anchor + ordered path -> immutable recovery witness와 carve/widen/rerender/rerandom false proof.
- `FinalRouteSoftlockCandidate`: isolated anchor/one-way recovery defect -> sorted typed softlock evidence.
- `FinalRouteRecoveryRequest`: successful MAP16_01 plan + successful MAP16_02 report + public/synthetic-labeled anchors/declared links -> sorted read-only validation input projection. 모든 operation/mutation counter를 명시한다.
- `SectorFinalRouteRecoveryReport`: nodes/edges/witnesses/recoveries/softlocks -> sector, source digest, required/covered/missing, crossing, mutation 및 MAP16_04 handoff count가 포함된 immutable proof packet.
- `FinalRouteRecoveryFailure`: failure code + subject + reason -> comparable/equatable typed failure.
- `FinalRouteRecoveryResult`: request + report-or-null + sorted failures -> atomic success/failure envelope. 실패 시 report와 digest는 비어 있다.
- `FinalRouteRecoveryDigest.ComputeInput`: MAP16_01/02 digest + sector + sorted anchors/edges + policy/counters -> canonical input SHA-256.
- `FinalRouteRecoveryDigest.ComputeOutput`: sorted nodes/edges/witnesses/recoveries/softlocks + counts + downstream owner -> canonical output SHA-256.
- `FinalRouteRecoveryDigest.HashCanonicalText`, `IsLowerHexSha256`: UTF-8/LF/invariant lower-hex digest와 형식 검증을 담당한다.

### `Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorFinalRouteRecoveryValidator.cs`

- `Validate`: immutable request -> successful `SectorFinalRouteRecoveryReport` 또는 typed failures를 가진 atomic result.
- `ValidateSources`: MAP16_01 plan/MAP16_02 report -> source identity, 48x32/1536/7-layer, upstream digest, intrusion/density/unowned-air/cleanup, forbidden-operation gate.
- `BuildPassability`: public final layer winners -> Solid/Hazard/blocked Protection이 제외된 passable cell map.
- `ValidateAnchors`: sorted public/reference anchors + passability/public authority -> exact base entry/exit, unique stable id, in-bounds/passable/protected-authority verdict.
- `BuildNodes`, `BuildEdges`: passable cells + exposed declared affordances -> stable row-major nodes와 sorted orthogonal/declared edges.
- `BuildWitnesses`, `AddAnchorWitnesses`: deterministic adjacency + base anchors -> base/socket/boundary/Special/high route witnesses.
- `FindPath`: stable-sorted adjacency -> deterministic breadth-first static witness path.
- `ValidateOneWayRecovery`: one-way declared edge -> return/recovery witness 또는 typed softlock.
- `ValidateWitnessCells`: 모든 witness path -> Solid/Hazard/blocked Protection crossing failure.

### `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/SectorFinalRouteRecoveryValidatorTests.cs`

- `ReferenceFinalRouteRecoveryFixture`: MAP16_01/02 public reference fixtures + MAP15_02/MAP08/MAP13/MAP14 identity label -> accepted, missing, blocked, isolated, source-mismatch request. production seed/player traversal claim은 하지 않는다.
- focused tests 10개는 report publication, base route, socket/boundary/Special connectivity, high recovery, zero softlock, typed atomic failures, repeat/reverse/culture determinism, no mutation, no physics/PlayMode/Tilemap bake, MAP16_04 lock을 각각 검증한다.

### Public authority consumed and change boundary

- MAP16_01: `SectorFinalCanvasLayerPlan`, request dimensions/sector/upstream digests, public `Cells`, seven public `Winners`, source/protection/counter values.
- MAP16_02: `SectorCanvasProtectionDensityReport`, source plan identity, intrusion/density/unowned-air/cleanup verdict, input/output digest와 mutation counters.
- identity labels: MAP15_02 external sockets, MAP08 boundary aperture, MAP13 Special entrance, MAP14 base/high/recovery reference anchors. 별도 production topology를 발명하거나 physical CSV를 reparsing하지 않았다.
- production change: 신규 Runtime C# 2개와 matching meta 2개만 추가.
- test change: 신규 focused EditMode C# 1개와 matching meta 1개만 추가.
- upstream existing production/test/CSV/meta 수정: 0.
- Editor/CSV/Scene/Prefab/Tilemap/ScriptableObject/ProjectSettings/Packages 수정: 0.
- downstream owner: `MAP16_04_IMPLEMENT_PATTERN_CHUNK_COORDINATES_AND_PARTITION`; 자동 open하지 않는다.

## Focused Verification

```text
Unity: 6000.3.8f1
Unity CLI: 1.0.0-beta.6
mode: EditMode
category_names: [MAP16_03]
discovered: 10
executed: 10
passed: 10
failed: 0
skipped: 0
inconclusive: 0
compile errors: 0
relevant Console errors after final verification/clear: 0
relevant Console warnings after final verification/clear: 0
CLI errors: 0
CLI warnings: 0

REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
FULL REGRESSION RUNS: 0
```

Unity command:

```text
unity test . --mode EditMode --output Logs/MAP16_03-results.xml --timeout 600 --format json -- -testCategory MAP16_03
exit code: 0
test-run result: Passed
testcasecount/total/passed/failed/inconclusive/skipped: 10/10/10/0/0/0
```

Static gates:

```text
required public concepts / additional request projection: 15 / 1
validator public entry points: 1
Runtime C# / matching meta: 2 / 2
focused EditMode C# / matching meta: 1 / 1
focused test methods: 10
duplicate new GUID hits: 0
System.IO/current time/random/UnityEngine/physics/Tilemap/Scene/GameObject API hits: 0
existing C#/test/CSV/meta modifications: 0
Scene/Prefab/Tilemap changes: 0
unrelated staged paths: 0
```

Commit subject: MAP16_03: validate final route recovery
Push: NOT PERFORMED
