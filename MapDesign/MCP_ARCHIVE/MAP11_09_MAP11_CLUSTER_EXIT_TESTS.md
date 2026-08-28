```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP11_09_MAP11_CLUSTER_EXIT_TESTS
  task_file: TASKS/MAP11_09_MAP11_CLUSTER_EXIT_TESTS.md
  requires_current_task: NONE
  requires_completed_task: MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES
  requires_result:
    path: REPORTS/MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES_RESULT.md
    status: PASS
    sha256: 58c3fca1a5fe482d248e15eb0ae87f62ae7fb8d80abca8feda17152291b23508
  requires_installed_task:
    path: TASKS/MAP11_08_CREATE_CLUSTER_PREVIEW_AND_PLAYMODE_FIXTURES.md
    sha256: fe790c7380326e7b3b9a02d1332b7ad3ab3233af045485d0e552f44b22990e30
  sets_current_task: MAP11_09_MAP11_CLUSTER_EXIT_TESTS
```

# MAP11_09 — MAP11 TerrainCluster Exit Tests

```text
TASK: MAP11_09_MAP11_CLUSTER_EXIT_TESTS
PHASE: MAP11 — TerrainCluster Authoring / Compilation
STATUS: CURRENT
NEXT: MAP12_01_IMPLEMENT_ACTIVITY_SHELL_AND_SLOT_COMPILER
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. User-Facing Implementation Report Requirement

이번 Task는 새 TerrainCluster 기능을 더 만들지 않는다. MAP11_01~08이 만든 현재 코드와 16개 TerrainCluster가 서로 맞물려 실제 Phase 계약을 충족하는지 전용 통합 테스트 하나로 승인하거나 차단한다.

Result의 첫 섹션은 반드시 한국어 `## User-Facing Implementation Report`로 작성하고 다음을 구체적으로 보고한다.

1. 새로 추가하거나 수정한 스크립트의 정확한 경로
2. 각 스크립트가 맡은 책임
3. 이 Task로 새로 가능해진 것
4. 전체 파이프라인에서의 위치
5. 아직 구현하지 않은 것
6. Unity Editor 또는 실제 게임 화면에서 보이는지 여부

그다음 섹션은 반드시 `## Responsibility and Added Functions`로 작성한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| MAP11 current code/data 전용 Phase Exit test | 새 production compiler/solver/renderer |
| 16종×2 variant 통합 결정론·경로·복귀 승인 | 기존 MAP11 C#/CSV repair |
| Static Shell의 Activity/Event 비의존성 증명 | MAP12 ActivityStructure/EventOverlay 구현 |
| raw density·pattern protection·sector-fit 승인 | biome density 수치 튜닝 |
| current artifact drift와 side effect 검사 | legacy 19347 또는 과거 category 재실행 |

검증 흐름:

```text
13 physical TerrainCluster CSVs / immutable catalog
→ footprint + role/socket
→ spine + traversal envelope + absolute protection
→ baseline/high/recovery Static Shell
→ diagnostic MicroPattern A/B
→ preview snapshot / 48×32 sector frame evidence
→ MAP11 Phase Exit verdict
```

Exit test는 기존 public authority를 호출한다. CSV parser, graph traversal, pattern renderer, structural signature 또는 preview model을 test 안에 다시 구현하지 않는다.

## 2. No-Broad-Regression Policy

정상 실행은 category `MAP11_09`만 선택한다.

```text
MAP11_09 dedicated EditMode integration selection: required
MAP09/MAP10 selections: 0
MAP11_01~08 selections: 0
legacy 19347 selections: 0
PlayMode selections: 0
unfiltered test selections: 0
```

MAP11_09 test가 기존 public API를 호출하는 것은 과거 category 재실행이 아니다. MAP11_08에서 승인한 PlayMode graybox `4/4 PASS`는 이번 production/data 변경이 없으므로 다시 실행하지 않는다.

실제 current artifact 문제를 발견한 경우에만:

1. 실패 invariant, 소유 Task, 원인을 Result에 기록한다.
2. 필요한 최소 owner verification 범위를 제안한다.
3. 이전 production C#/CSV를 이 Task에서 수정하지 않는다.
4. 관련 없는 회귀나 전체 suite를 실행하지 않는다.
5. `STATUS: BLOCKED`, `MAP11 PHASE EXIT: NOT APPROVED`로 STOP한다.

Task-owned test의 compile/assertion 실수는 신규 test 파일만 고치고 `MAP11_09`만 재실행할 수 있다.

## 3. Read-Only Preflight Authority

쓰기 전에 exact 확인한다.

