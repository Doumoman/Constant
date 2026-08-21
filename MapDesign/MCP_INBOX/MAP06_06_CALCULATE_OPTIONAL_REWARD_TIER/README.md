# MAP06_06 — Calculate Optional Reward Tier

MAP06_05 PASS/finalize 후 MAP06의 여섯 번째 Task만 여는 patch package다. Apply는 Master, Status, `MAP06_06` Task 문서만 설치하고 Assets는 변경하지 않는다.

기준선:

```text
Prior Result: MAP06_05_ASSIGN_ACCESS_RULES_AND_CLUES_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 0f8d8ba09d8c6f36cd75a8bdcdc808eb00bcc1d63031981425a580a64d481630
Previous MAP06_05 Task SHA-256: d80cf04261811777b65b6c99ca8b7ae368fc39f4a895d024c6639ada5226c587
Current MAP06_06 Task SHA-256: 8c8dd6a780b334edf7fb8c1276c1cc5d64332bf26f8c5ab9b69e9dabcb22a542
State after apply: 73 COMPLETE / MAP06_06 CURRENT / 131 LOCKED
```

실행 범위:

- MAP06_04 Type0 snapshot과 MAP06_05 access/clue assignment의 digest chain을 검증.
- 각 optional region에 `depth*2 + tool tier + fuel/10 + hidden difficulty` score를 checked integer로 계산.
- minimum score `0/4/8/12`에 따라 기존 `OptionalRewardTier`의 `Low/Medium/High/Unique` reservation 배정.
- 신규 Runtime production C# 6개, Runtime EditMode test C# 1개 생성.
- 기존 boundary assertions는 MAP06_06 symbols를 허용하고 MAP06_07+만 금지하도록 필요한 파일만 수정 가능.
- 실제 reward ID/item/pool/quantity/spawn, mandatory/core/unique reward, return/inactive/validator/overlay/generated CSV는 구현하지 않음.

Type4 기준은 유지한다: U+D mandatory, L/R independent, `UD/LUD/RUD/LRUD` all legal. attachment boundary base-closed `12`와 MAP06_05 access/clue/cost/preview를 보존한다. Authoring CSV는 원본이므로 수정하지 않는다. `MAP06_07_IMPLEMENT_RETURN_POLICY`는 PASS 전까지 `LOCKED / DO NOT START`다.




