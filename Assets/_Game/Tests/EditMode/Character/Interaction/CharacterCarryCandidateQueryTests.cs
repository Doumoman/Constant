using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Character.Interaction;
using UnityEngine;

namespace StarNight.Character.Tests.Interaction
{
    public sealed class CharacterCarryCandidateQueryTests
    {
        private static CharacterCarryCandidate Candidate(
            int id,
            Vector2 position,
            float width = 1f,
            float height = 1f,
            bool carryable = true,
            bool reachable = true,
            int priority = 0,
            CharacterCarryCandidateKind kind = CharacterCarryCandidateKind.OrdinaryCarryable)
        {
            return new CharacterCarryCandidate(
                id, kind, position, width, height, carryable, reachable, priority);
        }

        [Test]
        public void CarryCandidateQuery_SelectsSingleCandidateByDeterministicPriority()
        {
            var player = Vector2.zero;

            // 1) 명시적 우선순위가 거리보다 우선한다.
            var byPriority = new List<CharacterCarryCandidate>
            {
                Candidate(1, new Vector2(0.5f, 0f), priority: 5),
                Candidate(2, new Vector2(3f, 0f), priority: 1)
            };
            CharacterCarryCandidate selected;

            Assert.That(CharacterCarryCandidateQuery.TrySelectCandidate(
                player, byPriority, out selected), Is.True);
            Assert.That(selected.Id, Is.EqualTo(2));

            // 2) 우선순위가 같으면 가까운 후보.
            var byDistance = new List<CharacterCarryCandidate>
            {
                Candidate(3, new Vector2(2f, 0f)),
                Candidate(4, new Vector2(0.7f, 0f))
            };

            Assert.That(CharacterCarryCandidateQuery.TrySelectCandidate(
                player, byDistance, out selected), Is.True);
            Assert.That(selected.Id, Is.EqualTo(4));

            // 3) 거리까지 같으면 낮은 id 타이브레이크 — 항상 정확히 하나.
            var byId = new List<CharacterCarryCandidate>
            {
                Candidate(9, new Vector2(1f, 0f)),
                Candidate(7, new Vector2(-1f, 0f))
            };

            Assert.That(CharacterCarryCandidateQuery.TrySelectCandidate(
                player, byId, out selected), Is.True);
            Assert.That(selected.Id, Is.EqualTo(7));

            // 기절 소형 적도 같은 계약으로 선택 가능하다.
            var stunned = new List<CharacterCarryCandidate>
            {
                Candidate(11, new Vector2(0.4f, 0f),
                    kind: CharacterCarryCandidateKind.StunnedSmallEnemy)
            };

            Assert.That(CharacterCarryCandidateQuery.TrySelectCandidate(
                player, stunned, out selected), Is.True);
            Assert.That(selected.Kind,
                Is.EqualTo(CharacterCarryCandidateKind.StunnedSmallEnemy));
        }

        [Test]
        public void CarryCandidateQuery_RejectsOversizedOrNonCarryableCandidates()
        {
            var player = Vector2.zero;
            var candidates = new List<CharacterCarryCandidate>
            {
                Candidate(1, new Vector2(0.3f, 0f), width: 1.5f),          // 1×1 초과
                Candidate(2, new Vector2(0.4f, 0f), height: 2f),           // 1×1 초과
                Candidate(3, new Vector2(0.5f, 0f), carryable: false),     // 휴대 불가
                Candidate(4, new Vector2(0.6f, 0f), reachable: false)      // 도달 불가
            };
            CharacterCarryCandidate selected;

            // 적격 후보가 없으면 선택도 없다(들기 없음).
            Assert.That(CharacterCarryCandidateQuery.TrySelectCandidate(
                player, candidates, out selected), Is.False);

            // 적격 후보가 섞이면 그 후보만 선택된다.
            candidates.Add(Candidate(5, new Vector2(2f, 0f)));

            Assert.That(CharacterCarryCandidateQuery.TrySelectCandidate(
                player, candidates, out selected), Is.True);
            Assert.That(selected.Id, Is.EqualTo(5));
        }
    }
}
