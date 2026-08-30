TASK: MAP13_09_MAP13_SPECIAL_REGION_EXIT_TESTS
STATUS: PASS
MAP13_09: COMPLETE ELIGIBLE
MAP14_01_BUILD_PLANNER_INPUT_AND_PACING_ROLE: LOCKED / DO NOT START

## User-Facing Implementation Report

이번 Task가 추가한 것은 production 기능이 아니라 MAP13 phase-exit focused Editor test script 1개다. `MAP13_01~07 public reference plans/catalogs → MAP13_08 audit/preview publication → MAP14 planner input` 경계를 10개 `MAP13_09` EditMode test로 검증했다. production Runtime/Editor C#, CSV/schema/importer, Scene/Prefab/Tilemap과 기존 test는 수정하지 않았다.

MAP13이 이번 PASS로 승인하는 범위는 SpecialRegion reference contract의 canonical publication, placed-reference site/buffer/fixed-layer 무모순, Village의 비강제 local shell/state 경계, CoreResource의 reward/recovery/persistence proof, Forge/Boss의 seal/reset proof, Merchant/Maru의 MAP14 deferred boundary, 그리고 MAP13_08 read-only preview model을 MAP14 planner input이 소비할 수 있다는 것까지다.

아직 승인하지 않는 범위는 live generated-world placement/publication, MAP03/MAP14 placement solver, player collider/physics reachability, reward/save/inventory/crafting 실행, Boss AI/combat/HP/attack, 실제 NPC/shop/hint/gameplay object, production CSV/schema/importer, Scene/Prefab/Tilemap/bake/streaming이다.

실제 canonical publication 수치는 다음과 같다.

| Family | Artifacts | Routes | Recovery routes | States | Resets | Persistence checkpoints | Preview tokens |
|---|---:|---:|---:|---:|---:|---:|---:|
| Village | 3 | 19 | 0 | 15 | 0 | 0 | 149 |
| CoreResource | 3 | 9 | 3 | 21 | 3 | 21 | 92 |
| Landmark | 4 | 18 | 6 | 25 | 9 | 7 | 194 |
| Total | 10 | 46 | 9 | 61 | 12 | 28 | 435 |

| Artifact | Binding | Sections | Routes / Recovery | States | Resets | Checkpoints | Tokens |
|---|---|---:|---:|---:|---:|---:|---:|
| `SR_MAP13_08_VILLAGE_1X1` | REFERENCE FIXTURE | 8 | 6 / 0 | 5 | 0 | 0 | 42 |
| `SR_MAP13_08_VILLAGE_1X2` | REFERENCE FIXTURE | 8 | 6 / 0 | 5 | 0 | 0 | 48 |
| `SR_MAP13_08_VILLAGE_2X1` | REFERENCE FIXTURE | 8 | 7 / 0 | 5 | 0 | 0 | 59 |
| `SR_CASSIA_SAP_SITE_5` | REFERENCE FIXTURE | 8 | 3 / 1 | 7 | 1 | 7 | 31 |
| `SR_MOON_CORE_SITE_5` | REFERENCE FIXTURE | 8 | 3 / 1 | 7 | 1 | 7 | 30 |
| `SR_STAR_NURUK_SITE_5` | REFERENCE FIXTURE | 8 | 3 / 1 | 7 | 1 | 7 | 31 |
| `SR_MARU_TIME_SHRINE_5` | DEFERRED TO MAP14 | 8 | 4 / 1 | 4 | 2 | 0 | 33 |
| `SR_MOON_BOSS_SEAL_ARENA_12` | REFERENCE FIXTURE | 8 | 5 / 2 | 4 | 3 | 0 | 60 |
| `SR_MOON_SEAL_FORGE_9` | REFERENCE FIXTURE | 8 | 6 / 3 | 14 | 3 | 7 | 76 |
| `SR_WANDERING_MERCHANT_CAVE_3` | DEFERRED TO MAP14 | 8 | 3 / 0 | 3 | 1 | 0 | 25 |

aggregate는 artifact `10`, family `3/3/4`, binding `8 REFERENCE FIXTURE / 2 DEFERRED TO MAP14`, section `80 PASS / 0 FAIL`, audit error `0`, route/recovery/state/reset/checkpoint/token `46/9/61/12/28/435`, mutation/solver/gameplay claim `0/0/0`이다. source/component/artifact/section/report digest 122개는 모두 64자리 lowercase hex였고 repeat/reverse input/`tr-TR`에서 동일했다. public audit digest는 `a7ab6fd571425c4c8e64d7eecad5dd246a3d9a8a08044801800948fc2fa03e4e`와 일치했다.

Editor 가시성은 기존 `Tools/MapDesign/Special Region Validator & Preview`가 계속 제공한다. MAP13_09는 새 EditorWindow나 게임 화면을 만들거나 열지 않았다. default model은 Village 1×1 Overview, visible token `42`, audit section `8 PASS / 0 FAIL`, view `8`, overlay toggle `13`, legend `18`, binding label `2`, physics warning `PHYSICS NOT VERIFIED`를 게시했다. active scene `Assets/_Game/Scenes/MapGenerationProgressTest.unity`, root `3→3`, dirty `false→false`, selection `0→0`, Authoring/Generated inventory delta `0`이었다. 게임 내 신규 가시 요소는 없다.

## Responsibility and Added Functions

추가 script:

`Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/SpecialRegions/Map13SpecialRegionExitTests.cs`

matching meta:

`Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/SpecialRegions/Map13SpecialRegionExitTests.cs.meta`

