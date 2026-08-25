# MAP08_08_AUTHOR_CRATER_DOUGH_BOUNDARIES

MAP08_07 PASS 후 MAP08_08 하나만 여는 패치다.

```text
Prior Result: MAP08_07_AUTHOR_CRATER_MILL_BOUNDARIES_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 59cc98beaa374a319c656c50f0c5aaf26a4f25a29f591eb017bf21d4a9eb995a
Prior installed/repaired Task SHA-256: bf9085abb16be5c0bc736fa78b709fd32972f5903ba332622860d41d13aa4577
State after apply: 98 COMPLETE / MAP08_08 CURRENT / 106 LOCKED
```

## Payload

```text
PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
PAYLOAD/06_IMPLEMENTATION_STATUS.md
PAYLOAD/TASKS/MAP08_08_AUTHOR_CRATER_DOUGH_BOUNDARIES.md
```

## Scope

- `MAP08_07_AUTHOR_CRATER_MILL_BOUNDARIES`를 COMPLETE로 확정한다.
- `MAP08_08_AUTHOR_CRATER_DOUGH_BOUNDARIES`만 CURRENT로 연다.
- `PAIR_CRATER_DOUGH` MoonCrater↔MoonDough boundary 후보 matrix를 Authoring CSV와 Runtime validator로 완성하게 한다.
- 필수 matrix는 `BOUND_CLIFF` H/V, `BOUND_LAYER` V only, `BOUND_SOFT_BLEND` H/V의 5 active candidates다.
- Crater↔Dough 외 pair authoring, generated CSV, MAP09+ 작업은 열지 않는다.
- `MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES`와 이후 전체는 `LOCKED / DO NOT START` 상태로 유지한다.
- 구현 PASS 후에는 task-owned 파일만 stage해 상세 커밋을 만들도록 Task 본문에 요구사항을 포함한다.

## Apply Rule

Patch apply 단계에서는 Master/Status/Task 문서만 교체/생성한다. Assets, CSV, C#, tests, asmdef, Scene/Prefab은 patch apply 단계에서 직접 수정하지 않는다.
