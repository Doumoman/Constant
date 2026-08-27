control → Master/Status → current MAP04_11 Task → FAIL Result를 읽고 두 SHA precondition을 검증하라.
production을 수정하지 말고 기존 Map04ExitTests.cs만 repair하며 meta/GUID는 보존하라.
seeds 0..999를 attempts 0..99로 실행해 Completed + PASS_SITE handoff required = 1000, invalid/unclassified/lost = 0을 증명하라.
Completed publication은 site misownership 0과 MAP04 모든 invariant를, handoff는 exact 100 RetryRequired attempts·무출력·무변이를 증명하라.
기존 110 tests, MAP04 focused 1454, aggregate >=1564, Game.Map >=5359, Full >=5467, visual Game/Scene 18/18, compile/Console/warning 0을 실제 실행하라.
Assets meta 3148, test C# modification 1, production/unexpected 0을 확인하고 PASS일 때만 MAP04_11을 finalize하라; MAP05_01은 LOCKED로 유지하라.
