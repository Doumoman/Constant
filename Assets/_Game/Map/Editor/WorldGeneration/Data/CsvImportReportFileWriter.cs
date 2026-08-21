using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace StarNight.Map.Editor.WorldGeneration.Data
{
    public sealed class CsvImportReportWriteResult
    {
        public CsvImportReportWriteResult(bool success, string fullPath, string error)
        {
            Success = success;
            FullPath = fullPath ?? string.Empty;
            Error = error ?? string.Empty;
        }

        public bool Success { get; }
        public string FullPath { get; }
        public string Error { get; }
    }

    public sealed class CsvImportReportFileWriter
    {
        public const string ReportProjectRelativePath =
            "MapDesign/MCP/REPORTS/CsvImportReport.json";

        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public CsvImportReportWriteResult Write(byte[] serializerBytes)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var reportsDirectory = Path.Combine(projectRoot, "MapDesign", "MCP", "REPORTS");
            return WriteToDirectory(serializerBytes, reportsDirectory);
        }

        public CsvImportReportWriteResult WriteToDirectory(
            byte[] serializerBytes,
            string reportsDirectory)
        {
            var validationError = ValidateBytes(serializerBytes);
            if (validationError.Length != 0)
            {
                return new CsvImportReportWriteResult(false, string.Empty, validationError);
            }

            if (string.IsNullOrWhiteSpace(reportsDirectory))
            {
                return new CsvImportReportWriteResult(
                    false, string.Empty, "Report directory is missing.");
            }

            string destination = string.Empty;
            string temporary = string.Empty;
            try
            {
                var fullDirectory = Path.GetFullPath(reportsDirectory);
                Directory.CreateDirectory(fullDirectory);
                destination = Path.Combine(fullDirectory, "CsvImportReport.json");
                temporary = Path.Combine(
                    fullDirectory,
                    ".CsvImportReport." + Guid.NewGuid().ToString("N") + ".tmp");

                using (var stream = new FileStream(
                           temporary,
                           FileMode.CreateNew,
                           FileAccess.Write,
                           FileShare.None,
                           4096,
                           FileOptions.WriteThrough))
                {
                    stream.Write(serializerBytes, 0, serializerBytes.Length);
                    stream.Flush(true);
                }

                if (File.Exists(destination))
                {
                    File.Replace(temporary, destination, null);
                }
                else
                {
                    File.Move(temporary, destination);
                }

                return new CsvImportReportWriteResult(true, destination, string.Empty);
            }
            catch (Exception exception)
            {
                TryDeleteOwnTemporary(temporary);
                return new CsvImportReportWriteResult(false, destination, exception.Message);
            }
        }

        public static string ValidateBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return "Report bytes are empty.";
            }

            if (bytes.Length >= 3 &&
                bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf)
            {
                return "Report bytes must not contain a UTF-8 BOM.";
            }

            try
            {
                StrictUtf8.GetString(bytes);
            }
            catch (DecoderFallbackException exception)
            {
                return "Report bytes are not strict UTF-8: " + exception.Message;
            }

            if (bytes[bytes.Length - 1] != (byte)'\n')
            {
                return "Report bytes must end with LF.";
            }

            if (bytes.Length > 1 && bytes[bytes.Length - 2] == (byte)'\n')
            {
                return "Report bytes must end with exactly one LF.";
            }

            return string.Empty;
        }

        private static void TryDeleteOwnTemporary(string temporary)
        {
            if (string.IsNullOrEmpty(temporary)) return;
            try
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
            catch
            {
                // The original write failure is the actionable error.
            }
        }
    }
}
