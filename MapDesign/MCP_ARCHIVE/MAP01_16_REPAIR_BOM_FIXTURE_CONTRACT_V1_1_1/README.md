# MAP01_16 Repair v1.1.1 — BOM Fixture Contract

MAP01_16 v1.0의 잘못된 `UNEXPECTED_UTF8_BOM` fixture를 actual BOM-required pipeline에 맞는 `MISSING_UTF8_BOM` fixture로 교정하는 same-task patch다.

v1.1.1은 실제 설치된 Current Task SHA-256 `1e295a82ff4b7d622921f7bbf25f4580fe1e64cb55669d4201da68125f516f25`를 precondition으로 사용한다.

1. Current MAP01_16/BLOCKED state와 기존 Task SHA-256을 검증한다.
2. Task v1.1 payload만 적용한다.
3. `RUN_MAP01_16_PROMPT.md`로 같은 Current Task를 재개한다.

MAP01_17 이후는 계속 LOCKED다.
