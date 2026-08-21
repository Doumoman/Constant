using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SeedReplayBundle
    {
        private readonly byte[] seedManifestBytes;
        private readonly byte[] generatedWorldSectorsBytes;
        private readonly IReadOnlyList<string> fileNames;

        public SeedReplayBundle(
            SeedManifest manifest,
            string relativeDirectory,
            byte[] seedManifestBytes,
            byte[] generatedWorldSectorsBytes)
            : this(manifest, relativeDirectory, seedManifestBytes, generatedWorldSectorsBytes,
                new[] { SeedManifestCsvSerializer.FileName, GeneratedWorldDataCsvSerializer.FileName })
        {
        }

        public SeedReplayBundle(
            SeedManifest manifest,
            string relativeDirectory,
            byte[] seedManifestBytes,
            byte[] generatedWorldSectorsBytes,
            IEnumerable<string> fileNames)
        {
            Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            if (relativeDirectory == null) throw new ArgumentNullException(nameof(relativeDirectory));
            if (seedManifestBytes == null) throw new ArgumentNullException(nameof(seedManifestBytes));
            if (generatedWorldSectorsBytes == null) throw new ArgumentNullException(nameof(generatedWorldSectorsBytes));
            if (fileNames == null) throw new ArgumentNullException(nameof(fileNames));

            var names = new List<string>(fileNames);
            if (names.Count != 2 ||
                !string.Equals(names[0], SeedManifestCsvSerializer.FileName, StringComparison.Ordinal) ||
                !string.Equals(names[1], GeneratedWorldDataCsvSerializer.FileName, StringComparison.Ordinal))
                throw new ArgumentException("Replay bundle file set or order is not exact.", nameof(fileNames));

            var expectedDirectory = GetRelativeDirectory(manifest.WorldProfileId, manifest.Seed);
            if (!string.Equals(relativeDirectory, expectedDirectory, StringComparison.Ordinal))
                throw new ArgumentException("Replay bundle relative directory does not match manifest identity.", nameof(relativeDirectory));

            SeedManifest parsed;
            try
            {
                parsed = SeedManifestCsvSerializer.Deserialize(seedManifestBytes);
            }
            catch (ArgumentException exception)
            {
                throw new ArgumentException("Replay bundle contains an invalid seed manifest.", nameof(seedManifestBytes), exception);
            }
            if (!manifest.HasSameFields(parsed))
                throw new ArgumentException("Seed manifest bytes do not match the manifest object.", nameof(seedManifestBytes));

            ValidateGeneratedWorldSectors(generatedWorldSectorsBytes, manifest.Seed);

            RelativeDirectory = relativeDirectory;
            this.seedManifestBytes = (byte[])seedManifestBytes.Clone();
            this.generatedWorldSectorsBytes = (byte[])generatedWorldSectorsBytes.Clone();
            this.fileNames = new ReadOnlyCollection<string>(new List<string>(names));
        }

        public SeedManifest Manifest { get; }
        public string RelativeDirectory { get; }
        public byte[] SeedManifestBytes => (byte[])seedManifestBytes.Clone();
        public byte[] GeneratedWorldSectorsBytes => (byte[])generatedWorldSectorsBytes.Clone();
        public IReadOnlyList<string> FileNames => fileNames;

        public static string GetRelativeDirectory(string worldProfileId, ulong seed)
        {
            ValidateWorldProfileSegment(worldProfileId);
            return "GeneratedWorlds/" + worldProfileId + "/" +
                   seed.ToString("D16", CultureInfo.InvariantCulture);
        }

        internal static void ValidateWorldProfileSegment(string worldProfileId)
        {
            if (worldProfileId == null) throw new ArgumentNullException(nameof(worldProfileId));
            if (worldProfileId.Length == 0 || worldProfileId == "." || worldProfileId == "..")
                throw new ArgumentException("World profile identifier is not a safe path segment.", nameof(worldProfileId));
            const string invalid = "<>:\"/\\|?*";
            foreach (var character in worldProfileId)
            {
                if (char.IsControl(character) || invalid.IndexOf(character) >= 0)
                    throw new ArgumentException("World profile identifier is not a safe path segment.", nameof(worldProfileId));
            }
            if (worldProfileId[worldProfileId.Length - 1] == '.' ||
                worldProfileId[worldProfileId.Length - 1] == ' ' ||
                IsReservedWindowsSegment(worldProfileId))
                throw new ArgumentException("World profile identifier is not a safe path segment.", nameof(worldProfileId));
        }

        private static bool IsReservedWindowsSegment(string value)
        {
            var stemEnd = value.IndexOf('.');
            var stem = (stemEnd < 0 ? value : value.Substring(0, stemEnd)).ToUpperInvariant();
            if (stem == "CON" || stem == "PRN" || stem == "AUX" || stem == "NUL") return true;
            if (stem.Length == 4 && (stem.StartsWith("COM", StringComparison.Ordinal) ||
                                     stem.StartsWith("LPT", StringComparison.Ordinal)))
                return stem[3] >= '1' && stem[3] <= '9';
            return false;
        }

        internal static void ValidateGeneratedWorldSectors(byte[] bytes, ulong expectedSeed)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            IReadOnlyList<IReadOnlyList<string>> rows;
            try
            {
                rows = SeedManifestCsvSerializer.ParseStrictRows(bytes, nameof(bytes));
            }
            catch (ArgumentException exception)
            {
                throw new ArgumentException("Generated sector CSV has an invalid byte envelope.", nameof(bytes), exception);
            }
            if (rows.Count != 170 || rows[0].Count != 13 ||
                !string.Equals(string.Join(",", rows[0]), GeneratedWorldDataCsvSerializer.Header, StringComparison.Ordinal))
                throw new ArgumentException("Generated sector CSV has an invalid header or record count.", nameof(bytes));

            var seedText = expectedSeed.ToString(CultureInfo.InvariantCulture);
            for (var index = 0; index < 169; index++)
            {
                var row = rows[index + 1];
                if (row.Count != 13 ||
                    !string.Equals(row[0], seedText, StringComparison.Ordinal) ||
                    !string.Equals(row[1], (index % 13).ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal) ||
                    !string.Equals(row[2], (index / 13).ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal) ||
                    !string.Equals(row[3], "UNASSIGNED", StringComparison.Ordinal) ||
                    row.Skip(4).Take(7).Any(value => value.Length != 0) ||
                    !string.Equals(row[11], "-1", StringComparison.Ordinal) ||
                    !string.Equals(row[12], "0", StringComparison.Ordinal))
                    throw new ArgumentException("Generated sector CSV is not the exact P00 grid checkpoint.", nameof(bytes));
            }

            if (!bytes.SequenceEqual(BuildCanonicalGridBytes(expectedSeed)))
                throw new ArgumentException("Generated sector CSV bytes are not canonical serializer output.", nameof(bytes));
        }

        private static byte[] BuildCanonicalGridBytes(ulong seed)
        {
            var seedText = seed.ToString(CultureInfo.InvariantCulture);
            var builder = new StringBuilder(GeneratedWorldDataCsvSerializer.Header.Length + 169 * 48);
            builder.Append(GeneratedWorldDataCsvSerializer.Header).Append("\r\n");
            for (var index = 0; index < 169; index++)
            {
                var fields = new[]
                {
                    seedText,
                    (index % 13).ToString(CultureInfo.InvariantCulture),
                    (index / 13).ToString(CultureInfo.InvariantCulture),
                    "UNASSIGNED", "", "", "", "", "", "", "", "-1", "0"
                };
                builder.Append(string.Join(",", fields)).Append("\r\n");
            }
            var content = new UTF8Encoding(false, true).GetBytes(builder.ToString());
            var result = new byte[content.Length + 3];
            result[0] = 0xEF;
            result[1] = 0xBB;
            result[2] = 0xBF;
            Buffer.BlockCopy(content, 0, result, 3, content.Length);
            return result;
        }
    }
}
