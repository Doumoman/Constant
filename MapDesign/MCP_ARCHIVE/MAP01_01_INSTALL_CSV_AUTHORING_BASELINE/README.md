# MCP RECOVERY PATCH — MAP01_01 INSTALL CSV AUTHORING BASELINE v2.3

## 목적

상태 파일만 먼저 `MAP01_01 = CURRENT`로 바뀌었지만 실제 Task 파일과 입력 패키지가 설치되지 않은 half-applied 상태를 되돌리지 않고 복구한다. 복구 후 Map Package v1.0의 정적 Authoring CSV 49개와 `CSV_DATA_DICTIONARY.csv`를 확정된 WorldGeneration Authoring 폴더에 설치하고, 원본 패키지 검증·바이트 동일성·Unity meta·기존 아키텍처 테스트를 확인한다.

이번 패치는 CSV loader, registry, C#, ScriptableObject, Generated Output을 만들지 않는다.

## MAP00 Exit 및 재발행 source 검수

```text
MAP00_10 STATUS: PASS
MAP00 EXIT: APPROVED
MAP00 targeted EditMode: 53/53 PASS
Compile Errors: 0
Input tree: 64 files
Validator: exit 0 / ERROR 0 / WARNING 10
Dictionary unique file_name: 60
File map: 49 rows / category 6/9/2/5/7/7/3/6/4
Install source missing UTF-8 BOM: 0
Input relative-manifest SHA-256: 2b0d40ea2d67173168b452b722bc6af91268c28636abda6ae3a6a63457e7109e
```

이 v2.3은 half-applied 상태 전용 recovery/resume 패치다. 현재 상태가 이미 `MAP01_01 = CURRENT`라면 v2.2를 다시 적용하지 않고 이 v2.3만 사용한다.

## Recovery 대상 상태

이 패치는 다음 중간 상태에서만 실행한다.

```text
Current Task = TASKS/MAP01_01_INSTALL_CSV_AUTHORING_BASELINE.md
MAP00_01~10 = COMPLETE
205개 Task 개별 행
MAP01_01_INSTALL_CSV_AUTHORING_BASELINE = CURRENT
MAP01_02 이후 = LOCKED
Task destination = ABSENT 또는 payload와 BYTE IDENTICAL
Input destination = ABSENT 또는 payload와 TREE BYTE IDENTICAL
.APPLIED = ABSENT
Assets Authoring CSV = 0
```

v2.3 manifest는 위 half-applied 상태를 exact precondition으로 사용한다. 상태를 `NONE`으로 되돌리지 않고 누락된 Task 파일과 입력 트리를 설치한 뒤 다음 실행 가능 상태를 완성한다.

```text
Current Task = TASKS/MAP01_01_INSTALL_CSV_AUTHORING_BASELINE.md
MAP01_01_INSTALL_CSV_AUTHORING_BASELINE = CURRENT
MAP01_02 이후 = LOCKED
```

status를 수동으로 `NONE` 또는 `LOCKED`로 되돌리지 않는다.

## 적용 전 조건

```text
MAP00_01_PROJECT_AUDIT = COMPLETE
MAP00_02_FOLDER_AND_ASMDEF_PLAN = COMPLETE
MAP00_03_CREATE_MAP_MODULE_STRUCTURE = COMPLETE
MAP00_04_CREATE_TEST_STRUCTURE = COMPLETE
MAP00_05_DEFINE_WORLDGEN_CONSTANTS = COMPLETE
MAP00_06_IMPLEMENT_COORDINATE_VALUE_TYPES = COMPLETE
MAP00_07_IMPLEMENT_COORDINATE_CONVERSIONS = COMPLETE
MAP00_08_CREATE_COORDINATE_TESTS = COMPLETE
MAP00_09_CREATE_COORDINATE_DEBUG_VIEW = COMPLETE
MAP00_10_MAP00_EXIT_AUDIT = COMPLETE / PASS / EXIT APPROVED
MAP01_01_INSTALL_CSV_AUTHORING_BASELINE = CURRENT
MAP01_02 이후 Task = NOT STARTED
Current Task = TASKS/MAP01_01_INSTALL_CSV_AUTHORING_BASELINE.md
Task 파일과 입력 패키지 destination = ABSENT 또는 payload와 바이트 동일
.APPLIED = ABSENT
STATUS FINALIZE Upgrade v1.0 installed
```

조건이 다르면 패치를 적용하지 말고 `BLOCKED`로 종료한다.

## 사용 방법

1. ZIP을 압축 해제한다.
2. 생성된 `MAP01_01_INSTALL_CSV_AUTHORING_BASELINE` 폴더 전체를 `MapDesign/MCP_INBOX/` 바로 아래에 넣는다.
3. 최종 경로를 확인한다.

```text
MapDesign/MCP_INBOX/MAP01_01_INSTALL_CSV_AUTHORING_BASELINE/PATCH_MANIFEST.md
```

ZIP 자체를 INBOX에 넣거나 폴더를 이중 중첩하지 않는다.

4. 코딩 에이전트에게 다음 한 줄을 전달한다.

```text
MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.
```

정상 파이프라인:

```text
PATCH APPLY
→ MAP01_01 TASK EXECUTION
→ REPORTS/MAP01_01_INSTALL_CSV_AUTHORING_BASELINE_RESULT.md 생성
→ STATUS FINALIZE
→ MAP01_01 COMPLETE
→ Current Task NONE
→ STOP
```

## 실행 후 가져올 파일

```text
MapDesign/MCP/REPORTS/MAP01_01_INSTALL_CSV_AUTHORING_BASELINE_RESULT.md
```

가능하면 최종 `06_IMPLEMENTATION_STATUS.md`도 함께 가져온다.
