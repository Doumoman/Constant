# MAP08_04_FILTER_MANDATORY_BOUNDARIES

MAP08_03 PASS 후 MAP08_04 하나만 여는 패치다.

```text
Prior Result: MAP08_03_IMPLEMENT_BOUNDARY_CHUNK_RESOLVER_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 43a6d29466996164af4cc8e2d09dd6478a013f95c0b40ad15f132b3bead01445
Prior Task SHA-256: 1b5efb56faec6a0ea0e5fa6d751efc241f5474bf4403338fbb365b15f69b4b63
State after apply: 94 COMPLETE / MAP08_04 CURRENT / 110 LOCKED
```

## Payload

```text
PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
PAYLOAD/06_IMPLEMENTATION_STATUS.md
PAYLOAD/TASKS/MAP08_04_FILTER_MANDATORY_BOUNDARIES.md
```

## Scope

- `MAP08_03_IMPLEMENT_BOUNDARY_CHUNK_RESOLVER`를 COMPLETE로 확정한다.
- `MAP08_04_FILTER_MANDATORY_BOUNDARIES`만 CURRENT로 연다.
- Mandatory route boundary에서 `tool_requirement=NONE`이고 `mandatory_route_allowed=true`인 후보만 resolver 입력으로 남기는 filter를 구현하게 한다.
- warning renderer, 실제 boundary content authoring, generated CSV, MAP09+ 작업은 열지 않는다.
- `MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT`와 이후 전체는 `LOCKED / DO NOT START` 상태로 유지한다.

## Apply Rule

Patch apply 단계에서는 Master/Status/Task 문서만 교체/생성한다. Assets, CSV, C#, tests, asmdef, Scene/Prefab은 patch apply 단계에서 직접 수정하지 않는다.
