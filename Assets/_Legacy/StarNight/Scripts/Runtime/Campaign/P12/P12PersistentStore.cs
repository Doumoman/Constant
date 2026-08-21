#if LEGACY_DISABLED
using System;
using System.IO;
using UnityEngine;

namespace StarNight.Campaign.P12
{
    public static class P12PersistentStore
    {
        public const string ChallengeRecordFileName =
            "StarNight_P12_Challenge.json";
        public const string AccessibilityFileName =
            "StarNight_P12_Accessibility.json";

        private static string storageRootOverride;

        public static string StorageRoot =>
            string.IsNullOrWhiteSpace(storageRootOverride)
                ? Application.persistentDataPath
                : storageRootOverride;
        public static string ChallengeRecordPath =>
            Path.Combine(StorageRoot, ChallengeRecordFileName);
        public static string AccessibilityPath =>
            Path.Combine(StorageRoot, AccessibilityFileName);

        // GDD 2.4: only challenge records and settings options are
        // persisted. No gameplay power fields exist in this API.
        public static bool StoresOnlyRecordsAndOptions => true;

        public static void SetStorageRootForTests(string rootPath)
        {
            storageRootOverride =
                string.IsNullOrWhiteSpace(rootPath)
                    ? null
                    : rootPath.Trim();
        }

        public static bool SaveChallengeRecord(
            P12ChallengeRecordData data)
        {
            return WriteJson(
                ChallengeRecordPath,
                data ?? P12ChallengeRecordData.CreateEmpty());
        }

        public static P12ChallengeRecordData LoadChallengeRecord()
        {
            P12ChallengeRecordData loaded =
                ReadJson<P12ChallengeRecordData>(ChallengeRecordPath);
            return loaded ?? P12ChallengeRecordData.CreateEmpty();
        }

        public static bool DeleteChallengeRecord()
        {
            return DeleteFile(ChallengeRecordPath);
        }

        public static bool SaveAccessibility(P12AccessibilityData data)
        {
            return WriteJson(
                AccessibilityPath,
                data ?? P12AccessibilityData.CreateDefault());
        }

        public static P12AccessibilityData LoadAccessibility()
        {
            P12AccessibilityData loaded =
                ReadJson<P12AccessibilityData>(AccessibilityPath);
            return loaded ?? P12AccessibilityData.CreateDefault();
        }

        public static bool DeleteAccessibility()
        {
            return DeleteFile(AccessibilityPath);
        }

        private static bool WriteJson<T>(string path, T data)
            where T : class
        {
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(path, JsonUtility.ToJson(data));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static T ReadJson<T>(string path)
            where T : class
        {
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }

                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }

                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool DeleteFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return false;
                }

                File.Delete(path);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

#endif
