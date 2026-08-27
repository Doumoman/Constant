# MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT

MAP08_04 PASS 후 MAP08_05 하나만 여는 패치다.

```text
Prior Result: MAP08_04_FILTER_MANDATORY_BOUNDARIES_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: f189dc539efd54979d376d6bba5c809aadf93e7c63098d81ca0acd0656a7a4fd
Prior Task SHA-256: 9e3992c4bcf5ff7f891e236bfddeb00f746c6fee3effd2fb8a18b18da08781b9
State after apply: 95 COMPLETE / MAP08_05 CURRENT / 109 LOCKED
```

## Payload

```text
PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
PAYLOAD/06_IMPLEMENTATION_STATUS.md
PAYLOAD/TASKS/MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT.md
```

## Scope

- `MAP08_04_FILTER_MANDATORY_BOUNDARIES`를 COMPLETE로 확정한다.
- `MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT`만 CURRENT로 연다.
- Boundary profile `warning_microchunks_min`과 다음 biome marker category 최소 2종 조건을 Runtime-only contract/validator로 구현하게 한다.
- 실제 pair boundary microchunk content authoring, generated CSV, MAP09+ 작업은 열지 않는다.
- `MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES`와 이후 전체는 `LOCKED / DO NOT START` 상태로 유지한다.
- 구현 PASS 후에는 task-owned 파일만 stage해 상세 커밋을 만들도록 Task 본문에 요구사항을 포함한다.

## Apply Rule

Patch apply 단계에서는 Master/Status/Task 문서만 교체/생성한다. Assets, CSV, C#, tests, asmdef, Scene/Prefab은 patch apply 단계에서 직접 수정하지 않는다.
