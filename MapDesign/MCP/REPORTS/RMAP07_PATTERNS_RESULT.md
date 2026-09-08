# RMAP07_PATTERNS Result

```text
TASK: RMAP07_PATTERNS
STATUS: PASS
```

## USER-FACING IMPLEMENTATION REPORT

`Assets/_Game/Map/Scenes/MoonPalace/RMAP07/MoonPalacePatternGallery_RMAP07.unity`를 열면 48개의 typed 4×4 Base Geometry를 실제 Tilemap으로 볼 수 있다. 각 label은 Candidate ID와 Primary Role을 표시한다. `RMAP07_Player`는 `(2,1)`에서 시작하며 A/D(또는 방향키)로 이동하고 Space로 점프한다. 오른쪽의 `R0`, `MirrorX`, `MirrorY`, `R180` one-way Tilemap fixture는 실제 Player의 아래→위 통과와 상단 착지용이다.

초기 풀은 최종 500개를 주장하지 않는다. Port, 12×8 조립, 월드 생성, 실제 배치 문맥의 필수 경로 판정은 후속 Task 책임이다.

## RESPONSIBILITY AND FILES

| 실제 경로 | 판단 | RMAP07 책임 | 소유하지 않는 책임 |
| --- | --- | --- | --- |
| `Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/RmapPatternCatalog.cs` | NEW | 4×4 typed cell, 10 role, 24 intent tag, R0/MirrorX/MirrorY/R180, stable-ID dedup, A10 characteristics 및 CSV export API | Port, 3×2 MicroChunk 조립, 500개 확충 |
| `Assets/_Game/Live/Editor/RMAP07/{CharacterLivePatternGallerySceneBuilder.cs,Game.Character.Live.Rmap07.Editor.asmdef}` | NEW | 48개 gallery, Player, TilemapCollider2D solid terrain, four PlatformEffector2D fixtures 및 deterministic artifact publication | 기존 scene/prefab 교체 |
| `Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RMAP07/*.csv` | NEW / DERIVED | first-pool, origin/transform, A10, placement의 project-visible CSV 복제본 | 독립 authoring source; builder가 재생성 |
| `Assets/_Game/Map/Scenes/MoonPalace/RMAP07/MoonPalacePatternGallery_RMAP07.unity` | NEW | 48×56 (12×8 배수) saved gallery와 physical fixture | RMAP02~06 scene 수정 |
| `Assets/_Game/Tests/{EditMode/Map,PlayMode/Character}/RMAP07/` | NEW | catalog contract, CSV, 실제 Player/Tilemap/one-way focused evidence | broad regression |
| `MapDesign/MCP/GENERATED/RMAP07/` | NEW / DERIVED | catalog, relations, characteristics, selection, placement, fixture manifest 및 NUnit 결과 | source catalog 재작성 |

## PRECONDITIONS

- Apply 전 HEAD / branch: `3ee736cced0666c92f985f55e78397fc9283da2c` / `main` (`RMAP06 implement look camera and movement lab`).
- Inbox external expected SHA-256와 전체 bytes actual: `b976a386c798d7cd5052f46a0c65e858eaf97ccbd23981874298c1cc58d16d3b` = match.
- RMAP06 PASS Result SHA-256: `6cad2dd5629d05643c3c86449f990a90ad78370d5fda068246fb5f3c108b59e1` = metadata match.
- RMAP06 installed Task SHA-256: `1f11eeb9f775c78e6553d88dd2f49f9243f8d8b6faa71bf3fa707b915f9877aa` = metadata match; its archive bytes도 동일했다.
- RMAP06 Result 경로의 실제 마지막 commit: `3ee736cced0666c92f985f55e78397fc9283da2c`.
- Apply 전 Status: `227 COMPLETE / 0 CURRENT / 13 LOCKED`, Current `NONE`; Apply 후: `227 / 1 / 12`, Current `RMAP07_PATTERNS`. RMAP08은 정확히 한 `LOCKED` row였다.
- RMAP07 installed Task와 archive는 모두 `b976a386c798d7cd5052f46a0c65e858eaf97ccbd23981874298c1cc58d16d3b`이며 inbox source는 archive로 이동했다.

