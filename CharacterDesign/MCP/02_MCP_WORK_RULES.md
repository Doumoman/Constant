# MCP 작업 규칙

## 단일 작업

- 한 번에 정확히 하나의 작업만 CURRENT다.
- CURRENT가 아닌 TASK를 구현하거나 미리 수정하지 않는다.
- 한 작업에서 다음 작업의 코드를 함께 구현하지 않는다.

## 읽기와 쓰기

- TASK의 READ ALLOWLIST 밖 파일을 읽지 않는다.
- TASK의 WRITE ALLOWLIST 밖 파일을 수정하지 않는다.
- `{{TOKEN}}`이 남아 있는 ALLOWLIST는 실행 가능한 목록이 아니다. OPEN 패치에서 실제 경로로 치환해야 한다.
- 검색 결과가 범위를 벗어나면 즉시 중단한다.

## 결과 게이트

- 구현 후 TASK가 지정한 RESULT 파일을 작성한다.
- 성공 상태는 줄 하나의 정확한 `STATUS: PASS`다.
- 컴파일 실패, 테스트 실패, 범위 불명확은 FAIL 또는 BLOCKED다.
- PASS 결과가 있어도 동일 실행에서 다음 작업을 열지 않는다.

## 패치 분리

1. IMPLEMENT: 코드·테스트·RESULT 작성
2. FINALIZE: 현재 작업을 COMPLETED로 표시하고 커밋 요구사항 검증
3. OPEN: 다음 작업 TASK의 토큰을 실제 경로로 고정하고 CURRENT로 변경

세 단계는 하나의 패치로 합치지 않는다.

## 커밋

- 하네스 설치 패치 적용 중에는 commit/push하지 않는다.
- 구현 결과 PASS 후 FINALIZE 단계에서 변경 내용을 검토한다.
- 작업별 커밋에는 구현 내용과 테스트 결과를 본문에 기록한다.
- push는 사용자가 별도로 지시한 경우에만 수행한다.
