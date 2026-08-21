#if LEGACY_DISABLED
using System.Collections;
using StarNight.Core.Save;
using StarNight.Core.State;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarNight.Core.Flow
{
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class GameBootstrap : MonoBehaviour
    {
        public const string ServiceRootName = "[CoreServices]";
        public const string BootSceneName = "00_Boot";
        public const string TitleSceneName = "01_Title";

        private static GameBootstrap instance;

        [SerializeField] private bool autoLoadTitle = true;
        [SerializeField] private string titleSceneName = TitleSceneName;

        private bool titleLoadInProgress;

        public static GameBootstrap Instance => instance;
        public static bool IsReady => instance != null && instance.Services != null;
        public ServiceRegistry Services { get; private set; }
        public SettingsData Settings { get; private set; }
        public event System.Action<SettingsData> SettingsChanged;

        public float BgmVolume => ChannelVolume(Settings?.audio?.bgmVolume ?? 7);
        public float SfxVolume => ChannelVolume(Settings?.audio?.sfxVolume ?? 8);
        public float DialogueVolume => ChannelVolume(Settings?.audio?.dialogueVolume ?? 6);
        public float UiVolume => ChannelVolume(Settings?.audio?.uiVolume ?? 7);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureBootstrapExists()
        {
            if (Object.FindAnyObjectByType<GameBootstrap>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            var serviceRoot = new GameObject(ServiceRootName);
            serviceRoot.AddComponent<GameBootstrap>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            gameObject.name = ServiceRootName;
            DontDestroyOnLoad(gameObject);

            Services = new ServiceRegistry();
            var settingsRepository = new SettingsRepository();
            Services.Register(settingsRepository);
            Settings = settingsRepository.Load();
            ApplyRuntimeSettings(Settings);

            var runManager = new RunManager();
            var sceneTransition = new SceneTransitionService();
            var runRecords = new RunRecordRepository();
            runRecords.Load();
            GameFlowController gameFlow = gameObject.GetComponent<GameFlowController>();
            if (gameFlow == null)
            {
                gameFlow = gameObject.AddComponent<GameFlowController>();
            }

            gameFlow.Initialize(runManager, sceneTransition, runRecords);
            Services.Register(runManager);
            Services.Register(sceneTransition);
            Services.Register(runRecords);
            Services.Register(gameFlow);

            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            if (SceneManager.GetActiveScene().name == BootSceneName)
            {
                TryLoadTitle();
            }
        }

        private void OnDestroy()
        {
            if (instance != this)
            {
                return;
            }

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Services?.Dispose();
            Services = null;
            Settings = null;
            instance = null;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            ApplyMasterVolume(Settings, hasFocus);
        }

        public void PreviewSettings(SettingsData settings)
        {
            ApplyRuntimeSettings(settings);
        }

        public void SaveSettings(SettingsData settings)
        {
            if (Services == null || !Services.TryGet(out SettingsRepository repository))
            {
                return;
            }

            repository.Save(settings);
            Settings = repository.Current;
            ApplyRuntimeSettings(Settings);
            SettingsChanged?.Invoke(Settings);
        }

        public void RestoreSavedSettingsPreview()
        {
            ApplyRuntimeSettings(Settings);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == BootSceneName)
            {
                TryLoadTitle();
            }
        }

        private void TryLoadTitle()
        {
            if (!autoLoadTitle || titleLoadInProgress || string.IsNullOrWhiteSpace(titleSceneName))
            {
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(titleSceneName))
            {
                return;
            }

            StartCoroutine(LoadTitleScene());
        }

        private IEnumerator LoadTitleScene()
        {
            titleLoadInProgress = true;
            AsyncOperation operation = SceneManager.LoadSceneAsync(titleSceneName, LoadSceneMode.Single);
            if (operation != null)
            {
                yield return operation;
            }

            titleLoadInProgress = false;
        }

        private static void ApplyRuntimeSettings(SettingsData settings)
        {
            if (settings?.display == null)
            {
                return;
            }

            QualitySettings.vSyncCount = settings.display.verticalSync ? 1 : 0;
            Application.targetFrameRate = settings.display.frameLimit;
            ApplyMasterVolume(settings, Application.isFocused);

#if !UNITY_EDITOR
            FullScreenMode mode = settings.display.screenMode switch
            {
                ScreenModeSetting.Windowed => FullScreenMode.Windowed,
                ScreenModeSetting.Borderless => FullScreenMode.FullScreenWindow,
                _ => FullScreenMode.ExclusiveFullScreen,
            };
            int width = settings.display.useRecommendedResolution
                ? Display.main.systemWidth
                : settings.display.fallbackResolutionWidth;
            int height = settings.display.useRecommendedResolution
                ? Display.main.systemHeight
                : settings.display.fallbackResolutionHeight;
            Screen.SetResolution(width, height, mode);
#endif
        }

        private static void ApplyMasterVolume(SettingsData settings, bool hasFocus)
        {
            if (settings?.audio == null)
            {
                return;
            }

            AudioListener.volume = settings.audio.muteInBackground && !hasFocus
                ? 0f
                : ChannelVolume(settings.audio.masterVolume);
        }

        private static float ChannelVolume(int value)
        {
            return Mathf.Clamp(value, 0, 10) / 10f;
        }
    }
}

#endif
