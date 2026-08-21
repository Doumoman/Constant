# RUN MAP06_01 UNITY GATE RESUME

현재 BLOCKED는 코드 실패가 아니라 Unity 실행 환경 문제다. 먼저 Unity MCP가 현재 열린 Editor에 연결되어 있는지 확인하거나, 충돌 중인 Editor를 사용자가 정상 종료한 뒤 같은 프로젝트에서 Test Runner를 실행하라.

Phase A precondition:

```text
Current Task = TASKS/MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS.md
Current Task SHA-256 = c97006b76f8b2c55debc1cb2ef586c9af841de1abe25cbf2ad77aff76d0910b6
Current Result STATUS = BLOCKED
Current Result SHA-256 = 48d979155de5a7aa9bb239fee137590fd54b61f99c56cdc367f273dce99a0b27
```

값이 다르면 `BLOCKED`하고 변경하지 마. MAP06_02 이후 Task body는 읽거나 시작하지 마.

Do not modify code. 허용 write는 현재 Result뿐이다. 모든 gate PASS 후에만 Status finalize를 수행한다.

Required actual gates:

```text
OptionalRegionModelsTests 194/194 PASS
HorizontalBackboneRouterTests 142/142 PASS
MandatoryRouteGraphValidatorTests 298/298 PASS
MandatoryRouteMaskLookupBuilderTests 127/127 PASS
Map05ExitTests 132/132 PASS
UpDownConflictResolverTests 194/194 PASS
VerticalGatewayPlannerTests 156/156 PASS
Existing MAP05 aggregate 1959/1959 PASS
Actually executed total 2153/2153 PASS or higher if discovery count legitimately increases
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
Assets meta 3254
Authoring CSV/meta 50/50
duplicate GUID groups 0
```

Static checks alone cannot produce PASS. Unity/Test Runner가 또 막히면 `STATUS: BLOCKED`로 기록하고 MAP06_01 CURRENT, MAP06_02 LOCKED를 유지한다.

전부 PASS일 때만:

```text
STATUS: PASS
MAP06_01: COMPLETE ELIGIBLE
MAP06_02_ENUMERATE_OPTIONAL_ATTACHMENTS: LOCKED / DO NOT START
```
