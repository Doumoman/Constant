# CHAR03_01 MCP_INBOX Package

이 ZIP은 `CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE` 하나만 여는 patch package다.

## 사용 방법

1. ZIP을 repo root에 압축 해제한다.
2. `CharacterDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md`를 실행한다.
3. MCP가 `CharacterDesign/MCP/REPORTS/CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE_RESULT.md`를 생성하면 그 파일을 반환한다.

## 적용 범위

```text
CharacterDesign/MCP_INBOX/CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE/
```

적용 후 MCP patch apply가 다음 3개 payload만 `CharacterDesign/MCP/`에 반영한다.

```text
PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
PAYLOAD/06_IMPLEMENTATION_STATUS.md
PAYLOAD/TASKS/CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE.md
```

## 작업 성격

- CHAR03 첫 작업.
- MAP public coordinate/query contract와 캐릭터 runtime 연결.
- 방 경계 readiness gate 구현.
- 카메라 전환·입력/속도 KEEP 적용 세부·hysteresis는 CHAR03_02 소관.

## Expected Report

```text
CharacterDesign/MCP/REPORTS/CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE_RESULT.md
```
