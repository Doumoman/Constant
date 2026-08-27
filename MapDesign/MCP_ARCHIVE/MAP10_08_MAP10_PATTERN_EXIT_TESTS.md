```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP10_08_MAP10_PATTERN_EXIT_TESTS
  task_file: TASKS/MAP10_08_MAP10_PATTERN_EXIT_TESTS.md
  requires_current_task: NONE
  requires_completed_task: MAP10_07_CREATE_PATTERN_PREVIEW_AND_FOCUSED_TESTS
  requires_result:
    path: REPORTS/MAP10_07_CREATE_PATTERN_PREVIEW_AND_FOCUSED_TESTS_RESULT.md
    status: PASS
    sha256: a2bc48060053c2808f0c1745cae34d2d6a2321b1bae71ffd5b71ddb0e2abc25d
  requires_installed_task:
    path: TASKS/MAP10_07_CREATE_PATTERN_PREVIEW_AND_FOCUSED_TESTS.md
    sha256: 669e9a956ad632e55cfc835b308d18dda633593aff61862d0c8802c2410a5808
  sets_current_task: MAP10_08_MAP10_PATTERN_EXIT_TESTS
```

# MAP10_08 — MAP10 Pattern Exit Tests

```text
TASK: MAP10_08_MAP10_PATTERN_EXIT_TESTS
PHASE: MAP10 — 4×4 MicroPattern Authoring / Rendering
STATUS: CURRENT
NEXT: MAP11_01_IMPLEMENT_CLUSTER_FOOTPRINT_AND_LOCAL_CANVAS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Responsibility

이번 Task는 MAP10_01~07의 published authority를 변경 없이 연결해 **MicroPattern Phase Exit을 승인하거나 차단**한다.

```text
physical import / canonical row projection
→ exact 16-cell contract
→ 56 transforms and protection
→ candidate/RNG determinism
→ ordered render/conflict
→ signature/repetition/cleanup
→ read-only preview evidence
→ MAP10 Exit verdict
```

| 소유 | 소유하지 않음 |
|---|---|
| MAP10 전용 integration Exit tests | 새 production 기능/repair |
| phase evidence와 PASS/BLOCKED 판정 | 이전 category·legacy 재실행 |
| import/projection/determinism/protection 승인 | MAP11 Cluster 구현 |
| static drift/side-effect audit | Tilemap/Scene/Prefab/Generated |

## 1. No-Regression Exit Policy

정상 실행은 category `MAP10_08`만 선택한다.

```text
MAP10_08 dedicated integration selection: required
Prior MAP10_01~07 test selections: 0
Legacy 19347 selections: 0
PlayMode selections: 0
```

Phase Exit이라는 이유로 이전 category나 legacy를 반복 실행하지 않는다. MAP10_08 integration이 실제 production/data defect를 발견하면:

1. failing invariant와 owner Task를 기록한다.
2. MAP10_08 파일 외 prior production/data를 수정하지 않는다.
3. 관련 없는 regression을 실행하지 않는다.
4. `STATUS: BLOCKED`, MAP10 Exit 미승인으로 STOP한다.
5. 별도 repair Task 검수 전 MAP11을 열지 않는다.

Task-owned test/assertion/fixture 결함이면 그 파일만 고치고 MAP10_08만 재실행할 수 있다.

## 2. Read-Only Authorities

Preflight에서 exact 확인:

```text
MAP10_01 physical importer/schema/immutable catalog
MAP10_02 transform and protected application plan
MAP10_03 ordered renderer/atomic conflict
MAP10_04 four-biome profile/candidate index/RNG selection
MAP10_05 silhouette/repetition/local cleanup
MAP10_06 starter content
MAP10_07 preview model/window evidence
```

Current content authority:

```text
Definitions / physical cell rows: 24 / 453
Biomes: 6 / 6 / 6 / 6
Role groups: 12 Geometry / 4 SurfaceAffordance / 8 Detail
Allowed pattern-transform pairs: 56
Payload tokens: 24

