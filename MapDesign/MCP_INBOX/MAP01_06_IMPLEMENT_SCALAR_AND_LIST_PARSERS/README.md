# MCP PATCH — MAP01_06 IMPLEMENT SCALAR AND LIST PARSERS v1.0

## 목적

MAP01_04 validated `EffectiveValue`를 MAP01_02의 12개 schema type에 맞는 immutable typed value로 파싱한다. MAP01_05 successful PK index를 gate로 사용한다.

숫자는 invariant culture, Boolean은 exact `0/1`, enum은 ordinal allowed value, list는 `|` split 후 component trim과 empty-item 금지다. 오류가 하나라도 있으면 parsed record를 publish하지 않는다.

## MAP01_05 Gate

```text
MAP01_05 STATUS: PASS
Primary-key index: 32/32 PASS
Header/field validator: 29/29 PASS
Reader: 31/31 PASS
Schema: 23/23 PASS
Importer: 9/9 PASS
Architecture: 10/10 PASS
Targeted: 134/134 PASS
Compile errors / warnings: 0 / 0
```

## 적용 전 상태

```text
Current Task = NONE
205개 Task 개별 행
MAP00_01~10, MAP01_01~05 = COMPLETE
MAP01_06 이후 = LOCKED
```

status를 수동으로 CURRENT로 바꾸지 않는다.

## 사용 방법

1. ZIP을 압축 해제한다.
2. `MAP01_06_IMPLEMENT_SCALAR_AND_LIST_PARSERS` 폴더를 `MapDesign/MCP_INBOX/` 바로 아래에 넣는다.
3. `.APPLIED` 없는 패치는 이것 하나만 둔다.
4. 다음을 실행한다.

```text
MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.
```

정상 종료는 MAP01_06 Result PASS와 STATUS FINALIZE 후 `Current Task = NONE`이다.

## 핵심 경계

- scalar 전체 문자열은 trim/normalize하지 않는다.
- list component에만 trim을 적용하고 empty item을 버리지 않고 오류로 보고한다.
- invalid bool/enum/number/hex/date를 silent default로 바꾸지 않는다.
- FK와 domain range/상호 제약은 검사하지 않는다.
- reader/schema/validator/PK/importer/CSV를 수정하지 않는다.
- MAP01_07을 자동 시작하지 않는다.

## 실행 후 가져올 파일

```text
MapDesign/MCP/REPORTS/MAP01_06_IMPLEMENT_SCALAR_AND_LIST_PARSERS_RESULT.md
```
