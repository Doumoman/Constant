TASK: MAP11_04_IMPLEMENT_BASE_HIGH_AND_RECOVERY_ROUTES
STATUS: PASS
MAP11_04: COMPLETE ELIGIBLE
MAP11_05_IMPLEMENT_CLUSTER_PATTERN_ZONES_AND_RENDERER: LOCKED / DO NOT START

## User-Facing Implementation Report

이번 구현의 목적은 한 지형 클러스터 안에서 플레이어가 따라갈 기본 길, 선택 가능한 높은 길, 실패했을 때 기본 길로 돌아오는 길을 같은 지도 근거로 확인할 수 있게 하는 것이다. 플레이어 관점에서는 길이 끊기거나 복귀가 지나치게 짧거나 긴 콘텐츠가 이후 단계로 넘어가기 전에 걸러진다. 맵 제작자 관점에서는 아직 실제 점프 물리를 실행하지 않고도 저작한 길의 연결, 예상 이동 시간, 이점과 실패 지점을 데이터로 검증할 수 있다.

`TerrainClusterStaticShell.cs`는 활성 타일을 한 번씩만 게시하고, 기본 Air와 바닥 보호용 Solid를 구분하며, 이동 공간을 열어 두어야 하는 근거를 보존한다. `TerrainClusterRouteWitness.cs`는 시간 근거, high-route intent, 기본·높은·복귀 경로 증거, 최종 보고서와 오류 결과를 읽기 전용 모델로 제공한다. `TerrainClusterRouteWitnessCompiler.cs`는 앞 단계 산출물의 신원과 해시를 확인하고, 안정적인 기본 경로와 high/recovery 경로를 선택하며, 충돌 시 부분 결과 없이 실패한다. `TerrainClusterRouteWitnessCompilerTests.cs`는 이 동작과 시간 경계, 결정성, 실패 원자성을 MAP11_04 범위에서 검증한다.

새로 가능한 기능은 모든 variant를 합친 최소 Static Shell 생성, Entry에서 Exit까지의 결정적 기본 경로 선택, 저작된 high route의 구조·high point·이점 검증, 각 실패 지점에서 기본 경로로 돌아오는 2000~5000ms 복귀 경로 선택, 그리고 전체 증거의 canonical digest 발행이다.

실제 파이프라인 위치는 `MAP11_01 Local Canvas → MAP11_02 역할/소켓 → MAP11_03 이동 그래프/보호 공간 → MAP11_04 Static Shell과 route witness`다. 다음의 pattern zone/renderer는 MAP11_05 책임이며 이번 작업에서 시작하지 않았다.

아직 실제 collider, 속도, 점프 궤적, 공중 제어, 보상/NPC 배치, pattern 적용, starter 16종 콘텐츠, sector/world 조립은 구현하지 않았다. 따라서 지금은 게임 화면에 새 지형이 보이는 단계가 아니라, 이후 저작과 렌더 단계가 사용할 경로 데이터를 검증하는 단계다. 실제 화면 변화는 후속 pattern/content/render 연결이 완료된 뒤에 나타난다.

## Responsibility and Added Functions

| File | Responsibility | Added functions |
|---|---|---|
| `TerrainClusterStaticShell.cs(.meta)` | Pattern 제거 상태의 최소 immutable geometry | active exact-once cells, default Air, Floor Solid, protected-open provenance, lookup, pattern operation count 0 |
| `TerrainClusterRouteWitness.cs(.meta)` | Immutable intent, timing, witnesses, report, errors/result | defensive canonical collections, baseline/high/recovery evidence, atomic zero-output failure surface |
| `TerrainClusterRouteWitnessCompiler.cs(.meta)` | MAP11_01~03 artifact 결합과 route witness compile | identity/digest checks, stable BFS, high-route validation, weighted recovery selection, canonical SHA-256 digest |
| `TerrainClusterRouteWitnessCompilerTests.cs(.meta)` | MAP11_04 EditMode focused verification | shell/route/timing/digest/immutability/error/side-effect checks |

