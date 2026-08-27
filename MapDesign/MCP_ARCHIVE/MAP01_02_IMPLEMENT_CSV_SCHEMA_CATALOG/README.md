# MCP PATCH — MAP01_02 IMPLEMENT CSV SCHEMA CATALOG v1.0

## 목적

MAP01_01에서 설치한 `CSV_DATA_DICTIONARY.csv`의 60개 파일·679개 열 계약을 immutable `CsvSchemaCatalog`로 읽는 Runtime 모델, Editor bootstrap importer, EditMode tests를 구현한다.

이번 패치는 스키마 사전만 다룬다. 일반 CSV RFC4180 reader, 실제 데이터 행 validation, PK index, scalar/list parser, FK resolution, StaticDataRegistry는 만들지 않는다.

## MAP01_01 Gate

```text
MAP01_01 STATUS: PASS
Authoring CSV: 49 static + 1 dictionary = 50
Source/destination SHA-256: 50/50 identical
UTF-8 BOM: 50/50
.csv.meta: 50/50
GUID duplicate: 0
Architecture EditMode: 10/10 PASS
Compile errors: 0
```

## 적용 전 상태

```text
Current Task = NONE
205개 Task 개별 행
MAP00_01~10 = COMPLETE
MAP01_01_INSTALL_CSV_AUTHORING_BASELINE = COMPLETE
MAP01_02 이후 = LOCKED
```

상태를 수동으로 `CURRENT`로 바꾸지 않는다. PATCH APPLY가 Master·status·Task를 함께 설치하면서 MAP01_02 하나만 연다.

## 사용 방법

1. ZIP을 압축 해제한다.
2. 생성된 `MAP01_02_IMPLEMENT_CSV_SCHEMA_CATALOG` 폴더 전체를 `MapDesign/MCP_INBOX/` 바로 아래에 넣는다.
3. INBOX에는 `.APPLIED`가 없는 패치 폴더가 이것 하나뿐인지 확인한다.

```text
MapDesign/MCP_INBOX/MAP01_02_IMPLEMENT_CSV_SCHEMA_CATALOG/PATCH_MANIFEST.md
```

ZIP 자체를 INBOX에 넣거나 폴더를 이중 중첩하지 않는다.

4. 코딩 에이전트에게 다음 한 줄을 전달한다.

```text
MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.
```

정상 파이프라인:

```text
PATCH APPLY
→ MAP01_02 TASK EXECUTION
→ REPORTS/MAP01_02_IMPLEMENT_CSV_SCHEMA_CATALOG_RESULT.md 생성
→ STATUS FINALIZE
→ MAP01_02 COMPLETE
→ Current Task NONE
→ STOP
```

## 핵심 경계

- production builder에 baseline 수량 `60/679`를 하드코딩하지 않는다.
- dictionary bootstrap importer는 현재 사전의 unquoted 10-field restricted dialect만 읽는다.
- generic RFC4180 처리는 MAP01_03에 남긴다.
- CSV 50개와 meta 50개를 수정하지 않는다.
- 기존 code/test/asmdef/Scene/Prefab/Package/ProjectSettings를 수정하지 않는다.
- MAP01_03을 자동 시작하지 않는다.

## 실행 후 가져올 파일

```text
MapDesign/MCP/REPORTS/MAP01_02_IMPLEMENT_CSV_SCHEMA_CATALOG_RESULT.md
```

가능하면 최종 `06_IMPLEMENTATION_STATUS.md`도 함께 가져온다.
