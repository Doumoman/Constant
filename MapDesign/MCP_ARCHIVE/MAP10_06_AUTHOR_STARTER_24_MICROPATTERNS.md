```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP10_06_AUTHOR_STARTER_24_MICROPATTERNS
  task_file: TASKS/MAP10_06_AUTHOR_STARTER_24_MICROPATTERNS.md
  requires_current_task: NONE
  requires_completed_task: MAP10_05_IMPLEMENT_REPETITION_SIGNATURE_AND_LOCAL_CLEANUP
  requires_result:
    path: REPORTS/MAP10_05_IMPLEMENT_REPETITION_SIGNATURE_AND_LOCAL_CLEANUP_RESULT.md
    status: PASS
    sha256: 7808a9defbcc177dd2f0bd63ac5a4f697c04f1e5510e539800d3f5966e3221e0
  requires_installed_task:
    path: TASKS/MAP10_05_IMPLEMENT_REPETITION_SIGNATURE_AND_LOCAL_CLEANUP.md
    sha256: a11c6a03294b2aea017793747a1dfdb7b6ac2d38ff4ce487394e2246e2753e7a
  sets_current_task: MAP10_06_AUTHOR_STARTER_24_MICROPATTERNS
```

# MAP10_06 — Author Starter 24 MicroPatterns

```text
TASK: MAP10_06_AUTHOR_STARTER_24_MICROPATTERNS
PHASE: MAP10 — 4×4 MicroPattern Authoring / Rendering
STATUS: CURRENT
NEXT: MAP10_07_CREATE_PATTERN_PREVIEW_AND_FOCUSED_TESTS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Responsibility

이번 Task는 header-only인 두 V2 MicroPattern CSV에 **실제 starter 24개**를 작성하고 existing importer/contracts로 atomic import를 증명한다.

```text
12 Geometry motifs
+ 4 Surface/Affordance patterns
+ 8 Material/Hazard/Marker patterns
= 24 patterns, four biomes × 6
```

| 소유 | 소유하지 않음 |
|---|---|
| exact 24 catalog/cell data rows | 새 schema/importer/runtime 기능 |
| biome/profile assignment와 equal pattern mass | asset/prefab/gameplay binding |
| 4×4 operation/payload authoring | preview UI/MAP11 cluster placement |
| content import/digest evidence | RNG·cleanup·renderer 수정 |

## 1. Regression and Preflight

정상 실행은 category `MAP10_06`만 선택한다.

```text
Prior MAP00~10_05 selections: 0
Legacy 19347 selections: 0
```

이번 Task에서 승인된 두 CSV의 내용 변경과 full Authoring manifest 변경은 **expected content delta**이며 regression trigger가 아니다. focused/import 실패, compile/Console 오류, 승인 외 파일 변경, legacy 50-file drift, meta/asmdef/GUID 위반이 실제 발생한 경우에만 owner·원인·최소 관련 selection을 기록한다.

읽기 전용 확인:

1. MAP10_05 Result/installed/archive Task exact hash와 Status
2. MAP10_01 exact CSV headers/importer/token codec/16-cell rules
3. MAP10_02~05 public transform/profile/signature APIs
4. MAP10_04 exact four profile motif metadata
5. current two CSV가 header-only이고 matching meta/GUID가 존재
6. legacy Authoring 50/50 manifest와 current full 52-file manifest

```text
Legacy 50-file Authoring manifest:
f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb

Pre-task full 52-file Authoring manifest:
b49b6ebc4a65c26302ff08deb9100dbf727200e16b3588092b7cd53f63d214ba
```

## 2. Exact File Boundary

내용 수정 허용 CSV exact 2:

```text
Assets/_Game/Map/Data/WorldGeneration/Authoring/MicroPattern/micro_pattern_catalog_v2.csv
Assets/_Game/Map/Data/WorldGeneration/Authoring/MicroPattern/micro_pattern_cells_v2.csv
```

신규 focused Editor test 허용:

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Import/MicroPatternStarterContentTests.cs(.meta)
```

