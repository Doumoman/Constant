# 변경 통제·Git 규칙 v1.2

# 1. Git

PASS Result와 STATUS FINALIZE가 완료된 Task는 종료 전에 반드시 commit한다.

커밋은 다음만 포함한다.
- 해당 Task를 연 patch payload와 `.APPLIED`
- 해당 Task의 allowlist 구현·테스트·matching meta
- 해당 Task Result
- 해당 Task의 status finalize

기존에 존재하던 무관한 uncommitted change는 stage하거나 commit하지 않는다.

커밋 메시지 규칙:
- 제목에 Task ID와 핵심 구현을 명시
- 본문에 구현 내용, 주요 production script, test 실행 수, compile/Console/static gate를 상세히 기록
- 커밋 후 commit SHA와 제목을 종료 보고에 기록

자동 push/branch/reset/rebase/force는 금지한다. push는 사용자의 별도 명시 지시가 있을 때만 수행한다.

FAIL/BLOCKED Task의 부분 작업은 사용자의 별도 지시 없이 자동 commit하지 않는다.

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
