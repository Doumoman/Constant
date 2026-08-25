# MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES

MAP08_08 PASS 후 MAP08_09 하나만 여는 패치다.

```text
Prior Result: MAP08_08_AUTHOR_CRATER_DOUGH_BOUNDARIES_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: df4137defbb8e44cba12ef3b74cd8635044b886657525ec128e05dd5b1bd67c9
Prior installed Task SHA-256: 92106729b49fa13c0fbb95f0338b3b619582a1696f10ac88604401469907d769
State after apply: 99 COMPLETE / MAP08_09 CURRENT / 105 LOCKED
```

## Payload

```text
PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
PAYLOAD/06_IMPLEMENTATION_STATUS.md
PAYLOAD/TASKS/MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES.md
```

## Scope

- `MAP08_08_AUTHOR_CRATER_DOUGH_BOUNDARIES`를 COMPLETE로 확정한다.
- `MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES`만 CURRENT로 연다.
- `PAIR_ROOT_MILL` CassiaRoot↔AbandonedMill boundary 후보 matrix를 Authoring CSV와 Runtime validator로 완성하게 한다.
- 필수 matrix는 `BOUND_RUIN` H/V, `BOUND_TUNNEL` H/V, `BOUND_SOFT_BLEND` H/V의 6 active candidates다.
- Root↔Mill 외 pair authoring, generated CSV, MAP09+ 작업은 열지 않는다.
- `MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES`와 이후 전체는 `LOCKED / DO NOT START` 상태로 유지한다.
- 구현 PASS 후에는 task-owned 파일만 stage해 상세 커밋을 만들도록 Task 본문에 요구사항을 포함한다.

## Apply Rule

Patch apply 단계에서는 Master/Status/Task 문서만 교체/생성한다. Assets, CSV, C#, tests, asmdef, Scene/Prefab은 patch apply 단계에서 직접 수정하지 않는다.
