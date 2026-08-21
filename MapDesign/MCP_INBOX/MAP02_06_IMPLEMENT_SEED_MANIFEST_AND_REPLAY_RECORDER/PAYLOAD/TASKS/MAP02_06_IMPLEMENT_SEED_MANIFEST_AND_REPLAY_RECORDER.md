# MAP02_06 — Implement Seed Manifest and Replay Recorder

```yaml
status_control:
  task_key: MAP02_06_IMPLEMENT_SEED_MANIFEST_AND_REPLAY_RECORDER
  result_file: REPORTS/MAP02_06_IMPLEMENT_SEED_MANIFEST_AND_REPLAY_RECORDER_RESULT.md
```

## Objective

성공한 exact `PASS_GRID` recorded execution을 재실행 없이 immutable replay bundle로 기록한다. bundle은 Map Package v1.0의 exact `seed_manifest.csv`와 existing serializer가 만든 exact `generated_world_sectors.csv` 두 파일만 보유한다. 이를 caller가 지정한 generated-output root 아래에 원자적으로 publish/load하고, manifest identity를 검증한 뒤 `WorldGenerationRoot.ExecuteThroughRecorded(..., "PASS_GRID")`를 exact 1회 호출해 정적 sector CSV bytes가 같은지 확인한다.

`generation_started_utc`와 `generation_duration_ms`는 실행 진단값이므로 실행마다 달라질 수 있다. seed replay 결정론의 비교 대상은 profile/seed/content hash/build identity와 generated static sector bytes다. recorder/player/publisher는 Authoring CSV, Registry, Root, RNG, artifact를 변경하지 않는다.

## Mandatory Read / Scope

entrypoint → locked/work/CSV/Unity/change/patch/finalize rules → Master → Status → 이 Task → MAP02_05 PASS Result 순서로 읽는다.

Map Package v1.0 exact path가 installed tree에 있으면 아래 부분만 읽는다.

```text
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md  # generated output ownership only
01_FIXED_SPEC/04_RUNTIME_ARCHITECTURE.md                # replay identity/recorder only
02_PHASE_ROADMAP/MAP02_TOPOLOGY_GRAYBOX.md              # blank grid + manifest replay only
03_CSV_SCHEMA/CSV_DATA_DICTIONARY.csv                   # seed_manifest rows only
05_GENERATED_OUTPUT_SCHEMA/README.md                    # generated directory/file identity only
05_GENERATED_OUTPUT_SCHEMA/seed_manifest.csv            # exact header template only
05_GENERATED_OUTPUT_SCHEMA/generated_world_sectors.csv  # exact header template only
```

exact 문서가 installed tree에 없으면 이 Task의 frozen contract를 authoritative fallback으로 사용한다. 대체 문서를 broad search하거나 Legacy/다른 generator를 읽지 않는다.

기존 public API 확인은 아래 exact files로 제한한다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/Data/ContentVersionHash.cs
Assets/_Game/Map/Runtime/WorldGeneration/Data/Rfc4180CsvReader.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldData.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldDataCsvSerializer.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GridInitializationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GridInitializationPass.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationArtifactStore.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationExecutionRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationExecutionResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationRootResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationRoot.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/ContentVersionHashCalculatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/GeneratedWorldDataTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/GridInitializationPassTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/WorldGenerationRootTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/WorldGenerationExecutionRecordTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

approved Runtime/test `Generation` 폴더의 `rg --files` path-only inventory는 허용한다. content search는 위 exact files로만 한정하고 다른 file match body를 출력하는 broad recursive search를 금지한다. MAP02_07 이후 Task body, Legacy/Stage/P6/P11 generator, unrelated dictionary/generated rows, Scene/Prefab YAML은 읽거나 사용하지 않는다.

## WRITE ALLOWLIST

Runtime C# 신규 7:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SeedManifest.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SeedManifestCsvSerializer.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SeedReplayBundle.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SeedReplayRecorder.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SeedReplayVerificationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SeedReplayPlayer.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SeedReplayPublisher.cs
```

EditMode test C# 신규 1:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SeedReplayRecorderTests.cs
```

