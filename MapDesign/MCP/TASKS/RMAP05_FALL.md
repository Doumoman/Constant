```yaml
mcp_patch:
  format: single_task_v1
  task_id: RMAP05_FALL
  task_file: TASKS/RMAP05_FALL.md
  requires_current_task: NONE
  requires_completed_task: RMAP04_CLIMB
  requires_result:
    path: REPORTS/RMAP04_CLIMB_RESULT.md
    status: PASS
    sha256: 148150921891b00928f71a5b78c96ec693dbcdd44005fbbca27a5aae6ac257c3
  requires_installed_task:
    path: TASKS/RMAP04_CLIMB.md
    sha256: d70088c60a5f8167ba0b6fb306cd20168010008f92db9ceb973977cfbff0b18b
  sets_current_task: RMAP05_FALL
```

# RMAP05_FALL - 낙하 피해와 착지 경직

```text
TASK: RMAP05_FALL
DOCUMENT: v4.2 / RMAP04 PASS 이후 실행 지시서 / 2026-09-08
STATUS: CURRENT
INPUT: MapDesign/MCP_INBOX/RMAP05_FALL.md
EXPECTED_RESULT: MapDesign/MCP/REPORTS/RMAP05_FALL_RESULT.md
NEXT: RMAP06_LOOK
NEXT STATUS: LOCKED / DO NOT START
```

위 STATUS는 정상 Apply 이후의 실행 상태다. 배포된 MD만으로 저장소 상태가 바뀐 것은 아니다.
목표 분량은 약 1~2시간의 기능 책임 단위이며, 300줄은 문서 운영 상한이다.

## 1. User-Facing Goal / 이번 작업의 완료 모습

Unity에서 RMAP05 시험 씬을 열고 Play하면 실제 Player가 낙하 높이에 따라 일반 착지, 착지 경직, 체력 피해, 즉사를 겪는다.
Grab 성립, 사다리/기둥 등반 진입은 낙하 거리를 초기화하며, 해당 상태 자체에는 낙하 피해를 주지 않는다.
큰 낙하 뒤 일방향 발판 상단에 착지하면 피해/경직을 먼저 처리하고 그 뒤 낙하 거리를 초기화한다.
기본 최대 체력은 5이며, 30타일 이상 낙하는 사망 처리다.
주 요구 ID는 P09, P14, P16, P17, P18이다. 아래에 해당 명세의 전체 구현 조건을 포함했다.
RMAP06 Tab 관찰/입력 우선순위, RMAP07 이후 생성기/월드 조립/Full Run은 이번 구현에 넣지 않는다.

## 2. Preflight / 정상 single_task_v1 적용

