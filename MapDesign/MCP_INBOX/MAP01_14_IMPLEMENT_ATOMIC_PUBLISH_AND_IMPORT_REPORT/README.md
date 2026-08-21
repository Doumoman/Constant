# MCP PATCH — MAP01_14 ATOMIC PUBLISH AND IMPORT REPORT v1.0

MAP01_13 PASS (hash `54`, targeted `616/616`, full `636/636`, compile/warning `0/0`)를 gate로 Registry+hash snapshot을 원자적 교체하고, ERROR 시 last-good을 보존하며 deterministic `CsvImportReport.json` content를 만든다.

적용 전: Current Task `NONE`, MAP00_01~10/MAP01_01~13 `COMPLETE`, MAP01_14 이후 `LOCKED`, 205 tasks.

폴더를 `MapDesign/MCP_INBOX/` 바로 아래에 넣고 `.APPLIED` 없는 패치는 이것 하나만 둔 뒤 `MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.`

완료 후 `MapDesign/MCP/REPORTS/MAP01_14_IMPLEMENT_ATOMIC_PUBLISH_AND_IMPORT_REPORT_RESULT.md`를 가져온다. MAP01_15는 자동 시작하지 않는다.
