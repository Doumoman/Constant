using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class BiomePatchValidationViolation
    {
        internal BiomePatchValidationViolation(
            BiomePatchValidationRule rule,
            string biomeId,
            string patchId,
            int sectorIndex,
            string expected,
            string actual,
            string message)
        {
            if (!Enum.IsDefined(typeof(BiomePatchValidationRule), rule))
                throw new ArgumentOutOfRangeException(nameof(rule));
            Rule = rule;
            BiomeId = biomeId ?? throw new ArgumentNullException(nameof(biomeId));
            PatchId = patchId ?? throw new ArgumentNullException(nameof(patchId));
            SectorIndex = sectorIndex;
            Expected = expected ?? throw new ArgumentNullException(nameof(expected));
            Actual = actual ?? throw new ArgumentNullException(nameof(actual));
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public BiomePatchValidationRule Rule { get; }
        public string BiomeId { get; }
        public string PatchId { get; }
        public int SectorIndex { get; }
        public string Expected { get; }
        public string Actual { get; }
        public string Message { get; }
    }
}