1. Unity 프로젝트 루트와 적용 AGENTS.md, MapDesign/MCP/00_MCP_ENTRYPOINT.md부터 읽는다.
2. MCP의 Apply, ChangeControl, Finalize 및 RMAP/02_PROTOCOL_V4_2.md를 읽고 위 필드와 경로 규격을 대조한다.
3. 위 메타데이터 내부 경로는 MapDesign/MCP 기준이다. 본문 Assets/MapDesign 경로는 프로젝트 루트 기준이다.
4. 최초 적용은 Current NONE, RMAP01~RMAP04 COMPLETE, RMAP05~19 LOCKED, 미적용 inbox 후보 본 MD 1개를 확인한다.
5. 기준 상태는 240행 = 225 COMPLETE / 0 CURRENT / 15 LOCKED다. 실제 변경 이력이 있으면 근거를 확인한다.
6. RMAP04 설치 Task/Archive의 실제 bytes/SHA와 PASS Result의 TASK/STATUS/SHA를 위 expected 값과 대조한다.
7. RMAP04 Result의 COMMIT 항목은 작업 전 HEAD와 CLI 최종 보고 경로를 적은 보고 형식이다. Result 경로의 git log로 실제 작업 commit을 확인한다.
8. expected Result SHA는 사용자 첨부 원본에서 계산했다. 현지 committed Result bytes와 다르면 자동 재저장으로 통과시키지 않는다.
9. RMAP05 planned 원본 SHA는 `af49159f117994b70cc12883eb4e15e587bc0621837e3a8b1701861fe453cb14`이며, 이 MD는 선행 증거와 RMAP04 바인딩을 반영한 실행 지시서다.
10. 정상 Apply로 RMAP05만 LOCKED->CURRENT, Current NONE->RMAP05_FALL로 열고 Task를 바이트 동일하게 설치/Archive한다.
11. 다른 CURRENT, 선행 내용 변경, 서로 다른 Task/Archive, 미등록 ID/미지원 형식은 적용 전 중단한다. 적용기 규칙을 수정하지 않는다.
12. 같은 RMAP05 CURRENT 재개는 설치 Task/Archive/입력 동일성과 기존 결과를 확인한 뒤 현지 재개 규칙을 따른다.
13. 이미 RMAP05 COMPLETE+유효 PASS이면 기존 결과를 보고하고 STOP한다. 다음 Task를 자동으로 열지 않는다.
14. 무관한 변경은 보존한다. 관련 변경을 분리할 수 없으면 경로와 이유를 보고하며 reset/stash/강제 덮어쓰기를 하지 않는다.

## 3. Read Allowlist / 실제 연결점

- MapDesign/MCP/RMAP/{00_BASELINE_V4_2,01_SEQUENCE_V4_2,02_PROTOCOL_V4_2}.md.
- MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md, 06_IMPLEMENTATION_STATUS.md 및 현지 운영 규칙.
- RMAP04 Task/Archive/Result, MCP/GENERATED/RMAP04의 fixture manifest, 입력/PlayMode/EditMode 결과.
- RMAP03 Task/Archive/Result와 Grab 상태/안전 접점 증거 중 직접 소비에 필요한 범위.
- RMAP02 Task/Archive/Result와 기본 이동/점프/카메라 측정 로그 중 직접 영향 확인에 필요한 범위.
- RMAP01의 file_bindings.csv에서 PLAYER_MOVE/PROFILE/HEALTH/ANIMATION/COLLISION/SCENE 관련 행.
- `CharacterLiveMovementDriver`, `CharacterLiveMovementSettings`, `CharacterLiveGrabSurface`, `CharacterLiveClimbSurface`, `CharacterLiveOneWayPlatform`.
- RMAP03의 `IsGrabbing`, `HasSafeGrabContact`, `LastSafeGrabAnchor`.
- RMAP04의 `IsClimbing`, `HasSafeClimbContact`, `LastSafeClimbAnchor`, `IsDroppingThroughOneWay`, grounded/contact state.
- 기존 Health, damage, death, stun, animation, input-lock 소유자가 있으면 해당 파일과 직접 테스트.

긴 파일은 rg로 심볼을 찾고 필요한 구간으로 나눠 읽는다. 과거 전체 Task/CSV/코드 전수 재감사는 하지 않는다.

