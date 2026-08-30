```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP13_09_MAP13_SPECIAL_REGION_EXIT_TESTS
  task_file: TASKS/MAP13_09_MAP13_SPECIAL_REGION_EXIT_TESTS.md
  requires_current_task: NONE
  requires_completed_task: MAP13_08_CREATE_SPECIAL_VALIDATOR_AND_PREVIEW
  requires_result:
    path: REPORTS/MAP13_08_CREATE_SPECIAL_VALIDATOR_AND_PREVIEW_RESULT.md
    status: PASS
    sha256: fbf5c3181791cae9b25e92ed76d8e46828330a9b147ec82a246f7ea056664534
  requires_installed_task:
    path: TASKS/MAP13_08_CREATE_SPECIAL_VALIDATOR_AND_PREVIEW.md
    sha256: c5cd8963ecaa112f6a4cbc2d14568921e38f8eb484a4f8138a8bc4cd293c4d62
  sets_current_task: MAP13_09_MAP13_SPECIAL_REGION_EXIT_TESTS
```

# MAP13_09 — MAP13 SpecialRegion Exit Tests

```text
TASK: MAP13_09_MAP13_SPECIAL_REGION_EXIT_TESTS
PHASE: MAP13 — SpecialRegion / Village / Mandatory Landmarks
STATUS: CURRENT
NEXT: MAP14_01_BUILD_PLANNER_INPUT_AND_PACING_ROLE
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP13_01~08이 만든 SpecialRegion 계약을 phase-exit 관점에서 닫는다.

이번 Task는 새 production 기능을 추가하지 않는다. MAP13 public API와 MAP13_08 auditor/preview model을 소비하는 focused Editor test 1개만 추가해서 다음을 승인한다.

```text
MAP13_01~07 public plans/catalogs
→ MAP13_08 audit / preview model
→ MAP13_09 focused phase-exit assertions
→ MAP14 planner input can safely consume MAP13 reference contracts
```

중요: 이것은 broad regression이 아니다. legacy `19347`, PlayMode, unfiltered test, 과거 MAP09~12 category는 실행하지 않는다. 신규 test 안에서 MAP13 public API를 호출하는 것은 phase-exit consumer이며 과거 category 재실행으로 계산하지 않는다.

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 새로 추가한 script, test별 책임, 입력→출력, 실제 검증 수치, 이번 phase가 승인하는 범위, 아직 승인하지 않는 범위, Editor/게임 가시성을 반드시 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| MAP13 phase-exit focused assertions | production SpecialRegion CSV/schema/importer |
| MAP13_01~08 public output 간 상호 모순 검사 | MAP03/MAP14 placement solver |
| mandatory site 수, overlap 0, Village 비강제성 확인 | live generated world publication |
| CoreResource 보상/복구/중복/소실 risk 0 확인 | item/crafting/reward/save 실행 |
| Forge process order/resource-return/MoonSeal proof 확인 | inventory mutation/gameplay crafting |
| Boss seal gate/reset/no-new-movement proof 확인 | Boss AI/combat/HP/attack |
| optional Merchant/Maru deferred boundary 확인 | optional landmark world placement |
| focused EditMode category `MAP13_09` | broad regression, PlayMode reachability |

MAP13 exit PASS는 “SpecialRegion reference contract가 MAP14 planner input으로 넘어가도 되는 상태”만 의미한다. 실제 player collider reachability, Scene/Prefab/Tilemap, gameplay object, production placement는 승인하지 않는다.

## 2. Focused-Only and No-Regression Policy

정상 실행은 EditMode category `MAP13_09`만 선택한다.

```text
MAP13_09 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13_01~08 category selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
```

컴파일 확인과 Console 확인은 허용한다. 그러나 test selection은 `MAP13_09` category로 제한한다.

신규 test 작성 중 발견한 task-owned assertion 실수는 신규 test 파일만 수정하고 `MAP13_09`만 재실행한다.

upstream public API defect, 기존 데이터 모순, 또는 production code 수정이 필요한 문제를 발견하면 기존 파일을 고치지 않는다. owner/invariant/reason/minimum verification을 Result에 기록하고 `BLOCKED`로 STOP한다.

## 3. Read-Only Preflight

```text
MAP13_08 Result: PASS
MAP13_08 Result SHA-256:
fbf5c3181791cae9b25e92ed76d8e46828330a9b147ec82a246f7ea056664534

MAP13_08 installed Task SHA-256:
c5cd8963ecaa112f6a4cbc2d14568921e38f8eb484a4f8138a8bc4cd293c4d62

MAP13_08 COMPLETE / MAP13_09 CURRENT / MAP14_01 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required authority:

