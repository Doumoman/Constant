# CHAR03_02 MCP_INBOX Package

이 ZIP은 `CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY` 하나만 여는 patch package다.

## 사용 방법

1. ZIP을 repo root에 압축 해제한다.
2. `CharacterDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md`를 실행한다.
3. MCP가 `CharacterDesign/MCP/REPORTS/CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY_RESULT.md`를 생성하면 그 파일을 반환한다.

## 적용 범위

```text
CharacterDesign/MCP_INBOX/CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY/
```

적용 후 MCP patch apply가 다음 3개 payload만 `CharacterDesign/MCP/`에 반영한다.

```text
PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
PAYLOAD/06_IMPLEMENTATION_STATUS.md
PAYLOAD/TASKS/CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY.md
```

## 작업 성격

- CHAR03 두 번째 작업.
- 카메라룸 전환 정책, input KEEP, velocity KEEP, hysteresis 구현.
- 실제 Camera/Cinemachine/Scene/Prefab/Presentation 변경은 금지.
- CHAR03_03 exit audit는 별도 패키지 전까지 잠금 유지.

## Expected Report

```text
CharacterDesign/MCP/REPORTS/CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY_RESULT.md
```
