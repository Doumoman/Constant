# CHAR03_01 Repair MCP_INBOX Package

이 ZIP은 `CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE` BLOCKED 결과를 교정하기 위한 change-control revision package다.

## 사용 방법

1. ZIP을 repo root에 압축 해제한다.
2. `CharacterDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md`를 실행한다.
3. MCP가 `CharacterDesign/MCP/REPORTS/CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE_RESULT.md`를 다시 생성하면 그 파일을 반환한다.

## 적용 범위

```text
CharacterDesign/MCP_INBOX/CHAR03_01_REPAIR_MAP_REFERENCE_GUARD_SCOPE/
```

적용 후 MCP patch apply가 다음 3개 payload를 반영한다.

```text
PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
PAYLOAD/06_IMPLEMENTATION_STATUS.md
PAYLOAD/TASKS/CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE.md
```

## 작업 성격

- 새 CHAR03_02 task를 여는 패키지가 아니다.
- 실패한 CHAR03_01을 CURRENT로 유지한 채 task body를 repair-capable revision으로 교체한다.
- `CharacterGroundProbeTests.cs`의 obsolete map-reference guard 수정 권한만 추가한다.
- `Game.Map.Runtime`만 허용하고 Tilemap/InputSystem/Editor/Test/Legacy 참조 금지는 유지해야 한다.

## Expected Report

```text
CharacterDesign/MCP/REPORTS/CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE_RESULT.md
```
