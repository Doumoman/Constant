# MAP02_02 — Implement Deterministic RNG Streams

```yaml
status_control:
  task_key: MAP02_02_IMPLEMENT_DETERMINISTIC_RNG_STREAMS
  result_file: REPORTS/MAP02_02_IMPLEMENT_DETERMINISTIC_RNG_STREAMS_RESULT.md
```

## Objective

`world seed + RngStreamDefinition salt + stream ID + reset scope + stable scope identity + retry ordinal`를 exact domain-separated SHA-256 seed로 파생하고, platform·locale·실행 순서와 무관한 SplitMix64 stream을 구현한다. `RNG_WORLD_SITE`, `RNG_BIOME_PATCH`, `RNG_ROUTE`, `RNG_TYPE0`, `RNG_SECTOR_RECIPE`, `RNG_POPULATION` 6개를 서로 독립된 Runtime API로 열고, 한 stream의 draw/retry/candidate 수가 다른 stream의 수열을 변경하지 못하게 한다.

## Mandatory Read / Scope

entrypoint → locked/work/CSV/Unity/change/patch/finalize rules → Master → Status → 이 Task → MAP02_01 PASS Result 순서로 읽는다. 그 다음 아래만 읽는다.

```text
Map Package v1.0/01_FIXED_SPEC/02_COORDINATE_AND_ID_RULES.md  # RNG 안정성 절
Map Package v1.0/01_FIXED_SPEC/04_RUNTIME_ARCHITECTURE.md      # 결정론 절
Map Package v1.0/02_PHASE_ROADMAP/MAP02_TOPOLOGY_GRAYBOX.md    # RNG 입력/원칙만
Map Package v1.0/03_CSV_SCHEMA/CSV_DATA_DICTIONARY.csv         # rng_streams.csv 5행만
Map Package v1.0/04_CSV_STARTER/rng_streams.csv                # exact 7 rows
Map Package v1.0/04_CSV_STARTER/generation_passes.csv          # pass_id/rng_stream_id/reset 관계만
```

existing `CsvHexValue`, `RngStreamDefinition`, `WorldRouteDefinitionSet.RngStreams`, `StaticDataRegistry` definition root, `SectorCoord`, MAP01_07 direct tests, MAP02_01 production/tests, Runtime/EditMode asmdef를 읽어 exact public API를 재사용한다. MAP02_03 이후 Task body, Legacy/Stage/P6/P11 generator, 다른 CSV data rows, Scene/Prefab YAML, 비승인 production은 읽거나 사용하지 마.

## WRITE ALLOWLIST

Runtime C# 6:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/RngResetScope.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/RngStreamScope.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/DeterministicRngStream.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/DeterministicRngSeedDeriver.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/DeterministicRngStreamFactory.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationRngStreams.cs
```

EditMode test C# 1:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/DeterministicRngStreamTests.cs
```

신규 C# 7 + matching `.cs.meta` 7 + Result 1만 허용한다. existing MAP00/01/MAP02_01 C#/tests/meta, CSV/meta, asmdef, Scene/Prefab/Package/ProjectSettings 수정 금지. Runtime namespace `StarNight.Map.WorldGeneration.Generation`, existing `Game.Map.Runtime` / `Game.Map.Tests.EditMode` assembly를 재사용하고 `UnityEditor` reference/신규 asmdef/asmref를 만들지 마.

## Reset Scope Contract

`RngResetScope` exact 6 values/token:

```text
World  <-> WORLD
Pass   <-> PASS
Sector <-> SECTOR
Patch  <-> PATCH
Site   <-> SITE
Spawn  <-> SPAWN
```

parser/formatter는 ordinal exact switch를 사용하고 trim, case-fold, enum numeric cast, fallback을 금지한다. undefined/token mismatch는 즉시 거부한다.

`RngStreamScope`는 immutable value로 `ResetScope`, exact `Identity`, non-negative `AttemptOrdinal`을 가진다.

- `WORLD`: Identity exact empty string.
- `PASS`: exact `pass_id`.
- `SECTOR`: existing `SectorCoord` helper로 invariant exact `x,y`.
- `PATCH`/`SITE`/`SPAWN`: caller가 제공한 non-empty stable generated ID/scope ID.
- non-WORLD empty identity, WORLD non-empty identity, null, negative attempt를 거부한다.
- identity를 trim/case-fold/Unicode normalize하지 않는다.

## Seed Derivation v1 — Exact Bytes

`DeterministicRngSeedDeriver` input gate:

- non-null active `RngStreamDefinition`
- non-empty exact stream ID
- `SaltHex` exact 8 bytes; text/locale parse가 아닌 existing `CsvHexValue` bytes를 사용
- definition `ResetScope` token과 `RngStreamScope.ResetScope` exact match
- valid scope identity/attempt