신규 C# 8 + matching `.cs.meta` 8 + Result 1만 허용한다. existing C#/test/meta 수정은 `0`이다. 모두 existing approved `Generation` folders에 두며 새 directory/folder meta를 만들지 않는다. Runtime namespace `StarNight.Map.WorldGeneration.Generation`, existing `Game.Map.Runtime` / `Game.Map.Tests.EditMode` assembly를 재사용한다.

다른 MAP00/01/MAP02 C#/tests/meta, accepted legacy Editor folder meta 6개, Authoring CSV/meta, asmdef, Scene/Prefab/Package/ProjectSettings 수정 금지. `UnityEditor` reference와 신규 asmdef/asmref를 만들지 않는다. 실제 generated output은 focused test의 OS temp directory에만 쓴다. 프로젝트 `Assets`, `MapDesign`, repository root에는 generated/replay 파일을 쓰지 않는다.

## Frozen Generated Output Boundary

P00 grid checkpoint bundle의 exact file set과 ordinal order는 아래뿐이다.

```text
seed_manifest.csv
generated_world_sectors.csv
```

target relative directory는 exact 아래 형식이다.

```text
GeneratedWorlds/{world_profile_id}/{seed invariant D16}
```

- `D16`은 최소 16자리 zero padding이다. 16자리보다 긴 `ulong`은 truncate하지 않는다.
- `world_profile_id`는 manifest exact 값이며 하나의 safe path segment여야 한다. empty, `.`, `..`, slash/backslash, rooted path, traversal, control/invalid filename character를 거부한다.
- final file/directory identity에 timestamp, duration, GUID, random suffix, locale, machine path를 넣지 않는다.
- `generated_world_edges.csv`, biome patch, site, validation, spawn 등 후속 schema는 P00에서 아직 유효한 row를 만들 수 없으므로 placeholder/header-only 파일도 만들지 않는다.
- Authoring CSV tree와 generated output tree를 혼합하지 않는다.

## `SeedManifest` Contract

`SeedManifest`는 sealed immutable object이며 exact 11개 schema field를 보존한다.

```text
string WorldProfileId
ulong Seed
string ContentVersionHash
string GenerationProfileId
string GeneratorBuildId
bool Approved
DateTimeOffset GenerationStartedUtc
int GenerationDurationMilliseconds
int RetryCountTotal
IReadOnlyList<string> FailureRuleIds
string Notes
```

- string은 모두 non-null이며 임의 trim/case-fold/Unicode normalization을 하지 않는다.
- `ContentVersionHash`는 exact lowercase 64 hex다. recorder는 existing `ContentVersionHash` public API의 canonical hex만 사용한다.
- world/generation profile ID와 generator build ID는 non-empty다.
- UTC는 offset exact zero, duration/retry는 non-negative `Int32` 범위다.
- failure rule ID는 non-empty stable ID이며 list delimiter `|`를 포함할 수 없다. caller list를 copied read-only snapshot으로 보존하고 순서와 duplicate를 임의 변경하지 않는다.
- constructor는 null, invalid hash/UTC/range/state를 거부한다.
- P00 checkpoint의 exact 고정값은 `Approved=false`, empty failure rule list, `Notes="MAP02_GRID_CHECKPOINT_V1"`이다.
- `failure_rule_ids`는 generated-output validation rule ID용이다. Root issue/failure code를 이 열에 대신 기록하지 않는다. 이 Task는 성공한 grid checkpoint만 기록하므로 empty다.

## `seed_manifest.csv` Contract

`SeedManifestCsvSerializer`는 filesystem에 직접 쓰지 않고 copied `byte[]`로 serialize하며 strict bytes를 deserialize한다.

Exact 11-column header/order:

```text
world_profile_id,seed,content_version_hash,generation_profile_id,generator_build_id,approved,generation_started_utc,generation_duration_ms,retry_count_total,failure_rule_ids,notes
```

