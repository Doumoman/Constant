#if LEGACY_DISABLED
using System.IO;
using NUnit.Framework;
using StarNight.Core.Save;
using UnityEngine;

namespace StarNight.Core.Tests
{
    public sealed class SettingsRepositoryTests
    {
        private string temporaryDirectory;
        private string settingsPath;

        [SetUp]
        public void SetUp()
        {
            temporaryDirectory = Path.Combine(Path.GetTempPath(), "StarNight.SettingsTests", TestContext.CurrentContext.Test.ID);
            Directory.CreateDirectory(temporaryDirectory);
            settingsPath = Path.Combine(temporaryDirectory, SettingsRepository.FileName);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(temporaryDirectory, true);
            }
        }

        [Test]
        public void DefaultSettings_SerializeAndDeserializeWithRequiredDefaults()
        {
            SettingsData expected = SettingsData.CreateDefault();
            string json = JsonUtility.ToJson(expected);
            SettingsData actual = JsonUtility.FromJson<SettingsData>(json);

            Assert.That(actual.version, Is.EqualTo(1));
            Assert.That(actual.audio.masterVolume, Is.EqualTo(8));
            Assert.That(actual.display.frameLimit, Is.EqualTo(60));
            Assert.That(actual.gameplay.alwaysShowExitDirection, Is.True);
            Assert.That(actual.accessibility.visualBellAlert, Is.True);
        }

        [Test]
        public void Load_CorruptJsonMovesBakAndRestoresDefaults()
        {
            File.WriteAllText(settingsPath, "{ definitely-not-json }");
            var repository = new SettingsRepository(settingsPath);

            SettingsData loaded = repository.Load();

            Assert.That(repository.LastLoadRecoveredFromCorruption, Is.True);
            Assert.That(repository.LastBackupPath, Is.EqualTo(settingsPath + ".bak"));
            Assert.That(File.Exists(repository.LastBackupPath), Is.True);
            Assert.That(File.Exists(settingsPath), Is.True);
            Assert.That(loaded.audio.masterVolume, Is.EqualTo(8));
        }

        [Test]
        public void SaveAndReload_RestoresEveryCategoryAndBindingOverrides()
        {
            var repository = new SettingsRepository(settingsPath);
            SettingsData expected = SettingsData.CreateDefault();
            expected.audio.masterVolume = 3;
            expected.audio.muteInBackground = false;
            expected.display.screenMode = ScreenModeSetting.Windowed;
            expected.display.cameraShakePercent = 20;
            expected.display.roomTransitionSpeed = RoomTransitionSpeed.Fast;
            expected.gameplay.dialogueSpeed = DialogueSpeed.Fast;
            expected.gameplay.autoAdvanceDialogue = true;
            expected.accessibility.extendMaruTime = true;
            expected.accessibility.holdActionsAsToggle = true;
            expected.inputBindingOverridesJson = "{\"bindings\":\"override\"}";

            repository.Save(expected);
            SettingsData actual = new SettingsRepository(settingsPath).Load();

            Assert.That(actual.audio.masterVolume, Is.EqualTo(3));
            Assert.That(actual.audio.muteInBackground, Is.False);
            Assert.That(actual.display.screenMode, Is.EqualTo(ScreenModeSetting.Windowed));
            Assert.That(actual.display.cameraShakePercent, Is.EqualTo(20));
            Assert.That(actual.display.roomTransitionSpeed, Is.EqualTo(RoomTransitionSpeed.Fast));
            Assert.That(actual.gameplay.dialogueSpeed, Is.EqualTo(DialogueSpeed.Fast));
            Assert.That(actual.gameplay.autoAdvanceDialogue, Is.True);
            Assert.That(actual.accessibility.extendMaruTime, Is.True);
            Assert.That(actual.accessibility.holdActionsAsToggle, Is.True);
            Assert.That(actual.inputBindingOverridesJson, Is.EqualTo(expected.inputBindingOverridesJson));
        }
    }
}

#endif
