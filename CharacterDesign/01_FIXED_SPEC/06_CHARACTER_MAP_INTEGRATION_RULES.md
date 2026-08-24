# 캐릭터와 MAP 통합 규칙 v2.0

## 고정 계약

- 캐릭터는 MAP의 공용 논리 셀↔월드 좌표 변환 진입점(`WorldCoordinateUtility` 계열)만 사용한다.
- Tilemap, MicroChunk, Room Generator 내부 구현을 캐릭터 런타임에서 직접 참조·수정하지 않는다.
- 지형 파괴·변경은 요청과 결과 계약으로 처리하며 캐릭터가 타일을 직접 삭제하지 않는다.
- 목적지 카메라룸 지형이 준비되지 않았으면 경계 통과를 허용하지 않는다.
- 전환 중 입력과 속도의 기본 정책은 KEEP이다.
- 경계 진입에는 Hysteresis를 적용해 반복 전환 이벤트를 방지한다.

## 의존성(미해결, 고정 기록)

- 캐릭터용 MAP world query(고체/위험), terrain mutation request, room boundary gate, room readiness API는 현재 활성 코드에 없다(CHAR00_01 확인).
- `CHAR03_01` 시작 전에 위 API에 대한 별도 MAP 계약 승인이 필요하다. 캐릭터 Task가 이를 선행 구현하지 않는다.
- 타일 의미의 원천은 MAP의 `MicrochunkTileLayer`(GroundSolid / OneWay / Breakable / Hazard / Liquid / DecorationBack / DecorationFront / Marker)다.

## 최종 검증

- Type1/2/3 필수 경로와 Type0 선택 지역 복귀 가능성은 CHAR06 최종 통합 테스트에서 검증한다.
