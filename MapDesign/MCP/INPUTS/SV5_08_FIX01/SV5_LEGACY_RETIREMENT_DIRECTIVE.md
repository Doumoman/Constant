# SV5 레거시 격리·폐기 지시

- `DOCUMENT_TYPE`: `ARCHITECTURE_DIRECTIVE`
- `EXECUTION_AUTHORIZATION`: `NO`
- `CURRENT_CANON`: `SV5`
- `CURRENT_TASK`: `NONE`
- `NEXT_TASK_STATE`: `SV5_09 LOCKED`

## 목적

SV5 이전의 미완성 구현, 폐기된 가정, 복제된 성공 수치, 수동 `PASS` 표시, 구형 planner/export helper/test fixture는 현재 맵 제작의 구현 근거가 아니다. 이러한 자료를 활성 코드와 섞어 읽으면 LLM의 문맥 사용량이 커지고 현재 구조에 대한 추론을 왜곡한다.

앞으로 맵 제작의 생산 정본은 SV5 구현과 그 계약·검증 결과다. 새 구현은 구형 동작을 막연히 보존하거나 재사용하는 대신, 실제 소비자와 호환성 요구가 증명된 부분만 명시적으로 유지한다.

## SV5 생산 정본

다음 의미를 우선한다.

- 624×416 전역 world-coordinate 셀에 실제 지형을 배치한다.
- 4×4 마이크로패턴 경계와 상하좌우 AIR 중심선 셀 수를 실제로 계산한다.
- 통로는 바닥 지지, 착지 공간, 머리 여유, 일반 상승 +1, 점프·매달리기 최대 +2를 고려한다.
- gate는 실제 blocking cell 또는 cardinal blocking face로 검증한다.
- canonical FSM과 실제 physical reachability의 product로 진행 가능성과 우회 차단을 검증한다.
- 좌표 graph 도달, composed geometry 완료, 실제 player 완주 검증을 서로 다른 증거로 기록한다.
- `ComposedGeometryReady=false`, `PlayerVerified=false`인 상태를 추정이나 문자열 표기로 승격하지 않는다.

## 레거시 판정 기준

아래 조건을 모두 확인한 항목만 폐기 후보가 될 수 있다.

1. 동작이 SV5 생산 구현으로 대체되었다.
2. 현재 runtime/editor/test/serialization에서 참조되지 않는다.
3. 남아 있는 목적이 과거 호환 또는 역사 기록뿐이다.
4. 이동해도 활성 Unity assembly, scene, prefab, asset GUID, export 경로가 깨지지 않는다.

후보는 다음 중 하나로 분류한다.

- `ACTIVE_SV5_CANON`: 유지할 생산 정본
- `COMPAT_ADAPTER`: 실제 소비자가 확인된 최소 호환 계층
- `HISTORICAL_EVIDENCE`: 실행 코드와 분리해 보존할 완료 증거
- `RETIRE_CANDIDATE`: 참조가 없고 SV5로 대체된 격리 대상
- `UNKNOWN_BLOCKED`: 의존성 또는 소유권이 불명확하여 이동 금지

## 이 문서가 승인하지 않는 작업

이 문서는 실행 Task가 아니며 파일 이동·삭제를 승인하지 않는다. 다음 작업을 수행하지 않는다.

- SV5_09 등록, stage, Apply 또는 구현 시작
- 코드, scene, prefab, asset, `.meta` 파일의 이동이나 삭제
- `SOURCE_LOCK.json`, `INPUTS/`, `REPORTS/`, `GENERATED/`, 설치된 Task/Archive/Status 및 완료 증거 변경
- 사용자 dirty 파일, 설정 또는 패키지 정리
- Finalize, commit 또는 push

특히 완료된 MCP 증거를 “레거시”라는 이유만으로 폐기하지 않는다. 기존 보고서의 수치나 `PASS` 문자열은 새 구현의 진실로 재사용하지 않되, 감사용 역사 증거로는 원형을 보존한다.

## 실제 격리·폐기 Task의 필수 절차

실제 이동은 별도의 `single_task_v1` Task로 등록하고 정확한 source/destination allowlist를 가져야 한다.

1. 후보 경로와 참조를 전수 조사한다. 코드 검색뿐 아니라 `asmdef`/`asmref`, `.meta` GUID, scene, prefab, serialized asset, reflection, test 및 export helper 참조를 확인한다.
2. 각 항목을 위 다섯 범주로 분류하고 실제 소비자와 참조 수를 기록한다.
3. `RETIRE_CANDIDATE`만 `.meta`와 함께 이동한다. 삭제보다 복구 가능한 격리를 우선한다.
4. 단순히 `Assets` 안의 다른 폴더로 옮기는 것만으로는 컴파일·검색·LLM 문맥에서 제외되지 않을 수 있다. 격리 Task는 비활성 assembly 또는 `Assets` 밖의 명시적 archive 등 실제 제외 효과가 있는 목적지를 정해야 한다.
5. 호환 adapter는 실제 소비자가 있을 때만 남기며, 소비자 없는 wrapper나 중복 abstraction을 새로 만들지 않는다.
6. allowlist 안에서만 참조를 갱신하고, Unity compile과 관련 focused EditMode/PlayMode 및 scene·prefab serialization 검사를 수행한다.
7. old path, new path, 분류 사유, 참조 수, raw SHA-256, bytes, `.meta` GUID, rollback mapping을 manifest로 남긴다.
8. 활성 참조 0과 검증 PASS 후 task-owned atomic commit을 만든다. push는 별도 승인이 있을 때만 한다.

권장 archive 이름은 별도 Task에서 확정한다. 예를 들어 `MapDesign/LEGACY_ARCHIVE/PRE_SV5/` 같은 비활성 위치를 검토할 수 있으나, MCP의 역사 증거와 잠금 파일은 이동 대상이 아니다.

## 즉시 중단 조건

다음 중 하나라도 발생하면 기대값이나 검사를 완화하지 말고 `BLOCKED`로 보고한다.

- `SOURCE_LOCK` 또는 선행 commit 불일치
- serialized GUID나 동적 참조의 소비자 여부가 불명확함
- Task write allowlist 밖 변경이 반드시 필요함
- 무관한 dirty 변경과 실제 충돌함
- 복구 불가능한 데이터 손실 가능성이 있음

## 다음 LLM에 대한 인수 지시

레거시 재사용이나 보존을 제안하기 전에 이 문서를 읽는다. SV5 이전 코드의 이름, 완료 플래그, 보고 수치를 생산 구현의 근거로 간주하지 않는다. 기본 선택은 SV5 API와 최신 계약이며, 구형 코드가 필요하다고 판단하면 추측하지 말고 정확한 consumer, reference, serialization 경로를 제시한다.

실제 레거시 이동은 SV5_09가 아니라 별도의 명시적 격리·폐기 Task로 수행한다. 그 Task가 등록되기 전에는 조사와 제안만 가능하다.

현재 기준 완료 증거:

- SV5_08 commit: `953b833cb22904721f9e8687283a65d5bae64cb7`
- SV5_08 review: `MapDesign/MCP/GENERATED/SV5_08/SV5_08_REVIEW.zip`
- SV5_09: `LOCKED`
