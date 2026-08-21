#if LEGACY_DISABLED
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Stage.Layout;
using StarNight.Stage.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace StarNight.Map.Tests
{
    public sealed class MapE09ProceduralGenerationPlayModeTests
    {
        [UnityTest]
        public IEnumerator SameSeedReproducesWhileDifferentSeedsVaryAndKeepMainRoute()
        {
            StageMapProfile profile = ScriptableObject.CreateInstance<StageMapProfile>();
            profile.StageId = "PLAY-E09";
            profile.MinRooms = 6;
            profile.MaxRooms = 8;
            profile.MainRouteLengthRange = new Vector2Int(4, 6);
            profile.BranchCountRange = new Vector2Int(1, 2);
            profile.LoopCountRange = new Vector2Int(0, 1);
            profile.AllowedFamilies = new List<LayoutFamily>
            {
                LayoutFamily.LinearBend,
                LayoutFamily.VerticalSpine,
                LayoutFamily.TwinBranchMerge,
                LayoutFamily.BrokenSpiral,
                LayoutFamily.HubAndSpokes,
            };
            var templates = new List<RoomTemplate>
            {
                CreateTemplate("MicroStart", RoomSizeCatalog.Micro, RoomRole.Start),
                CreateTemplate("WideMain", RoomSizeCatalog.Wide, RoomRole.Main),
                CreateTemplate("TallBranch", RoomSizeCatalog.Tall, RoomRole.Branch),
                CreateTemplate("LargeExit", RoomSizeCatalog.Large, RoomRole.Exit),
                CreateTemplate("LongRest", RoomSizeCatalog.LongHall, RoomRole.Rest),
                CreateTemplate("ShaftSecret", RoomSizeCatalog.DeepShaft, RoomRole.Secret),
            };

            StageGeneratedLayout first = StageMapGenerator.Generate(profile, templates, 77);
            StageGeneratedLayout repeated = StageMapGenerator.Generate(profile, templates, 77);
            StageGeneratedLayout different = StageMapGenerator.Generate(profile, templates, 78);
            Assert.That(first.ValidationHash, Is.EqualTo(repeated.ValidationHash));
            Assert.That(different.ValidationHash, Is.Not.EqualTo(first.ValidationHash));
            Assert.That(first.HasValidMainRoute, Is.True);
            Assert.That(different.HasValidMainRoute, Is.True);
            Assert.That(first.ErrorCount, Is.Zero);
            Assert.That(different.ErrorCount, Is.Zero);

            Object.Destroy(profile);
            for (int index = 0; index < templates.Count; index++) Object.Destroy(templates[index]);
            yield return null;
        }

        private static RoomTemplate CreateTemplate(string id, Vector2Int size, RoomRole role)
        {
            RoomTemplate template = ScriptableObject.CreateInstance<RoomTemplate>();
            template.RoomId = id;
            template.SizeCells = size;
            template.Role = role;
            template.Sockets = new List<RoomSocketDefinition>
            {
                CreateSocket("Left", CardinalDirection.Left, new Vector2Int(0, 2), TraversalType.Walk, 2),
                CreateSocket("Right", CardinalDirection.Right, new Vector2Int(size.x, 2), TraversalType.Walk, 2),
                CreateSocket("Up", CardinalDirection.Up, new Vector2Int(size.x / 2, size.y), TraversalType.Climb, 0),
                CreateSocket("Down", CardinalDirection.Down, new Vector2Int(size.x / 2, 0), TraversalType.Climb, 0),
            };
            return template;
        }

        private static RoomSocketDefinition CreateSocket(string id, CardinalDirection side, Vector2Int cell, TraversalType traversal, int floor)
        {
            return new RoomSocketDefinition
            {
                SocketGuid = id,
                Side = side,
                LocalCell = cell,
                OpeningSizeCells = Vector2Int.one,
                Traversal = traversal,
                MainRouteAllowed = true,
                FloorHeightCell = floor,
            };
        }
    }
}

#endif
