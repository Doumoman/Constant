TASK: MAP12_03_IMPLEMENT_ACTIVITY_COMPATIBILITY_FREQUENCY_AND_CAPS
STATUS: PASS
MAP12_03: COMPLETE ELIGIBLE
MAP12_04_IMPLEMENT_EVENT_OVERLAY_ASSIGNMENT_RULES: LOCKED / DO NOT START

## User-Facing Implementation Report

MAP12_03은 검증된 Activity 프로필과 TerrainCluster placement opportunity를 연결하는 compatibility index와, World → BiomePatch → Sector 순서의 계층형 빈도 예산 및 Strong cap 적용 placement plan을 구현했다. 결과는 계획 데이터만 게시하며 Canvas, geometry, Prefab, Scene, Tilemap을 쓰거나 변경하지 않는다.

- `ActivityCompatibility.cs`: Activity 강도, placement 프로필, sector opportunity, 명시적 clearance rectangle/Air/reservation/AbsoluteProtected 증거, 안정적인 rejection 모델을 소유한다.
- `ActivityCandidateIndex.cs`: MAP11/MAP12 digest identity, `BiomePatchSnapshot` sector/patch/biome ownership, biome/pacing/access/chunk/clearance 계약을 검증하고 immutable candidate/rejection index를 canonical digest와 함께 게시한다. duplicate candidate key는 모든 source를 제외하고 atomic failure로 처리한다.
- `ActivityFrequencyPlanner.cs`: 60..120 permille 정책, round-half-up 및 strict integer band, World→Patch→Sector largest-remainder 배분, `RNG_SECTOR_RECIPE` 기반 priority/weighted draw, World/Patch/Sector Strong hard cap과 atomic unsatisfied failure를 구현한다.
- `ActivityCompatibilityFrequencyTests.cs`: compatibility, rejection, clearance, duplicate, canonical/culture 안정성, 빈도 경계, 계층 예산, weighted determinism, Strong cap, RNG isolation, no-mutation을 검증하는 MAP12_03 전용 EditMode test 13개를 제공한다.

실제 100-opportunity fixture에서 World target/selected는 `8/8` (`8/100 = 80 permille`), Patch budget/selected는 `4/50 + 4/50`, Sector selected 합계는 `8`이었다. cap `World/Patch/Sector = 0/0/0`에서 Strong selected/counter는 모두 `0`이었고 Ordinary fallback으로 target을 정확히 채웠다. weighted fixture는 sector stream 100개, priority draw 100회와 selected-position weighted draw 8회로 총 108 draw를 게시했다.

## Responsibility and Added Functions

### Inputs

- validated Activity/cluster/variant identity와 Activity, shell, removal-safety SHA-256 digest
- allowed `MoonpalaceBiomeId`, `PacingRole`, `AccessClass`, active chunk 범위, clearance 요구 크기, 정수 weight, Ordinary/Strong 분류
- stable opportunity ID, `SectorCoord`, `BiomePatchId`, `BiomePatchSnapshot` ownership, MAP11 catalog/signature/authoring digest, working Canvas Air 및 protection/reservation 증거
- target permille, 3-scope Strong cap, exact world seed, non-negative attempt ordinal, 기존 `DeterministicRngStreamFactory`

### Outputs

- canonical immutable `ActivityCandidateIndex`와 stable `ActivityCompatibilityRejection`
- exact rational evidence를 포함한 World/Patch/Sector `ActivityScopeBudget`
- candidate key, scope, weight/total/ticket, priority, draw before/after, 3-scope Strong counter before/after를 포함한 `ActivityPlacementDecision`
- atomic `ActivityFrequencyPlan` 또는 stable-sorted `ActivityCompatibilityError`

### Non-Ownership

- 실제 TerrainCluster/Sector Canvas placement, slot/Prefab 생성, Tilemap/Scene/geometry mutation을 수행하지 않는다.
- starter Activity content/CSV, EventOverlay assignment/cooldown/별도 RNG stream, gameplay state machine을 소유하지 않는다.
- RNG registry, pass catalog, asmdef, 기존 MAP11/MAP12 artifact/source를 수정하지 않는다.

### Downstream

- MAP12_04는 이 plan의 선택된 opportunity/activity identity와 deterministic evidence를 EventOverlay assignment 입력으로 사용할 수 있다.
- MAP12_04는 별도 검토 전까지 계속 LOCKED이며 이번 실행에서 시작하지 않았다.

## Compatibility and Clearance Evidence

