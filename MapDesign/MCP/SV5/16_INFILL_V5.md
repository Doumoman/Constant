# SV5_08 — 일반 공간의 실제 셀과 부모 연결

현재 상태: Task 실행 PASS / native Finalize 전. 이 문서 자체는 Finalize 증거가 아니다.
다음 Task는 matching PASS Result와 Finalize가 생기기 전에는 이 후보를 accepted 결과로 소비하지 않는다.

## 사용자 승인 기반의 레거시 비의존 규칙

2026-09-12 사용자 지시에 따라 이 규칙은 SV5_08 및 이후 SV5 생산 구현의 핵심 원칙이다.
SV5 이전 구현과 완료 표시는 정본 구현이 아니며 입력 호환 자료로만 취급한다. 기존 파일과
SOURCE_LOCK 증거는 읽기 전용으로 보존하지만, 새 공간의 생성·셀 소유·이동·밀도 성공 여부를
그 레거시 성공 플래그나 이름·개수 기반 assertion에서 가져오지 않는다.

생산 구현은 실제 624×416 좌표 셀, cardinal AIR 중심선, SOLID 지지면, 2칸 머리 여유,
정확한 개구와 진행 gate를 자체 정본으로 계산한다. 레거시 `Sv5SpaceGraphPlan`은 고정 입력을
새 정본 입력으로 투영하고 결과를 기존 소비자에게 노출하는 경계 어댑터로만 남긴다.
과거 82개 시험은 호환성 회귀이며 새 구현 완료의 충분조건이 아니다. 빈 컬렉션의 `All`,
문자열 존재, 고정 개수 복사 또는 과거 proof 재사용은 새 PASS 근거가 될 수 없다.

후보 생성은 하나의 결정적 탐색을 실제 목표 도달 또는 후보 고갈까지 수행한다. 동일한 전체
월드를 반복 생성해 이전 leaf를 금지하는 방식은 정본 탐색으로 사용하지 않는다. 무거운 기존
contact/FSM product는 accepted 결과의 경계 호환성 검사에서 한 번 수행하며 후보 열거 내부의
성공 판정으로 사용하지 않는다. `ComposedGeometryReady=false`, `PlayerVerified=false`는 유지한다.

## 생산 진입점과 보존

`Sv5SpaceGraphPlanner.PlanWithInfill(core, seed, profile = null, diversity = null, infill = null)`은
기존 `Plan`으로 동일한 SV5_07 기준선을 만든 뒤 typed `Sv5InfillPlan`을 연결한다.
`infill: new Sv5InfillProfile(enabled: false)`는 diversity를 끄지 않고 기존 기준선을 반환한다.
기존 Plan/생성자의 NONE canonical token은 변경하지 않는다.

`Sv5SpaceGraphPlan.Infill`에 실제 셀, 패턴 인스턴스, 부모/host, 개구, 정적 경로와 진단을 보유한다.
`AttachInfill`은 baseline digest 일치 후 새 AIR를 해당 host aperture에 포함하고 contact/projection/
reservation/physical product를 다시 계산한다. 이전 PASS 결과를 새 geometry의 결과로 복사하지 않는다.
기존 core, place, port, ordered centerline, gate geometry/predicate는 보존 대상이다.

## 소유 셀과 연결

EMPTY_ROOM, SMALL_CAVE, LANDING, DEAD_END는 CONTRACT I03의 실제 셀 recipe와 좌우 mirror다.
부모의 실제 사용된 자식 개구만 열고 미사용 출입 후보는 SOLID로 유지한다.
기존 ordinary A~F는 동일한 bounds/port에서 LEGACY_ORDINARY_PORTED로 완성하며 신규 개수에서 제외한다.
기존 incident envelope를 내부에서 AIR로 보존하는 국소 조정은 기존 포트를 이동시키는 권한이 아니다.

신규 후보는 덜 채워진 156×104 구역, 고정 SHA rank, 좌표/recipe 순서로 평가한다.
같은 room rank에 여러 parent aperture가 있을 때 첫 입구만 남겨 다른 합법적 경로를 잃지 않도록
정확한 입구 좌표별 후보를 유지한다. host 이름은 접촉 검사를 면제하는 용도로 쓰지 않는다.
외부 연결은 최대 24개의 cardinal centerline 셀이다. 내부 탐색은 각 열에 실제 바닥과 2칸 AIR를
배치할 수 있는지 확인하며 인접 지지면의 높이 차이는 최대 1이다.
사용자 확인에 따라 +1 계단의 가로/세로 AIR 셀도 각각 합산한다. 바닥 지지점 수로 환산하지 않는다.
전체 witness의 방 내부 끝점과 외부 connector 소유 셀은 구분하며 `ExternalCenterline`에 외부 셀을 명시한다.
기존 passage/clearance/protected AIR에 SOLID를 넣지 않으며 다른 host와의 셀/face 접촉을 거절한다.
기존 예약 및 동일 AIR 공유분은 신규 고유 소유셀 증가량으로 세지 않는다.

