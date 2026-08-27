# MAP08_03_IMPLEMENT_BOUNDARY_CHUNK_RESOLVER

MAP08_02 PASS 후 MAP08_03 하나만 여는 패치다.

```text
Prior Result: MAP08_02_IMPLEMENT_BOUNDARY_CANDIDATE_INDEX_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 2a160c7bc32cf7177208bbb0d06c0e449ef7dd3e7904bb23060484509d893c54
Prior Task SHA-256: 767fa235852c8b892fdb4dffe6cfdbda4283aa994f0dce64b295bdbbd4857e50
State after apply: 93 COMPLETE / MAP08_03 CURRENT / 111 LOCKED
```

## Payload

```text
PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
PAYLOAD/06_IMPLEMENTATION_STATUS.md
PAYLOAD/TASKS/MAP08_03_IMPLEMENT_BOUNDARY_CHUNK_RESOLVER.md
```

## Scope

- `MAP08_02_IMPLEMENT_BOUNDARY_CANDIDATE_INDEX`를 COMPLETE로 확정한다.
- `MAP08_03_IMPLEMENT_BOUNDARY_CHUNK_RESOLVER`만 CURRENT로 연다.
- Candidate index lookup 결과에 deterministic weight, tie-break, request direction, transform policy를 적용하는 resolver를 구현하게 한다.
- mandatory filter, warning renderer, 실제 boundary content authoring, generated CSV, MAP09+ 작업은 열지 않는다.
- `MAP08_04_FILTER_MANDATORY_BOUNDARIES`와 이후 전체는 `LOCKED / DO NOT START` 상태로 유지한다.

## Apply Rule

Patch apply 단계에서는 Master/Status/Task 문서만 교체/생성한다. Assets, CSV, C#, tests, asmdef, Scene/Prefab은 patch apply 단계에서 직접 수정하지 않는다.
