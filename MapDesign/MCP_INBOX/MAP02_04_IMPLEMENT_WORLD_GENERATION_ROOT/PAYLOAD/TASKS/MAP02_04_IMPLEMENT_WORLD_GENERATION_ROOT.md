# MAP02_04 — Implement World Generation Root

```yaml
status_control:
  task_key: MAP02_04_IMPLEMENT_WORLD_GENERATION_ROOT
  result_file: REPORTS/MAP02_04_IMPLEMENT_WORLD_GENERATION_ROOT_RESULT.md
```

## Objective

immutable `StaticDataRegistry`의 `GenerationProfileDefinition`과 `GenerationPassDefinition`을 source of truth로 사용해 enabled pass를 deterministic order로 계획하고 실행하는 `WorldGenerationRoot`를 구현한다. Root는 input/output artifact dependency, exact pass implementation/class binding, RNG facade 전달, failure policy와 retry ordinal, transactional output commit을 소유한다.

현재 production pass는 `PASS_GRID` 하나만 구현돼 있다. 따라서 actual grid prefix는 실행 가능하게 하고, 아직 구현되지 않은 후속 pass를 임의 stub/skip하지 않는다. 전체 profile 실행 요청은 missing implementation을 pass 실행 전에 deterministic failure로 반환해야 한다. focused test의 명시적 fake pass만으로 full 10-pass orchestration을 검증한다.

## Mandatory Read / Scope

entrypoint → locked/work/CSV/Unity/change/patch/finalize rules → Master → Status → 이 Task → MAP02_03 PASS Result 순서로 읽는다.

Map Package v1.0 exact path가 installed tree에 있으면 아래 부분만 읽는다.

```text
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md  # pass ownership/retry boundary
01_FIXED_SPEC/04_RUNTIME_ARCHITECTURE.md                # WorldGenerationRoot responsibility
02_PHASE_ROADMAP/MAP02_TOPOLOGY_GRAYBOX.md              # pass order/RNG/root only
03_CSV_SCHEMA/CSV_DATA_DICTIONARY.csv                   # generation_profiles/passes rows only
04_CSV_STARTER/generation_profiles.csv                  # exact active profile row
04_CSV_STARTER/generation_passes.csv                    # exact 10 rows
```

exact 문서가 installed tree에 없으면 이 Task에 동결된 starter plan과 contract를 authoritative fallback으로 사용한다. 대체 문서를 broad search하거나 Legacy/다른 generator를 읽지 않는다.

기존 public API 확인은 아래 exact files로 제한한다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/Data/WorldGenerationDefinitions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/WorldRouteDefinitionSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/StaticDataRegistry.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/RngResetScope.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/RngStreamScope.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/DeterministicRngStreamFactory.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationRngStreams.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GridInitializationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GridInitializationPass.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/WorldRouteDefinitionBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/StaticDataRegistryBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/DeterministicRngStreamTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/GridInitializationPassTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

approved Runtime/test `Generation` 폴더의 `rg --files` path-only inventory는 허용한다. content search는 위 exact files로만 한정하고 다른 file match body를 출력하는 broad recursive search를 금지한다. MAP02_05 이후 Task body, Legacy/Stage/P6/P11 generator, unrelated CSV rows, Scene/Prefab YAML은 읽거나 사용하지 않는다.

## WRITE ALLOWLIST

Runtime C# 7:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationFailurePolicy.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationArtifactStore.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationPassContracts.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationPassRegistry.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GridInitializationPassAdapter.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationRootResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationRoot.cs
```

EditMode test C# 1:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/WorldGenerationRootTests.cs
```

신규 C# 8 + matching `.cs.meta` 8 + Result 1만 허용한다. 모두 existing approved `Generation` folders에 두며 새 directory/folder meta를 만들지 않는다. Runtime namespace `StarNight.Map.WorldGeneration.Generation`, existing `Game.Map.Runtime` / `Game.Map.Tests.EditMode` assembly를 재사용한다.

existing MAP00/01/MAP02_01~03 C#/tests/meta, accepted legacy Editor folder meta 6개, Authoring CSV/meta, asmdef, Scene/Prefab/Package/ProjectSettings 수정 금지. `UnityEditor` reference와 신규 asmdef/asmref를 만들지 않는다.

## Frozen Starter Execution Plan

`GEN_MOONPALACE_V1`은 active profile이며 exact enabled rows는 아래 10개다. CSV row order가 아니라 `PassOrder`, then `PassId` ordinal로 정렬한다.