수직 actionless host에는 clearance 바깥의 지지된 측면 입구 후보를 둔다.
두 높이의 실제 AIR face가 기존 shaft와 연결되며 host clearance에는 AIR만 추가한다.
`HostAccess`는 기존 centerline에서 새 supported 입구까지의 cardinal AIR 경로이고,
`Path`/정적 witness는 그 입구에서 새 방까지 이어진다. 외부 길이에는 HostAccess의 신규 연결 셀도 포함한다.
이 구분은 기존 미조립 shaft의 Player 이동을 검증했다는 뜻이 아니다.
Type0는 실제 `Core.RouteSource.Secrets`의 봉쇄 chunk 좌표로 보호하며, core 보호의 다른 이름으로 대체하지 않는다.

## 정적 화면 검사와 실제 Player의 구분

`Sv5InfillPatterns.Screen`은 바닥 SOLID, standing AIR 2칸, +1 이동의 0.4×0.8 AABB sweep,
실제 접근/복귀 witness를 계산한다. 임의 속도·중력·점프 궤적은 도입하지 않는다.
모든 셀의 전역 cardinal 통행 검사는 우회 검증이며 중력 기반 Player 완주 증명이 아니다.
`ComposedGeometryReady=false`, `PlayerVerified=false`를 유지한다.

4×4 pattern은 고정 16셀과 16-bit write mask를 가진 신규 INFILL payload다.
mask 밖은 UNKNOWN이며 미소유 잔여를 AIR/SOLID로 채우지 않는다.
ID는 mask+셀의 semantic hash이고 기존 POOL500 ID를 새로 지어내지 않는다.
SV5_41의 합성 및 SV5_44의 실제 Player 검증은 이번 단계에 포함되지 않는다.

## 출력 및 검증 경계

최종 출력은 GENERATED/SV5_08의 default/repeat 각 ON 한 벌과 동일 셀에서 만든 전체 전후 및 A1~D4다.
기존 9개 시험의 쓰기만 이번 `_work/legacy_exports/<fixture>`로 격리했고 과거 입력 읽기는 유지한다.
중간 XML은 `_work`에 있으며 최신 source와 SHA를 대조한 실행만 해당 소스의 증거로 취급한다.
T01~T12, 두 integration 사례, 기존 82개 focused, 전체 SOURCE_LOCK 및 비소유 dirty 대조가
완료되기 전에는 최종 PASS, Finalize, 소유 commit 또는 Review ZIP을 만들지 않는다.

## 최종 실행 결과

seed 1304의 default ON은 신규 213개 방과 64,409개 고유 셀을, repeat ON은 신규 232개 방과
70,266개 고유 셀을 생성했다. 두 사례 모두 최소 128개·최소 고유 면적·분포 조건을 충족했고,
목표 256 미달은 후보를 숨기거나 수치를 낮추지 않고 `CANDIDATES_EXHAUSTED`와 실제 shortfall
(default 43, repeat 24)로 기록했다. 모든 recipe, 기존 ordinary 6개, 부모 연결, 최대 24 AIR
중심선 셀, SOLID 지지면과 2칸 머리 여유를 같은 payload에서 검증했다.

최종 focused EditMode는 기존 82개와 신규 13개를 실제 발견하여 95/95 PASS, Failed 0,
Skipped 0이었다. `focused_results.xml` raw SHA-256은
`21d2d31470b6c9a85a74192e3a1dc142460f707aea1dd6368d5b76dc368f2a34`, 99,364 bytes다.
SOURCE_LOCK의 645개 ALWAYS와 선행 commit blob 41개, 비소유 dirty 기준 373개는 모두 일치했고
이번 임시 `_work`는 최종 증거를 남긴 뒤 삭제했다. 전체 전후 비교와 A1~D4는 확정된 동일
셀 payload에서 생성했다. 결과는 여전히 국소 정적 셀/이동 증거이므로
`ComposedGeometryReady=false`, `PlayerVerified=false`다.
