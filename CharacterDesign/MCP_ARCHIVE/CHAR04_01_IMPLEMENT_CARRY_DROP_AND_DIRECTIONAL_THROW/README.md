# CHAR04_01 MCP_INBOX Package

이 ZIP은 `CHAR04_01_IMPLEMENT_CARRY_DROP_AND_DIRECTIONAL_THROW` 하나만 여는 patch package다.

## 사용 방법

1. ZIP을 repo root에 압축 해제한다.
2. `CharacterDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md`를 실행한다.
3. MCP가 `CharacterDesign/MCP/REPORTS/CHAR04_01_IMPLEMENT_CARRY_DROP_AND_DIRECTIONAL_THROW_RESULT.md`를 생성하면 그 파일을 반환한다.

## 적용 범위

```text
CharacterDesign/MCP_INBOX/CHAR04_01_IMPLEMENT_CARRY_DROP_AND_DIRECTIONAL_THROW/
```

적용 후 MCP patch apply가 다음 3개 payload만 `CharacterDesign/MCP/`에 반영한다.

```text
PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
PAYLOAD/06_IMPLEMENTATION_STATUS.md
PAYLOAD/TASKS/CHAR04_01_IMPLEMENT_CARRY_DROP_AND_DIRECTIONAL_THROW.md
```

## 작업 성격

- CHAR04 첫 작업.
- Carryable 검색, 단일 휴대 슬롯, 안전 내려놓기, 방향 투척, 소유자 충돌 유예 요청 정책 구현.
- 밟기·피해·충격·폭탄·로프·체력·HUD는 이후 task 소관.
- CHAR04_02는 별도 패키지 전까지 잠금 유지.

## Expected Report

```text
CharacterDesign/MCP/REPORTS/CHAR04_01_IMPLEMENT_CARRY_DROP_AND_DIRECTIONAL_THROW_RESULT.md
```
