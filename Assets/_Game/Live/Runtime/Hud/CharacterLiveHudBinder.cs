using StarNight.Character.Live.Presentation;
using StarNight.Character.Live.Run;
using UnityEngine;
using UnityEngine.UI;

namespace StarNight.Character.Live.Hud
{
    /// <summary>
    /// 라이브 HUD 바인더(씬의 유일한 HUD 바인딩 경로). 매 프레임 뷰 모델을
    /// 투영해 uGUI Text에 반영만 한다 — 권위 상태 없음. 미배선 UI 참조는
    /// 예외 없이 건너뛴다. 피드백 로그와 연출 소비자를 소유해 이후
    /// 시스템의 수신 표면으로 노출한다(도구 배선은 후속 과제 소관).
    /// 오디오/Animator/세이브/씬 로드를 호출하지 않는다.
    /// </summary>
    public sealed class CharacterLiveHudBinder : MonoBehaviour
    {
        [SerializeField] private CharacterLiveRunBootstrap bootstrap;
        [SerializeField] private Text healthText;
        [SerializeField] private Text bombText;
        [SerializeField] private Text ropeText;
        [SerializeField] private Text roomText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text feedbackText;

        private CharacterLiveFeedbackLog feedbackLog;
        private CharacterLivePresentationEventConsumer presentationConsumer;

        /// <summary>이후 시스템이 호출하는 피드백 sink(수신 표면).</summary>
        public CharacterLiveFeedbackLog FeedbackLog
        {
            get { return feedbackLog; }
        }

        /// <summary>캐릭터 연출 이벤트 배치 소비자(수신 표면).</summary>
        public CharacterLivePresentationEventConsumer PresentationConsumer
        {
            get { return presentationConsumer; }
        }

        public bool HasBootstrap
        {
            get { return bootstrap != null; }
        }

        private void Awake()
        {
            feedbackLog = new CharacterLiveFeedbackLog();
            presentationConsumer = new CharacterLivePresentationEventConsumer(
                bootstrap == null ? null : bootstrap.Session, feedbackLog);
            ApplyFallbackFont();
        }

        private void Update()
        {
            CharacterLiveHudSnapshot snapshot = CharacterLiveHudSnapshotSource
                .Project(bootstrap == null ? null : bootstrap.Session, feedbackLog);

            SetText(healthText, BuildHealthLine(in snapshot));
            SetText(bombText, "BOMB " + snapshot.BombCount);
            SetText(ropeText, "ROPE " + snapshot.RopeCount);
            SetText(roomText, "ROOM " + snapshot.RoomLabel);
            SetText(statusText, "RUN " + snapshot.RunStatusLabel);
            SetText(feedbackText, snapshot.LatestFeedback);
        }

        private static string BuildHealthLine(in CharacterLiveHudSnapshot snapshot)
        {
            string line = "HP " + snapshot.CurrentHealth + "/" + snapshot.MaxHealth;
            return snapshot.IsInvulnerable ? line + " INV" : line;
        }

        /// <summary>미배선 참조는 조용히 건너뛴다(씬 로드 중 예외 금지).</summary>
        private static void SetText(Text target, string value)
        {
            if (target == null)
            {
                return;
            }

            if (target.text != value)
            {
                target.text = value;
            }
        }

        /// <summary>폰트 미지정 Text에 내장 기본 폰트를 지정한다(에셋 추가 없음).</summary>
        private void ApplyFallbackFont()
        {
            Font fallback = null;
            Text[] targets =
            { healthText, bombText, ropeText, roomText, statusText, feedbackText };

            for (int index = 0; index < targets.Length; index++)
            {
                Text target = targets[index];

                if (target == null || target.font != null)
                {
                    continue;
                }

                if (fallback == null)
                {
                    fallback = Resources.GetBuiltinResource<Font>(
                        "LegacyRuntime.ttf");
                }

                target.font = fallback;
            }
        }
    }
}
