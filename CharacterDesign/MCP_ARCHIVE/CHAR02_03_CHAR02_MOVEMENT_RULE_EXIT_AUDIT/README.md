# CHAR02_03 MCP_INBOX Package

이 ZIP은 `CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT` 하나만 여는 patch package다.

## 사용 방법

1. ZIP을 repo root에 압축 해제한다.
2. `CharacterDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md`를 실행한다.
3. MCP가 `CharacterDesign/MCP/REPORTS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT_RESULT.md`를 생성하면 그 파일을 반환한다.

## 적용 범위

```text
CharacterDesign/MCP_INBOX/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT/
```

적용 후 MCP patch apply가 다음 3개 payload만 `CharacterDesign/MCP/`에 반영한다.

```text
PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
PAYLOAD/06_IMPLEMENTATION_STATUS.md
PAYLOAD/TASKS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT.md
```

## 작업 성격

- CHAR02 이동 문법 exit audit.
- 코드 구현 task가 아니다.
- 결과 리포트만 작성한다.
- CHAR02_02의 코요테 지연 점프 3셀 통과 가능성을 반드시 판정한다.
- 통과하면 CHAR02 EXIT 승인 및 CHAR03_01 별도 patch 진입 가능.
- 실패하면 CHAR03_01은 계속 blocked다.

## Expected Report

```text
CharacterDesign/MCP/REPORTS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT_RESULT.md
```
