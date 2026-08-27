# RUN MAP09_00R

이 패치 폴더를 `MapDesign/MCP_INBOX/`에 넣고 아래 문장을 Codex CLI에 입력하세요.

```text
MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.
MAP09_00R_INSTALL_SINGLE_MD_INBOX_PROTOCOL 하나만 적용·실행하고,
Result PASS일 때만 Status Finalize와 atomic commit을 수행한 뒤 STOP해.
MAP09_01은 시작하지 말고, 관련 없는 worktree 변경은 건드리거나 stage하지 마.
Git push는 하지 마.
```

## Required Result Header

```text
TASK: MAP09_00R_INSTALL_SINGLE_MD_INBOX_PROTOCOL
STATUS: PASS | FAIL | BLOCKED
MAP09_00R: COMPLETE ELIGIBLE only if PASS
MCP SINGLE MD INBOX: APPROVED only if PASS
MAP09_01_FREEZE_BASELINE_AND_REGISTER_V2_PASSES: LOCKED / DO NOT START
```

## Required State After Apply

```text
215 rows
106 COMPLETE
1 CURRENT
108 LOCKED
Current Task: MAP09_00R_INSTALL_SINGLE_MD_INBOX_PROTOCOL
```

## After PASS

다음 패치부터는 압축 없이 `<TASK_ID>.md` 하나만 `MCP_INBOX`에 넣는다.
