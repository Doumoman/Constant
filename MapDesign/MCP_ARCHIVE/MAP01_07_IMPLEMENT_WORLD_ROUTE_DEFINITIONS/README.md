# MCP PATCH — MAP01_07 IMPLEMENT WORLD ROUTE DEFINITIONS v1.0

## 목적

MAP01_06 successful typed parse 결과에서 world/generation/RNG, route·socket·edge, sector recipe 계열 exact 13개 CSV를 immutable typed definition set으로 변환한다.

모든 column과 source record를 보존하고 CSV row order와 무관하게 안정 정렬한다. FK는 string ID로만 유지하며 domain validation과 Registry는 구현하지 않는다.

## MAP01_06 Gate

```text
MAP01_06 STATUS: PASS
Scalar/list parser: 97/97 PASS
Primary-key index: 32/32 PASS
Header/field validator: 29/29 PASS
Reader: 31/31 PASS
Schema: 23/23 PASS
Importer: 9/9 PASS
Architecture: 10/10 PASS
Targeted: 231/231 PASS
Full EditMode: 274/274 PASS
Compile errors / warnings: 0 / 0
```

## 적용 전 상태

```text
Current Task = NONE
205개 Task 개별 행
MAP00_01~10, MAP01_01~06 = COMPLETE
MAP01_07 이후 = LOCKED
```

status를 수동으로 CURRENT로 바꾸지 않는다.

## 사용 방법

1. ZIP을 압축 해제한다.
2. `MAP01_07_IMPLEMENT_WORLD_ROUTE_DEFINITIONS` 폴더를 `MapDesign/MCP_INBOX/` 바로 아래에 넣는다.
3. `.APPLIED` 없는 패치는 이것 하나만 둔다.
4. 다음을 실행한다.

```text
MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.
```

정상 종료는 MAP01_07 Result PASS와 STATUS FINALIZE 후 `Current Task = NONE`이다.

## 핵심 경계

- exact 13개 CSV와 13개 row definition만 만든다.
- 모든 schema column을 compile-time typed property로 보존한다.
- inactive row와 optional empty도 버리지 않는다.
- FK는 string ID이며 target resolve를 하지 않는다.
- world/route/recipe domain invariant와 16-cell 검증을 하지 않는다.
- reader/schema/validator/PK/parser/importer/CSV를 수정하지 않는다.
- MAP01_08을 자동 시작하지 않는다.

## 실행 후 가져올 파일

```text
MapDesign/MCP/REPORTS/MAP01_07_IMPLEMENT_WORLD_ROUTE_DEFINITIONS_RESULT.md
```
