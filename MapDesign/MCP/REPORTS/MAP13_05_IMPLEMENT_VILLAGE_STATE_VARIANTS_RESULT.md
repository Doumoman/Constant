TASK: MAP13_05_IMPLEMENT_VILLAGE_STATE_VARIANTS
STATUS: PASS
MAP13_05: COMPLETE ELIGIBLE
MAP13_06_AUTHOR_THREE_CORE_RESOURCE_REGIONS: LOCKED / DO NOT START

## User-Facing Implementation Report

MAP13_04의 immutable `VillageShellPlan`을 그대로 유지하면서 caller-authored NPC·inventory·door marker와 명시적 IndividualHostile target을 정확히 다섯 Village 상태 snapshot으로 compile하는 계층을 추가했다. 상태 변화는 marker enum에만 존재하며 road, Facility, door 좌표, access witness, slot occupant, persistence 또는 collision을 수정하지 않는다.

세 shell shape 모두 정확히 5개 variant를 발행했다.

| Source shape | Bounds | Road cells | Facility bindings / door markers | NPC markers | Inventory markers | Published variants |
|---|---:|---:|---:|---:|---:|---:|
| 1×1 | 48×32 | 48 | 5/5 | 3 | 2 | 5/5 |
| 2×1 | 96×32 | 96 | 6/6 | 3 | 2 | 5/5 |
| 1×2 | 48×64 | 64 | 5/5 | 3 | 2 | 5/5 |

실제 state matrix는 다음과 같다.

| Variant | NPC marker states | Inventory marker states | Door marker states |
|---|---|---|---|
| Normal | Normal 3 | Standard 2 | Standard 5 또는 6 |
| Friendly | Friendly 3 | FriendlyAccess 2 | Welcome 5 또는 6 |
| IndividualHostile | explicit target 1만 Hostile, 나머지 2는 Normal | Standard 2 | Standard 5 또는 6 |
| AllHostile | Hostile 3 | Unavailable 2 | Alert 5 또는 6 |
| Evacuation | Evacuated 3 | Evacuated 2 | Evacuated 5 또는 6 |

각 marker는 explicit stable ID와 exact MAP13_04 Facility binding ID를 보존한다. NPC/inventory source coordinate는 binding slot coordinate이고, door marker는 각 Facility binding마다 정확히 하나이며 기존 door coordinate와 exact 일치한다. door state는 presentation marker일 뿐 collision, lock, open/close, path blocking ownership과 write count가 모두 0이다.

모든 snapshot은 Village aggregate, road, facility, access digest, layout ID/shape/bounds, road cell 수, Facility binding 수, ordered Entry↔Return road witness digest, slot/door coordinate digest, Facility road-return witness digest를 동일하게 발행한다. 다섯 snapshot의 모든 비교에서 이 shell identity가 변하지 않았고, state compiler의 FixedCollision/FixedAccess/geometry/access/persistence/RNG/world/tile/Scene/Prefab mutation count는 모두 0이었다.

오류가 하나라도 있으면 partial snapshot이나 fallback target 없이 variant set과 digest를 모두 비운다. missing/duplicate marker, unknown Facility binding, door mismatch, insufficient NPC, missing/unknown target, duplicate/missing variant, source digest mismatch, non-Village 및 shell-invariant corruption을 accumulated·deduplicated·stable-sorted error로 확인했다. reverse enumeration, repeat compile, `tr-TR`, read-only collection과 digest 안정성도 확인했다.

새 기능은 Runtime data/compiler API와 focused EditMode test에만 존재한다. Editor/Game 화면에 새 UI나 오브젝트는 없으며 실제 NPC spawn/AI/combat, item/price/stock/shop, door collider/lock/animation, transition trigger/state machine/save-load, content authoring은 구현하지 않았다.

## Responsibility and Added Functions

