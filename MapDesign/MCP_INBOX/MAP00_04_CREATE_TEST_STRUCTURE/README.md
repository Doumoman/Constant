# MCP PATCH — MAP00_04 CREATE TEST STRUCTURE v1.0

## 목적

광역 WorldGeneration 모듈의 폴더·namespace·assembly 경계를 이후 구현 단계에서도 자동 검증할 수 있도록 기본 EditMode 아키텍처 테스트를 만든다.

이번 패치는 프로덕션 C#, CSV, asmdef, Scene, Prefab 또는 PlayMode 로직을 만들지 않는다.

## 적용 전 조건

```text
MAP00_01_PROJECT_AUDIT = COMPLETE
MAP00_02_FOLDER_AND_ASMDEF_PLAN = COMPLETE
MAP00_03_CREATE_MAP_MODULE_STRUCTURE = COMPLETE
Current Task = NONE
STATUS FINALIZE Upgrade v1.0 installed
```

조건이 다르면 패치를 적용하지 말고 `BLOCKED`로 종료한다.

## 사용 방법

1. ZIP을 압축 해제한다.
2. 생성된 `MAP00_04_CREATE_TEST_STRUCTURE` 폴더 전체를 `MapDesign/MCP_INBOX/` 바로 아래에 넣는다.
3. 최종 경로를 확인한다.

```text
MapDesign/MCP_INBOX/MAP00_04_CREATE_TEST_STRUCTURE/PATCH_MANIFEST.md
```

ZIP 자체를 INBOX에 넣거나 폴더를 이중 중첩하지 않는다.

4. 코딩 에이전트에게 다음 한 줄을 전달한다.

```text
MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.
```

정상 파이프라인:

```text
PATCH APPLY
→ MAP00_04 TASK EXECUTION
→ REPORTS/MAP00_04_CREATE_TEST_STRUCTURE_RESULT.md 생성
→ STATUS FINALIZE
→ MAP00_04 COMPLETE
→ Current Task NONE
→ STOP
```

## 실행 후 가져올 파일

```text
MapDesign/MCP/REPORTS/MAP00_04_CREATE_TEST_STRUCTURE_RESULT.md
```

가능하면 최종 `06_IMPLEMENTATION_STATUS.md`도 함께 가져온다.

