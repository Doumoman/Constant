# RUN MAP07_03 REPAIR

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS.md`, `REPORTS/MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS_RESULT.md`를 순서대로 읽어라.

Exact gates:

```text
MAP07_03 Result STATUS: BLOCKED
MAP07_03 Result SHA-256: e267a5439a52aebda336256e726a9dd8d74f8a3a7317992e24b1da57dc40ab80
Previous MAP07_03 Task SHA-256: 82434805780000e3695cbdda45d5888c4234ba617bdc5bcded843643b4c7aac8
Revised MAP07_03 Task SHA-256: f9aee2e6fe0c0a3222eae894cb562ef2100813c4a91e16461fd03e5d5d4cb170
Current Task: MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS
MAP07_04_IMPLEMENT_SOCKET_EDGE_VALIDATION: LOCKED / DO NOT START
```

Current Task가 MAP07_03이 아니거나 어느 SHA라도 다르면 `BLOCKED`하고 변경하지 마. MAP07_04 이후 Task body는 읽거나 시작하지 마.

이번 repair는 Task 문서 교체만 수행한다. Assets, CSV, Runtime/Test C#, Master, Status는 patch apply 단계에서 변경하지 않는다.

Repair 후 MAP07_03을 같은 단계에서 재실행한다. v1.1 Task의 핵심 차이는 다음뿐이다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkEnums.cs
```

위 파일은 implementation 단계에서 exact 1개 기존 MAP07_01 model-file modification으로 허용된다. 허용 내용은 `MicrochunkObjectOrientation`에 package tokens `L`, `R`, `U`, `D` 대응 directional values를 추가하고 `None`을 보존하는 것뿐이다. 다른 MAP07_01 model semantics는 변경하지 않는다.

전부 PASS일 때만 MAP07_03 COMPLETE/Current Task NONE으로 finalize한다. `MAP07_04_IMPLEMENT_SOCKET_EDGE_VALIDATION`은 LOCKED로 유지하고 자동 시작하지 않는다.
