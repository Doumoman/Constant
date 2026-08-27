# MAP03_02 — Enumerate Start and Site Candidates

```yaml
status_control:
  task_key: MAP03_02_ENUMERATE_START_AND_SITE_CANDIDATES
  result_file: REPORTS/MAP03_02_ENUMERATE_START_AND_SITE_CANDIDATES_RESULT.md
```

## TASK TYPE

```text
RUNTIME DETERMINISTIC RAW-ORIGIN CANDIDATE ENUMERATION + IMMUTABLE CATALOG + EDITMODE TESTS
```

## Objective

MAP03_01의 immutable reservation models와 승인된 13×13 P00 grid를 사용해 Start, Boss, Forge, 핵심 자원 3개의 **raw origin candidates**를 결정적으로 열거한다.

이번 Task는 후보의 identity/group/order와 origin coordinate만 만든다. footprint cell transform·월드 경계 배치·entry 변환·충돌·거리·비용·Core 용량·RNG 선택·backtracking은 수행하지 않는다.

starter exact output:

```text
Start outer ring 0..1 = 88 candidates (ring 0: 48, ring 1: 40)
Boss groups = 1 x 169
Forge groups = 1 x 169
Core-resource groups = 3 x 169
Special-site raw origins = 845
All groups = 6
All candidates = 933
Village candidates = 0
```

## Mandatory Read Order

1. `00_MCP_ENTRYPOINT.md`
2. `01_PROJECT_LOCKED_RULES.md`
3. `02_MCP_WORK_RULES.md`
4. `03_DATA_CSV_RULES.md`
5. `04_UNITY_MCP_RULES.md`
6. `05_CHANGE_CONTROL_RULES.md`
7. `07_PATCH_APPLY_RULES.md`
8. `08_STATUS_FINALIZE_RULES.md`
9. `MASTER_IMPLEMENTATION_TASK_LIST.md`
10. `06_IMPLEMENTATION_STATUS.md`
11. 이 Task
12. `REPORTS/MAP03_01_IMPLEMENT_SITE_RESERVATION_MODELS_RESULT.md`

MAP03_01 Result의 exact `STATUS: PASS`, focused `81/81`, targeted `1595/1595`, full `1635/1635`, final Assets meta `2998`, existing Assets modification `0`을 확인한다.

## Map Package Reference

Map Package v1.0 exact installed path가 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/01_WORLD_FIXED_SPEC.md
01_FIXED_SPEC/02_COORDINATE_AND_ID_RULES.md
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
01_FIXED_SPEC/05_IMPLEMENTATION_ORDER.md
02_PHASE_ROADMAP/MAP03_SPECIAL_SITE_RESERVATION.md
```

exact 문서가 installed tree에 없으면 이 Task의 frozen contracts를 authoritative fallback으로 사용한다. 대체 문서, 과거 하네스, Legacy generator를 broad search하지 않는다.

## READ ALLOWLIST

### Existing typed definition roots

```text
Assets/_Game/Map/Runtime/WorldGeneration/Data/WorldGenerationDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/WorldRouteDefinitionSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/SpecialMapDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/SpecialVillageDefinitionSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/StaticDataRegistry.cs
```

### Existing grid and reservation models

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGridIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GridInitializationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteFootprint.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteEntryAnchor.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/CoreBiomeSeed.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorReservation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteReservationSnapshot.cs
```

### Focused tests / assemblies

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/WorldRouteDefinitionBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/SpecialVillageDefinitionBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/StaticDataRegistryBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/GridInitializationPassTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteReservationModelsTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

위 C#/asmdef와 matching meta, approved `Generation` Runtime/Test 직계 파일명의 path-only inventory, Authoring CSV/meta count·hash, 전체 meta GUID, task marker 이후 change-scope path만 확인할 수 있다.

금지:

- Authoring CSV body 직접 재파싱·수정
- MAP03_03 이후 Task body
- unrelated production/test C# body
- Legacy/Stage/P6/P11 generator body
- Scene/Prefab YAML

## WRITE ALLOWLIST

