# SV5_11_HUB_SHELL INBOX patch

이 ZIP은 Unity 프로젝트 루트에 그대로 압축 해제한다.
`STAGE.py`는 패키지, SV5_10 Finalize commit blob, 상태와 소유 경계를 검사한 뒤
`MapDesign/MCP_INBOX/SV5_11_HUB_SHELL.md` 하나만 설치한다.

Apply, Unity, Finalize, commit, push는 자동 실행하지 않는다. 검사 결과는 콘솔에만 출력하며
CHECK/FILES/VERIFY 복제 파일을 만들지 않는다.

## 실행

PowerShell에서 Unity 프로젝트 루트로 이동한 뒤:

```powershell
python -X utf8 MapDesign/MCP/INPUTS/SV5_11_HUB_SHELL/STAGE.py `
  --root . `
  --expected-manifest-sha256 <PACKAGE_MANIFEST_SHA256> `
  --mode check

python -X utf8 MapDesign/MCP/INPUTS/SV5_11_HUB_SHELL/STAGE.py `
  --root . `
  --expected-manifest-sha256 <PACKAGE_MANIFEST_SHA256> `
  --mode stage
```

두 번째 명령이 `PASS_STAGE`를 출력한 뒤에만 현지 MCP의 정상 Apply 절차를 수행한다.
