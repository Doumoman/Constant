#if LEGACY_DISABLED
using NUnit.Framework;
using StarNight.Core.State;
using StarNight.Stage.Lab;
using UnityEngine;

namespace StarNight.Stage.Tests
{
    public sealed class Core00LegacyRuntimeIsolationTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            FeatureFlag.ResetNewStageArchitecture();
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void NewArchitectureFlagPreventsLegacyLabConstruction()
        {
            FeatureFlag.SetNewStageArchitecture(true);
            root = new GameObject("CORE00_LegacyIsolationTest");

            Core04TwoRoomLab lab = root.AddComponent<Core04TwoRoomLab>();
            lab.BuildIfNeeded();

            Assert.That(lab.RuntimeRoot, Is.Null);
            Assert.That(root.transform.Find("Core04TwoRoomRuntime"), Is.Null);
        }
    }
}

#endif
