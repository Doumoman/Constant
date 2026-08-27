# MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES

MAP08_05 PASS 후 MAP08_06 하나만 여는 패치다.

```text
Prior Result: MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: ff8e384a5d450d356017cb91ad52a25b1143931a823c48b67023a7b6be599ac0
Prior Task SHA-256: 7336541b62db64f0a4e40c2a892a6a07000fa011c87934a68fb9f785c10842a6
State after apply: 96 COMPLETE / MAP08_06 CURRENT / 108 LOCKED
```

## Payload

```text
PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
PAYLOAD/06_IMPLEMENTATION_STATUS.md
PAYLOAD/TASKS/MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES.md
```

## Scope

- `MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT`를 COMPLETE로 확정한다.
- `MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES`만 CURRENT로 연다.
- `PAIR_CRATER_ROOT` MoonCrater↔CassiaRoot boundary 후보 matrix를 Authoring CSV와 Runtime validator로 완성하게 한다.
- 필수 matrix는 `BOUND_SOFT_BLEND`, `BOUND_CLIFF`, `BOUND_TUNNEL` × `HORIZONTAL`, `VERTICAL`의 6 active candidates다.
- Crater↔Root 외 pair authoring, generated CSV, MAP09+ 작업은 열지 않는다.
- `MAP08_07_AUTHOR_CRATER_MILL_BOUNDARIES`와 이후 전체는 `LOCKED / DO NOT START` 상태로 유지한다.
- 구현 PASS 후에는 task-owned 파일만 stage해 상세 커밋을 만들도록 Task 본문에 요구사항을 포함한다.

## Apply Rule

Patch apply 단계에서는 Master/Status/Task 문서만 교체/생성한다. Assets, CSV, C#, tests, asmdef, Scene/Prefab은 patch apply 단계에서 직접 수정하지 않는다.
