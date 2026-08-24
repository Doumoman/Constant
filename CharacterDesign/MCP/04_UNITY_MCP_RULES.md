# Unity MCP 실행 규칙

- 코드 변경 후 Unity 컴파일 완료를 확인한다.
- Console을 정리해 오류의 발생 시점을 분리하되 기존 사용자 로그를 삭제했다고 보고하지 않는다.
- TASK에 지정된 EditMode/PlayMode 테스트만 실행한다.
- 테스트 실패를 숨기기 위해 Ignore, Explicit, #if 또는 조기 return을 추가하지 않는다.
- 테스트 Scene/Prefab/Asset 생성이 필요한 작업만 해당 경로를 WRITE ALLOWLIST에 포함한다.
- Unity가 자동 생성하는 `.meta`는 대응 파일과 같은 WRITE ALLOWLIST로 취급하되 RESULT에 기록한다.
- Animator, Audio, Camera 참조가 없어도 순수 논리 테스트는 실행 가능해야 한다.
- 최종 통합 전에는 생성 MAP 전체를 임의 수정하지 않는다.
