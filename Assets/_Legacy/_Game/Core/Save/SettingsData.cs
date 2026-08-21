#if LEGACY_DISABLED
using System;

namespace StarNight.Core.Save
{
    public enum ScreenModeSetting
    {
        FullScreen = 0,
        Windowed = 1,
        Borderless = 2,
    }

    public enum RoomTransitionSpeed
    {
        Slow = 0,
        Normal = 1,
        Fast = 2,
        Instant = 3,
    }

    public enum DialogueSpeed
    {
        Slow = 20,
        Normal = 35,
        Fast = 55,
    }

    [Serializable]
    public sealed class SettingsData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public AudioSettingsData audio = new();
        public DisplaySettingsData display = new();
        public GameplaySettingsData gameplay = new();
        public AccessibilitySettingsData accessibility = new();
        public string inputBindingOverridesJson = string.Empty;

        public static SettingsData CreateDefault()
        {
            return new SettingsData();
        }
    }

    [Serializable]
    public sealed class AudioSettingsData
    {
        public int masterVolume = 8;
        public int bgmVolume = 7;
        public int sfxVolume = 8;
        public int dialogueVolume = 6;
        public int uiVolume = 7;
        public bool muteInBackground = true;
    }

    [Serializable]
    public sealed class DisplaySettingsData
    {
        public ScreenModeSetting screenMode = ScreenModeSetting.FullScreen;
        public bool useRecommendedResolution = true;
        public int fallbackResolutionWidth = 1920;
        public int fallbackResolutionHeight = 1080;
        public bool verticalSync = true;
        public int frameLimit = 60;
        public int cameraShakePercent = 70;
        public int parallaxPercent = 100;
        public int flashPercent = 100;
        public RoomTransitionSpeed roomTransitionSpeed = RoomTransitionSpeed.Normal;
    }

    [Serializable]
    public sealed class GameplaySettingsData
    {
        public bool alwaysShowExitDirection = true;
        public bool showInteractionPrompt = true;
        public bool showTutorialHints = true;
        public DialogueSpeed dialogueSpeed = DialogueSpeed.Normal;
        public bool autoAdvanceDialogue;
        public bool vibration = true;
        public bool showTimerNumbers;
    }

    [Serializable]
    public sealed class AccessibilitySettingsData
    {
        public bool hazardOutline;
        public bool highContrastInteractions;
        public bool visualBellAlert = true;
        public bool reducedCameraShake;
        public bool reducedFlashing;
        public bool removeFallDamage;
        public bool extendMaruTime;
        public bool halfBossDamage;
        public bool holdActionsAsToggle;
        public bool travelerAssist;
    }
}

#endif
