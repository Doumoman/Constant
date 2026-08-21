#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Stages.P5
{
    [DefaultExecutionOrder(-150)]
    [DisallowMultipleComponent]
    public sealed class P5ExitGuidance2D : MonoBehaviour
    {
        public const float RequiredGuidanceSeconds = 1f;

        [SerializeField] private Camera stageCamera;
        [SerializeField] private MonoBehaviour cameraFollowDriver;
        [SerializeField] private MonoBehaviour movementDriver;
        [SerializeField] private Transform player;
        [SerializeField] private Transform exitTarget;
        [SerializeField] private Transform edgeDirectionIndicator;
        [SerializeField] private P5StageCoreLoop2D coreLoop;
        [SerializeField] private P5SliceTelemetry2D telemetry;
        [SerializeField, Min(0.1f)] private float duration =
            RequiredGuidanceSeconds;
        [SerializeField, Min(0f)] private float cameraPeekDistance = 6f;
        [SerializeField] private bool autoPlay = true;

        private Vector3 cameraStartPosition;
        private float elapsed;
        private float directionSign = 1f;
        private bool cameraFollowWasEnabled;
        private bool movementWasEnabled;

        public event Action GuidanceStarted;
        public event Action GuidanceCompleted;

        public bool WasPlayed { get; private set; }
        public bool IsPlaying { get; private set; }
        public float DirectionSign => directionSign;

        public void Configure(
            Camera targetCamera,
            MonoBehaviour targetCameraFollowDriver,
            Transform playerTransform,
            Transform targetExit,
            Transform edgeIndicator,
            P5StageCoreLoop2D targetCoreLoop,
            P5SliceTelemetry2D targetTelemetry,
            float guidanceDuration = RequiredGuidanceSeconds,
            float peekDistance = 6f,
            bool playAutomatically = true,
            MonoBehaviour targetMovementDriver = null)
        {
            if (!Mathf.Approximately(
                    guidanceDuration,
                    RequiredGuidanceSeconds))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(guidanceDuration),
                    guidanceDuration,
                    "The P5 entry cue must reveal the exit direction for one second.");
            }

            stageCamera = targetCamera;
            cameraFollowDriver = targetCameraFollowDriver;
            player = playerTransform;
            movementDriver = targetMovementDriver != null
                ? targetMovementDriver
                : playerTransform != null
                    ? playerTransform.GetComponent<
                        StarNight.Player.PlayerMotor2D>()
                    : null;
            exitTarget = targetExit;
            edgeDirectionIndicator = edgeIndicator;
            coreLoop = targetCoreLoop;
            telemetry = targetTelemetry;
            duration = RequiredGuidanceSeconds;
            cameraPeekDistance = Mathf.Max(0f, peekDistance);
            autoPlay = playAutomatically;
            ResetGuidanceForTests();
        }

        private void Start()
        {
            if (autoPlay)
            {
                PlayOnce();
            }
        }

        private void Update()
        {
            if (!IsPlaying)
            {
                return;
            }

            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            if (stageCamera != null)
            {
                float peek = Mathf.Sin(normalized * Mathf.PI)
                    * cameraPeekDistance;
                stageCamera.transform.position =
                    cameraStartPosition + Vector3.right * directionSign * peek;
            }

            if (normalized >= 1f)
            {
                CompleteGuidance();
            }
        }

        private void OnDisable()
        {
            if (!IsPlaying)
            {
                return;
            }

            IsPlaying = false;
            RestoreDriversAndCamera();
        }

        public bool PlayOnce()
        {
            if (WasPlayed || player == null || exitTarget == null)
            {
                return false;
            }

            WasPlayed = true;
            IsPlaying = true;
            elapsed = 0f;
            directionSign = exitTarget.position.x >= player.position.x
                ? 1f
                : -1f;
            if (stageCamera != null)
            {
                cameraStartPosition = stageCamera.transform.position;
            }

            if (cameraFollowDriver != null)
            {
                cameraFollowWasEnabled = cameraFollowDriver.enabled;
                cameraFollowDriver.enabled = false;
            }

            if (movementDriver != null)
            {
                movementWasEnabled = movementDriver.enabled;
                movementDriver.enabled = false;
            }

            if (edgeDirectionIndicator != null)
            {
                edgeDirectionIndicator.gameObject.SetActive(true);
                Vector3 scale = edgeDirectionIndicator.localScale;
                scale.x = Mathf.Abs(scale.x) * directionSign;
                edgeDirectionIndicator.localScale = scale;
            }

            telemetry?.MarkExitGuidanceShown();
            GuidanceStarted?.Invoke();
            return true;
        }

        public void SkipGuidanceForTests()
        {
            if (!WasPlayed)
            {
                PlayOnce();
            }

            CompleteGuidance();
        }

        public void ResetGuidanceForTests()
        {
            WasPlayed = false;
            IsPlaying = false;
            elapsed = 0f;
        }

        private void CompleteGuidance()
        {
            if (!IsPlaying)
            {
                return;
            }

            IsPlaying = false;
            RestoreDriversAndCamera();
            coreLoop?.CompleteIntroAndBegin();
            GuidanceCompleted?.Invoke();
        }

        private void RestoreDriversAndCamera()
        {
            if (stageCamera != null)
            {
                stageCamera.transform.position = cameraStartPosition;
            }

            if (cameraFollowDriver != null)
            {
                cameraFollowDriver.enabled = cameraFollowWasEnabled;
            }

            if (movementDriver != null)
            {
                movementDriver.enabled = movementWasEnabled;
            }
        }
    }
}

#endif
