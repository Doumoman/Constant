```yaml
mcp_patch:
  format: single_task_v1
  task_id: RMAP06_LOOK
  task_file: TASKS/RMAP06_LOOK.md
  requires_current_task: NONE
  requires_completed_task: RMAP05_FALL
  requires_result:
    path: REPORTS/RMAP05_FALL_RESULT.md
    status: PASS
    sha256: 457ae2a27a2aee29ab9fc8e313dfc92ad6a4c76efb85237515327204c260b94a
  requires_installed_task:
    path: TASKS/RMAP05_FALL.md
    sha256: 056c65cdaf0040e99cb7423a43c9bb8579c4fb80735e89d202911c858db669f9
  sets_current_task: RMAP06_LOOK
```

# RMAP06_LOOK - 관찰 카메라와 이동 실험실 통합

```text
TASK: RMAP06_LOOK
DOCUMENT: v4.2 / RMAP05 PASS 이후 실행 지시서 / 2026-09-08 / SHA 검증 대상 정정본 1
STATUS: CURRENT
INPUT: MapDesign/MCP_INBOX/RMAP06_LOOK.md
EXPECTED_RESULT: MapDesign/MCP/REPORTS/RMAP06_LOOK_RESULT.md
NEXT: RMAP07_PATTERNS
NEXT STATUS: LOCKED / DO NOT START
```

위 STATUS는 정상 Apply 이후의 실행 상태다. 배포된 MD만으로 저장소 상태가 바뀐 것은 아니다.
목표 분량은 약 1~2시간의 기능 책임 단위이며, 300줄은 문서 운영 상한이다.

## 1. User-Facing Goal / 이번 작업의 완료 모습

Unity에서 RMAP06 이동 실험실 씬을 열고 Play하면 달리기, 걷기, 점프, Grab, 등반, one-way, 낙하 피해까지 한 곳에서 확인할 수 있다.
Player가 지상 정지, 모서리 Grab 정지, 사다리/기둥 정지 상태일 때 Tab+방향을 1초 유지하면 카메라가 8방향으로 주변을 관찰한다.
관찰 오프셋은 정규화된 방향으로 총 3타일이며, 이동 시간은 0.24초, Tab 또는 방향 해제 후 복귀는 0.18초다.
관찰 중 Player 이동/행동 입력은 잠기고, 월드 가장자리에서는 12x8 시야가 월드 밖을 보지 않도록 clamp된다.
일반 공중, 피격/넉백, 착지 경직, death, 장치/마루 강제 이동 중에는 관찰하지 않는다.
주 요구 ID는 C03, C04, C05, E02이다. 아래에 해당 명세의 전체 구현 조건을 포함했다.
RMAP07 패턴 후보/역할형 라이브러리, 생성 Seed, Full Run은 이번 구현에 넣지 않는다.

## 2. Preflight / 정상 single_task_v1 적용

1. Unity 프로젝트 루트와 적용 AGENTS.md, MapDesign/MCP/00_MCP_ENTRYPOINT.md부터 읽는다.
2. MCP의 Apply, ChangeControl, Finalize 및 RMAP/02_PROTOCOL_V4_2.md를 읽고 위 필드와 경로 규격을 대조한다.
3. 위 메타데이터 내부 경로는 MapDesign/MCP 기준이다. 본문 Assets/MapDesign 경로는 프로젝트 루트 기준이다.
4. 최초 적용은 Current NONE, RMAP01~RMAP05 COMPLETE, RMAP06~19 LOCKED, 미적용 inbox 후보 본 MD 1개를 확인한다.
5. 기준 상태는 240행 = 226 COMPLETE / 0 CURRENT / 14 LOCKED다. 실제 변경 이력이 있으면 근거를 확인한다.
6. RMAP05 설치 Task/Archive의 실제 bytes/SHA와 PASS Result의 TASK/STATUS/SHA를 위 expected 값과 대조한다.
7. RMAP05 Result의 COMMIT 항목은 작업 전 HEAD와 CLI 최종 보고 경로를 적은 보고 형식이다. Result 경로의 git log로 실제 작업 commit을 확인한다.
8. expected Result SHA는 사용자 첨부 원본에서 계산했다. 현지 committed Result bytes와 다르면 자동 재저장으로 통과시키지 않는다.
9. 이번 RMAP06 실행본의 수신 검증값은 이 파일과 함께 전달한 실행 지시문의 SHA-256이다. inbox 파일 전체 원본 bytes의 SHA와 대조한다.
   수정 전 planned 문서는 별도 기획 자료이며, 그 해시는 이번 실행본의 expected SHA가 아니다.
   파일 자체의 expected SHA는 자기참조를 피하기 위해 본문에 넣지 않는다. 외부 전달값이 없거나 불일치하면 적용 전에 중단한다.
   줄바꿈 정규화/재저장으로 해시를 맞추지 않는다. 현지 프로토콜의 별도 manifest/등록 SHA 검증도 유지하며, 충돌 시 해당 경로와 expected/actual을 보고한다.