## REUSE / ADAPT DECISIONS

| 연결점 | 판단 | 근거와 경계 |
| --- | --- | --- |
| `MoonPalaceMicroPatternCandidateLibrary`의 VIS01 500 mask 후보 | ADAPT | 기존 CandidateId/`0x` mask 및 bit `y*4+x` (solid=1)을 8 source로 보존·변환했다. typed ONE_WAY는 이진 mask와 합치지 않는 RMAP07 cell 값이다. 기존 500 source를 수정하지 않았다. |
| `MicroPatternTransforms.cs` | KEEP | 기존 instruction-layer transform을 변경하지 않았다. RMAP07은 BaseCell 3종의 별도 immutable transform을 같은 lower-left mapping으로 제공한다. |
| RMAP02 Terrain tile / Player baseline | REUSE | `RMAP02_Terrain.asset`, Player prefab, `ConfigureRmap02`을 실제 scene/test에 사용했다. seven-layer `GeneratedUnityTilemapApplier`는 logical bake 전용이므로 catalog를 그 API에 억지로 넣지 않았다. |
| RMAP04 one-way wiring | REUSE | `TilemapCollider2D + Rigidbody2D(static) + PlatformEffector2D(useOneWay) + CharacterLiveOneWayPlatform + OneWay GrabSurface`를 four transform fixture에 재사용했다. |
| RMAP06 builder/lab | KEEP / REFERENCE | isolated scene, saved manifest, actual Player fixture 구성 방식을 참조했으며 RMAP06 파일은 변경하지 않았다. |

## REQUIREMENT EVIDENCE

| 요구 | 산출물·검사 | 판정 |
| --- | --- | --- |
| A01 | `RmapPatternCatalog`의 `Width=4`, `Height=4`, `CellCount=16`; lower-left/x-right/y-up/index=`y*4+x`; 12×8 조립은 API에 포함하지 않음 | PASS |
| A06 | AIR/SOLID/ONE_WAY_PLATFORM만 허용, 기존 VIS01 binary mask source relation 보존, 3^16 열거 없음 | PASS |
| A07 | `rmap07_selection_manifest.json`: 88 source-transform relations → 56 distinct Base Geometry, 32 relation collapse, 48 selected/8 excluded; overlay는 BaseCells16 key에 미포함 | PASS |
| A08 | selection manifest와 catalog/test가 10 Primary Role 모두 확인 | PASS |
| A09 | 24 tag enum 전부가 first pool에 존재; direction 역변환에 표현할 tag가 없을 때 `UnresolvedDirectionalTags`로 남김; REQUIRED_OK/SECRET_SHELL 충돌 2 candidate를 관계 CSV에 review-required로 기록 | PASS |
| A10 | `rmap07_auto_characteristics.csv`: typed EdgeOpenCellSet, span/headroom/context, launch/landing, solid-only grab corners, fall columns, climb overlay cells, 4-neighbor AIR components, three ratio, transform symmetry | PASS |
| A11 | `TransformCells`가 R0/MirrorX/MirrorY/R180 좌표 `(x,y)`, `(3-x,y)`, `(x,3-y)`, `(3-x,3-y)`를 적용하고 stable BaseCells16 SHA ID로 dedup | PASS |
| A12 | current saved scene의 actual Player/Tilemap test가 four physical one-way transform에서 아래→위 통과와 top landing을 확인 | PASS |
| A15 | 48 unique starter candidates, 48 placement rows, UTF-8 no-BOM round-trip CSV, gallery/selection/fixture manifests | PASS |

## CATALOG AND VISIBLE EVIDENCE