```text
MAP11_08 Result: PASS
MAP11_08 Result SHA-256:
58c3fca1a5fe482d248e15eb0ae87f62ae7fb8d80abca8feda17152291b23508

MAP11_08 installed Task SHA-256:
fe790c7380326e7b3b9a02d1332b7ad3ab3233af045485d0e552f44b22990e30

MAP11_08: COMPLETE
MAP11_09: CURRENT
MAP12_01: LOCKED
unrelated staged path: 0
Unity compile / relevant Console error: 0 / 0
```

Current content authority:

```text
Authoring CSV/meta: 65 / 65
TerrainCluster CSV/meta: 13 / 13
Generated CSV: 0
schema: 24 tables / 143 columns / 44 FK
TerrainCluster schema: 13 tables / 89 columns
catalog entries: 16
variants / baselines: 32 / 16
biome × pacing: 4 × 4
footprint sizes 2/3/4/5 chunks: 4/4/4/4
Quiet exact candidates: 4
structural signatures / duplicates: 16 / 0
```

Current approved digests:

```text
TerrainCluster catalog:
9d26786af477731d57503f16cc899210da6636f48dfb0542791e8fa591bd3bf7

Structural signature set:
2884a639d9cef923e8b86a7fba2c0430cdfad2de11a63fd138d51dacdce13d8a

Full Authoring manifest:
ff4761537986a4c9433775359d9b62ad806914ef30462a320c97b355126a5b6c
```

Baseline drift가 있으면 신규 test를 만들기 전에 `BLOCKED`로 보고한다. 승인 수치를 맞추기 위해 CSV나 production code를 자동 수정하지 않는다.

## 4. Exact Write Boundary