10. 정상 Apply로 RMAP06만 LOCKED->CURRENT, Current NONE->RMAP06_LOOK으로 열고 Task를 바이트 동일하게 설치/Archive한다.
11. 다른 CURRENT, 선행 내용 변경, 서로 다른 Task/Archive, 미등록 ID/미지원 형식은 적용 전 중단한다. 적용기 규칙을 수정하지 않는다.
12. 같은 RMAP06 CURRENT 재개는 설치 Task/Archive/입력 동일성과 기존 결과를 확인한 뒤 현지 재개 규칙을 따른다.
13. 이미 RMAP06 COMPLETE+유효 PASS이면 기존 결과를 보고하고 STOP한다. 다음 Task를 자동으로 열지 않는다.
14. 무관한 변경은 보존한다. 관련 변경을 분리할 수 없으면 경로와 이유를 보고하며 reset/stash/강제 덮어쓰기를 하지 않는다.

## 3. Read Allowlist / 실제 연결점

- MapDesign/MCP/RMAP/{00_BASELINE_V4_2,01_SEQUENCE_V4_2,02_PROTOCOL_V4_2}.md.
- MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md, 06_IMPLEMENTATION_STATUS.md 및 현지 운영 규칙.
- RMAP05 Task/Archive/Result, MCP/GENERATED/RMAP05의 fixture manifest, 입력/PlayMode/EditMode 결과.
- RMAP02~RMAP04 Task/Archive/Result와 이동/Grab/등반/one-way/카메라 증거 중 E02 표에 필요한 범위.
- RMAP01의 file_bindings.csv에서 CAMERA/INPUT/PLAYER_MOVE/PROFILE/HEALTH/SCENE 관련 행.
- `CharacterLiveInputSource` -> `CharacterInputSnapshot` -> `CharacterLiveMovementDriver` fixed-step path.
- RMAP02의 `CharacterLiveCameraFollowDriver`, 12x8 viewport, world clamp, RMAP02 저장 씬.
- RMAP03의 `IsGrabbing`, `HasSafeGrabContact`, `LastSafeGrabAnchor`, `CharacterLiveGrabSurface`.
- RMAP04의 `IsClimbing`, `HasSafeClimbContact`, `LastSafeClimbAnchor`, `IsDroppingThroughOneWay`.
- RMAP05의 `CharacterLiveFallDamageState.CanAcceptInput`, `IsStunned`, `IsDead`와 input gate.

긴 파일은 rg로 심볼을 찾고 필요한 구간으로 나눠 읽는다. 과거 전체 Task/CSV/코드 전수 재감사는 하지 않는다.

| 경로 | RMAP05 상태 | 이번 책임 |
|---|---|---|
| Assets/_Game/Live/Input/CharacterLiveControls.inputactions | ADAPT | Tab action과 8방향 관찰 입력 배선 |
| Assets/_Game/Character/Runtime/Input/CharacterInputSnapshot.cs | ADAPT | Look held/direction/wait 소비 또는 필요한 입력 상태 전달 |
| Assets/_Game/Live/Runtime/Input/CharacterLiveInputSource.cs | ADAPT | Input System -> snapshot의 관찰 입력과 우선순위 배선 |
| Assets/_Game/Live/Runtime/Movement/CharacterLiveMovementDriver.cs | ADAPT | 관찰 허용 상태, 관찰 중 이동/행동 잠금, 기존 state 소비 |
| Assets/_Game/Live/Runtime/Movement/CharacterLiveFallDamageState.cs | EXISTING | 경직/death/input gate를 관찰 금지 조건으로 소비 |
| Assets/_Game/Live/Runtime/Camera/CharacterLiveCameraFollowDriver.cs | ADAPT | 기존 연속 follow에 look offset, timing, clamp를 추가 |
| Assets/_Game/Map/Scenes/MoonPalace/RMAP05/MoonPalaceFallDamage_RMAP05.unity | EXISTING | RMAP06 실험실 씬의 기준 또는 회귀 확인 대상 |
| Assets/_Game/Map/Scenes/MoonPalace/RMAP06/MoonPalaceMovementLab_RMAP06.unity | PROPOSED | 모든 기본 이동과 관찰을 확인하는 저장 씬 |
| Assets/_Game/Live/Runtime/Camera/CharacterLiveLookModeState.cs | PROPOSED | 관찰 대기/발동/복귀 상태 보조 타입 |

