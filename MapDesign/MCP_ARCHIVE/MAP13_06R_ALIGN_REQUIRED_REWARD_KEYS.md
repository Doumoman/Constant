```yaml
mcp_repair:
  format: current_task_repair_v1
  repair_id: MAP13_06R_ALIGN_REQUIRED_REWARD_KEYS
  repairs_current_task: MAP13_06_AUTHOR_THREE_CORE_RESOURCE_REGIONS
  requires_current_task: MAP13_06_AUTHOR_THREE_CORE_RESOURCE_REGIONS
  requires_blocked_result:
    path: REPORTS/MAP13_06_AUTHOR_THREE_CORE_RESOURCE_REGIONS_RESULT.md
    status: BLOCKED
    sha256: 39faac2bac212de87405d944427bb0ce4c514a2544c48b0aaf84c10976d1c296
  requires_installed_task:
    path: TASKS/MAP13_06_AUTHOR_THREE_CORE_RESOURCE_REGIONS.md
    sha256: ec7a880f0239819025b9df6f3b9021143523721003c24b026f2e9dce6054ccbb
  preserves_current_task: MAP13_06_AUTHOR_THREE_CORE_RESOURCE_REGIONS
  next_task_remains_locked: MAP13_07_AUTHOR_FORGE_BOSS_AND_OPTIONAL_REGIONS
```

# MAP13_06R — Align Required Reward Keys

```text
REPAIR: MAP13_06R_ALIGN_REQUIRED_REWARD_KEYS
CURRENT TASK: MAP13_06_AUTHOR_THREE_CORE_RESOURCE_REGIONS
STATUS EFFECT: NONE — MAP13_06 stays CURRENT
NEXT: MAP13_07_AUTHOR_FORGE_BOSS_AND_OPTIONAL_REGIONS stays LOCKED
```

## 0. Repair Decision

MAP13_06은 구현 전에 original Task §7의 축약 persistence key와 기존 MAP09/MAP13_03 authority가 생성·강제하는 key가 일치하지 않아 정상 `BLOCKED`됐다.

잘못 지정된 축약 key:

```text
SR_STATE_MOON_CORE_REWARD
SR_STATE_CASSIA_SAP_REWARD
SR_STATE_STAR_NURUK_REWARD
```

현재 authority는 `SpecialPersistenceKey.ForSlot(regionId, Reward, slotId)`로 region identity, Reward scope, slot identity를 모두 결합한다. 기존 규칙은 key collision을 방지하고 MAP09 contract → MAP13_03 placed slot → persistence safety proof의 identity를 일관되게 보존한다.

따라서 기존 source를 바꾸거나 alias를 추가하지 않는다. 이 repair는 original MAP13_06 Task의 세 key만 현재 authoritative derived value로 정정하고 같은 Task를 재개한다.

## 1. Apply / Audit Procedure

이 파일은 새 Master Task가 아니다. normal `NONE → CURRENT` task-open flow를 실행하지 않는다.

Preflight에서 다음을 확인한다.

1. Current Task는 exact `MAP13_06_AUTHOR_THREE_CORE_RESOURCE_REGIONS`이고 계속 `CURRENT`다.
2. `MAP13_07_AUTHOR_FORGE_BOSS_AND_OPTIONAL_REGIONS`는 `LOCKED`다.
3. 현재 BLOCKED Result status/SHA가 metadata와 exact 일치한다.
4. installed original MAP13_06 Task SHA가 metadata와 exact 일치한다.
5. BLOCKED 실행에서 신규 Runtime/test/meta가 `0/0`, 기존 source/CSV/meta 수정이 `0`이다.
6. Status Finalize와 atomic commit이 수행되지 않았다.
7. 다른 unapplied inbox candidate와 unrelated staged path가 `0`이다.

이 repair를 byte-identical하게 다음 두 경로에 설치한다.

```text
MCP/TASKS/MAP13_06R_ALIGN_REQUIRED_REWARD_KEYS.md
MCP_ARCHIVE/MAP13_06R_ALIGN_REQUIRED_REWARD_KEYS.md
```

두 copy의 SHA가 inbox source와 일치한 뒤 inbox source를 이동/제거한다. repair 설치 중 Master와 Status를 변경하지 않는다. original MAP13_06 Task와 이 addendum이 합쳐져 effective specification이 된다.

state/SHA/path collision이 하나라도 맞지 않으면 project를 수정하지 않고 `BLOCKED`로 STOP한다.

## 2. Exact Supersession

Original MAP13_06 Task §7의 exact required Reward key 세 줄만 다음으로 supersede한다.