정상 경로에서 신규 focused Exit test와 meta만 허용한다.

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/TerrainClusters/Map11ClusterPhaseExitTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/TerrainClusters/Map11ClusterPhaseExitTests.cs.meta
```

사용할 기존 assembly/namespace:

```text
Assembly: MapAuthoring.Tests.EditMode
Namespace: StarNight.MapAuthoring.Tests.EditMode.WorldGeneration.TerrainClusters
Category: MAP11_09
```

수정 금지:

```text
existing C# / test / CSV / meta
asmdef / asmref
Authoring and Generated content
Scene / Prefab / ScriptableObject / Tilemap / Material / Texture
Settings / Packages
```

별도 audit production model, exporter, fixture asset 또는 report asset을 만들지 않는다. assembly reference가 부족하면 production logic을 복사하지 말고 `BLOCKED`로 보고한다.

## 5. Dedicated Exit Matrix

`Map11ClusterPhaseExitTests`는 current compiled code/data를 직접 읽고 최소 아래 case를 독립적으로 식별 가능하게 검증한다. 메서드 수는 코드 가독성에 맞게 조정할 수 있지만, Result에는 각 gate의 실제 PASS 증거를 따로 기록한다.

### A. Physical authority and import

- exact 13 physical paths에서만 atomic import한다.
- exact 16 catalog entries와 `13/89`, `24/143/44`를 확인한다.
- Authoring `65/65`, TerrainCluster `13/13`, Generated `0`을 확인한다.
- catalog/signature-set/full-manifest digest가 preflight와 exact 같다.
- missing/duplicate/invalid FK와 partial publication은 `0`이다.

### B. Deterministic compilation — all 16 × 2

모든 cluster와 정확히 두 SpineVariant를 current public compiler chain으로 compile한다.

- canonical input과 reverse-enumerated input의 artifact/digest가 같다.
- invariant culture와 `tr-TR` culture의 artifact/digest가 같다.
- 동일 입력 반복 compile이 byte/semantic-equivalent 결과를 낸다.
- RNG/seed draw와 retry는 `0`이다.
- variants `32`, baseline variant exact `16`이다.
- structural signatures `16`, duplicate `0`이다.

### C. Footprint, canvas and sector fit

- active chunk count 분포는 `2/3/4/5 = 4/4/4/4`이다.
- 모든 footprint chunk coordinate는 unique하고 connected다.
- 모든 chunk bounding box는 최대 `4×4`다.
- tile bounds는 최대 `48×32`이며 fixed sector frame `[0..47]×[0..31]`에 translation 가능하다.
- local/sector 변환 round-trip과 Entry/Exit/anchor 좌표가 보존된다.
- canvas active-cell coverage에 gap/extra/partial publication이 없다.

Recovery cluster의 approved shapes는 exact 확인한다.

```text
MoonCrater Recovery: (0,0),(1,0),(2,0),(2,1),(3,1) → 4×2 / 48×16
CassiaRoot Recovery: (0,1),(1,0),(1,1),(1,2),(2,1) → 3×3 / 36×24
AbandonedMill Recovery: (0,2),(1,0),(1,1),(1,2),(2,0) → 3×3 / 36×24
MoonDough Recovery: (0,0),(0,1),(1,1),(1,2),(2,2) → 3×3 / 36×24
```

### D. Baseline, high and recovery routes

MAP11_03/04가 publish한 graph와 witness를 사용한다. test 안에 별도 BFS/경로 solver를 만들지 않는다.

모든 `16×2`에서:

- primary Entry에서 Exit까지 baseline witness가 source spine edge로만 이어진다.
- 각 witness step의 MovementKind, clearance, landing과 envelope provenance가 source 계약과 같다.
- high route는 baseline에서 분기하고 high point/benefit을 지나 baseline 또는 Exit에 재결합한다.
- high failure node는 exact recovery witness로 baseline 안전 지점에 도달한다.
- recovery 예상 시간은 `2000..5000 ms`이고 teleport/synthetic edge는 `0`이다.
- orphan node/anchor, out-of-envelope step, missing landing/rejoin은 `0`이다.

### E. Static Shell and Activity/Event removal boundary

여기서 `event removal`은 MAP12를 미리 구현하는 작업이 아니다.

- TerrainCluster 13-table catalog/compile 입력에는 ActivityStructure 또는 EventOverlay instance/FK가 필요하지 않다.
- Activity/Event object, prefab, marker assignment가 없는 상태로도 16×2 Static Shell compile과 baseline/recovery path가 성공한다.
- optional marker/presentation overlay가 current public boundary에 존재하면 test-owned in-memory empty input으로 제거하고 static structural digest와 route witness가 변하지 않는지 확인한다.
- 그런 optional input 표면이 아직 없다면, MAP11 compile API가 이를 요구하지 않는다는 type/input-dependency evidence로 승인하고 가짜 Event type을 만들지 않는다.
- Event removal을 위해 타일을 carve하거나 synthetic route를 추가하지 않는다.

Result에는 어떤 방식으로 비의존성을 증명했는지 `absent input` 또는 `empty optional input`으로 명시한다.

### F. Pattern-free shell, Pattern A/B and protection

- PatternFree snapshots는 exact `16×2 = 32`가 성공한다.
- PatternFree에서도 baseline/recovery witness가 성립한다.
- 대표 네 biome의 diagnostic Pattern A/B diff는 모두 non-empty다.

```text
MoonCrater: BOWL / ROCK_SHELF
CassiaRoot: ARCH / HOLLOW_POCKET
AbandonedMill: BROKEN_PILLAR / ORTHOGONAL_CARVE
MoonDough: BOUNCE_CUP / STICKY_SHELF
```

- A/B 적용 후 protected write/change는 `0/0`이다.
- Route Spine, Traversal Envelope, landing, recovery, Entry/Exit와 AbsoluteProtected가 보존된다.
- 패턴이 실패한 필수 경로를 임의 carve로 보정하지 않는다.

### G. Raw density evidence — no tuning

MAP10 biome profile의 density policy는 계속 `Uncalibrated`다. 숫자 threshold나 biome 튜닝값을 새로 만들지 않는다.

각 PatternFree와 네 대표 A/B snapshot에서 다음 raw integer invariant만 승인한다.

- active, solid, air, protected, pattern-target, changed count가 음수가 아니다.
- solid + air와 compiler가 정의한 covered state 합계가 active coverage와 exact 일치한다.
- per-chunk count 합계가 cluster total과 exact 일치한다.
- changed count는 active bounds 안이고 A/B representative는 non-zero다.
- protected changed count는 `0`이다.
- 같은 snapshot의 density evidence/digest는 반복과 culture change에서 같다.

Result에 raw count/range를 보고하되 이를 gameplay balance 승인으로 표현하지 않는다.

### H. Quiet pool and preview consistency

- Quiet cluster는 exact 네 biome에 하나씩이고 reward/strong activity/event count는 `0`이다.
- biome/use query 결과 exact one, selection draw/retry `0`이다.
- MAP11_08 preview model의 32 PatternFree와 네 A/B snapshot이 같은 compiler artifact/digest/provenance를 표시한다.
- preview는 read-only이며 Authoring/Generated/Scene/Prefab/Tilemap mutation이 `0`이다.

## 6. Forbidden-Failure Fixtures

physical CSV를 수정하지 않고 test-owned in-memory input으로 최소 아래 failure를 확인한다.

```text
duplicate cluster ID or footprint coordinate → atomic failure / catalog 0
5×1 and 1×5 footprint → InvalidFootprint / artifact 0
missing route source edge or recovery witness → compile failure / artifact 0
protected pattern write → reject or ForceNoChange contract / protected change 0
```

Failure test는 기존 validation API에 직접 입력한다. production/content defect를 감추기 위한 fallback이나 fixture-only solver를 만들지 않는다.

## 7. Focused Verification and Static Gates

Unity refresh/compile 후 category `MAP11_09` EditMode만 실행한다.

```text
MAP11_09 discovered = executed
pass = executed
fail / skip / inconclusive = 0 / 0 / 0
compile / Console relevant error = 0 / 0
prior category selections = 0
legacy selections = 0
PlayMode selections = 0
unfiltered selections = 0
```

Static scope:

```text
new Map11ClusterPhaseExitTests.cs/meta: exact 1/1
existing C#/test/CSV/meta modifications: 0
Authoring/Generated content modifications: 0
asmdef/Scene/Prefab/Settings/Packages modifications: 0
current three digests unchanged
duplicate GUID: 0
unapplied candidate/diff-check/unrelated staged: 0/0/0
Git push: NOT PERFORMED
```

Test Runner initialization timeout으로 executed 0이면 PASS로 세지 않는다. filter가 올바른지 확인해 재시도하고, 계속 실행 불가하면 `BLOCKED`로 보고한다.

## 8. Required Result and User-Visible Reporting

Result 경로:

```text
MapDesign/MCP/REPORTS/MAP11_09_MAP11_CLUSTER_EXIT_TESTS_RESULT.md
```

상단 verdict:

```text
TASK: MAP11_09_MAP11_CLUSTER_EXIT_TESTS
STATUS: PASS | BLOCKED
MAP11 PHASE EXIT: APPROVED | NOT APPROVED
MAP11_09: COMPLETE ELIGIBLE | NOT COMPLETE
MAP12_01_IMPLEMENT_ACTIVITY_SHELL_AND_SLOT_COMPILER: LOCKED / DO NOT START
```

첫 섹션은 한국어로 다음 표를 채운다.

```text
## User-Facing Implementation Report

