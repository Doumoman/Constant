TASK: MAP13_01_IMPLEMENT_SPECIAL_FOOTPRINT_SITE_BRIDGE_AND_COORDINATES
STATUS: PASS
MAP13_01: COMPLETE ELIGIBLE
MAP13_02_IMPLEMENT_ENTRY_BUFFER_PRIORITY_AND_COLLISION_RULES: LOCKED / DO NOT START

## User-Facing Implementation Report

MAP03의 typed `SiteReservation`/`SiteReservationSnapshot` authority와 MAP09의 validated `SpecialRegionContract`를 잇는 additive runtime bridge를 구현했다. 기존 MAP03/MAP09 파일은 수정하지 않았으며, authored sector/tile/side 좌표를 예약의 최종 footprint transform에 따라 placed sector, world sector, 48x32 local tile, region-wide tile 및 placed side로 투영한다.

추가·수정 스크립트 전체 목록:

- 신규 `Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionSiteCoordinates.cs`: authored/placed typed coordinate와 `TryProject`/`TryUnproject`를 제공한다. 기존 `SiteFootprintTransformer`로 sector offset과 side를 변환하고, bridge-owned tile mirror 및 world/region 좌표 검증만 담당한다.
- 신규 `Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionSiteBridge.cs`: exact reservation/kind/footprint/sector-row/port-anchor binding, immutable canonical bindings, typed atomic errors, MAP03 identity digest와 MAP09 contract digest 및 bridge digest를 제공한다.
- 신규 `Assets/_Game/Tests/EditMode/Map/WorldGeneration/SpecialRegions/SpecialRegionSiteBridgeTests.cs`: `MAP13_01` category에서 지원 shape/transform, kind matrix, 좌표 round-trip, payload 보존, anchor/edge, 결정성, negative atomicity를 검증한다.
- 각 신규 C#과 같은 경로의 matching `.meta` 3개를 신규 추가했다.
- 수정한 기존 C#/test/CSV/meta 스크립트는 없다.

지원 결과:

- `1x1`, `2x1`, `1x2` full rectangle 각각에 `R0`, `MirrorX`, `MirrorY`, `R180`을 적용한 12개 조합이 exact MAP03 sector row와 함께 compile되었다.
- Village/CoreResource/Forge/Boss의 4개 exact kind pair가 compile되었으며 Start와 OptionalLandmark는 typed `UnsupportedKind`로 atomic reject된다.
- fixed shell, replaceable slot, Entry/Return port는 source/placed provenance를 모두 유지한다. slot ID, required flag, persistence scope/key, AccessClass, port ID와 MAP03 entry socket identity도 보존한다.
- L/R은 placed tile `x=0/47`, D/U는 `y=0/31`인 exterior edge만 허용하며, transformed sector/side가 정확히 하나의 MAP03 anchor와 일치하고 anchor exterior world sector가 유효할 때만 publish한다.
- invalid ID/kind/shape/coordinate/anchor/port는 bridge와 digest를 모두 빈 상태로 유지한다. MAP03 snapshot 생성자가 invalid/missing/foreign sector row를 publication 전에 차단하며 bridge도 row identity와 orphan row를 재검증한다.
- input reverse, repeat, `tr-TR`, caller collection mutation에서 bridge 및 digest가 동일했다. RNG, filesystem output, Unity lifecycle, world mutation, static mutable cache는 사용하지 않는다.

Editor/게임 가시성:

- Editor: 신규 메뉴, EditorWindow, inspector, Scene object 또는 authoring asset은 없다. Unity Test Runner에서 `MAP13_01` EditMode category와 public runtime API로만 확인 가능하다.
- 게임 화면: 신규 GameObject, MonoBehaviour, renderer, Tilemap, collider, prefab 또는 UI가 없으므로 시각적 변화는 없다.
- runtime pipeline 위치: validated MAP09 contract와 현재 MAP03 snapshot/publication을 소비한 뒤 downstream placement 전에 immutable coordinate bridge를 제공한다.

미구현 기능:

- MAP13_02의 entry buffer, priority 및 collision rule
- 실제 Village/CoreResource/Forge/Boss/OptionalLandmark content, facility/state/gameplay object
- SectorCanvas composition, Tilemap/collider/physics/player reachability 및 Scene/Prefab 배치
- reservation solver/selection 변경, MAP03/MAP09 validator 재설계, serialization/CSV/authoring/generated asset

## Responsibility and Added Functions

| Field | Evidence |
|---|---|
| Task responsibility | MAP03 reservation identity/origin/final footprint/transform과 MAP09 SpecialRegion authored coordinates 사이의 exact typed bridge |
| Added scripts | Runtime 2개 + EditMode test 1개의 exact path와 matching meta 3개 |
| `SpecialRegionSiteCoordinates.cs` inputs | footprint dimensions/transform/origin, authored sector offset, `LocalTileCoord`, optional `SiteEntrySide` |
| `SpecialRegionSiteCoordinates.cs` outputs | transformed offset, world `SectorCoord`, transformed local tile, region-wide tile, optional transformed side; invalid input은 `false` |
| `SpecialRegionAuthoredCoordinate` | source sector/local tile/optional side의 immutable value |
| `SpecialRegionPlacedCoordinate` | placed sector/world sector/local tile/region tile/optional side의 immutable value |
| `SpecialRegionSiteCoordinateTransformer.TryProject/TryUnproject` | MAP03 sector/side authority 재사용, 48x32 tile mirror, redundant placed-field 검증, identity round-trip |
| `SpecialRegionSiteSectorBinding` | source/placed offset와 exact world sector/index/local role 연결 |
| `SpecialRegionSiteFixedShellBinding` | shell ID 및 source/placed coordinate provenance 보존 |
| `SpecialRegionSiteSlotBinding` | slot kind/ID/required/persistence payload 및 좌표 provenance 보존 |
| `SpecialRegionSitePortBinding` | port/slot/AccessClass/persistence, source/placed side, matched MAP03 socket와 exterior sector 보존 |
| `SpecialRegionSiteBridge` | region/reservation identity, origin/dimensions/transform, canonical read-only bindings 및 source digests publish |
| `SpecialRegionSiteBridgeCompiler.Compile` | snapshot, approved publication 또는 validation result와 validated/raw contract overload를 받아 모든 invariant 통과 시에만 bridge 하나를 atomic publish |
| `SpecialRegionSiteBridgeErrorCode/Error/Result` | required error group을 dedupe/stable-sort하고 any error에서 bridge/digest를 0으로 유지 |
| `SpecialRegionSiteBridgeCanonicalDigest` | culture-independent MAP03 reservation identity와 complete bridge SHA-256 계산 |
| Explicit non-ownership | reservation solver, entry buffer/priority/collision, content, Canvas/Tilemap, gameplay 및 persistence state 생성 |
| Downstream consumer | 별도 검토 후 MAP13_02만 unlock 가능; 이 수행에서는 시작하지 않음 |

## Coordinate and Binding Evidence

| Transform | source sector/tile/side probe | exact placed result |
|---|---|---|
| R0 | `(0,0)/(3,5)/L` in `2x1`, origin `(5,4)` | `(0,0)`, world `(5,4)`, tile `(3,5)`, region `(3,5)`, `L` |
| MirrorX | same | `(1,0)`, world `(6,4)`, tile `(44,5)`, region `(92,5)`, `R` |
| MirrorY | same | `(0,0)`, world `(5,4)`, tile `(3,26)`, region `(3,26)`, `L` |
| R180 | same | `(1,0)`, world `(6,4)`, tile `(44,26)`, region `(92,26)`, `R` |

각 행은 `TryUnproject`로 원래 authored coordinate와 exact identity round-trip했다. 12 shape/transform case의 sector binding 수는 각각 footprint cell 수와 같고 각 binding의 index/coordinate/reservation ID/kind/local offset/local role이 snapshot row와 일치했다.

## Negative and Determinism Evidence

