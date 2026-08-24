# CLI MCP Entry

Workflow:

```text
INBOX package -> apply payload -> run current task -> write RESULT -> stop
```

Required upstream anchor:

```text
CharacterDesign/MCP/REPORTS/CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT_RESULT.md
SHA-256: 6efc2ac08d7cb52fd8ba260888310dd403ae64d191767a9338b174a0897fc96c
Required text: STATUS: PASS
Required text: CHARACTER_FINAL_EXIT_DECISION: APPROVED
Required text: Character harness final state: COMPLETE
```

If the anchor is missing or different, write `STATUS: BLOCKED`.

