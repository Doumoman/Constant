# SV5_06_FIX03 Result

TASK: SV5_06_FIX03
STATUS: PASS

## 선행·등록·Apply

- package SHA-256: `58c9096f0a83e70557af30d43cf425b4e263abbe27fbf7a8ffdf6e84fd70f0d8`
- manifest SHA-256: `b0b536a2f5d952e05bab7882681b2fceed30bd389444254210433d7b6716cf00`
- 선행 SV5_06_FIX02 Result SHA-256: `20a611e78a10887e494fa3c02f64daa734e0f0dc3f0f775e86490b936372dc41`
- 선행 installed Task SHA-256: `149d300e2010a08d9f399a94e8fdfa6289e12752e6af332b5115b89cd6c0ba5d`
- 선행 commit / parent: `05ff2344263d1cbd46f6c9b21995a06539d02832` / `de57f7b1454d2b138bda9915bdb03ad1ae09cbe3`
- 등록 전: 247 COMPLETE / 0 CURRENT / 41 LOCKED / Current NONE / 288행
- 등록 후: 247 COMPLETE / 0 CURRENT / 42 LOCKED / Current NONE / 289행
- Apply 후: 247 COMPLETE / 1 CURRENT / 41 LOCKED / Current `TASKS/SV5_06_FIX03.md` / 289행
- 등록 Status SHA: `ab6cb816b3d43ef50bfac9e4b90b0e88b8c27a714a01e294707283161f014b89` → `d4e329bbdf708eb5392040ba23e05b6473330342294348824d7872396448aa55`
- Apply Status SHA: `82af89777dba17b5fb052cc0b75bdcb3dfaf6bac9b0e94fe16363c5cd3da095f`
- 등록 Master SHA: `882675204091f954c2c2e06a4196574a7202b403751ae3cdbc29d385da46322e` → `a4322f20b840a0548c1f4404a23708d7d9b92dbe27e4be43f296fa09cef64686`
- Task / Archive SHA-256: `ba914e5b18054260e59f5311a7a3d27eb50d146ba65eafe1844ad59a32f213c1`
- `pre-registration`, LF `git apply --check`, LF `git apply`, `pre-stage`, `stage`, Apply 뒤 `post-readonly`: 모두 지정 절차와 SHA로 통과했다.

## FIX02 반례와 구현 결과

- exact FIX02 `connections.csv`, `ports.csv`, `contact_checks.csv`, `gate_geometry.json` 잠금을 먼저 확인하고 `REPRODUCE.py`를 실행했다. 7,188 접촉 중 기존 separated 225 = FACE 164 + SHARED 61, empty boundary 225, 양 route Passage 포함 93을 재현했다.
- 수정 전 독립 좌표 모델에서 Forge 1건, Seal 2건, Exit 3건의 닫힌 상태 우회가 모두 reachable이었다. 재현 증거 SHA-256은 `0f6dccc19d20e1978b90484dfafd9a36c6bdd0cbc48943b2ba15299ae95b0a63`이다.
- 생산 `Sv5SpacePhysicalMovement`는 모든 connection의 `Centerline`과 `ApertureCells`를 route ID 없는 단일 world-coordinate 이동 집합으로 만든다. `Clearance`와 `INFILL_PENDING`은 통행 AIR가 아니다.
- 7,188개 SHARED/FACE 접촉을 전부 실제 좌표 이동과 판정에 포함했다. 6,963개 same-predicate 접촉은 `Join`, 225개 different-predicate 접촉은 nonempty boundary와 typed global gate geometry가 있는 `ConditionalGate`다.
- blocking face/cell은 world 좌표에 전역 적용하며 세 gate를 매 상태에서 동시에 평가한다. Forge/Seal/Boss 전의 6개 금지 상태는 모두 target unreachable이고, 조건 충족 뒤 3개 상태는 각각 정상 witness와 함께 reachable이다.
- source route/predicate 진단 label과 집합 입력 순서는 reachability를 바꾸지 않는다. 접촉, boundary, gate geometry, SEALED/OPEN 의미 mutation은 semantic digest를 바꾼다.
- 기존 FIX02 W01/W02, ProtectedAir, RMAP13 자원 3종의 6순서, reachable-state 역도달과 dead-end 0을 보존했다. RMAP13 생산 파일은 수정하지 않았다.
- 기존 회귀의 export는 모두 `GENERATED/SV5_06_FIX03/legacy_exports/` 하위 고유 폴더로 분리했다. FIX02 Task/Archive/Result/generated 핵심 SHA는 불변이다.

