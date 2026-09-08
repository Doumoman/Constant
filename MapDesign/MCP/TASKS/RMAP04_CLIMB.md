```yaml
mcp_patch:
  format: single_task_v1
  task_id: RMAP04_CLIMB
  task_file: TASKS/RMAP04_CLIMB.md
  requires_current_task: NONE
  requires_completed_task: RMAP03_GRAB
  requires_result:
    path: REPORTS/RMAP03_GRAB_RESULT.md
    status: PASS
    sha256: 66293f22ce730ed9b8bdd62cc22818509fda44669372399ef91939bc16c045f9
  requires_installed_task:
    path: TASKS/RMAP03_GRAB.md
    sha256: 87d6c6301c65708b749c2576e06fef2eea6a6b4331cd52ee954602e7db2a9cd0
  sets_current_task: RMAP04_CLIMB
```

# RMAP04_CLIMB - 사다리, 기둥, 일방향 발판

```text
TASK: RMAP04_CLIMB
DOCUMENT: v4.2 / RMAP03 PASS 이후 실행 지시서 / 2026-09-08
STATUS: CURRENT
INPUT: MapDesign/MCP_INBOX/RMAP04_CLIMB.md
EXPECTED_RESULT: MapDesign/MCP/REPORTS/RMAP04_CLIMB_RESULT.md
NEXT: RMAP05_FALL
NEXT STATUS: LOCKED / DO NOT START
```

위 STATUS는 정상 Apply 이후의 실행 상태다. 배포된 MD만으로 저장소 상태가 바뀐 것은 아니다.
목표 분량은 약 1~2시간의 기능 책임 단위이며, 300줄은 문서 운영 상한이다.

## 1. User-Facing Goal / 이번 작업의 완료 모습

Unity에서 RMAP04 시험 씬을 열고 Play하면 같은 실제 Player가 사다리와 기둥 등반축에 진입하고 오르내릴 수 있다.
Up 또는 Down 입력이 있을 때만 등반축에 진입하며, 기본 등반은 빠르고 Shift 등반은 느리다.
좌우+Jump로 등반축에서 이탈하고, 기본 이탈은 약 1.3타일 높이와 최대 약 4타일 수평, Shift 이탈은 약 2타일 수평을 목표로 한다.
일방향 발판은 아래에서 위로 통과하고 상단에는 착지하며, Down+Jump로 0.18초 동안 해당 발판만 drop-through 한다.
일방향 발판은 Grab 대상이 아니다. RMAP03의 Grab 금지 사례가 RMAP04에서 실제 pass-through 발판으로 구현되더라도 Grab 금지는 유지한다.
주 요구 ID는 P11, P12, P13, P15이다. 아래에 해당 명세의 전체 구현 조건을 포함했다.
RMAP05 낙하 피해/경직/사망, RMAP06 관찰 모드, RMAP07 이후 생성기/월드 조립/Full Run은 이번 구현에 넣지 않는다.

## 2. Preflight / 정상 single_task_v1 적용

1. Unity 프로젝트 루트와 적용 AGENTS.md, MapDesign/MCP/00_MCP_ENTRYPOINT.md부터 읽는다.
2. MCP의 Apply, ChangeControl, Finalize 및 RMAP/02_PROTOCOL_V4_2.md를 읽고 위 필드와 경로 규격을 대조한다.
3. 위 메타데이터 내부 경로는 MapDesign/MCP 기준이다. 본문 Assets/MapDesign 경로는 프로젝트 루트 기준이다.
4. 최초 적용은 Current NONE, RMAP01~RMAP03 COMPLETE, RMAP04~19 LOCKED, 미적용 inbox 후보 본 MD 1개를 확인한다.
5. 기준 상태는 240행 = 224 COMPLETE / 0 CURRENT / 16 LOCKED다. 실제 변경 이력이 있으면 근거를 확인한다.
6. RMAP03 설치 Task/Archive의 실제 bytes/SHA와 PASS Result의 TASK/STATUS/SHA를 위 expected 값과 대조한다.
7. RMAP03 Result의 COMMIT 항목은 작업 전 HEAD와 CLI 최종 보고 경로를 적은 보고 형식이다. Result 경로의 git log로 실제 작업 commit을 확인한다.
8. expected Result SHA는 사용자 첨부 원본에서 계산했다. 현지 committed Result bytes와 다르면 자동 재저장으로 통과시키지 않는다.
9. RMAP04 planned 원본 SHA는 `f4ab5838e6ac9a39bdadddf1e26e6868d4d254957dc9bcf827cc5d7f4022e140`이며, 이 MD는 선행 증거와 RMAP03 바인딩을 반영한 실행 지시서다.
10. 정상 Apply로 RMAP04만 LOCKED->CURRENT, Current NONE->RMAP04_CLIMB로 열고 Task를 바이트 동일하게 설치/Archive한다.
11. 다른 CURRENT, 선행 내용 변경, 서로 다른 Task/Archive, 미등록 ID/미지원 형식은 적용 전 중단한다. 적용기 규칙을 수정하지 않는다.
12. 같은 RMAP04 CURRENT 재개는 설치 Task/Archive/입력 동일성과 기존 결과를 확인한 뒤 현지 재개 규칙을 따른다.
13. 이미 RMAP04 COMPLETE+유효 PASS이면 기존 결과를 보고하고 STOP한다. 다음 Task를 자동으로 열지 않는다.
14. 무관한 변경은 보존한다. 관련 변경을 분리할 수 없으면 경로와 이유를 보고하며 reset/stash/강제 덮어쓰기를 하지 않는다.

