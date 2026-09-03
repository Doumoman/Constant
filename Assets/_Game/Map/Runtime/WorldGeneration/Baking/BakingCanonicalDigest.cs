using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.Map.WorldGeneration.Baking
{
    public static class BakingCanonicalDigest
    {
        public static Encoding Utf8NoBomEncoding => new UTF8Encoding(false);

        public static string NormalizeLineEndingsToLf(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            return text.Replace("\r\n", "\n").Replace('\r', '\n');
        }

        public static string HashCanonicalText(string canonicalText)
        {
            if (canonicalText == null) throw new ArgumentNullException(nameof(canonicalText));
            var normalized = NormalizeLineEndingsToLf(canonicalText);
            using (var sha = SHA256.Create())
            {
                return string.Concat(sha.ComputeHash(Utf8NoBomEncoding.GetBytes(normalized))
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        public static string HashCanonicalLines(IEnumerable<string> canonicalLines)
        {
            if (canonicalLines == null) throw new ArgumentNullException(nameof(canonicalLines));
            var lines = canonicalLines.ToArray();
            if (lines.Any(value => value == null))
                throw new ArgumentException("Canonical lines cannot contain null values.", nameof(canonicalLines));
            return HashCanonicalText(string.Join("\n", lines));
        }

        public static bool IsLowerHexSha256(string value) => value != null && value.Length == 64 &&
            value.All(character => (character >= '0' && character <= '9') ||
                                   (character >= 'a' && character <= 'f'));

        public static string ComputeCanvas(SectorCanvasContract canvas)
        {
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));
            var material = new StringBuilder();
            Append(material, "id", canvas.Id.Value);
            Append(material, "dimensions", Number(canvas.Width) + "," + Number(canvas.Height));
            Append(material, "cells", ComputeResolvedCells(canvas.Cells));
            Append(material, "stamp", ComputeStamp(canvas.ValidationStamp));
            return Sha256(material.ToString());
        }

        public static string ComputeResolvedCells(IEnumerable<SectorCanvasCell> cells)
        {
            if (cells == null) throw new ArgumentNullException(nameof(cells));
            var material = new StringBuilder();
            foreach (var cell in cells.Where(value => value != null).OrderBy(value => value.CanonicalIndex))
                Append(material, "cell", CellSemantic(cell));
            return Sha256(material.ToString());
        }

        public static string ComputeSourceArtifactSet(IEnumerable<SectorCanvasCell> cells)
        {
            if (cells == null) throw new ArgumentNullException(nameof(cells));
            var records = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var cell in cells.Where(value => value != null && value.Provenance != null))
            {
                foreach (var source in cell.Provenance.Sources.Where(value => value != null))
                    records.Add(SourceSemantic(source));
                foreach (var key in cell.Provenance.PersistenceKeys)
                    records.Add("PERSISTENCE/" + key.Value);
            }
            return Sha256(string.Join("\n", records));
        }

        public static string ComputeStamp(SectorCanvasValidationStamp stamp)
        {
            if (stamp == null) return string.Empty;
            return Sha256(string.Join("\n", new[]
            {
                Number((int)stamp.State),
                stamp.PassCatalogDigest,
                stamp.LayerCatalogDigest,
                stamp.SourceArtifactSetDigest,
                stamp.ResolvedCellsDigest,
                stamp.ValidationRulesetVersion,
            }));
        }

        public static bool AreCellsEquivalent(SectorCanvasCell left, SectorCanvasCell right)
            => left != null && right != null && string.Equals(CellSemantic(left), CellSemantic(right), StringComparison.Ordinal);

        internal static string CellSemantic(SectorCanvasCell cell)
        {
            var material = new StringBuilder();
            material.Append(Number(cell.Coordinate.X)).Append(',').Append(Number(cell.Coordinate.Y));
            foreach (SectorCanvasLayerKind layer in Enum.GetValues(typeof(SectorCanvasLayerKind)))
            {
                var value = cell.Layers.Get(layer);
                material.Append('/').Append(Number((int)layer)).Append(':')
                    .Append(value.IsExplicitEmpty ? "EMPTY" : value.StableId);
            }
            foreach (var source in cell.Provenance.Sources)
                material.Append("/SOURCE:").Append(SourceSemantic(source));
            foreach (var key in cell.Provenance.PersistenceKeys)
                material.Append("/KEY:").Append(key.Value);
            return material.ToString();
        }

        internal static string SourceSemantic(CanvasSourceRef source)
        {
            return string.Join("/", new[]
            {
                Number((int)source.Kind),
                source.StableId,
                Number(source.PassOrder),
                source.IsProtected ? "1" : "0",
                string.Join(",", source.OwnedLayers.Select(value => Number((int)value))),
            });
        }

        internal static string Sha256(string material) => HashCanonicalText(material ?? string.Empty);

        internal static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static void Append(StringBuilder material, string name, string value)
            => material.Append(name).Append('=').Append(value ?? string.Empty).Append('\n');
    }
}
