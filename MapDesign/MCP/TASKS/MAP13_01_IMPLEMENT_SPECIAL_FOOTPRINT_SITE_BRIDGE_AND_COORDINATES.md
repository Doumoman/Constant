```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP13_01_IMPLEMENT_SPECIAL_FOOTPRINT_SITE_BRIDGE_AND_COORDINATES
  task_file: TASKS/MAP13_01_IMPLEMENT_SPECIAL_FOOTPRINT_SITE_BRIDGE_AND_COORDINATES.md
  requires_current_task: NONE
  requires_completed_task: MAP12_07_MAP12_ACTIVITY_EXIT_TESTS
  requires_result:
    path: REPORTS/MAP12_07_MAP12_ACTIVITY_EXIT_TESTS_RESULT.md
    status: PASS
    sha256: cfc29b7757130f144e3b57198f048d450409f2cb088fd7ab8e7465ee27b6ff06
  requires_installed_task:
    path: TASKS/MAP12_07_MAP12_ACTIVITY_EXIT_TESTS.md
    sha256: 9cc540315e11798536669f344908acbf201675314185dd6aa44e6c93564c39f8
  sets_current_task: MAP13_01_IMPLEMENT_SPECIAL_FOOTPRINT_SITE_BRIDGE_AND_COORDINATES
```

# MAP13_01 — Special Footprint / Site Bridge / Coordinates

```text
TASK: MAP13_01_IMPLEMENT_SPECIAL_FOOTPRINT_SITE_BRIDGE_AND_COORDINATES
PHASE: MAP13 — SpecialRegion / Village / Mandatory Landmarks
STATUS: CURRENT
NEXT: MAP13_02_IMPLEMENT_ENTRY_BUFFER_PRIORITY_AND_COLLISION_RULES
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP03의 승인된 `SiteReservation`과 MAP09의 `SpecialRegionContract`를 typed bridge로 연결하고, `1×1 / 2×1 / 1×2` region의 authored sector/tile/side 좌표를 예약된 world sector/local tile 좌표로 결정론적으로 투영한다.

```text
MAP03 SiteReservation identity/origin/final footprint/transform
+ MAP09 SpecialRegion footprint/slot/port contract
→ exact reservation binding
→ authored sector + 48×32 local tile transform
→ placed sector/world sector/local tile/side
→ immutable bridge + canonical digest
```

이번 Task는 기존 MAP03/MAP09 파일을 변경하지 않는 additive bridge다. Result의 첫 섹션은 반드시 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성하고 파일별 책임·input→output·새 기능·파이프라인 위치·미구현·Editor/게임 가시성을 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| Reservation ID/kind/footprint exact binding | MAP03 reservation solver/selection 변경 |
| source sector/tile/side ↔ placed/world coordinate transform | 48×32 SectorCanvas composition |
| 1×1, 2×1, 1×2 footprint와 sector-row identity | entry apron, Quiet buffer, collision priority |
| Entry/Return port와 MAP03 anchor의 좌표·side 대응 | Village road/facility/state/content |
| immutable bridge/result/error/digest | fixed shell/slot/persistence 재설계 |
| focused MAP13_01 test | MAP13_02 이후 작업, Scene/Prefab/Tilemap |

실제 Region 콘텐츠, Village, MoonCore/CassiaSap/StarNuruk, Forge, Boss, OptionalLandmark, Activity/Event 배치와 gameplay object는 만들지 않는다.

## 2. Focused-Only Policy

정상 실행은 category `MAP13_01` EditMode만 선택한다.

```text
MAP13_01 EditMode: required
MAP03/MAP09/MAP10/MAP11/MAP12 categories: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
```

기존 public API 호출은 과거 category 재실행이 아니다. 실제 upstream defect를 발견하면 기존 파일을 고치지 말고 owner/invariant/reason/minimum verification을 기록해 `BLOCKED`로 STOP한다. Task-owned 신규 파일 문제만 신규 파일 안에서 고치고 `MAP13_01`만 재실행한다.

## 3. Read-Only Preflight

```text
MAP12_07 Result: PASS
MAP12 PHASE EXIT: APPROVED
MAP12_07 Result SHA-256:
cfc29b7757130f144e3b57198f048d450409f2cb088fd7ab8e7465ee27b6ff06

MAP12_07 installed Task SHA-256:
9cc540315e11798536669f344908acbf201675314185dd6aa44e6c93564c39f8

MAP12_07 COMPLETE / MAP13_01 CURRENT / MAP13_02 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required current authority:

```text
MAP03:
SiteReservationId, SiteReservation, SiteReservationSnapshot or approved publication
SiteFootprint, SiteFootprintCell, SectorReservation
SiteFootprintTransform: R0 / MirrorX / MirrorY / R180
SiteEntryAnchor, SiteEntrySide, SiteFootprintTransformer

MAP09:
SpecialRegionId, SpecialRegionKind, SpecialRegionFootprint
SpecialRegionContract, SpecialRegionValidator
SpecialRegionSlot, Entry/Return ports
SectorCoord, LocalTileCoord, WorldGenConstants (Sector 48×32)
```

