#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using StarNight.Core.State;
using UnityEngine;

namespace StarNight.Core.Save
{
    public sealed class RunRecordRepository
    {
        public const string FileName = "records.json";
        public const string FolkloreFlagPrefix = "FOLKLORE.";

        private static readonly UTF8Encoding Utf8WithoutBom = new(false);
        private readonly string recordPath;

        public RunRecordRepository(string path = null)
        {
            recordPath = string.IsNullOrWhiteSpace(path)
                ? Path.Combine(Application.persistentDataPath, FileName)
                : path;
        }

        public string RecordPath => recordPath;
        public RunRecordData Current { get; private set; }
        public bool LastLoadRecoveredFromCorruption { get; private set; }
        public bool PersistenceEnabled { get; set; } = true;

        public RunRecordData Load()
        {
            LastLoadRecoveredFromCorruption = false;
            if (!File.Exists(recordPath))
            {
                Current = RunRecordData.CreateDefault();
                Save(Current);
                return Current;
            }

            try
            {
                string json = File.ReadAllText(recordPath, Utf8WithoutBom);
                if (string.IsNullOrWhiteSpace(json))
                {
                    throw new InvalidDataException("Run record file is empty.");
                }

                RunRecordData loaded = JsonUtility.FromJson<RunRecordData>(json);
                if (loaded == null)
                {
                    throw new InvalidDataException("Run record JSON did not produce data.");
                }

                Current = Normalize(loaded);
                return Current;
            }
            catch (Exception exception) when (exception is ArgumentException
                or IOException
                or UnauthorizedAccessException
                or InvalidDataException)
            {
                LastLoadRecoveredFromCorruption = true;
                BackupDamagedFile();
                Current = RunRecordData.CreateDefault();
                Save(Current);
                return Current;
            }
        }

        public void Record(RunResultSnapshot result, RunState run)
        {
            if (result == null || run == null)
            {
                return;
            }

            Current ??= RunRecordData.CreateDefault();
            Current = Normalize(Current);
            if (result.IsCleared)
            {
                Current.completedRunCount++;
                AddUnique(Current.viewedEndingIds, result.endingId);
                if (result.runTime > 0f &&
                    (Current.bestClearedRunTime <= 0f || result.runTime < Current.bestClearedRunTime))
                {
                    Current.bestClearedRunTime = result.runTime;
                }
            }
            else
            {
                Current.failedRunCount++;
            }

            if (StageRank(result.reachedStageId) > StageRank(Current.highestReachedStage))
            {
                Current.highestReachedStage = result.reachedStageId ?? string.Empty;
            }

            RecordFlagSuffixes(run.flags, RunResultSnapshot.MemoryTravelerFlagPrefix, Current.metMemoryTravelerIds);
            RecordFlagSuffixes(run.flags, FolkloreFlagPrefix, Current.discoveredFolkloreIds);
            Save(Current);
        }

        public void Save(RunRecordData data)
        {
            Current = Normalize(data ?? RunRecordData.CreateDefault());
            if (!PersistenceEnabled)
            {
                return;
            }

            string directory = Path.GetDirectoryName(recordPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string temporaryPath = recordPath + ".tmp";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(Current, true), Utf8WithoutBom);
            File.Copy(temporaryPath, recordPath, true);
            File.Delete(temporaryPath);
        }

        private static RunRecordData Normalize(RunRecordData data)
        {
            data.version = RunRecordData.CurrentVersion;
            data.viewedEndingIds = NormalizeList(data.viewedEndingIds);
            data.metMemoryTravelerIds = NormalizeList(data.metMemoryTravelerIds);
            data.discoveredFolkloreIds = NormalizeList(data.discoveredFolkloreIds);
            data.highestReachedStage ??= string.Empty;
            data.bestClearedRunTime = Math.Max(0f, data.bestClearedRunTime);
            data.completedRunCount = Math.Max(0, data.completedRunCount);
            data.failedRunCount = Math.Max(0, data.failedRunCount);
            return data;
        }

        private static List<string> NormalizeList(List<string> source)
        {
            var result = new List<string>();
            if (source == null)
            {
                return result;
            }

            foreach (string value in source)
            {
                AddUnique(result, value);
            }
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        private static void RecordFlagSuffixes(HashSet<string> flags, string prefix, List<string> target)
        {
            if (flags == null)
            {
                return;
            }

            foreach (string flag in flags)
            {
                if (!string.IsNullOrEmpty(flag) && flag.StartsWith(prefix, StringComparison.Ordinal))
                {
                    AddUnique(target, flag.Substring(prefix.Length));
                }
            }
            target.Sort(StringComparer.Ordinal);
        }

        private static void AddUnique(List<string> target, string value)
        {
            if (target == null || string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            string normalized = value.Trim();
            if (!target.Contains(normalized))
            {
                target.Add(normalized);
            }
        }

        private static int StageRank(string stageId)
        {
            if (string.IsNullOrWhiteSpace(stageId))
            {
                return -1;
            }

            string[] parts = stageId.Split('-');
            if (parts.Length == 1 && int.TryParse(parts[0], out int single))
            {
                return single * 100;
            }
            if (parts.Length >= 2 && int.TryParse(parts[0], out int major) && int.TryParse(parts[1], out int minor))
            {
                return major * 100 + minor;
            }
            return 0;
        }

        private void BackupDamagedFile()
        {
            if (!File.Exists(recordPath))
            {
                return;
            }

            string backupPath = recordPath + ".bak";
            int suffix = 1;
            while (File.Exists(backupPath))
            {
                backupPath = recordPath + ".bak." + suffix++;
            }
            File.Move(recordPath, backupPath);
        }
    }
}

#endif
