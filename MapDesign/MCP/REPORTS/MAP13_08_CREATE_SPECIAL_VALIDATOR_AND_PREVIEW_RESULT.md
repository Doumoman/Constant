TASK: MAP13_08_CREATE_SPECIAL_VALIDATOR_AND_PREVIEW
STATUS: PASS
MAP13_08: COMPLETE ELIGIBLE
MAP13_09_MAP13_SPECIAL_REGION_EXIT_TESTS: LOCKED / DO NOT START

## User-Facing Implementation Report

MAP13_01~07의 public plan을 exact 10개 immutable audit input으로 투영하고, footprint/buffer/site/layer/route/state/reset/persistence를 atomic audit하는 Runtime validator와 read-only Unity Editor preview를 추가했다. 설치 Task와 archive의 SHA-256은 모두 `c5cd8963ecaa112f6a4cbc2d14568921e38f8eb484a4f8138a8bc4cd293c4d62`로 byte-identical하다.

사용자는 `Tools/MapDesign/Special Region Validator & Preview`에서 Family/Artifact/View를 고르고 13개 overlay를 켜거나 끄면서 scale-to-fit grid, text+token legend, binding/provenance/physics banner, 8-section audit와 4단계 digest를 볼 수 있다. 창은 Save/Apply/Fix/Generate/Bake/Export를 제공하지 않으며 source plan, Scene, asset, Generated content를 변경하지 않는다.

실제 audit publication 수치는 다음과 같다.

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

성공 report는 artifact `10`, section `80 PASS / 0 FAIL`, error `0`, binding `8 REFERENCE FIXTURE / 2 DEFERRED TO MAP14`, mutation/solver/gameplay claim `0/0/0`을 게시한다. 모든 mandatory route는 ordered no-tool witness이며 synthetic edge/teleport/carve/pathfinding/world mutation을 만들지 않는다. Merchant/Maru는 footprint, world origin, reservation, bridge, buffer, fixed-slot, placed ownership claim이 모두 0이다.

Preview 실제 UI 수치와 open evidence:

- menu 등록: 447개 Unity menu item 중 exact menu 1개 확인
- 실제 window: title `Special Region Validator & Preview`, type `SpecialRegionPreviewWindow`, instance ID `-22062`, focused, position `(1037,685)`, size `1000×680`
- selector: Family 3값, Artifact 10값, View 8값(`Overview/Footprint/Layers/Routes/States/Reset/Audit/Compare`)
- overlay toggle: 13개, legend: 18개, binding label 종류: 2개, physics warning: `PHYSICS NOT VERIFIED` 1종
- default snapshot: Village 1×1 Overview, visible token 42개, `REFERENCE FIXTURE` 및 “not a live/generated world” provenance 표시
- 실제 scroll audit: `8 PASS / 0 FAIL`; Identity 1, FootprintBindingBuffer 1, FixedCollision 1, FixedAccess 2, ReplaceableSlots 2, Routes 6, States 5, ResetPersistence 0
- default source/component/artifact/audit digest는 각각 64-hex로 표시됐다. audit digest는 `a7ab6fd571425c4c8e64d7eecad5dd246a3d9a8a08044801800948fc2fa03e4e`다.
- 창 1개를 실제로 열어 selector/grid/legend/binding banner/audit panel을 캡처 확인한 후 닫았고, 열린 window 목록에서 제거됨을 확인했다.

새 파이프라인 위치:

```text
MAP13_01~07 public plans/catalogs/digests
→ deterministic in-memory REFERENCE FIXTURE / DEFERRED TO MAP14 projection
→ SpecialRegionValidationAuditor atomic cross-proof
→ immutable report, sections, errors and stable digests
→ SpecialRegionPreviewModel filtering
→ read-only SpecialRegionPreviewWindow
→ 별도 검수 후 MAP13_09 phase-exit consumer
```

이제 Editor에서 family/artifact별 placed shell과 deferred-local 경계를 한 화면에서 비교하고, footprint/seam/buffer/fixed-access/replaceable slot/route/state/reset/persistence witness와 digest drift를 검사할 수 있다. invalid input은 partial report/grid 없이 accumulated·deduplicated·stable-sorted error만 반환하고, reverse input/repeat/`tr-TR`에서도 digest가 동일하다.

아직 미구현한 범위는 physical SpecialRegion CSV/schema/importer/serializer, production live world catalog와 MAP03/MAP14 placement solver, player collider·physics reachability, gameplay object spawn, item/crafting/reward/save 실행, Scene/Prefab/Tilemap authoring, edit/apply/auto-fix/generate/bake/export, MAP13_09+다. `REFERENCE FIXTURE`는 실제 생성 월드가 아니며 `DEFERRED TO MAP14`는 placed 상태가 아니다.

Editor/게임 가시성: Editor에서는 새 메뉴와 read-only 창이 보인다. 게임 runtime 화면, Scene hierarchy, Prefab, Tilemap, gameplay object에는 가시 변화가 0이며 PlayMode는 실행하지 않았다.