## 3. Read Allowlist / 실제 연결점

- MapDesign/MCP/RMAP/{00_BASELINE_V4_2,01_SEQUENCE_V4_2,02_PROTOCOL_V4_2}.md.
- MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md, 06_IMPLEMENTATION_STATUS.md 및 현지 운영 규칙.
- RMAP03 Task/Archive/Result, MCP/GENERATED/RMAP03의 fixture manifest, 입력/PlayMode/EditMode 결과.
- RMAP02 Task/Archive/Result, RMAP02 fixture manifest와 이동/카메라 측정 로그 중 직접 영향 확인에 필요한 범위.
- RMAP01의 file_bindings.csv에서 PLAYER_MOVE/PROFILE/OVERLAY/COLLISION/SCENE 관련 행.
- RMAP03가 추가/수정한 `CharacterLiveMovementDriver`, `CharacterLiveMovementSettings`, `CharacterLiveGrabSurface`, `CharacterLiveGrabMovingSolid`.
- `CharacterLiveMovementDriver.IsGrabbing`, `HasSafeGrabContact`, `LastSafeGrabAnchor`와 Player Body/BodyCollider/Input snapshot.
- 실제 Input Actions, ConsumeFixedSnapshot 흐름, Rigidbody2D/CapsuleCollider2D, LayerMask/Physics2D collision ignore 경로.
- 기존 one-way/platform/ladder/pole/surface/overlay 모델이 있으면 해당 파일과 시험 fixture. 없으면 RMAP04 전용 최소 구현만 둔다.

긴 파일은 rg로 심볼을 찾고 필요한 구간으로 나눠 읽는다. 과거 전체 Task/CSV/코드 전수 재감사는 하지 않는다.

| 경로 | RMAP03 상태 | 이번 책임 |
|---|---|---|
| Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab | REUSE | 공용 본체 보존, 씬/test 인스턴스 기준 등반 확인 |
| Assets/_Game/Live/Input/CharacterLiveControls.inputactions | ADAPT | Up/Down/Shift/Jump 조합과 drop-through 입력 확인 |
| Assets/_Game/Live/Runtime/Input/CharacterLiveInputSource.cs | ADAPT | climb/drop-through에 필요한 snapshot 입력을 기존 흐름에 추가 |
| Assets/_Game/Live/Runtime/Player/CharacterLivePlayerRig.cs | EXISTING | Body, BodyCollider, InputSource, Movement 조합 접점 확인 |
| Assets/_Game/Live/Runtime/Movement/CharacterLiveMovementSettings.cs | ADAPT | 등반 속도, Shift 속도, 하강 속도, one-way ignore 0.18초 소유 |
| Assets/_Game/Live/Runtime/Movement/CharacterLiveMovementDriver.cs | ADAPT | 등반 상태, 이탈, one-way 충돌 필터를 기존 fixed-step motor에 통합 |
| Assets/_Game/Live/Runtime/Movement/CharacterLiveGrabSurface.cs | EXISTING | one-way는 Grab 금지로 계속 분류, safe anchor 접점은 소비 가능 |
| Assets/_Game/Map/Scenes/MoonPalace/RMAP03/MoonPalaceCornerGrab_RMAP03.unity | EXISTING | RMAP04 전용 씬/fixture의 기준 또는 회귀 확인 대상 |
| Assets/_Game/Map/Scenes/MoonPalace/RMAP04/MoonPalaceClimbOneWay_RMAP04.unity | PROPOSED | 등반축과 일방향 발판 저장 씬 |
| Assets/_Game/Live/Runtime/Movement/CharacterLiveClimbSurface.cs | PROPOSED | 사다리/기둥 등반축 표면 또는 trigger 분류 |
| Assets/_Game/Live/Runtime/Movement/CharacterLiveOneWayPlatform.cs | PROPOSED | 실제 one-way 발판과 drop-through 대상 관리 |

