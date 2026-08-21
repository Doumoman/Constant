#if LEGACY_DISABLED
using NUnit.Framework;
using StarNight.Map;
using StarNight.MapAuthoring.Editor;
using StarNight.Stage.CameraSystem;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests
{
    public sealed class GCore08EditorLabsTests
    {
        [Test]
        public void LabsPreviewSeedCameraSecretAnchorAndDuplicateContract()
        {
            var room = GlobalCoreEditorLabModels.GenerateRoomTiles(10801, new Vector2Int(4, 3));
            Assert.That(room.HasT0MainRoute, Is.True);
            Assert.That(room.ValidationErrors, Is.Empty);

            StageCameraLabResult camera = GlobalCoreEditorLabModels.PreviewCamera(
                new Vector2Int(24, 16),
                CameraTileProfile.ReferenceAspect);
            Assert.That(camera.VisibleHeightTiles, Is.EqualTo(11f));
            Assert.That(camera.VisibleWidthTiles, Is.EqualTo(11f * 16f / 9f).Within(0.001f));

            SecretDimensionLabResult secret = GlobalCoreEditorLabModels.PreviewSecret(
                10801,
                "ROOM_A",
                "ANCHOR_01",
                new Vector2Int(2, 1),
                ToolTag.Pickaxe);
            Assert.That(secret.IsValid, Is.True, secret.Failure);
            Assert.That(secret.MainPortalId, Is.EqualTo("SECRET_ANCHOR_01"));
            Assert.That(secret.ReturnSafeCell, Is.EqualTo(new Vector2Int(2, 1)));

            InventoryInteractionLabState inventory = GlobalCoreEditorLabModels.CreateInventoryState(1001, 1, 10);
            inventory = GlobalCoreEditorLabModels.ApplyDuplicate(inventory);
            Assert.That(inventory.CurrentDurability, Is.EqualTo(10));
            Assert.That(inventory.LastFeedbackMessage, Is.EqualTo("내구도 완전 회복"));
            Assert.That(inventory.RuntimeCopyReplaced, Is.False);

            inventory = GlobalCoreEditorLabModels.DepleteWithoutAutoSwap(inventory);
            Assert.That(inventory.CurrentDurability, Is.Zero);
            Assert.That(inventory.RuntimeCopyReplaced, Is.False);
        }
    }
}

#endif
