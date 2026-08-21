# MAP01_01 통합 실행 프롬프트

```text
MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.
```

수동 실행이 필요한 경우:

```text
MapDesign/MCP/00_MCP_ENTRYPOINT.md부터 읽어.
PATCH_MANIFEST.md의 사전 조건과 copy_operations를 검증해 패치를 적용한 뒤,
06_IMPLEMENTATION_STATUS.md의 Current Task인
TASKS/MAP01_01_INSTALL_CSV_AUTHORING_BASELINE.md 하나만 수행해.

이것은 half-applied 상태 전용 recovery v2.3이야.
적용 전 status의 Current Task가 TASKS/MAP01_01_INSTALL_CSV_AUTHORING_BASELINE.md이고
205개 Task가 개별 행으로 있으며 MAP00_01~10은 COMPLETE,
MAP01_01은 CURRENT, MAP01_02 이후는 모두 LOCKED인 것이 정상 precondition이야.
status를 NONE/LOCKED로 되돌리지 마. 누락된 Task 파일과 입력 트리만 manifest에 따라 설치하고,
이미 일부 존재한다면 payload와 바이트 단위로 전부 같을 때만 재사용해.
하나라도 다르면 덮어쓰지 말고 BLOCKED로 종료해.

MAP00_10 Result의 STATUS: PASS와 `MAP00 EXIT: APPROVED`를 확인해.
입력 트리가 정확히 64개이고 aggregate SHA-256이
입력 root 기준 relative-manifest SHA-256이
2b0d40ea2d67173168b452b722bc6af91268c28636abda6ae3a6a63457e7109e이며,
입력 패키지 검증이 exit 0 / ERROR 0 / WARNING 10인지 먼저 확인하고,
정본 Authoring CSV 49개와 CSV_DATA_DICTIONARY.csv만 확정 경로에 바이트 그대로 설치해.
CSV loader, registry, C#, asmdef, ScriptableObject, Generated Output은 만들거나 수정하지 마.

architecture test 10/10, compile error 0, 관련 신규 warning 0까지 확인해.
Result가 PASS일 때만 STATUS FINALIZE를 수행하고 MAP01_02는 시작하지 마.
```
