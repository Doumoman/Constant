#if LEGACY_DISABLED
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace StarNight.Core.Save
{
    public sealed class SettingsRepository
    {
        public const string FileName = "settings.json";

        private static readonly UTF8Encoding Utf8WithoutBom = new(false);
        private readonly string settingsPath;

        public SettingsRepository(string settingsPath = null)
        {
            this.settingsPath = string.IsNullOrWhiteSpace(settingsPath)
                ? Path.Combine(Application.persistentDataPath, FileName)
                : settingsPath;
        }

        public string SettingsPath => settingsPath;
        public SettingsData Current { get; private set; }
        public bool LastLoadRecoveredFromCorruption { get; private set; }
        public string LastBackupPath { get; private set; }

        public SettingsData Load()
        {
            LastLoadRecoveredFromCorruption = false;
            LastBackupPath = string.Empty;

            if (!File.Exists(settingsPath))
            {
                Current = SettingsData.CreateDefault();
                Save(Current);
                return Current;
            }

            try
            {
                string json = File.ReadAllText(settingsPath, Utf8WithoutBom);
                if (string.IsNullOrWhiteSpace(json))
                {
                    throw new InvalidDataException("Settings file is empty.");
                }

                SettingsData loaded = JsonUtility.FromJson<SettingsData>(json);
                if (loaded == null)
                {
                    throw new InvalidDataException("Settings JSON did not produce data.");
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
                LastBackupPath = BackupDamagedFile();
                Current = SettingsData.CreateDefault();
                Save(Current);
                return Current;
            }
        }

        public void Save(SettingsData settings)
        {
            Current = Normalize(settings ?? SettingsData.CreateDefault());

            string directory = Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string temporaryPath = settingsPath + ".tmp";
            string json = JsonUtility.ToJson(Current, true);
            File.WriteAllText(temporaryPath, json, Utf8WithoutBom);
            File.Copy(temporaryPath, settingsPath, true);
            File.Delete(temporaryPath);
        }

        private string BackupDamagedFile()
        {
            if (!File.Exists(settingsPath))
            {
                return string.Empty;
            }

            string backupPath = settingsPath + ".bak";
            int suffix = 1;
            while (File.Exists(backupPath))
            {
                backupPath = settingsPath + ".bak." + suffix;
                suffix++;
            }

            File.Move(settingsPath, backupPath);
            return backupPath;
        }

        private static SettingsData Normalize(SettingsData settings)
        {
            settings.version = SettingsData.CurrentVersion;
            settings.audio ??= new AudioSettingsData();
            settings.display ??= new DisplaySettingsData();
            settings.gameplay ??= new GameplaySettingsData();
            settings.accessibility ??= new AccessibilitySettingsData();
            settings.inputBindingOverridesJson ??= string.Empty;

            settings.audio.masterVolume = Mathf.Clamp(settings.audio.masterVolume, 0, 10);
            settings.audio.bgmVolume = Mathf.Clamp(settings.audio.bgmVolume, 0, 10);
            settings.audio.sfxVolume = Mathf.Clamp(settings.audio.sfxVolume, 0, 10);
            settings.audio.dialogueVolume = Mathf.Clamp(settings.audio.dialogueVolume, 0, 10);
            settings.audio.uiVolume = Mathf.Clamp(settings.audio.uiVolume, 0, 10);

            settings.display.fallbackResolutionWidth = Math.Max(1, settings.display.fallbackResolutionWidth);
            settings.display.fallbackResolutionHeight = Math.Max(1, settings.display.fallbackResolutionHeight);
            settings.display.frameLimit = settings.display.frameLimit <= 0 ? 60 : settings.display.frameLimit;
            settings.display.cameraShakePercent = Mathf.Clamp(settings.display.cameraShakePercent, 0, 100);
            settings.display.parallaxPercent = Mathf.Clamp(settings.display.parallaxPercent, 0, 100);
            settings.display.flashPercent = Mathf.Clamp(settings.display.flashPercent, 0, 100);
            if (!Enum.IsDefined(typeof(ScreenModeSetting), settings.display.screenMode))
            {
                settings.display.screenMode = ScreenModeSetting.FullScreen;
            }
            if (!Enum.IsDefined(typeof(RoomTransitionSpeed), settings.display.roomTransitionSpeed))
            {
                settings.display.roomTransitionSpeed = RoomTransitionSpeed.Normal;
            }
            if (!Enum.IsDefined(typeof(DialogueSpeed), settings.gameplay.dialogueSpeed))
            {
                settings.gameplay.dialogueSpeed = DialogueSpeed.Normal;
            }

            return settings;
        }
    }
}

#endif
