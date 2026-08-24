# CharacterDesign

MAP과 병렬로 진행하는 캐릭터 로직 전용 하네스다. 작업 순서는 압축된 7단계·26개 Task로 유지한다.

## 디렉터리

```text
CharacterDesign/
├─ 01_FIXED_SPEC/
├─ 02_PHASE_ROADMAP/
├─ 03_DATA_SCHEMA/
├─ 04_TEST_FIXTURES/
├─ 05_GENERATED_OUTPUT_SCHEMA/
├─ MCP/
│  ├─ INPUTS/
│  ├─ REPORTS/
│  ├─ TASKS/
│  └─ TEMPLATES/
├─ MCP_INBOX/
└─ MCP_ARCHIVE/
```

## 실행

MCP에는 항상 다음 파일을 시작점으로 지정한다.

```text
CharacterDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md
```

한 번의 실행은 `PATCH APPLY → CURRENT TASK → REPORT → STATUS FINALIZE → STOP`까지만 수행한다. 다음 Task는 새 `MCP_INBOX` 패키지로만 열린다.

## 결과 전달

MCP 실행 후 `CharacterDesign/MCP/REPORTS/<TASK>_RESULT.md` 하나를 assistant에게 전달한다. PASS 판정 전에는 다음 INBOX를 적용하지 않는다.
