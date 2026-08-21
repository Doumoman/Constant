# 변경 통제·Git 규칙 v1.1

# 1. Git

자동 commit/push/branch/reset/rebase/force 금지.

# 2. 기존 변경 보호

기존 uncommitted change를 임의로 되돌리지 않는다.

# 3. `06_IMPLEMENTATION_STATUS.md` 수정 권한

일반 TASK EXECUTION:
```text
수정 금지
```

PATCH APPLY:
```text
PATCH_MANIFEST가 replace를 명시한 경우에만 허용
```

STATUS FINALIZE:
```text
08_STATUS_FINALIZE_RULES.md가 허용한 필드만 수정 가능
```

# 4. Source of Truth 변경

고정 월드 크기, Sector/MicroChunk 크기, Route Type, 필수 경로 규칙,
패치 규칙, CSV schema, 생성 순서 핵심 원칙은 임의 변경 금지.
