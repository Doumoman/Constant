# MAP04_11 Repair — Split Runtime Scene Adapter

Unity는 `Editor` 폴더에서 컴파일된 `MonoBehaviour`를 scene component로 붙이지 않는다. 이전 Task가 Editor harness를 scene root에 직접 부착하도록 요구해 `BLOCKED`됐다.

이 repair는 역할을 분리한다.

- Runtime: attachable `MapGenerationProgressSceneAdapter`
- Editor: 기존 harness 파일의 Custom Inspector, fixture와 manual action runner
- Scene: root에는 adapter와 topology/site/biome overlay만 부착

기존 Scenes folder/meta, scene/meta, variable overlay source와 GUID는 보존한다. 신규 asset은 runtime adapter C#과 meta 하나씩뿐이며 Assets meta는 `3151 -> 3152`다.

적용 전:

- Current Task SHA: `bdf27df70d15d8040b0c9c36e538da39dc97d1f2dfc84d91201e35de2ba2f623`
- BLOCKED Result SHA: `409ec72e04acc17fd314c85f0a270ff0c23ab9898035e13983e598ba3cb9252c`
- 상태: `56 COMPLETE / MAP04_11 CURRENT / 148 LOCKED`

모든 scene/tests/1,000-world exit gate가 PASS할 때만 MAP04_11을 finalize한다. MAP05_01은 계속 LOCKED다.