| 경로 | RMAP04 상태 | 이번 책임 |
|---|---|---|
| Assets/_Game/Character/Runtime/Input/CharacterInputSnapshot.cs | ADAPT | 경직 중 입력 억제와 기존 held 값 소비 확인 |
| Assets/_Game/Character/Runtime/Movement/UnityPhysics2DCharacterCollisionWorld.cs | ADAPT | grounded/landing 판정과 one-way 착지 구분 확인 |
| Assets/_Game/Live/Runtime/Movement/CharacterLiveMovementSettings.cs | ADAPT | 낙하 경계, 0.5초 경직, 체력 기본값 또는 profile 연결 |
| Assets/_Game/Live/Runtime/Movement/CharacterLiveMovementDriver.cs | ADAPT | fall tracker, landing 처리 순서, Grab/climb reset 통합 |
| Assets/_Game/Live/Runtime/Movement/CharacterLiveGrabSurface.cs | EXISTING | Grab 성립 순간 낙하 거리 초기화 접점 소비 |
| Assets/_Game/Live/Runtime/Movement/CharacterLiveClimbSurface.cs | EXISTING | 등반 진입 순간 낙하 거리 초기화 접점 소비 |
| Assets/_Game/Live/Runtime/Movement/CharacterLiveOneWayPlatform.cs | EXISTING | one-way 착지 피해 우선 처리와 drop-through 상태 확인 |
| Assets/_Game/Map/Scenes/MoonPalace/RMAP04/MoonPalaceClimbOneWay_RMAP04.unity | EXISTING | RMAP05 전용 낙하 씬/fixture의 기준 |
| Assets/_Game/Map/Scenes/MoonPalace/RMAP05/MoonPalaceFallDamage_RMAP05.unity | PROPOSED | 30타일 이상 낙하, Grab/climb/one-way 초기화 시험 씬 |
| Assets/_Game/Live/Runtime/Movement/CharacterLiveFallDamageState.cs | PROPOSED | 기존 Health 소유자와 연결되는 낙하 측정/경직 보조 타입 |

EXISTING/ADAPT는 RMAP04 보고 기준이며 현지 경로와 직접 참조를 대조한다.
Health/Animation 소유자는 먼저 기존 구현을 찾는다. 없을 때만 RMAP05 범위의 최소 Player-local 상태로 만들고 Result에 이유를 적는다.

## 4. Write Allowlist / 변경 경계

- 위 Live Movement/Settings/Input/Player 계층의 낙하 측정, 피해 적용, 경직, 사망, 입력 억제에 필요한 필드와 상태.
- 기존 Health/Damage/Animation/Input-lock 소유자가 있으면 그 공개 API에 맞춘 최소 연결.
- 기존 소유자가 없을 때의 RMAP05 전용 최소 Health/FallDamage state. 공용 전투 Health 시스템으로 확대하지 않는다.
- RMAP03 Grab 성립과 RMAP04 등반 진입을 소비하는 reset 접점. Grab/climb 동작 자체를 재작성하지 않는다.
- one-way 착지 처리 순서와 selected platform drop-through 상태의 필요한 조회. one-way collision 동작을 재튜닝하지 않는다.
- RMAP05 전용 시험 씬, scene builder, fixture manifest, 필요한 Tile/Prefab/Surface marker와 .meta.
- 기존 Test assembly의 RMAP05 전용 EditMode/PlayMode 테스트 및 직접 영향받은 기존 테스트의 최소 수정.
- MapDesign/MCP/GENERATED/RMAP05의 필요한 입력/측정/씬 확인 증거, 지정 Task/Archive/Result와 상태 기록.
- asmdef/asmref는 기존 assembly를 우선 사용한다. 컴파일에 필요한 최소 참조만 추가하고 순환을 만들지 않는다.

RMAP02 이동/점프/카메라, RMAP03 Grab 판정, RMAP04 climb/one-way 통과 수치는 소비 대상이다.
낙하 피해 때문에 run/walk/jump 목표, Grab 허용 거리, climb 속도, one-way 0.18초 ignore를 재튜닝하지 않는다.
RMAP06 관찰 입력 우선순위, 적/함정 피해, 저장/부활/체크포인트, 전투 체력 시스템 확장, 생성 월드 배치는 범위 밖이다.
패키지 추가/업데이트, 전체 설정 재작성, 운영 프로토콜/과거 Task/Result 변경, build 설정 변경도 범위 밖이다.

## 5. P09 / Grab 낙하 거리 초기화

