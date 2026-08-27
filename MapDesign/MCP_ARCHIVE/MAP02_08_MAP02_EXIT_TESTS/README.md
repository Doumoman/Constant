# MAP02_08 — MAP02 Exit Tests

MAP02_07 PASS 상태에서 MAP02의 여덟 번째이자 마지막 Task만 여는 patch package다. Patch apply는 Master, Status, 새 Task 문서만 설치하고 Assets를 변경하지 않는다.

내부 `RUN_MAP02_08_PROMPT.md`로 실행한다. production은 수정하지 않고 신규 `Map02ExitTests.cs` 하나로 169-cell/624-link topology, six RNG vectors·독립성, recorded grid→manifest→atomic publish/load→replay, 100회 동일 static sector hash, overlay orientation을 통합 검증한다. 기존 focused/targeted/full tests와 현 프로젝트 Scene/Game visual 12개도 다시 수행한다. 현재 Assets meta `2988`과 accepted legacy folder meta `6/6`을 baseline으로 고정했고 새 directory는 만들지 않는다.
