# Character Harness Patch Manifest

## 하네스 설치 패치 허용 범위

- `CharacterDesign/**` 신규 추가

## 설치 패치 금지 범위

- Assets, Packages, ProjectSettings, MapDesign 수정
- 기존 파일 삭제·덮어쓰기·이동·이름 변경
- asmdef, Scene, Prefab, ScriptableObject, CSV 수정
- Unity 런타임·에디터 코드 수정
- git commit 또는 git push

## 작업 패치 공통 규칙

- 현재 TASK만 실행
- 고정 READ/WRITE ALLOWLIST 준수
- 지정 테스트 개수 유지
- RESULT 작성
- PASS 전 FINALIZE 금지
- FINALIZE와 다음 OPEN 분리

## SHA 검증

`CharacterDesign/PACKAGE_MANIFEST.sha256`은 자기 자신을 제외한 CharacterDesign 파일을 기록한다. 파일이 수정되면 매니페스트를 다시 생성하지 말고 설치 패치를 BLOCKED 처리한다.