AddSolid / CarveAir / Geometry NoChange: 54 / 41 / 289
All non-NoChange instructions: 164

Catalog digest:
6a5aefd2eb368348d594158cc3f14e94d0ea509ea2cdd207a7715e8da80d19ac
Catalog CSV SHA-256:
f9d9e9cc60c4e4d7561c5aa6502228c18fc9566e3e0febab206ea3264b408267
Cells CSV SHA-256:
e702ae5d02d7ec9d2cda129c1361699e37d942c280c8f9bd1f3200f155084381
Full 52-file Authoring manifest:
4415ae4af5196d6793f5d0152c0688e5bf35dc4ad23442791e45d3cfd81d0851
Generated CSV: 0
```

## 3. Exact Write Boundary

신규 focused Exit test만 허용:

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/MicroPatterns/MicroPatternPhaseExitTests.cs(.meta)
```

existing production/test/CSV/meta/asmdef는 수정하지 않는다. 별도 audit production model, exporter, asset, scene fixture, generated report 파일을 만들지 않는다.

## 4. Import and In-Memory Export Projection Gate

physical two-file importer는 exact `24/453`을 atomic publish해야 한다.

export 증거는 production file writer가 아니라 **test-owned in-memory canonical row projection**이다.

1. imported immutable definitions에서 catalog rows를 pattern ID ordinal로 투영한다.
2. 각 pattern의 Geometry 16 rows와 non-NoChange non-Geometry rows만 투영한다.
3. 결과는 exact `24 catalog / 453 cell rows`여야 한다.
4. existing MAP10_01 builder로 projected rows를 다시 build한다.
5. rebuilt definition/catalog semantic digest가 physical import와 exact 같아야 한다.
6. input definition/published CSV를 수정하거나 파일을 쓰지 않는다.

이는 schema lossless round-trip proof이며 production CSV exporter 구현을 의미하지 않는다.

## 5. Exact Contract Gates

### A. Cell and content

- 24 definitions 모두 unique 4×4 좌표 `16`, normalized layer `6`.
- missing/duplicate/out-of-range/layer mismatch/payload invalid `0`.
- four biomes `6/6/6/6`, role groups `12/4/8`.
- exact operation/payload totals와 Pattern별 transform mass `1000`.

### B. Transform

- allowed pairs `56/56`이 MAP10_02 actual transform으로 성공한다.
- transformed coordinates는 exact 16 unique/in-bounds다.
- MirrorX/MirrorY/R180 involution과 R0 identity를 available pair에서 검증한다.
- unsupported transform fallback `0`.

### C. Protected write zero

각 pattern의 first canonical non-NoChange target을 `TraversalEnvelope`로 보호한다.

```text
RejectCandidate definitions: 12/12 rejected; renderer publication 0
ForceNoChange definitions: 12/12 successful; protected target write 0
Protected source/provenance loss: 0
```

### D. Candidate and deterministic RNG

- profile catalog exact four, density policy `Uncalibrated` 유지.
- biome별 candidate pool은 exact six Pattern IDs를 가진다.
- candidate input reversal은 같은 index/digest다.
- Pattern ID별 allowed-transform weight sum은 `1000`.
- same world seed/sector/attempt/index는 same decision/digest다.
- seed/sector/attempt one-field change evidence와 other-stream independence를 확인한다.
- invalid/empty candidate batch는 no decision/no draw다.

### E. Renderer and conflict

- all 56 Clean OperationWitness fixtures가 실제 ordered renderer를 통과한다.
- write stages exact `10/20/30/40/50/60`이며 모든 write가 before/after diff에 존재한다.
- cross-layer implicit clear `0`.
- fixed Material conflict fixture는 atomic reject, partial delta/digest `0/0`.

### F. Signature, repetition, cleanup

- Geometry 12개 non-zero pairwise-distinct signature.
- non-Geometry 12개 explicit zero signature.
- mirror-equivalent effective geometry signature equality.
- same Pattern ID third-repeat filtering은 RNG 전에 적용되고 reroll/draw discard `0`.
- solid speck, air pinhole, head snag, boxed-bottom pit exact rules가 immutable delta를 낸다.
- protected cleanup write `0`, missing halo/cascade/global reachability inference `0`.