| Order | Pass ID | Class | RNG | Inputs | Outputs | Policy | Max retries |
|---:|---|---|---|---|---|---|---:|
| 0 | `PASS_GRID` | `GridInitializationPass` | empty | empty | `GRID` | `FAIL_WORLD` | 1 |
| 10 | `PASS_SITE` | `SpecialSiteReservationPass` | `RNG_WORLD_SITE` | `GRID` | `SITE_RESERVATIONS` | `RETRY_PASS` | 200 |
| 20 | `PASS_BIOME` | `BiomePatchPass` | `RNG_BIOME_PATCH` | `GRID\|SITE_RESERVATIONS` | `BIOME_PATCHES` | `RETRY_PASS` | 100 |
| 30 | `PASS_ROUTE` | `MandatoryRoutePass` | `RNG_ROUTE` | `SITE_RESERVATIONS\|BIOME_PATCHES` | `ROUTE123` | `RETRY_PASS` | 200 |
| 40 | `PASS_TYPE0` | `OptionalRegionPass` | `RNG_TYPE0` | `ROUTE123` | `TYPE0_REGIONS` | `RETRY_PASS` | 100 |
| 50 | `PASS_SECTOR_RECIPE` | `SectorRecipePass` | `RNG_SECTOR_RECIPE` | `BIOME_PATCHES\|ROUTE123\|TYPE0_REGIONS` | `SECTOR_RECIPES` | `RETRY_SCOPE` | 20 |
| 60 | `PASS_MICRO_SOLVE` | `SectorConstraintPass` | `RNG_SECTOR_RECIPE` | `SECTOR_RECIPES` | `MICROCHUNKS` | `RETRY_SCOPE` | 20 |
| 70 | `PASS_BAKE` | `TilemapBakePass` | empty | `MICROCHUNKS` | `BAKED_TILES` | `FAIL_WORLD` | 1 |
| 80 | `PASS_POPULATION` | `PopulationPass` | `RNG_POPULATION` | `BAKED_TILES` | `SPAWNS` | `RETRY_SCOPE` | 10 |
| 90 | `PASS_VALIDATION` | `WorldValidationPass` | empty | `SPAWNS` | `VALIDATION` | `FAIL_WORLD` | 1 |

Root는 이 표를 production lookup table로 hard-code하지 않는다. 표는 imported typed definitions의 exact verification vector다. generic profile/pass definitions을 실행하며 ID/token/class/artifact string을 trim, case-fold, alias, reflection fallback으로 바꾸지 않는다.

## Failure Policy Contract

`WorldGenerationFailurePolicy` exact values/tokens:

```text
FailWorld  <-> FAIL_WORLD
RetryPass  <-> RETRY_PASS
RetryScope <-> RETRY_SCOPE
ReportOnly <-> REPORT_ONLY
```

parse/format은 ordinal exact다. null/empty/space/case/numeric/unknown/undefined를 거부한다.

`MaxRetryCount`는 initial attempt 이후 허용되는 추가 retry 수다.

```text
initial attempt ordinal = 0
maximum attempt ordinal = MaxRetryCount
maximum total attempts  = MaxRetryCount + 1
```

negative retry count와 integer overflow 가능 값은 plan validation에서 거부한다. `FAIL_WORLD`/`REPORT_ONLY`는 retry count 값과 관계없이 retry하지 않는다.

## Immutable Artifact Store

`WorldGenerationArtifactStore`는 artifact ID → non-null immutable artifact object의 ordinal read-only snapshot이다.

Public behavior:

```text
int Count
IReadOnlyList<string> ArtifactIds
bool Contains(string artifactId)
object Get(string artifactId)
T Get<T>(string artifactId)
bool TryGet(string artifactId, out object artifact)
bool TryGet<T>(string artifactId, out T artifact)
```

- empty initial store를 지원한다.
- ID는 exact non-empty string이며 trim/case normalization하지 않는다.
- caller dictionary/order를 복사하고 `StringComparer.Ordinal` / ID ordinal enumeration을 사용한다.
- null value, duplicate ID, wrong typed `Get`, missing `Get`을 거부한다.
- public add/remove/set/overwrite API를 노출하지 않는다.
- pass success output은 Root만 새 immutable snapshot으로 transactional commit한다.
- already-owned artifact ID를 overwrite하지 않는다. P00/P01… pass ownership을 보존한다.
- artifact object 자체는 immutable Runtime contract여야 하며 Root가 clone/mutate하지 않는다.

## Pass Contracts

`IWorldGenerationPass`:

```text
string PassId { get; }
string ClassName { get; }
WorldGenerationPassResult Execute(WorldGenerationPassContext context)
```

