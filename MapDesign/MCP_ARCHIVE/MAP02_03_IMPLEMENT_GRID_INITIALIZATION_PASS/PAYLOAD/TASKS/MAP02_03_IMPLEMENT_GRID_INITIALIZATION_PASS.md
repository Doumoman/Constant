# MAP02_03 — Implement Grid Initialization Pass

```yaml
status_control:
  task_key: MAP02_03_IMPLEMENT_GRID_INITIALIZATION_PASS
  result_file: REPORTS/MAP02_03_IMPLEMENT_GRID_INITIALIZATION_PASS_RESULT.md
```

## Objective

MAP02의 `P00 Grid`를 구현한다. exact 13×13 = 169개 `SectorCell`을 아래쪽 행부터 `(y * WorldGenConstants.SectorColumns + x)`로 생성하고, 각 index의 L/R/U/D 이웃 index를 사전 계산하며 월드 밖은 exact `-1`로 기록한다. 출력은 기존 immutable `GeneratedWorldData`와 immutable neighbor snapshot을 묶은 `GridInitializationResult`다.

이 Task는 순수·stateless 초기화 pass만 만든다. RNG, pass orchestration, retry/record, file I/O, replay, overlay는 후속 Task 범위다.

## Mandatory Read / Scope

entrypoint → locked/work/CSV/Unity/change/patch/finalize rules → Master → Status → 이 Task → MAP02_02 PASS Result 순서로 읽는다.

Map Package v1.0의 exact path가 installed tree에 존재하면 아래 부분만 읽는다.

```text
01_FIXED_SPEC/01_WORLD_FIXED_SPEC.md                    # 13×13/169 수치만
01_FIXED_SPEC/02_COORDINATE_AND_ID_RULES.md             # 원점·L/R/U/D 방향만
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md  # P00 Grid ownership만
02_PHASE_ROADMAP/MAP02_TOPOLOGY_GRAYBOX.md              # grid/neighbor/initial cell만
04_CSV_STARTER/generation_passes.csv                    # PASS_GRID row만
```

exact 문서가 installed tree에 없으면 이 Task에 동결된 계약을 authoritative fallback으로 사용하고, 대체 문서를 broad search하거나 Legacy/다른 generator를 읽지 않는다.

기존 API 확인은 아래 exact 파일로 제한한다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldCoordinateUtility.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedSectorRole.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldData.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldDataCsvSerializer.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/RngResetScope.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/RngStreamScope.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/DeterministicRngStream.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/DeterministicRngSeedDeriver.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/DeterministicRngStreamFactory.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGenerationRngStreams.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/GeneratedWorldDataTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/DeterministicRngStreamTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

경로 발견에는 approved `Generation` / test `Generation` 폴더의 `rg --files` path-only inventory만 허용한다. content search는 위 exact 파일로 한정하고, broad recursive `rg`가 다른 파일의 match body를 출력하게 하지 않는다. MAP02_04 이후 Task body, Legacy/Stage/P6/P11 generator, 다른 CSV rows, Scene/Prefab YAML은 읽거나 사용하지 않는다.

## WRITE ALLOWLIST

Runtime C# 4:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/WorldGridIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorNeighborIndices.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GridInitializationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GridInitializationPass.cs
```

EditMode test C# 1:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/GridInitializationPassTests.cs
```

신규 C# 5 + matching `.cs.meta` 5 + Result 1만 허용한다. 모든 target은 이미 승인된 `Generation` 디렉터리에 둔다. roadmap의 `Generation/Passes/GridInitializationPass.cs`는 logical responsibility로만 해석하고, 현재 locked 36-directory scaffold에 없는 새 `Passes` directory/folder meta는 만들지 않는다. MAP00 structure/assembly regression을 보존하기 위한 exact path 결정이다.

existing MAP00/01/MAP02_01~02 C#/tests/meta, accepted legacy Editor folder meta 6개, Authoring CSV/meta, asmdef, Scene/Prefab/Package/ProjectSettings 수정 금지. Runtime namespace `StarNight.Map.WorldGeneration.Generation`, existing `Game.Map.Runtime` / `Game.Map.Tests.EditMode` assembly를 재사용하며 `UnityEditor` reference와 신규 asmdef/asmref를 만들지 않는다.

## `WorldGridIndex` Contract

`WorldGridIndex`는 stateless public static helper이며 모든 수치는 `WorldGenConstants`를 사용한다. public behavior:

```text
int ToIndex(SectorCoord coordinate)
SectorCoord ToCoordinate(int index)
int GetLeftIndex(int index)
int GetRightIndex(int index)
int GetUpIndex(int index)
int GetDownIndex(int index)
```

Exact mapping:

```text
index = coordinate.Y * WorldGenConstants.SectorColumns + coordinate.X
x = index % WorldGenConstants.SectorColumns
y = index / WorldGenConstants.SectorColumns
```

- valid index는 `0..WorldGenConstants.SectorCount-1`; 그 밖은 즉시 거부한다.
- existing valid `SectorCoord`를 사용하고 coordinate를 clamp/wrap/normalize하지 않는다.
- 모든 `0..168`에서 `ToIndex(ToCoordinate(index)) == index`다.
- literal `13`, `12`, `169`를 알고리즘에 중복 하드코딩하지 않는다.
- 원점은 왼쪽 아래이며 Y가 증가할수록 위쪽 행이다.

