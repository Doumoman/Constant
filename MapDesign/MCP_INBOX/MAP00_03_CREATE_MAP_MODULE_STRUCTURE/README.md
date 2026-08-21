# MCP PATCH — MAP00_03 v1.0

## 목적

`MAP00_02_FOLDER_AND_ASMDEF_PLAN_RESULT.md`에서 승인된 광역 월드 생성기용 디렉터리 구조만 Unity 프로젝트에 만든다.

이 패치는 C#, CSV, asmdef, Scene, Prefab 또는 실제 생성 로직을 만들지 않는다.

## 적용 전 조건

```text
MAP00_01_PROJECT_AUDIT = COMPLETE
MAP00_02_FOLDER_AND_ASMDEF_PLAN = COMPLETE
Current Task = NONE
STATUS FINALIZE Upgrade v1.0 installed
```

조건이 다르면 패치를 적용하지 말고 `BLOCKED`로 종료한다.

## 사용 방법

1. 이 패치 폴더 전체를 `MapDesign/MCP_INBOX/`에 넣는다.
2. INBOX에 적용되지 않은 패치가 이 패치 하나뿐인지 확인한다.
3. 코딩 에이전트에게 다음 한 줄을 전달한다.

```text
MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.
```

정상 파이프라인:

```text
PATCH APPLY
→ MAP00_03 TASK EXECUTION
→ REPORTS/MAP00_03_CREATE_MAP_MODULE_STRUCTURE_RESULT.md 생성
→ STATUS FINALIZE
→ MAP00_03 COMPLETE
→ Current Task NONE
→ STOP
```

## 실행 후 사용자에게 가져올 파일

```text
MapDesign/MCP/REPORTS/MAP00_03_CREATE_MAP_MODULE_STRUCTURE_RESULT.md
```

가능하면 최종 `06_IMPLEMENTATION_STATUS.md`도 함께 가져오면 상태를 교차 검증할 수 있다.

