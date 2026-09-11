

## SV5_08_FIX01 — inclusive connector length and report handoff

Active infill generation keeps the SV5_08 production API and actual 1×1/4×4 payloads.
A new ordinary connector includes both host/parent and child entry endpoints, at most24 ordered cardinal AIR cell visits.
Root HostAccess joins Path at exactly one equal endpoint; child Path starts at its parent boundary.
Ownership-filtered ExternalCenterline is a separate diagnostic. +1 diagonal movement consumes two cardinal moves.
The new length policy/digest and exports are defined by INPUTS/SV5_08_FIX01/LENGTH_RULE.json and REPAIR_CONTRACT.md.
Historical08 artifacts are immutable. Current tests/exports write only GENERATED/SV5_08_FIX01.
Legacy inventory is read-only: actual consumers determine ACTIVE/COMPAT/HISTORICAL/CANDIDATE/UNKNOWN; no retirement here.
Report handoff preference: when the user uploads a Result/Review, review it and provide the next concrete normal/repair package
and inline execution instruction in the same response when evidence suffices. Do not wait for another “go”.
If an exact required input is missing, state that specific blocker; do not invent hashes or permissions.
This preference does not open SV5_09 locally or authorize future Task execution/push without its own bound handoff.