`WorldGenerationPassContext` immutable properties:

```text
ulong WorldSeed
StaticDataRegistry StaticData
GenerationProfileDefinition GenerationProfile
GenerationPassDefinition PassDefinition
WorldGenerationArtifactStore Inputs
WorldGenerationRngStreams RngStreams
int AttemptOrdinal
string RetryScopeId
```

- `Inputs`에는 definition이 선언한 input artifact exact subset만 들어간다. pass가 다른 이전 artifact에 암묵적으로 접근하지 못한다.
- first attempt `AttemptOrdinal=0`, `RetryScopeId=empty`다.
- `RETRY_PASS` retry scope는 계속 empty다.
- `RETRY_SCOPE` 후속 attempt는 직전 failure가 반환한 exact non-empty stable scope ID를 받는다.
- context/registry/definitions/artifacts를 pass가 수정하지 못한다.
- Root는 execution마다 existing `WorldGenerationRngStreams` facade를 fresh 구성해 전달하지만 RNG draw는 하지 않는다. pass는 definition의 exact `RngStreamId`와 reset/scope 계약을 따른다.

`WorldGenerationPassResult`는 immutable result-based contract다.

Success:

```text
Succeeded = true
Outputs = exact non-null read-only artifact map
FailureCode/Message/RetryScopeId = empty
```

Failure:

```text
Succeeded = false
Outputs = empty
FailureCode = exact non-empty stable ID
FailureMessage = non-null exact diagnostic
RetryScopeId = empty except RETRY_SCOPE request
```

factory methods는 input collections를 copy한다. success/failure 혼합, null result, partial failure outputs를 허용하지 않는다. expected failure는 exception이 아니라 failure result로 반환한다.

## Pass Registry / Grid Adapter

`WorldGenerationPassRegistry`는 `IWorldGenerationPass.PassId` ordinal immutable map이다.

- null pass, empty ID/class, duplicate ID를 거부한다.
- definition `PassId`와 runtime pass `PassId`, definition `ClassName`과 runtime `ClassName`은 exact ordinal match여야 한다.
- reflection, assembly scan, `Activator`, class-name fuzzy lookup, service locator, singleton을 사용하지 않는다.
- definitions에 없는 extra registered implementation은 보관 가능하지만 실행 계획에는 자동 삽입하지 않는다.

`GridInitializationPassAdapter`는 existing `GridInitializationPass`를 수정하지 않고 root contract에 연결한다.

```text
PassId    = PASS_GRID
ClassName = GridInitializationPass
Inputs    = empty
Outputs   = GRID -> GridInitializationResult
```

adapter는 context seed로 existing pass를 exact 1회 호출하고 RNG draw/file I/O/log/time을 추가하지 않는다.

## Root Construction / Plan Validation

`WorldGenerationRoot` public surface:

```text
WorldGenerationRoot(StaticDataRegistry staticData, WorldGenerationPassRegistry passRegistry)
WorldGenerationRootResult Execute(string generationProfileId, ulong worldSeed)
WorldGenerationRootResult ExecuteThrough(string generationProfileId, ulong worldSeed, string inclusivePassId)
```

constructor는 non-null immutable inputs만 보존한다. `Execute`는 selected profile의 모든 enabled pass, `ExecuteThrough`는 selected enabled pass를 포함한 prefix만 계획한다.

pass invocation 전에 전체 requested plan을 검증한다.

1. exact generation profile exists and is active
2. referenced world profile exists and is active
3. profile pass rows exist; enabled rows only are selected
4. deterministic `PassOrder`, then `PassId` ordinal order
5. duplicate pass ID/order, invalid class/policy/retry rejected
6. input/output list item non-empty; duplicates and same-pass input/output overlap rejected
7. each input artifact has exactly one earlier enabled producer
8. output ownership collision/overwrite rejected
9. non-empty RNG ID resolves to active existing definition; empty stays empty
10. every requested pass has exact registered implementation/class match
11. `ExecuteThrough` target exists, is enabled, and belongs to selected profile

plan error가 하나라도 있으면 pass invocation `0`, artifacts empty, deterministic unsuccessful result다. 가능한 안전한 plan errors는 pass order/field order로 누적한다.

현재 production registry에 grid adapter만 등록한 경우:

- `ExecuteThrough("GEN_MOONPALACE_V1", seed, "PASS_GRID")`는 실제 grid prefix를 PASS한다.
- full `Execute("GEN_MOONPALACE_V1", seed)`는 후속 implementation missing을 실행 전에 보고한다.
- missing pass를 skip, no-op, placeholder artifact, reflection stub로 위조하지 않는다.

