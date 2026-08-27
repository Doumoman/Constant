# MCP PATCH — MAP01_09 IMPLEMENT SPECIAL VILLAGE DEFINITIONS v1.0

## 목적

MAP01_06 successful typed parse 결과의 special-map 5개와 village/shop 7개, exact 12개 CSV를 immutable typed definition set으로 변환한다. FK/domain validation은 아직 수행하지 않는다.

## MAP01_08 Gate

```text
MAP01_08 STATUS: PASS
Biome/boundary definitions: 36/36 PASS
World/route definitions: 59/59 PASS
Parser/PK/validator/reader/schema/importer/architecture: ALL PASS
Targeted: 326/326 PASS
Full EditMode: 369/369 PASS
Compile errors / warnings: 0 / 0
```

## 적용 전 상태

```text
Current Task = NONE
205개 Task 개별 행
MAP00_01~10, MAP01_01~08 = COMPLETE
MAP01_09 이후 = LOCKED
```

status를 수동으로 CURRENT로 바꾸지 않는다.

## 사용 방법

1. ZIP을 압축 해제한다.
2. `MAP01_09_IMPLEMENT_SPECIAL_VILLAGE_DEFINITIONS` 폴더를 `MapDesign/MCP_INBOX/` 바로 아래에 넣는다.
3. `.APPLIED` 없는 패치는 이것 하나만 둔다.
4. `MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.`를 실행한다.

정상 종료는 MAP01_09 Result PASS와 STATUS FINALIZE 후 `Current Task = NONE`이다. MAP01_10은 자동 시작하지 않는다.

## 실행 후 가져올 파일

```text
MapDesign/MCP/REPORTS/MAP01_09_IMPLEMENT_SPECIAL_VILLAGE_DEFINITIONS_RESULT.md
```
