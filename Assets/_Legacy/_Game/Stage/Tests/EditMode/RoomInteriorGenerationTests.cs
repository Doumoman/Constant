#if LEGACY_DISABLED
using System.Linq;
using NUnit.Framework;
using StarNight.Map;
using StarNight.Map.Placement;
using StarNight.Stage.Layout;
using StarNight.Tools.Bomb;
using UnityEngine;

namespace StarNight.Stage.Tests
{
    public sealed class RoomInteriorGenerationTests
    {
        [Test]
        public void CommonRoomWithoutRegionProfileKeepsMicroChunkAndT0Contracts()
        {
            RoomInteriorLayout first = RoomInteriorGenerator.GenerateCommonTestRoom(1701);
            RoomInteriorLayout repeated = RoomInteriorGenerator.GenerateCommonTestRoom(1701);
            RoomInteriorLayout different = RoomInteriorGenerator.GenerateCommonTestRoom(1702);

            Assert.That(first.ChunkGridSize, Is.EqualTo(new Vector2Int(4, 3)));
            Assert.That(first.Chunks.Count, Is.EqualTo(12));
            Assert.That(first.ValidationErrors, Is.Empty);
            Assert.That(first.HasT0MainRoute, Is.True);
            Assert.That(first.ValidationHash, Is.EqualTo(repeated.ValidationHash));
            Assert.That(first.ValidationHash, Is.Not.EqualTo(different.ValidationHash));

            foreach (GeneratedMicroChunk chunk in first.Chunks)
            {
                Assert.That(chunk.Cells.Length,
                    Is.EqualTo(GeneratedMicroChunk.Width * GeneratedMicroChunk.Height));
                Assert.That(chunk.OriginCell, Is.EqualTo(new Vector2Int(
                    chunk.GridCell.x * GeneratedMicroChunk.Width,
                    chunk.GridCell.y * GeneratedMicroChunk.Height)));
                foreach (GeneratedMicroSocket socket in chunk.Sockets)
                {
                    Assert.That(chunk.GetCell(socket.LocalCell), Is.EqualTo(MicroCellKind.Empty));
                }
            }

            Assert.That(first.Chunks.Where(chunk => chunk.MainRoute)
                .All(chunk => chunk.Role != ChunkPatternRole.Condition), Is.True);
            Assert.That(first.Chunks.GroupBy(chunk => chunk.PatternId)
                .All(group => group.Count() <= 2), Is.True);
        }

