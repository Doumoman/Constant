# TOOL-13 회귀 테스트·승인 보고서

- 작성일: 2026-08-04 KST
- Unity: 6000.3.8f1
- 상태: 자동 회귀 승인 완료 / 수동 시나리오와 Development Player 성능 승인 대기

## 적용 기준

이 보고서는 아래 3개 승인 문서만 기준으로 작성했다.

1. `별을_물어오는_밤_통합구현_기준서_코어흐름_UI_서사_v2.1.md`
2. `별을_물어오는_밤_1x1타일_맵요소_맵배치에디터_구현하네스_v1.0.md`
3. `별을_물어오는_밤_1x1타일_도구_물체상호작용_구현하네스_v1.0.md`

## 자동 회귀 결과

| 구분 | 결과 | 비고 |
|---|---:|---|
| EditMode 통합 회귀 | 125 / 125 통과 | 7.6559초, 실패·스킵 0 |
| PlayMode 핵심 물리·전환 | 7 / 7 통과 | 5.6673초, 실패·스킵 0 |
| CriticalCarry Void 복구 | 1 / 1 통과 | 0.8초 복구 후 활성·위치·물리 복원 확인 |
| 맵 시드 검증 | 500 / 500 통과 | 고유 해시 500, 37.5628ms |
| 시드 구조 실패 | 0 | 외곽·바닥·포털·메인 경로·마루 경로 모두 0 |
| 컴파일 오류 | 0 | 최종 스크립트 컴파일 기준 |

PlayMode 러너 초기화가 한 차례 시간 초과됐으나 테스트 본문은 시작되지 않은 상태였다. 에디터를 정상 씬으로 복구한 뒤 같은 CriticalCarry 테스트를 재실행해 1 / 1 통과했다.

## 최종 15개 기준 대조

| # | 승인 상태 | 구현·검증 근거 |
|---:|---|---|
| 1 | 자동 통과 | Router 우선순위, Action ID, X·Z·C 단일 발행 회귀 |
| 2 | 자동 통과 | Safe Cell 후보, Portal Gap·Void 거부, Heavy 1×2 Clearance |
| 3 | 자동 통과 | 도구 Snapshot 복원, Heavy 실제 포털 운반, 100회 방 왕복 |
| 4 | 자동 통과 | 폭탄 3×3 제한과 UnbreakableBoundary·PortalBoundary 무반응 |
| 5 | 자동 통과 | Rope Anchor·Segment의 RoomBounds 외부 생성 거부 |
| 6 | 자동 통과 | 곡괭이·삽·절굿공이의 빈 셀·거부 반응 자원 미소모 |
| 7 | 자동 통과 | 물뿌리 3셀 순서 처리와 Solid 이후 전파 중단 |
| 8 | 자동 통과 | Hook Pull 안전 위치 정지, 사거리·Portal 차폐 거부 |
| 9 | 자동 통과 | Laser 무반사, Deflectable 지정 투사체만 반사 |
| 10 | 자동 통과 | CriticalCarry가 Void 진입 후 사라지지 않고 0.8초 뒤 Anchor 복귀 |
| 11 | 자동 통과 | Tool Interaction Lab의 6개 도구, Light·Medium·Heavy·Fixed 독립 데이터 |
| 12 | 자동 통과 | 공용·마루·6개 지역 카탈로그 Bake Validator 오류 0 |
| 13 | 자동 통과 | 실제 6개 Tool Definition·Runtime Prefab의 HUD 자원·가격·프롬프트 일치 |
| 14 | 자동 통과 | 도구 판정 30/60fps 동일, 점프 판정 30/60/120fps 허용 오차 내 동일 |
| 15 | 자동 통과 | 최대 낙하 착지, OutOfBounds 복구, 포털 이음새, 500시드 외곽·바닥 실패 0 |

## 수동 승인 시나리오

| 시나리오 | 자동 대체 범위 | 실제 플레이 승인 |
|---|---|---|
| A. 입력 충돌 | Context 우선, 일반 위치 Throw, `↓+X` Safe Cell | 대기 |
| B. 폭탄 복구 | 집기·연쇄·3×3·경계 보호 | 대기 |
| C. 로프 | Ceiling·StarKnot·Heavy 등반 거부·Snapshot | 대기 |
| D. 도구 자원 | 빈 공간 미소모·유효 반응 1 소모·상점 완전 수리 | 대기 |
| E. 갈고리 | World/Object Anchor·차폐·벽 정지 | 대기 |
| F. 우산 | 낙하 제한·바람 1.8·Laser·Deflectable | 대기 |
| G. 방 전환 | 상자·도구·폭탄 상태, Hook 차단, ResidualSimulation | 대기 |

수동 항목은 애니메이션, 체감 입력, 화면 피드백, 실제 콜라이더 조합을 사람이 한 번 연속 플레이해 승인해야 한다.

## 성능 측정

- 500시드 생성·검증: 37.5628ms, 500개 모두 고유 해시, 실패 0.
- 30/60fps 판정 일치: 자동 회귀 통과.
- `1920×1080 / 60fps`, 현재 방+인접 방 CPU 8ms 이하: Development Player 측정 대기.
- 방 전환 GC Alloc 1KB 이하: Development Player Profiler 측정 대기.
- 스테이지 전환 외 50fps 미만 프레임 드롭 금지: 연속 플레이 캡처 대기.

에디터 Test Runner 시간은 빌드 런타임 프레임 성능으로 간주하지 않는다.

## 문서와 구현 비교에서 수정한 차이

1. 맵 구현은 MAP-E11까지 완료됐지만 `MapBuildTag`가 MAP-E09로 남아 있던 차이를 MAP-E11 BatchValidation으로 정합화했다.
2. 전체 회귀 반복 시 Unity AssetDatabase가 삭제된 고정 테스트 ID를 캐시해 Bake 검증이 흔들리던 문제를, Bake 후 자산 재로드와 매 실행 고유 테스트 ID로 제거했다.
3. 도구 30/60fps 판정, 폭탄 경계 보호, 실제 Tool HUD·가격·프롬프트, CriticalCarry 0.8초 Void 복구 회귀를 최종 승인 항목으로 추가했다.

## 다음 승인 작업

1. A~G를 Tool Interaction Lab과 실제 2-Room 흐름에서 한 번 연속 수동 플레이한다.
2. Development Player에서 CPU Frame, GC Alloc, 최소 FPS를 한 번 캡처한다.
3. 두 결과가 기준을 만족하면 TOOL-13을 최종 승인하고 스테이지별 세부 방 제작으로 이동한다.
