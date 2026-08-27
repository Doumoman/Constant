using System;
using StarNight.Map.WorldGeneration.Boundaries;

namespace StarNight.Map.WorldGeneration.MicroPatterns
{
    public sealed class MicroPatternCatalogRowV2
    {
        public MicroPatternCatalogRowV2(
            string patternId,
            string selectionWeight,
            string biomeIds,
            string allowedTransforms,
            string protectedPolicy,
            string sourceFile = "",
            int recordNumber = 0)
        {
            PatternId = patternId ?? string.Empty;
            SelectionWeight = selectionWeight ?? string.Empty;
            BiomeIds = biomeIds ?? string.Empty;
            AllowedTransforms = allowedTransforms ?? string.Empty;
            ProtectedPolicy = protectedPolicy ?? string.Empty;
            SourceFile = sourceFile ?? string.Empty;
            RecordNumber = recordNumber;
        }

        public string PatternId { get; }
        public string SelectionWeight { get; }
        public string BiomeIds { get; }
        public string AllowedTransforms { get; }
        public string ProtectedPolicy { get; }
        public string SourceFile { get; }
        public int RecordNumber { get; }
    }

    public sealed class MicroPatternCellRowV2
    {
        public MicroPatternCellRowV2(
            string patternId,
            string localX,
            string localY,
            string operation,
            string layer,
            string payloadId,
            string sourceFile = "",
            int recordNumber = 0)
        {
            PatternId = patternId ?? string.Empty;
            LocalX = localX ?? string.Empty;
            LocalY = localY ?? string.Empty;
            Operation = operation ?? string.Empty;
            Layer = layer ?? string.Empty;
            PayloadId = payloadId ?? string.Empty;
            SourceFile = sourceFile ?? string.Empty;
            RecordNumber = recordNumber;
        }

        public string PatternId { get; }
        public string LocalX { get; }
        public string LocalY { get; }
        public string Operation { get; }
        public string Layer { get; }
        public string PayloadId { get; }
        public string SourceFile { get; }
        public int RecordNumber { get; }
    }

    public static class MicroPatternCellTokenCodec
    {
        public static bool TryParseLayer(string token, out MicroPatternLayer value)
        {
            switch (token)
            {
                case "GEOMETRY": value = MicroPatternLayer.Geometry; return true;
                case "SURFACE": value = MicroPatternLayer.Surface; return true;
                case "AFFORDANCE": value = MicroPatternLayer.Affordance; return true;
                case "MATERIAL": value = MicroPatternLayer.Material; return true;
                case "HAZARD": value = MicroPatternLayer.Hazard; return true;
                case "MARKER": value = MicroPatternLayer.Marker; return true;
                default: value = default; return false;
            }
        }

        public static bool TryParseOperation(string token, out MicroPatternOperation value)
        {
            switch (token)
            {
                case "NO_CHANGE": value = MicroPatternOperation.NoChange; return true;
                case "ADD_SOLID": value = MicroPatternOperation.AddSolid; return true;
                case "CARVE_AIR": value = MicroPatternOperation.CarveAir; return true;
                case "SURFACE": value = MicroPatternOperation.SetSurface; return true;
                case "AFFORDANCE": value = MicroPatternOperation.SetAffordance; return true;
                case "MATERIAL": value = MicroPatternOperation.SetMaterial; return true;
                case "HAZARD": value = MicroPatternOperation.SetHazard; return true;
                case "MARKER": value = MicroPatternOperation.SetMarker; return true;
                default: value = default; return false;
            }
        }

        public static bool TryParseTransform(string token, out MicroPatternTransform value)
        {
            switch (token)
            {
                case "R0": value = MicroPatternTransform.R0; return true;
                case "MIRROR_X": value = MicroPatternTransform.MirrorX; return true;
                case "MIRROR_Y": value = MicroPatternTransform.MirrorY; return true;
                case "R180": value = MicroPatternTransform.R180; return true;
                default: value = default; return false;
            }
        }

        public static bool TryParseProtectedPolicy(
            string token,
            out MicroPatternProtectedPolicy value)
        {
            switch (token)
            {
                case "FORCE_NO_CHANGE":
                    value = MicroPatternProtectedPolicy.ForceNoChange;
                    return true;
                case "REJECT_CANDIDATE":
                    value = MicroPatternProtectedPolicy.RejectCandidate;
                    return true;
                default:
                    value = default;
                    return false;
            }
        }

        public static bool TryParseBiome(string token, out MoonpalaceBiomeId value)
        {
            return MoonpalaceBiomeId.TryParse(token, out value);
        }

        public static string ToLayerToken(MicroPatternLayer value)
        {
            switch (value)
            {
                case MicroPatternLayer.Geometry: return "GEOMETRY";
                case MicroPatternLayer.Surface: return "SURFACE";
                case MicroPatternLayer.Affordance: return "AFFORDANCE";
                case MicroPatternLayer.Material: return "MATERIAL";
                case MicroPatternLayer.Hazard: return "HAZARD";
                case MicroPatternLayer.Marker: return "MARKER";
                default: throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        public static string ToOperationToken(MicroPatternOperation value)
        {
            switch (value)
            {
                case MicroPatternOperation.NoChange: return "NO_CHANGE";
                case MicroPatternOperation.AddSolid: return "ADD_SOLID";
                case MicroPatternOperation.CarveAir: return "CARVE_AIR";
                case MicroPatternOperation.SetSurface: return "SURFACE";
                case MicroPatternOperation.SetAffordance: return "AFFORDANCE";
                case MicroPatternOperation.SetMaterial: return "MATERIAL";
                case MicroPatternOperation.SetHazard: return "HAZARD";
                case MicroPatternOperation.SetMarker: return "MARKER";
                default: throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }
    }
}
