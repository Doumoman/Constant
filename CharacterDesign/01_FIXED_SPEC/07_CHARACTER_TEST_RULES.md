# 캐릭터 테스트 규칙 v2.0

- 각 Task 문서는 고정된 gate 개수와 이름을 가진다. 개수를 임의로 줄이거나 이름만 바꿔 통과 처리할 수 없다.
- EditMode는 순수 상태·입력·수치·계약 검증을 담당한다.
- PlayMode는 Rigidbody2D, Collider2D, MAP 어댑터, 방 경계와 실제 프레임 진행을 검증한다.
- 테스트용 지형은 생성 결과와 분리된 고정 fixture를 우선 사용하고 MAP 공용 논리 셀 좌표로 기술한다.
- 검증 코스는 `04_TEST_FIXTURES/`의 고정 fixture ID 16개를 canonical 식별자로 사용한다. fixture ID의 개명·삭제는 CHANGE CONTROL 대상이다.
- 테스트는 복제 상수가 아니라 실제 등록 설정 소스를 사용한다.
- 실패 테스트를 Ignore, Explicit, 조건부 컴파일 또는 조기 반환으로 숨기지 않는다.
- 결과 보고서에는 실행 명령, 전체/성공/실패 개수, 실패 이름과 로그 위치를 기록한다.
- 컴파일 오류가 있으면 테스트 미실행이라도 STATUS는 FAIL 또는 BLOCKED다.
