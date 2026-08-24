using System;
using System.Collections.Generic;
using StarNight.Character.MapIntegration;
using StarNight.Character.RunState;

namespace StarNight.Character.GeneratedRunValidation
{
    /// <summary>
    /// 결정적 시드 스윕(순수). 고정 시드 목록을 입력 순서대로 검증하고
    /// 시드별 결과(통과/진단 수/다이제스트)를 그대로 보고한다 — 실패를
    /// 숨기거나 자산을 변조하지 않는다.
    /// </summary>
    public static class CharacterGeneratedRunSeedSweepPolicy
    {
        /// <summary>기준 고정 시드 8종 — 스윕 최소 요구.</summary>
        public static readonly IReadOnlyList<int> DefaultSeeds =
            new int[] { 11, 23, 37, 41, 53, 67, 79, 97 };

        public static List<CharacterGeneratedRunValidationResult> Sweep(
            IReadOnlyList<int> seeds,
            Func<int, CharacterGeneratedRunSnapshot> snapshotProvider,
            int actorId,
            in CharacterRunInventoryState inventory,
            ICharacterRoomReadinessSource readinessSource)
        {
            var results = new List<CharacterGeneratedRunValidationResult>();

            if (seeds == null || snapshotProvider == null)
            {
                return results;
            }

            for (int index = 0; index < seeds.Count; index++)
            {
                int seed = seeds[index];
                var snapshot = snapshotProvider(seed);

                results.Add(CharacterGeneratedRunValidationPolicy.Validate(
                    snapshot, actorId, in inventory, readinessSource));
            }

            return results;
        }

        /// <summary>스윕 집계 — 통과/실패/총 진단 수(숨김 없음).</summary>
        public static void CountOutcomes(
            IReadOnlyList<CharacterGeneratedRunValidationResult> results,
            out int passedCount,
            out int failedCount,
            out int diagnosticCount)
        {
            passedCount = 0;
            failedCount = 0;
            diagnosticCount = 0;

            if (results == null)
            {
                return;
            }

            for (int index = 0; index < results.Count; index++)
            {
                if (results[index].Passed)
                {
                    passedCount++;
                }
                else
                {
                    failedCount++;
                }

                diagnosticCount += results[index].Diagnostics.Count;
            }
        }
    }
}
