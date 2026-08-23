using System;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public sealed class MicrochunkTileLayerRuleViolation
    {
        public MicrochunkLocalCoord Coordinate { get; }
        public MicrochunkTileLayer FirstLayer { get; }
        public MicrochunkTileLayer SecondLayer { get; }
        public string FirstCode { get; }
        public string SecondCode { get; }
        public string Reason { get; }

        public MicrochunkTileLayerRuleViolation(
            MicrochunkLocalCoord coordinate,
            MicrochunkTileLayer firstLayer,
            MicrochunkTileLayer secondLayer,
            string firstCode,
            string secondCode,
            string reason)
        {
            ValidateLayer(firstLayer, nameof(firstLayer));
            ValidateLayer(secondLayer, nameof(secondLayer));
            if (firstLayer == secondLayer) throw new ArgumentException("A violation requires two distinct layers.");
            if (string.IsNullOrWhiteSpace(firstCode)) throw new ArgumentException("First tile code is required.", nameof(firstCode));
            if (string.IsNullOrWhiteSpace(secondCode)) throw new ArgumentException("Second tile code is required.", nameof(secondCode));
            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A deterministic reason is required.", nameof(reason));

            Coordinate = coordinate;
            if ((int)firstLayer < (int)secondLayer)
            {
                FirstLayer = firstLayer;
                SecondLayer = secondLayer;
                FirstCode = firstCode;
                SecondCode = secondCode;
            }
            else
            {
                FirstLayer = secondLayer;
                SecondLayer = firstLayer;
                FirstCode = secondCode;
                SecondCode = firstCode;
            }

            Reason = reason;
        }

        private static void ValidateLayer(MicrochunkTileLayer layer, string parameterName)
        {
            if (!Enum.IsDefined(typeof(MicrochunkTileLayer), layer))
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }
}