- 두 CSV의 existing `.meta`는 byte-unchanged다.
- UTF-8 BOM, exact existing header/order, RFC4180, LF, one final LF를 유지한다.
- catalog는 `pattern_id` ordinal, cells는 `(pattern_id,y,x,layer)` ordinal로 기록한다.
- 새 production C#, schema, CSV column/file, Generated file, asset/SO를 만들지 않는다.

## 3. Authoring Notation

아래 geometry template은 `y=3` top row부터 `y=0` bottom row 순서다.

```text
. = GEOMETRY / NO_CHANGE / empty payload
+ = GEOMETRY / ADD_SOLID / empty payload
- = GEOMETRY / CARVE_AIR / empty payload
```

모든 pattern은 geometry row를 exact 16개 가진다. physical CSV는 canonical `y=0..3`, 각 y의 `x=0..3` 순서로 펼친다. 추가 layer instruction은 같은 coordinate의 별도 row다.

Biome/profile assignment는 별도 미승인 column을 추가하지 않고 exact one `biome_ids` 값으로 수행한다. 모든 pattern의 total transform mass는 `1000`이다.

```text
1 transform × weight 1000
2 transforms × weight 500
4 transforms × weight 250
```

## 4. Exact 12 Geometry Patterns

모두 `RejectCandidate`이며 profile의 canonical motif 3개를 biome별로 exact 구현한다.

| Pattern ID | Biome | Transforms | Weight | y3 / y2 / y1 / y0 |
|---|---|---|---:|---|
| MP_CRATER_BROKEN_SLOPE | MoonCrater | R0,MirrorX,MirrorY,R180 | 250 | `...+ / ..++ / .+++ / ++++` |
| MP_CRATER_BOWL | MoonCrater | R0,MirrorY | 500 | `.--. / ---- / .--. / ....` |
| MP_CRATER_ROCK_SHELF | MoonCrater | R0,MirrorX,MirrorY,R180 | 250 | `.... / .+++ / ++++ / ....` |
| MP_ROOT_ARCH | CassiaRoot | R0,MirrorY | 500 | `++++ / +--+ / +--+ / ....` |
| MP_ROOT_VERTICAL_TUNNEL | CassiaRoot | R0 | 1000 | `.--. / .--. / .--. / .--.` |
| MP_ROOT_HOLLOW_POCKET | CassiaRoot | R0,MirrorX,MirrorY,R180 | 250 | `.... / .--- / .--+ / .+++` |
| MP_MILL_BROKEN_PILLAR | AbandonedMill | R0,MirrorX,MirrorY,R180 | 250 | `.++. / .++. / .+.. / .++.` |
| MP_MILL_BEAM_OVERHANG | AbandonedMill | R0,MirrorX,MirrorY,R180 | 250 | `++++ / ...+ / ...+ / ....` |
| MP_MILL_ORTHOGONAL_CARVE | AbandonedMill | R0,MirrorX,MirrorY,R180 | 250 | `---. / ..-. / ..-. / ....` |
| MP_DOUGH_BOUNCE_CUP | MoonDough | R0 | 1000 | `.... / +--+ / +--+ / .++.` |
| MP_DOUGH_SOFT_POCKET | MoonDough | R0,MirrorX,MirrorY,R180 | 250 | `.... / .--- / .--- / ..-.` |
| MP_DOUGH_STICKY_SHELF | MoonDough | R0,MirrorX,MirrorY,R180 | 250 | `.... / .... / ++++ / ..++` |

Expected geometry operations:

```text
AddSolid 52
CarveAir 41
Non-NoChange Geometry total 93
12 non-zero mirror-invariant signatures, all pairwise distinct
```

## 5. Exact 4 Surface/Affordance Patterns

모두 geometry template `.... / .... / .... / ....`, `ForceNoChange`다.