```text
MAP13_01 site bridge and coordinate provenance
MAP13_02 entry/return/apron/buffer/collision/priority contracts
MAP13_03 fixed shell, replaceable slot, persistence contracts
MAP13_04 Village shell/facility/access plans
MAP13_05 Village state variants
MAP13_06 CoreResource region plans
MAP13_07 Forge/Boss/Merchant/Maru landmark plans
MAP13_08 SpecialRegionValidationAuditor and SpecialRegionPreviewModel
```

required public authority가 없거나 existing source modification이 필요하면 `BLOCKED`다.

## 4. Exact Write Boundary

정상 범위는 focused Editor test 1개와 matching meta뿐이다.

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/SpecialRegions/Map13SpecialRegionExitTests.cs(.meta)
```

```text
Tests: Game.Map.Editor.Tests / StarNight.Map.Editor.Tests.WorldGeneration.SpecialRegions
Category: MAP13_09
```

수정·생성 금지:

```text
Runtime production C#
Editor production C#
existing C# / test / CSV / meta
V2 schema registry/test and Authoring/Generated CSV/meta
asmdef / asmref
Scene / Prefab / ScriptableObject / Tilemap / Material / Texture
Settings / Packages
PlayMode test/helper
preview screenshot/export/generated file
```

필요한 폴더가 이미 없으면 먼저 STOP하고 보고한다. 이번 exit test 때문에 folder meta를 새로 만들지 않는다.

## 5. Required Exit Assertions

`Map13SpecialRegionExitTests`는 MAP13_08에서 게시된 public auditor/preview model을 소비해 exact phase-exit assertions를 작성한다. MAP13_01~07의 내부 구현을 복제하거나 private reflection으로 우회하지 않는다.

최소 test surface:

```text
category: MAP13_09
test count: 8~12 focused tests
all tests deterministic and culture-stable
no scene/prefab/tilemap/asset mutation
```

### 5.1 Canonical publication

다음을 확인한다.

```text
artifact total: 10
family split: Village 3 / CoreResource 3 / Landmark 4
binding split: REFERENCE FIXTURE 8 / DEFERRED TO MAP14 2
section total: 80 PASS / 0 FAIL
audit error count: 0
mutation/solver/gameplay claim: 0 / 0 / 0
preview token total: 435
route total: 46
recovery route total: 9
state total: 61
reset total: 12
persistence checkpoint total: 28
```

모든 source/component/artifact/report digest는 64-hex이고 repeat/reverse input/`tr-TR`에서 동일해야 한다. 공개 accessor로 확인 가능한 MAP13_08 audit digest가 있으면 `a7ab6fd571425c4c8e64d7eecad5dd246a3d9a8a08044801800948fc2fa03e4e`와 일치하는지도 확인한다. 해당 digest가 default preview snapshot 전용이면 Result에 그렇게 보고하고 hard assertion은 64-hex/stability로 제한한다.

### 5.2 Mandatory site and overlap closure

다음을 승인한다.

```text
Village placed reference layouts: 3
CoreResource mandatory site plans: 3
Forge placed mandatory site: 1
Boss placed mandatory site: 1
Merchant/Maru placed claim: 0
duplicate artifact: 0
footprint/site/buffer/collision/fixed/access overlap failure: 0
synthetic edge / teleport / carve / world mutation: 0
```

Village는 SpecialRegion shell/facility/state reference일 뿐, global progression의 필수 방문 blocker로 승인하지 않는다. MAP13 exit test는 Village 미방문 또는 bypass 가능한 설계 경계를 깨는 mandatory dependency claim이 없는지 확인한다.

### 5.3 Village closure

Village 1×1/2×1/1×2에 대해 다음을 확인한다.

```text
state variants: exact 5 each
route witness: Entry → central road → facility/access → road → Return
facility/access routes: present
recovery route: 0
persistence checkpoint: 0
shell identity changes across variants: 0
required global progression reward: 0
```

2×1/1×2는 internal sector seam crossing evidence가 있어야 한다.

### 5.4 CoreResource closure

MoonCore, CassiaSap, StarNuruk에 대해 다음을 확인한다.

```text
required reward: exact 1 each
persistence checkpoints: exact 7 each
recovery route: exact 1 each
permanent loss risk: 0
duplicate benefit risk: 0
tool dependency on mandatory route: 0
Village/facility/inventory dependency: 0
failure rejoins RecoveryJoin → Low route
```

reward/save/inventory 실행을 주장하지 않는다.

### 5.5 Forge and Boss closure

Forge:

```text
process order: Grind → Mix → Press → MoonlightCure → MoonSeal
MoonSeal reward/key proof: present
stage failure returns all three required resources
ManualReset → SafeCorridor → Low route proof: present
resource permanent loss/duplicate risk: 0
inventory/crafting/save execution claim: 0
```

Boss:

```text
state order: GateLocked → GateAccepted → EncounterActive → Defeated
SealGate requires MoonSeal proof: present
encounter reset preserves accepted seal proof: present
fall/failure central recovery proof: present
new movement rule: 0
AI/combat/HP/attack execution claim: 0
```

### 5.6 Optional deferred closure

MerchantCave와 MaruShrine은 다음 경계를 지켜야 한다.

```text
binding: DEFERRED TO MAP14
world origin/reservation/bridge/buffer/fixed-slot/placed ownership claim: 0
optional progression dependency: 0
local Entry → optional interaction/choice → same local Return proof: present
```

Maru는 preview-before-choice, persistent choice, reroll/duplicate benefit risk 0을 확인한다. 실제 NPC, shop, hint, curiosity, world placement는 승인하지 않는다.

### 5.7 Preview consistency and read-only proof

`SpecialRegionPreviewModel`을 통해 다음을 확인한다.

```text
families: 3
artifacts: 10
view modes: 8
overlay toggles: 13
legend entries: 18
binding label kinds: 2
physics warning: PHYSICS NOT VERIFIED
default artifact: Village 1×1 Overview
default visible tokens: 42
default audit sections: 8 PASS / 0 FAIL
```

preview/model build 전후로 active scene path, scene root count, dirty state, Unity selection, Authoring/Generated inventory가 바뀌지 않아야 한다.

MAP13_09는 Editor window를 반드시 열 필요는 없다. MAP13_08에서 실제 window open evidence를 이미 확인했으므로, 이번 task는 model/window public contract를 read-only로 소비하는 데 집중한다. 단, window를 열어야만 확인 가능한 public contract가 있으면 한 번 열고 닫은 뒤 Result에 이유와 side effect 0을 기록한다.

## 6. Expected Result Report

Result에는 다음을 반드시 포함한다.

```text
TASK: MAP13_09_MAP13_SPECIAL_REGION_EXIT_TESTS
STATUS: PASS | FAIL | BLOCKED
MAP13_09: COMPLETE ELIGIBLE only when PASS
MAP14_01_BUILD_PLANNER_INPUT_AND_PACING_ROLE: LOCKED / DO NOT START
```

`## User-Facing Implementation Report`에 다음을 한국어로 적는다.