Salt numeric은 hex byte order를 unsigned 64-bit big-endian으로 해석한다. SHA-256 input을 아래 순서로 exact 연결한다.

```text
raw ASCII                         "STARNIGHT_MAP_RNG_V1"
u64 big-endian                    world_seed
8 raw salt bytes                  salt_hex
u64be UTF-8 byte length + bytes   rng_stream_id
u64be UTF-8 byte length + bytes   reset_scope token
u64be UTF-8 byte length + bytes   scope identity
u64 big-endian                    attempt_ordinal
```

- string encoding strict UTF-8, invalid surrogate 거부; length는 char가 아닌 byte 수.
- SHA-256 digest의 first 8 bytes를 unsigned big-endian `InitialState`로 사용한다.
- BinaryWriter/.NET native endian, `HashCode`, `GetHashCode`, GUID, timestamp, machine path, process/thread ID, random salt를 사용하지 않는다.
- 입력 요소 하나라도 다르면 domain seed가 달라져야 한다.

## SplitMix64 v1 — Exact Sequence

`DeterministicRngStream` instance는 독립 mutable state를 가지되 shared/static/global state를 사용하지 않는다. `NextUInt64()` 1회는 unchecked unsigned 64-bit arithmetic으로 exact 다음을 수행한다.

```text
state += 0x9E3779B97F4A7C15
z = state
z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9
z = (z ^ (z >> 27)) * 0x94D049BB133111EB
return z ^ (z >> 31)
```

Public behavior:

- `InitialState` read-only, `DrawCount` read-only; 상태 setter/restore/global reseed 없음.
- `NextUInt64()`는 exact 수열과 actual draw count를 진행한다.
- `NextInt(exclusiveMax)` / `NextInt(minInclusive,maxExclusive)`는 modulo bias를 피하는 rejection sampling. bound `b`의 threshold는 unchecked `(0UL - b) % b`; `r < threshold`면 재추첨한다.
- invalid/empty range를 거부하고 `int.MinValue .. int.MaxValue` 반개구간처럼 표현 가능한 최대 범위도 width 오버플로 없이 처리한다.
- `NextDouble01()`은 `(NextUInt64() >> 11) * (1.0 / 9007199254740992.0)`으로 exact `[0,1)`을 반환한다.
- API는 `System.Random`, `UnityEngine.Random`, LINQ enumeration order, wall-clock/thread schedule에 의존하지 않는다.

## Required Stream Factory / Independence

`DeterministicRngStreamFactory`는 existing immutable `WorldRouteDefinitionSet`/`RngStreams`의 exact `RngStreamDefinition` instance를 사용하고 소스를 clone/mutate/filter하지 않는다. stream ID + world seed + scope를 받아 매번 fresh instance를 반환한다. missing/inactive/invalid salt/scope mismatch는 거부하고 partial/fallback stream을 만들지 않는다.

`WorldGenerationRngStreams`는 아래 exact required catalog를 ordinal/read-only로 고정하고 construction 시 존재·active·reset scope를 검증한다. salt 값은 hard-code하지 않고 definition에서 소유한다.

```text
RNG_WORLD_SITE      WORLD
RNG_BIOME_PATCH     PASS
RNG_ROUTE           PASS
RNG_TYPE0           PASS
RNG_SECTOR_RECIPE   SECTOR
RNG_POPULATION      SPAWN
```

Typed creation methods는 exact scope를 받아 fresh stream을 만든다. generic factory는 `RNG_VILLAGE` 및 후속 active definitions도 같은 v1 규칙으로 열 수 있되, MAP02 required catalog에 임의로 추가하지 않는다.

- 같은 input으로 새로 만든 stream은 항상 같은 수열.
- 한 stream을 N회 더 draw해도 다른 stream instance/state/draw count/수열에 영향 `0`.
- stream 생성/소비 순서를 바꿔도 ID별 수열은 같다.
- singleton/cache/shared mutable dictionary로 stream instance를 재사용하지 않는다.

## Required Known Vectors

Common world seed `0x0123456789ABCDEF`, attempt `0`, fixed starter definitions로 아래를 exact 검증한다.

