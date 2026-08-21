# MCP PATCH — MAP01_11 FOREIGN KEY RESOLVER v1.0

MAP01_10 PASS (`64/64`, targeted `438/438`, full `481/481`, compile/warning `0/0`)를 gate로 schema-declared single/list FK를 49개 static parsed source 전체에서 해석한다.

적용 전: Current Task `NONE`, MAP00_01~10/MAP01_01~10 `COMPLETE`, MAP01_11 이후 `LOCKED`, Task 205개.

1. 폴더를 `MapDesign/MCP_INBOX/` 바로 아래에 넣는다.
2. `.APPLIED` 없는 패치는 이것 하나만 둔다.
3. `MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.`

완료 후 `MapDesign/MCP/REPORTS/MAP01_11_IMPLEMENT_FOREIGN_KEY_RESOLVER_RESULT.md`를 가져온다. MAP01_12는 자동 시작하지 않는다.