- encoding exact UTF-8 BOM `EF BB BF`; BOM은 시작에 1회만 존재한다.
- record separator exact CRLF, header 1 + data row exact 1, final CRLF exact 1이다.
- Map Package v1.0 header-only template는 exact `184` bytes / SHA-256 `fb45bfbb905f165b4702515484b97c83232fca9aa7bf775dd46cc52421761b0c`다.
- ulong/int는 invariant decimal, approved는 exact `0`/`1`이다.
- UTC text는 invariant exact `yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'`다.
- failure rule IDs는 original order의 exact `|` join이며 P00은 empty field다.
- 모든 string field는 RFC4180 comma/quote/CR/LF escaping과 doubled quote를 적용한다.
- strict deserialize는 BOM/header/CRLF/record count/field count/typed grammar를 검증하고 trailing/extra record, bare line ending, malformed quote, invalid UTF-8, duplicate BOM을 거부한다.
- existing `Rfc4180CsvReader` public API를 재사용할 수 있지만 수정하지 않는다. strict byte envelope 검증을 생략하지 않는다.
- deserialize→serialize는 accepted input의 exact canonical bytes와 같아야 한다.

## `SeedReplayBundle` Contract

`SeedReplayBundle`은 한 grid checkpoint의 immutable in-memory bundle이다.

```text
SeedManifest Manifest
string RelativeDirectory
byte[] SeedManifestBytes
byte[] GeneratedWorldSectorsBytes
IReadOnlyList<string> FileNames
```

- exact 두 filename과 ordinal order만 허용한다.
- byte arrays와 filename collection을 방어 복사하고 mutable reference를 노출하지 않는다.
- `SeedManifestBytes`를 strict parse한 값이 `Manifest`의 모든 11개 field와 exact 일치해야 한다.
- `RelativeDirectory`가 manifest world ID/seed로 계산한 frozen path와 exact 일치해야 한다.
- generated sectors bytes는 UTF-8 BOM/CRLF/exact 13-column header, 169 rows, row seed exact manifest seed인 existing serializer output이어야 한다. validation은 existing serializer/parser public contract를 재사용하며 파일을 다시 생성해 silently repair하지 않는다.
- extra/missing/duplicate file, null bytes, mismatched identity를 constructor에서 거부한다.

## `SeedReplayRecorder` Contract

Recorder input은 caller가 이미 exact `WorldGenerationRoot.ExecuteThroughRecorded(generationProfileId, seed, "PASS_GRID")`로 얻은 successful `WorldGenerationExecutionResult`, existing non-null `ContentVersionHash`, non-empty generator build ID다.

Recorder는 아래를 전부 확인한다.

1. existing Result와 ExecutionRecord 모두 success
2. `InclusivePassId == "PASS_GRID"`
3. `LastCompletedPassId == "PASS_GRID"`
4. actual pass record exact 1, pass ID `PASS_GRID`, successful attempt exact 1
5. root/pass/attempt world seed exact 동일, retry total `0`
6. artifact store에 exact `GRID -> GridInitializationResult` 존재
7. grid world data seed와 record seed exact 동일
8. grid는 exact 169-cell P00 topology/neutral state이며 existing serializer가 정상 serialize 가능
9. execution duration/retry가 manifest `Int32` 범위

Recorder output mapping:

```text
world_profile_id       = ExecutionRecord.WorldProfileId
seed                   = ExecutionRecord.WorldSeed
content_version_hash   = provided ContentVersionHash canonical lowercase hex
generation_profile_id  = ExecutionRecord.GenerationProfileId
generator_build_id     = provided exact value
approved               = 0
generation_started_utc = ExecutionRecord.StartedUtc
generation_duration_ms = ExecutionRecord.DurationMilliseconds
retry_count_total      = ExecutionRecord.RetryCountTotal
failure_rule_ids       = empty
notes                  = MAP02_GRID_CHECKPOINT_V1
```

