```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP13_04_IMPLEMENT_VILLAGE_SHELL_FACILITIES_AND_ACCESS
  task_file: TASKS/MAP13_04_IMPLEMENT_VILLAGE_SHELL_FACILITIES_AND_ACCESS.md
  requires_current_task: NONE
  requires_completed_task: MAP13_03_SEPARATE_FIXED_SHELL_SLOTS_AND_PERSISTENCE
  requires_result:
    path: REPORTS/MAP13_03_SEPARATE_FIXED_SHELL_SLOTS_AND_PERSISTENCE_RESULT.md
    status: PASS
    sha256: df2dcf6e2b8f048481224cd44c2d8a69233e81c9e1b61f05467db9018999bf2f
  requires_installed_task:
    path: TASKS/MAP13_03_SEPARATE_FIXED_SHELL_SLOTS_AND_PERSISTENCE.md
    sha256: 959e15ed38be4e7486539719daa7661c9bf81bad86b12d726ac9c595a6624bdd
  sets_current_task: MAP13_04_IMPLEMENT_VILLAGE_SHELL_FACILITIES_AND_ACCESS
```

# MAP13_04 — Village Shell, Facilities and Access

```text
TASK: MAP13_04_IMPLEMENT_VILLAGE_SHELL_FACILITIES_AND_ACCESS
PHASE: MAP13 — SpecialRegion / Village / Mandatory Landmarks
STATUS: CURRENT
NEXT: MAP13_05_IMPLEMENT_VILLAGE_STATE_VARIANTS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP13_03의 placed Village fixed/access/slot layer에 caller-authored central road, Kitchen/Repair, optional Facility slot과 explicit access witness를 결합해 immutable Village shell plan을 만든다.

```text
Village SpecialRegion fixed/access/slot layers
+ explicit central-road cells
+ exact Kitchen / Repair + optional Facility definitions
+ explicit door→path→road witnesses
→ validated 1×1 / 2×1 / 1×2 Village shell plan
```

이번 Task는 content를 C# static catalog에 하드코딩하거나 ID 문자열에서 의미를 추론하지 않는다. compiler와 in-memory focused fixtures만 추가하며 actual Authoring CSV, building Prefab, NPC, inventory, Tilemap을 만들지 않는다.

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 정확한 script 경로, class/method별 input→output, 새 기능, 파이프라인 위치, 미구현, Editor/게임 가시성을 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| 1×1/2×1/1×2 Village layout validation | MAP03 reservation 선택/placement |
| explicit central road와 sector seam 연결 | 자동 road/path 탐색·굴착 |
| Kitchen/Repair required binding | 실제 Kitchen/Repair gameplay/Prefab |
| optional Facility slot 3~4개 | optional 시설 종류/재고/NPC 배정 |
| 모든 시설의 door→road→door witness | PlayerController/collider/physics proof |
| immutable shell plan/digest/errors | MAP13_05 state variant |

기존 MAP09/MAP13 contract, fixed shell, collision, persistence와 CSV schema는 변경하지 않는다.

## 2. Focused-Only Policy

정상 실행은 EditMode category `MAP13_04`만 선택한다.

```text
MAP13_04 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13_01~03 selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
```

current public API 호출은 과거 category 재실행이 아니다. upstream defect면 기존 파일을 고치지 말고 owner/invariant/reason/minimum verification을 기록해 `BLOCKED`로 STOP한다. 신규 파일 자체 문제만 고치고 `MAP13_04`만 재실행한다.

## 3. Read-Only Preflight

```text
MAP13_03 Result: PASS
MAP13_03 Result SHA-256:
df2dcf6e2b8f048481224cd44c2d8a69233e81c9e1b61f05467db9018999bf2f

MAP13_03 installed Task SHA-256:
959e15ed38be4e7486539719daa7661c9bf81bad86b12d726ac9c595a6624bdd

MAP13_03 COMPLETE / MAP13_04 CURRENT / MAP13_05 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required authority:

```text
MAP13_01 Village SpecialRegionSiteBridge and region-wide coordinates
MAP13_02 Entry/Return/apron/bidirectional plan
MAP13_03 FixedCollision, FixedAccess, Facility slot and immutable digests
MAP09 SpecialRegionKind.Village, slot IDs/kinds, AccessClass.MandatoryNoTool
Sector 48×32 and footprint 1×1 / 2×1 / 1×2
```

input region이 Village가 아니거나 current digest identity가 맞지 않으면 `BLOCKED`가 아니라 typed compile failure다. public authority 자체가 없거나 기존 source 수정이 필요하면 Task를 `BLOCKED`로 보고한다.

## 4. Exact Write Boundary

