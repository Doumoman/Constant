# RUN MAP01_15 REMEDIATION v1.2

entrypoint와 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 MAP01_15 Task, 기존 BLOCKED Result, 보존된 CsvImportReport.json을 읽어라.

Task 하단의 `BLOCKED Remediation v1.2 — Special Map BOOL Materialization`만 추가 실행하라. v1.1 성공 수정은 보존하고 `SpecialMapDefinitions.cs`의 exact 세 public BOOL API/materialization과 허용된 대응 테스트만 교정하라.

full actual reimport, focused/targeted/full tests, Unity/visual/hash evidence가 전부 PASS일 때만 MAP01_15 COMPLETE/Current Task NONE으로 finalize하라. 실패 시 CURRENT/BLOCKED 유지. MAP01_16은 시작하지 마.