- 이번 Task가 추가한 것은 production 기능이 아니라 MAP13 phase-exit test 1개라는 점
- MAP13이 승인하는 범위: reference SpecialRegion contracts → MAP14 planner input
- 승인하지 않는 범위: live placement, physics, gameplay, CSV/schema/importer, Scene/Prefab/Tilemap
- 실제 수치: artifact/family/binding/route/recovery/state/reset/checkpoint/token/section/error
- 회귀를 돌리지 않았다는 증거: prior category 0, legacy 19347 0, PlayMode 0, unfiltered 0
- Editor/게임 가시성: Editor에는 기존 MAP13_08 preview가 계속 보이나 MAP13_09가 새 창/게임 화면을 만들지 않음

`## Responsibility and Added Functions`에 다음을 적는다.

- 추가한 script exact path
- test class와 각 test method별 책임
- 각 test method의 input→output
- upstream 수정 여부 0
- production code 추가 여부 0
- 미구현/미승인 범위

Focused verification block:

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP13_09]
discovered: <N>
executed: <N>
passed: <N>
failed: 0
skipped: 0
inconclusive: 0
compile errors: 0
relevant Console errors after final verification/clear: 0
relevant Console warnings after final verification/clear: 0

REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

If PASS:

```text
Commit subject: MAP13_09: approve SpecialRegion phase exit
Push: NOT PERFORMED
```

If FAIL or BLOCKED, do not finalize Status and do not open MAP14_01.

## 7. Finalize Rules

PASS일 때만 Status Finalize와 atomic commit을 수행한다.

Commit에 포함 가능한 파일:

```text
MCP/TASKS/MAP13_09_MAP13_SPECIAL_REGION_EXIT_TESTS.md
MCP_ARCHIVE/MAP13_09_MAP13_SPECIAL_REGION_EXIT_TESTS.md
MCP/REPORTS/MAP13_09_MAP13_SPECIAL_REGION_EXIT_TESTS_RESULT.md
MCP/06_IMPLEMENTATION_STATUS.md
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/SpecialRegions/Map13SpecialRegionExitTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/SpecialRegions/Map13SpecialRegionExitTests.cs.meta
```

관련 없는 dirty/untracked/staged 파일은 수정·stage·commit하지 않는다.

```text
Git push: forbidden
Next task MAP14_01: do not start
STOP after Result and optional PASS finalize commit
```
