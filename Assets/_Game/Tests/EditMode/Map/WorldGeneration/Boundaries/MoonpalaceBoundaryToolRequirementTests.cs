using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;

namespace StarNight.Map.Tests.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceBoundaryToolRequirementTests
    {
        private static readonly string[] AcceptedTokens =
        {
            "NONE",
            "Pickaxe",
            "Rope",
            "Bomb",
            "KeyItem",
        };

        public static IEnumerable ToolRequirementCases
        {
            get
            {
                for (var index = 0; index < 200; index++)
                {
                    yield return new TestCaseData(index)
                        .SetName("BoundaryToolRequirementContract_" + index.ToString("D3"));
                }
            }
        }

        [TestCaseSource(nameof(ToolRequirementCases))]
        public void BoundaryToolRequirementContract(int caseIndex)
        {
            if (caseIndex % 2 == 0)
            {
                AssertAcceptedToken(AcceptedTokens[(caseIndex / 2) % AcceptedTokens.Length]);
            }
            else
            {
                AssertRejectedToken(CreateRejectedToken(caseIndex / 2));
            }
        }

        private static void AssertAcceptedToken(string token)
        {
            Assert.That(MoonpalaceBoundaryToolRequirement.TryParse(token, out var parsed), Is.True);
            Assert.That(parsed.IsDefined, Is.True);
            Assert.That(parsed.Token, Is.EqualTo(token));
            Assert.That(parsed.ToString(), Is.EqualTo(token));
            Assert.That(MoonpalaceBoundaryToolRequirement.Parse(token), Is.EqualTo(parsed));
            Assert.That(parsed.GetHashCode(), Is.EqualTo(
                MoonpalaceBoundaryToolRequirement.Parse(token).GetHashCode()));
        }

        private static void AssertRejectedToken(string token)
        {
            Assert.That(MoonpalaceBoundaryToolRequirement.TryParse(token, out var parsed), Is.False);
            Assert.That(parsed.IsDefined, Is.False);
            Assert.Throws<ArgumentException>(() => MoonpalaceBoundaryToolRequirement.Parse(token));
        }

        private static string CreateRejectedToken(int ordinal)
        {
            switch (ordinal % 10)
            {
                case 0:
                    return null;
                case 1:
                    return string.Empty;
                case 2:
                    return " ";
                case 3:
                    return "none";
                case 4:
                    return "None";
                case 5:
                    return "PICKAXE";
                case 6:
                    return " Pickaxe";
                case 7:
                    return "Rope ";
                case 8:
                    return "Bomb" + ordinal;
                default:
                    return "Unknown-" + ordinal;
            }
        }
    }
}
