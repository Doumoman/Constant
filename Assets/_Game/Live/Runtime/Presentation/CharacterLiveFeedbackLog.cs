using System.Collections.Generic;

namespace StarNight.Character.Live.Presentation
{
    /// <summary>
    /// 라이브 피드백 sink(수신 표면). 이후 시스템(도구/방/스폰/피해/사망/런
    /// 실패)이 메시지를 이 인스턴스에 추가하고 HUD가 최신 메시지를 읽는다.
    /// 추가 순서 보존, 용량 초과 시 가장 오래된 항목 제거(결정적).
    /// 권위 게임플레이 상태를 저장하지 않는다 — 표시용 데이터 전용.
    /// </summary>
    public sealed class CharacterLiveFeedbackLog
    {
        public const int DefaultCapacity = 64;

        private readonly List<CharacterLiveFeedbackMessage> messages;
        private readonly int capacity;

        public CharacterLiveFeedbackLog(int capacity = DefaultCapacity)
        {
            this.capacity = capacity < 1 ? 1 : capacity;
            messages = new List<CharacterLiveFeedbackMessage>();
        }

        public int Count
        {
            get { return messages.Count; }
        }

        public int TotalAppendedCount { get; private set; }

        public bool HasMessages
        {
            get { return messages.Count > 0; }
        }

        public CharacterLiveFeedbackMessage GetMessage(int index)
        {
            return messages[index];
        }

        /// <summary>최신 메시지 텍스트 — 비어 있으면 빈 문자열(안정 값).</summary>
        public string LatestText
        {
            get
            {
                return messages.Count == 0
                    ? string.Empty
                    : messages[messages.Count - 1].Text;
            }
        }

        public void Append(in CharacterLiveFeedbackMessage message)
        {
            messages.Add(message);
            TotalAppendedCount++;

            if (messages.Count > capacity)
            {
                messages.RemoveAt(0);
            }
        }

        public void Append(CharacterLiveFeedbackCategory category, string text)
        {
            Append(new CharacterLiveFeedbackMessage(category, text));
        }
    }
}
