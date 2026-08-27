# MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES

MAP08_10 PASS 후 MAP08_11 하나만 여는 패치다.

```text
Prior Result: MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 058b5ed32dbca7ca06adf12595ac693f5218dec5f8ed0bfe14d5c12c03563f5a
Prior installed Task SHA-256: f8050e209aa342602616afadc7cf0c3258731c8034ab9aff94f2bd94918d04f8
State after apply: 101 COMPLETE / MAP08_11 CURRENT / 103 LOCKED
```

## Payload

```text
PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
PAYLOAD/06_IMPLEMENTATION_STATUS.md
PAYLOAD/TASKS/MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES.md
```

## Scope

- `MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES`를 COMPLETE로 확정한다.
- `MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES`만 CURRENT로 연다.
- `PAIR_MILL_DOUGH` AbandonedMill↔MoonDough boundary 후보 matrix를 Authoring CSV와 Runtime validator로 완성하게 한다.
- 필수 matrix는 `BOUND_RUIN` H/V, `BOUND_LAYER` V only, `BOUND_TUNNEL` H/V의 5 active candidates다.
- CSV delta는 exact `+5/+5/+480/+10`이고, 변형 후에도 H 2셀/V 3셀 clear corridor를 보장한다.
- generated CSV, MAP08_12+, MAP09+ 작업은 열지 않는다.
- `MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR`와 이후 전체는 `LOCKED / DO NOT START`로 유지한다.

## Apply Rule

Patch apply 단계에서는 Master/Status/Task 문서만 교체/생성한다. Assets, CSV, C#, tests, asmdef, Scene/Prefab은 patch apply 단계에서 직접 수정하지 않는다.