- Grab 성립 순간 낙하 거리를 0으로 초기화한다.
- Grab 자체에 낙하 피해를 주지 않는다. 벽을 잡는 순간 피해/경직/사망을 발생시키면 실패다.
- RMAP03의 실제 `IsGrabbing`, `HasSafeGrabContact`, `LastSafeGrabAnchor` 전환을 소비한다.
- 별도 가짜 Grab 상태나 테스트 전용 reset flag를 만들지 않는다.
- Grab 접촉 전까지 관측한 fall peak/거리와 Grab 성립 후 reset 값을 로그로 남긴다.
- Grab 후 이탈해 다시 떨어지는 경우 reset 이후 높이부터 새 낙하로 측정한다.
- 위험/금지 표면에 Grab하지 못한 경우에는 reset도 발생하지 않아야 한다.

## 6. P14 / 등반축 낙하 거리 초기화

- 사다리 또는 기둥을 잡은 순간 낙하 거리를 초기화한다.
- 실제 등반 진입이 성립하기 전 단순 trigger 영역 접촉으로 초기화하지 않는다.
- RMAP04의 `IsClimbing`, `HasSafeClimbContact`, `LastSafeClimbAnchor` 또는 실제 진입 이벤트를 소비한다.
- Up/Down 입력이 없어 등반하지 않은 접촉, 기둥 꼭대기 비지지 상태, one-way 통과 상태는 climb reset으로 처리하지 않는다.
- 등반축에서 이탈한 뒤 떨어지면 reset 이후 하강 거리부터 새로 측정한다.
- ladder와 pole 양쪽에서 같은 로직을 사용했음을 fixture 좌표와 함께 기록한다.

## 7. P16 / 일방향 착지 처리 순서

- 큰 낙하 뒤 일방향 발판에 착지해도 낙하 피해와 경직을 먼저 처리한다.
- 그 뒤 낙하 거리를 초기화한다. 먼저 초기화해서 피해가 0이 되면 실패다.
- RMAP04의 actual one-way top landing과 selected-platform drop-through 구현을 소비한다.
- Down+Jump drop-through 중 선택 발판을 무시하는 상태는 낙하 피해 판정을 지워서는 안 된다.
- one-way가 아닌 일반 고체 착지와 같은 피해 표를 사용하되, one-way의 top-only 착지 조건을 구분해 증거를 남긴다.
- 착지 직후 0.5초 경직 중 입력 잠금이 있어도 Player가 platform 안으로 재침투하거나 다른 고체를 무시하지 않아야 한다.
- 처리 순서를 로그에 `fallDistance -> damage/stun/death -> reset` 형태로 남긴다.

## 8. P17 / 낙하 피해 표와 기본 체력

| 낙하 거리 | 피해 | 착지 |
|---|---:|---|
| 0 이상 6 미만 | 0 | 일반 착지 |
| 6 이상 10 미만 | 0 | 착지 모션 + 0.5초 경직 |
| 10 이상 15 미만 | 1 | 0.5초 경직 |
| 15 이상 20 미만 | 2 | 0.5초 경직 |
| 20 이상 25 미만 | 3 | 0.5초 경직 |
| 25 이상 30 미만 | 4 | 0.5초 경직 |
| 30 이상 | 즉사 | 사망 처리 |

- 기본 최대 체력은 5다.
- 표의 정수 구간은 위의 연속 거리 구간으로 해석한다. 소수 거리를 먼저 반올림하지 않는다.
- 기존 체력 소유자를 사용하고 중복 Health를 만들지 않는다.
- 기존 체력 소유자가 없으면 RMAP05 범위의 최소 Player health state를 만들고, 이후 전투/저장 체력과 통합할 접점을 Result에 적는다.
- 즉사는 체력 수치와 별도로 death state를 명확히 남긴다. 단순히 Health를 0으로 만들고 계속 조작 가능하게 두면 실패다.
- 6~9.999 구간은 피해 0이어도 착지 경직이 있어야 한다.
- 각 경계 6/10/15/20/25/30 전후의 판정을 focused test 또는 실측 로그로 확인한다.

## 9. P18 / 낙하 측정과 경직 동기화