`generated_world_sectors.csv`는 artifact의 `GridInitializationResult.WorldData`를 existing `GeneratedWorldDataCsvSerializer`로 exact 1회 serialize한 bytes다. recorder는 Root/pass를 호출하지 않고, record를 얻기 위해 재실행하지 않으며, filesystem에도 쓰지 않는다. invalid/failed/post-grid/partial execution을 bundle로 위조하지 않고 argument/contract error로 거부한다.

## `SeedReplayPublisher` Contract

Publisher만 `System.IO`를 사용한다. public behavior는 caller-supplied absolute/full output root와 bundle을 받아 frozen relative directory에 publish하고, 같은 root/world/seed에서 exact bundle을 load하는 것이다.

- root는 non-empty rooted/full path여야 하며 normalize한 경로 밖으로 target이 벗어나면 거부한다.
- target parent를 만든 뒤 target seed directory의 deterministic sibling 이름 `{seedDir}.staging`과 `{seedDir}.backup`을 사용한다.
- 호출 시작 시 staging 또는 backup이 이미 존재하면 stale state로 실패한다. broad cleanup/delete로 숨기지 않는다.
- 새 staging에 exact 두 파일을 ordinal order로 쓰고 flush/close한 뒤 다시 load하여 bytes/file set/manifest identity를 검증한다.
- destination이 없으면 staging directory를 destination으로 single directory move한다.
- destination이 있으면 destination→backup, staging→destination 순서로 swap하고 성공 후 exact backup만 삭제한다.
- swap 실패 시 현재 호출이 이동한 exact directory만 다뤄 가능한 경우 backup을 original destination으로 복원하고 원 exception을 보존한다. unrelated sibling/parent/root를 삭제하지 않는다.
- successful return에서 destination에는 exact 두 파일만 있고 staging/backup은 없다.
- load는 exact 두 regular files만 허용한다. extra file, subdirectory, link/reparse indirection, missing/duplicate/case-variant filename, manifest/path identity mismatch를 거부한다.
- overwrite는 두 파일의 atomic directory set 단위다. 개별 final file을 in-place overwrite하지 않는다.
- publisher는 Authoring CSV/Registry/Unity AssetDatabase를 읽거나 변경하지 않는다.

focused tests는 `Path.GetTempPath()` 아래 각 test 전용 fresh directory만 사용하고 `try/finally`로 그 exact test directory를 정리한다. repository/Assets/MapDesign 경로를 target으로 사용하지 않는다.

## `SeedReplayVerificationResult` Contract

sealed immutable result의 minimum public fields는 아래다.

```text
bool Succeeded
string Code
string Message
```

success는 code/message exact empty다. failure는 non-empty stable code와 non-null deterministic message다. stack, absolute path, timestamp, duration, GUID, locale-dependent text를 message에 넣지 않는다.

Stable failure code set:

```text
INVALID_BUNDLE
INVALID_MANIFEST
CONTENT_HASH_MISMATCH
GENERATOR_BUILD_MISMATCH
REPLAY_EXECUTION_FAILED
ARTIFACT_MISMATCH
```

factory/constructor는 success/failure state consistency를 강제한다.

## `SeedReplayPlayer` Contract

`SeedReplayPlayer`는 non-null existing `WorldGenerationRoot`를 constructor dependency로 받고 mutable/static global state를 보유하지 않는다. verify input은 bundle, current existing `ContentVersionHash`, current non-empty generator build ID다.

검증 순서와 호출 경계는 exact 아래다.

