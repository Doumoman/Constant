using System;
using System.Collections.Generic;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public static class MicrochunkSocketEdgeValidator
    {
        public const string MissingBandReason = "BAND_ID_NOT_FOUND";
        public const string MissingEdgeSignatureReason = "EDGE_SIGNATURE_ID_NOT_FOUND";
        public const string SolidEdgeReferenceReason = "SOLID_EDGE_REFERENCE_FORBIDDEN";
        public const string BandAxisMismatchReason = "BAND_AXIS_MISMATCH";
        public const string SignatureAxisMismatchReason = "SIGNATURE_AXIS_MISMATCH";
        public const string SignatureBandMismatchReason = "SIGNATURE_BAND_ID_MISMATCH";
        public const string TraversalMismatchReason = "SIGNATURE_TRAVERSAL_KIND_MISMATCH";
        public const string ToolRequirementMismatchReason = "SIGNATURE_TOOL_REQUIREMENT_MISMATCH";
        public const string MandatorySocketNotAllowedReason = "SIGNATURE_DISALLOWS_MANDATORY_SOCKET";
        public const string MinimumSafeTilesBelowBandMinimumReason = "SOCKET_MINIMUM_SAFE_TILES_BELOW_BAND_MINIMUM";
        public const string BandRangeReversedReason = "BAND_RANGE_REVERSED";
        public const string BandRangeOutOfBoundsReason = "BAND_RANGE_OUT_OF_BOUNDS";
        public const string BandMinimumClearanceNegativeReason = "BAND_MINIMUM_CLEARANCE_NEGATIVE";
        public const string ClearanceDepthOutOfRangeReason = "SOCKET_CLEARANCE_DEPTH_OUT_OF_RANGE";
        public const string MissingTileCellReason = "MISSING_TILE_CELL_FOR_SOCKET_CLEARANCE";
        public const string BlockingTileCellReason = "BLOCKING_TILE_CELL_IN_SOCKET_CLEARANCE";

        public static MicrochunkSocketEdgeValidationResult ValidateDefinition(
            MicrochunkDefinition definition,
            IReadOnlyDictionary<string, MicrochunkSocketBandDefinition> bandsById,
            IReadOnlyDictionary<string, MicrochunkEdgeSignatureDefinition> signaturesById)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (bandsById == null) throw new ArgumentNullException(nameof(bandsById));
            if (signaturesById == null) throw new ArgumentNullException(nameof(signaturesById));

            var cellsByCoordinate = new Dictionary<MicrochunkLocalCoord, MicrochunkTileCell>();
            foreach (var cell in definition.TileCells)
            {
                cellsByCoordinate.Add(cell.Coordinate, cell);
            }

            var violations = new List<MicrochunkSocketEdgeValidationViolation>();
            foreach (var socket in definition.Sockets)
            {
                var expectedAxis = GetExpectedAxis(socket.Side);

                bandsById.TryGetValue(socket.BandId, out var band);
                if (band == null)
                {
                    AddViolation(violations, definition.Id, socket, null, MissingBandReason);
                }
                else
                {
                    ValidateBand(definition.Id, socket, band, expectedAxis, violations);
                }

                signaturesById.TryGetValue(socket.EdgeSignatureId, out var signature);
                if (signature == null)
                {
                    AddViolation(violations, definition.Id, socket, null, MissingEdgeSignatureReason);
                }
                else
                {
                    ValidateSignature(definition.Id, socket, signature, expectedAxis, violations);
                }

                if (band != null && CanEvaluateClearance(socket, band, expectedAxis))
                {
                    ValidateClearance(definition.Id, socket, band, cellsByCoordinate, violations);
                }
            }

            return new MicrochunkSocketEdgeValidationResult(definition.Sockets.Count, violations);
        }

        private static void ValidateBand(
            MicrochunkId microchunkId,
            MicrochunkSocketDefinition socket,
            MicrochunkSocketBandDefinition band,
            MicrochunkEdgeAxis expectedAxis,
            ICollection<MicrochunkSocketEdgeValidationViolation> violations)
        {
            if (band.Axis == MicrochunkEdgeAxis.Solid)
            {
                AddViolation(violations, microchunkId, socket, null, SolidEdgeReferenceReason);
            }
            else if (band.Axis != expectedAxis)
            {
                AddViolation(violations, microchunkId, socket, null, BandAxisMismatchReason);
            }

            if (band.MinimumClearanceTiles < 0)
            {
                AddViolation(violations, microchunkId, socket, null, BandMinimumClearanceNegativeReason);
            }
            else if (socket.MinimumSafeTiles < band.MinimumClearanceTiles)
            {
                AddViolation(violations, microchunkId, socket, null, MinimumSafeTilesBelowBandMinimumReason);
            }

            if (band.MinimumLocalCoordinate > band.MaximumLocalCoordinate)
            {
                AddViolation(violations, microchunkId, socket, null, BandRangeReversedReason);
            }
            else if (!IsBandRangeInBounds(band))
            {
                AddViolation(violations, microchunkId, socket, null, BandRangeOutOfBoundsReason);
            }

            var maximumDepth = expectedAxis == MicrochunkEdgeAxis.HorizontalEdge
                ? MicrochunkConstants.WidthTiles
                : MicrochunkConstants.HeightTiles;
            if (socket.MinimumSafeTiles > maximumDepth)
            {
                AddViolation(violations, microchunkId, socket, null, ClearanceDepthOutOfRangeReason);
            }
        }

        private static void ValidateSignature(
            MicrochunkId microchunkId,
            MicrochunkSocketDefinition socket,
            MicrochunkEdgeSignatureDefinition signature,
            MicrochunkEdgeAxis expectedAxis,
            ICollection<MicrochunkSocketEdgeValidationViolation> violations)
        {
            if (string.Equals(socket.EdgeSignatureId, "EDGE_SOLID", StringComparison.Ordinal) ||
                signature.Axis == MicrochunkEdgeAxis.Solid)
            {
                AddViolation(violations, microchunkId, socket, null, SolidEdgeReferenceReason);
            }
            else if (signature.Axis != expectedAxis)
            {
                AddViolation(violations, microchunkId, socket, null, SignatureAxisMismatchReason);
            }

            if (!string.IsNullOrEmpty(signature.BandId) &&
                !string.Equals(signature.BandId, socket.BandId, StringComparison.Ordinal))
            {
                AddViolation(violations, microchunkId, socket, null, SignatureBandMismatchReason);
            }

            if (signature.TraversalKind != socket.TraversalKind)
            {
                AddViolation(violations, microchunkId, socket, null, TraversalMismatchReason);
            }

            if (signature.ToolRequirement != socket.ToolRequirement)
            {
                AddViolation(violations, microchunkId, socket, null, ToolRequirementMismatchReason);
            }

            if (socket.MandatoryAllowed && !signature.MandatoryAllowed)
            {
                AddViolation(violations, microchunkId, socket, null, MandatorySocketNotAllowedReason);
            }
        }

        private static bool CanEvaluateClearance(
            MicrochunkSocketDefinition socket,
            MicrochunkSocketBandDefinition band,
            MicrochunkEdgeAxis expectedAxis)
        {
            if (band.Axis != expectedAxis ||
                band.MinimumLocalCoordinate > band.MaximumLocalCoordinate ||
                !IsBandRangeInBounds(band))
            {
                return false;
            }

            var maximumDepth = expectedAxis == MicrochunkEdgeAxis.HorizontalEdge
                ? MicrochunkConstants.WidthTiles
                : MicrochunkConstants.HeightTiles;
            return socket.MinimumSafeTiles <= maximumDepth;
        }

        private static bool IsBandRangeInBounds(MicrochunkSocketBandDefinition band)
        {
            switch (band.Axis)
            {
                case MicrochunkEdgeAxis.HorizontalEdge:
                    return band.MinimumLocalCoordinate >= 0 &&
                           band.MaximumLocalCoordinate < MicrochunkConstants.HeightTiles;
                case MicrochunkEdgeAxis.VerticalEdge:
                    return band.MinimumLocalCoordinate >= 0 &&
                           band.MaximumLocalCoordinate < MicrochunkConstants.WidthTiles;
                default:
                    return false;
            }
        }

        private static void ValidateClearance(
            MicrochunkId microchunkId,
            MicrochunkSocketDefinition socket,
            MicrochunkSocketBandDefinition band,
            IReadOnlyDictionary<MicrochunkLocalCoord, MicrochunkTileCell> cellsByCoordinate,
            ICollection<MicrochunkSocketEdgeValidationViolation> violations)
        {
            foreach (var coordinate in EnumerateClearanceCoordinates(socket, band))
            {
                if (!cellsByCoordinate.TryGetValue(coordinate, out var cell))
                {
                    AddViolation(violations, microchunkId, socket, coordinate, MissingTileCellReason);
                }
                else if (IsBlocking(cell))
                {
                    AddViolation(violations, microchunkId, socket, coordinate, BlockingTileCellReason);
                }
            }
        }

        private static IEnumerable<MicrochunkLocalCoord> EnumerateClearanceCoordinates(
            MicrochunkSocketDefinition socket,
            MicrochunkSocketBandDefinition band)
        {
            switch (socket.Side)
            {
                case MicrochunkSide.Left:
                    for (var y = band.MinimumLocalCoordinate; y <= band.MaximumLocalCoordinate; y++)
                    {
                        for (var x = 0; x < socket.MinimumSafeTiles; x++)
                        {
                            yield return new MicrochunkLocalCoord(x, y);
                        }
                    }
                    break;
                case MicrochunkSide.Right:
                    for (var y = band.MinimumLocalCoordinate; y <= band.MaximumLocalCoordinate; y++)
                    {
                        for (var x = MicrochunkConstants.WidthTiles - socket.MinimumSafeTiles;
                             x < MicrochunkConstants.WidthTiles;
                             x++)
                        {
                            yield return new MicrochunkLocalCoord(x, y);
                        }
                    }
                    break;
                case MicrochunkSide.Down:
                    for (var y = 0; y < socket.MinimumSafeTiles; y++)
                    {
                        for (var x = band.MinimumLocalCoordinate; x <= band.MaximumLocalCoordinate; x++)
                        {
                            yield return new MicrochunkLocalCoord(x, y);
                        }
                    }
                    break;
                case MicrochunkSide.Up:
                    for (var y = MicrochunkConstants.HeightTiles - socket.MinimumSafeTiles;
                         y < MicrochunkConstants.HeightTiles;
                         y++)
                    {
                        for (var x = band.MinimumLocalCoordinate; x <= band.MaximumLocalCoordinate; x++)
                        {
                            yield return new MicrochunkLocalCoord(x, y);
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(socket.Side));
            }
        }

        private static bool IsBlocking(MicrochunkTileCell cell)
        {
            var occupancy = MicrochunkTileLayerOccupancy.FromCell(cell);
            return occupancy.IsOccupied(MicrochunkTileLayer.GroundSolid) ||
                   occupancy.IsOccupied(MicrochunkTileLayer.Breakable) ||
                   occupancy.IsOccupied(MicrochunkTileLayer.Hazard) ||
                   occupancy.IsOccupied(MicrochunkTileLayer.Liquid);
        }

        private static MicrochunkEdgeAxis GetExpectedAxis(MicrochunkSide side)
        {
            switch (side)
            {
                case MicrochunkSide.Left:
                case MicrochunkSide.Right:
                    return MicrochunkEdgeAxis.HorizontalEdge;
                case MicrochunkSide.Up:
                case MicrochunkSide.Down:
                    return MicrochunkEdgeAxis.VerticalEdge;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side));
            }
        }

        private static void AddViolation(
            ICollection<MicrochunkSocketEdgeValidationViolation> violations,
            MicrochunkId microchunkId,
            MicrochunkSocketDefinition socket,
            MicrochunkLocalCoord? coordinate,
            string reason)
        {
            violations.Add(new MicrochunkSocketEdgeValidationViolation(
                microchunkId,
                socket.SocketId,
                socket.Side,
                socket.BandId,
                socket.EdgeSignatureId,
                coordinate,
                reason));
        }
    }
}
