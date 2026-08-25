# MAP08_07_AUTHOR_CRATER_MILL_BOUNDARIES

MAP08_06 PASS 후 MAP08_07 하나만 여는 패치다.

```text
Prior Result: MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 618cec23763ab38d4053a30ae348a4d6c187e2a8d4587d786247a514956a2ece
Prior installed/repaired Task SHA-256: 24799620d254846e3a99c0a3dadcb00156ab3f6f4804bbf09d2ce4eacda70293
State after apply: 97 COMPLETE / MAP08_07 CURRENT / 107 LOCKED
```

## Payload

```text
PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
PAYLOAD/06_IMPLEMENTATION_STATUS.md
PAYLOAD/TASKS/MAP08_07_AUTHOR_CRATER_MILL_BOUNDARIES.md
```

## Scope

- `MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES`를 COMPLETE로 확정한다.
- `MAP08_07_AUTHOR_CRATER_MILL_BOUNDARIES`만 CURRENT로 연다.
- `PAIR_CRATER_MILL` MoonCrater↔AbandonedMill boundary 후보 matrix를 Authoring CSV와 Runtime validator로 완성하게 한다.
- 필수 matrix는 `BOUND_RUIN`, `BOUND_SOFT_BLEND` × `HORIZONTAL`, `VERTICAL`의 4 active candidates다.
- Crater↔Mill 외 pair authoring, generated CSV, MAP09+ 작업은 열지 않는다.
- `MAP08_08_AUTHOR_CRATER_DOUGH_BOUNDARIES`와 이후 전체는 `LOCKED / DO NOT START` 상태로 유지한다.
- 구현 PASS 후에는 task-owned 파일만 stage해 상세 커밋을 만들도록 Task 본문에 요구사항을 포함한다.

## Apply Rule

Patch apply 단계에서는 Master/Status/Task 문서만 교체/생성한다. Assets, CSV, C#, tests, asmdef, Scene/Prefab은 patch apply 단계에서 직접 수정하지 않는다.
