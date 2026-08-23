# 캐릭터 고정 구현 순서

| 단계 | 역할 | 진입 의존성 | 종료 게이트 |
|---|---|---|---|
| CHAR00 | 기준선과 하네스 | 없음 | 기존 접점과 실제 경로가 등록되고 이후 구현 작업을 실행할 수 있음 |
| CHAR01 | 핵심 이동 | CHAR00 EXIT APPROVED | 고정 테스트룸에서 이동·점프·착지·상태 전환이 재현 가능함 |
| CHAR02 | 이동 문법 검증 | CHAR01 EXIT APPROVED | 2셀 높이·2셀 달리기 틈·3셀 기본 통과 불가가 모두 검증됨 |
| CHAR03 | MAP·방 전환 연동 | CHAR02 EXIT APPROVED | 준비되지 않은 방은 차단되고 준비된 방 전환에서 입력·속도 KEEP과 Hysteresis가 동작함 |
| CHAR04 | 상호작용과 접촉 전투 | CHAR03 EXIT APPROVED | Carryable·기절 적·투척물·밟기·접촉 피해가 고정 테스트룸에서 모두 판정됨 |
| CHAR05 | 장비·생존·런 상태 | CHAR04 EXIT APPROVED | 장비·피해·사망·런 상태가 중복 없이 동작하고 HUD·연출이 논리와 분리됨 |
| CHAR06 | 생성 맵·최종 검증 | CHAR05 EXIT APPROVED | 생성 맵 회귀·Unity 테스트·빌드·최종 EXIT AUDIT가 모두 PASS |

단계를 건너뛰거나 병렬 CURRENT로 열 수 없다. MAP 작업과 캐릭터 작업은 병렬일 수 있지만 캐릭터 하네스 내부 작업은 직렬이다.
