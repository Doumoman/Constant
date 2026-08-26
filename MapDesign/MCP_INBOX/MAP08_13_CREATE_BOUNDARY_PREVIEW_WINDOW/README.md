# MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW

MAP08_12 PASS 후 MAP08_13 하나만 여는 패치다. MAP08_10은 다른 채팅에서 작업된 PASS Result로 source-chain에 반영했다.

```text
Prior Result: MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 26801f7bc31d354b9639278ec133970e7840f3c7dacb9f0f2a4b6a0e0288896b
Prior installed Task SHA-256: cdaadd203562acdcad7024349499a2df3ac0ccd9860ef580473e984ae9490966
External accounted MAP08_10 Result SHA-256: 058b5ed32dbca7ca06adf12595ac693f5218dec5f8ed0bfe14d5c12c03563f5a
State after apply: 103 COMPLETE / MAP08_13 CURRENT / 101 LOCKED
```

## Payload

```text
PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
PAYLOAD/06_IMPLEMENTATION_STATUS.md
PAYLOAD/TASKS/MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW.md
```

## Scope

- `MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR`를 COMPLETE로 확정한다.
- `MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW`만 CURRENT로 연다.
- MAP08_12 coverage report를 읽는 Editor-only boundary preview window를 구현하게 한다.
- 표시 대상은 6 pair, 31 candidates, 31 microchunks, 2976 tile rows, 62 sockets, transition direction, overlay toggles, disabled/invalid reasons, marker/evidence다.
- Runtime coverage rule, Authoring CSV row 추가/삭제, generated CSV, sector assembly, Scene/Prefab, MAP09+ 작업은 열지 않는다.
- `MAP08_14_MAP08_EXIT_TESTS`와 이후 전체는 `LOCKED / DO NOT START` 상태로 유지한다.
- 구현 PASS 후에는 task-owned 파일만 stage해 상세 커밋을 만들도록 Task 본문에 요구사항을 포함한다.

## Apply Rule

Patch apply 단계에서는 Master/Status/Task 문서만 교체/생성한다. Assets, CSV, C#, tests, asmdef, Scene/Prefab은 patch apply 단계에서 직접 수정하지 않는다.