| Stream | Scope identity | InitialState | first `NextUInt64()` | second `NextUInt64()` |
|---|---|---:|---:|---:|
| `RNG_WORLD_SITE` | empty | `60D4B46EBF6EF00D` | `F627BD56683B33FC` | `4CA318D8E4EA97BA` |
| `RNG_BIOME_PATCH` | `PASS_BIOME` | `98BC23250806566B` | `D2E329C4A736E686` | `F63F41F61CC1B52C` |
| `RNG_ROUTE` | `PASS_ROUTE` | `8EDC9EB9BA0977DC` | `CA6E229CF519975D` | `2289076DA3C2FFE2` |
| `RNG_TYPE0` | `PASS_TYPE0` | `570969677634D631` | `3F79615689D9D77E` | `8A8D7006920CD2E8` |
| `RNG_SECTOR_RECIPE` | `6,6` | `08D7C54EF3F843DE` | `612FB5C8F12DDB0A` | `DD0D4A17DDF66EA1` |
| `RNG_POPULATION` | `6,6` | `36D00A33DAED7549` | `472FBC58241A8307` | `93591B6C5B950D32` |

위 테이블을 실행환경에서 계산해 맞추는다. 테스트를 통과시키려고 production에 vector 결과를 lookup/hard-code하지 마.

## DO NOT

- `System.Random`, `UnityEngine.Random`, `Random.Range`, engine/version-dependent PRNG 금지
- stream ID/salt/reset scope/scope identity/attempt 중 하나라도 시드에서 생략 금지
- shared/global RNG, singleton/service locator, cached mutable stream instance 금지
- CSV 재파싱, raw row dictionary, salt fallback/default, inactive stream 사용 금지
- candidate filtering/sort/weight selection, biome/site/route/type0/recipe/population 생성 알고리즘 금지
- grid/neighbor, `WorldGenerationRoot`, pass retry execution/record, seed manifest/replay/file I/O 금지
- `GeneratedWorldData`/serializer 수정, EditorWindow/overlay/visual/Scene integration 금지
- exception swallow, test vector lookup table, test skip/ignore/assertion 완화 금지
- existing C#/tests/CSV/meta/asmdef/Scene/Prefab/Package/ProjectSettings/Git 변경 금지

## Tests / Verification

Focused minimum 48 cases:

- exact 6 reset scope token roundtrip, invalid/case/space/numeric rejection
- WORLD/non-WORLD identity, null/empty, negative attempt, sector identity contract
- salt exact 8 bytes, inactive/missing definition, reset mismatch rejection
- seed material magic/order/u64be/UTF-8 byte-length and six known InitialState vectors
- six known SplitMix64 first/second outputs, zero/max state wraparound
- same input 100회 exact same sequence; world seed/salt/ID/scope/scope ID/attempt one-field mutation sensitivity
- all six required IDs/scopes exact, missing/inactive/wrong-scope catalog failure, `RNG_VILLAGE` generic support
- interleaved/reversed stream creation and consumption order independence
- one stream extra draw/rejection/retry does not alter other five streams
- `NextUInt64` draw count, `NextInt` valid range/full int range/invalid range/rejection, `NextDouble01` exact `[0,1)`
- invariant behavior under at least two cultures; no `System.Random`/Unity random/hash/time/path/thread/global state
- existing definitions/Registry/MAP02_01 world data unchanged and read-only

```text
New deterministic RNG streams: >=48 PASS
MAP02_01 GeneratedWorldData: 56/56 PASS
MAP00 coordinate/architecture regression: PASS
MAP01 Registry/content/import regression: PASS
Previous targeted baseline: 923/923 PASS
Targeted total: >=971 PASS
Full project EditMode: >=991 PASS
Unity 6000.3.8f1 / force refresh / compile error 0 / relevant warning 0
PlayMode NOT RUN / Visual NOT APPLICABLE / Scene-Prefab changes NONE
```

Authoring CSV/meta `50/50` unchanged, existing production/test/asmdef modifications `0`, new meta `7` valid/GUID duplicate `0`. Unity evidence가 없거나 한 조건이라도 실패하면 `BLOCKED`.

## Result / Completion

Result: `REPORTS/MAP02_02_IMPLEMENT_DETERMINISTIC_RNG_STREAMS_RESULT.md`.

Required sections: TASK, STATUS, SUMMARY, READ, MASTER BACKLOG CHECK, MAP02_01 GATE CHECK, CREATED, PREEXISTING_IDENTICAL, RESET SCOPE, SEED DERIVATION V1, SPLITMIX64, REQUIRED STREAMS, KNOWN VECTORS, INDEPENDENCE, TEST, UNITY, ASSET META VALIDATION, CHANGE SCOPE, OUT_OF_SCOPE_FINDINGS, DONE CONDITIONS, NEXT, Recommended Commit.

모든 계약과 회귀가 PASS일 때만 MAP02_02 COMPLETE, Current Task NONE으로 finalize한다. `MAP02_03_IMPLEMENT_GRID_INITIALIZATION_PASS`는 LOCKED로 유지하고 자동 시작하지 마.

Recommended Commit: `feat(map): add deterministic rng streams`
