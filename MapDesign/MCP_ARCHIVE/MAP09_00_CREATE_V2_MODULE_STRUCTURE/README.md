# MAP09_00_CREATE_V2_MODULE_STRUCTURE

MAP08_14 PASS/finalize 뒤 V2 구조 전환 Task 하나만 여는 패치다. MAP09 기능 구현은 아직 시작하지 않는다.

```text
Prior Result: MAP08_14_MAP08_EXIT_TESTS_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 5d0b2f0d478ef8479b93e1b9163445f6e736022b533dee77f81690b8670cf2d1
Prior installed Task SHA-256: 6fffc0ed3f8ca333cf7d74d44c437ab6e4193871ce8b2a7a254405e4bcaa5e8e
State after apply: 105 COMPLETE / MAP09_00 CURRENT / 108 LOCKED
```

## Payload

```text
PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
PAYLOAD/06_IMPLEMENTATION_STATUS.md
PAYLOAD/TASKS/MAP09_00_CREATE_V2_MODULE_STRUCTURE.md
```

## Scope

- 기존 MAP00~08 파일·폴더·meta·GUID를 그대로 보존한다.
- Runtime 9, Runtime EditMode test 9, Authoring/Generated 6의 정확한 24개 V2 기능 루트만 additive로 만든다.
- 기존 `Domain/Data/Generation/Validation/Random/Diagnostics`, `Microchunks`, `Boundaries`를 이동하거나 재배치하지 않는다.
- C#, CSV, asmdef/asmref, Scene/Prefab, Generated CSV는 변경하지 않는다.
- 구조 PASS 뒤에도 `MAP09_01_FREEZE_BASELINE_AND_REGISTER_V2_PASSES`는 다음 별도 patch 전까지 LOCKED다.

## Apply Rule

Patch apply 단계에서는 Master/Status/Task 문서만 설치한다. Unity 폴더/meta는 Task execution 단계에서만 생성한다.