focused tests에서만 explicit fake implementations 9개를 등록해 full 10-pass orchestration을 검증한다. fake는 production assembly에 만들지 않는다.

## Execution / Retry / Transaction Contract

각 enabled pass에 대해:

1. 현재 committed store에서 declared input-only snapshot 생성
2. attempt context 생성
3. pass exact 1회 호출
4. success output ID set이 declared output ID set과 exact 일치하는지 확인
5. null/missing/unexpected/duplicate output, ownership collision이면 terminal root failure
6. success일 때만 모든 output을 한 번에 commit
7. failure output은 commit하지 않고 input snapshot도 바꾸지 않음

Policy behavior:

- `FAIL_WORLD`: first failure에서 즉시 terminal failure.
- `RETRY_PASS`: same pass/input snapshot, empty retry scope, ordinal을 1씩 증가해 max까지 retry.
- `RETRY_SCOPE`: failure의 non-empty scope를 다음 context에 전달해 same pass/input snapshot을 max까지 retry. missing scope면 즉시 terminal failure.
- `REPORT_ONLY`: issue를 보존하고 output commit 없이 다음 pass로 진행. 후속 declared input이 실제로 없으면 그 pass 호출 전에 deterministic `MissingInputArtifact` terminal failure.
- retry exhaustion: 마지막 failure를 보존하고 terminal `RetryExhausted`.

pass가 throw하거나 null result를 반환하면 Root는 stack/path/time을 기록하지 않고 stable issue code와 exception type만 보존해 terminal failure로 종료한다. exception을 retry하거나 성공으로 삼지 않는다.

## Root Result Contract

`WorldGenerationRootResult`와 `WorldGenerationRootIssue`는 immutable이다.

Result:

```text
bool Succeeded
WorldGenerationArtifactStore Artifacts
IReadOnlyList<WorldGenerationRootIssue> Issues
string LastCompletedPassId
```

Issue minimum fields:

```text
string PassId
string Code
string Message
int AttemptOrdinal
string RetryScopeId
bool Terminal
```

- success는 requested plan 전체 완료, terminal issue 0이다.
- unsuccessful result는 terminal issue exact 1이며 이전 `REPORT_ONLY` issues를 앞에 보존할 수 있다.
- plan failure는 LastCompletedPassId empty, artifacts empty, invocation 0이다.
- runtime failure는 이전 successful pass artifacts를 immutable partial snapshot으로 보존한다.
- issue order는 plan/pass/attempt execution order이며 timestamp, duration, GUID, thread, stack, machine path를 포함하지 않는다.
- pass별 시작 시각·소요 시간·완전한 attempt record는 MAP02_05 책임이므로 선행 구현하지 않는다.

Minimum stable issue categories:

```text
MISSING_PROFILE, INACTIVE_PROFILE, MISSING_WORLD_PROFILE, INACTIVE_WORLD_PROFILE
INVALID_PASS_DEFINITION, INVALID_ARTIFACT_PLAN, MISSING_PASS_IMPLEMENTATION
PASS_CLASS_MISMATCH, INVALID_RNG_DEFINITION, UNKNOWN_THROUGH_PASS
MISSING_INPUT_ARTIFACT, NULL_PASS_RESULT, UNHANDLED_PASS_EXCEPTION
OUTPUT_SET_MISMATCH, ARTIFACT_OWNERSHIP_CONFLICT, MISSING_RETRY_SCOPE
PASS_FAILED, RETRY_EXHAUSTED
```

## Determinism / State Isolation

- source CSV/definition collection input order가 달라도 selected plan order는 같다.
- same profile/seed/pass registry로 100회 grid prefix 실행 시 `GRID` topology와 existing CSV bytes/hash가 동일하다.
- fresh/reused Root 호출 간 artifact/issue/pass state가 누출되지 않는다.
- failed attempt의 outputs와 RNG instance/state는 다음 attempt/다른 pass로 공유되지 않는다.
- Root는 Registry/definition/pass registry/artifact values를 clone, filter, mutate하지 않는다.
- locale, wall clock, frame, thread, Unity object lifecycle에 의존하지 않는다.

## Baseline / Meta Stability

MAP02_03 PASS 이후 baseline:

```text
GridInitializationPass focused: 90/90
Targeted EditMode: 1116/1116
Full EditMode: 1136/1136
Authoring CSV/meta: 50/50
Assets meta: 2959
accepted legacy Editor folder meta: 6/6
duplicate GUID groups: 0
```

legacy folder meta 6개는 정상 baseline이며 삭제·재작성·신규 drift로 분류하지 않는다. 새 directory/folder meta expected `0`. clean path final Assets meta는 matching meta 8개가 추가된 `2967`이다.