## Neighbor Contract

`SectorNeighborIndices`는 sealed immutable object다.

```text
public const int NoNeighbor = -1
SectorNeighborIndices(int index, int leftIndex, int rightIndex, int upIndex, int downIndex)
int Index
int LeftIndex
int RightIndex
int UpIndex
int DownIndex
int ValidNeighborCount
```

유효 neighbor는 `0..168`, 월드 밖 sentinel만 `-1`이다. self-reference, valid duplicate, 다른 음수나 169 이상을 거부한다. public setter/mutable field/caller-owned mutable collection을 노출하지 않는다.

각 index `i`와 coordinate `(x,y)`의 exact 값:

```text
L = x > 0                              ? i - 1             : -1
R = x < WorldGenConstants.SectorColumns-1 ? i + 1          : -1
U = y < WorldGenConstants.SectorRows-1 ? i + SectorColumns : -1
D = y > 0                              ? i - SectorColumns : -1
```

neighbor 순서 의미는 항상 L/R/U/D다. 좌우 row wrap, 상하 flip, diagonal, toroidal wrap은 금지한다.

고정 orientation examples:

```text
index   0 = ( 0, 0): L=-1, R= 1, U=13, D=-1
index  12 = (12, 0): L=11, R=-1, U=25, D=-1
index  84 = ( 6, 6): L=83, R=85, U=97, D=71
index 156 = ( 0,12): L=-1, R=157, U=-1, D=143
index 168 = (12,12): L=167, R=-1, U=-1, D=155
```

Global invariants:

```text
corner cells                4 × 2 neighbors
non-corner boundary cells  44 × 3 neighbors
interior cells             121 × 4 neighbors
directed valid links       624
undirected grid edges      312
connected components         1
```

모든 valid L/R과 U/D는 reciprocal이어야 한다.

## `GridInitializationResult` Contract

`GridInitializationResult`는 sealed immutable snapshot으로 아래를 제공한다.

```text
GeneratedWorldData WorldData
IReadOnlyList<SectorNeighborIndices> Neighbors
GridInitializationResult(GeneratedWorldData worldData, IEnumerable<SectorNeighborIndices> neighbors)
SectorNeighborIndices GetNeighbors(int index)
SectorNeighborIndices GetNeighbors(SectorCoord coordinate)
bool TryGetNeighbors(int index, out SectorNeighborIndices neighbors)
```

- null 없는 exact 169 neighbor entry와 exact index set `0..168`을 요구한다.
- caller order와 무관하게 index 오름차순 copied read-only snapshot을 보관한다.
- caller collection mutation이 결과를 바꾸지 못한다.
- `WorldData.Cells[i].Index == i`, coordinate가 `WorldGridIndex.ToCoordinate(i)`와 exact 일치해야 한다.
- 각 neighbor entry는 `WorldGridIndex`의 exact L/R/U/D와 일치해야 하며 잘못된 topology를 자동 수정하지 않고 거부한다.
- invalid `Get`은 거부하고 invalid `TryGet`은 false/null을 반환한다.
- Unity object/scene reference, serializer/file path, mutable cache를 보관하지 않는다.

## `GridInitializationPass` Contract

`GridInitializationPass`는 public sealed, parameterless, stateless Runtime class다.

```text
public const string PassId = "PASS_GRID"
public const string OutputArtifactId = "GRID"
GridInitializationResult Execute(ulong worldSeed)
```

`Execute`는 y outer / x inner 순서로 exact 169회를 순회한다.

1. `SectorCoord(x,y)` 생성
2. `WorldGridIndex.ToIndex`로 index 생성
3. `SectorCell.CreateUnassigned(index, coordinate)` 생성
4. L/R/U/D를 사전 계산한 `SectorNeighborIndices` 생성
5. existing `GeneratedWorldData(worldSeed, cells)`와 immutable neighbor snapshot을 result로 반환

모든 초기 cell은 exact 아래 상태다.

```text
Role = Unassigned
PrimaryBiomeId = empty
SecondaryBiomeId = empty
PatchId = empty
RouteMaskId = empty
SpecialSiteInstanceId = empty
BoundaryProfileId = empty
SectorRecipeId = empty
ReservationId = empty
ShortestDistanceFromStart = -1
MandatoryGraphNode = false
```

PASS_GRID는 CSV상 input artifact와 RNG stream이 없다. `Execute` API는 RNG/factory/Registry/time/context를 받지 않고, `DeterministicRngStream`, `System.Random`, `UnityEngine.Random`을 생성·draw·참조하지 않는다. seed는 `GeneratedWorldData.Seed` 보존에만 사용하며 topology와 neutral cell fields를 바꾸지 않는다.

## Determinism / Ownership

