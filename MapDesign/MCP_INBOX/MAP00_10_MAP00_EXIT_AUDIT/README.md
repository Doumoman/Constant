# MCP PATCH — MAP00_10 MAP00 EXIT AUDIT v1.0

## 목적

MAP00_01~09 산출물을 읽기 전용으로 최종 감사하고 MAP00 Phase Gate 통과 여부를 판정한다.

이번 패치는 구현 파일이나 테스트를 만들지 않는다. 기존 targeted EditMode 53개, assembly/namespace 경계, 잠긴 dimension magic-number 중복, Legacy dependency, debug window/Scene overlay, 변경 범위를 재검증하고 Result 1개만 생성한다.

## 적용 전 조건

```text
MAP00_01_PROJECT_AUDIT = COMPLETE / PASS
MAP00_02_FOLDER_AND_ASMDEF_PLAN = COMPLETE / PASS
MAP00_03_CREATE_MAP_MODULE_STRUCTURE = COMPLETE / PASS
MAP00_04_CREATE_TEST_STRUCTURE = COMPLETE / PASS
MAP00_05_DEFINE_WORLDGEN_CONSTANTS = COMPLETE / PASS
MAP00_06_IMPLEMENT_COORDINATE_VALUE_TYPES = COMPLETE / PASS
MAP00_07_IMPLEMENT_COORDINATE_CONVERSIONS = COMPLETE / PASS
MAP00_08_CREATE_COORDINATE_TESTS = COMPLETE / PASS
MAP00_09_CREATE_COORDINATE_DEBUG_VIEW = COMPLETE / PASS
MAP01 이후 Task = NOT STARTED
Current Task = NONE
STATUS FINALIZE Upgrade v1.0 installed
```

기존 `MAP01_01_INSTALL_CSV_AUTHORING_BASELINE` 패치는 HOLD다. MAP00_10 PASS 뒤에도 자동 실행하지 않으며 최신 프로젝트 상태에 맞춰 별도 재검증·재발행한다.

## 정확한 폴더명

```text
MAP00_10_MAP00_EXIT_AUDIT
```

folder name, `patch_id`, Task ID가 모두 위 문자열과 같아야 한다.

## 사용 방법

1. ZIP을 압축 해제한다.
2. `MCP_INBOX`에 다른 미적용 패치가 없음을 확인한다.
3. 생성된 폴더 전체를 `MapDesign/MCP_INBOX/` 바로 아래에 넣는다.
4. 최종 경로를 확인한다.

```text
MapDesign/MCP_INBOX/MAP00_10_MAP00_EXIT_AUDIT/PATCH_MANIFEST.md
```

ZIP 자체를 INBOX에 넣거나 폴더를 이중 중첩하지 않는다.

5. 코딩 에이전트에게 다음 한 줄을 전달한다.

```text
MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.
```

정상 파이프라인:

```text
PATCH APPLY
→ MAP00_10 READ-ONLY EXIT AUDIT
→ REPORTS/MAP00_10_MAP00_EXIT_AUDIT_RESULT.md 생성
→ STATUS FINALIZE
→ MAP00_10 COMPLETE
→ Current Task NONE
→ MAP01 자동 시작 없이 STOP
```

## 실행 후 가져올 파일

```text
MapDesign/MCP/REPORTS/MAP00_10_MAP00_EXIT_AUDIT_RESULT.md
```

가능하면 최종 `06_IMPLEMENTATION_STATUS.md`도 함께 가져온다.
