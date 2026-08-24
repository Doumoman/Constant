# CHAR06_03 MCP_INBOX Package

이 패키지는 캐릭터 하네스의 다음 단일 작업만 연다.

```text
CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD
```

## Extraction Root

이번 ZIP은 중첩 경로를 피하기 위해 `CharacterDesign/`를 최상위에 포함하지 않는다.

ZIP을 다음 위치에 풀면 된다.

```text
CharacterDesign/MCP_INBOX/
```

정상 배치 결과:

```text
CharacterDesign/MCP_INBOX/CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD/
```

## Apply

1. `PATCH_MANIFEST.md`의 entry gate와 hash를 확인한다.
2. `PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md`를 `CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md`에 복사한다.
3. `PAYLOAD/06_IMPLEMENTATION_STATUS.md`를 `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md`에 복사한다.
4. `PAYLOAD/TASKS/CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD.md`를 `CharacterDesign/MCP/TASKS/CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD.md`에 생성한다.
5. `RUN_CHAR06_03_PROMPT.md`를 사용해 MCP 작업을 실행한다.

## Expected Report

```text
CharacterDesign/MCP/REPORTS/CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD_RESULT.md
```

## Scope

이 작업은 검증 전용이다.

```text
Unity compile
Character EditMode
MAP and Character EditMode regression
PlayMode discovery and run
active build target validation
console and scope audit
```

열지 않는 범위:

```text
CHAR06_04 final audit
runtime or test implementation
MAP edits
ProjectSettings or Packages edits
```