Do not replace/redefine these types. Missing public access, type collision, baseline drift or existing-file modification requirement is `BLOCKED`.

## 4. Exact Write Boundary

정상 범위는 Runtime 2개, focused test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionSiteCoordinates.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionSiteBridge.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SpecialRegions/SpecialRegionSiteBridgeTests.cs(.meta)
```

```text
Runtime assembly: Game.Map.Runtime
Runtime namespace: StarNight.Map.WorldGeneration.SpecialRegions
Test assembly: Game.Map.Tests.EditMode
Test namespace: StarNight.Map.Tests.EditMode.WorldGeneration.SpecialRegions
Category: MAP13_01
```

수정 금지:

```text
existing C# / test / CSV / meta
asmdef / asmref
Authoring / Generated
Scene / Prefab / ScriptableObject / Tilemap / Material / Texture
Settings / Packages
```

새 helper 파일, Editor window, asset, serializer 또는 CSV를 추가하지 않는다. 두 Runtime 파일 내부 private helper는 허용한다.

## 5. Coordinate Spaces and Transform

프로젝트 naming/style에 맞추되 아래 semantic surface를 제공한다.

```text
SpecialRegionAuthoredCoordinate
  source sector offset
  source LocalTileCoord
  optional source side

SpecialRegionPlacedCoordinate
  transformed sector offset
  world SectorCoord
  transformed LocalTileCoord
  region-wide tile coordinate
  optional transformed side

SpecialRegionSiteCoordinateTransformer.TryProject
SpecialRegionSiteCoordinateTransformer.TryUnproject
```

좌표 범위:

```text
source/placed sector: footprint 안
local tile X: 0..47
local tile Y: 0..31
region tile X = placedSectorX * 48 + placedTileX
region tile Y = placedSectorY * 32 + placedTileY
world sector = reservation origin + placed sector offset
```

MAP03 transform와 같은 dimension-preserving 규칙을 사용한다. 90도 회전이나 width/height swap은 없다.

| Transform | Sector offset | Local tile | Side |
|---|---|---|---|
| R0 | `(sx, sy)` | `(tx, ty)` | same |
| MirrorX | `(W-1-sx, sy)` | `(47-tx, ty)` | L↔R |
| MirrorY | `(sx, H-1-sy)` | `(tx, 31-ty)` | U↔D |
| R180 | both axes | `(47-tx, 31-ty)` | L↔R, U↔D |

Requirements:

- sector offset transform은 existing `SiteFootprintTransformer` authority를 재사용한다.
- tile-level mirror만 이번 bridge가 추가한다.
- invalid/default/out-of-range coordinate, enum, dimension, overflow는 `false`/typed error이며 clamp/wrap/exception-text publication이 없다.
- `TryProject → TryUnproject` round-trip은 identity다.
- caller input mutation, culture, Unity lifecycle, object identity에 의존하지 않는다.

## 6. Site Bridge Model and Compilation

프로젝트 style에 맞추되 아래 책임을 같은 파일에 둔다.

```text
SpecialRegionSiteSectorBinding
SpecialRegionSitePortBinding
SpecialRegionSiteBridge
SpecialRegionSiteBridgeErrorCode / Error
SpecialRegionSiteBridgeResult
SpecialRegionSiteBridgeCompiler.Compile
SpecialRegionSiteBridgeCanonicalDigest
```

Compiler input은 current approved MAP03 reservation snapshot/publication과 validated `SpecialRegionContract`다. 모든 입력을 검증한 뒤에만 하나의 immutable bridge를 publish한다.

### 6.1 Reservation and kind binding

- contract의 typed `SiteReservationId`가 snapshot에서 exact 한 reservation을 찾는다.
- string 추정, prefix 변환, 자동 ID 생성 또는 first-match fallback은 금지한다.
- exact kind compatibility:

| MAP03 Site kind | MAP09 SpecialRegion kind |
|---|---|
| Village | Village |
| CoreResource | CoreResource |
| Forge | Forge |
| Boss | Boss |

- MAP03 `Start`와 MAP09 `OptionalLandmark`는 이번 bridge 대상이 아니다. 억지 kind 변환 없이 typed `UnsupportedKind`로 거부한다.
- region ID는 contract authority를 그대로 보존하며 reservation ID에서 재생성하지 않는다.

### 6.2 Footprint and sector rows

- 지원 shape는 exact full rectangle `1×1`, `2×1`, `1×2`다.
- normalized source offsets를 reservation transform으로 투영한 set이 MAP03 final local footprint set과 exact 같아야 한다.
- sparse, duplicate, disconnected, 2×2, 3×1, 1×3 이상은 거부한다.
- 각 placed offset은 exact world sector와 snapshot `SectorReservation` row 하나에 대응한다.
- row의 reservation ID, kind, local X/Y, world coordinate/index가 모두 일치해야 한다.
- footprint 밖 row, orphan row, overlap, missing/duplicate row는 atomic failure다.

### 6.3 Slot and port projection

- contract의 fixed-shell cell, replaceable slot, Entry/Return port 좌표를 모두 같은 transformer로 projection할 수 있어야 한다.
- 이번 Task는 shell/slot 내용을 변경하지 않고 source/placed coordinate provenance만 연결한다.
- Entry/Return port의 transformed sector/side가 referenced MAP03 entry anchor의 occupied sector/side와 일치해야 한다.
- L/R port local tile은 `x=0/47`, D/U port local tile은 `y=0/31`의 corresponding exterior edge여야 한다.
- Entry/Return `AccessClass`, slot ID, persistence key와 payload는 보존하며 bridge가 새 key/state를 만들지 않는다.
- anchor exterior sector는 existing MAP03 non-clamp 규칙으로 world 안이어야 한다.

### 6.4 Output, atomicity and digest

Success output:

```text
region/reservation identity and kinds
origin, transform, dimensions and shape
canonical sector bindings
canonical fixed/slot/port coordinate bindings
source and placed coordinate provenance
MAP03 reservation identity + MAP09 contract digest
canonical bridge digest
```

Collections은 copied/read-only/canonical order다. same input/reverse enumeration/repeat/`tr-TR`는 같은 bridge/digest를 게시한다. any error는 bridge/digest `0`; errors는 accumulated, deduplicated, stable-sorted다. RNG stream/draw, filesystem output, static mutable cache는 `0`이다.

Minimum error groups:

```text
MissingInput | InvalidReservation | ReservationNotFound | ReservationIdMismatch
UnsupportedKind | KindMismatch | UnsupportedFootprint | FootprintMismatch
MissingSectorRow | SectorRowMismatch | CoordinateOutOfRange | TransformMismatch
MissingEntryAnchor | PortAnchorMismatch | PortNotOnExteriorEdge
ContractValidationFailed | NonCanonicalPublication
```

## 7. Focused Tests

`SpecialRegionSiteBridgeTests`는 physical content를 만들지 않고 public constructors/validators로 in-memory fixtures를 조립한다.

최소 검증:

1. `1×1 / 2×1 / 1×2 × 4 transforms` reservation/footprint/sector-row compile 성공
2. asymmetric sector/tile/side exact transform table과 project/unproject round-trip
3. Village/CoreResource/Forge/Boss exact kind matrix
4. fixed shell/slot/Entry/Return coordinate provenance와 AccessClass/persistence identity 보존
5. port exterior edge와 MAP03 entry anchor sector/side exact 일치
6. reverse input, repeat, `tr-TR`, caller mutation에서 bridge/digest 안정성
7. invalid ID/kind/shape/row/coordinate/anchor/port fixture의 atomic zero publication
8. Start/OptionalLandmark 명시적 unsupported, clamp/wrap/RNG/filesystem/world mutation 0

Production parser/solver를 test에 복제하지 않는다.

## 8. Verification and Result

Unity refresh/compile 후 `MAP13_01` EditMode만 실행한다.

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
MapDesign/MCP/REPORTS/MAP13_01_IMPLEMENT_SPECIAL_FOOTPRINT_SITE_BRIDGE_AND_COORDINATES_RESULT.md
```

