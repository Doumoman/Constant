# CHAR02_03 Repair MCP_INBOX Package

이 ZIP은 `CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT` 실패를 교정하기 위한 change-control revision package다.

## 사용 방법

1. ZIP을 repo root에 압축 해제한다.
2. `CharacterDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md`를 실행한다.
3. MCP가 `CharacterDesign/MCP/REPORTS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT_RESULT.md`를 다시 생성하면 그 파일을 반환한다.

## 적용 범위

```text
CharacterDesign/MCP_INBOX/CHAR02_03_REPAIR_COYOTE_THREE_CELL_GAP_RULE/
```

적용 후 MCP patch apply가 다음 3개 payload를 반영한다.

```text
PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
PAYLOAD/06_IMPLEMENTATION_STATUS.md
PAYLOAD/TASKS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT.md
```

## 작업 성격

- 새 CHAR03 task를 여는 패키지가 아니다.
- 실패한 CHAR02_03을 CURRENT로 유지한 채 task body를 repair-capable revision으로 교체한다.
- 교정 범위는 Character movement runtime/test에 한정한다.
- 핵심 blocker는 코요테 지연 점프로 동일 높이 3셀 틈이 통과되는 문제다.
- 통과하면 `CHAR02 EXIT: APPROVED`, `CHAR03_01 ENTRY: ELIGIBLE FOR SEPARATE PATCH`를 받아야 한다.

## Expected Report

```text
CharacterDesign/MCP/REPORTS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT_RESULT.md
```