        [Test]
        public void SoftSoilPocketAndToolEscapeFollowGlobalContracts()
        {
            RoomInteriorLayout layout = RoomInteriorGenerator.GenerateCommonTestRoom(2701);
            Assert.That(layout.ValidationErrors, Is.Empty);
            Assert.That(layout.HiddenContents.Count, Is.EqualTo(1));
            GeneratedHiddenContent hidden = layout.HiddenContents[0];
            Assert.That(layout.FindChunk(hidden.ChunkGridCell).MainRoute, Is.False);
            Assert.That(hidden.RevealTools & (ToolTag.Bomb | ToolTag.Pickaxe | ToolTag.Shovel),
                Is.EqualTo(ToolTag.Bomb | ToolTag.Pickaxe | ToolTag.Shovel));

            Assert.That(layout.ToolEscapes.Count, Is.EqualTo(1));
            GeneratedToolEscape escape = layout.ToolEscapes[0];
            Assert.That(layout.FindChunk(escape.ChunkGridCell).MainRoute, Is.False);
            var escapeState = new ToolEscapeRuntimeState();
            escapeState.Configure(escape);
            escapeState.NotifyRequiredToolAvailable(false, 10f);
            escapeState.Tick(11.19f, false);
            Assert.That(escapeState.RecoveryRackHasTool, Is.False);
            escapeState.Tick(11.21f, false);
            Assert.That(escapeState.RecoveryRackHasTool, Is.True);
            Assert.That(escapeState.TryTakeRecoveryTool(), Is.True);
            escapeState.Configure(escape);
            escapeState.TickAbandonHold(true, 1f);
            escapeState.TickAbandonHold(true, 1f);
            Assert.That(escapeState.OpenReason, Is.EqualTo(ToolEscapeOpenReason.RewardAbandoned));
            Assert.That(escapeState.RewardForfeited, Is.True);
            escapeState.Configure(escape);
            escapeState.Tick(0f, true);
            Assert.That(escapeState.OpenReason, Is.EqualTo(ToolEscapeOpenReason.ThirdBellEmergency));

            Assert.That(BombExplosionDispatcher.ApprovedCellOffsets.Count, Is.EqualTo(13));
            Assert.That(BombExplosionDispatcher.ApprovedCellOffsets, Contains.Item(new Vector2Int(2, 0)));
            Assert.That(SoftSoilContract.ExplosionAbsorptionCost, Is.EqualTo(1));
            var soilTrace = SoftSoilContract.TraceExplosion(
                Vector2Int.zero,
                cell => cell == Vector2Int.right);
            Assert.That(soilTrace.Any(cell => cell.Cell == Vector2Int.right &&
                                             cell.IsSoftSoil && cell.RemainingEnergy == 0), Is.True);
            Assert.That(soilTrace.Any(cell => cell.Cell == Vector2Int.right * 2), Is.False);
            Assert.That(soilTrace.Any(cell => cell.Cell == Vector2Int.left * 2), Is.True);
            Assert.That(SoftSoilContract.ReduceImpactGrade(ToolTag.HeavyImpact), Is.EqualTo(ToolTag.LightImpact));
            Assert.That(SoftSoilContract.ReduceImpactGrade(ToolTag.LightImpact), Is.EqualTo(ToolTag.None));

            GeneratedMicroChunk entryChunk = layout.FindChunk(new Vector2Int(0, layout.ChunkGridSize.y / 2));
            Vector2Int unsafeSupportWorldCell = layout.EntryWorldCell + Vector2Int.down;
            Vector2Int unsafeSupportLocalCell = unsafeSupportWorldCell - entryChunk.OriginCell;
            MicroCellKind originalSupport = entryChunk.GetCell(unsafeSupportLocalCell);
            entryChunk.SetCell(unsafeSupportLocalCell, MicroCellKind.SoftSoil);
            Assert.That(RoomInteriorValidator.Validate(layout),
                Has.Some.Contains("Portal safe floor cannot depend on removable soil"));
            entryChunk.SetCell(unsafeSupportLocalCell, originalSupport);

            GameObject soilObject = new GameObject("SoftSoilContractTest");
            MapElementDefinition definition = ScriptableObject.CreateInstance<MapElementDefinition>();
            try
            {
                definition.ElementId = "COMMON_Block_SoftSoil_Test";
                definition.CommonProfile.Kind = CommonElementKind.SoftSoil;
                definition.BehaviorProfile.InitialState = MapElementState.Idle;
                GridOccupier occupier = soilObject.AddComponent<GridOccupier>();
                occupier.Configure(Vector2Int.zero, definition.Footprint, OccupancyLayer.Fixture);
                soilObject.AddComponent<ElementRuntimeId>();
                soilObject.AddComponent<ElementStateMachine>();
                MapElementInstance instance = soilObject.AddComponent<MapElementInstance>();
                CommonElementDriver driver = soilObject.AddComponent<CommonElementDriver>();
                ToolReactionReceiver reactions = soilObject.AddComponent<ToolReactionReceiver>();
                instance.Configure(definition, null, "soft_soil_test");
                instance.SetMapRoomState(MapRoomState.Active);
                driver.Rebind();

                ToolReactionResult pickaxe = reactions.TryReact(new ToolReactionContext
                {
                    ActionId = 1,
                    Tags = ToolTag.Pickaxe | ToolTag.LightImpact,
                });
                ToolReactionResult bomb = reactions.TryReact(new ToolReactionContext
                {
                    ActionId = 2,
                    Tags = ToolTag.Bomb | ToolTag.HeavyImpact,
                });
                ToolReactionResult cushioned = driver.NotifyImpact(
                    20,
                    2f,
                    3f,
                    Vector2Int.down,
                    forceHeavyImpact: true);
                ToolReactionResult shovel = reactions.TryReact(new ToolReactionContext
                {
                    ActionId = 3,
                    Tags = ToolTag.Shovel | ToolTag.LightImpact,
                });
                Assert.That(pickaxe.Accepted && pickaxe.ConsumeToolResource && !pickaxe.ChangedState, Is.True);
                Assert.That(bomb.Accepted && !bomb.ConsumeToolResource && !bomb.ChangedState, Is.True);
                Assert.That(cushioned.Accepted, Is.True);
                Assert.That(driver.LastResolvedImpactTags, Is.EqualTo(ToolTag.LightImpact));
                Assert.That(shovel.Accepted && shovel.ConsumeToolResource && shovel.ChangedState, Is.True);
                Assert.That(instance.CurrentState, Is.EqualTo(MapElementState.Broken));

                GameObject pocketObject = new GameObject("EmbeddedPocketContractTest");
                try
                {
                    EmbeddedPocketRuntime pocket = pocketObject.AddComponent<EmbeddedPocketRuntime>();
                    pocket.Configure(hidden);
                    ToolReactionResult reveal = pocket.TryReact(new ToolReactionContext
                    {
                        ActionId = 10,
                        Tags = ToolTag.Bomb,
                    });
                    Assert.That(reveal.Accepted && reveal.ChangedState && !reveal.ConsumeToolResource, Is.True);
                    Assert.That(pocket.TryCollect(), Is.True);
                }
                finally
                {
                    Object.DestroyImmediate(pocketObject);
                }
            }
            finally
            {
                Object.DestroyImmediate(soilObject);
                Object.DestroyImmediate(definition);
            }
        }
    }
}

#endif
