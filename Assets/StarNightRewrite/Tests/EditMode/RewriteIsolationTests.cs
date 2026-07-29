using System.IO;
using NUnit.Framework;
using StarNight.Rewrite.Core;
using StarNight.Rewrite.Player;
using UnityEngine;

namespace StarNight.Rewrite.Tests
{
    public sealed class RewriteIsolationTests
    {
        private static string RewriteRoot =>
            Path.Combine(Application.dataPath, "StarNightRewrite");

        [Test]
        public void RewriteAssemblies_DoNotReferenceLegacyRuntime()
        {
            string forbiddenAssembly = string.Concat("StarFetching", "Night.Runtime");
            string[] assemblyDefinitions = Directory.GetFiles(
                RewriteRoot,
                "*.asmdef",
                SearchOption.AllDirectories);

            Assert.That(assemblyDefinitions, Is.Not.Empty);

            foreach (string path in assemblyDefinitions)
            {
                StringAssert.DoesNotContain(
                    forbiddenAssembly,
                    File.ReadAllText(path),
                    path);
            }
        }

        [Test]
        public void RewriteRuntimeSources_DoNotUseLegacyNamespace()
        {
            string forbiddenNamespace = string.Concat("StarFetching", "Night");
            string runtimeRoot = Path.Combine(RewriteRoot, "Runtime");
            string[] runtimeSources = Directory.GetFiles(
                runtimeRoot,
                "*.cs",
                SearchOption.AllDirectories);

            Assert.That(runtimeSources, Is.Not.Empty);

            foreach (string path in runtimeSources)
            {
                StringAssert.DoesNotContain(
                    forbiddenNamespace,
                    File.ReadAllText(path),
                    path);
            }
        }

        [Test]
        public void RewriteContract_MatchesRw0Bootstrap()
        {
            Assert.That(RewriteSceneRoot.ContractVersion, Is.EqualTo("RW0-v1"));
            Assert.That(PlayerAssemblyMarker.ContractVersion, Is.EqualTo(1));
        }
    }
}
