using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SeedReplayPublisher
    {
        public SeedReplayBundle Publish(string outputRoot, SeedReplayBundle bundle)
        {
            if (bundle == null) throw new ArgumentNullException(nameof(bundle));
            var root = ValidateOutputRoot(outputRoot);
            var validated = new SeedReplayBundle(
                bundle.Manifest,
                bundle.RelativeDirectory,
                bundle.SeedManifestBytes,
                bundle.GeneratedWorldSectorsBytes,
                bundle.FileNames);
            var destination = ResolveDirectory(root, validated.RelativeDirectory);
            var staging = destination + ".staging";
            var backup = destination + ".backup";
            if (Directory.Exists(staging) || File.Exists(staging) ||
                Directory.Exists(backup) || File.Exists(backup))
                throw new IOException("Replay publish has stale staging or backup state.");

            var parent = Directory.GetParent(destination);
            if (parent == null) throw new IOException("Replay destination has no parent directory.");
            Directory.CreateDirectory(parent.FullName);
            RejectReparsePath(root, parent.FullName);
            Directory.CreateDirectory(staging);
            WriteExact(Path.Combine(staging, SeedManifestCsvSerializer.FileName), validated.SeedManifestBytes);
            WriteExact(Path.Combine(staging, GeneratedWorldDataCsvSerializer.FileName), validated.GeneratedWorldSectorsBytes);
            LoadDirectory(staging, validated.RelativeDirectory, validated.Manifest.WorldProfileId, validated.Manifest.Seed);

            var movedOriginal = false;
            try
            {
                if (Directory.Exists(destination))
                {
                    RejectReparsePath(root, destination);
                    Directory.Move(destination, backup);
                    movedOriginal = true;
                }
                else if (File.Exists(destination))
                {
                    throw new IOException("Replay destination is not a directory.");
                }

                Directory.Move(staging, destination);
                var published = LoadDirectory(
                    destination,
                    validated.RelativeDirectory,
                    validated.Manifest.WorldProfileId,
                    validated.Manifest.Seed);
                if (movedOriginal)
                    Directory.Delete(backup, true);
                if (Directory.Exists(staging) || Directory.Exists(backup) ||
                    File.Exists(staging) || File.Exists(backup))
                    throw new IOException("Replay publish left staging or backup residue.");
                return published;
            }
            catch (Exception exception)
            {
                if (movedOriginal && Directory.Exists(backup))
                {
                    try
                    {
                        if (Directory.Exists(destination) && !Directory.Exists(staging) && !File.Exists(staging))
                            Directory.Move(destination, staging);
                        if (!Directory.Exists(destination) && !File.Exists(destination))
                            Directory.Move(backup, destination);
                    }
                    catch
                    {
                    }
                }
                ExceptionDispatchInfo.Capture(exception).Throw();
                throw;
            }
        }

        public SeedReplayBundle Load(string outputRoot, string worldProfileId, ulong seed)
        {
            var root = ValidateOutputRoot(outputRoot);
            var relativeDirectory = SeedReplayBundle.GetRelativeDirectory(worldProfileId, seed);
            var directory = ResolveDirectory(root, relativeDirectory);
            RejectReparsePath(root, directory);
            return LoadDirectory(directory, relativeDirectory, worldProfileId, seed);
        }

        private static SeedReplayBundle LoadDirectory(
            string directory,
            string relativeDirectory,
            string expectedWorldProfileId,
            ulong expectedSeed)
        {
            if (!Directory.Exists(directory) || File.Exists(directory))
                throw new IOException("Replay directory is missing or not a directory.");
            var directoryInfo = new DirectoryInfo(directory);
            if ((directoryInfo.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Replay directory cannot be a reparse point.");

            var entries = directoryInfo.GetFileSystemInfos();
            if (entries.Length != 2 || entries.Any(entry =>
                    (entry.Attributes & FileAttributes.ReparsePoint) != 0 ||
                    !(entry is FileInfo)))
                throw new IOException("Replay directory must contain exactly two regular files.");
            var names = entries.Select(entry => entry.Name).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var expectedNames = new[]
            {
                GeneratedWorldDataCsvSerializer.FileName,
                SeedManifestCsvSerializer.FileName
            };
            if (!names.SequenceEqual(expectedNames, StringComparer.Ordinal))
                throw new IOException("Replay directory file set or filename casing is not exact.");

            var manifestBytes = File.ReadAllBytes(Path.Combine(directory, SeedManifestCsvSerializer.FileName));
            var sectorsBytes = File.ReadAllBytes(Path.Combine(directory, GeneratedWorldDataCsvSerializer.FileName));
            SeedManifest manifest;
            try
            {
                manifest = SeedManifestCsvSerializer.Deserialize(manifestBytes);
            }
            catch (ArgumentException exception)
            {
                throw new IOException("Replay seed manifest is invalid.", exception);
            }
            if (!string.Equals(manifest.WorldProfileId, expectedWorldProfileId, StringComparison.Ordinal) ||
                manifest.Seed != expectedSeed)
                throw new IOException("Replay manifest identity does not match its directory.");
            try
            {
                return new SeedReplayBundle(manifest, relativeDirectory, manifestBytes, sectorsBytes);
            }
            catch (ArgumentException exception)
            {
                throw new IOException("Replay bundle bytes are invalid.", exception);
            }
        }

        private static void WriteExact(string path, byte[] bytes)
        {
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
        }

        private static string ValidateOutputRoot(string outputRoot)
        {
            if (outputRoot == null) throw new ArgumentNullException(nameof(outputRoot));
            if (outputRoot.Length == 0 || !Path.IsPathRooted(outputRoot))
                throw new ArgumentException("Output root must be a non-empty absolute path.", nameof(outputRoot));
            var full = Path.GetFullPath(outputRoot);
            var supplied = outputRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var normalized = full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!string.Equals(supplied, normalized, PathComparison))
                throw new ArgumentException("Output root must already be a full normalized path.", nameof(outputRoot));
            return full;
        }

        private static string ResolveDirectory(string root, string relativeDirectory)
        {
            var relativeSystemPath = relativeDirectory.Replace('/', Path.DirectorySeparatorChar);
            var target = Path.GetFullPath(Path.Combine(root, relativeSystemPath));
            var rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                             Path.DirectorySeparatorChar;
            if (!target.StartsWith(rootPrefix, PathComparison))
                throw new ArgumentException("Replay target escapes the output root.", nameof(relativeDirectory));
            return target;
        }

        private static void RejectReparsePath(string root, string destination)
        {
            var rootFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var destinationFull = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var current = new DirectoryInfo(destinationFull);
            while (current != null)
            {
                if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Replay path cannot traverse a reparse point.");
                if (string.Equals(current.FullName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    rootFull, PathComparison))
                    return;
                current = current.Parent;
            }
            throw new IOException("Replay path is outside the output root.");
        }

        private static StringComparison PathComparison =>
            Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    }
}
