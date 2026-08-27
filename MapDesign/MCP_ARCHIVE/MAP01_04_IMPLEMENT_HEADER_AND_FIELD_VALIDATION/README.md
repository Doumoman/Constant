# MCP PATCH — MAP01_04 IMPLEMENT HEADER AND FIELD VALIDATION v1.0

## 목적

RFC4180 reader 결과와 immutable schema catalog를 대조해 header 누락·추가·중복·순서, field count, required/default를 exact source location으로 검증한다.

typed scalar/list, PK/FK, Registry는 구현하지 않는다.

## MAP01_03 Gate

```text
MAP01_03 STATUS: PASS
Reader: 31/31 PASS
Schema: 23/23 PASS
Importer: 9/9 PASS
Architecture: 10/10 PASS
Targeted: 73/73 PASS
Compile errors / warnings: 0 / 0
```

## 적용 전 상태

```text
Current Task = NONE
205개 Task 개별 행
MAP00_01~10, MAP01_01~03 = COMPLETE
MAP01_04 이후 = LOCKED
```

status를 수동으로 CURRENT로 바꾸지 않는다.

## 사용 방법

1. ZIP을 압축 해제한다.
2. `MAP01_04_IMPLEMENT_HEADER_AND_FIELD_VALIDATION` 폴더를 `MapDesign/MCP_INBOX/` 바로 아래에 넣는다.
3. `.APPLIED` 없는 패치는 이것 하나만 둔다.
4. 다음을 실행한다.

```text
MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.
```

정상 종료는 MAP01_04 Result PASS와 STATUS FINALIZE 후 `Current Task = NONE`이다.

## 핵심 경계

- raw string과 default 적용까지만 한다.
- whitespace를 empty로 취급하지 않는다.
- header 오류가 있으면 unsafe data mapping을 하지 않는다.
- 오류 하나라도 있으면 validated records는 0개다.
- reader/schema/importer/CSV를 수정하지 않는다.
- MAP01_05를 자동 시작하지 않는다.

## 실행 후 가져올 파일

```text
MapDesign/MCP/REPORTS/MAP01_04_IMPLEMENT_HEADER_AND_FIELD_VALIDATION_RESULT.md
```
