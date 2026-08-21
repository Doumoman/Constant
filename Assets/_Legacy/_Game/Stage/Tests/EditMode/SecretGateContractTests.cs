#if LEGACY_DISABLED
using System;
using NUnit.Framework;
using StarNight.Interaction.Targeting;
using StarNight.Map;
using StarNight.Stage.Secrets;
using UnityEngine;

namespace StarNight.Stage.Tests
{
    public sealed class SecretGateContractTests
    {
        [Test]
        public void FiveGateTypesEnforceOpeningAndBlindDiscoveryRules()
        {
            Assert.That(Enum.GetValues(typeof(SecretGateType)), Has.Length.EqualTo(5));

            Assert.That(SecretGateContract.OpensFromTool(SecretGateType.CrackedWall, ToolTag.Bomb), Is.True);
            Assert.That(SecretGateContract.OpensFromTool(SecretGateType.CrackedWall, ToolTag.Pickaxe), Is.True);
            Assert.That(SecretGateContract.OpensFromTool(SecretGateType.CrackedWall, ToolTag.HeavyImpact), Is.True);
            Assert.That(SecretGateContract.OpensFromTool(SecretGateType.CrackedWall, ToolTag.Shovel), Is.False);

            Assert.That(SecretGateContract.OpensFromTool(SecretGateType.DirtSeal, ToolTag.Shovel), Is.True);
            Assert.That(SecretGateContract.OpensFromTool(SecretGateType.DirtSeal, ToolTag.Bomb | ToolTag.Pickaxe), Is.False);

            Assert.That(SecretGateContract.OpensFromTool(SecretGateType.ThinFloor, ToolTag.Pound), Is.True);
            Assert.That(SecretGateContract.OpensFromTool(SecretGateType.ThinFloor, ToolTag.HeavyImpact), Is.True);
            Assert.That(SecretGateContract.OpensFromTool(SecretGateType.ThinFloor, ToolTag.Bomb), Is.True);
            Assert.That(SecretGateContract.OpensFromTool(SecretGateType.ThinFloor, ToolTag.Pickaxe), Is.False);

            Assert.That(SecretGateContract.OpensFromTool(SecretGateType.MechanismSeal, ToolTag.HeavyImpact), Is.False);
            Assert.That(SecretGateContract.OpensFromContext(SecretGateType.MechanismSeal, false), Is.True);
            Assert.That(SecretGateContract.OpensFromTool(SecretGateType.BlindPanel, ToolTag.Bomb), Is.False);
            Assert.That(SecretGateContract.OpensFromContext(SecretGateType.BlindPanel, false), Is.False);

            GameObject blindObject = new GameObject("BlindPanelContract");
            GameObject restoredObject = new GameObject("BlindPanelRestored");
            try
            {
                SecretAnchor blind = blindObject.AddComponent<SecretAnchor>();
                blind.Configure("BLIND", 1, "BLIND_SECRET", null, null, null, SecretGateType.BlindPanel);
                Assert.That(blind.IsDiscovered, Is.False);
                Assert.That(blind.CanReceive(new ContextReceiverQuery(null, null)), Is.False);

                ToolReactionResult discovery = blind.TryReact(new ToolReactionContext
                {
                    ActionId = 1,
                    Tags = ToolTag.Bomb | ToolTag.HeavyImpact,
                });
                Assert.That(discovery.Accepted, Is.True);
                Assert.That(discovery.ChangedState, Is.True);
                Assert.That(discovery.ConsumeToolResource, Is.True);
                Assert.That(blind.IsDiscovered, Is.True);
                Assert.That(blind.IsRevealed, Is.False, "Discovery must not open a BlindPanel.");
                Assert.That(blind.CanReceive(new ContextReceiverQuery(null, null)), Is.True);

                string snapshot = blind.CaptureRoomState();
                SecretAnchor restored = restoredObject.AddComponent<SecretAnchor>();
                restored.Configure("BLIND_RESTORED", 2, "BLIND_SECRET_2", null, null, null, SecretGateType.BlindPanel);
                restored.RestoreRoomState(snapshot);
                Assert.That(restored.IsDiscovered, Is.True);
                Assert.That(restored.IsRevealed, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(blindObject);
                UnityEngine.Object.DestroyImmediate(restoredObject);
            }
        }
    }
}

#endif