- 같은 seed로 100회 실행하면 cell/index/coordinate/neighbor tuple과 existing `GeneratedWorldDataCsvSerializer` bytes/hash가 동일하다.
- seed `0`과 `ulong.MaxValue`를 보존한다.
- 다른 seed는 `WorldData.Seed`와 CSV seed field만 달라지며 topology와 neutral fields는 같다.
- pass instance 재사용, fresh instance, 호출 순서에 따라 결과가 달라지지 않는다.
- 외부 RNG stream의 생성/소비 순서와 무관하다. PASS_GRID 자체는 RNG state를 소유하지 않는다.
- P00가 소유하는 index/coordinate/neighbor 결과를 후속 pass가 덮어쓸 mutable API를 제공하지 않는다.

## Baseline / Meta Stability

MAP02_02 PASS 이후 clean baseline:

```text
Authoring CSV/meta: 50/50
Assets meta: 2954
accepted legacy Editor folder meta: 6/6
duplicate GUID groups: 0
targeted EditMode: 1026/1026
full EditMode: 1046/1046
```

legacy Editor folder meta 6개는 이미 baseline에 포함된 정상 Unity metadata다. 삭제·재생성·새 drift로 분류하지 말고 bytes/hash를 보존한다. 이 Task는 directory를 만들지 않으므로 새 folder meta expected `0`이다. clean path 최종값은 matching meta 5개가 추가된 Assets meta `2959`다.

## DO NOT

- existing `SectorCell`, `GeneratedWorldData`, serializer, RNG production/test 수정 금지
- `Generation/Passes` 또는 다른 새 directory/folder meta 생성 금지
- `WorldGenerationRoot`, common pass interface, CSV pass orchestration 구현 금지
- pass start time/duration/retry/failure record 구현 금지
- seed manifest/replay recorder/generated file I/O/JSON/hash production 구현 금지
- biome/site/route/type0/recipe/population assignment 금지
- overlay/Gizmo/EditorWindow/Scene·Game visual integration 금지
- diagonal/wrapped neighbor, y flip, placeholder ID, hidden topology correction 금지
- singleton/static mutable state, exception swallow, test skip/ignore/assertion 완화 금지
- CSV/meta/asmdef/Scene/Prefab/Package/ProjectSettings/Git 변경 금지

## Tests / Verification

Focused minimum 48 cases:

- all 169 index↔coordinate exhaustive roundtrip and invalid index rejection
- exact five orientation examples and all boundary no-wrap cases
- all 169 exact L/R/U/D values against independent expected formula
- corner 4, edge 44, interior 121 distribution
- directed 624 / undirected 312 / reciprocal links / connected 169-cell BFS
- `SectorNeighborIndices` invalid range/self/duplicate rejection and immutable properties
- result null/count/index/coordinate/topology mismatch rejection, copied read-only order/lookup/TryGet
- exact 169 unassigned cells and all ID/sentinel/bool defaults
- seed `0`/`ulong.MaxValue`, same-seed 100-run topology and CSV byte/hash equality
- different seed changes only seed, pass reuse/fresh instance equality
- exact `PASS_GRID` / `GRID`, no RNG parameter/state/draw/random dependency
- existing MAP02_01 `56/56` and MAP02_02 `103/103` regressions unchanged
- no new directory/folder meta, accepted six preserved, no existing file modification

```text
New GridInitializationPass: >=48 PASS
MAP02_01 GeneratedWorldData: 56/56 PASS
MAP02_02 deterministic RNG streams: 103/103 PASS
MAP00 coordinate/architecture regression: PASS
MAP01 Registry/content/import regression: PASS
Previous targeted baseline: 1026/1026 PASS
Targeted total: >=1074 PASS
Full project EditMode: >=1094 PASS
Unity 6000.3.8f1 / force refresh / compile error 0 / relevant warning 0
PlayMode NOT RUN / Visual NOT APPLICABLE / Scene-Prefab changes NONE
```

Authoring CSV/meta `50/50` unchanged. 기존 accepted folder meta `6/6` unchanged. 신규 matching meta `5/5` valid, final Assets meta `2959`, project duplicate GUID `0`, task marker 이후 final Assets 변경 exact allowlisted C# 5 + meta 5, unexpected `0`을 확인한다. Unity evidence가 없거나 한 조건이라도 실패하면 `BLOCKED`.

## Result / Completion

Result: `REPORTS/MAP02_03_IMPLEMENT_GRID_INITIALIZATION_PASS_RESULT.md`.

Required sections: TASK, STATUS, SUMMARY, READ, MASTER BACKLOG CHECK, MAP02_02 GATE CHECK, CREATED, PREEXISTING_IDENTICAL, GRID INDEX, NEIGHBOR TOPOLOGY, INITIAL CELLS, GRID INITIALIZATION PASS, DETERMINISM, TEST, UNITY, ASSET META VALIDATION, CHANGE SCOPE, OUT_OF_SCOPE_FINDINGS, DONE CONDITIONS, NEXT, Recommended Commit.

모든 계약과 회귀가 PASS일 때만 MAP02_03 COMPLETE, Current Task NONE으로 finalize한다. `MAP02_04_IMPLEMENT_WORLD_GENERATION_ROOT`는 LOCKED로 유지하고 자동 시작하지 마.

Recommended Commit: `feat(map): initialize deterministic world grid`
