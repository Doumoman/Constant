TASK: MAP13_04_IMPLEMENT_VILLAGE_SHELL_FACILITIES_AND_ACCESS
STATUS: PASS
MAP13_04: COMPLETE ELIGIBLE
MAP13_05_IMPLEMENT_VILLAGE_STATE_VARIANTS: LOCKED / DO NOT START

## User-Facing Implementation Report

Village 전용 shell 계획을 메모리 안에서 검증·발행하는 계층을 추가했다. 호출자가 명시한 layout ID, 1×1/2×1/1×2 shape, central-road 순서, Kitchen/Repair/Optional 정의, Facility slot ID, occupant intent, door 및 access witness만 사용한다. 파일명·표시 문자열·slot 이름에서 의미를 추론하거나 자동 road/path 탐색, carve, teleport를 수행하지 않는다.

실제 검증 결과는 다음과 같다.

| Shape | 정확한 region bounds | Central-road cells | 활성 sector coverage | Apron intersections | Internal seam | Facility bindings | Road-return witnesses |
|---|---:|---:|---:|---:|---|---:|---:|
| 1×1 | 48×32 | 48 | 1/1 | Entry 1 + Return 1 | 해당 없음 | 5 = Kitchen 1 + Repair 1 + Optional 3 | 5/5 |
| 2×1 | 96×32 | 96 | 2/2 | Entry 1 + Return 1 | x=47/48 cardinal pair 1 | 6 = Kitchen 1 + Repair 1 + Optional 4 | 6/6 |
| 1×2 | 48×64 | 64 | 2/2 | Entry 1 + Return 1 | y=31/32 cardinal pair 1 | 5 = Kitchen 1 + Repair 1 + Optional 3 | 5/5 |

모든 road/door/witness 좌표는 region-wide bounds 안에서 world sector/local tile로 투영되고 다시 같은 region tile로 round-trip한다. 각 Facility는 owning slot과 cardinal-adjacent인 고유 door를 가지며, focused fixture의 witness는 `slot → door → road` 형식(중간 path 0개)이다. 동일 source cell의 forward/reverse 열거가 모두 발행되고 `AccessClass.MandatoryNoTool`, tool/synthetic edge/teleport/carve/physics claim 0을 유지한다. Kitchen과 Repair는 exact 1개씩 assignment가 필수이고, Optional 3개 또는 4개는 assigned와 explicit empty를 모두 허용한다.

오류가 하나라도 있으면 plan과 digest를 모두 비우고, 오류를 중복 제거한 뒤 enum/path/detail의 안정 순서로 누적 발행한다. non-Village, source digest 불일치, shape/footprint 불일치, out-of-range/disconnected/missing road, apron/sector/seam 누락, fixed/slot/path 충돌, required clear, 잘못된 door/witness, missing/duplicate Facility를 focused test로 확인했다. reverse input enumeration, repeat compile, `tr-TR`, read-only collection, digest 안정성도 확인했다.

새 기능은 Runtime data/compiler API와 EditMode test에만 존재한다. Editor window, Scene, Prefab, Tilemap, Authoring asset 또는 실제 게임 UI는 추가하지 않았으므로 현재 가시성은 코드/API와 테스트 결과에 한정된다. 실제 building prefab, NPC/inventory, player physics, state variant는 아직 구현하지 않았으며 MAP13_05 이후의 별도 소유 범위다.

## Responsibility and Added Functions