EXISTING/ADAPT는 RMAP03 보고 기준이며 현지 경로와 직접 참조를 대조한다.
동명 파일이 다른 위치에 있으면 실제 경로와 이유를 Result에 적고, 추측 경로에 새 파일을 만들지 않는다.

## 4. Write Allowlist / 변경 경계

- 위 Live Input/Player/Movement/Settings의 등반, 등반 이탈, one-way, drop-through 구현에 필요한 필드와 상태.
- 필요하면 CharacterLiveClimbSurface, CharacterLiveOneWayPlatform, CharacterLiveClimbState 등 보조 타입을 Live Runtime/Movement에 추가한다.
- 사다리/기둥은 통과형 trigger 또는 현지 기존 overlay로 구현하되 일반 Solid와 분리한다.
- one-way 발판은 일반 고체와 collision 책임을 분리하고, drop-through ignore는 선택된 발판에만 적용한다.
- RMAP04 전용 시험 씬, scene builder, fixture manifest, 필요한 Tile/Prefab/Surface marker와 .meta.
- 기존 Test assembly의 RMAP04 전용 EditMode/PlayMode 테스트 및 직접 영향받은 기존 테스트의 최소 수정.
- MapDesign/MCP/GENERATED/RMAP04의 필요한 입력/측정/씬 확인 증거, 지정 Task/Archive/Result와 상태 기록.
- asmdef/asmref는 기존 assembly를 우선 사용한다. 컴파일에 필요한 최소 참조만 추가하고 순환을 만들지 않는다.

RMAP03 Grab 상태와 safe contact는 소비 대상이다. Grab 금지 표면을 Grab 가능으로 바꾸지 않는다.
RMAP02 run/walk/jump/camera 수치, GeneratedUnityTilemapApplier, RMAP03 moving-safe Grab은 필요 없이 재튜닝하지 않는다.
낙하 피해/경직/사망, 사다리 위 전투/애니메이션, 문/피스톤/전체 플랫폼 콘텐츠 풀, 생성 월드 배치는 범위 밖이다.
패키지 추가/업데이트, 전체 설정 재작성, 운영 프로토콜/과거 Task/Result 변경, build 설정 변경도 범위 밖이다.

## 5. P11 / 등반축 공통 로직과 진입

- 사다리와 기둥은 통과형 등반축이며 같은 Player 로직을 사용한다.
- 등반축 영역 안에서 Up 또는 Down 입력이 있어야 진입한다. 단순 접촉만으로 자동 등반하지 않는다.
- 등반축은 일반 Solid 지형과 분리한다. 등반축 trigger가 벽/바닥 충돌을 잘못 막거나 생성하지 않는다.
- 기둥 꼭대기에는 설 수 없다. 꼭대기에서 발밑 지지를 제공하지 않고, 별도 solid cap을 추가하지 않는다.
- 등반 중에는 중력/수직 속도 처리와 Rigidbody2D 이동을 명확히 소유한다. 기존 motor와 경쟁하는 이동 보정을 만들지 않는다.
- RMAP03 Grab 중 Up/Down 입력으로 등반축에 진입할 수 있는 경우와, 등반축이 없으면 Grab 유지/이탈이 기존대로 남는 경우를 구분한다.
- 등반축 진입/유지/이탈 상태와 RMAP05가 소비할 마지막 안전 접촉 정보를 Result에 적는다.

## 6. P12 / 등반 속도와 하강

- 기본 등반은 빠르고 Shift는 느리다.
- Down으로 빠른 하강이 가능하다. Up/Down 입력이 없을 때의 정지 또는 미끄러짐 정책을 명확히 정한다.
- 구체적인 상승 속도, 하강 속도, Shift 속도는 CharacterLiveMovementSettings 또는 기존 단일 profile에서 소유한다.
- Player와 검증이 같은 값을 읽도록 한다. test 전용 상수나 생성 검증기용 복제 상수를 만들지 않는다.
- 사다리와 기둥은 같은 수치 모델을 쓰되 fixture 이름과 좌표를 구분해 증거를 남긴다.
- 등반 중 벽/천장/one-way/platform과 충돌하는 경우 실제 Collider2D 흐름을 보존한다.
- 속도 실측은 fixedDeltaTime, 입력 조건, 허용오차와 함께 Result에 기록한다.

