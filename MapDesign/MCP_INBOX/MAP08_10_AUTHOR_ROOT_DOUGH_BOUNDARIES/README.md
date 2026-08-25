# MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES

MAP08_09 PASS 후 MAP08_10 하나만 여는 패치다.

```text
Prior Result: MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: c923b445d9dc7b1d057ec368cb154a5745f2e473e67a736fcf8ee20f66a9ef87
Prior installed Task SHA-256: c559168ad86e866124cc87843926d66b98f59ad1243a33755fce7f78621fd01f
State after apply: 100 COMPLETE / MAP08_10 CURRENT / 104 LOCKED
```

## Payload

```text
PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
PAYLOAD/06_IMPLEMENTATION_STATUS.md
PAYLOAD/TASKS/MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES.md
```

## Scope

- `MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES`를 COMPLETE로 확정한다.
- `MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES`만 CURRENT로 연다.
- `PAIR_ROOT_DOUGH` CassiaRoot↔MoonDough boundary 후보 matrix를 Authoring CSV와 Runtime validator로 완성하게 한다.
- 필수 matrix는 `BOUND_TUNNEL` H/V, `BOUND_LAYER` V only, `BOUND_SOFT_BLEND` H/V의 5 active candidates다.
- Root↔Dough 외 pair authoring, generated CSV, MAP09+ 작업은 열지 않는다.
- `MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES`와 이후 전체는 `LOCKED / DO NOT START` 상태로 유지한다.
- 구현 PASS 후에는 task-owned 파일만 stage해 상세 커밋을 만들도록 Task 본문에 요구사항을 포함한다.

## Apply Rule

Patch apply 단계에서는 Master/Status/Task 문서만 교체/생성한다. Assets, CSV, C#, tests, asmdef, Scene/Prefab은 patch apply 단계에서 직접 수정하지 않는다.