### 신규 Runtime production C# — exact 6

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteOriginCandidate.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateGroup.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateCatalog.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateEnumerationError.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateEnumerationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SiteCandidateEnumerator.cs
```

### 신규 Runtime EditMode test — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SiteCandidateEnumerationTests.cs
```

신규 C# 7개와 matching `.cs.meta` 7개, Result 1만 생성한다. 기존 production/tests/meta/asmdef/asmref는 수정하지 않는다. 기존 approved directory를 재사용하며 새 directory/folder meta를 만들지 않는다.

## Namespace / Assembly

```text
Runtime namespace: StarNight.Map.WorldGeneration.Generation
Test namespace:    StarNight.Map.Tests.WorldGeneration.Generation
Runtime assembly:  Game.Map.Runtime
Test assembly:     Game.Map.Tests.EditMode
```

`UnityEditor`, Unity object/lifecycle, record/record struct, `required`, `init`, nullable-reference directive, reflection factory, singleton/static mutable state를 도입하지 않는다.

## Input Contract

public API:

```text
public sealed class SiteCandidateEnumerator

SiteCandidateEnumerationResult Enumerate(
    GridInitializationResult grid,
    WorldProfileDefinition worldProfile,
    GenerationProfileDefinition generationProfile,
    IEnumerable<SpecialMapDefinition> specialMaps)
```

호출자는 immutable Registry roots에서 exact typed definitions를 전달한다. enumerator는 Registry singleton이나 filesystem에서 자체 조회하지 않는다.

input gate:

- `grid`, world/generation profile, specialMaps와 모든 item은 non-null이다.
- grid는 exact 169 cells/index/coordinate identity를 가진다. topology와 cells를 clone/mutate하지 않는다.
- world/generation profile은 active이고 `generationProfile.WorldProfileId == worldProfile.WorldProfileId` ordinal exact다.
- world profile의 fixed width/height/sector values가 `WorldGenConstants`와 일치한다.
- `StartEdgeRingMin/Max`는 `0 <= min <= max <= 6`이며 starter exact `0/1`이다.
- special map ID는 ordinal unique다. inactive definitions는 candidate source에서 제외한다.
- active `VILLAGE`는 MAP03_08 소유이므로 유효하게 제외한다.
- active required unknown site role은 오류다. silent omission하지 않는다.
- expected required site definitions는 아래 exact 5개이며 각각 active/required count `1`이어야 한다.

| Kind | Source definition ID |
|---|---|
| Boss | `SITE_MOON_BOSS_VAULT` |
| Forge | `SITE_MOON_SEAL_FORGE` |
| CoreResource | `SITE_CASSIA_SAP_HEART` |
| CoreResource | `SITE_DEEP_STAR_YEAST` |
| CoreResource | `SITE_MOON_CORE_METEOR` |

각 expected definition은 role token, canonical non-empty primary biome, positive footprint width/height within 13×13, non-negative distance fields, non-empty unique route types `1|2|3`을 만족해야 한다. expected source 누락/비활성/role mismatch/required-count mismatch와 unexpected active required Boss/Forge/CoreResource source는 오류다.

## `SiteOriginCandidate` Contract

immutable properties:

```text
SiteReservationKind Kind
string SourceDefinitionId
int RequiredInstanceOrdinal
SectorCoord Origin
int OriginIndex
int EdgeRing
int CandidateOrdinal
```

- SourceDefinitionId는 canonical ID다. Start는 `worldProfile.WorldProfileId`, 다른 kind는 special map ID다.
- 이 Task의 fixed sources는 모두 required instance ordinal `0`이다. generic model은 `>=0`을 허용한다.
- Origin은 world grid 안이고 `OriginIndex == WorldGridIndex.ToIndex(Origin)`이다.
- `EdgeRing = min(x, 12-x, y, 12-y)`이며 exact `0..6`이다.
- CandidateOrdinal은 해당 group의 deterministic origin order에서 exact `0..Count-1`이다.
- reservation ID, transform, footprint cells, entry, score, selection flag를 포함하지 않는다.

## `SiteCandidateGroup` Contract

immutable properties/API:

