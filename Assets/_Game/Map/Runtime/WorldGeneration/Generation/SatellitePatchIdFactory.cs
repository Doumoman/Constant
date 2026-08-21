using System;
using System.Globalization;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SatellitePatchIdFactory
    {
        private const string Prefix = "PATCHINST_SAT_";

        public BiomePatchId Create(string biomeId, int satelliteOrdinal)
        {
            if (!TryCreate(biomeId, satelliteOrdinal, out var patchId))
                throw new ArgumentException("Biome ID and Satellite ordinal must be canonical.");
            return patchId;
        }

        public bool TryCreate(
            string biomeId,
            int satelliteOrdinal,
            out BiomePatchId patchId)
        {
            if (!ReservationValidation.IsCanonicalId(biomeId, false) ||
                satelliteOrdinal < 0 || satelliteOrdinal > 99)
            {
                patchId = default(BiomePatchId);
                return false;
            }

            return BiomePatchId.TryCreate(
                Prefix + biomeId + "_" +
                satelliteOrdinal.ToString("D2", CultureInfo.InvariantCulture),
                out patchId);
        }
    }
}
