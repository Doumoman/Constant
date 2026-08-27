# RUN MAP04_08

MCP control 문서 → Master/Status → 현재 Task → MAP04_07 Result 순으로 읽고 `TASKS/MAP04_08_EXPORT_BIOME_PATCH_RESULTS.md`를 exact 실행하라.

허용된 신규 C#/meta/Result만 만들고 filesystem/generated CSV/기존 Assets를 수정하지 마. focused `>=120`, regressions `290/290`, actual `>=410`, failed/skipped `0/0`; discovery-only targeted/full `>=4912/>=4981`; compile/Console/warning `0/0/0`; final meta `3132`; exact Assets changes `14`를 확인하라.

PASS일 때 MAP04_08만 COMPLETE/Current NONE으로 finalize하고 MAP04_09는 LOCKED로 유지하라. Result는 compact template만 사용하라.
