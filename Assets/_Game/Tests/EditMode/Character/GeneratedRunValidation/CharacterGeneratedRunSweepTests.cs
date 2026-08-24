using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Character.GeneratedRunValidation;
using StarNight.Character.Integration;
using StarNight.Character.RunState;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.Tests.GeneratedRunValidation
{
    public sealed class CharacterGeneratedRunSweepTests
    {
        [Test]
        public void RandomRun_SeedSweepIsDeterministicAndReportsReproducibleDiagnostics()
        {
            var inventory = new CharacterRunInventoryState(
                CharacterGeneratedRunFixtures.ActorId, 4, 4);

            // 고정 시드 목록 최소 8종.
            Assert.That(CharacterGeneratedRunSeedSweepPolicy.DefaultSeeds.Count,
                Is.GreaterThanOrEqualTo(8));

            // (1) 유효 픽스처 스윕: 8시드 전부 통과, 두 번 돌려도 다이제스트
            //     완전 동일(결정적).
            var first = CharacterGeneratedRunSeedSweepPolicy.Sweep(
                CharacterGeneratedRunSeedSweepPolicy.DefaultSeeds,
                CharacterGeneratedRunFixtures.ValidRun,
                CharacterGeneratedRunFixtures.ActorId,
                in inventory, CharacterGeneratedRunFixtures.ReadyRooms());
            var second = CharacterGeneratedRunSeedSweepPolicy.Sweep(
                CharacterGeneratedRunSeedSweepPolicy.DefaultSeeds,
                CharacterGeneratedRunFixtures.ValidRun,
                CharacterGeneratedRunFixtures.ActorId,
                in inventory, CharacterGeneratedRunFixtures.ReadyRooms());

            Assert.That(first.Count, Is.EqualTo(8));
            for (int index = 0; index < first.Count; index++)
            {
                Assert.That(first[index].Seed, Is.EqualTo(
                    CharacterGeneratedRunSeedSweepPolicy.DefaultSeeds[index]));
                Assert.That(first[index].Passed, Is.True,
                    "seed " + first[index].Seed);
                Assert.That(second[index].Digest, Is.EqualTo(first[index].Digest));
            }

            int passedCount;
            int failedCount;
            int diagnosticCount;
            CharacterGeneratedRunSeedSweepPolicy.CountOutcomes(
                first, out passedCount, out failedCount, out diagnosticCount);
            Assert.That(passedCount, Is.EqualTo(8));
            Assert.That(failedCount, Is.EqualTo(0));
            Assert.That(diagnosticCount, Is.EqualTo(0));

            // (2) 홀수 시드에 예약 셀 침범 아이템을 심은 픽스처: 실패가
            //     숨김없이 보고되고, 실패 진단은 시드·아이템 ID·사유를
            //     식별하며 재현 가능하다.
            CharacterGeneratedRunSnapshot BrokenOnOddSeeds(int seed)
            {
                var run = CharacterGeneratedRunFixtures.ValidRun(seed);
                if (seed % 2 == 0)
                {
                    return run;
                }

                return CharacterGeneratedRunFixtures.Custom(
                    seed, CharacterGeneratedRunFixtures.Start(seed),
                    CharacterGeneratedRunFixtures.DefaultRooms(),
                    CharacterGeneratedRunFixtures.DefaultMicrochunks(),
                    new List<CharacterGeneratedRouteEdgeSnapshot>
                    {
                        CharacterGeneratedRunFixtures.BasicRoute()
                    },
                    new List<CharacterGeneratedItemPlacementSnapshot>
                    {
                        // 루트 진입 셀(12,3) 침범.
                        new CharacterGeneratedItemPlacementSnapshot(
                            9, CharacterGeneratedRunFixtures.RoomB,
                            new WorldTileCoord(12, 3))
                    });
            }

            var brokenFirst = CharacterGeneratedRunSeedSweepPolicy.Sweep(
                CharacterGeneratedRunSeedSweepPolicy.DefaultSeeds,
                BrokenOnOddSeeds, CharacterGeneratedRunFixtures.ActorId,
                in inventory, CharacterGeneratedRunFixtures.ReadyRooms());
            var brokenSecond = CharacterGeneratedRunSeedSweepPolicy.Sweep(
                CharacterGeneratedRunSeedSweepPolicy.DefaultSeeds,
                BrokenOnOddSeeds, CharacterGeneratedRunFixtures.ActorId,
                in inventory, CharacterGeneratedRunFixtures.ReadyRooms());

            CharacterGeneratedRunSeedSweepPolicy.CountOutcomes(
                brokenFirst, out passedCount, out failedCount, out diagnosticCount);

            // 기본 8시드 {11,23,37,41,53,67,79,97} 전부 홀수 → 8 실패.
            Assert.That(failedCount, Is.EqualTo(8));
            Assert.That(diagnosticCount, Is.EqualTo(8));

            foreach (var result in brokenFirst.Where(entry => !entry.Passed))
            {
                Assert.That(result.Diagnostics.Count, Is.GreaterThanOrEqualTo(1));
                Assert.That(result.Diagnostics[0].Kind, Is.EqualTo(
                    CharacterGeneratedRunValidationDiagnosticKind.ItemOnReservedCell));
                Assert.That(result.Diagnostics[0].Subject, Does.Contain("item:9"));
                Assert.That(result.Diagnostics[0].Subject, Does.Contain("cell:12,3"));
            }

            // 실패 다이제스트도 재현 가능(같은 입력 → 같은 요약).
            for (int index = 0; index < brokenFirst.Count; index++)
            {
                Assert.That(brokenSecond[index].Digest,
                    Is.EqualTo(brokenFirst[index].Digest));
            }

            // 유효 스윕과 실패 스윕의 다이제스트는 서로 다르다(요약이 실제
            // 결과를 반영).
            Assert.That(brokenFirst[0].Digest, Is.Not.EqualTo(first[0].Digest));
        }
    }
}
