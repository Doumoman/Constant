#if LEGACY_DISABLED
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.MapAuthoring.Editor
{
    public static class BakeHashUtility
    {
        public static string ComputeAssetFileHash(string assetPath)
        {
            var absolutePath = AssetPathUtility.ToAbsolutePath(assetPath);
            if (!File.Exists(absolutePath))
            {
                return string.Empty;
            }

            using var stream = File.OpenRead(absolutePath);
            using var sha256 = SHA256.Create();
            return ToHex(sha256.ComputeHash(stream));
        }

        public static string ComputeStringHash(string value)
        {
            using var sha256 = SHA256.Create();
            return ToHex(sha256.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty)));
        }

        private static string ToHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            for (var index = 0; index < bytes.Length; index++)
            {
                builder.Append(bytes[index].ToString("x2"));
            }

            return builder.ToString();
        }
    }
}

#endif
