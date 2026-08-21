# MCP PATCH — MAP01_08 IMPLEMENT BIOME BOUNDARY DEFINITIONS v1.0

## 목적

MAP01_06 successful typed parse 결과에서 biome type/patch rule/boundary profile/pair/chunk catalog exact 5개 CSV를 immutable typed definition set으로 변환한다.

모든 typed column과 source record를 보존하고 안정 정렬한다. FK, pair canonicalization, biome/boundary domain validation은 구현하지 않는다.

## MAP01_07 Gate

```text
MAP01_07 STATUS: PASS
World/route definitions: 59/59 PASS
Scalar/list parser: 97/97 PASS
Primary-key index: 32/32 PASS
Header/field validator: 29/29 PASS
Reader: 31/31 PASS
Schema: 23/23 PASS
Importer: 9/9 PASS
Architecture: 10/10 PASS
Targeted: 290/290 PASS
Full EditMode: 333/333 PASS
Compile errors / warnings: 0 / 0
```

## 적용 전 상태

```text
Current Task = NONE
205개 Task 개별 행
MAP00_01~10, MAP01_01~07 = COMPLETE
MAP01_08 이후 = LOCKED
```

status를 수동으로 CURRENT로 바꾸지 않는다.

## 사용 방법

1. ZIP을 압축 해제한다.
2. `MAP01_08_IMPLEMENT_BIOME_BOUNDARY_DEFINITIONS` 폴더를 `MapDesign/MCP_INBOX/` 바로 아래에 넣는다.
3. `.APPLIED` 없는 패치는 이것 하나만 둔다.
4. 다음을 실행한다.

```text
MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.
```

정상 종료는 MAP01_08 Result PASS와 STATUS FINALIZE 후 `Current Task = NONE`이다.

## 핵심 경계

- exact 5개 CSV와 5개 row definition만 만든다.
- inactive row, optional empty, list order/duplicate를 보존한다.
- biome A/B 방향을 정렬하거나 역방향 pair를 만들지 않는다.
- FK resolve, 가중치 대응, min/max와 후보 검증을 하지 않는다.
- 기존 loader/parser/world-route definitions/CSV를 수정하지 않는다.
- MAP01_09를 자동 시작하지 않는다.

## 실행 후 가져올 파일

```text
MapDesign/MCP/REPORTS/MAP01_08_IMPLEMENT_BIOME_BOUNDARY_DEFINITIONS_RESULT.md
```