| Pattern ID | Biome | Transforms | Weight | Additional instructions |
|---|---|---|---:|---|
| MP_CRATER_GRIP_RIDGE | MoonCrater | R0,MirrorX,MirrorY,R180 | 250 | SURFACE `SURF_CRATER_ROUGH`: `(0..3,1)`; AFFORDANCE `AFF_GRIP`: `(0,1),(2,1)` |
| MP_ROOT_CLIMB_VINES | CassiaRoot | R0,MirrorX | 500 | SURFACE `SURF_ROOT_BARK`: `(1,0..3)`; AFFORDANCE `AFF_CLIMB`: `(1,0..3)` |
| MP_MILL_BEAM_GRIP | AbandonedMill | R0,MirrorY | 500 | SURFACE `SURF_MILL_BEAM`: `(0..3,2)`; AFFORDANCE `AFF_GRAB`: `(0,2),(3,2)` |
| MP_DOUGH_BOUNCE_STRIP | MoonDough | R0 | 1000 | SURFACE `SURF_DOUGH_SOFT`: `(0..3,0)`; AFFORDANCE `AFF_BOUNCE`: `(1,0),(2,0)` |

Expected additional rows: Surface `16`, Affordance `10`, subtotal `26`.

## 6. Exact 8 Material/Hazard/Marker Patterns

모두 geometry template `.... / .... / .... / ....`, `ForceNoChange`다.

| Pattern ID | Biome | Transforms | Weight | Additional instructions |
|---|---|---|---:|---|
| MP_CRATER_DUST_PATCH | MoonCrater | R0 | 1000 | MATERIAL `MAT_MOON_DUST`: `(1,1),(2,1),(1,2),(2,2)`; MARKER `MARK_CRATER_DETAIL`: `(2,2)` |
| MP_CRATER_METEOR_CUE | MoonCrater | R0 | 1000 | HAZARD `HZ_METEOR_EDGE`: `(1,0),(2,0)`; MARKER `MARK_METEOR_CUE`: `(1,1),(2,1)` |
| MP_ROOT_SAP_PATCH | CassiaRoot | R0 | 1000 | MATERIAL `MAT_CASSIA_SAP`: `(1,1),(2,1),(1,2),(2,2)`; HAZARD `HZ_STICKY_SAP`: `(1,1),(2,1)` |
| MP_ROOT_SPROUT_MARK | CassiaRoot | R0 | 1000 | MATERIAL `MAT_ROOT_FIBER`: `(1,0),(2,0)`; MARKER `MARK_ROOT_SPROUT`: `(1,1),(2,1)` |
| MP_MILL_RUST_PATCH | AbandonedMill | R0,MirrorX | 500 | MATERIAL `MAT_MILL_RUST`: `(0,0),(1,1),(2,2),(3,3)`; HAZARD `HZ_SHARP_DEBRIS`: `(1,0),(2,0)` |
| MP_MILL_GEAR_SOCKET | AbandonedMill | R0 | 1000 | MATERIAL `MAT_MILL_IRON`: `(1,1),(2,1),(1,2),(2,2)`; MARKER `MARK_GEAR_SOCKET`: `(1,1),(2,2)` |
| MP_DOUGH_FERMENT_PATCH | MoonDough | R0 | 1000 | MATERIAL `MAT_DOUGH_FERMENT`: `(1,1),(2,1),(1,2),(2,2)`; HAZARD `HZ_FERMENT_BUBBLE`: `(1,2),(2,2)` |
| MP_DOUGH_RECOVERY_PAD | MoonDough | R0 | 1000 | MATERIAL `MAT_DOUGH_SOFT`: `(0,0),(1,0),(2,0),(3,0)`; MARKER `MARK_RECOVERY_PAD`: `(1,0),(2,0)` |

Expected additional rows:

```text
Material 26
Hazard 8
Marker 9
Subtotal 43
```

payload IDs are semantic stable tokens only. 이 Task는 실제 Tile/Prefab/physics effect를 만들거나 존재한다고 주장하지 않는다.

## 7. Exact Dataset Totals

```text
Catalog rows: 24
Patterns per biome: 6 / 6 / 6 / 6
Role groups: Geometry 12 / Surface-Affordance 4 / Detail 8

Base Geometry rows: 24 × 16 = 384
Additional layer rows: 26 + 43 = 69
Cell CSV data rows: 453

Geometry AddSolid / CarveAir: 52 / 41
Surface / Affordance: 16 / 10
Material / Hazard / Marker: 26 / 8 / 9
All non-NoChange instructions: 162
Geometry NoChange rows: 291
```

exact payload token inventory는 `24 unique`, invalid/empty payload `0`이어야 한다.

## 8. Content Validation

새 focused Editor test는 physical two-file import와 기존 API를 사용해 다음을 증명한다.

