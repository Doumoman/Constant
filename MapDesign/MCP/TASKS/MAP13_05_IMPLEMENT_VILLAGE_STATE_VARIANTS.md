```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP13_05_IMPLEMENT_VILLAGE_STATE_VARIANTS
  task_file: TASKS/MAP13_05_IMPLEMENT_VILLAGE_STATE_VARIANTS.md
  requires_current_task: NONE
  requires_completed_task: MAP13_04_IMPLEMENT_VILLAGE_SHELL_FACILITIES_AND_ACCESS
  requires_result:
    path: REPORTS/MAP13_04_IMPLEMENT_VILLAGE_SHELL_FACILITIES_AND_ACCESS_RESULT.md
    status: PASS
    sha256: f5419f1218885ebe89a24d8106a481df93da80d9c25821f4398748f2ab96ab26
  requires_installed_task:
    path: TASKS/MAP13_04_IMPLEMENT_VILLAGE_SHELL_FACILITIES_AND_ACCESS.md
    sha256: 98d26c04eb9de8a8a9401f84f613d1bacc501fe1ce8adf799a1a02a6a8b9a075
  sets_current_task: MAP13_05_IMPLEMENT_VILLAGE_STATE_VARIANTS
```

# MAP13_05 — Village State Variants

```text
TASK: MAP13_05_IMPLEMENT_VILLAGE_STATE_VARIANTS
PHASE: MAP13 — SpecialRegion / Village / Mandatory Landmarks
STATUS: CURRENT
NEXT: MAP13_06_AUTHOR_THREE_CORE_RESOURCE_REGIONS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP13_04의 immutable Village shell plan을 그대로 유지하면서, caller-authored NPC·inventory·door marker를 아래 다섯 상태의 immutable snapshot으로 compile한다.

```text
VillageShellPlan
+ explicit NPC / inventory / door marker definitions
+ one explicit IndividualHostile target NPC ID
→ Normal / Friendly / IndividualHostile / AllHostile / Evacuation snapshots
```

이번 Task의 핵심은 **상태가 바뀌어도 shell 이동 구조가 단 한 cell도 바뀌지 않음을 증명하는 것**이다. state snapshot은 marker 의미만 게시하며 FixedCollision, FixedAccess, central road, Facility slot·door 좌표, road-return witness 또는 persistence ownership을 수정하지 않는다.

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 정확한 script 경로, class/method별 input→output, 새 기능, 파이프라인 위치, 미구현, Editor/게임 가시성을 실제 결과로 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| 다섯 Village marker-state snapshot | actual NPC spawn/AI/faction/combat |
| explicit IndividualHostile target 1명 | target 탐색·거리 판정·집단 전파 |
| NPC disposition marker | NPC Prefab/animation/health |
| inventory availability marker | item/price/stock/shop gameplay |
| door presentation marker | door collider/lock/open/close/navigation |
| shell-invariant proof와 state digest | trigger/state machine/save load 실행 |

MAP13_01~04의 source plan, CSV, content catalog, Scene/Prefab/Tilemap은 변경하지 않는다.

## 2. Focused-Only Policy

정상 실행은 EditMode category `MAP13_05`만 선택한다.

```text
MAP13_05 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13_01~04 selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
```

current public API 호출은 과거 category 재실행이 아니다. 실제 upstream defect를 발견하면 기존 파일을 고치지 말고 owner/invariant/reason/minimum verification을 기록해 `BLOCKED`로 STOP한다. 신규 파일 자체 문제만 고치고 `MAP13_05`만 재실행한다.

## 3. Read-Only Preflight

```text
MAP13_04 Result: PASS
MAP13_04 Result SHA-256:
f5419f1218885ebe89a24d8106a481df93da80d9c25821f4398748f2ab96ab26

MAP13_04 installed Task SHA-256:
98d26c04eb9de8a8a9401f84f613d1bacc501fe1ce8adf799a1a02a6a8b9a075

MAP13_04 COMPLETE / MAP13_05 CURRENT / MAP13_06 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required authority:

```text
MAP13_01 Village SpecialRegionSiteBridge and coordinates
MAP13_02 Entry/Return/apron/bidirectional plan
MAP13_03 FixedCollision / FixedAccess / Facility slot layers and digests
MAP13_04 VillageShellPlan, central road, Facility bindings, doors and access witnesses
```

source가 Village가 아니거나 expected digest가 맞지 않으면 typed compile failure다. required public authority가 없거나 기존 source 수정이 필요하면 Task를 `BLOCKED`로 보고한다.

## 4. Exact Write Boundary