상단 verdict:

```text
TASK: MAP13_01_IMPLEMENT_SPECIAL_FOOTPRINT_SITE_BRIDGE_AND_COORDINATES
STATUS: PASS | BLOCKED
MAP13_01: COMPLETE ELIGIBLE | NOT COMPLETE
MAP13_02_IMPLEMENT_ENTRY_BUFFER_PRIORITY_AND_COLLISION_RULES: LOCKED / DO NOT START
```

`## User-Facing Implementation Report`에서 정확히 보고한다.

- 추가/수정 script 전체 경로와 신규/수정 여부
- script/class별 책임과 input→output
- 1×1/2×1/1×2 및 transform별 실제 결과
- 이번에 새로 가능해진 기능과 파이프라인 위치
- 아직 미구현한 MAP13_02+ 기능
- Editor/게임 화면 가시성

`## Responsibility and Added Functions`에서 아래를 표로 보고한다.

| Field | Required evidence |
|---|---|
| Task responsibility | MAP03 reservation ↔ MAP09 SpecialRegion coordinate bridge |
| Added scripts | Runtime 2 + test 1 exact paths |
| Added functions | public types/methods별 sole responsibility |
| Inputs consumed | current reservation snapshot/publication + validated contract |
| Outputs produced | immutable sector/slot/port bindings + digest/errors |
| Explicit non-ownership | solver, buffer/priority, Village/content, Canvas/Tilemap/gameplay |
| Downstream consumer | 별도 검수 후 MAP13_02만 unlock 가능 |

그 뒤 footprint/transform/kind/sector-row/port matrix, digest/determinism, negative fixtures, focused test, static scope, regression selections와 commit handoff를 실제 수치로 기록한다.

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
Subject: MAP13_01: bridge SpecialRegion site coordinates
Push: NOT PERFORMED
```

Result가 PASS여도 MAP13_02를 자동 시작하지 않고 별도 검수까지 LOCKED로 유지한다.