1. exact BOM/header/path와 importer atomic success
2. catalog/cell row totals `24/453`
3. exact 24 IDs와 no extra/missing/duplicate
4. biome별 6, role group `12/4/8`
5. every pattern exact 16 coordinate coverage
6. layer/operation/payload matrix와 exact 24 payload tokens
7. transform allowlist와 total mass 1000 per Pattern ID
8. protected policy: Geometry RejectCandidate, others ForceNoChange
9. exact per-layer operation totals
10. 12 geometry patterns의 non-zero/pairwise-distinct MAP10_05 signatures
11. 12 non-geometry patterns의 explicit zero geometry signature
12. row-order-independent catalog digest와 immutable definitions
13. legacy 50 files/meta 및 both CSV meta byte-unchanged
14. Generated/asset/SO/Scene/Prefab/runtime side effect 0

test가 expected IDs/coordinates/payloads를 production CSV에서 복사해 스스로 정답으로 삼는 방식은 금지한다. Task table의 golden expectations를 test-owned constants로 독립 검증한다.

## 9. Change Boundary

허용:

- existing two MicroPattern CSV content
- 신규 focused Editor test C# + meta
- installed/archive Task, Result, PASS 후 Status Finalize

금지:

- both CSV meta 변경/GUID 재생성
- 다른 50 Authoring CSV/meta 또는 Generated 변경
- existing production/test C#/meta 수정
- 신규 production code/schema/CSV/asset/SO/Editor UI
- MAP10_02~05 renderer/profile/RNG/signature/cleanup 변경
- 실제 payload asset/physics binding 구현
- 문제 trigger 없는 이전/legacy test 실행
- unrelated stage/commit, Git push

## 10. Required Result

Result:

```text
MAP10_06_AUTHOR_STARTER_24_MICROPATTERNS_RESULT.md
```

상단:

```text
TASK: MAP10_06_AUTHOR_STARTER_24_MICROPATTERNS
STATUS: PASS | FAIL | BLOCKED
MAP10_06: COMPLETE ELIGIBLE | NOT COMPLETE
MAP10_07_CREATE_PATTERN_PREVIEW_AND_FOCUSED_TESTS: LOCKED / DO NOT START
```

첫 구현 섹션은 반드시 `Responsibility and Added Functions`다.

| Field | Required report |
|---|---|
| Task responsibility | exact 24 starter CSV content와 biome/profile assignment |
| Added functions | 추가한 콘텐츠의 실제 3개 role group, motif, payload 기능 |
| Inputs consumed | MAP10_01 schema/importer와 MAP10_02~05 contracts |
| Outputs produced | 24-definition immutable catalog, row/digest/signature evidence |
| Explicit non-ownership | production code/assets/physics/preview/cluster placement 미구현 |
| Downstream consumers | MAP10_07~08과 MAP11 cluster pattern renderer |

이후 exact 24 inventory, CSV row/hash/GUID, biome/role/operation/payload totals, import/digest/signature evidence, focused/regression policy, Unity/static/change scope, commit handoff를 기록한다.

focused 수치:

```text
MAP10_06 focused: discovered/executed/pass/fail/skip
REGRESSION TRIGGER DETECTED: NO | YES(owner/reason/minimum scope)
PRIOR TASK TEST SELECTIONS: 0 (normal path)
LEGACY TEST SELECTIONS: 0 (normal path)
```

static gate:

```text
compile/Console/relevant warning: 0/0/0
legacy Authoring 50/50 + manifest f630219... byte-unchanged
MicroPattern CSV data rows: 24/453; existing metas byte-unchanged
new full 52-file Authoring manifest: recorded
Generated CSV: 0
existing MAP00~10_05 production/test modifications: 0
other roots/asmdef/Scene/Prefab/Settings/Packages changes: 0
duplicate GUID/unapplied candidate/diff-check: 0/0/0
unrelated staged/included: 0
```

PASS일 때만 Finalize하고 exact two CSV + new focused test/meta + task protocol files만 atomic commit한다.

```text
Subject: MAP10_06: author starter MicroPatterns
Push: NOT PERFORMED
```

MAP10_07을 자동 시작하지 않는다.