### G. Preview and side effects

- MAP10_07 model이 exact 24 IDs/56 Clean snapshots와 three fixtures를 read-only publish한다.
- five 4×4 panels, stage/diff/digest/error evidence가 완전하다.
- Authoring/Generated/asset/Scene/Prefab/SO/Tilemap mutation `0`.

## 6. Dedicated Focused Verification

category `MAP10_08`에 최소 아래 integration cases를 둔다.

1. physical authority hashes/inventory/import
2. in-memory canonical row projection round-trip
3. exact 16-cell/layer/content totals
4. all 56 transforms/involution
5. all 24 protected-overlap outcomes/write zero
6. four-biome candidate mass/index determinism
7. deterministic RNG repeatability/no-draw failure
8. all 56 ordered render/diff and layer order
9. atomic same-layer conflict
10. signature/repetition integration
11. local cleanup/protection/no-cascade
12. preview evidence and forbidden side effects

MAP10_01~07 Result의 PASS 문구만 복사해서 승인하지 않는다. 위 test가 compiled current code/data를 직접 통합 검증해야 한다.

## 7. Static Exit Gates

```text
Unity compile / Console error / relevant warning: 0 / 0 / 0
MAP10_08 focused: all discovered executed and PASS; skip/inconclusive 0
MicroPattern CSV hashes, 24/453 rows, catalog digest exact
full Authoring manifest 4415ae... unchanged
Generated CSV: 0
existing MAP00~10_07 production/test/CSV/meta modifications: 0
other roots/asmdef/Scene/Prefab/Settings/Packages changes: 0
new test/meta valid; duplicate GUID 0
unapplied candidate/diff-check/unrelated staged: 0/0/0
```

## 8. Required Result and Exit Verdict

```text
MAP10_08_MAP10_PATTERN_EXIT_TESTS_RESULT.md
```

상단:

```text
TASK: MAP10_08_MAP10_PATTERN_EXIT_TESTS
STATUS: PASS | BLOCKED
MAP10 PHASE EXIT: APPROVED | NOT APPROVED
MAP10_08: COMPLETE ELIGIBLE | NOT COMPLETE
MAP11_01_IMPLEMENT_CLUSTER_FOOTPRINT_AND_LOCAL_CANVAS: LOCKED / DO NOT START
```

첫 구현 섹션은 반드시 `Responsibility and Added Functions`다.

| Field | Required report |
|---|---|
| Task responsibility | MAP10 current-code/data integration Exit 판정 |
| Added functions | dedicated Exit test와 실제 검증 범위; production 기능 추가 0 |
| Inputs consumed | MAP10_01~07 published authorities/content/preview |
| Outputs produced | import/projection/transform/protection/determinism/render/cleanup verdict |
| Explicit non-ownership | repair/MAP11/Tilemap/Scene/Generated 미구현 |
| Downstream consumers | 별도 검수 후 MAP11_01만 unlock 가능 |

이후 Exit matrix, physical/in-memory round-trip, exact content, transform/protection, candidate/RNG, render/conflict, signature/repetition/cleanup, preview/side effects, focused/regression policy, static/change scope, commit handoff를 기록한다.

```text
MAP10_08 focused: discovered/executed/pass/fail/skip
REGRESSION TRIGGER DETECTED: NO | YES(owner/reason/minimum scope)
PRIOR TASK TEST SELECTIONS: 0 (normal path)
LEGACY TEST SELECTIONS: 0 (normal path)
```

PASS일 때만 MAP10 Phase Exit을 APPROVED로 기록하고 Status Finalize 후 task-owned test/meta/protocol 파일만 atomic commit한다.

```text
Subject: MAP10_08: approve MicroPattern phase exit
Push: NOT PERFORMED
```

Result가 PASS여도 MAP11_01을 자동 시작하지 않는다. 사용자가 Result를 전달하고 별도 검수받을 때까지 계속 LOCKED다.
