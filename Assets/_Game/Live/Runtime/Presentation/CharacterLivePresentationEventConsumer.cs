using System.Collections.Generic;
using StarNight.Character.Live.Run;
using StarNight.Character.Presentation;

namespace StarNight.Character.Live.Presentation
{
    /// <summary>
    /// 캐릭터 연출 이벤트 요청(CHAR05_04) 라이브 소비자. 배치 정렬·중복
    /// 제거·순번은 CharacterPresentationBridge.NormalizeBatch에 위임해
    /// 캐릭터 계약의 순서/중복 의미를 그대로 보존하고, 수락된 이벤트만
    /// 결정적 텍스트로 변환해 피드백 로그에 순서대로 추가한다.
    /// 오디오/Animator/타임라인/세이브/씬 로드 API를 호출하지 않는다.
    /// </summary>
    public sealed class CharacterLivePresentationEventConsumer
    {
        private readonly CharacterLiveRunSession session;
        private readonly CharacterLiveFeedbackLog feedbackLog;
        private readonly List<CharacterPresentationEventRequest> normalizedBuffer;

        public CharacterLivePresentationEventConsumer(
            CharacterLiveRunSession session,
            CharacterLiveFeedbackLog feedbackLog)
        {
            this.session = session;
            this.feedbackLog = feedbackLog;
            normalizedBuffer = new List<CharacterPresentationEventRequest>();
        }

        public int AcceptedCount { get; private set; }
        public int DuplicateEventCount { get; private set; }
        public int UnknownEventCount { get; private set; }
        public int MissingTargetCount { get; private set; }
        public int MissingSinkCount { get; private set; }
        public CharacterLivePresentationDiagnosticKind LastDiagnostic
        { get; private set; }

        /// <summary>마지막 배치의 정규화 결과(감사용 read-only 표면).</summary>
        public IReadOnlyList<CharacterPresentationEventRequest> LastNormalizedBatch
        {
            get { return normalizedBuffer; }
        }

        /// <summary>
        /// 원시 이벤트 배치 소비. 반환값은 이번 배치에서 피드백으로 수락된
        /// 이벤트 수. 정규화(우선순위→입력 순서, 내용 동등 중복 1건화,
        /// SequenceId 부여)는 캐릭터 브리지가 수행한다.
        /// </summary>
        public int ConsumeBatch(
            IReadOnlyList<CharacterPresentationEventRequest> rawEvents)
        {
            if (feedbackLog == null)
            {
                MissingSinkCount++;
                LastDiagnostic = CharacterLivePresentationDiagnosticKind.MissingSink;
                return 0;
            }

            LastDiagnostic = CharacterLivePresentationDiagnosticKind.None;
            CharacterPresentationBridge.NormalizeBatch(rawEvents, normalizedBuffer);

            int rawCount = rawEvents == null ? 0 : rawEvents.Count;
            int removedAsDuplicate = rawCount - normalizedBuffer.Count;
            if (removedAsDuplicate > 0)
            {
                DuplicateEventCount += removedAsDuplicate;
                LastDiagnostic =
                    CharacterLivePresentationDiagnosticKind.DuplicateEvent;
            }

            int accepted = 0;
            for (int index = 0; index < normalizedBuffer.Count; index++)
            {
                CharacterPresentationEventRequest request = normalizedBuffer[index];

                if (!IsKnownType(request.Type))
                {
                    UnknownEventCount++;
                    LastDiagnostic =
                        CharacterLivePresentationDiagnosticKind.UnknownEvent;
                    continue;
                }

                if (IsActorScoped(request.Type) && !IsSessionActor(request))
                {
                    MissingTargetCount++;
                    LastDiagnostic =
                        CharacterLivePresentationDiagnosticKind.MissingTarget;
                    continue;
                }

                feedbackLog.Append(CategoryOf(request.Type), BuildText(in request));
                accepted++;
                AcceptedCount++;
            }

            return accepted;
        }

        private bool IsSessionActor(in CharacterPresentationEventRequest request)
        {
            return session != null
                && session.IsRunStarted
                && request.ActorOrSourceId == session.ActorId;
        }

        private static bool IsKnownType(CharacterPresentationEventType type)
        {
            return type >= CharacterPresentationEventType.RunFailure
                && type <= CharacterPresentationEventType.InventoryChanged;
        }

        /// <summary>
        /// 액터 범위 타입 — BombExploded는 소스 id가 폭발 id라 제외된다.
        /// </summary>
        private static bool IsActorScoped(CharacterPresentationEventType type)
        {
            return type != CharacterPresentationEventType.BombExploded;
        }

        private static CharacterLiveFeedbackCategory CategoryOf(
            CharacterPresentationEventType type)
        {
            switch (type)
            {
                case CharacterPresentationEventType.RunFailure:
                    return CharacterLiveFeedbackCategory.RunFailure;
                case CharacterPresentationEventType.Death:
                    return CharacterLiveFeedbackCategory.Death;
                case CharacterPresentationEventType.Damage:
                    return CharacterLiveFeedbackCategory.Damage;
                default:
                    return CharacterLiveFeedbackCategory.Tool;
            }
        }

        private static string BuildText(
            in CharacterPresentationEventRequest request)
        {
            switch (request.Type)
            {
                case CharacterPresentationEventType.RunFailure:
                    return "RUN FAILURE";
                case CharacterPresentationEventType.Death:
                    return "DEATH";
                case CharacterPresentationEventType.Damage:
                    return "DAMAGE -" + request.Amount;
                case CharacterPresentationEventType.BombExploded:
                    return "BOMB EXPLODED ("
                        + request.Cell.X + "," + request.Cell.Y + ")";
                case CharacterPresentationEventType.BombPlaced:
                    return "BOMB PLACED ("
                        + request.Cell.X + "," + request.Cell.Y + ")";
                case CharacterPresentationEventType.RopePlaced:
                    return "ROPE PLACED ("
                        + request.Cell.X + "," + request.Cell.Y + ")";
                default:
                    return "ITEM USED -" + request.Amount;
            }
        }
    }
}