## Responsibility and Added Functions

| Field | Evidence |
|---|---|
| Task responsibility | exact MAP13 audit + read-only Editor preview |
| Added scripts | Runtime 2 + Editor production 2 + focused Editor test 1; exact 경로와 matching meta는 아래 표에 기록 |
| Inputs consumed | MAP13_01 site bridge, MAP13_02 entry/buffer/collision, MAP13_03 fixed/replaceable/persistence, MAP13_04 Village shell, MAP13_05 Village variants, MAP13_06 Core plans, MAP13_07 landmark plans/catalogs/digests |
| Outputs produced | immutable audit request/result/report/section/artifact, stable errors/digests, filtered preview snapshot, read-only Editor window |
| Explicit non-ownership | live placement/CSV, physics/gameplay, edit/apply/fix/generate/bake/export, Scene/Prefab/Tilemap, MAP13_09 approval |
| Downstream consumer | 별도 검수 뒤 MAP13_09만 unlock 가능; 이 작업은 MAP13_09를 시작하지 않음 |

추가한 모든 script와 class/method별 책임·input→output:

| Script | Class/method | 책임 | Input → Output |
|---|---|---|---|
| `Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionValidationAudit.cs` | audit family/binding/section/error/token enums | exact family, binding, 8 section, 24 atomic error group, 18 preview token vocabulary 고정 | enum value → canonical semantic identity |
| 같은 파일 | `SpecialRegionValidationAuditError` + `CompareTo/Equals/GetHashCode/ToString` | error code/path/detail을 immutable value로 보존하고 stable sort/dedup key 제공 | finding fields → comparable error |
| 같은 파일 | `SpecialRegionAuditRoute`, `SpecialRegionAuditToken` | ordered route witness와 grid token coordinate/text 보존 | authored witness/token fields → immutable route/token |
| 같은 파일 | `SpecialRegionAuditMetrics` + `WithViolation` | footprint/site/layer/route/state/reset/risk/deferred/mutation proof 수치와 invalid test copy 작성 | proof facts 또는 error code → immutable metrics/corrupted copy |
| 같은 파일 | `SpecialRegionAuditArtifactInput` + `WithViolation` | artifact source/component/digest, counts, routes/tokens/metrics를 defensive-copy | fixture/public plan projection → canonical artifact input |
| 같은 파일 | `SpecialRegionAuditRequest` | caller input을 defensive-copy | artifact sequence → immutable request |
| 같은 파일 | `SpecialRegionAuditSectionResult`, `SpecialRegionAuditArtifactResult` | section별 PASS count/detail/digest와 artifact aggregate 게시 | validated artifact facts → immutable section/artifact result |
| 같은 파일 | `SpecialRegionValidationReport`, `SpecialRegionValidationAuditResult` | 10-artifact aggregate 수치 또는 atomic errors 게시; failure에서는 report/digest 미게시 | artifact results/errors → success report or errors |
| `Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionValidationAuditor.cs` | `SpecialRegionValidationAuditor.Audit` | exact canonical 10개, binding/footprint/buffer/site/layer/routes/state/reset/risk/deferred/zero-mutation 교차 검증 | `SpecialRegionAuditRequest` → `SpecialRegionValidationAuditResult` |
| 같은 파일 | `SpecialRegionValidationCanonicalDigest.ComputeSection` | section identity/count/detail material digest | artifact ID + section fields → 64-hex digest |
| 같은 파일 | `ComputeArtifact` | input/source/component/section publication digest | artifact input + 8 section results → 64-hex digest |
| 같은 파일 | `ComputeReport` | canonical ordered artifact aggregate digest | success report → 64-hex audit digest |
| `Assets/_Game/Editor/MapAuthoring/WorldGeneration/SpecialRegions/SpecialRegionPreviewModel.cs` | `SpecialRegionPreviewViewMode`, `SpecialRegionPreviewOverlay` | exact 8 view와 13 overlay 계약 | selector/toggle value → typed preview mode/filter |
| 같은 파일 | `SpecialRegionPreviewSelection`, `SpecialRegionPreviewLegendEntry` | selected family/artifact와 color-independent token meaning 보존 | selection/legend fields → immutable values |
| 같은 파일 | `SpecialRegionPreviewSnapshot`, `SpecialRegionPreviewBuildResult` | filtered tokens, bounds/design metadata, banner/warning, audit counts/digests 또는 atomic inline errors 게시 | selected artifact + filter + audit → immutable snapshot/result |
| 같은 파일 | `SpecialRegionPreviewModel` constructor / `Reload` | public MAP13 compilers로 exact 10 fixture를 다시 만들고 auditor 실행; static mutable cache 없음 | none → refreshed immutable inputs/audit/default selection |
| 같은 파일 | `BuildDefault` / `Build` | default 또는 명시 selection/view/overlay의 scale-to-fit token projection 생성 | selection + view + toggles → preview build result |
| 같은 파일 | `TrySelectArtifact` / `TrySelectViewMode` | exact ID/string을 typed selector value로 안전하게 해석 | selector text → bool + typed selection/mode |
| 같은 파일 | internal `SpecialRegionReferenceFixtureFactory.BuildAll` 및 Village/Core/Landmark/placed helper group | existing public constructors/compilers 결과를 exact 10 deterministic audit inputs로 투영; parser/validator/graph logic과 live world를 소유하지 않음 | MAP13 public catalogs/plans → ordered read-only artifact inputs |
| `Assets/_Game/Editor/MapAuthoring/WorldGeneration/SpecialRegions/SpecialRegionPreviewWindow.cs` | `Open` / `CloseAllOpen` | exact menu/title/min-size window lifecycle | menu call 또는 open windows → opened window / closed count |
| 같은 파일 | `Reload` | explicit button action으로 model rebuild, inline error 보존 | user click → bool + current snapshot/error |
| 같은 파일 | `TrySelectFamily` / `TrySelectArtifact` / `TrySelectViewMode` / `TrySetOverlay` | selector/toggle 변경 후 read-only snapshot rebuild | UI value → bool + filtered snapshot |
| 같은 파일 | `OnGUI`와 selector/banner/grid/legend/audit draw helper group | selector 3종, toggle 13개, banner, scroll grid, text legend, details/audit/error를 IMGUI로 렌더 | current immutable snapshot → Editor-only pixels, no asset mutation |
| `Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/SpecialRegions/SpecialRegionValidationPreviewTests.cs` | `SpecialRegionValidationPreviewTests` 12 focused tests | exact 10/3-3-4, 8/2 binding, audit proof/count, recovery/zero mutation, deferred claims, atomic failure, determinism/immutability, UI contract, open-close no-mutation 검증 | public auditor/model/window → NUnit PASS/diagnostic |

