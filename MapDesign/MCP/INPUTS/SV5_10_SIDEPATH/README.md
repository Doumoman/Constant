# SV5_10_SIDEPATH INBOX patch

이 폴더 전체를 Unity 프로젝트의 `MapDesign/MCP/INPUTS/SV5_10_SIDEPATH/`에 둔다.
`STAGE.py`는 package, predecessor commit blob, 현재 상태와 소유 경계를 검사하고 INBOX MD 하나만 설치한다.
Apply, Unity, Finalize, commit, push는 실행하지 않는다. 검사 결과는 콘솔에만 출력하며 CHECK/FILES/VERIFY 복제 파일을 만들지 않는다.