## DO NOT

- existing Registry/definition/RNG/grid production 또는 tests 수정 금지
- 후속 site/biome/route/type0/recipe/micro/bake/population/validation pass production stub 구현 금지
- missing implementation skip/no-op/placeholder success 금지
- common pass discovery reflection/assembly scan/service locator/singleton 금지
- pass output in-place mutation, artifact overwrite, failed partial output commit 금지
- timing/duration/execution record/seed manifest/replay/file I/O 구현 금지
- EditorWindow/overlay/Scene·Game integration 금지
- exception swallow, nondeterministic issue data, test skip/ignore/assertion 완화 금지
- new directory/folder meta/asmdef/asmref, CSV/meta/Scene/Prefab/Package/ProjectSettings/Git 변경 금지

## Tests / Verification

Focused minimum 64 cases:

- exact four failure-policy token roundtrip and invalid token rejection
- retry max/attempt ordinal 0..N/additional retry semantics and overflow gate
- artifact empty/copy/order/typed lookup/invalid/null/duplicate/immutability/overwrite rejection
- pass result success/failure exclusivity, copied outputs, stable fields
- context exact input subset, attempt/scope/static-data/RNG facade preservation
- registry null/empty/duplicate/exact pass/class resolution, extra registration isolation
- exact starter 10-row plan order/RNG/input/output/policy/retry verification
- inactive/unknown profile/world/pass, disabled pass, duplicate order/ID, invalid policy/retry
- missing/duplicate/cyclic-or-forward artifact dependency and output ownership collision
- unknown through target, missing implementation and class mismatch pre-invocation failure
- actual `PASS_GRID` adapter prefix success, exact `GRID` type/topology/seed
- production grid-only full execution missing implementations with invocation 0
- fake 10-pass success exact order and artifact chain
- `FAIL_WORLD`, `RETRY_PASS`, `RETRY_SCOPE`, `REPORT_ONLY` exact behavior
- same input snapshot on retry, failed output non-commit, success atomic output commit
- missing retry scope, retry exhaustion, null result, thrown exception, output set mismatch
- same seed 100 grid prefix, fresh/reused Root isolation, culture invariance
- existing MAP02_01 `56/56`, MAP02_02 `103/103`, MAP02_03 `90/90` regressions
- accepted meta 6 unchanged, new directory 0, existing file modification 0

```text
New WorldGenerationRoot: >=64 PASS
MAP02_01 GeneratedWorldData: 56/56 PASS
MAP02_02 deterministic RNG streams: 103/103 PASS
MAP02_03 GridInitializationPass: 90/90 PASS
MAP00 coordinate/architecture regression: PASS
MAP01 Registry/content/import regression: PASS
Previous targeted baseline: 1116/1116 PASS
Targeted total: >=1180 PASS
Full project EditMode: >=1200 PASS
Unity 6000.3.8f1 / force refresh / compile error 0 / relevant warning 0
PlayMode NOT RUN / Visual NOT APPLICABLE / Scene-Prefab changes NONE
```

Authoring CSV/meta `50/50` unchanged, accepted folder meta `6/6` unchanged, 신규 matching meta `8/8` valid, final Assets meta `2967`, duplicate GUID groups `0`, task marker 이후 Assets 변경 exact allowlisted C# 8 + meta 8 = `16`, unexpected `0`을 확인한다. 하나라도 실패하면 `BLOCKED`.

## Result / Completion

Result: `REPORTS/MAP02_04_IMPLEMENT_WORLD_GENERATION_ROOT_RESULT.md`.

Required sections: TASK, STATUS, SUMMARY, READ, MASTER BACKLOG CHECK, MAP02_03 GATE CHECK, CREATED, PREEXISTING_IDENTICAL, STARTER PLAN, FAILURE POLICY, ARTIFACT STORE, PASS CONTRACTS, PASS REGISTRY, GRID ADAPTER, ROOT PLAN, EXECUTION AND RETRY, ROOT RESULT, DETERMINISM, TEST, UNITY, ASSET META VALIDATION, CHANGE SCOPE, OUT_OF_SCOPE_FINDINGS, DONE CONDITIONS, NEXT, Recommended Commit.

모든 계약과 회귀가 PASS일 때만 MAP02_04 COMPLETE, Current Task NONE으로 finalize한다. `MAP02_05_IMPLEMENT_PASS_EXECUTION_RECORDS`는 LOCKED로 유지하고 자동 시작하지 마.

Recommended Commit: `feat(map): orchestrate world generation passes`