## 7. P13 / 등반축 점프 이탈

- 좌우+Jump로 등반축에서 이탈한다.
- 기본 이탈은 높이 약 1.3타일, 최대 수평 약 4타일을 목표로 한다.
- Shift 이탈은 높이 약 1.3타일, 수평 약 2타일을 목표로 한다.
- 기존 RMAP02 점프 수치와 공중 제어를 재사용하고, 독립된 또 하나의 점프 튜닝 집합을 만들지 않는다.
- 이탈 뒤 일반 공중 제어를 사용한다. 새 벽차기 상태, 등반 전용 공중 물리, 브레이크 애니메이션을 만들지 않는다.
- 이탈 직후 같은 등반축에 즉시 재진입해 입력이 무효화되지 않도록 짧은 재진입 억제 또는 입력 조건을 둔다.
- 이탈 실측은 같은 높이 착지 기준으로 기본/Shift 수평 거리와 최대 높이를 기록한다.

## 8. P15 / 일방향 발판 행동

- 일방향 발판은 상단 면만 충돌하며 아래에서 위로 통과한다.
- 위에서 내려오면 상단에 착지할 수 있다. 옆면/아랫면에서 일반 solid처럼 막히지 않는다.
- 일방향 발판은 모서리 Grab 대상이 아니다. `CharacterLiveGrabSurface` 또는 동일 분류 경로에서 Grab 금지가 유지되어야 한다.
- Down+Jump로 아래로 통과하며 초기 충돌 무시 시간은 0.18초다.
- ignore는 통과 대상으로 잡은 발판에만 적용한다. 다른 일반 고체, 벽, 천장, moving safe solid까지 무시하지 않는다.
- 0.18초 뒤에는 Player가 발판과 분리되었거나 아래에 있을 때 정상 충돌 후보로 돌아온다. 안쪽에서 끼면 실패로 기록하고 좁게 수리한다.
- drop-through 중에도 카메라 추적, 입력 snapshot, 기존 낙하/공중 제어가 끊기지 않아야 한다.

## 9. 구현 순서 / 한 작업 안에서 완료

1. 정상 Apply와 RMAP03 PASS 체인 확인 후 현재 Player input/motor/contact 흐름을 읽는다.
2. 등반축 surface/trigger 분류와 one-way platform 분류를 기존 surface 체계에 맞춘다.
3. Up/Down/Shift/Jump/drop-through 입력을 snapshot에 필요한 만큼만 추가한다.
4. 등반 enter/hold/move/exit와 jump 이탈을 기존 fixed-step movement에 통합한다.
5. one-way 충돌 필터와 0.18초 선택 발판 ignore를 구현한다.
6. RMAP04 전용 씬/fixture와 focused test를 만들고 실패를 좁게 고친다.
7. 사용자 조작 안내, 파일별 책임, 요구 ID별 증거를 Result에 쓰고 PASS일 때만 Finalize/commit한다.

## 10. Focused Checks / 실제 실행 증거

필요한 새 테스트만 기존 assembly에 둔다. 테스트 수를 고정하거나 문서/메타데이터 숫자를 검사하는 테스트를 추가하지 않는다.
- EditMode: climb surface 분류, ladder/pole 공통 모델, one-way Grab 금지, drop-through 대상 선택을 확인한다.
- PlayMode: 실제 Player/Rigidbody2D/CapsuleCollider2D와 실제 trigger/collider로 등반 enter/hold/move/exit를 확인한다.
- P11: Up/Down 진입, 단순 접촉 미진입, ladder/pole 공통 로직, 기둥 꼭대기 비지지를 확인한다.
- P12: 기본 등반, Shift 느린 등반, Down 빠른 하강, 속도 설정 단일 소유를 확인한다.
- P13: 기본/Shift 좌우+Jump 이탈의 높이와 수평 거리, 이탈 뒤 기존 공중 제어, 즉시 재진입 억제를 확인한다.
- P15: 아래에서 위로 통과, 상단 착지, Down+Jump 0.18초 drop-through, 다른 고체 충돌 유지, Grab 금지를 확인한다.
- RMAP03 Grab의 직접 영향 회귀는 필요한 최소 범위만 재확인하고 이유를 적는다.
- Unity refresh/compile은 수행한다. broad build, legacy 19347, full/unfiltered regression, 불필요한 Player build는 실행하지 않는다.
- 자동 입력은 기존 Input System->snapshot->motor 흐름을 통과시킨다. 초기 배치 외 teleport/검사 marker로 성공을 대신하지 않는다.

