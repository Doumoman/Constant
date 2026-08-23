using System;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public sealed class MicrochunkTransformOptions
    {
        private static readonly MicrochunkTransformOptions defaultOptions =
            new MicrochunkTransformOptions();

        public static MicrochunkTransformOptions Default => defaultOptions;

        public Func<string, MicrochunkTileLayer, MicrochunkTransform, string> TileCodeRemapper { get; }
        public Func<MicrochunkSide, MicrochunkSide, string, MicrochunkTransform, string> SocketBandRemapper { get; }
        public Func<MicrochunkId, MicrochunkTransform, MicrochunkId> IdProjector { get; }

        public MicrochunkTransformOptions(
            Func<string, MicrochunkTileLayer, MicrochunkTransform, string> tileCodeRemapper = null,
            Func<MicrochunkSide, MicrochunkSide, string, MicrochunkTransform, string> socketBandRemapper = null,
            Func<MicrochunkId, MicrochunkTransform, MicrochunkId> idProjector = null)
        {
            TileCodeRemapper = tileCodeRemapper;
            SocketBandRemapper = socketBandRemapper;
            IdProjector = idProjector;
        }

        internal string RemapTileCode(
            string originalCode,
            MicrochunkTileLayer layer,
            MicrochunkTransform transform)
        {
            var remapped = TileCodeRemapper == null
                ? originalCode
                : TileCodeRemapper(originalCode, layer, transform);
            return RequireToken(remapped, "Tile-code remappers must return a non-empty token.");
        }

        internal string RemapSocketBand(
            MicrochunkSide originalSide,
            MicrochunkSide transformedSide,
            string originalBandId,
            MicrochunkTransform transform)
        {
            var remapped = SocketBandRemapper == null
                ? originalBandId
                : SocketBandRemapper(originalSide, transformedSide, originalBandId, transform);
            return RequireToken(remapped, "Socket-band remappers must return a non-empty token.");
        }

        internal MicrochunkId ProjectId(MicrochunkId originalId, MicrochunkTransform transform)
        {
            var projected = IdProjector == null ? originalId : IdProjector(originalId, transform);
            if (!projected.IsValid)
            {
                throw new InvalidOperationException("Microchunk ID projectors must return a valid ID.");
            }

            return projected;
        }

        private static string RequireToken(string value, string message)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(message);
            }

            return value;
        }
    }
}
