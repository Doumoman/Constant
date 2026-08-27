# MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR

MAP08_11 PASS 후 MAP08_12 하나만 여는 패치다.

```text
Prior Result: MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 9c9ce342563858987b2489ae6aa9a50bee2473be4639b07cd7176ee18bcbde4c
Prior installed Task SHA-256: MUST COMPUTE FROM INSTALLED PROJECT FILE
State after apply: 102 COMPLETE / MAP08_12 CURRENT / 102 LOCKED
```

## Payload

```text
PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
PAYLOAD/06_IMPLEMENTATION_STATUS.md
PAYLOAD/TASKS/MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR.md
```

## Scope

- `MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES`를 COMPLETE로 확정한다.
- `MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR`만 CURRENT로 연다.
- 여섯 Moonpalace biome pair 전체의 boundary coverage validator를 Runtime/Test로 구현하게 한다.
- 검증 대상은 31 candidates, 31 microchunks, 2976 tile rows, 62 mandatory no-tool sockets다.
- Authoring CSV row 추가/삭제, generated CSV, preview window, sector assembly, MAP09+ 작업은 열지 않는다.
- `MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW`와 이후 전체는 `LOCKED / DO NOT START` 상태로 유지한다.
- 구현 PASS 후에는 task-owned 파일만 stage해 상세 커밋을 만들도록 Task 본문에 요구사항을 포함한다.

## Apply Rule

Patch apply 단계에서는 Master/Status/Task 문서만 교체/생성한다. Assets, CSV, C#, tests, asmdef, Scene/Prefab은 patch apply 단계에서 직접 수정하지 않는다.