Runtime namespace is `StarNight.Map.WorldGeneration.TerrainClusters`; assemblies are `Game.Map.Runtime` and `Game.Map.Tests.EditMode`.

Published semantic surface:

```text
TerrainClusterShellOccupancy
TerrainClusterStaticShellProvenance
TerrainClusterStaticShellCell
TerrainClusterStaticShell
TraversalEdgeDurationEvidence
TerrainClusterHighRouteDefinition
TerrainClusterRouteWitnessIntent
TerrainClusterRouteWitnessEdge
TerrainClusterBaselineRouteWitness
TerrainClusterHighRouteWitness
TerrainClusterRecoveryRouteWitness
TerrainClusterRouteWitnessReport
TerrainClusterRouteWitnessCompileRequest
TerrainClusterRouteWitnessCompileErrorCode
TerrainClusterRouteWitnessCompileError
TerrainClusterRouteWitnessCompileResult
TerrainClusterRouteWitnessCompiler
```

## Predecessor, Install, and Status Evidence

```text
HEAD before task: 5886474ab1178ec03daf54724317a90459c8b987
HEAD title: MAP11_03: compile route spine and traversal envelope
MAP11_03 Result SHA-256: 5d92d816fbe6570a75d76d89554a8b8c5a780236bf737a187f635c8bc043a8c1
MAP11_04 inbox/installed/archive Task SHA-256: 4ede8aabf1c78ed607d10be0b51e0430cb62a3c4ebdcb5042dbb81b8bb25faa4
Phase A Status: 215 rows = 126 COMPLETE / 1 CURRENT / 88 LOCKED
Phase A inbox candidates after archive: 0
Phase A staged paths: 0
MAP11_05: LOCKED
```

The task was installed byte-identically, its inbox source was moved to the archive, and only the Current Task field plus the MAP11_04 row were opened.

## Static Shell Evidence

- Every active Local Canvas tile is published exactly once; inactive tiles publish zero cells.
- Cells begin as explicit `Air`; every compiled `Floor` requirement becomes `Solid`.
- `Centerline`, `Clearance`, `JumpArc`, `DropColumn`, `Landing`, and `Recovery` requirements remain explicit `Air` and set protected-open evidence.
- All variants are unioned. Duplicate equal requirements are coalesced while unique variant/edge/envelope/source-coordinate provenance remains lossless.
- A tile required as both Solid and Air produces `StaticShellConflict`; the result exposes no partial shell or route data.
- Pattern operation count is exactly 0. No Surface/Affordance/Material/Hazard/Marker payload is created.

## Baseline, High, and Recovery Witness Evidence

- The intent baseline must equal the single MAP11_03 source baseline variant.
- Entry port→Entry role→Entry node and Exit node→Exit role→Exit port chains are exact. BuildUp, Core, and Recovery evidence must occur in source order on the selected path.
- Stable BFS chooses minimum edge count, then the ordinal edge-ID sequence. Reversed input order produces the same baseline path.
- High routes require a unique `HIGH_ROUTE_[A-Z0-9_]+` ID, a contiguous directed alternate path, baseline divergence/rejoin nodes, an authored high point on that path, structural distinction, at least two distinct valid benefit IDs, and valid non-Entry/Exit failure nodes.
- No high route is inferred from y position. The authored high-point designation is the evidence.
- Each failure node receives a directed source-edge-only recovery witness to a baseline node. A Recovery role node on the baseline is preferred; selection is minimum authored milliseconds, then edge count, then ordinal edge-ID sequence.
- Recovery duration 2000ms and 5000ms succeeds inclusively; 1999ms returns `RecoveryTooShort`, and 5001ms returns `RecoveryTooLong`.
- Missing, duplicate, unknown, non-positive, or cross-ruleset timing evidence returns `InvalidDurationEvidence`.