```text
SiteReservationKind Kind
string SourceDefinitionId
int RequiredInstanceOrdinal
int PlacementPriority
IReadOnlyList<SiteOriginCandidate> Candidates
int Count
SiteOriginCandidate GetCandidate(int candidateOrdinal)
bool TryGetCandidateByOrigin(SectorCoord origin, out SiteOriginCandidate candidate)
```

group의 exact placement priority:

```text
Start = 0
Boss = 10
Forge = 20
CoreResource = 30
Village = not enumerated
```

- candidate는 non-null/non-empty이며 group kind/source/instance identity가 exact 동일하다.
- origin/index는 unique다.
- caller order와 무관하게 OriginIndex 오름차순으로 copied read-only 보관하고 CandidateOrdinal이 그 위치와 exact 일치해야 한다.
- group은 후보를 추첨·shuffle/filter하지 않는다.

## `SiteCandidateCatalog` Contract

immutable properties/API:

```text
ulong Seed
string WorldProfileId
string GenerationProfileId
SiteCandidateGroup StartGroup
IReadOnlyList<SiteCandidateGroup> SiteGroups
IReadOnlyList<SiteCandidateGroup> Groups
int TotalCandidateCount

bool TryGetGroup(SiteReservationKind kind,
    string sourceDefinitionId,
    int requiredInstanceOrdinal,
    out SiteCandidateGroup group)
```

- exact one Start group과 five site groups를 요구한다.
- group key `(Kind, SourceDefinitionId, RequiredInstanceOrdinal)`는 unique다.
- Groups order는 PlacementPriority, SourceDefinitionId ordinal, instance ordinal이다.
- StartGroup은 Groups[0], SiteGroups는 나머지의 copied read-only view다.
- Seed는 `grid.WorldData.Seed`, profile IDs는 exact input identity다.
- collection mutation, lazy public enumeration, mutable dictionary/array 노출이 없다.

## Enumeration Contract

### Start

각 grid coordinate에 대해 아래 exact edge ring을 계산한다.

```text
edgeRing = min(x, SectorColumns - 1 - x,
               y, SectorRows - 1 - y)
```

`StartEdgeRingMin <= edgeRing <= StartEdgeRingMax`인 coordinate만 포함한다. starter `0..1` 결과:

```text
ring 0 = 48
ring 1 = 40
total = 88
duplicate/missing/extra = 0
```

four corners와 both outer rings를 포함하고 inner ring 2 이상은 제외한다. candidate order는 WorldGridIndex `0..168` 중 조건을 만족하는 순서다.

### Boss / Forge / Core Resources

각 exact source definition/group에 P00의 모든 `0..168` origin을 한 번씩 포함한다.

```text
Boss:         1 group x 169
Forge:        1 group x 169
CoreResource: 3 groups x 169
```

중요: 이것은 **raw origin enumeration**이다.

- 2×1 Boss도 `(12,12)`를 포함한 all 169 origins를 가진다.
- footprint width/height, transform, sparse cells, entry side를 적용해 경계 후보를 제거하지 않는다.
- altitude, edge preference, distance, Core capacity, occupied state로 후보를 제거하지 않는다.
- 이 boundary/transform/overlap 검사는 MAP03_03 `FootprintPlacementSolver` 책임이다.

### Exact catalog

group order:

```text
0 START / WORLD_MOONPALACE_V1 / 0
1 BOSS / SITE_MOON_BOSS_VAULT / 0
2 FORGE / SITE_MOON_SEAL_FORGE / 0
3 CORE_RESOURCE / SITE_CASSIA_SAP_HEART / 0
4 CORE_RESOURCE / SITE_DEEP_STAR_YEAST / 0
5 CORE_RESOURCE / SITE_MOON_CORE_METEOR / 0
```

counts:

```text
Start = 88
Sites = 845
Total = 933
Village = 0
```

## Error / Result Contract

`SiteCandidateEnumerationErrorCode` minimum:

```text
MissingGrid
InvalidGrid
MissingWorldProfile
MissingGenerationProfile
InactiveProfile
ProfileWorldMismatch
InvalidWorldDimensions
InvalidStartRing
MissingSpecialMapInput
NullSpecialMap
DuplicateSpecialMapId
MissingRequiredSite
UnexpectedRequiredSite
SiteRoleMismatch
InvalidRequiredCount
InvalidSiteDefinition
```