Unity 도구 부재/미실행은 NOT RUN 또는 BLOCKED로 기록한다. 과거 PASS, 논리 Bake, Preview를 실제 등반/one-way 증거로 대체하지 않는다.

## 11. Required Outputs / Result

- 실제 Player가 사용하는 사다리/기둥 공통 등반축 로직.
- 기본/Shift 등반 속도, Down 하강, 좌우+Jump 이탈, 이탈 후 기존 공중 제어.
- 실제 one-way 발판의 상향 통과, 상단 착지, Down+Jump 0.18초 drop-through, Grab 금지.
- RMAP04 전용 씬/fixture 또는 RMAP03 기반 확장 씬과 필요한 증거 파일.
- MapDesign/MCP/GENERATED/RMAP04에는 필요한 입력, 측정, 씬 확인 증거만 저장한다.
- Result에 다음 항목을 포함한다. 실행하지 않은 기능/검사를 PASS로 적지 않는다.

```text
TASK: RMAP04_CLIMB
STATUS: PASS 또는 FAIL 또는 BLOCKED 또는 STATUS_CONFLICT
USER-FACING IMPLEMENTATION REPORT: 가능한 등반/발판 조작, 씬 여는 경로, 키 배치, 관찰 방법
RESPONSIBILITY AND FILES: 실제 경로 | 추가/수정 | 책임 | 소유하지 않는 책임
PRECONDITIONS: 현재 HEAD/branch, 선행 Task/Archive/Result SHA와 commit, 적용 전후 상태
CHANGED: 재사용/ADAPT/신규 판단, 공개 API/설정/Prefab/Scene/assembly 변경 이유
REQUIREMENT EVIDENCE: P11/P12/P13/P15 각각 산출물, 실측/관찰, 판정
VALIDATION: 실제 filter/job/count, Play 확인, 실패/최소 수정, 미실행과 이유
UNITY VISIBLE OUTPUT: 씬, fixture 좌표, climb/one-way surface 분류, Player 상태 증거
OUT-OF-SCOPE FINDINGS: 낙하 피해/경직/사망/관찰/자동 생성/Full Run의 남은 책임
RMAP05 BINDINGS: 확인된 fall/damage reset 접점과 API, EXISTING/PROPOSED 구분
FOLLOWUP: 현지 single_task_v1 메타데이터 필드/경로 규칙, 설치 Task의 실제 SHA-256
FINAL EVIDENCE: 필수 미확인 유무, Current/상태, Task와 Archive bytes 동일 여부
NEXT: RMAP05_FALL LOCKED / NOT STARTED
COMMIT: 작업 전 HEAD, 실제 생성 commit SHA 또는 미생성 이유
```

## 12. PASS / 실패 / Finalize / STOP

P11/P12/P13/P15 구현과 실제 Player/물리/등반/one-way의 필수 확인이 모두 있어야 PASS다.
상태/선행 체인 충돌은 STATUS_CONFLICT, 필수 자료/도구 부재는 BLOCKED, 기능/확인 기준 불충족은 FAIL로 보고한다.
확인된 필수 경로 단절/충돌 실패를 검증 완화나 침묵 수리로 숨기지 않는다. 어려운 배치 자체는 허용하지만 필수 유일 경로 단절은 실패다.
PASS Result 후 기존 Finalize로 RMAP04 CURRENT->COMPLETE, Current->NONE만 수행한다. RMAP05~19는 LOCKED다.
기준선에서 상태는 적용 후 224 COMPLETE / 1 CURRENT / 15 LOCKED, 완료 후 225 COMPLETE / 0 CURRENT / 15 LOCKED다.
Task/Archive/Result/허용된 코드/asset/증거/상태 변경만 commit한다. 무관한 staged 변경을 포함하거나 임의 unstage하지 않는다.
commit 메시지: RMAP04 implement climb axes and one-way platforms
최종 CLI 보고에는 Result 경로, 실제 commit SHA, Result SHA-256, 설치 Task SHA-256, Current NONE, RMAP05 LOCKED를 적는다.
Git push와 RMAP05 실행 없이 STOP한다.