- 구현 기본안은 마지막 착지/Grab/등반 초기화 이후 관측한 발바닥 최고 높이에서 현재 착지 발바닥 높이까지의 하강 거리다.
- 발바닥 기준은 RMAP02의 0.4x0.8 foot-pivot Player와 같은 기준을 사용한다.
- 하강 거리, 피해, 경직의 소유권과 초기화 시점을 한곳에서 관리한다.
- 0.5초 경직은 fixed-step 기준으로 입력 잠금과 이동 억제가 실제로 재현되어야 한다.
- 경직 중 중력/충돌/카메라 추적은 유지한다. 입력 잠금을 위해 Rigidbody2D나 Collider를 꺼버리지 않는다.
- 6~9타일 착지 모션은 기존 애니메이션이 있으면 연결하고, 없으면 state/log로 명확히 표현한다.
- 기존 Player와 맞지 않는 측정 대안이 필요하면 피해 표는 유지하고, 대안 수식과 이유를 Result에 적는다.
- fall tracker는 생성 검증기나 world bake에 의존하지 않는다. 실제 Player runtime contact를 기준으로 한다.

## 10. 구현 순서 / 한 작업 안에서 완료

1. 정상 Apply와 RMAP04 PASS 체인 확인 후 현재 Player grounded/Grab/climb/one-way 상태 접점을 읽는다.
2. 기존 Health/Animation/Input-lock 소유자를 찾고, 없으면 RMAP05 최소 state의 위치와 공개 범위를 정한다.
3. foot-pivot 최고점 기반 fall tracker와 착지 event를 기존 fixed-step movement에 통합한다.
4. Grab/climb 성립 reset과 one-way 착지 피해 우선 처리 순서를 연결한다.
5. RMAP05 전용 30타일 이상 낙하 fixture와 경계별 측정 fixture를 만든다.
6. focused EditMode/PlayMode 확인으로 실패를 좁게 고친다.
7. 사용자 조작 안내, 파일별 책임, 요구 ID별 증거를 Result에 쓰고 PASS일 때만 Finalize/commit한다.

## 11. Focused Checks / 실제 실행 증거

필요한 새 테스트만 기존 assembly에 둔다. 테스트 수를 고정하거나 문서/메타데이터 숫자를 검사하는 테스트를 추가하지 않는다.
- EditMode: 피해 표 경계, 소수 거리 비교, settings/profile 단일 소유, Health owner 연결을 확인한다.
- PlayMode: 실제 Player/Rigidbody2D/CapsuleCollider2D와 실제 collider 착지로 fallDistance, damage, stun, death를 확인한다.
- P09: 큰 낙하 중 Grab 성립 시 거리 reset, Grab 자체 피해 없음, Grab 후 재낙하 재측정을 확인한다.
- P14: ladder/pole 실제 등반 진입 순간 reset, 단순 trigger 접촉 미reset을 확인한다.
- P16: 큰 낙하 뒤 one-way 상단 착지는 피해/경직 후 reset, drop-through와 다른 고체 충돌 유지를 확인한다.
- P17: 6/10/15/20/25/30 경계 전후, max health 5, 30 이상 즉사를 확인한다.
- P18: foot-pivot 최고점 측정, 0.5초 입력 잠금, 경직 중 충돌/중력/카메라 유지, 기존 animation/state 연결을 확인한다.
- RMAP02/RMAP03/RMAP04의 직접 영향 회귀는 필요한 최소 범위만 재확인하고 이유를 적는다.
- Unity refresh/compile은 수행한다. broad build, legacy 19347, full/unfiltered regression, 불필요한 Player build는 실행하지 않는다.
- 자동 입력은 기존 Input System->snapshot->motor 흐름을 통과시킨다. 초기 배치 외 teleport/검사 marker로 성공을 대신하지 않는다.

Unity 도구 부재/미실행은 NOT RUN 또는 BLOCKED로 기록한다. 과거 PASS, 논리 Bake, Preview를 실제 낙하 피해 증거로 대체하지 않는다.