`SiteCandidateEnumerationError`는 exact code, canonical-or-empty source ID, non-empty stable message를 보존한다. path, stack, timestamp, thread, culture-sensitive exception message는 identity에 포함하지 않는다.

errors는 source ID ordinal, error code ordinal, message ordinal로 정렬한다. 가능한 독립 input 오류를 누적한다. expected validation failure는 exception을 던지지 않고 failure result로 반환한다.

`SiteCandidateEnumerationResult`:

```text
bool Succeeded
SiteCandidateCatalog Catalog
IReadOnlyList<SiteCandidateEnumerationError> Errors
```

- success: non-null Catalog, errors `0`
- failure: null Catalog, errors `>=1`
- partial group/catalog publish 금지

## Determinism / Ownership

- specialMaps 입력 순서와 collection implementation이 달라도 exact same groups/candidates/order다.
- world seed `0`, `4660`, `ulong.MaxValue`를 보존하며 seed는 후보 membership/order를 바꾸지 않는다.
- current culture `en-US`/`tr-TR`, wall clock, frame, thread, filesystem에 무관하다.
- `RNG_WORLD_SITE`, other RNG stream, `System.Random`, `UnityEngine.Random`을 생성·draw·참조하지 않는다.
- source grid/profile/definition object와 caller collection은 clone 또는 mutate하지 않는다. candidate membership을 위한 active/role selection만 수행하고 필요한 scalar identity를 immutable output으로 복사한다.
- reused/fresh enumerator 100회 결과가 exact 동일하다.

## Scope Boundary / DO NOT

- `SiteFootprintTransform` variant enumeration/application 금지
- footprint cell/entry socket 변환·world-bound placement 금지
- collision/occupancy solver와 reserved-sector 생성 금지
- distance index/constraint, candidate cost/weight/altitude penalty 금지
- RNG draw/shuffle/weighted choice 금지
- reservation ID 생성, candidate 선택, backtracking/retry 금지
- Core capacity flood-fill, `CoreBiomeSeed` 생성 금지
- village distance bucket/layout candidate 생성 금지
- `PASS_SITE`, pass adapter, Root registry integration 금지
- generated_special_sites/sector serializer, file I/O, replay bundle 확장 금지
- existing MAP03_01 models, grid/root/manifest/overlay 수정 금지
- Authoring/generated CSV/meta, asmdef/asmref, Scene/Prefab, Package/ProjectSettings 변경 금지
- test skip/ignore/assertion 완화, Git operation 금지
- MAP03_03 선행 작업 금지

## Collision Handling

1. 신규 destination이 없으면 생성한다.
2. exact 계약과 바이트 동일한 preexisting destination만 `PREEXISTING_IDENTICAL`로 재사용한다.
3. 다르면 overwrite/merge/delete하지 않고 `STATUS: BLOCKED`다.
4. 기존 `.meta` GUID와 사용자 변경을 보존한다.

## Required Tests

`SiteCandidateEnumerationTests.cs` actual NUnit cases 최소 `72`개다.

minimum groups:

- candidate/group/catalog constructor null/range/identity/duplicate/order/read-only rejection
- exact edge-ring formula over all 169 coordinates
- ring distribution `48/40/32/24/16/8/1` for rings `0..6`
- starter Start membership exact 88, four corners included, inner ring excluded
- each exact five site groups all index/coordinate `0..168`, missing/extra/duplicate `0`
- Boss raw origin `(12,12)` retained, proving no placement filtering
- exact groups/order/counts `6 / 88 / 845 / 933`, Village `0`
- exact required source ID/role/profile/world validation success
- missing/inactive/duplicate/null/unexpected/wrong-role/wrong-count/invalid-definition errors and no partial catalog
- invalid/mismatched profile/world/ring/grid errors
- input order reversal/shuffle and array/list variation stability
- seeds `0/4660/ulong.MaxValue`, fresh/reused 100-run identity
- `en-US`/`tr-TR` culture invariance and source collection mutation isolation
- public mutation-surface/dependency audit
- RNG draw/dependency, transform/placement/distance/cost/backtracking/village/pass/file-I/O production `0`