기존 `SpecialRegionValidationErrorCode/Error/Result` 이름은 MAP09 public API가 이미 소유하므로 기존 파일을 수정하지 않고 이번 audit 전용 충돌 없는 `SpecialRegionValidationAuditErrorCode/Error/Result`를 추가했다. auditor/report/preview의 의미와 atomic contract는 MAP13_08 범위로 분리된다.

## Focused Verification

최종 authoritative Unity selection:

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP13_08]
job_id: 53748f7929f840f6bb828b2f8783ced8
discovered: 12
executed: 12
passed: 12
failed: 0
skipped: 0
inconclusive: 0
resultState: Passed
durationSeconds: 1.3194078
compile errors: 0
relevant Console errors after final verification/clear: 0
relevant Console warnings after final verification/clear: 0
```

첫 category job `1f2f148dc3864cda9b6b5727dbe4a66b`은 신규 파일 import 전이라 0개를 실행했고, 전체 AssetDatabase refresh 후 exact 12개가 discovery됐다. 중간 job `20829f61a2f3416f89472ceb2dc8d1c3`은 신규 test의 recovery 집계 기대값 오류 1건을 발견했고 실제 report 수치 9로 고쳤다. 이후 모든 실행은 `MAP13_08` EditMode category만 선택했다. 최종 Test Framework의 결과 저장 알림 1건과 setup/cleanup 알림 2건은 test/compile 오류가 아니며 확인 후 Console을 clear해 오류/경고 0건을 재확인했다.

## Window and Static Side Effects

실제 window open→visual inspection→close와 focused open→Reload→selection/toggle→close에서 active scene path, root count, dirty state, Unity selection, Authoring/Generated inventory가 동일했다. 창은 닫힌 뒤 open window 목록에 없었다. 임시 시각 캡처는 workspace 밖 OS temp에만 작성했으며 task commit 대상이 아니다.

```text
new Runtime C#/meta: 2/2
new Editor production C#/meta: 2/2
new focused Editor test C#/meta: 1/1
new allowed SpecialRegions folder meta: 2
focused [Test] / Category attributes: 12 / 1
existing C#/test/CSV/meta modifications: 0
new/modified Authoring or Generated CSV/meta: 0
schema registry/test modifications: 0
MAP09/MAP13_01~07 production/test modifications: 0
PlayMode/Scene/Prefab/Tilemap/Settings/Packages changes: 0
duplicate GUID groups: 0
unapplied inbox candidate/diff-check/unrelated staged: 0/0/0
unrelated included paths: 0
Git push: NOT PERFORMED
```

기존 dirty `Constant.slnx`와 untracked `TerrainClusters.meta` 3개는 수정하거나 stage하지 않았다. atomic commit에는 installed/archive Task, Runtime/Editor/test C#·meta, 허용된 folder meta 2개, 이 Result, finalized Status만 포함한다.

REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0

Commit subject: `MAP13_08: add SpecialRegion validator and preview`

Push: NOT PERFORMED