| Field | Evidence |
|---|---|
| Task responsibility | Village shell shape/bounds, explicit central road, exact Facility matrix, door/path→road access witness를 검증하고 immutable canonical plan으로 compile한다. |
| Added scripts | `Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/VillageShellFacilities.cs`; `Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/VillageShellFacilityCompiler.cs`; `Assets/_Game/Tests/EditMode/Map/WorldGeneration/SpecialRegions/VillageShellFacilityAccessTests.cs` 및 각 matching meta. |
| Added functions | `VillageLayoutId`와 shape/facility enum은 explicit identity/meaning을 표현한다. `VillageRoadCell`, `VillageFacilityDefinition`, `VillageFacilityAccessWitness`, `VillageShellDefinition`은 caller input을 방어 복사한다. `VillageRoadAccess`, `VillageFacilityBinding`, `VillageShellPlan`은 projected forward/reverse evidence와 zero-mutation counters를 read-only로 발행한다. `VillageShellCompileRequest`는 MAP13_01/02/03 source와 expected digest를 묶는다. `VillageShellFacilityCompiler.Compile`은 source identity, footprint, road, apron, seam, Facility assignment, door/access를 누적 검증하고 원자적으로 성공/실패한다. `VillageShellCanonicalDigest.Compute`, `ComputeRoad`, `ComputeFacilities`, `ComputeAccess`는 culture/display/order 독립 SHA-256을 만든다. `VillageShellErrorCode`, `VillageShellError`, `VillageShellResult`는 stable error/result surface를 제공한다. |
| Inputs consumed | MAP13_01 `SpecialRegionSiteBridge`; MAP13_02 `SpecialRegionEntryBufferPlan`; MAP13_03 `SpecialRegionFixedSlotLayerPlan`; caller-authored `VillageShellDefinition`; 세 source의 expected canonical digest. |
| Outputs produced | Immutable `VillageShellPlan`, projected canonical road와 Entry↔Return witness, Kitchen/Repair/Optional slot bindings, 모든 Facility의 forward/reverse road-return evidence, road/facility/access/aggregate digest, accumulated stable errors. |
| Explicit non-ownership | Content catalog/CSV, building Prefab, NPC/inventory, player collider/physics reachability, automatic solver/carve, Tilemap/Scene/Authoring mutation, persistence execution, MAP13_05 state variants. |
| Downstream consumer | 별도 검증에서 PASS가 확인된 뒤 MAP13_05가 이 immutable Village shell plan을 source로 사용할 수 있다. 이 작업은 MAP13_05를 시작하거나 unlock하지 않는다. |

## Focused Verification

Unity 6000.3.8f1에서 최종 authoritative selection은 다음 하나뿐이다.

```text
mode: EditMode
category_names: [MAP13_04]
job_id: faea1f603049413c9ff35dbbbd65b790
discovered: 12
executed: 12
passed: 12
failed: 0
skipped: 0
inconclusive: 0
resultState: Passed
durationSeconds: 1.7831032
compile errors: 0
relevant Console errors after final compile: 0
```

첫 동일-filter 시작은 Asset Database가 신규 `.cs` 3개를 처음 인지한 시점과 Test Framework cleanup snapshot이 겹쳐 결과 summary가 생성되지 않았다. asset import가 안정된 뒤 범위를 변경하지 않고 같은 `MAP13_04` EditMode filter를 재실행해 위의 완전한 12/12 PASS 결과를 얻었다. 다른 category 또는 test mode 선택은 없었다.

검증된 focused cases:

1. 세 shape의 exact bounds, coordinate round-trip, full sector coverage
2. ordered connected road, Entry/Return apron 교차, 2×1 및 1×2 seam crossing
3. Kitchen/Repair exact required 2개와 Optional 3/4개 matrix
4. 모든 5/6 Facility의 door→road forward/reverse MandatoryNoTool witness
5. required clear 거부, optional assigned/explicit empty 허용
6. fixed/slot/path collision, disconnected/out-of-range/missing road 원자 실패
7. non-Village, shape mismatch, missing/duplicate Facility 원자 실패
8. source digest mismatch, invalid door/witness, road-return 누락 원자 실패
9. reverse/repeat/`tr-TR` digest 안정성과 collection 불변성
10. RNG/world/tile/placement/spawn/despawn mutation 0 및 stable sorted error 발행

## Static Scope and Handoff

```text
new Runtime C#/meta: 2/2
new focused test C#/meta: 1/1
existing C#/test/CSV/meta modifications: 0
asmdef/asmref changes: 0
Authoring/Generated/Scene/Prefab/Tilemap/Settings/Packages changes: 0
duplicate GUID: 0
runtime UnityEngine/UnityEditor/Tilemap/Physics/Prefab/filesystem/RNG/time dependencies: 0
runtime static mutable collection cache: 0
unapplied inbox candidates: 0
installed/archive SHA-256: 98d26c04eb9de8a8a9401f84f613d1bacc501fe1ce8adf799a1a02a6a8b9a075
status row count before/finalize input: 215
unrelated staged paths before finalize: 0
Git push: NOT PERFORMED
```

기존 dirty `Constant.slnx`와 기존 untracked `TerrainClusters.meta` 3개는 수정하거나 stage하지 않는다. Status Finalize와 commit에는 task-owned Runtime/test/meta, installed/archive Task, Result, 정확히 두 필드만 바뀐 Status만 포함한다.

REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0

Commit subject: `MAP13_04: implement Village shell and facility access`

Push: NOT PERFORMED