## Immutability, Digest, and Error Evidence

- Intent, shell, witness, report, and error collections are defensive read-only copies in canonical order.
- Errors accumulate, deduplicate, and stable-sort by code/path/detail.
- Any error publishes zero report, shell, baseline, high routes, recovery routes, and digest.
- All 22 required error distinctions from `MissingInput` through `NonCanonicalPublication` are present.
- The canonical digest includes the timing ruleset, MAP11_03 digest, every shell cell and provenance, canonical intent/durations, port-role-node identities, mandatory roles, all path nodes/edges/movements/coordinates, benefits, protected coordinates, recovery targets, and recovery timings.
- Locale and reversed intent/evidence enumeration do not change the digest; a semantic duration change does.

## Focused Verification and Trigger Record

Unity Editor: `6000.3.8f1`, instance `Constant@ced6e0df`.

```text
MAP11_04 final focused: discovered 16 / executed 16 / pass 16 / fail 0 / skip 0 / inconclusive 0
Final focused job: 0aac3e316b204514877f3937b9294f21
Unity compilation errors: 0
Final cleared Console: errors 0 / relevant warnings 0 / total entries 0
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 TEST SELECTIONS: 0
PLAYMODE TEST SELECTIONS: 0
```

The focused fixture assertions cover all 20 required verification subjects, including exact shell coverage, deterministic baseline selection, role chains, valid/invalid high routes, every recovery witness, inclusive/exclusive timing boundaries, evidence rejection, canonical immutability/digest, atomic accumulated failure, and forbidden side-effect symbols.

```text
REGRESSION TRIGGER DETECTED: YES
Owner: MAP11_04 task-owned compiler/test fixture
Cause 1: initial compile captured an out parameter inside a LINQ lambda; fixed by a local baseline variable
Cause 2: initial recovery boundary fixture gave the ordinary rejoin path 1500ms, so the correct minimum-duration selector chose it instead of the boundary edge; fixed only the fixture timing evidence
Minimum related selection: refresh/compile and rerun MAP11_04 focused only
First focused run: discovered 16 / executed 16 / pass 9 / fail 7
Final focused run: discovered 16 / executed 16 / pass 16 / fail 0
```

No MAP09, MAP10, MAP11_01, MAP11_02, MAP11_03, legacy 19347, PlayMode, or unfiltered test selection was run.

## Static Gates and Change Scope

| Gate | Actual result |
|---|---|
| Existing MAP11_01~03 production/test/meta modifications | 0 |
| Existing MAP00~MAP11_03 production/test/CSV/meta modifications | 0 |
| Runtime forbidden symbol hits | 0 |
| MicroPattern definitions / physical rows | 24 / 453 |
| Catalog CSV SHA-256 | `f9d9e9cc60c4e4d7561c5aa6502228c18fc9566e3e0febab206ea3264b408267` |
| Cells CSV SHA-256 | `e702ae5d02d7ec9d2cda129c1361699e37d942c280c8f9bd1f3200f155084381` |
| Full 52-file Authoring manifest | `4415ae4af5196d6793f5d0152c0688e5bf35dc4ad23442791e45d3cfd81d0851` |
| Generated CSV | 0 |
| Valid Assets meta/GUID rows | 3929 / 3929 |
| Duplicate GUID groups | 0 |
| Missing task-owned `.meta` | 0 |
| Existing asmdef/asmref/Scene/Prefab/Settings/Packages changes | 0 |
| Unapplied inbox candidate / legacy collision | 0 / 0 |
| Staged paths before Finalize | 0 |

Only three new Runtime C# files and metas, one focused test and meta, installed/archive task documents, this Result, and the implementation status file are eligible for the atomic commit. No unrelated file is included.

## Commit Handoff

```text
Subject: MAP11_04: implement base high and recovery routes
Push: NOT PERFORMED
```