정상 범위는 Runtime 2개, focused test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/VillageShellFacilities.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/VillageShellFacilityCompiler.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SpecialRegions/VillageShellFacilityAccessTests.cs(.meta)
```

```text
Runtime: Game.Map.Runtime / StarNight.Map.WorldGeneration.SpecialRegions
Tests: Game.Map.Tests.EditMode / StarNight.Map.Tests.EditMode.WorldGeneration.SpecialRegions
Category: MAP13_04
```

수정 금지:

```text
existing C# / test / CSV / meta
asmdef / asmref
Authoring / Generated
Scene / Prefab / ScriptableObject / Tilemap / Material / Texture
Settings / Packages
```

helper 파일, static starter catalog, Editor window, asset, importer, serializer는 추가하지 않는다.

## 5. Village Layout and Facility Model

프로젝트 naming/style에 맞추되 다음 semantic surface를 제공한다.

```text
VillageLayoutId
VillageLayoutShape: OneByOne / TwoByOne / OneByTwo
VillageFacilityKind: Kitchen / Repair / Optional
VillageFacilityRequirement: Required / Optional
VillageRoadCell
VillageFacilityDefinition
VillageFacilityAccessWitness
VillageShellDefinition
VillageFacilityBinding
VillageShellPlan
VillageShellCompileRequest
VillageShellFacilityCompiler.Compile
VillageShellCanonicalDigest
VillageShellErrorCode / Error / Result
```

모든 stable ID는 explicit caller input이다. filename, slot ID prefix/suffix, region ID 또는 display name에서 layout/facility 의미를 추론하지 않는다.

### 5.1 Footprint shape

- input bridge footprint는 exact full rectangle `1×1`, `2×1`, `1×2` 중 하나다.
- caller `VillageLayoutShape`가 bridge dimensions와 exact 일치해야 한다.
- `2×2`, sparse, disconnected, transposed mismatch는 거부한다.
- region-wide tile bounds는 각각 exact `48×32`, `96×32`, `48×64`다.
- 모든 road/door/path coordinate는 placed region-wide bounds 안이고 world sector/local tile로 round-trip 가능해야 한다.

### 5.2 Central road

central road는 caller가 explicit unique cell set과 ordered witness로 제공한다. compiler가 자동 탐색하거나 carve하지 않는다.

- road cell은 4-neighbor connected다.
- Entry apron과 Return apron에 각각 non-empty intersection을 가진다.
- 모든 active footprint sector를 최소 한 cell로 통과한다.
- `2×1`은 vertical internal sector seam `x=47/48`, `1×2`는 horizontal seam `y=31/32`를 cardinal-adjacent road pair로 건넌다.
- FixedCollision, Facility slot coordinate와 겹치지 않는다.
- road는 collision payload를 쓰지 않는 immutable `VillageRoadAccess` evidence다.
- road witness는 Entry→Road→Return과 reverse 순서를 source cells만으로 게시한다.

`1×1`에는 internal sector seam 요구가 없지만 Entry/Return apron 연결은 동일하게 필수다.

### 5.3 Required and optional facilities

exact requirements:

```text
Kitchen: exact 1, Required
Repair:  exact 1, Required
Optional: 3 or 4, Optional
total Facility bindings: 5 or 6
```

- 각 definition은 exact MAP13_03 `Facility` replaceable slot 하나를 참조한다.
- slot, definition, door와 witness identity는 unique하다.
- Kitchen/Repair는 Clear intent가 허용되지 않고 occupant assignment가 항상 존재한다.
- Optional은 assigned 또는 explicit empty일 수 있으며 어떤 optional archetype인지 이번 Task가 정하지 않는다.
- required/optional 의미를 slot ID로 추론하지 않고 definition field로 기록한다.
- door는 owning Facility slot의 cardinal-adjacent non-collision cell이다.
- facility payload는 marker/occupant plan이며 Solid/Collision/Route/Access/Persistence owner가 되지 않는다.

### 5.4 Every facility returns to road

각 Kitchen/Repair/Optional definition은 explicit ordered access witness를 가진다.

```text
Facility slot
→ door
→ zero-or-more access path cells
→ central road cell
```

- consecutive cell은 cardinal-adjacent이고 duplicate/backtrack loop가 없다.
- path는 FixedCollision, 다른 Facility slot과 겹치지 않는다.
- final cell은 exact central road member다.
- forward/reverse witness가 같은 cells에서 성립한다.
- AccessClass는 `MandatoryNoTool`이며 tool/progression gate/synthetic edge/teleport/carve는 `0`이다.
- all Facility road-return witness count는 total Facility count와 exact 같다.
- 이는 static cell/access proof이며 player physics reachability를 주장하지 않는다.

## 6. Output, Digest and Atomic Failure

Success output:

```text
layout identity/shape/bounds
canonical road cells and Entry↔Return witness
Kitchen/Repair required bindings
3 or 4 optional bindings
all Facility door/path/road-return witnesses
source bridge/entry-buffer/fixed-slot digests
road/facility/access/aggregate canonical digests
```

Collections은 defensive-copy/read-only/canonical order다. same input/reverse enumeration/repeat/`tr-TR`는 same semantic plan/digest를 게시한다. display text, time, object identity, Unity lifecycle은 digest에서 제외한다.

Any error는 plan/digests `0`; errors는 accumulated, deduped, stable-sorted다. RNG, filesystem, Tilemap, Scene, world mutation, static mutable cache는 `0`이다.

Minimum error groups:

```text
MissingInput | DigestMismatch | NotVillage | UnsupportedShape | ShapeMismatch
CoordinateOutOfRange | InvalidRoad | DisconnectedRoad | MissingApronConnection
MissingSectorCoverage | MissingSeamCrossing | RoadCollision
MissingKitchen | MissingRepair | InvalidOptionalCount | DuplicateFacility
FacilitySlotMismatch | RequiredFacilityClear | InvalidDoor | InvalidAccessWitness
FacilityCannotReturnToRoad | NonCanonicalPublication
```

## 7. Focused Tests

test-owned explicit definitions으로 최소 다음을 검증한다.

1. `1×1 / 2×1 / 1×2` three valid layout compile
2. exact region-wide bounds와 coordinate round-trip
3. connected central road, Entry/Return apron intersection
4. 2×1 vertical seam 및 1×2 horizontal seam crossing
5. Kitchen/Repair exact required two + optional three/four matrix
6. every 5/6 Facility door→path→road and reverse witness
7. required Facility clear rejection, optional assigned/empty acceptance
8. fixed/slot/path collision과 disconnected/out-of-bounds/missing road failure
9. non-Village, shape mismatch, missing/duplicate facility atomic failure
10. reverse/repeat/culture/immutability/digest와 RNG/world/tile mutation 0

road/path solver, Prefab, physics 또는 CSV parser를 test 안에 만들지 않는다.

## 8. Verification and Required Result

Unity refresh/compile 후 `MAP13_04` EditMode만 실행한다.

```text
discovered = executed = passed
failed / skipped / inconclusive = 0 / 0 / 0
compile / relevant Console error = 0 / 0
prior category / legacy / PlayMode / unfiltered selections = 0 / 0 / 0 / 0
```

Static gate:

```text
new Runtime C#/meta: 2/2
new focused test C#/meta: 1/1
existing C#/test/CSV/meta modifications: 0
Authoring/Generated/Scene/Prefab/Tilemap/Settings/Packages changes: 0
duplicate GUID: 0
unapplied candidate/diff-check/unrelated staged: 0/0/0
Git push: NOT PERFORMED
```

Result 경로:

```text
MapDesign/MCP/REPORTS/MAP13_04_IMPLEMENT_VILLAGE_SHELL_FACILITIES_AND_ACCESS_RESULT.md
```

상단 verdict:

```text
TASK: MAP13_04_IMPLEMENT_VILLAGE_SHELL_FACILITIES_AND_ACCESS
STATUS: PASS | BLOCKED
MAP13_04: COMPLETE ELIGIBLE | NOT COMPLETE
MAP13_05_IMPLEMENT_VILLAGE_STATE_VARIANTS: LOCKED / DO NOT START
```

`## User-Facing Implementation Report`에서 다음을 실제 수치로 보고한다.

- 신규/수정 script 전체 경로와 class/method별 input→output
- 세 shape의 bounds/road/seam/facility/witness 결과
- Kitchen/Repair/optional 개수와 road-return proof
- 새로 가능해진 것과 파이프라인 위치
- 아직 미구현한 MAP13_05+와 physical authoring/Prefab 범위
- Editor/게임 가시성

`## Responsibility and Added Functions`에는 아래를 표로 보고한다.

| Field | Required evidence |
|---|---|
| Task responsibility | Village shell/central road/Facility access compile |
| Added scripts | Runtime 2 + test 1 exact paths |
| Added functions | public type/method별 sole responsibility |
| Inputs consumed | MAP13_01 bridge + MAP13_02 buffer + MAP13_03 layers + explicit definition |
| Outputs produced | immutable Village shell/road/facility/access plan + digests/errors |
| Explicit non-ownership | content catalog/CSV, Prefab/NPC/inventory, physics, MAP13_05 |
| Downstream consumer | 별도 검수 후 MAP13_05만 unlock 가능 |

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
Subject: MAP13_04: implement Village shell and facility access
Push: NOT PERFORMED
```

Result가 PASS여도 MAP13_05를 자동 시작하지 않고 별도 검수까지 LOCKED로 유지한다.