EXISTING/ADAPT는 RMAP05 보고 기준이며 현지 경로와 직접 참조를 대조한다.
동명 파일이 다른 위치에 있으면 실제 경로와 이유를 Result에 적고, 추측 경로에 새 파일을 만들지 않는다.

## 4. Write Allowlist / 변경 경계

- 위 Live Input/Movement/Camera/Settings 계층의 관찰 입력, 관찰 상태, 입력 잠금, 카메라 오프셋, clamp에 필요한 필드와 상태.
- 필요하면 CharacterLiveLookModeState, CharacterLiveLookCameraOffset 또는 유사 보조 타입을 Live Runtime/Camera 또는 Movement에 추가한다.
- RMAP06 전용 이동 실험실 씬, scene builder, fixture manifest, 필요한 marker/prefab/Tile과 .meta.
- 기존 Test assembly의 RMAP06 전용 EditMode/PlayMode 테스트 및 직접 영향받은 기존 테스트의 최소 수정.
- MapDesign/MCP/GENERATED/RMAP06의 필요한 입력/측정/씬 확인 증거, 지정 Task/Archive/Result와 상태 기록.
- asmdef/asmref는 기존 assembly를 우선 사용한다. 컴파일에 필요한 최소 참조만 추가하고 순환을 만들지 않는다.

RMAP02 연속 camera, RMAP03 Grab, RMAP04 climb/one-way, RMAP05 fall damage/input gate는 소비 대상이다.
관찰 때문에 run/walk/jump, Grab 거리, climb 속도, one-way 0.18초, fall damage 표를 재튜닝하지 않는다.
관찰 모드는 world generation, seed, save state, replay, minimap, UI 시스템을 소유하지 않는다.
패키지 추가/업데이트, 전체 설정 재작성, 운영 프로토콜/과거 Task/Result 변경, build 설정 변경도 범위 밖이다.

## 5. C03 / 관찰 카메라 입력과 시간

- Tab+방향 입력을 1초 유지해 발동한다.
- 방향은 상하좌우와 대각선 네 방향을 포함한 8방향이다.
- 대각선은 정규화해 총 이동 거리가 3타일을 넘지 않게 한다.
- 관찰 오프셋 목표는 Player follow 중심에서 3타일이다. 카메라 시야는 계속 12x8이다.
- 관찰 이동 시간 초기안은 0.24초, 해제 후 복귀는 0.18초다.
- Tab 또는 방향 입력이 해제되면 복귀한다. 방향이 바뀌면 현지 UX에 맞게 새 1초 대기 또는 목표 갱신 중 하나를 정하고 Result에 적는다.
- 1초 대기 중에는 아직 look offset을 발동하지 않으며, 대기 입력이 Player 이동/등반과 충돌하지 않게 소비 규칙을 둔다.
- 실제 값, fixed/update 기준, 허용오차, 8방향별 실측 오프셋을 Result에 기록한다.

## 6. C04 / 관찰 허용 상태

- 지상 정지 상태에서만 관찰할 수 있다. 수평 이동/점프/낙하 중에는 관찰하지 않는다.
- 모서리 Grab 정지 상태에서만 관찰할 수 있다. Grab 이탈, Down 억제, moving solid 운반 중 강제 이동이면 관찰하지 않는다.
- 사다리/기둥 정지 상태에서만 관찰할 수 있다. 등반 이동 중, 빠른 하강 중, 등반축 이탈 중에는 관찰하지 않는다.
- 일반 공중, 피격/넉백, 착지 경직, death, 장치나 마루에 의한 강제 이동 중에는 관찰하지 않는다.
- RMAP05의 `CanAcceptInput=false`, `IsStunned=true`, `IsDead=true`는 관찰 금지 조건이다.
- 관찰 허용 여부는 실제 Player runtime state를 본다. 테스트 전용 marker로 허용 상태를 속이지 않는다.
- 허용/금지 상태별 판정 표를 Result에 남긴다.

