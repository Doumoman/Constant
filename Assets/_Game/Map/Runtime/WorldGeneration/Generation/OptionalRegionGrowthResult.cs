using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalRegionGrowthResult
    {
        public OptionalRegionGrowthResult(
            OptionalRegionSnapshot snapshot,
            OptionalRegionGrowthDiagnostics diagnostics,
            string sourceAttachmentDigest,
            string sourceMandatoryGraphDigest,
            OptionalRegionGrowthSettings settings)
        {
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            if (!IsCanonicalIdentity(sourceAttachmentDigest))
                throw new ArgumentException("Attachment digest must be a canonical non-empty identity.", nameof(sourceAttachmentDigest));
            if (!IsCanonicalIdentity(sourceMandatoryGraphDigest))
                throw new ArgumentException("Mandatory graph digest must be a canonical non-empty identity.", nameof(sourceMandatoryGraphDigest));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (!string.Equals(snapshot.SourceMandatoryGraphDigest, sourceMandatoryGraphDigest, StringComparison.Ordinal))
                throw new ArgumentException("Snapshot graph identity must match the result.", nameof(snapshot));
            if (snapshot.Regions.Count != diagnostics.AcceptedRegionCount || snapshot.Cells.Count != diagnostics.AcceptedCellCount)
                throw new ArgumentException("Snapshot counts must match diagnostics.", nameof(snapshot));

            SourceAttachmentDigest = sourceAttachmentDigest;
            SourceMandatoryGraphDigest = sourceMandatoryGraphDigest;
            RngDrawCount = 0;
            CanonicalDigest = ComputeDigest(snapshot, diagnostics, sourceAttachmentDigest, sourceMandatoryGraphDigest, settings);
        }

        public OptionalRegionSnapshot Snapshot { get; }
        public OptionalRegionGrowthDiagnostics Diagnostics { get; }
        public string SourceAttachmentDigest { get; }
        public string SourceMandatoryGraphDigest { get; }
        public string CanonicalDigest { get; }
        public int RngDrawCount { get; }

        private static bool IsCanonicalIdentity(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && string.Equals(value, value.Trim(), StringComparison.Ordinal);
        }

        private static string ComputeDigest(
            OptionalRegionSnapshot snapshot,
            OptionalRegionGrowthDiagnostics diagnostics,
            string attachmentDigest,
            string graphDigest,
            OptionalRegionGrowthSettings settings)
        {
            var text = new StringBuilder();
            text.Append("S|").Append(attachmentDigest).Append('|').Append(graphDigest).Append('|')
                .Append(settings.MaxRegions.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(settings.MaxCellsPerRegion.ToString(CultureInfo.InvariantCulture));
            foreach (var depth in settings.TargetDepthPattern)
                text.Append('|').Append(depth.Value.ToString(CultureInfo.InvariantCulture));
            text.Append('\n');
            foreach (var region in snapshot.Regions)
            {
                var attachment = region.Attachment;
                text.Append("R|").Append(region.RegionId.Value).Append('|')
                    .Append(attachment.AttachmentOrder.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(attachment.MandatoryRouteSectorIndex.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(attachment.MandatoryRouteNodeId.Value).Append('|')
                    .Append(attachment.EntrySectorIndex.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(attachment.EntrySideFromMandatoryDx.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(attachment.EntrySideFromMandatoryDy.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(region.MaxDepth.Value.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(OptionalRegionTokenCodec.ToToken(region.AccessRule)).Append('|')
                    .Append(OptionalRegionTokenCodec.ToToken(region.RewardTier)).Append('|')
                    .Append(OptionalRegionTokenCodec.ToToken(region.ReturnPolicy)).Append('\n');
                foreach (var cell in region.Cells)
                    text.Append("C|").Append(region.RegionId.Value).Append('|')
                        .Append(cell.SectorIndex.ToString(CultureInfo.InvariantCulture)).Append('|')
                        .Append(cell.Depth.Value.ToString(CultureInfo.InvariantCulture)).Append('|')
                        .Append(cell.IsAttachmentCell ? '1' : '0').Append('|')
                        .Append(cell.RequiresReturnConnection ? '1' : '0').Append('\n');
            }
            text.Append("D|").Append(Invariant(diagnostics.SourceCandidateCount)).Append('|')
                .Append(Invariant(diagnostics.AttemptedCandidates)).Append('|').Append(Invariant(diagnostics.AcceptedRegionCount)).Append('|')
                .Append(Invariant(diagnostics.RejectedCandidateCount)).Append('|').Append(Invariant(diagnostics.RegionLimitSkipped)).Append('|')
                .Append(Invariant(diagnostics.AcceptedCellCount)).Append('|').Append(Invariant(diagnostics.RawCellProbes)).Append('|')
                .Append(Invariant(diagnostics.OutOfBoundsCellRejected)).Append('|').Append(Invariant(diagnostics.MandatoryCellRejected)).Append('|')
                .Append(Invariant(diagnostics.AdditionalMandatoryBridgeRejected)).Append('|').Append(Invariant(diagnostics.SiteReservationCellRejected)).Append('|')
                .Append(Invariant(diagnostics.BiomeReservedCellRejected)).Append('|').Append(Invariant(diagnostics.ClaimedCellRejected)).Append('|')
                .Append(Invariant(diagnostics.DuplicateFrontierRejected)).Append('|').Append(Invariant(diagnostics.HorizontalThroughCellRejected)).Append('|')
                .Append(Invariant(diagnostics.NoTargetDepthPathRejected)).Append('|').Append(Invariant(diagnostics.Depth1RegionCount)).Append('|')
                .Append(Invariant(diagnostics.Depth2RegionCount)).Append('|').Append(Invariant(diagnostics.Depth3RegionCount)).Append('|')
                .Append(Invariant(diagnostics.Depth4RegionCount)).Append('\n');
            foreach (var code in diagnostics.RejectionCodes) text.Append("X|").Append(code).Append('\n');

            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()));
                var result = new StringBuilder(64);
                foreach (var value in hash) result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }

        private static string Invariant(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