추가/수정 스크립트:
- exact path와 신규/수정 여부

스크립트 책임:
- class/test별 실제 책임

이번에 새로 가능해진 것:
- MAP11 Phase를 어떤 증거로 승인/차단할 수 있게 되었는지

파이프라인 위치:
- 13 CSV → catalog → compiler → route/pattern/preview → Exit verdict

아직 미구현:
- MAP12 Activity/Event, production Sector placement, game Tilemap/physics 등

Editor/게임 가시성:
- 신규 gameplay 화면 0
- 기존 TerrainCluster Preview 메뉴 유지 여부
- 신규 test가 Test Runner에서만 보이는지
```

그다음 `## Responsibility and Added Functions`에서 아래를 보고한다.

| Field | Required evidence |
|---|---|
| Task responsibility | MAP11 current-code/data Phase Exit 판정 |
| Added functions | 신규 test class와 각 gate; production 기능 추가 0 |
| Inputs consumed | MAP11_01~08 current public authorities와 physical content |
| Outputs produced | determinism/reachability/recovery/removal/density/sector-fit verdict |
| Explicit non-ownership | repair, MAP12, production world/Tilemap/physics 미구현 |
| Downstream consumer | 별도 검수 후 MAP12_01만 unlock 가능 |

이후 아래 actual evidence를 생략 없이 기록한다.

- added/modified file manifest와 책임
- preflight inventory와 세 digest
- 16×2 compile/determinism/culture matrix
- footprint 분포와 sector-fit, 네 Recovery shapes
- baseline/high/recovery witness totals와 duration range
- Activity/Event absence 방식과 Static Shell 결과
- PatternFree 32 및 대표 A/B diff/protected totals
- raw density count/range와 `Uncalibrated` 유지
- Quiet/preview consistency
- negative fixture와 atomic failure 결과
- focused test discovered/executed/pass/fail/skip
- regression selections 0 또는 실제 trigger owner/reason/minimum scope
- static/change scope와 unrelated staged 0
- commit handoff

정상 경로 문구:

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY TEST SELECTIONS: 0
PLAYMODE SELECTIONS: 0
```

PASS일 때만 MAP11 Phase Exit을 `APPROVED`로 기록하고 Status Finalize 후 task-owned test/meta/protocol 파일만 atomic commit한다.

```text
Subject: MAP11_09: approve TerrainCluster phase exit
Push: NOT PERFORMED
```

Result가 PASS여도 MAP12_01을 자동 시작하지 않는다. 사용자가 Result를 전달하고 별도 검수받을 때까지 계속 LOCKED다.
