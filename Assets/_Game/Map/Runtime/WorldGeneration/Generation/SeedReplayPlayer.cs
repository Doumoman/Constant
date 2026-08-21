using System;
using System.Linq;
using StarNight.Map.WorldGeneration.Data;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SeedReplayPlayer
    {
        private readonly WorldGenerationRoot root;

        public SeedReplayPlayer(WorldGenerationRoot root)
        {
            this.root = root ?? throw new ArgumentNullException(nameof(root));
        }

        public SeedReplayVerificationResult Verify(
            SeedReplayBundle bundle,
            ContentVersionHash currentContentVersionHash,
            string currentGeneratorBuildId)
        {
            if (currentContentVersionHash == null)
                throw new ArgumentNullException(nameof(currentContentVersionHash));
            if (currentGeneratorBuildId == null)
                throw new ArgumentNullException(nameof(currentGeneratorBuildId));
            if (currentGeneratorBuildId.Length == 0)
                throw new ArgumentException("Generator build identifier must be non-empty.", nameof(currentGeneratorBuildId));

            SeedReplayBundle validatedBundle;
            try
            {
                if (bundle == null)
                    return Fail(SeedReplayVerificationResult.InvalidBundleCode, "Replay bundle is null.");
                validatedBundle = new SeedReplayBundle(
                    bundle.Manifest,
                    bundle.RelativeDirectory,
                    bundle.SeedManifestBytes,
                    bundle.GeneratedWorldSectorsBytes,
                    bundle.FileNames);
            }
            catch (ArgumentException)
            {
                return Fail(SeedReplayVerificationResult.InvalidBundleCode, "Replay bundle envelope is invalid.");
            }

            SeedManifest manifest;
            try
            {
                manifest = SeedManifestCsvSerializer.Deserialize(validatedBundle.SeedManifestBytes);
            }
            catch (ArgumentException)
            {
                return Fail(SeedReplayVerificationResult.InvalidManifestCode, "Seed manifest is invalid.");
            }
            if (!manifest.IsGridCheckpoint() || !manifest.HasSameFields(validatedBundle.Manifest))
                return Fail(SeedReplayVerificationResult.InvalidManifestCode, "Seed manifest is not a P00 grid checkpoint.");
            if (!string.Equals(manifest.ContentVersionHash, currentContentVersionHash.Hex, StringComparison.Ordinal))
                return Fail(SeedReplayVerificationResult.ContentHashMismatchCode, "Current content hash does not match the seed manifest.");
            if (!string.Equals(manifest.GeneratorBuildId, currentGeneratorBuildId, StringComparison.Ordinal))
                return Fail(SeedReplayVerificationResult.GeneratorBuildMismatchCode, "Current generator build does not match the seed manifest.");

            var replay = root.ExecuteThroughRecorded(
                manifest.GenerationProfileId,
                manifest.Seed,
                GridInitializationPass.PassId);
            if (!SeedReplayRecorder.TryGetGridCheckpoint(replay, out var record, out var grid) ||
                !string.Equals(record.GenerationProfileId, manifest.GenerationProfileId, StringComparison.Ordinal) ||
                !string.Equals(record.WorldProfileId, manifest.WorldProfileId, StringComparison.Ordinal) ||
                record.WorldSeed != manifest.Seed)
                return Fail(SeedReplayVerificationResult.ReplayExecutionFailedCode, "Grid replay execution did not reproduce the recorded checkpoint identity.");

            var replayBytes = GeneratedWorldDataCsvSerializer.Serialize(grid.WorldData);
            if (!replayBytes.SequenceEqual(validatedBundle.GeneratedWorldSectorsBytes))
                return Fail(SeedReplayVerificationResult.ArtifactMismatchCode, "Grid replay artifact bytes do not match the recorded artifact.");
            return SeedReplayVerificationResult.Success();
        }

        private static SeedReplayVerificationResult Fail(string code, string message)
        {
            return SeedReplayVerificationResult.Failure(code, message);
        }
    }
}
