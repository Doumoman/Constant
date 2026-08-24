# CHAR04_02 MCP_INBOX Package

이 ZIP은 `CHAR04_02_IMPLEMENT_STOMP_STUN_REMOVAL_AND_CONTACT_DAMAGE` 하나만 여는 patch package다.

## 사용 방법

1. ZIP을 repo root에 압축 해제한다.
2. `CharacterDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md`를 실행한다.
3. MCP가 `CharacterDesign/MCP/REPORTS/CHAR04_02_IMPLEMENT_STOMP_STUN_REMOVAL_AND_CONTACT_DAMAGE_RESULT.md`를 생성하면 그 파일을 반환한다.

## 적용 범위

```text
CharacterDesign/MCP_INBOX/CHAR04_02_IMPLEMENT_STOMP_STUN_REMOVAL_AND_CONTACT_DAMAGE/
```

적용 후 MCP patch apply가 다음 3개 payload만 `CharacterDesign/MCP/`에 반영한다.

```text
PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
PAYLOAD/06_IMPLEMENTATION_STATUS.md
PAYLOAD/TASKS/CHAR04_02_IMPLEMENT_STOMP_STUN_REMOVAL_AND_CONTACT_DAMAGE.md
```

## 작업 성격

- CHAR04 두 번째 작업.
- 플레이어-적 접촉 판정, stomp, stun, removal, rebound, contact damage candidate 구현.
- 투척 충격·환경 충격·폭탄·로프·체력·HUD는 이후 task 소관.
- CHAR04_03은 별도 패키지 전까지 잠금 유지.

## Expected Report

```text
CharacterDesign/MCP/REPORTS/CHAR04_02_IMPLEMENT_STOMP_STUN_REMOVAL_AND_CONTACT_DAMAGE_RESULT.md
```
