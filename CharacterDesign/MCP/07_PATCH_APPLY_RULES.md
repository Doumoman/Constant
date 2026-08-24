# MCP Patch Apply Rules v2.0

## 1. 구조

```text
CharacterDesign/
├─ MCP/
├─ MCP_INBOX/
└─ MCP_ARCHIVE/
```

## 2. 적용 전 조건

manifest의 다음 항목을 검증한다.

- `patch_id`
- `requires_status`
- `requires_result`
- `sets_current_task`
- `copy_operations`
- `forbidden_operations`

선행 상태, 결과, SHA-256 중 하나라도 다르면 BLOCKED하고 아무 파일도 바꾸지 않는다.

## 3. 적용

- manifest의 `copy_operations`만 수행한다.
- destination이 기존 파일이면 mode가 `replace`일 때만 교체한다.
- Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용한다.
- PATCH APPLY에서는 Assets, Packages, ProjectSettings, MapDesign, 코드, 테스트를 수정하지 않는다.

## 4. 적용 후 검증

1. 모든 destination이 존재하고 빈 파일이 아니다.
2. 새 `06_IMPLEMENTATION_STATUS.md`의 Current Task가 `sets_current_task`와 일치한다.
3. Current Task 상태가 정확히 CURRENT다.
4. 이전 COMPLETE와 미래 LOCKED 상태가 보존됐다.
5. Task 파일과 Master의 task key가 일치한다.

## 5. Archive

적용 완료 패키지는 가능하면 `MCP_ARCHIVE/<PATCH_ID>/`로 이동한다. 이동이 불확실하면 원래 INBOX 패키지에 `.APPLIED`를 생성한다.

## 6. 실패

어느 검증이라도 실패하면 Task 실행과 STATUS FINALIZE를 금지하고 PATCH APPLY `STATUS: BLOCKED`로 종료한다.
