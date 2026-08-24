using System;
using System.IO;
using UnityEngine;

namespace StarNight.MapAuthoring.Microchunks
{
    public sealed class MicrochunkCsvImportSource
    {
        public const string CatalogFileName = "microchunk_catalog.csv";
        public const string TileCellsFileName = "microchunk_tile_cells.csv";
        public const string SocketsFileName = "microchunk_sockets.csv";
        public const string SocketBandsFileName = "socket_band_definitions.csv";
        public const string ObjectSlotsFileName = "microchunk_object_slots.csv";
        public const string VariantsFileName = "microchunk_variants.csv";
        public const string TileCodesFileName = "tile_code_dictionary.csv";
        public const string ObjectSlotPoolsFileName = "object_slot_pools.csv";
        public const string EdgeSignaturesFileName = "edge_signatures.csv";

        public const string AuthoringRoot =
            "Assets/_Game/Map/Data/WorldGeneration/Authoring";

        private readonly byte[] catalogBytes;
        private readonly byte[] tileCellBytes;
        private readonly byte[] socketBytes;
        private readonly byte[] socketBandBytes;
        private readonly byte[] objectSlotBytes;
        private readonly byte[] variantBytes;
        private readonly byte[] tileCodeBytes;
        private readonly byte[] objectSlotPoolBytes;
        private readonly byte[] edgeSignatureBytes;

        public byte[] CatalogBytes => Clone(catalogBytes);
        public byte[] TileCellBytes => Clone(tileCellBytes);
        public byte[] SocketBytes => Clone(socketBytes);
        public byte[] SocketBandBytes => Clone(socketBandBytes);
        public byte[] ObjectSlotBytes => Clone(objectSlotBytes);
        public byte[] VariantBytes => Clone(variantBytes);
        public byte[] TileCodeBytes => Clone(tileCodeBytes);
        public byte[] ObjectSlotPoolBytes => Clone(objectSlotPoolBytes);
        public byte[] EdgeSignatureBytes => Clone(edgeSignatureBytes);

        public MicrochunkCsvImportSource(
            byte[] catalogBytes,
            byte[] tileCellBytes,
            byte[] socketBytes = null,
            byte[] socketBandBytes = null,
            byte[] objectSlotBytes = null,
            byte[] variantBytes = null,
            byte[] tileCodeBytes = null,
            byte[] objectSlotPoolBytes = null,
            byte[] edgeSignatureBytes = null)
        {
            this.catalogBytes = RequireSnapshot(catalogBytes, nameof(catalogBytes));
            this.tileCellBytes = RequireSnapshot(tileCellBytes, nameof(tileCellBytes));
            this.socketBytes = CloneOrEmpty(socketBytes);
            this.socketBandBytes = CloneOrEmpty(socketBandBytes);
            this.objectSlotBytes = CloneOrEmpty(objectSlotBytes);
            this.variantBytes = CloneOrEmpty(variantBytes);
            this.tileCodeBytes = CloneOrEmpty(tileCodeBytes);
            this.objectSlotPoolBytes = CloneOrEmpty(objectSlotPoolBytes);
            this.edgeSignatureBytes = CloneOrEmpty(edgeSignatureBytes);
        }

        public static MicrochunkCsvImportSource FromProjectAuthoringCsv()
        {
            return new MicrochunkCsvImportSource(
                ReadRequired("MicroChunk", CatalogFileName),
                ReadRequired("MicroChunk", TileCellsFileName),
                ReadOptional("MicroChunk", SocketsFileName),
                ReadOptional("Route", SocketBandsFileName),
                ReadOptional("MicroChunk", ObjectSlotsFileName),
                ReadOptional("MicroChunk", VariantsFileName),
                ReadOptional("MicroChunk", TileCodesFileName),
                ReadOptional("MicroChunk", ObjectSlotPoolsFileName),
                ReadOptional("Route", EdgeSignaturesFileName));
        }

        private static byte[] ReadRequired(string directory, string fileName)
        {
            var path = ResolveProjectPath(directory, fileName);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Required Authoring CSV was not found.", path);
            }
            return File.ReadAllBytes(path);
        }

        private static byte[] ReadOptional(string directory, string fileName)
        {
            var path = ResolveProjectPath(directory, fileName);
            return File.Exists(path) ? File.ReadAllBytes(path) : Array.Empty<byte>();
        }

        private static string ResolveProjectPath(string directory, string fileName)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var normalizedRoot = projectRoot.TrimEnd(
                                     Path.DirectorySeparatorChar,
                                     Path.AltDirectorySeparatorChar) +
                                 Path.DirectorySeparatorChar;
            var relative = Path.Combine(
                AuthoringRoot.Replace('/', Path.DirectorySeparatorChar),
                directory,
                fileName);
            var fullPath = Path.GetFullPath(Path.Combine(projectRoot, relative));
            if (!fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Authoring CSV path escaped the Unity project root.");
            }
            return fullPath;
        }

        private static byte[] RequireSnapshot(byte[] bytes, string parameterName)
        {
            if (bytes == null) throw new ArgumentNullException(parameterName);
            return Clone(bytes);
        }

        private static byte[] CloneOrEmpty(byte[] bytes)
        {
            return bytes == null ? Array.Empty<byte>() : Clone(bytes);
        }

        private static byte[] Clone(byte[] bytes)
        {
            return (byte[])bytes.Clone();
        }
    }
}
