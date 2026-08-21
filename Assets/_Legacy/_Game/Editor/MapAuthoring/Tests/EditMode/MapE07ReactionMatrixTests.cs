#if LEGACY_DISABLED
using System.Linq;
using NUnit.Framework;
using StarNight.Map;
using StarNight.MapAuthoring.Editor;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests
{
    public sealed class MapE07ReactionMatrixTests
    {
        [Test]
        public void CommonAndMaruCatalogsHaveCompleteUnambiguousReactionContracts()
        {
            var definitions = CommonElementCatalogFactory.EnsureCatalog()
                .Concat(MaruElementCatalogFactory.EnsureCatalog())
                .ToArray();

            foreach (var definition in definitions)
            {
                var report = MapElementValidator.ValidateSourceForBake(definition);
                var matrixErrors = report.Issues.Where(issue =>
                    issue.Severity == ValidationSeverity.Error &&
                    issue.Code.StartsWith("TOOL_", System.StringComparison.Ordinal)).ToArray();
                Assert.That(matrixErrors, Is.Empty,
                    $"{definition.ElementId}: {string.Join(" | ", matrixErrors.Select(issue => issue.ToString()))}");

                foreach (var tool in ToolReactionMatrix.AtomicTools)
                {
                    var matchCount = definition.ToolReactions.Entries.Count(entry =>
                        entry != null && entry.Reaction != ElementReactionType.None &&
                        (entry.Tool & tool) != 0);
                    Assert.That(matchCount, Is.LessThanOrEqualTo(1),
                        $"{definition.ElementId}:{tool}");
                }
            }

            var cracked = definitions.Single(item =>
                item.CommonProfile.Kind == CommonElementKind.CrackedBlock);
            AssertReaction(cracked, ToolTag.Bomb, ElementReactionType.Break, 1);
            AssertReaction(cracked, ToolTag.Pickaxe, ElementReactionType.Break, 1);
            AssertReaction(cracked, ToolTag.Pound, ElementReactionType.Break, 1);
            AssertReaction(cracked, ToolTag.HeavyImpact, ElementReactionType.Break, 1);

            var soil = definitions.Single(item =>
                item.CommonProfile.Kind == CommonElementKind.SoftSoil);
            AssertReaction(soil, ToolTag.Shovel, ElementReactionType.Break, 1);
            AssertReaction(soil, ToolTag.Bomb, ElementReactionType.SetState, 1);
            AssertReaction(soil, ToolTag.Pickaxe, ElementReactionType.SetState, 1);
            AssertReaction(soil, ToolTag.LightImpact, ElementReactionType.SetState, 1);
            AssertReaction(soil, ToolTag.HeavyImpact, ElementReactionType.SetState, 1);

            var pendulum = definitions.Single(item =>
                item.CommonProfile.Kind == CommonElementKind.PendulumBall);
            AssertReaction(pendulum, ToolTag.Hook, ElementReactionType.Pull, 1);
            AssertReaction(pendulum, ToolTag.Bomb, ElementReactionType.Push, 1);

            var rolling = definitions.Single(item =>
                item.CommonProfile.Kind == CommonElementKind.RollingBoulder);
            AssertReaction(rolling, ToolTag.Bomb, ElementReactionType.SetState, 1);
            AssertReaction(rolling, ToolTag.Hook, ElementReactionType.Pull, 1);

            var container = definitions.Single(item =>
                item.CommonProfile.Kind == CommonElementKind.BreakableContainer);
            AssertReaction(container, ToolTag.LightImpact, ElementReactionType.Break, 1);
            AssertReaction(container, ToolTag.HeavyImpact, ElementReactionType.Break, 1);
            AssertReaction(container, ToolTag.Pickaxe, ElementReactionType.Break, 1);
            AssertReaction(container, ToolTag.Pound, ElementReactionType.Break, 1);
            AssertReaction(container, ToolTag.Bomb, ElementReactionType.Break, 1);

            var ropeAnchor = definitions.Single(item =>
                item.CommonProfile.Kind == CommonElementKind.RopeAnchor);
            AssertReaction(ropeAnchor, ToolTag.Rope, ElementReactionType.SetState, 1);

            var hookAnchor = definitions.Single(item =>
                item.CommonProfile.Kind == CommonElementKind.HookAnchor);
            AssertReaction(hookAnchor, ToolTag.Hook, ElementReactionType.Pull, 1);

            var unbreakable = definitions.Single(item =>
                item.CommonProfile.Kind == CommonElementKind.UnbreakableBlock);
            Assert.That(unbreakable.ToolReactions.Entries, Is.Empty);

            var invalid = ScriptableObject.CreateInstance<MapElementDefinition>();
            try
            {
                CommonElementCatalogFactory.Configure(invalid, "COMMON_Block_SoftSoil");
                invalid.ToolReactions.Entries.Add(new ToolReactionEntry
                {
                    Tool = ToolTag.Pickaxe | ToolTag.HeavyImpact,
                    Reaction = ElementReactionType.Break,
                    StrengthRequired = 1,
                });
                var invalidReport = MapElementValidator.ValidateSourceForBake(invalid);
                Assert.That(invalidReport.Issues.Any(issue =>
                    issue.Code == "TOOL_REACTION_AMBIGUOUS"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(invalid);
            }
        }

        private static void AssertReaction(
            MapElementDefinition definition,
            ToolTag tool,
            ElementReactionType reaction,
            int strength)
        {
            Assert.That(definition.ToolReactions.TryResolve(tool, out var entry, out var matched),
                Is.True, $"{definition.ElementId}:{tool}");
            Assert.That(matched, Is.EqualTo(tool));
            Assert.That(entry.Reaction, Is.EqualTo(reaction));
            Assert.That(entry.StrengthRequired, Is.EqualTo(strength));
            Assert.That(ToolReactionReceiver.ResolveFeedback(entry), Is.Not.EqualTo(FeedbackId.None));
        }
    }
}

#endif