1. bundle exact file/path/byte envelope 검증; 실패 `INVALID_BUNDLE`
2. seed manifest strict parse 및 P00 fixed fields 검증; 실패 `INVALID_MANIFEST`
3. manifest/current content hash ordinal 비교; 실패 `CONTENT_HASH_MISMATCH`
4. manifest/current generator build ID ordinal 비교; 실패 `GENERATOR_BUILD_MISMATCH`
5. 위 precondition이 모두 PASS한 뒤에만 manifest generation profile/seed로 `ExecuteThroughRecorded(..., "PASS_GRID")` exact 1회 호출
6. replay Result/ExecutionRecord success, world profile/seed/target/last pass, one pass/attempt/retry 0, `GRID` artifact 검증; 실패 `REPLAY_EXECUTION_FAILED`
7. replay grid를 existing serializer로 exact 1회 serialize해 recorded `generated_world_sectors.csv`와 byte-for-byte 비교; 다르면 `ARTIFACT_MISMATCH`, 같으면 success

- content/build mismatch와 invalid bundle/manifest에서는 Root invocation `0`이다.
- player는 recorded diagnostic start/duration과 replay diagnostic start/duration을 같다고 요구하지 않는다.
- manifest를 새 replay timing으로 다시 serialize해 비교하지 않는다.
- unexpected programming/instrumentation exception을 success나 다른 artifact로 숨기지 않는다. expected replay result failure만 stable verification result로 투영한다.
- verify는 bundle/files/Root/Registry/artifact를 mutate하지 않고 파일을 쓰지 않는다.

## Determinism / Replay Boundary

- same registry/profile/seed/content hash/build ID로 100회 grid prefix를 기록하면 `generated_world_sectors.csv` bytes/SHA-256은 exact 동일하다.
- 서로 다른 valid clock schedule이면 manifest started/duration bytes는 달라도 허용한다. 그 차이는 static generated world mismatch가 아니다.
- same bundle을 fresh/reused player로 100회 검증해도 success와 sector bytes가 같다.
- different seed/content/build/profile/tampered sector row를 각각 탐지하고 stable code를 반환한다.
- roadmap의 `100회 동일 hash`는 generated static sector output hash를 뜻한다. timing-bearing `seed_manifest.csv` 또는 whole directory byte hash의 동일성을 요구하거나 위조하지 않는다.
- publisher rerun은 timing이 다른 manifest를 directory 단위로 교체할 수 있지만 같은 seed의 sector bytes는 동일해야 한다.

## Baseline / Meta Stability

MAP02_05 PASS 이후 clean baseline:

```text
Pass execution record focused: 77/77
WorldGenerationRoot focused: 84/84
MAP02_01/02/03 focused: 56/103/90
Targeted EditMode: 1277/1277
Full EditMode: 1297/1297
Authoring CSV/meta: 50/50
Assets meta: 2973
accepted legacy Editor folder meta: 6/6
duplicate GUID groups: 0
```

legacy folder meta 6개는 정상 baseline이며 삭제·재작성·신규 drift로 분류하지 않는다. 새 directory/folder meta expected `0`. 신규 matching meta 8개 반영 clean final Assets meta는 `2981`이다.

## DO NOT

- Root/pass/record/RNG/artifact/Registry/content hash 기존 구현 수정 금지
- recorder/player가 Root를 재실행하거나 hidden full generation을 수행하는 것 금지
- timing을 RNG, replay identity, generated static bytes, success 판정에 사용 금지
- whole bundle/manifest timing bytes를 정적 결정론 hash로 취급 금지
- post-grid generated CSV, edges placeholder, JSON, checksum sidecar, approval/failure bundle 구현 금지
- `approved=1`, placeholder biome/route/site/recipe ID, Root issue를 failure rule ID로 기록 금지
- content hash 재계산, generator build ID 자동 탐색/추론, reflection/assembly scan 금지
- random/GUID/timestamp temp name, per-file final overwrite, stale staging 자동 삭제 금지
- static mutable recorder/current bundle/cache, singleton service locator, event bus 금지
- UnityEditor/AssetDatabase/MonoBehaviour/ScriptableObject/Scene·Game overlay 금지
- exception swallow, malformed input canonical repair, test skip/ignore/assertion 완화 금지
- new directory/folder meta/asmdef/asmref, Authoring CSV/meta/Scene/Prefab/Package/ProjectSettings/Git 변경 금지

## Tests / Verification

