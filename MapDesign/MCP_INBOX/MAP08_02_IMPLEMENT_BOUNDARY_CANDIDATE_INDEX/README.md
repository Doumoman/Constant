# MAP08_02_IMPLEMENT_BOUNDARY_CANDIDATE_INDEX

MAP08_01 PASS 후 MAP08_02 하나만 여는 패치다.

```text
Prior Result: MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: bc9298f3e51615b4d9724bcd2d7c8809b1ba8d3455aa30e8436f6a25ab6d5970
Prior Task SHA-256: 19b9c50827238251e0851e7bfee6e6a216141696ed434509a47ff08b0e39848d
State after apply: 92 COMPLETE / MAP08_02 CURRENT / 112 LOCKED
```

## Payload

```text
PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
PAYLOAD/06_IMPLEMENTATION_STATUS.md
PAYLOAD/TASKS/MAP08_02_IMPLEMENT_BOUNDARY_CANDIDATE_INDEX.md
```

## Scope

- `MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS`를 COMPLETE로 확정한다.
- `MAP08_02_IMPLEMENT_BOUNDARY_CANDIDATE_INDEX`만 CURRENT로 연다.
- pair/profile/orientation/route/signature key 기반 immutable boundary candidate index를 구현하게 한다.
- resolver, mandatory filter, warning renderer, 실제 boundary content authoring, generated CSV, MAP09+ 작업은 열지 않는다.
- `MAP08_03_IMPLEMENT_BOUNDARY_CHUNK_RESOLVER`와 이후 전체는 `LOCKED / DO NOT START` 상태로 유지한다.

## Apply Rule

Patch apply 단계에서는 Master/Status/Task 문서만 교체/생성한다. Assets, CSV, C#, tests, asmdef, Scene/Prefab은 patch apply 단계에서 직접 수정하지 않는다.