## 7. C05 / 관찰 입력 소비와 해제

- 관찰 발동 중 Player 이동과 행동을 완전히 잠근다.
- 이동/Jump/Action/Grab/Climb/Drop-through 입력이 관찰 유지 중 실행되지 않아야 한다.
- 관찰 대기 중 Tab+방향 입력은 Player 이동/등반 입력과 충돌하지 않도록 우선순위를 정한다.
- Tab 또는 방향 해제, 허용 상태 상실, stun/death 진입, world clamp 변화는 관찰 해제 또는 복귀로 처리한다.
- 해제 후 0.18초 복귀가 끝나면 기존 연속 Player follow와 입력을 정상 복원한다.
- 월드 가장자리에서 관찰 오프셋을 clamp해 12x8 시야가 월드 밖을 보지 않게 한다.
- 관찰 offset은 저장/생성/월드 상태가 아니다. 카메라 local runtime 상태로만 둔다.
- 기존 CameraRoomDriver snap을 다시 켜지 않는다. RMAP02 follow driver와 경쟁하지 않게 한다.

## 8. E02 / 이동 실험실 전체 확인 목록

- 평지 달리기/걷기, 공중 좌우 제어, 1타일 계단 점프와 1타일 통로.
- 가변 점프, 약 2타일 걷기 점프와 최대 약 4타일 달리기 점프.
- 자동 Grab, 상승 중 금지, Down 억제, 움직이는 안전 고체 Grab.
- 사다리/기둥 진입, 빠른/느린 등반, 빠른 하강, 기본 4타일/Shift 2타일 이탈.
- 일방향 발판 상향 통과, 상단 착지, Down+Jump, Grab 금지.
- 6/10/15/20/25/30타일 낙하, 0.5초 경직, Grab/등반 낙하 거리 초기화.
- 연속 12x8 카메라, Tab 8방향 관찰, 월드 clamp.
- 위 항목의 개별 작업 증거는 RMAP02~05 Result와 GENERATED 로그를 재사용한다.
- RMAP06에서 새로 확인할 것은 관찰과 기존 상태 전환의 교차 부분, 한 씬에서의 연결, 미확인 항목 구분이다.
- 모든 항목을 새 테스트로 다시 작성하거나 모든 생성 Seed에 재실행하지 않는다.

## 9. 구현 순서 / 한 작업 안에서 완료

1. 정상 Apply와 RMAP05 PASS 체인 확인 후 현재 camera/input/movement/fall gate 접점을 읽는다.
2. Tab action과 8방향 입력을 기존 Input System -> snapshot 흐름에 추가한다.
3. 관찰 대기/발동/복귀 상태와 C04 허용 상태 판정을 기존 Player runtime state에 연결한다.
4. RMAP02 CameraFollow에 look offset, timing, release, clamp를 좁게 추가한다.
5. RMAP06 이동 실험실 씬/fixture와 E02 확인표를 구성한다.
6. focused EditMode/PlayMode 확인으로 실패를 좁게 고친다.
7. 사용자 조작 안내, 파일별 책임, 요구 ID별 증거를 Result에 쓰고 PASS일 때만 Finalize/commit한다.

## 10. Focused Checks / 실제 실행 증거

필요한 새 테스트만 기존 assembly에 둔다. 테스트 수를 고정하거나 문서/메타데이터 숫자를 검사하는 테스트를 추가하지 않는다.
- EditMode: 8방향 정규화, 1초 hold threshold, 3타일 offset, 0.24/0.18초 timing, 허용 상태 판정표를 확인한다.
- PlayMode: 실제 Player/Camera/Input System으로 Tab+방향 대기, 발동, 유지, 해제, 복귀를 확인한다.
- C03: 8방향별 3타일 관찰, 대각선 정규화, 1초 미만 미발동, release 복귀를 확인한다.
- C04: 지상 정지/Grab 정지/climb 정지는 허용, 공중/stun/death/착지 경직/강제 이동은 금지한다.
- C05: 관찰 중 이동/행동 잠금, 대기 입력 소비, 해제 후 입력 복원, 월드 가장자리 clamp를 확인한다.
- E02: RMAP02~05의 기존 증거를 표로 재사용하고, RMAP06 실험실에서 교차 상태를 필요한 범위만 확인한다.
- RMAP02~05의 직접 영향 회귀는 필요한 최소 범위만 재확인하고 이유를 적는다.
- Unity refresh/compile은 수행한다. broad build, legacy 19347, full/unfiltered regression, 불필요한 Player build는 실행하지 않는다.
- 자동 입력은 기존 Input System->snapshot->motor/camera 흐름을 통과시킨다. 초기 배치 외 teleport/검사 marker로 성공을 대신하지 않는다.