정상 범위는 Runtime 2개, focused test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/VillageStateVariants.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/VillageStateVariantCompiler.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SpecialRegions/VillageStateVariantTests.cs(.meta)
```

```text
Runtime: Game.Map.Runtime / StarNight.Map.WorldGeneration.SpecialRegions
Tests: Game.Map.Tests.EditMode / StarNight.Map.Tests.EditMode.WorldGeneration.SpecialRegions
Category: MAP13_05
```

수정 금지:

```text
existing C# / test / CSV / meta
asmdef / asmref
Authoring / Generated
Scene / Prefab / ScriptableObject / Tilemap / Material / Texture
Settings / Packages
```

helper 파일, static starter content, Editor window, asset, importer, serializer는 추가하지 않는다.

## 5. Marker and Variant Model

프로젝트 naming/style에 맞추되 다음 semantic surface를 제공한다.

```text
VillageStateKind: Normal / Friendly / IndividualHostile / AllHostile / Evacuation
VillageNpcMarkerState: Normal / Friendly / Hostile / Evacuated
VillageInventoryMarkerState: Standard / FriendlyAccess / Unavailable / Evacuated
VillageDoorMarkerState: Standard / Welcome / Alert / Evacuated

VillageNpcMarkerDefinition
VillageInventoryMarkerDefinition
VillageDoorMarkerDefinition
VillageStateMarkerSetDefinition
VillageNpcMarkerSnapshot
VillageInventoryMarkerSnapshot
VillageDoorMarkerSnapshot
VillageStateVariantSnapshot
VillageStateVariantSet
VillageStateVariantCompileRequest
VillageStateVariantCompiler.Compile
VillageStateVariantCanonicalDigest
VillageStateVariantErrorCode / Error / Result
```

이름은 기존 code style 때문에 합리적으로 조정할 수 있지만 아래 의미와 ownership은 유지한다.

### 5.1 Explicit marker identity

- 모든 marker stable ID와 Facility binding ID는 caller의 explicit input이다.
- filename, display string, marker/Facility ID prefix·suffix에서 의미를 추론하지 않는다.
- NPC, inventory, door marker collection은 각각 non-empty다.
- NPC marker는 최소 2개여야 `IndividualHostile`과 `AllHostile`이 구조적으로 구분된다.
- 모든 marker ID는 자기 종류 안에서 unique하고, 참조하는 Facility binding은 MAP13_04 plan에 exact 존재해야 한다.
- door marker는 각 MAP13_04 Facility binding에 exact 1개이며, 그 Facility의 기존 door coordinate를 참조한다. 새 door 좌표를 만들지 않는다.
- NPC와 inventory marker는 기존 Facility binding에 결합한 semantic marker다. spawn point, item payload 또는 collision을 소유하지 않는다.

### 5.2 Exact five-state matrix

compiler는 한 요청에서 exact 다섯 snapshot을 canonical order로 발행한다.

| Variant | NPC markers | Inventory markers | Door markers |
|---|---|---|---|
| `Normal` | 모두 `Normal` | 모두 `Standard` | 모두 `Standard` |
| `Friendly` | 모두 `Friendly` | 모두 `FriendlyAccess` | 모두 `Welcome` |
| `IndividualHostile` | explicit target exact 1명만 `Hostile`, 나머지 `Normal` | 모두 `Standard` | 모두 `Standard` |
| `AllHostile` | 모두 `Hostile` | 모두 `Unavailable` | 모두 `Alert` |
| `Evacuation` | 모두 `Evacuated` | 모두 `Evacuated` | 모두 `Evacuated` |

- IndividualHostile target ID는 explicit, existing, unambiguous NPC marker여야 한다.
- compiler가 target을 선택하거나 인접 NPC·시설로 적대를 전파하지 않는다.
- `FriendlyAccess`/`Unavailable`은 availability marker일 뿐 item, price, stock 또는 interaction result가 아니다.
- door states는 presentation marker일 뿐 collider, lock, open/close 또는 path blocking effect가 없다.
- variant 간 marker ID, Facility binding, source coordinate와 collection count는 동일하고 state enum만 달라진다.

## 6. Shell-Invariant Proof

각 snapshot은 다음 MAP13_04 source identity를 그대로 게시한다.

```text
Village aggregate digest
road digest
facility digest
access digest
layout ID / shape / bounds
road cell count and ordered Entry↔Return witness identity
Facility binding count and slot/door coordinates
Facility road-return witness identity
```

다섯 snapshot과 모든 ordered pair 비교에서 위 값은 exact 동일해야 한다. state compiler의 geometry/access mutation counters는 모두 `0`이다.

```text
FixedCollision writes: 0
FixedAccess writes: 0
road/path/carve writes: 0
Facility/door coordinate writes: 0
slot occupant/persistence writes: 0
RNG/world/tile/Scene/Prefab mutation: 0
```

state variant는 별도 marker-state plan이다. 기존 `VillageShellPlan`을 clone하여 cell을 고치거나, 통행 보존을 위해 synthetic edge/teleport/carve를 추가하지 않는다.

## 7. Output, Digest and Atomic Failure

Success output:

```text
exact five canonical VillageStateVariantSnapshot
canonical NPC/inventory/door marker snapshots
explicit IndividualHostile target identity
unchanged shell-invariant source digests/counts
per-variant marker digest
aggregate variant-set digest
zero geometry/access/persistence mutation counters
```

Collections은 defensive-copy/read-only/canonical order다. same input/reverse enumeration/repeat/`tr-TR`는 same semantic snapshots/digests를 게시한다. display text, time, object identity, Unity lifecycle은 digest에서 제외한다.

Any error는 variant set과 digests `0`; errors는 accumulated, deduped, stable-sorted다. partial snapshot, fallback target, implicit marker 또는 source mutation은 발행하지 않는다.

Minimum error groups:

```text
MissingInput | DigestMismatch | NotVillage
MissingMarkerKind | DuplicateMarker | UnknownFacilityBinding | DoorBindingMismatch
InsufficientNpcMarkers | MissingIndividualTarget | UnknownIndividualTarget
DuplicateVariant | MissingVariant | VariantMatrixMismatch
ShellInvariantViolation | NonCanonicalPublication
```

## 8. Focused Tests

test-owned explicit marker definitions으로 최소 다음을 검증한다.

1. valid `1×1`, `2×1`, `1×2` Village shell source에서 exact five variants compile
2. Normal/Friendly/IndividualHostile/AllHostile/Evacuation matrix exact 일치
3. IndividualHostile가 explicit target exact 1명만 Hostile로 바꾸고 나머지는 Normal 유지
4. all-hostile/friendly/evacuation의 NPC·inventory·door marker state exact 일치
5. 모든 variant의 marker identity/Facility binding/coordinate/count 보존
6. 다섯 variant의 모든 ordered pair에서 shell/road/facility/access digest와 witness identity 보존
7. door marker 변화가 collision/lock/path-blocking claim을 만들지 않음
8. missing/duplicate marker, unknown Facility, door mismatch, invalid individual target 원자 실패
9. source digest mismatch/non-Village/variant-set 불완전 상태 원자 실패
10. reverse/repeat/culture/immutability/digest와 모든 mutation counter 0

actual NPC, shop/inventory, door MonoBehaviour, state trigger, save/load 또는 physics fixture를 test 안에 만들지 않는다.

## 9. Verification and Required Result

Unity refresh/compile 후 `MAP13_05` EditMode만 실행한다.

```text
discovered = executed = passed
failed / skipped / inconclusive = 0 / 0 / 0
compile / relevant Console error = 0 / 0
prior category / legacy / PlayMode / unfiltered selections = 0 / 0 / 0 / 0
```

Static gate:

```text
new Runtime C#/meta: 2/2
new focused test C#/meta: 1/1
existing C#/test/CSV/meta modifications: 0
Authoring/Generated/Scene/Prefab/Tilemap/Settings/Packages changes: 0
duplicate GUID: 0
unapplied candidate/diff-check/unrelated staged: 0/0/0
Git push: NOT PERFORMED
```

Result 경로:

```text
MapDesign/MCP/REPORTS/MAP13_05_IMPLEMENT_VILLAGE_STATE_VARIANTS_RESULT.md
```

상단 verdict:

```text
TASK: MAP13_05_IMPLEMENT_VILLAGE_STATE_VARIANTS
STATUS: PASS | BLOCKED
MAP13_05: COMPLETE ELIGIBLE | NOT COMPLETE
MAP13_06_AUTHOR_THREE_CORE_RESOURCE_REGIONS: LOCKED / DO NOT START
```

`## User-Facing Implementation Report`에서 다음을 실제 수치로 보고한다.

