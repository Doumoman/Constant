# SV5_20_JUMP_PLAYER FIX02 active-task amendment

This corrective input supersedes FIX01 while SV5_20 remains CURRENT. It does
not register, stage, Apply, finalize, or open another Task.

FIX02 corrects the patch to four removals because both receiving TOP_ONLY cells
already belong to the immutable SV5_19 recovery overlay. It also replaces the
late fixed-step jump schedule with a support-edge-driven grounded trigger.

Run `VERIFY.py --check` before resuming. Do not run the superseded FIX01 checker
and do not create ad-hoc CHECK/FILES/VERIFY copies.

