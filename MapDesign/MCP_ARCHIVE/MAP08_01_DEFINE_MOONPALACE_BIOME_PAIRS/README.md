# MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS

MAP07_13 PASS와 MAP07 phase exit approval 이후 MAP08의 첫 Task만 여는 패치다.

```text
Prior Result: MAP07_13_MAP07_STARTER_AND_EXIT_TESTS_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 263a2bbf291e4df25dbe6bc101986e11ebf39bc0fc3d0074759fb7450b6df77e
Prior Task SHA-256: 698a330dcd7a8ba14ec33cec51b68ea56be9382abd0eefde96eb2a516c81effb
State after apply: 91 COMPLETE / MAP08_01 CURRENT / 113 LOCKED
```

## Payload

```text
PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
PAYLOAD/06_IMPLEMENTATION_STATUS.md
PAYLOAD/TASKS/MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS.md
```

## Scope

- `MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS`만 CURRENT로 연다.
- 월궁 biome 4종과 unordered 6개 pair, H/V 방향, mandatory no-tool precondition, warning marker minimum 계약을 정의하게 한다.
- boundary candidate index, resolver, 실제 boundary content, generated CSV, MAP09+ 작업은 열지 않는다.
- `MAP08_02_IMPLEMENT_BOUNDARY_CANDIDATE_INDEX`와 이후 전체는 `LOCKED / DO NOT START` 상태로 유지한다.

## Apply Rule

Patch apply 단계에서는 Master/Status/Task 문서만 교체/생성한다. Assets, CSV, C#, tests, asmdef, Scene/Prefab은 patch apply 단계에서 직접 수정하지 않는다. 구현자는 이 Task를 MCP_INBOX에 넣고 Result를 생성한 뒤 PASS일 때만 다음 단계로 진행한다.