## 12. Required Outputs / Result

- 실제 Player runtime의 낙하 거리 측정, 피해 표 적용, 착지 경직, death state.
- Grab 성립과 ladder/pole 등반 진입의 낙하 거리 reset.
- one-way 착지의 피해 우선 처리와 drop-through/다른 고체 충돌 유지.
- RMAP05 전용 씬/fixture와 30타일 이상 낙하 및 경계값 측정 증거.
- MapDesign/MCP/GENERATED/RMAP05에는 필요한 입력, 측정, 씬 확인 증거만 저장한다.
- Result에 다음 항목을 포함한다. 실행하지 않은 기능/검사를 PASS로 적지 않는다.

```text
TASK: RMAP05_FALL
STATUS: PASS 또는 FAIL 또는 BLOCKED 또는 STATUS_CONFLICT
USER-FACING IMPLEMENTATION REPORT: 가능한 낙하 피해/경직/사망 조작, 씬 여는 경로, 관찰 방법
RESPONSIBILITY AND FILES: 실제 경로 | 추가/수정 | 책임 | 소유하지 않는 책임
PRECONDITIONS: 현재 HEAD/branch, 선행 Task/Archive/Result SHA와 commit, 적용 전후 상태
CHANGED: 재사용/ADAPT/신규 판단, Health/Animation/Prefab/Scene/assembly 변경 이유
REQUIREMENT EVIDENCE: P09/P14/P16/P17/P18 각각 산출물, 실측/관찰, 판정
VALIDATION: 실제 filter/job/count, Play 확인, 실패/최소 수정, 미실행과 이유
UNITY VISIBLE OUTPUT: 씬, fixture 좌표, fall table, Player health/stun/death 상태 증거
OUT-OF-SCOPE FINDINGS: 관찰 모드/자동 생성/Full Run/전투 체력/저장 부활의 남은 책임
RMAP06 BINDINGS: 확인된 look-mode/input-priority 접점과 API, EXISTING/PROPOSED 구분
FOLLOWUP: 현지 single_task_v1 메타데이터 필드/경로 규칙, 설치 Task의 실제 SHA-256
FINAL EVIDENCE: 필수 미확인 유무, Current/상태, Task와 Archive bytes 동일 여부
NEXT: RMAP06_LOOK LOCKED / NOT STARTED
COMMIT: 작업 전 HEAD, 실제 생성 commit SHA 또는 미생성 이유
```

## 13. PASS / 실패 / Finalize / STOP

P09/P14/P16/P17/P18 구현과 실제 Player/물리/낙하 피해의 필수 확인이 모두 있어야 PASS다.
상태/선행 체인 충돌은 STATUS_CONFLICT, 필수 자료/도구 부재는 BLOCKED, 기능/확인 기준 불충족은 FAIL로 보고한다.
확인된 필수 경로 단절/충돌 실패를 검증 완화나 침묵 수리로 숨기지 않는다. 어려운 배치 자체는 허용하지만 필수 유일 경로 단절은 실패다.
PASS Result 후 기존 Finalize로 RMAP05 CURRENT->COMPLETE, Current->NONE만 수행한다. RMAP06~19는 LOCKED다.
기준선에서 상태는 적용 후 225 COMPLETE / 1 CURRENT / 14 LOCKED, 완료 후 226 COMPLETE / 0 CURRENT / 14 LOCKED다.
Task/Archive/Result/허용된 코드/asset/증거/상태 변경만 commit한다. 무관한 staged 변경을 포함하거나 임의 unstage하지 않는다.
commit 메시지: RMAP05 implement fall damage and landing stun
최종 CLI 보고에는 Result 경로, 실제 commit SHA, Result SHA-256, 설치 Task SHA-256, Current NONE, RMAP06 LOCKED를 적는다.
Git push와 RMAP06 실행 없이 STOP한다.
