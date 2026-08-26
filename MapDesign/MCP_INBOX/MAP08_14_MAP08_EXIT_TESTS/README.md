# MAP08_14_MAP08_EXIT_TESTS

MAP08_13 PASS 후 MAP08_14 하나만 여는 패치다. 이 단계는 MAP08 phase exit tests이며 MAP09는 아직 열지 않는다.

```text
Prior Result: MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: cb72264380c94a35ab6abe42f672c06e994f30deadc5a867546a31279b9bf7cd
Prior installed Task SHA-256: 5e932e82ae7ed78e732c26907ac4cd480e43d7bf14ee9978df46d2917310153d
State after apply: 104 COMPLETE / MAP08_14 CURRENT / 100 LOCKED
```

## Payload

```text
PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
PAYLOAD/06_IMPLEMENTATION_STATUS.md
PAYLOAD/TASKS/MAP08_14_MAP08_EXIT_TESTS.md
```

## Scope

- `MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW`를 COMPLETE로 확정한다.
- `MAP08_14_MAP08_EXIT_TESTS`만 CURRENT로 연다.
- MAP08 phase exit tests와 approval report를 구현하게 한다.
- 검증 대상은 6 pair, 31 candidates, 31 microchunks, 2976 tile rows, 62 sockets, A/B direction reversal, H/V edge compatibility, warning evidence, MAP08_12 digest, MAP08_13 preview projection이다.
- Authoring CSV row 추가/삭제, generated CSV, Runtime/Editor production 신규 구현, sector assembly, Scene/Prefab, MAP09+ 작업은 열지 않는다.
- `MAP09_01_IMPLEMENT_SECTOR_RECIPE_RESOLVER`와 이후 전체는 `LOCKED / DO NOT START` 상태로 유지한다.
- 구현 PASS 후에는 task-owned 파일만 stage해 상세 커밋을 만들도록 Task 본문에 요구사항을 포함한다.

## Apply Rule

Patch apply 단계에서는 Master/Status/Task 문서만 교체/생성한다. Assets, CSV, C#, tests, asmdef, Scene/Prefab은 patch apply 단계에서 직접 수정하지 않는다.