금지:

- `[Ignore]`, `[Explicit]`, inconclusive, assumption-based skip
- reflection으로 private state를 바꿔 success를 만드는 test
- test order/current filesystem/wall clock 의존
- boundary raw candidates를 현재 footprint 크기에 맞춰 제거

## Regression / Verification

```text
New SiteCandidateEnumerationTests: >=72 PASS
MAP03_01 SiteReservationModelsTests: 81/81 PASS
MAP02 phase focused aggregate: 667/667 PASS
SpecialVillageDefinitionBuilderTests: 57/57 PASS
BiomeBoundaryDefinitionBuilderTests: 38/38 PASS
StaticDataRegistryBuilderTests: 53/53 PASS
ContentVersionHash: 54/54 PASS
Previous Game.Map targeted baseline: 1595/1595 PASS
Game.Map targeted total: >=1667 PASS
Previous full project EditMode baseline: 1635/1635 PASS
Full project EditMode total: >=1707 PASS
failed = 0 / skipped = 0
Unity 6000.3.8f1 / forced refresh / compile error 0 / relevant warning 0
PlayMode NOT RUN / Visual NOT APPLICABLE / saved Scene-Prefab changes NONE
```

Unity compile/test 증거가 없으면 `BLOCKED`다.

## Asset / Meta / Change Gate

clean baseline:

```text
Authoring CSV/meta = 50/50
Assets meta = 2998
accepted legacy Editor folder meta = 6/6
duplicate GUID groups = 0
```

완료 시:

```text
new Runtime production C# = 6
new Runtime test C# = 1
new matching cs.meta = 7
final Assets meta = 3005
task-marker 이후 exact Assets changes = 14
existing Assets modifications = 0
unexpected Assets changes = 0
new directory/folder meta = 0
```

Authoring CSV/meta `50/50`과 accepted legacy folder meta `6/6`의 bytes/hash를 보존한다. 신규 meta는 `fileFormatVersion: 2`, unique lowercase 32-hex GUID이며 project duplicate GUID `0`이어야 한다.

## Failure Policy

- input/model/count/determinism/test/compile/meta/change-scope 한 조건이라도 불일치하면 `STATUS: FAIL`이다.
- Unity/Test Runner 접근이 없어 실제 compile/regression을 수행할 수 없으면 `STATUS: BLOCKED`다.
- FAIL/BLOCKED를 existing production 수정, assertion 완화, placement/solver 선행 구현으로 해결하지 않는다.
- PASS가 아니면 STATUS FINALIZE를 수행하지 않고 MAP03_03을 열지 않는다.

## Result / Completion

Result: `REPORTS/MAP03_02_ENUMERATE_START_AND_SITE_CANDIDATES_RESULT.md`.

필수 섹션:

```text
TASK
STATUS
SUMMARY
PATCH APPLY
READ
MASTER BACKLOG CHECK
MAP03_01 GATE CHECK
CREATED
MODIFIED
PREEXISTING_IDENTICAL
INPUT GATE
SITE ORIGIN CANDIDATE
CANDIDATE GROUP
CANDIDATE CATALOG
START ENUMERATION
SPECIAL SITE ENUMERATION
ERROR AND RESULT CONTRACT
DETERMINISM AND OWNERSHIP
TEST
UNITY
ASSET META VALIDATION
CHANGE SCOPE
PRODUCTION OWNERSHIP AUDIT
OUT_OF_SCOPE_FINDINGS
DONE CONDITIONS
NEXT
Recommended Commit
```

PASS Result에는 exact six group identity/order, ring counts, five `169/169`, total `933`, raw boundary retention, focused/targeted/full counts, final meta/GUID, exact no-later-work evidence를 기록한다.

모든 조건 PASS 시에만 MAP03_02 COMPLETE, Current Task NONE으로 finalize한다. `MAP03_03_IMPLEMENT_FOOTPRINT_PLACEMENT_SOLVER`는 LOCKED로 유지하고 자동 시작하지 않는다.

Recommended Commit: `feat(map): enumerate deterministic site origins`
