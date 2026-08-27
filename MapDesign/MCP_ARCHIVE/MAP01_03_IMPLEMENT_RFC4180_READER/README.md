# MCP PATCH — MAP01_03 IMPLEMENT RFC4180 READER v1.0

## 목적

UTF-8 CSV의 comma, quoted field, escaped quote, CRLF/LF, multiline, BOM을 exact source location과 함께 읽는 generic `Rfc4180CsvReader`를 구현한다. MAP01_02의 dictionary bootstrap importer도 새 reader를 사용하도록 교체한다.

header/required/default validation, PK index, scalar/list parser, FK resolution, Registry는 이번 패치 범위가 아니다.

## MAP01_02 Gate

```text
MAP01_02 STATUS: PASS
Schema catalog: 60 files / 679 columns
Schema tests: 30/30 PASS
Architecture regression: 10/10 PASS
Targeted EditMode: 40/40 PASS
Compile errors / warnings: 0 / 0
Authoring CSV/meta: 50 / 50 unchanged
```

## 적용 전 상태

```text
Current Task = NONE
205개 Task 개별 행
MAP00_01~10, MAP01_01~02 = COMPLETE
MAP01_03 이후 = LOCKED
```

상태를 수동으로 CURRENT로 바꾸지 않는다.

## 사용 방법

1. ZIP을 압축 해제한다.
2. `MAP01_03_IMPLEMENT_RFC4180_READER` 폴더 전체를 `MapDesign/MCP_INBOX/` 바로 아래에 넣는다.
3. `.APPLIED` 없는 패치는 이것 하나만 남긴다.
4. 다음을 전달한다.

```text
MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.
```

정상 종료:

```text
PATCH APPLY → MAP01_03 실행 → Result PASS → STATUS FINALIZE
→ MAP01_03 COMPLETE → Current Task NONE → STOP
```

## 핵심 경계

- syntax reader만 구현한다.
- UTF-8 BOM present/absent는 모두 읽는다.
- invalid UTF-8, UTF-16/32 BOM, bare CR, invalid quote transition은 위치와 함께 실패한다.
- syntax 실패 시 partial records를 publish하지 않는다.
- 기존 dictionary importer의 comma split을 새 reader 호출로 교체한다.
- CSV/schema builder/asmdef/Scene/Prefab/Package/ProjectSettings를 수정하지 않는다.
- MAP01_04를 자동 시작하지 않는다.

## 실행 후 가져올 파일

```text
MapDesign/MCP/REPORTS/MAP01_03_IMPLEMENT_RFC4180_READER_RESULT.md
```
