#if LEGACY_DISABLED
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.MapAuthoring.Editor;
using StarNight.Stage.Layout;
using StarNight.Stage.Layout.Authoring;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests
{
    public sealed class MapE09ProceduralPreviewTests
    {
        [Test]
        public void SixteenSeedPreviewIsDeterministicVariableAndMainRouteSafe()
        {
            StageMapProfile profile = StageMapProfileSampleFactory.EnsureSample();
            IReadOnlyList<RoomTemplate> templates = RoomTemplateSampleFactory.EnsureSamples();
            StageGeneratedLayout first = StageMapGenerator.Generate(profile, templates, 10801);
            StageGeneratedLayout repeated = StageMapGenerator.Generate(profile, templates, 10801);
            Assert.That(repeated.ValidationHash, Is.EqualTo(first.ValidationHash));
            Assert.That(repeated.Rooms.Count, Is.EqualTo(first.Rooms.Count));

            var hashes = new HashSet<string>();
            var placementFingerprints = new HashSet<string>();
            for (int index = 0; index < 16; index++)
            {
                StageGeneratedLayout generated = StageMapGenerator.Generate(profile, templates, 10801 + index);
                Assert.That(generated.ErrorCount, Is.Zero, $"Seed {generated.Seed} / {generated.ValidationHash}");
                Assert.That(generated.HasValidMainRoute, Is.True, $"Seed {generated.Seed}");
                Assert.That(generated.Connections, Has.All.Matches<StageGeneratedConnection>(edge =>
                    edge.IsValid && edge.Bidirectional && !edge.RequiresCorridor));
                hashes.Add(generated.ValidationHash);
                placementFingerprints.Add(CreatePlacementFingerprint(generated));
            }
            Assert.That(hashes.Count, Is.GreaterThan(1));
            Assert.That(placementFingerprints.Count, Is.GreaterThan(1));

            EditorSceneManager.OpenScene(EditorSceneBuildGuard.StageLayoutLabPath);
            StageLayoutPreviewApplier.Apply(first, false);
            Assert.That(Object.FindObjectsByType<StageRoomProxy>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.EqualTo(first.Rooms.Count));
            Assert.That(Object.FindObjectsByType<StageCorridorProxy>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.Zero);
            Assert.That(Object.FindObjectsByType<StageElementSlotPreview>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.GreaterThan(0));
            Assert.That(StageLayoutValidator.ValidateCurrentScene().ErrorCount, Is.Zero);
        }

        private static string CreatePlacementFingerprint(StageGeneratedLayout layout)
        {
            var parts = new List<string>();
            for (int index = 0; index < layout.Rooms.Count; index++)
            {
                StageGeneratedRoom room = layout.Rooms[index];
                parts.Add($"{room.Template.SizeCells.x}x{room.Template.SizeCells.y}@{room.PositionCells.x},{room.PositionCells.y}");
            }
            return string.Join("|", parts);
        }
    }
}

#endif
