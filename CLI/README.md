# CLI — Character Live Integration Harness

짧은 경로 버전이다. Windows 압축 해제 경로 제한을 피하기 위해 폴더명과 파일명을 줄였다.

## Start

MCP에는 아래 파일을 기준으로 시작시킨다.

```text
CLI/START.md
```

첫 작업:

```text
CLI/INBOX/L00_01/
```

첫 RESULT:

```text
CLI/MCP/REPORTS/L00_01_RESULT.md
```

## Scope

완료된 `CharacterDesign` 순수 계약을 실제 플레이 가능한 Unity 라이브 계층에 연결한다.

```text
keyboard input
player prefab/runtime composition
spawn request consumer
route/camera request consumer
generated map snapshot adapter
carry/drop/throw/bomb/rope consumers
HUD/presentation binding
PlayMode smoke
build validation
```