- 신규/수정 script 전체 경로와 class/method별 input→output
- 다섯 state별 NPC/inventory/door marker 상태와 실제 marker 수
- IndividualHostile target exact-one 증명
- shell/road/facility/access digest·coordinate·witness 불변 결과
- 새로 가능해진 것과 파이프라인 위치
- 아직 미구현한 실제 NPC/AI/inventory/door behavior, transition trigger/save/load, MAP13_06+
- Editor/게임 가시성

`## Responsibility and Added Functions`에는 아래를 표로 보고한다.

| Field | Required evidence |
|---|---|
| Task responsibility | 다섯 Village marker-state variant와 shell-invariant proof compile |
| Added scripts | Runtime 2 + test 1 exact paths |
| Added functions | public type/method별 sole responsibility와 input→output |
| Inputs consumed | MAP13_04 VillageShellPlan + expected digest + explicit marker definitions/target |
| Outputs produced | immutable exact-five variant set + marker/shell invariant digests/errors |
| Explicit non-ownership | NPC/AI/combat, item/stock/shop, door collision/lock, transitions/save, content authoring |
| Downstream consumer | 별도 검수 후 MAP13_06만 unlock 가능 |

그 뒤 focused test, static scope, regression selections, task-owned files와 commit handoff를 기록한다.

정상 문구:

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

PASS일 때만 Status Finalize 후 task-owned 파일만 atomic commit한다.

```text
Subject: MAP13_05: implement Village state variants
Push: NOT PERFORMED
```

Result가 PASS여도 MAP13_06을 자동 시작하지 않고 별도 검수까지 LOCKED로 유지한다.