| Evidence | PASS observation |
|---|---|
| representative chain | `TC_CRATER_BOWL_ASCENT / SPINE_CRATER_BOWL_ASCENT_BASE` |
| MAP11 catalog / signature / authoring | exact required SHA-256 identity verified |
| MAP12 shell / removal proof | exact required SHA-256 identity verified |
| ownership | SectorCoord + BiomePatchId + primary biome matched `BiomePatchSnapshot` |
| eligible fixture | profiles/opportunities/candidates/rejections = `1/1/1/0` |
| mismatch fixture | profiles/opportunities/candidates/rejections = `9/1/1/8` |
| clearance fixture | opportunities/candidates/rejections = `2/1/4` |
| duplicate fixture | duplicate key sources all excluded; zero index publication; `DuplicateCandidate + EmptyCandidateIndex` |

Clearance 검증은 caller가 제공한 rectangle만 검사했다. 좌표 수/unique/bounds, final working Canvas Air, Device/Hazard/Projectile reservation 비중첩, AbsoluteProtected 비중첩을 각각 증명하며 rectangle search 또는 packing은 수행하지 않았다.

## Frequency, Caps, and RNG Evidence

| Scope | Eligible | Target | Selected | Rate | Ordinary | Strong |
|---|---:|---:|---:|---:|---:|---:|
| World | 100 | 8 | 8 | 80 permille | 8 | 0 |
| PATCH_A | 50 | 4 | 4 | 80 permille | 4 | 0 |
| PATCH_B | 50 | 4 | 4 | 80 permille | 4 | 0 |
| Sector total | 100×1 | 8 | 8 | discrete 0/1 | 8 | 0 |

- 60/120 permille inclusive PASS; 59/121은 stream/draw `0/0`에서 atomic rejection.
- World/Patch/Sector target sum은 각각 8로 exact equality.
- 작은 1-opportunity Sector는 feasible integer 6..12% band가 없음을 `BandFeasible=false`, `DiscreteApproximation=true`로 게시.
- 같은 index/seed/sector/attempt는 동일 plan digest/decision/ticket/draw evidence를 재현.
- seed 또는 attempt 한 필드 변화는 canonical plan digest를 변경.
- 기존 `RNG_SECTOR_RECIPE`만 사용; unrelated stream의 동일 첫 draw는 invalid plan 전후 동일.
- cap 0/0/0에서 Strong은 allowed set에서 제외되고 Ordinary가 선택되며 모든 before/after counter가 0.
- Strong-only + cap 0 target은 cap을 넘기지 않고 `StrongCapUnsatisfiable` atomic failure.

## Negative Atomic Matrix

| Case | Publication | RNG evidence |
|---|---|---|
| null/empty candidate index | plan/digest 0 | streams/draws 0/0 |
| TargetPermille 59 or 121 | plan/digest 0 | streams/draws 0/0 |
| duplicate candidate key | index/digest 0 | streams/draws 0/0 |
| incompatible biome/pacing/access/chunks/identity/digest | stable rejection, compatible candidates preserved | index RNG 0/0 |
| invalid clearance Air/rectangle/reservation/protection | stable rejection, compatible candidates preserved | index RNG 0/0 |
| Strong-only target over caps | plan/digest 0 | consumed evidence retained only on failure result |

## Focused Validation

```text
Unity compile / Console error / warning: 0 / 0 / 0
MAP12_03 EditMode discovered / executed / passed: 13 / 13 / 13
failed / skipped / inconclusive: 0 / 0 / 0
final focused job: 3cfd49c8e99e43ac8238ceaae12c586d
initial import/runner retries: same MAP12_03 category only; no PASS claimed for executed 0

REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY TEST SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

## Static and Change Scope

```text
new Runtime C#/meta: 3 / 3
new focused test C#/meta: 1 / 1
existing C#/test/CSV/meta changes: 0
RNG registry/pass catalog changes: 0 / 0
Authoring/Generated changes: 0 / 0
asmdef/Scene/Prefab/Tilemap/Settings/Packages changes: 0
MAP11/MAP12_01~02 artifact/source modifications: 0
duplicate GUID: 0
installed/archive Task SHA-256: 36956a1f8fb339a0dd52d8e98d5875d9c2505da7a5c923b08f75e17c520ded89
installed/archive byte-identical: YES
inbox/diff-check/unrelated staged: 0 / 0 / 0
pre-existing unrelated untracked meta files preserved and excluded: 3
Git push: NOT PERFORMED
```

Atomic commit subject: `MAP12_03: implement activity compatibility frequency and caps`.
