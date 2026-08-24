# START — Character Live Integration

Apply and run one MCP task package.

## Package

```text
CLI/INBOX/L00_01
```

## Apply

Verify:

```text
CLI/INBOX/L00_01/MANIFEST.md
```

Then copy:

```text
CLI/INBOX/L00_01/PAYLOAD/MASTER.md -> CLI/MCP/MASTER.md
CLI/INBOX/L00_01/PAYLOAD/STATUS.md -> CLI/MCP/STATUS.md
CLI/INBOX/L00_01/PAYLOAD/TASK.md -> CLI/MCP/TASKS/L00_01.md
```

Do not edit `Assets/**`, `Packages/**`, `ProjectSettings/**`, `MapDesign/**`, or `CharacterDesign/**` during package apply.

## Run

Execute only:

```text
CLI/MCP/TASKS/L00_01.md
```

Expected output:

```text
CLI/MCP/INPUTS/LIVE_SRC.md
CLI/MCP/REPORTS/L00_01_RESULT.md
```