test class: `StarNight.Map.Editor.Tests.WorldGeneration.SpecialRegions.Map13SpecialRegionExitTests`

| Test method | 책임 | Input → Output |
|---|---|---|
| `CanonicalPublicationPublishesExactPhaseExitTotals` | canonical 10-artifact publication과 모든 aggregate 수치 승인 | `SpecialRegionPreviewModel.AuditResult.Report` → 10 artifacts, 3/3/4 families, 8/2 bindings, 80/0 sections, 46/9 routes, 61 states, 12 resets, 28 checkpoints, 435 tokens, zero claims |
| `CanonicalDigestsRemainLowerHexAcrossRepeatReverseAndTurkishCulture` | 모든 공개 digest 형식·고정값·순서/문화권 안정성 승인 | canonical artifacts + repeat/reverse/`tr-TR` audit → 122개의 64-hex digest와 fixed audit digest 일치 |
| `MandatoryReferenceSitesClosePlacementAndOverlapWithoutVillageRewardDependency` | Village 3/Core 3/Forge 1/Boss 1 placed-reference claim과 overlap/risk 0 승인 | 8 reference artifacts → exact single world/reservation/bridge/ownership claims, overlap/tool/unrecoverable/mutation 0, Village reward dependency 0 |
| `VillagesPublishFiveShellPreservingLocalVariantsFacilitiesAndSeams` | Village 1×1/1×2/2×1 local road/facility/state/seam 경계 승인 | 3 Village audit artifacts → 5 states each, local Entry→road→facility/access→Return, recovery/checkpoint/reward 0, multi-sector seam evidence |
| `CoreResourcesPublishOneRewardSevenCheckpointsAndRecoveryRejoin` | MoonCore/CassiaSap/StarNuruk reward·persistence·recovery·dependency closure 승인 | 3 public definitions + audit artifacts → reward 1/checkpoints 7/recovery 1 each, Failure→RecoveryJoin→Low, loss/duplicate/tool/Village/inventory/facility dependency 0 |
| `ForgePublishesOrderedMoonSealProcessAndLosslessManualResets` | Forge process, MoonSeal output, three-resource return과 safe reset 승인 | public Forge catalog + audit artifact → Grind→Mix→Press→MoonlightCure→MoonSeal, 3 ledgers, 3 ManualReset→SafeCorridor, loss/duplicate/mutation 0 |
| `BossPublishesSealGateStateOrderAcceptedResetAndCentralRecovery` | Boss seal gate/state/reset/recovery/no-new-movement 경계 승인 | public Boss catalog + audit artifact → GateLocked→GateAccepted→EncounterActive→Defeated, MoonSeal marker, accepted-seal-preserving reset, 2 central recoveries, gameplay claim 0 |
| `OptionalMerchantAndMaruRemainDeferredLocalAndNonProgression` | Merchant/Maru의 MAP14 deferred-local ownership 경계 승인 | 2 public optional definitions + audit artifacts → footprint/world claims 0, local Entry→interaction/choice→Return, mandatory dependency 0, Maru preview/persistent-choice/no-reroll proof |
| `PreviewPublishesExactSelectorsOverlaysLegendWarningsAndDefaultSnapshot` | preview public contract의 exact selector/filter/banner 수치 승인 | `SpecialRegionPreviewModel.BuildDefault/Build` → families 3, artifacts 10, views 8, overlays 13, legend 18, binding labels 2, default tokens 42, physics warning |
| `PreviewModelBuildIsReadOnlyForSceneSelectionAndMapDataInventory` | model reload/build의 Editor side effect 0 승인 | scene/selection/inventory snapshot + Reload/Compare build → scene path/root/dirty/selection/inventory unchanged |

upstream source 수정: `0`

production code 추가: `0`

기존 C#/test/CSV/meta 수정: `0`

Scene/Prefab/Tilemap/ScriptableObject/Material/Texture 변경: `0`

PlayMode helper/test 추가: `0`

미구현·미승인 범위는 live placement, physics/gameplay 실행, production data pipeline, rendering/bake/stream/save/population이며 MAP14 이후 owner에게 남긴다.

## Focused Verification

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP13_09]
job_id: bce4d99265354f0e94d734e6983606ab
duration_seconds: 0.8751689
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
```

첫 compile에서 신규 test file의 NUnit constraint 호환 오류 4건을 신규 파일 안에서만 수정했다. 첫 `MAP13_09` focused job `832ddd20481c4ae6879779b941d0117b`은 digest collection 산술 기대값 `132` 오기 1건을 발견해 10 executed / 9 passed / 1 failed였고, 실제 구성 `30 + 10 + 80 + 2 = 122`로 신규 assertion만 바로잡았다. 이후에도 같은 `MAP13_09` EditMode category만 선택했으며 최종 authoritative job은 10/10 PASS다. Test Framework의 result-save와 setup/cleanup 알림은 확인 후 Console을 clear했고 error/warning 0을 재확인했다.

## Approval Boundary

MAP13 PASS는 reference SpecialRegion contracts가 상호 모순 없이 MAP14 planner input으로 넘어갈 수 있다는 승인이다. Village는 방문하지 않아도 되는 local reference shell이며 global progression blocker가 아니다. Merchant/Maru는 여전히 `DEFERRED TO MAP14`이고 placed ownership이 없다. 이 PASS는 live player reachability, physics, runtime gameplay, production placement 또는 asset publication 승인이 아니다.

Commit subject: `MAP13_09: approve SpecialRegion phase exit`

Push: NOT PERFORMED
