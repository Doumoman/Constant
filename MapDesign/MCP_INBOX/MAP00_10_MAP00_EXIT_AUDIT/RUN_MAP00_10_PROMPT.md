# MAP00_10 통합 실행 프롬프트

```text
MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.
```

수동 실행이 필요한 경우:

```text
MapDesign/MCP/00_MCP_ENTRYPOINT.md부터 읽어.
PATCH_MANIFEST.md의 사전 조건과 copy_operations를 검증해 패치를 적용한 뒤,
06_IMPLEMENTATION_STATUS.md의 Current Task인
TASKS/MAP00_10_MAP00_EXIT_AUDIT.md 하나만 수행해.

이 Task는 읽기 전용 MAP00 exit audit이야.
Assets, 기존 코드·테스트·meta·asmdef·CSV·Scene·Prefab·Package·ProjectSettings를
생성·수정·삭제·이동·이름 변경하지 말고 Result 1개만 만들어.

MAP00_01~09 Result chain, 승인 디렉터리 36개, 기존 asmdef 5개,
Runtime C# 6개, Editor C# 2개, MAP00 test C# 8개와 각 meta를 감사해.
기존 targeted EditMode 53개를 다시 실행하고, compile error 0,
잠긴 dimension magic-number 중복 0, Legacy/Stage/P6/P11 dependency 0,
WorldGen/Coordinates 창과 Scene overlay 시각 검증 PASS를 확인해.

모든 gate가 통과했을 때만 STATUS: PASS와 `MAP00 EXIT: APPROVED`를 기록해.
검증이 불가능하면 PASS 대신 BLOCKED, 계약 위반이나 test 실패면 FAIL로 기록해.
STATUS FINALIZE 뒤 Current Task를 NONE으로 만들고 MAP01은 시작하지 마.
기존 MAP01_01 패키지는 HOLD / DO NOT RUN이야.
```
