TASK: MAP16_02_VALIDATE_PROTECTION_CLEANUP_AND_DENSITY
STATUS: PASS
MAP16_02: COMPLETE ELIGIBLE only when PASS
MAP16_03_VALIDATE_FINAL_ROUTE_AND_RECOVERY: LOCKED / DO NOT START

## User-Facing Implementation Report

이번 작업은 MAP16_01의 48x32 final canvas를 읽어 protection 침범, cleanup 후보와 안전 projection, solid/reachable density, unowned AIR 영역을 판정하는 순수 메모리 validation report를 추가했다. 실제 Tilemap bake, 12x8 slice 생성, Scene/Prefab/GameObject 변경, 파일/Generated asset export, gameplay runtime 또는 player traversal은 구현하거나 실행하지 않았다.

추가한 스크립트와 책임은 다음과 같다.

- `Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorCanvasProtectionDensityReport.cs`: protection intrusion, cleanup candidate/projection, density budget, unowned AIR region, typed failure/result와 canonical digest를 immutable public model로 제공한다.
- `Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorCanvasProtectionDensityValidator.cs`: 성공한 `SectorFinalCanvasLayerPlan`을 입력받아 보호 권위를 분류하고 침범·cleanup·density·unowned AIR를 결정적으로 검증하며, 위반 시 partial report 없이 원자적으로 실패한다.
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/SectorCanvasProtectionDensityValidatorTests.cs`: `REFERENCE PROTECTION CLEANUP DENSITY REPORT`로 명시된 test-only 48x32 fixture와 정확히 10개의 MAP16_02 focused test를 제공한다.

새로 가능해진 기능은 MAP16_01 canvas 자체를 바꾸지 않고 다음 단계가 소비할 수 있는 typed validation packet을 만드는 것이다. ProtectedOpen, boundary aperture, fixed slice, Special entrance 권위를 공개 winner의 layer/source/protection/priority로 분류하고, 침범은 여섯 종류의 typed evidence로 거부한다. 1-cell noise, head snag, shallow pit, one-cell lip, unowned pocket을 cleanup 후보로만 제안하며 보호 권위 셀은 projection 변경 대상에서 제외한다. solid와 abstract reachable cell은 permille budget으로, 목적 없는 AIR는 deterministic flood와 bounding box로 검증한다.

최종 accepted reference report의 관측값은 다음과 같다.

- sector 48x32, cell 1536/1536, unique coordinate 1536/1536, out-of-bounds 0
- layer required/covered/missing 7/7/0
- protected cell 4, ProtectedOpen 1, fixed 1, boundary aperture 1, Special entrance 1
- protection intrusion 0: `ProtectedOpenSolidIntrusion` 0, `ProtectedOpenHazardIntrusion` 0, `BoundaryApertureBlocked` 0, `FixedSliceOverwritten` 0, `SpecialEntranceBlocked` 0, `ProtectionLayerMissing` 0
- synthetic invalid plan에서 위 여섯 intrusion kind를 모두 검출했고 모든 실패는 report와 digest가 없는 atomic failure였다.
- cleanup candidate kind required/covered/missing 6/6/0, 총 17건: single Solid noise 1, single AIR noise 4, head snag 1, shallow pit 1, one-cell lip 4, unowned AIR pocket 6
- cleanup projection 제안 12셀; ProtectedOpen/fixed/boundary/Special 변경 0/0/0/0, 총 보호 권위 변경 0
- solid 768/1536, 500 permille, 승인 envelope 400..650, PASS
- abstract reachable 715/1536, 465 permille, 승인 envelope 350..550, PASS
- density budget 5종 모두 PASS, violation 0
- 최대 unowned AIR box width 8, height 6, area 48, 승인 한계 8x6/48, violation 0
- input digest: `9c73c1735ba7bc9df1d30c1b03098d86922b1b755698e411d6833f8182f7b090`
- output digest: `b14b8ebd68ce434c25ce6b89683f8864039c3379c18951de0a616113760af9c4`
- repeat/reverse/tr-TR culture digest mismatch 0

새 RNG draw, 12x8 slice 생성, generated file write, Tilemap/Scene/Prefab/GameObject mutation, gameplay spawn, production seed 승인, sector reroll, fallback carve, full regression은 모두 0이다. MAP16_01의 source plan 객체, cell stable token, input/output digest 및 MAP15/MAP14/MAP08/MAP07 upstream identity는 검증 전후 동일했다. 기존 Runtime/Editor/test/CSV/Scene/Prefab/Tilemap/asmdef/ProjectSettings/Packages 파일은 수정하지 않았다.

이번 결과는 test-only deterministic reference report이며 production seed 또는 MAP16 phase exit를 승인하지 않는다. 아직 구현하지 않은 범위는 실제 canvas cleanup 적용, final route/player recovery 승인, collider/physics/PlayMode traversal, 12x8 partition/slice, Tilemap bake, runtime spawn이다. 이 책임은 자동 개방되지 않은 MAP16_03 이후 작업에 남아 있으며, Editor와 게임 화면의 가시적 변화는 없다.

Focused verification:

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP16_02]
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

후보 patch SHA-256은 `66c646bdf389be2143ece4f63dcdf64075a508003aa12778387c87bf2bb89a1c`이며 installed Task와 archive는 각각 21376 bytes로 byte-exact 일치한다. 적용 전 요구된 MAP16_01 Result SHA-256 `c3be5d6a37259a431280e7ed3502e0d021819a9a4f41a99f10b5767e6a2a8657` 및 installed Task SHA-256 `022fcd69b825c127e96d2d2515231c8646d362ff58c474ca6d4ec420ee247d90`도 일치했다. 적용 후 inbox MD와 staged file은 각각 0개였다.

## Responsibility and Added Functions

### `Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorCanvasProtectionDensityReport.cs`

- `ProtectionIntrusionKind`, `CleanupCandidateKind`, `CleanupProjectionState`, `DensityBudgetKind`, `DensityBudgetVerdict`, `UnownedAirRegionKind`, `ProtectionDensityFailureCode`: 침범, cleanup, budget 및 실패 의미를 typed enum으로 고정한다.
- `SectorCanvasProtectionIntrusion`: coordinate/layer/source owner/claim/reason 입력을 stable sorted intrusion evidence로 발행한다.
- `SectorCanvasCleanupCandidate`: 현재 cell과 제안 cell, source/claim/reason 입력을 typed cleanup evidence로 발행한다.
- `SectorCanvasCleanupProjection`: candidate coordinate 입력을 중복 없는 read-only 제안 목록과 보호 권위별 변경 수, safety verdict로 변환한다.
- `SectorCanvasDensityBudget`: budget kind와 observed/min/max 입력을 PASS/FAIL verdict와 stable token으로 변환한다.
- `SectorCanvasUnownedAirRegion`: flood 결과의 min/max coordinate와 area 입력을 width/height/area 및 bounded/oversized verdict로 변환한다.
- `SectorCanvasProtectionDensityReport`: MAP16_01 source plan과 모든 검증 산출물을 immutable/read-only packet으로 묶고 count, permille, 최대 box, mutation proof와 MAP16_03 handoff를 공개한다.
- `ProtectionDensityFailure`, `ProtectionDensityResult`: code/subject/reason의 deterministic failure와 성공 report 또는 atomic no-report 결과를 제공한다.
- `ProtectionDensityDigest.ComputeInput`, `ComputeOutput`, `HashCanonicalText`, `IsLowerHexSha256`: MAP16_01 digest·sorted cell과 sorted validation evidence를 UTF-8 LF/invariant lower-hex SHA-256으로 변환한다.

### `Assets/_Game/Map/Runtime/WorldGeneration/Baking/SectorCanvasProtectionDensityValidator.cs`

- `SectorCanvasProtectionDensityValidator.Validate(SectorFinalCanvasLayerPlan) -> ProtectionDensityResult`: 공개 MAP16_01 plan의 크기, 좌표, 7개 layer, source/provenance, digest, mutation 0을 검증한 뒤 protection/cleanup/density/unowned AIR report를 만든다. intrusion, budget, unowned box 또는 digest 위반 시 partial report 없이 실패한다.
- `ClassifyAuthority`: per-cell winning claims -> ProtectedOpen/fixed/boundary/Special/explicit protection facts.
- `DetectProtectionIntrusions`: authority facts와 Terrain/Material/Hazard/Protection winner -> 여섯 typed intrusion 목록.
- `BuildCleanupCandidates`: 4-neighbor terrain probe와 unowned region -> six-kind cleanup evidence.
- `BuildCleanupProjection`: cleanup evidence와 authority mask -> 보호 권위를 제외한 read-only changed-coordinate proposal 및 실제 권위별 변경 수.
- `CountAbstractReachableCells`: 공개 Terrain/Affordance와 authority seed -> orthogonal deterministic flood count. 이는 physics/player traversal 판정이 아니다.
- `BuildUnownedAirRegions`: 목적 없는 AIR cells -> sorted connected regions와 8x6/48 box evidence.
- `ProtectionDensityDigest`: sorted input/output evidence -> deterministic digest.

### `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/SectorCanvasProtectionDensityValidatorTests.cs`

- `ReferenceProtectionDensityFixture`: MAP16_01 공개 claim/request/finalizer API와 48x32 상수를 소비해 test-only 1536x7 reference plan 및 synthetic invalid variants를 만든다.
- 정확히 10개의 `[Category("MAP16_02")]` test: report 수치, protection 0, six cleanup kinds, projection safety, 두 density envelope, unowned AIR limit, repeat/reverse/culture digest, 여섯 typed invalid intrusion의 atomic failure, mutation 0, MAP16_03 lock을 검증한다.

소비한 public authority는 MAP16_01 `SectorFinalCanvasLayerPlan`, `FinalCanvasCell`, `FinalCanvasLayerClaim`, public counts/digests와 MAP15_07 exit, MAP15_06 world assembly, MAP14 protected route/ownership, MAP13 Special entrance, MAP08 boundary aperture, MAP07 fixed canvas를 전달하는 공개 request identity다. live production canvas가 별도로 노출되지 않은 상태이므로 test는 명시된 reference fixture만 사용하며 물리 CSV나 private field를 읽지 않는다.

새 Runtime production 파일 2개와 focused EditMode test 1개 및 matching `.meta`만 추가했다. upstream production 구현 수정, Editor/CSV/Scene/Prefab/Tilemap 변경은 모두 없다. downstream owner는 `MAP16_03_VALIDATE_FINAL_ROUTE_AND_RECOVERY`이며 이 Result는 해당 task를 시작하거나 잠금을 해제하지 않는다.

Commit subject: MAP16_02: validate protection cleanup density
Push: NOT PERFORMED
