TASK: MAP16_01_FINALIZE_CANVAS_LAYERS_AND_FIXED_PRECEDENCE
STATUS: PASS
MAP16_01: COMPLETE ELIGIBLE only when PASS
MAP16_02_VALIDATE_PROTECTION_CLEANUP_AND_DENSITY: LOCKED / DO NOT START

## User-Facing Implementation Report

이번 작업은 MAP15 world assembly 결과를 받아 sector-local 최종 canvas를 확정하는 순수 메모리 계약을 추가했다. 대상은 정확히 48x32 크기의 논리 canvas와 레이어별 claim 우선순위이며, 실제 Tilemap 배치, 12x8 slice 생성, Scene/Prefab/GameObject 수정, 파일 생성, gameplay spawn은 수행하지 않는다.

추가한 스크립트와 책임은 다음과 같다.

- `Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorFinalCanvasLayerPlan.cs`: final canvas의 7개 레이어, cell/claim/source owner/priority/protection/conflict/failure 모델, 읽기 전용 plan/result, canonical SHA-256 digest 계약을 제공한다.
- `Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorCanvasLayerFinalizer.cs`: MAP15_07 승인 및 upstream identity를 검증하고, 고정된 비교 규칙으로 모든 claim의 winner를 선택하며, 금지된 overwrite를 typed conflict로 실패시키고 완전한 plan만 원자적으로 발행한다.
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/SectorCanvasLayerFinalizerTests.cs`: MAP16_01 전용 reference fixture와 정확히 10개의 focused EditMode test로 크기, 레이어, precedence, protection, provenance, conflict, digest 결정성, atomic failure, mutation 0, downstream lock을 검증한다.

새로 가능해진 기능은 한 sector의 Terrain, Affordance, Material, Hazard, Marker, Protection, SourceOwner 7개 레이어를 cell별 winner와 함께 확정하고, 각 winner의 source owner와 provenance를 추적하며, 동일 입력을 repeat/reverse/culture 변경으로 처리해도 같은 digest를 발행하는 것이다. 약한 claim이 MAP07 fixed authority, MAP08 boundary aperture, mandatory-route protected-open, special entrance를 덮으려 하면 silent overwrite 대신 typed conflict/failure와 빈 plan으로 종료한다.

검증된 reference canvas 수치는 다음과 같다.

- sector: 48x32, 관측 cell 1536/1536, unique coordinate 1536, out-of-bounds 0
- layer: required 7, covered 7, missing 0; 각 레이어 winner 1536, 전체 winner 10752
- winner source owner 공개 10752/10752, provenance 공개 10752/10752
- protected cell 4, fixed cell 1, boundary aperture cell 1, marker 2
- MAP07 fixed precedence: weaker claim 대비 winner 2/2, fixed source owner 유지
- MAP08 boundary precedence: weaker claim 대비 winner 2/2, boundary source owner 유지
- 정상 plan conflict 0, silent overwrite 0, protected-open overwrite violation 0, special entrance blocked violation 0
- synthetic invalid case에서 `FixedSliceOverwrite`, `BoundaryApertureOverwrite`, `MandatoryRouteProtectedOpenBlocked`, `SpecialEntranceBlocked`, `SamePriorityDifferentValue`를 typed conflict로 검증했고 모든 실패는 partial plan 없이 원자적으로 종료했다.
- input digest: `3411490c6b949eb154075f4c0df21655604e9f9ea51e2832b9d61a99d167d4e1`
- output digest: `a1f7e8c1984b9abd8f34244ff2db632dac9ef768189ed2ecb8cc9be7ae29ba28`
- repeat/reverse/tr-TR culture digest mismatch 0; canonical record는 coordinate/layer/priority/source owner/provenance/claim id 기준으로 정렬된다.

새 RNG draw, slice 생성, generated file write, Tilemap/Scene/Prefab/GameObject mutation, gameplay spawn, production seed 승인, sector reroll, fallback carve, full regression은 모두 0이다. 입력 claim 및 MAP15/MAP14/MAP08/MAP07 upstream digest도 전후 동일하여 기존 world assembly와 authoring 자료를 수정하지 않았고, 기존 production C#, asmdef, CSV, Scene, Prefab, Tilemap에도 변경이 없다.

아직 구현하지 않은 범위는 실제 Tilemap canvas 생성, 12x8 slice, cleanup/density 검증, Scene/Prefab/GameObject 반영, gameplay/NPC/reward spawn, production seed 승인이다. 이 책임은 이번 결과로 자동 개방되지 않으며 다음 owner인 MAP16_02가 계속 `LOCKED` 상태로 담당한다. 따라서 Editor와 게임 화면에서 새로 보이는 시각적 변화는 없다.

Focused verification:

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP16_01]
discovered: 10
executed: 10
passed: 10
failed: 0
skipped: 0
inconclusive: 0
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

후보 patch SHA-256은 `022fcd69b825c127e96d2d2515231c8646d362ff58c474ca6d4ec420ee247d90`이며 설치본과 archive는 각각 20090 bytes로 byte-exact 일치한다. 요구된 MAP15_07 Result SHA-256 `1bf40f24898f41f6f004a9b363262d287445200e5f6c223edf2ea35386300dc8` 및 installed Task SHA-256 `28992f41ceb77c41e6dc87fc245414e7e2979832693521174f347c28f0de5bb5`도 적용 전에 일치했다. inbox 후보는 적용 후 0개였고, 작업 시작 전 staged file은 0개였다.

## Responsibility and Added Functions

### `Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorFinalCanvasLayerPlan.cs`

- `FinalCanvasLayerKind`, `FinalCanvasCellKind`, `FinalCanvasSourceOwner`, `FinalCanvasClaimPriority`, `FinalCanvasProtectionKind`, `FinalCanvasConflictKind`, `FinalCanvasLayerFailureCode`: 레이어 값, 소유자, 고정 precedence, 보호 및 실패 의미를 명시한다.
- `FinalCanvasCellCoordinate`: `(x, y)`를 입력받아 48x32 bounds, row-major identity와 stable ordering을 제공한다.
- `FinalCanvasLayerClaim`: coordinate/layer/value/source owner/priority/provenance/protection 입력을 immutable claim과 stable token으로 변환한다.
- `FinalCanvasCell`: 한 coordinate의 7개 winning claim을 읽기 전용 cell로 발행하고 `Winner(layer)` 조회를 제공한다.
- `FinalCanvasLayerSummary`: layer와 winner count를 deterministic summary로 발행한다.
- `FinalCanvasConflict`, `FinalCanvasLayerFailure`: 덮어쓰기 또는 입력 위반을 typed, sorted evidence로 발행한다.
- `FinalCanvasLayerRequest`: upstream identity와 claim 및 금지 operation counter를 입력받아 immutable request와 canonical input digest를 만든다.
- `SectorFinalCanvasLayerPlan`: 1536 cells, 7 summaries, source/priority counts, precedence/protection/mutation 증거와 downstream handoff를 읽기 전용으로 발행한다.
- `FinalCanvasLayerResult`: 성공 시 완전한 plan과 두 digest를, 실패 시 plan 없는 sorted failure/conflict를 반환한다.
- `FinalCanvasLayerDigest.ComputeInput`, `ComputeOutput`, `HashCanonicalText`, `IsLowerHexSha256`: path/time/random/object identity와 무관한 UTF-8 LF canonical SHA-256 입력·출력과 형식 검증을 제공한다.

### `Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorCanvasLayerFinalizer.cs`

- `SectorCanvasLayerFinalizer.Finalize(FinalCanvasLayerRequest) -> FinalCanvasLayerResult`: upstream approval, 48x32/1536 coordinate, 모든 cell의 7개 layer, explicit owner/provenance/reason, digest identity, mutation 0을 검증한다. 각 coordinate/layer를 priority 내림차순, source owner 내림차순, provenance 오름차순, claim id 오름차순으로 결정하고, typed conflict가 하나라도 있으면 partial plan 없이 실패한다.
- `ReferencePublicationLabel`: test/reference 전용 publication authority를 고정하고 production approval로 오인되지 않도록 한다.

### `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/SectorCanvasLayerFinalizerTests.cs`

- `ReferenceFinalCanvasFixture`: public approved 48x32 sector 상수와 MAP15_07 world assembly, MAP15_06 overlay, MAP14 ownership/protection, MAP08 boundary, MAP07 fixed authority identity를 읽어 1536x7 synthetic claim 입력을 구성한다.
- 10개 `[Category("MAP16_01")]` test: 정상 plan과 수치, 좌표 완전성, MAP07/MAP08 precedence, protected route/special entrance 거부, conflict 결정성, owner/provenance 완전성, digest repeat/reverse/culture 결정성, invalid input atomic failure, mutation 0, MAP16_02 lock을 검증한다.

소비한 public authority는 MAP15_07 exit approval/digest, MAP15_06 world assembly/overlay identity, MAP14 sector ownership/protected route identity, MAP08 boundary aperture authority, MAP07 fixed canvas authority와 `WorldGenConstants.SectorWidthTiles/SectorHeightTiles`이다. upstream 구현 파일은 수정하지 않았다.

production/Editor/CSV/Scene/Prefab/Tilemap 변경은 없고, 새 Runtime model/finalizer와 EditMode test 및 각 `.meta`만 추가했다. downstream owner는 `MAP16_02_VALIDATE_PROTECTION_CLEANUP_AND_DENSITY`이며 이 Result는 해당 task를 시작하거나 잠금을 해제하지 않는다.

Commit subject: MAP16_01: finalize canvas layers precedence
Push: NOT PERFORMED