Unity 도구 부재/미실행은 NOT RUN 또는 BLOCKED로 기록한다. 과거 PASS, 논리 Bake, Preview를 실제 관찰 모드 증거로 대체하지 않는다.

## 11. Required Outputs / Result

- Tab+8방향 1초 hold 관찰, 3타일 offset, 0.24초 이동, 0.18초 복귀.
- 지상/Grab/climb 정지 허용과 공중/stun/death/착지 경직/강제 이동 금지.
- 관찰 중 입력 잠금, release 복귀, world edge clamp, 기존 CameraFollow 복원.
- RMAP06 이동 실험실 씬/fixture와 E02 전체 이동 확인표.
- MapDesign/MCP/GENERATED/RMAP06에는 필요한 입력, 측정, 씬 확인 증거만 저장한다.
- Result에 다음 항목을 포함한다. 실행하지 않은 기능/검사를 PASS로 적지 않는다.

```text
TASK: RMAP06_LOOK
STATUS: PASS 또는 FAIL 또는 BLOCKED 또는 STATUS_CONFLICT
USER-FACING IMPLEMENTATION REPORT: 가능한 관찰 조작, 씬 여는 경로, 키 배치, 관찰 방법
RESPONSIBILITY AND FILES: 실제 경로 | 추가/수정 | 책임 | 소유하지 않는 책임
PRECONDITIONS: 현재 HEAD/branch, 외부 전달 expected SHA와 inbox 실제 SHA, 선행 Task/Archive/Result SHA와 commit, 적용 전후 상태
CHANGED: 재사용/ADAPT/신규 판단, Input/Camera/Movement/Scene/assembly 변경 이유
REQUIREMENT EVIDENCE: C03/C04/C05/E02 각각 산출물, 실측/관찰, 판정
VALIDATION: 실제 filter/job/count, Play 확인, 실패/최소 수정, 미실행과 이유
UNITY VISIBLE OUTPUT: 씬, fixture 좌표, look offset/timing/clamp, Player 상태 증거
OUT-OF-SCOPE FINDINGS: 패턴 후보/자동 생성/Full Run/저장 상태/UI 확장의 남은 책임
RMAP07 BINDINGS: 확인된 pattern/role/microchunk 접점과 API, EXISTING/PROPOSED 구분
FOLLOWUP: 현지 single_task_v1 메타데이터 필드/경로 규칙, 설치 Task의 실제 SHA-256
FINAL EVIDENCE: 필수 미확인 유무, Current/상태, Task와 Archive bytes 동일 여부
NEXT: RMAP07_PATTERNS LOCKED / NOT STARTED
COMMIT: 작업 전 HEAD, 실제 생성 commit SHA 또는 미생성 이유
```

## 12. PASS / 실패 / Finalize / STOP

C03/C04/C05/E02 구현과 실제 Player/Camera/Input의 필수 확인이 모두 있어야 PASS다.
상태/선행 체인 충돌은 STATUS_CONFLICT, 필수 자료/도구 부재는 BLOCKED, 기능/확인 기준 불충족은 FAIL로 보고한다.
확인된 필수 경로 단절/충돌 실패를 검증 완화나 침묵 수리로 숨기지 않는다. 어려운 배치 자체는 허용하지만 필수 유일 경로 단절은 실패다.
PASS Result 후 기존 Finalize로 RMAP06 CURRENT->COMPLETE, Current->NONE만 수행한다. RMAP07~19는 LOCKED다.
기준선에서 상태는 적용 후 226 COMPLETE / 1 CURRENT / 13 LOCKED, 완료 후 227 COMPLETE / 0 CURRENT / 13 LOCKED다.
Task/Archive/Result/허용된 코드/asset/증거/상태 변경만 commit한다. 무관한 staged 변경을 포함하거나 임의 unstage하지 않는다.
commit 메시지: RMAP06 implement look camera and movement lab
최종 CLI 보고에는 Result 경로, 실제 commit SHA, Result SHA-256, 설치 Task SHA-256, Current NONE, RMAP07 LOCKED를 적는다.
Git push와 RMAP07 실행 없이 STOP한다.
