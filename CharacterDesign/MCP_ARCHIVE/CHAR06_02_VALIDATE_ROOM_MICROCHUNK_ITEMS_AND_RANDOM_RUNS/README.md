# CHAR06_02 MCP_INBOX Package

이 패키지는 캐릭터 하네스의 다음 단일 작업만 연다.

```text
CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS
```

## Apply

1. `PATCH_MANIFEST.md`의 entry gate와 hash를 확인한다.
2. `PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md`를 `CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md`에 복사한다.
3. `PAYLOAD/06_IMPLEMENTATION_STATUS.md`를 `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md`에 복사한다.
4. `PAYLOAD/TASKS/CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS.md`를 `CharacterDesign/MCP/TASKS/CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS.md`에 생성한다.
5. `RUN_CHAR06_02_PROMPT.md`를 사용해 MCP 작업을 실행한다.

## Expected Report

```text
CharacterDesign/MCP/REPORTS/CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS_RESULT.md
```

## Scope

이 작업은 생성 맵을 캐릭터 런타임이 안전하게 소비할 수 있는지 검증한다.

```text
room and microchunk validation
route request validation through CHAR06_01
item placement validation
bomb and rope affordance validation
deterministic random seed sweep
```

열지 않는 범위:

```text
CHAR06_03 full PlayMode/build validation
MAP generator rewrite
MAP authoring data edits
Tilemap, scene, prefab, UI, audio, save, ProjectSettings, Packages
```

