# MCP PATCH — MAP01_05 IMPLEMENT PRIMARY KEY INDEX v1.0

## 목적

MAP01_04 validated record의 effective raw string으로 파일별 단일·복합 PK를 구조적으로 수집하고 immutable lookup index를 만든다.

중복 키가 있으면 첫 행을 포함한 모든 occurrence의 exact source 위치를 보고하며 partial index를 publish하지 않는다. typed parsing, FK, Registry는 구현하지 않는다.

## MAP01_04 Gate

```text
MAP01_04 STATUS: PASS
Header/field validator: 29/29 PASS
Reader: 31/31 PASS
Schema: 23/23 PASS
Importer: 9/9 PASS
Architecture: 10/10 PASS
Targeted: 102/102 PASS
Compile errors / warnings: 0 / 0
```

## 적용 전 상태

```text
Current Task = NONE
205개 Task 개별 행
MAP00_01~10, MAP01_01~04 = COMPLETE
MAP01_05 이후 = LOCKED
```

status를 수동으로 CURRENT로 바꾸지 않는다.

## 사용 방법

1. ZIP을 압축 해제한다.
2. `MAP01_05_IMPLEMENT_PRIMARY_KEY_INDEX` 폴더를 `MapDesign/MCP_INBOX/` 바로 아래에 넣는다.
3. `.APPLIED` 없는 패치는 이것 하나만 둔다.
4. 다음을 실행한다.

```text
MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.
```

정상 종료는 MAP01_05 Result PASS와 STATUS FINALIZE 후 `Current Task = NONE`이다.

## 핵심 경계

- PK component는 `PrimaryKeyOrder`의 `EffectiveValue` 그대로다.
- key equality/hash는 component별 ordinal이며 delimiter join을 사용하지 않는다.
- duplicate의 첫 행과 모든 후속 행 위치를 한 group으로 보고한다.
- duplicate가 하나라도 있으면 usable/partial index는 없다.
- reader/schema/validator/importer/CSV를 수정하지 않는다.
- MAP01_06을 자동 시작하지 않는다.

## 실행 후 가져올 파일

```text
MapDesign/MCP/REPORTS/MAP01_05_IMPLEMENT_PRIMARY_KEY_INDEX_RESULT.md
```
