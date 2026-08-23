using System;
using System.Collections.Generic;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public static class MicrochunkTransformer
    {
        public static MicrochunkTransformResult Transform(
            MicrochunkDefinition definition,
            MicrochunkTransform transform)
        {
            return Transform(definition, transform, MicrochunkTransformOptions.Default);
        }

        public static MicrochunkTransformResult Transform(
            MicrochunkDefinition definition,
            MicrochunkTransform transform,
            MicrochunkTransformOptions options)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (options == null) throw new ArgumentNullException(nameof(options));
            MicrochunkTransformUtility.ValidateTransform(transform);

            var transformed = new MicrochunkDefinition(
                options.ProjectId(definition.Id, transform),
                definition.DisplayName,
                definition.WidthTiles,
                definition.HeightTiles,
                definition.UsageClass,
                definition.BiomeIds,
                definition.RouteRoles,
                definition.AllowedTransforms,
                definition.SelectionWeight,
                definition.Threat,
                definition.Cognitive,
                definition.Chain,
                definition.TileDataComplete,
                definition.PrefabId,
                definition.Active,
                definition.Notes,
                TransformCells(definition.TileCells, transform, options),
                TransformSockets(definition.Sockets, transform, options),
                TransformObjectSlots(definition.ObjectSlots, transform));

            return new MicrochunkTransformResult(definition, transformed, transform);
        }

        private static IEnumerable<MicrochunkTileCell> TransformCells(
            IEnumerable<MicrochunkTileCell> cells,
            MicrochunkTransform transform,
            MicrochunkTransformOptions options)
        {
            foreach (var cell in cells)
            {
                yield return new MicrochunkTileCell(
                    MicrochunkTransformUtility.TransformCoordinate(cell.Coordinate, transform),
                    options.RemapTileCode(cell.GroundCode, MicrochunkTileLayer.GroundSolid, transform),
                    options.RemapTileCode(cell.OneWayCode, MicrochunkTileLayer.OneWay, transform),
                    options.RemapTileCode(cell.BreakableCode, MicrochunkTileLayer.Breakable, transform),
                    options.RemapTileCode(cell.HazardCode, MicrochunkTileLayer.Hazard, transform),
                    options.RemapTileCode(cell.LiquidCode, MicrochunkTileLayer.Liquid, transform),
                    options.RemapTileCode(cell.DecorationBackCode, MicrochunkTileLayer.DecorationBack, transform),
                    options.RemapTileCode(cell.DecorationFrontCode, MicrochunkTileLayer.DecorationFront, transform),
                    options.RemapTileCode(cell.MarkerCode, MicrochunkTileLayer.Marker, transform));
            }
        }

        private static IEnumerable<MicrochunkSocketDefinition> TransformSockets(
            IEnumerable<MicrochunkSocketDefinition> sockets,
            MicrochunkTransform transform,
            MicrochunkTransformOptions options)
        {
            foreach (var socket in sockets)
            {
                var transformedSide = MicrochunkTransformUtility.TransformSide(socket.Side, transform);
                yield return new MicrochunkSocketDefinition(
                    socket.SocketId,
                    transformedSide,
                    options.RemapSocketBand(socket.Side, transformedSide, socket.BandId, transform),
                    socket.TraversalKind,
                    socket.Direction,
                    socket.MandatoryAllowed,
                    socket.ToolRequirement,
                    socket.EdgeSignatureId,
                    socket.RouteLayer,
                    socket.MinimumSafeTiles,
                    socket.Notes);
            }
        }

        private static IEnumerable<MicrochunkObjectSlotDefinition> TransformObjectSlots(
            IEnumerable<MicrochunkObjectSlotDefinition> objectSlots,
            MicrochunkTransform transform)
        {
            foreach (var slot in objectSlots)
            {
                yield return new MicrochunkObjectSlotDefinition(
                    slot.SlotId,
                    MicrochunkTransformUtility.TransformCoordinate(slot.Anchor, transform),
                    slot.Category,
                    slot.AllowedPoolId,
                    slot.Required,
                    MicrochunkTransformUtility.TransformOrientation(slot.Orientation, transform),
                    slot.VisibleFromRoute,
                    slot.ForbiddenRadiusTiles,
                    slot.RequiredMarkerCode,
                    slot.Notes);
            }
        }
    }
}