| Region | Required Reward slot | Authoritative key |
|---|---|---|
| `SR_MOON_CORE_SITE_5` | `SR_SLOT_MOON_CORE_REWARD` | `SR_STATE_MOON_CORE_SITE_5_REWARD_MOON_CORE_REWARD` |
| `SR_CASSIA_SAP_SITE_5` | `SR_SLOT_CASSIA_SAP_REWARD` | `SR_STATE_CASSIA_SAP_SITE_5_REWARD_CASSIA_SAP_REWARD` |
| `SR_STAR_NURUK_SITE_5` | `SR_SLOT_STAR_NURUK_REWARD` | `SR_STATE_STAR_NURUK_SITE_5_REWARD_STAR_NURUK_REWARD` |

각 value는 현재 public authority의 아래 호출 결과와 exact 같아야 한다.

```text
SpecialPersistenceKey.ForSlot(regionId, SpecialPersistenceScope.Reward, slotId)
```

Catalog/compiler/test가 별도 문자열 조합 규칙을 복제하지 않는다. public `ForSlot` 결과를 source identity로 사용하고, authored definition·MAP13_03 placed Reward slot·required-resource safety proof가 그 exact key를 함께 보존해야 한다.

금지:

- original installed Task 또는 historical BLOCKED Result rewrite
- `SpecialPersistenceKey.ForSlot` 변경
- MAP09/MAP13_01~05 production/test 수정
- short-key alias, mapping dictionary, fallback/surrogate key
- reflection/internal constructor로 invalid proof 합성
- persistence validation 완화 또는 assertion 제거
- CSV/schema/Prefab/Scene/Tilemap 변경

Original Task의 나머지 ID, 1×1/48×32, design canvas 36×16, active chunk, route, reward, write boundary와 focused-only 정책은 그대로 유지한다.

## 3. Resume MAP13_06

정정된 read-only preflight가 PASS하면 original MAP13_06 Task §4부터 재개한다.

허용되는 project 파일은 original allowlist와 동일하다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/CoreResourceRegionDefinitions.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/CoreResourceRegionCompiler.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/CoreResourceRegionStarterCatalog.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SpecialRegions/CoreResourceRegionAuthoringTests.cs(.meta)
```

세 region starter definition, low/high/recovery graph, `MandatoryNoTool`, exact required Reward/persistence proof와 atomic compiler를 original specification대로 구현한다.

Task-owned 실패는 위 신규 파일에서만 고치고 `MAP13_06` focused만 재실행한다. 기존 authority change가 다시 필요하면 범위를 넓히지 말고 `BLOCKED`로 보고한다.

## 4. Focused Verification

Unity refresh/compile 후 exact `MAP13_06` EditMode category만 실행한다.

```text
discovered = executed = passed
failed / skipped / inconclusive = 0 / 0 / 0
compile / relevant Console error = 0 / 0
```

다음은 선택하지 않는다.

```text
MAP09/MAP10/MAP11/MAP12/MAP13_01~05 categories
legacy 19347
PlayMode
unfiltered tests
```

이 repair는 specification value만 정정하며 기존 source를 변경하지 않으므로 prior owner regression을 실행할 이유가 없다. current public API는 `MAP13_06` focused test 안에서만 호출한다.

## 5. Required Result Rewrite

같은 Result 경로를 새 최종 결과로 rewrite한다.

```text
REPORTS/MAP13_06_AUTHOR_THREE_CORE_RESOURCE_REGIONS_RESULT.md
```

Header:

```text
TASK: MAP13_06_AUTHOR_THREE_CORE_RESOURCE_REGIONS
STATUS: PASS | BLOCKED
MAP13_06: COMPLETE ELIGIBLE | NOT COMPLETE
MAP13_07_AUTHOR_FORGE_BOSS_AND_OPTIONAL_REGIONS: LOCKED / DO NOT START
```

첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`를 유지한다.

Original Task의 모든 보고 항목에 더해 다음을 기록한다.

- original MAP13_06 Task SHA
- MAP13_06R repair SHA
- prior BLOCKED Result SHA
- 잘못된 short key 3개와 corrected authoritative key 3개
- `SpecialPersistenceKey.ForSlot` source identity exact 일치 결과
- 추가한 모든 script와 class/method별 책임·input→output
- region별 active chunk/node/edge/low/high/recovery/failure/reward 실제 수
- 새로 가능해진 기능과 파이프라인 위치
- physical CSV/device/physics/reward/save/Prefab/Tilemap 등 미구현 범위
- Editor/게임 가시성
- `MAP13_06` focused 수치와 prohibited selection `0`
- existing source/CSV/schema modifications `0`
- unrelated staged/included paths `0`

정상 selection 문구:

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

PASS일 때만 MAP13_06을 Finalize하고 original Task + repair + task-owned 신규 파일 + rewritten Result + exact Status fields만 atomic commit한다. Git push는 하지 않는다.

PASS여도 MAP13_07을 자동 시작하지 않고 STOP한다.
