# SV5_20_JUMP_PLAYER FIX03 active-task amendment

This package supersedes FIX01 and FIX02 while `SV5_20_JUMP_PLAYER` remains
CURRENT. It is not a new Task: do not stage an MCP_INBOX file, run native
Apply, or change lifecycle state.

FIX03 addresses the three symmetric physical defects proven by the 18-case
diagnostic: LINK_02 head impact, LINK_03 non-impulsive Grab release, and
LINK_06 obstruction by the later JS08 solid. Work remains inside the two
24x32 recipe fixtures and must not change Player tuning or predecessors.

Run `VERIFY.py --check` before resuming. Do not run the superseded FIX01 or
FIX02 checker, and do not create ad-hoc CHECK/FILES/VERIFY copies.
