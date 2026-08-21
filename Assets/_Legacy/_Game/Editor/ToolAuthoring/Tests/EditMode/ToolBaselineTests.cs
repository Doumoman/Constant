#if LEGACY_DISABLED
using NUnit.Framework;
using StarNight.ToolAuthoring.Editor;

namespace StarNight.ToolAuthoring.Tests
{
    public sealed class ToolBaselineTests
    {
        [Test]
        public void Tool00Build_FixesApprovedContractAndIsolatesLegacyCode()
        {
            ToolImplementationBaseline baseline = ToolBaselineBuilder.Build();
            string[] errors = ToolBaselineBuilder.Validate(baseline);

            Assert.That(errors, Is.Empty, string.Join("\n", errors));
            Assert.That(baseline.InputActionSha256, Is.EqualTo(baseline.InputActionBackupSha256));
            Assert.That(baseline.DisabledLegacyToolCode, Has.Length.EqualTo(3));
            Assert.That(
                baseline.AssemblyBoundaryStatus,
                Is.EqualTo("IsolatedFromLegacyAssemblyCSharp"));
            Assert.That(
                baseline.LayerMigrationStatus,
                Is.EqualTo("Applied")
                    .Or.EqualTo("FrozenContract_MigrationPendingExplicitApproval"));
        }
    }
}

#endif
