#if LEGACY_DISABLED
using System;
using System.Collections;
using StarNight.Core.Save;
using StarNight.Core.State;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarNight.Core.Flow
{
    public enum GameApplicationState
    {
        Boot,
        Title,
        NewRunLoading,
        Playing,
        Paused,
        StageTransition,
        RunResult,
    }

    [DisallowMultipleComponent]
    public sealed class GameFlowController : MonoBehaviour
    {
        public const string RunShellSceneName = "02_RunShell";
        public const string FirstStageSceneName = "10_Prologue_0_1";

        private RunManager runManager;
        private SceneTransitionService sceneTransition;
        private RunRecordRepository runRecords;
        private Coroutine activeTransition;
        private GameApplicationState stateBeforePause = GameApplicationState.Playing;
        private float timeScaleBeforePause = 1f;
        private bool audioWasPausedBeforePause;
        private float timeScaleBeforeRunResult = 1f;

        public event Action<GameApplicationState> StateChanged;

        public GameApplicationState State { get; private set; } = GameApplicationState.Boot;
        public bool IsTransitioning => activeTransition != null || (sceneTransition?.IsTransitioning ?? false);
        public bool IsPaused => State == GameApplicationState.Paused;
        public RunResultSnapshot LastRunResult { get; private set; }

        public void Initialize(
            RunManager manager,
            SceneTransitionService transitionService,
            RunRecordRepository recordRepository)
        {
            if (runManager != null)
            {
                return;
            }

            runManager = manager ?? throw new ArgumentNullException(nameof(manager));
            sceneTransition = transitionService ?? throw new ArgumentNullException(nameof(transitionService));
            runRecords = recordRepository ?? throw new ArgumentNullException(nameof(recordRepository));
            SceneManager.sceneLoaded += HandleSceneLoaded;

            if (SceneManager.GetActiveScene().name == GameBootstrap.TitleSceneName)
            {
                SetState(GameApplicationState.Title);
            }
        }

        public bool StartNewRun()
        {
            if (runManager == null || IsTransitioning)
            {
                return false;
            }

            ExitRunResultForSceneChange();
            activeTransition = StartCoroutine(StartNewRunRoutine());
            return true;
        }

        public bool BeginStandaloneSession()
        {
            if (runManager == null || IsTransitioning)
            {
                return false;
            }
            if (State == GameApplicationState.Playing)
            {
                Time.timeScale = 1f;
                AudioListener.pause = false;
                return true;
            }
            if (State != GameApplicationState.Boot && State != GameApplicationState.Title)
            {
                return false;
            }

            ExitRunResultForSceneChange();
            if (!runManager.HasActiveRun)
            {
                runManager.StartNewRun();
            }
            Time.timeScale = 1f;
            AudioListener.pause = false;
            LastRunResult = null;
            SetState(GameApplicationState.Playing);
            return true;
        }

        public bool RestartRun()
        {
            if (runManager == null || IsTransitioning)
            {
                return false;
            }

            ExitPauseForSceneChange();
            ExitRunResultForSceneChange();
            runManager.AbandonRun();
            activeTransition = StartCoroutine(StartNewRunRoutine());
            return true;
        }

        public bool ReturnToTitle()
        {
            if (runManager == null || IsTransitioning)
            {
                return false;
            }

            ExitPauseForSceneChange();
            ExitRunResultForSceneChange();
            activeTransition = StartCoroutine(ReturnToTitleRoutine());
            return true;
        }

        public bool TryPause()
        {
            if (State != GameApplicationState.Playing && State != GameApplicationState.StageTransition)
            {
                return false;
            }

            stateBeforePause = State;
            timeScaleBeforePause = Time.timeScale;
            audioWasPausedBeforePause = AudioListener.pause;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            SetState(GameApplicationState.Paused);
            return true;
        }

        public bool TryResume()
        {
            if (State != GameApplicationState.Paused)
            {
                return false;
            }

            RestorePauseSideEffects();
            SetState(stateBeforePause == GameApplicationState.StageTransition
                ? GameApplicationState.StageTransition
                : GameApplicationState.Playing);
            return true;
        }

        public bool TryBeginStageTransition()
        {
            if (State != GameApplicationState.Playing || IsTransitioning)
            {
                return false;
            }

            SetState(GameApplicationState.StageTransition);
            return true;
        }

        public bool EnterRunResult()
        {
            if (State == GameApplicationState.Title || State == GameApplicationState.Boot ||
                State == GameApplicationState.NewRunLoading || State == GameApplicationState.RunResult)
            {
                return false;
            }

            if (runManager?.Current == null || runManager.Current.phase == RunPhase.Running)
            {
                return false;
            }

            ExitPauseForSceneChange();
            runManager.TickRun(0f);
            LastRunResult = RunResultSnapshot.Capture(runManager.Current);
            runRecords?.Record(LastRunResult, runManager.Current);
            timeScaleBeforeRunResult = Time.timeScale;
            Time.timeScale = 0f;
            SetState(GameApplicationState.RunResult);
            return true;
        }

        public bool CompleteRun(RunPhase completionPhase, string endingId)
        {
            return runManager != null &&
                   runManager.CompleteRun(completionPhase, endingId) &&
                   EnterRunResult();
        }

        private void Update()
        {
            if (runManager == null || Time.timeScale <= 0f)
            {
                return;
            }

            if (State == GameApplicationState.Playing || State == GameApplicationState.StageTransition)
            {
                runManager.TickRun(Time.unscaledDeltaTime);
            }
        }

        public void CompleteStageTransition(bool succeeded)
        {
            if (State != GameApplicationState.StageTransition)
            {
                return;
            }

            SetState(GameApplicationState.Playing);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (State == GameApplicationState.Paused)
            {
                RestorePauseSideEffects();
            }
            else if (State == GameApplicationState.RunResult)
            {
                RestoreRunResultSideEffects();
            }
        }

        private IEnumerator StartNewRunRoutine()
        {
            SetState(GameApplicationState.NewRunLoading);

            yield return sceneTransition.LoadSingle(RunShellSceneName);
            if (!sceneTransition.LastOperationSucceeded)
            {
                SetState(GameApplicationState.Title);
                activeTransition = null;
                yield break;
            }

            runManager.StartNewRun();
            LastRunResult = null;

            yield return sceneTransition.LoadAdditive(FirstStageSceneName);
            if (!sceneTransition.LastOperationSucceeded)
            {
                runManager.AbandonRun();
                SetState(GameApplicationState.Title);
                activeTransition = null;
                yield break;
            }

            SetState(GameApplicationState.Playing);
            activeTransition = null;
        }

        private IEnumerator ReturnToTitleRoutine()
        {
            runManager.AbandonRun();
            yield return sceneTransition.LoadSingle(GameBootstrap.TitleSceneName);
            SetState(sceneTransition.LastOperationSucceeded
                ? GameApplicationState.Title
                : GameApplicationState.RunResult);
            activeTransition = null;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == GameBootstrap.TitleSceneName && !IsTransitioning)
            {
                SetState(GameApplicationState.Title);
            }
        }

        private void SetState(GameApplicationState next)
        {
            if (State == next)
            {
                return;
            }

            State = next;
            StateChanged?.Invoke(next);
        }

        private void ExitPauseForSceneChange()
        {
            if (State != GameApplicationState.Paused)
            {
                return;
            }

            RestorePauseSideEffects();
            SetState(stateBeforePause);
        }

        private void RestorePauseSideEffects()
        {
            Time.timeScale = Mathf.Approximately(timeScaleBeforePause, 0f) ? 1f : timeScaleBeforePause;
            AudioListener.pause = audioWasPausedBeforePause;
        }

        private void ExitRunResultForSceneChange()
        {
            if (State == GameApplicationState.RunResult)
            {
                RestoreRunResultSideEffects();
            }
        }

        private void RestoreRunResultSideEffects()
        {
            Time.timeScale = Mathf.Approximately(timeScaleBeforeRunResult, 0f) ? 1f : timeScaleBeforeRunResult;
        }
    }
}

#endif
