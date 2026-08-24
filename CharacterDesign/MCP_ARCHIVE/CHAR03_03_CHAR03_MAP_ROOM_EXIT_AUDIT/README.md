# CHAR03_03 MCP_INBOX Package

이 ZIP은 `CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT` 하나만 여는 patch package다.

## 사용 방법

1. ZIP을 repo root에 압축 해제한다.
2. `CharacterDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md`를 실행한다.
3. MCP가 `CharacterDesign/MCP/REPORTS/CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT_RESULT.md`를 생성하면 그 파일을 반환한다.

## 적용 범위

```text
CharacterDesign/MCP_INBOX/CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT/
```

적용 후 MCP patch apply가 다음 3개 payload만 `CharacterDesign/MCP/`에 반영한다.

```text
PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
PAYLOAD/06_IMPLEMENTATION_STATUS.md
PAYLOAD/TASKS/CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT.md
```

## 작업 성격

- CHAR03 종료 감사.
- 구현 task가 아니다.
- MAP coordinate/query, readiness gate, camera-room transition policy, input/velocity KEEP, hysteresis를 통합 감사한다.
- 통과하면 CHAR03 EXIT 승인 및 CHAR04_01 별도 patch 진입 가능.
- CHAR04_01은 자동으로 열지 않는다.

## Expected Report

```text
CharacterDesign/MCP/REPORTS/CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT_RESULT.md
```