| Field | Evidence |
|---|---|
| Task responsibility | Exact-five Village marker-state variant와 MAP13_04 shell-invariant proof를 compile한다. |
| Added scripts | `Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/VillageStateVariants.cs`; `Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/VillageStateVariantCompiler.cs`; `Assets/_Game/Tests/EditMode/Map/WorldGeneration/SpecialRegions/VillageStateVariantTests.cs` 및 각 matching meta. |
| Added functions | `VillageStateKind`, NPC/inventory/door state enum은 exact matrix를 표현한다. `VillageNpcMarkerDefinition`, `VillageInventoryMarkerDefinition`, `VillageDoorMarkerDefinition`, `VillageStateMarkerSetDefinition`은 explicit marker/Facility/target/variant 입력을 방어 복사한다. 세 marker snapshot과 `VillageStateVariantSnapshot`은 state와 source coordinate를 read-only로 발행한다. `VillageStateVariantSet`은 canonical exact-five 결과와 zero-mutation counters를 제공한다. `VillageStateVariantCompileRequest`는 source kind, MAP13_04 plan, expected digest와 marker set을 묶는다. `VillageStateVariantCompiler.Compile`은 source/invariant, marker identity, exact door binding, target, requested variants와 state matrix를 누적 검증하고 원자적으로 성공/실패한다. `VillageStateVariantCanonicalDigest`는 marker, snapshot, shell witness/coordinate, aggregate SHA-256을 계산한다. Error enum/error/result는 stable atomic failure surface를 제공한다. |
| Inputs consumed | MAP13_04 `VillageShellPlan`, expected Village aggregate digest, explicit source region kind, NPC/inventory/door marker definitions, explicit IndividualHostile target marker ID, exact requested state kinds. |
| Outputs produced | Immutable exact-five `VillageStateVariantSet`, canonical NPC/inventory/door snapshots, per-variant marker/snapshot digest, aggregate digest, unchanged shell/road/facility/access/coordinate/witness proof, stable errors. |
| Explicit non-ownership | NPC spawn/AI/faction/combat, item/price/stock/shop, door collision/lock/open-close/navigation, transition/state machine/save-load, CSV/content catalog/Prefab/Scene/Tilemap authoring. |
| Downstream consumer | 별도 검증에서 PASS가 확인된 뒤 후속 작업이 immutable marker-state variant set을 사용할 수 있다. 이 작업은 MAP13_06을 시작하거나 unlock하지 않는다. |

## Focused Verification

최종 authoritative Unity selection은 다음 하나뿐이다.

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP13_05]
job_id: a1c633391b2542e4a62b278e6f181615
discovered: 12
executed: 12
passed: 12
failed: 0
skipped: 0
inconclusive: 0
resultState: Passed
durationSeconds: 1.8610804
compile errors: 0
relevant Console errors after final compile: 0
```

첫 동일-filter 시작은 신규 `.cs`가 Test Framework cleanup snapshot 이후 처음 등록되는 Asset Database import 타이밍과 겹쳐 summary를 만들지 못했다. import가 안정된 뒤 범위를 변경하지 않고 같은 `MAP13_05` EditMode category를 재실행해 위의 완전한 12/12 PASS 결과를 얻었다. 다른 category나 test mode 선택은 없었다.

Focused cases는 세 shape exact-five compile, five-state matrix, target exact-one, marker/Facility/coordinate identity, shell/road/facility/access/ordered witness 불변, door non-ownership, marker/target/variant/source 오류 원자 실패, reverse/repeat/culture/immutability/digest 및 모든 mutation counter 0을 포함한다.

## Static Scope and Handoff

```text
new Runtime C#/meta: 2/2
new focused test C#/meta: 1/1
existing C#/test/CSV/meta modifications: 0
asmdef/asmref changes: 0
Authoring/Generated/Scene/Prefab/Tilemap/Settings/Packages changes: 0
duplicate GUID: 0
runtime UnityEngine/UnityEditor/MonoBehaviour/ScriptableObject/Tilemap/Physics/Prefab/filesystem/RNG/time dependencies: 0
runtime static mutable collection cache: 0
unapplied inbox candidates: 0
installed/archive SHA-256: 3bddc96bb417a2a575472b81c8729f2ac2de52804df5f52f02dc97b914505f58
status row count before/finalize input: 215
unrelated staged paths before finalize: 0
Git push: NOT PERFORMED
```

기존 dirty `Constant.slnx`와 기존 untracked `TerrainClusters.meta` 3개는 수정하거나 stage하지 않는다. Status Finalize와 commit에는 task-owned Runtime/test/meta, installed/archive Task, Result, 정확히 두 필드만 변경한 Status만 포함한다.

REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0

Commit subject: `MAP13_05: implement Village state variants`

Push: NOT PERFORMED
