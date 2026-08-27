# RUN MAP07_13 FINALIZE

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, MAP07_13 PASS Result를 순서대로 읽어라.

Exact gates:

```text
MAP07_13 Result STATUS: PASS
MAP07_13 Result SHA-256: 263a2bbf291e4df25dbe6bc101986e11ebf39bc0fc3d0074759fb7450b6df77e
MAP07_13 Task SHA-256: 698a330dcd7a8ba14ec33cec51b68ea56be9382abd0eefde96eb2a516c81effb
MAP07 PHASE EXIT: APPROVED
```

Apply scope:

```text
Replace MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
Replace MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
Create no Task file
Set Current Task to NONE
Set MAP07_13 COMPLETE
Keep MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS LOCKED / DO NOT START
```

Forbidden:

```text
MAP08_01 or later Task body read/start
Assets changes
Authoring CSV changes
generated CSV changes
C# or test changes
Scene/Prefab/ProjectSettings/Packages changes
asmdef/asmref changes
git commit
git push
```

After apply, required final state:

```text
205 status rows
91 COMPLETE
0 CURRENT
114 LOCKED
Current Task: NONE
MAP07 Phase: COMPLETE / EXIT APPROVED
MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS: LOCKED / DO NOT START
```
