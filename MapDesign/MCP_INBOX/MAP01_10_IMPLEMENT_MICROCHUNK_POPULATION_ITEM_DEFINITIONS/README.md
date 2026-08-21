# MCP PATCH — MAP01_10 MICROCHUNK POPULATION ITEM DEFINITIONS v1.0

MAP01_09 `48/48`, targeted `374/374`, full `417/417`, compile/warning `0/0` PASS를 gate로 삼아 남은 static definition exact 16개 CSV를 immutable typed definition set으로 변환한다.

적용 전 exact 상태:

```text
Current Task = NONE
MAP00_01~10, MAP01_01~09 = COMPLETE
MAP01_10 이후 = LOCKED
205 Task rows
```

1. ZIP을 풀고 폴더를 `MapDesign/MCP_INBOX/` 바로 아래에 넣는다.
2. `.APPLIED` 없는 패치는 이것 하나만 둔다.
3. `MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.`를 실행한다.

완료 후 `MapDesign/MCP/REPORTS/MAP01_10_IMPLEMENT_MICROCHUNK_POPULATION_ITEM_DEFINITIONS_RESULT.md`를 가져온다. MAP01_11은 자동 시작하지 않는다.