| Fixture | Atomic verdict |
|---|---|
| missing snapshot/validation | `MissingInput`, bridge/digest 0 |
| invalid or absent typed reservation ID | `InvalidReservation`/`ReservationNotFound`, fallback lookup 0 |
| Start / OptionalLandmark | `UnsupportedKind`, kind coercion 0 |
| sparse/duplicate/disconnected/2x2/3x1/1x3 footprint | `UnsupportedFootprint`, bridge 0 |
| transformed footprint mismatch | `FootprintMismatch`/`TransformMismatch`, bridge 0 |
| bad local tile/sector/enum/redundant world-region field | projection false or `CoordinateOutOfRange`, clamp/wrap 0 |
| missing/mismatched/orphan sector row | MAP03 constructor reject 또는 `MissingSectorRow`/`SectorRowMismatch`, bridge 0 |
| missing/mismatched anchor | `MissingEntryAnchor`/`PortAnchorMismatch`, bridge 0 |
| non-exterior port tile | `PortNotOnExteriorEdge`, bridge 0 |
| invalid contract/publication | `ContractValidationFailed`/`NonCanonicalPublication`, bridge 0 |

Error collection은 code/path/detail ordinal order로 dedupe/sort된다. 성공 collection도 canonical copied read-only collection이며 input reference나 enumeration order에 의존하지 않는다.

## Focused Verification

허용된 유일한 테스트 selection:

```text
Unity: 6000.3.8f1
mode/category: EditMode / MAP13_01
job: 140a967006c14480a78f484e2a4923b3
discovered / executed / passed: 30 / 30 / 30
failed / skipped / inconclusive: 0 / 0 / 0
duration: 1.5841365 seconds
script validation errors / warnings: 0 / 0
final relevant Console errors / warnings: 0 / 0
```

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

`MAP13_01` category selection은 1회였다. 과거 Task category, legacy 19347, PlayMode 및 unfiltered test는 실행하지 않았다.

## Static Scope and Commit Handoff

```text
input/installed/archive Task SHA-256:
7c487cfb1aa405c4300496f5d87b2adc27864694f59385417f24edc2896bb4bb

new Runtime C#/meta: 2 / 2
new focused EditMode test C#/meta: 1 / 1
existing C#/test/CSV/meta modifications: 0
Authoring/Generated/Scene/Prefab/SO/Tilemap/Material/Texture changes: 0
asmdef/asmref/Settings/Packages changes: 0
duplicate GUID groups across 4004 meta files: 0
unapplied inbox candidate/diff-check/unrelated staged: 0 / 0 / 0
pre-existing unrelated untracked TerrainClusters.meta excluded: 3
Git push: NOT PERFORMED
```

Task-owned asset inventory:

```text
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionSiteCoordinates.cs
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionSiteCoordinates.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionSiteBridge.cs
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionSiteBridge.cs.meta
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SpecialRegions/SpecialRegionSiteBridgeTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SpecialRegions/SpecialRegionSiteBridgeTests.cs.meta
```

Task-owned protocol inventory:

```text
NEW MapDesign/MCP/TASKS/MAP13_01_IMPLEMENT_SPECIAL_FOOTPRINT_SITE_BRIDGE_AND_COORDINATES.md
NEW MapDesign/MCP_ARCHIVE/MAP13_01_IMPLEMENT_SPECIAL_FOOTPRINT_SITE_BRIDGE_AND_COORDINATES.md
NEW MapDesign/MCP/REPORTS/MAP13_01_IMPLEMENT_SPECIAL_FOOTPRINT_SITE_BRIDGE_AND_COORDINATES_RESULT.md
MODIFIED MapDesign/MCP/06_IMPLEMENTATION_STATUS.md (open/finalize fields and Last Completed/Result only)
```

Atomic commit handoff:

```text
base HEAD: a1d0f8eddf82e09777399baf8fd75ab77349c30f
subject: MAP13_01: bridge SpecialRegion site coordinates
allowlist: installed Task, archive, Runtime 2 C#/meta, focused test C#/meta, Result, status finalize only
Status Finalize: PERFORMED as the ordered next protocol phase before the enclosing commit
final state: MAP13_01 COMPLETE / Current Task NONE
next state: MAP13_02 LOCKED / DO NOT START
commit hash: enclosing atomic Git commit (Result-self convention; verify with `git show --format=%H HEAD`)
unrelated staged/included: 0 / 0
Git push: NOT PERFORMED
```

Result: PASS
