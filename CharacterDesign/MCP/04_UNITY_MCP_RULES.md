# Unity MCP 실행 규칙 v2.0

- 코드 변경 후 import/domain reload와 compile 완료를 확인한다.
- 신규 오류와 기존 사용자 오류를 구분하고 기존 로그를 삭제했다고 보고하지 않는다.
- Task에 지정된 focused EditMode/PlayMode만 실행하고 실제 실행 수를 REPORT에 기록한다.
- Scene/Prefab/Asset 생성은 Task WRITE ALLOWLIST에 정확한 경로가 있을 때만 허용한다.
- Unity가 생성한 대응 `.meta`는 같은 WRITE ALLOWLIST로 취급하되 REPORT에 기록한다.
- Animator, Audio, Camera가 없어도 순수 캐릭터 논리는 테스트 가능해야 한다.
- 테스트 실패를 Ignore, Explicit, `#if`, 빈 assertion, 조기 return으로 숨기지 않는다.
- 생성 MAP 전체, Packages, ProjectSettings, asmdef는 현재 Task가 명시하지 않으면 수정하지 않는다.