- `rmap07_first_pool.csv`: 48 rows / 48 unique CandidateId / VOID_CLEAR 1.
- 역할 분포: SLOPE_RISE_RIGHT 3, SLOPE_RISE_LEFT 3, CEILING_FLAT 1, CEILING_ROUGH 7, WALL_LEFT 3, WALL_RIGHT 3, VOID_CLEAR 1, SPARSE_AIR_PLATFORM 4, STANDABLE_LEDGE 22, VERTICAL_PASSAGE 1.
- selected origin relation은 76개(21 source ID), 그중 VIS01 source relation 25개다. `rmap07_origin_transform_relations.csv`가 source/mask/original reference/transform/candidate/tag/conflict를 보존한다.
- `rmap07_gallery_placements.csv`는 CandidateId별 4×4 lower-left origin과 BaseCells16을, `rmap07_gallery_manifest.json`은 bounds/player/fixture/control을 기록한다.
- gallery solid terrain은 1×1 Tilemap cell과 direct `TilemapCollider2D`를 사용한다. fixture strips만 RMAP04식 thin top-only profile(0.20 world-tile height)을 사용하며 cell type/transform 의미를 뒤집지 않는다.

## VALIDATION

- Unity Editor 연결 상태: no connected Pipeline instance; batch Unity fallback 사용.
- Builder/compile: `unity run . --editor-version 6000.3.8f1 --timeout 300 --non-interactive --no-banner -- -executeMethod StarNight.Character.Live.Rmap07.Editor.CharacterLivePatternGallerySceneBuilder.Build` completed; scene/CSV/manifest regenerated.
- EditMode focused filter `StarNight.Map.Tests.EditMode.Rmap07.RmapPatternCatalogTests`: 5 / 5 passed. 16-cell validity, all roles/tags, typed characteristics, transform/reclassification/dedup, CSV/stability/VIS01 relation을 확인했다.
- PlayMode focused filter `StarNight.Character.Tests.PlayMode.Rmap07.RmapPatternGalleryPlayModeTests`: 2 / 2 passed. saved RMAP07 scene에서 actual Player가 TilemapCollider2D solid ground에 착지하고, R0/MirrorX/MirrorY/R180 Tilemap one-way 모두 통과 후 top face에 착지했다.
- broad/full/unfiltered regression, legacy 19347, Player build, 3^16 enumeration, final 500 expansion은 이번 Task 범위 밖이므로 실행하지 않았다.

## RMAP08 BINDINGS

| 상태 | 실제 경로 / API | RMAP08 사용 경계 |
| --- | --- | --- |
| EXISTING (RMAP07 생성) | `RmapPatternCatalog.BuildInitialPool()`, `RmapPatternCatalogSnapshot.TryGetCandidate()` | stable CandidateId catalog 조회 |
| EXISTING (RMAP07 생성) | `RmapPatternCandidate.BaseCells`, `PrimaryRole`, `Origins`, `Characteristics` | port가 아닌 base geometry/origin/role read-only input |
| EXISTING (RMAP07 생성) | `RmapPatternAutomaticCharacteristics.EdgeOpenCellSets`, `ContextRequired`, `SymmetricTransforms` | Type 0~4 port 판정을 대체하지 않는 edge evidence |
| EXISTING (RMAP07 생성) | `RmapPatternCatalog.TransformCells(...)`와 `Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RMAP07/*.csv` | deterministic transform/rebuild input; CSV는 derived view |
| PROPOSED (RMAP08 소유) | `Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/RmapPortCatalog.cs` | Type 0~4 coordinate/direction/interior-link contract; 이번 Task는 만들지 않음 |

## OUT-OF-SCOPE FINDINGS

- EdgeOpenCellSet은 typed local boundary 기록이며 Type 0~4 Port 통과 판정이나 neighbor consistency proof가 아니다.
- 3×2 Pattern→12×8 MicroChunk composition, protected space/overlay selection, 500 distinct role-qualified pool, seed/world generation, camera-room snap, and full-world bake remain unopened work.
- 24 intent tags are author intent, not a proof of mandatory traversal or Player-profile jump reachability. Those context requirements are surfaced in characteristics rather than silently passed.

## FOLLOWUP / FINAL EVIDENCE

- Current installed Task SHA-256 and archive SHA-256: `b976a386c798d7cd5052f46a0c65e858eaf97ccbd23981874298c1cc58d16d3b`.
- Result is written before Phase C/D; commit SHA is therefore recorded by final CLI report rather than retroactively rewriting this evidence.
- Phase C may only close RMAP07. `RMAP08_PORTS` remains `LOCKED` and is not started.

## COMMIT

Pending Phase C Finalize and scoped Phase D atomic commit.