Focused minimum 64 cases:

- immutable SeedManifest validation/copy/exact field preservation and exact P00 constants
- exact 184-byte header template SHA, BOM/CRLF/one-row/final CRLF, invariant numeric/UTC/bool/list/RFC4180 escaping
- strict deserialize rejection matrix: BOM/header/UTF-8/line ending/quote/field/record/type/hash/UTC/range
- canonical deserialize→serialize exact bytes
- bundle exact two-file order, defensive copies, relative path D16 including 0/1234/ulong max
- unsafe world ID/path traversal/root escape and manifest/path mismatch rejection
- successful recorded PASS_GRID mapping exact 11 manifest fields and existing sectors bytes
- failed/full/post-grid/wrong pass/multiple attempt/retry/missing GRID/wrong seed/non-grid artifact rejection without Root re-execution
- publisher new publish, load, replacement, read-back, exact file set, no staging/backup residue
- publisher stale staging/backup, extra/missing/case-variant file/subdirectory and invalid root rejection
- player precondition order and Root invocation 0 on invalid/hash/build mismatch
- player exact one replay invocation, identity/record/artifact checks, sector byte equality
- tampered header/row/seed/profile/static byte를 각 boundary에서 constructor/load rejection 또는 exact stable verification code로 탐지
- two timing schedules differ only manifest diagnostics; sector bytes exact
- same seed 100 recorder/replay runs, fresh/reused instance isolation
- no later-output placeholder, JSON/sidecar, UnityEditor/static mutable state
- existing `56/103/90/84/77` focused regressions
- accepted meta 6 unchanged, existing modification 0, new directory 0

```text
New seed manifest/replay recorder: >=64 PASS
MAP02_01 GeneratedWorldData: 56/56 PASS
MAP02_02 deterministic RNG streams: 103/103 PASS
MAP02_03 GridInitializationPass: 90/90 PASS
MAP02_04 WorldGenerationRoot: 84/84 PASS
MAP02_05 execution records: 77/77 PASS
MAP00 coordinate/architecture regression: PASS
MAP01 Registry/content/import regression: PASS
Previous targeted baseline: 1277/1277 PASS
Targeted total: >=1341 PASS
Full project EditMode: >=1361 PASS
Unity 6000.3.8f1 / force refresh / compile error 0 / relevant warning 0
PlayMode NOT RUN / Visual NOT APPLICABLE / Scene-Prefab changes NONE
```

Authoring CSV/meta `50/50` unchanged, accepted folder meta `6/6` unchanged, 신규 matching meta `8/8` valid, final Assets meta `2981`, project duplicate GUID `0`을 확인한다. Task marker 이후 final Assets 변경은 신규 C# 8 + matching meta 8 = `16`, unexpected `0`이어야 한다. existing Assets modification exact `0`이다. Unity evidence가 없거나 한 조건이라도 실패하면 `BLOCKED`.

## Result / Completion

Result: `REPORTS/MAP02_06_IMPLEMENT_SEED_MANIFEST_AND_REPLAY_RECORDER_RESULT.md`.

Required sections: TASK, STATUS, SUMMARY, READ, MASTER BACKLOG CHECK, MAP02_05 GATE CHECK, CREATED, MODIFIED, PREEXISTING_IDENTICAL, SEED MANIFEST, CSV BYTES, REPLAY BUNDLE, RECORDER, PUBLISHER, PLAYER, VERIFICATION RESULT, DETERMINISM BOUNDARY, TEST, UNITY, ASSET META VALIDATION, CHANGE SCOPE, OUT_OF_SCOPE_FINDINGS, DONE CONDITIONS, NEXT, Recommended Commit.

모든 계약과 회귀가 PASS일 때만 MAP02_06 COMPLETE, Current Task NONE으로 finalize한다. `MAP02_07_CREATE_WORLD_TOPOLOGY_OVERLAY`는 LOCKED로 유지하고 자동 시작하지 마.

Recommended Commit: `feat(map): add grid seed replay bundles`
