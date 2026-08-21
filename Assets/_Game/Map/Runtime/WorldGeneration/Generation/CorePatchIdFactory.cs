using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class CorePatchIdFactory
    {
        private const string Prefix = "PATCHINST_CORE_";

        public BiomePatchId CreateCorePatchId(SiteReservationId sourceReservationId)
        {
            if (!TryCreateCorePatchId(sourceReservationId, out var patchId))
                throw new ArgumentException("Source reservation ID must be valid.", nameof(sourceReservationId));
            return patchId;
        }

        public bool TryCreateCorePatchId(
            SiteReservationId sourceReservationId,
            out BiomePatchId patchId)
        {
            if (!sourceReservationId.IsValid)
            {
                patchId = default(BiomePatchId);
                return false;
            }

            return BiomePatchId.TryCreate(Prefix + sourceReservationId.Value, out patchId);
        }
    }
}
