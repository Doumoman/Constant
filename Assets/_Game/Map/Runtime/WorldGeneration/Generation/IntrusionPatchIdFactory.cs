using System;
using System.Globalization;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class IntrusionPatchIdFactory
    {
        public BiomePatchId Create(string biomeId, int intrusionOrdinal)
        {
            if (!TryCreate(biomeId, intrusionOrdinal, out var patchId))
                throw new ArgumentException("Intrusion patch identity is invalid.");
            return patchId;
        }

        public bool TryCreate(string biomeId, int intrusionOrdinal, out BiomePatchId patchId)
        {
            patchId = default(BiomePatchId);
            if (!ReservationValidation.IsCanonicalId(biomeId, false) ||
                intrusionOrdinal < 0 || intrusionOrdinal > 99)
                return false;

            return BiomePatchId.TryCreate(
                "PATCHINST_INTR_" + biomeId + "_" +
                intrusionOrdinal.ToString("D2", CultureInfo.InvariantCulture),
                out patchId);
        }
    }
}