## G01–G08와 focused EditMode

- G01: exact FIX02 225/164/61/93 반례를 고정하고 생산 전역 검증의 6 closed + 3 open 상태를 확인했다.
- G02/G03: 다른 predicate SHARED/FACE에서 무관 face만 남긴 실제 생산 입력은 전역 우회를 검출하고, 채택 boundary는 통과한다. same-predicate join은 통행된다.
- G04: route/predicate 진단 label과 connection/gate 열거 순서를 바꿔도 좌표 reachability가 동일하다.
- G05: 빈·가짜 boundary와 SHARED의 직접 face-only separation fixture를 생산 validator가 거부한다.
- G06: 모든 상태가 세 gate의 closed/open 집합 전체를 소비하고 blocking geometry는 route owner label과 무관하게 전역 적용된다.
- G07: contact/boundary/gate/state 의미 변경은 physical digest를 각각 바꾸며 집합 순서만 바꾸면 동일하다.
- G08: 하나의 accepted plan이 JSON/CSV/proof/20 SVG를 생성하고 기존 FIX02 바이트를 보존한다.
- Unity Editor: `6000.3.8f1 (1c7db571dde0)`; Unity CLI: `1.0.0-beta.9`
- 명령: `unity test . --mode EditMode --filter StarNight.Map.Tests.EditMode.Sv5 --output MapDesign/MCP/GENERATED/SV5_06_FIX03/focused_results.xml --timeout 1800`
- 발견/실행/통과/실패/스킵: 59 / 59 / 59 / 0 / 0
- XML raw SHA-256 / bytes: `984170a4aef8456ac1e93b4e4ea9a91817e44643faabff9c5578d34d02bb2303` / 56,063
- 무필터 전체, PlayMode, build, Scene Bake, Player 실행은 수행하지 않았다.

## 채택 계획과 증거

- plan digest: `0d74dabc7c5ad27aec04a4025bca6ac543f504caca2703e60865f210899dbb30`
- physical movement digest: `50e0a08ffcf5e00a74370993c961a3c5a392010daf2a5815adce8c74d18423f9`
- 28 connections / 7,188 physical contacts / 3 conditional gates / 9 physical gate states / 6 projection orders
- contact coverage 오류 0 / reservation 충돌 0 / gate geometry 오류 0 / gate state 오류 0 / diagnostics 0
- `ComposedGeometryReady=false`, `PlayerVerified=false`
- BINDING SHA-256: `5e58dcc60a8c491383a1b8830f2cb1dbaf48806c8caca43e4779ee1129ae7f74`
- `space_graph.json`: `3062e052653e68e4096a1cc45189acbecdaa96a93c3639e4e4a19240c81e71f9`
- `physical_contact_checks.json`: `c26606db953e8ecf569407578e3cecfabc78e11b5514c4104fc3586b9cc703b7`
- `gate_geometry.json`: `99f4bab5835490763a1a06aabf687e67ba016ded217425cc34888865a0245953`
- `physical_gate_state_checks.json`: `93b0a1d3600690039fd66daf7e6b6688d17619edf215f38e3f1afb3d4f1922aa`
- `state_proofs.json`: `4dca6e805f34f09509c3d4517da556eedc64e1b039735e23c116d35895399770`
- `validation.json`: `8caa93140d35f845cdffdb2832b8e1785e5fbf904a827990fcaebd796b8bb372`
- 활성 설명 `SV5/13_SPACE_CONTACT_FIX03.md`: `cb03fa0fdc9281c7fcb59f0e87d7d0df4bc85dd51d371787b6bd19de0956b1b4`
- protocol suffix SHA-256 `362a02c8ed5628b0de0b0480b6cda000e3b4cef8ad26c78422af79670603251b`가 `SV5/02_PROTOCOL_V5.md` 끝에 정확히 1회 존재한다.

## 종료 조건

정상 Finalize 예상 상태는 289행 = 248 COMPLETE / 0 CURRENT / 41 LOCKED, Current NONE이며 SV5_07은 LOCKED다. 합성 geometry, 실제 지형과 Player 검증, Family 분포·밀도·재합류는 기존 SV5_07/08/09 및 후속 소유 범위에 남긴다. 후속 Task를 열거나 push하지 않는다.
