# MCP PATCH — MAP00_09 CREATE COORDINATE DEBUG VIEW v1.0

## 목적

`WorldGen/Coordinates` EditorWindow와 Scene View overlay에서 마우스 위치의 World/Sector/MicroChunk/Local 좌표를 동시에 표시한다.

이번 패치는 Editor 전용 Preview C# 1개, EditorWindow C# 1개, Editor EditMode test C# 1개만 만든다. Runtime, Scene object, Prefab, CSV 또는 생성 알고리즘은 변경하지 않는다.

## 적용 전 조건

```text
MAP00_01_PROJECT_AUDIT = COMPLETE
MAP00_02_FOLDER_AND_ASMDEF_PLAN = COMPLETE
MAP00_03_CREATE_MAP_MODULE_STRUCTURE = COMPLETE
MAP00_04_CREATE_TEST_STRUCTURE = COMPLETE / PASS
MAP00_05_DEFINE_WORLDGEN_CONSTANTS = COMPLETE / PASS
MAP00_06_IMPLEMENT_COORDINATE_VALUE_TYPES = COMPLETE / PASS
MAP00_07_IMPLEMENT_COORDINATE_CONVERSIONS = COMPLETE / PASS
MAP00_08_CREATE_COORDINATE_TESTS = COMPLETE / PASS
MAP01 이후 Task = NOT STARTED
Current Task = NONE
STATUS FINALIZE Upgrade v1.0 installed
```

기존 `MAP01_01_INSTALL_CSV_AUTHORING_BASELINE` 패치는 HOLD다. `MCP_INBOX`에 넣거나 실행하지 않는다.

## 정확한 폴더명

```text
MAP00_09_CREATE_COORDINATE_DEBUG_VIEW
```

folder name, `patch_id`, Task ID가 모두 위 문자열과 같아야 한다.

## 사용 방법

1. ZIP을 압축 해제한다.
2. `MCP_INBOX`에 다른 미적용 패치가 없음을 확인한다.
3. 생성된 폴더 전체를 `MapDesign/MCP_INBOX/` 바로 아래에 넣는다.
4. 최종 경로를 확인한다.

```text
MapDesign/MCP_INBOX/MAP00_09_CREATE_COORDINATE_DEBUG_VIEW/PATCH_MANIFEST.md
```

ZIP 자체를 INBOX에 넣거나 폴더를 이중 중첩하지 않는다.

5. 코딩 에이전트에게 다음 한 줄을 전달한다.

```text
MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.
```

정상 파이프라인:

```text
PATCH APPLY
→ MAP00_09 TASK EXECUTION
→ REPORTS/MAP00_09_CREATE_COORDINATE_DEBUG_VIEW_RESULT.md 생성
→ STATUS FINALIZE
→ MAP00_09 COMPLETE
→ Current Task NONE
→ STOP
```

## 실행 후 가져올 파일

```text
MapDesign/MCP/REPORTS/MAP00_09_CREATE_COORDINATE_DEBUG_VIEW_RESULT.md
```

가능하면 최종 `06_IMPLEMENTATION_STATUS.md`도 함께 가져온다.
