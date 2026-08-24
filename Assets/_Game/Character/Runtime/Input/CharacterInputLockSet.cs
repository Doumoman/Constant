using System;
using System.Collections.Generic;

namespace StarNight.Character.Input
{
    /// <summary>
    /// 입력 잠금을 단일 bool이 아니라 사유(reason) 집합으로 관리한다.
    /// 사유 하나를 제거해도 다른 사유가 남아 있으면 잠금이 유지된다.
    /// 카메라룸 전환은 입력 잠금 사유가 아니다(전환 중 입력 KEEP 계약) —
    /// 전환 상태는 CharacterPlayerState가 잠금과 무관한 플래그로 추적한다.
    /// </summary>
    public sealed class CharacterInputLockSet
    {
        private readonly HashSet<string> reasons = new HashSet<string>(StringComparer.Ordinal);

        public int Count
        {
            get { return reasons.Count; }
        }

        public bool IsLocked
        {
            get { return reasons.Count > 0; }
        }

        public bool Add(string reason)
        {
            ValidateReason(reason);
            return reasons.Add(reason);
        }

        public bool Remove(string reason)
        {
            ValidateReason(reason);
            return reasons.Remove(reason);
        }

        public bool Contains(string reason)
        {
            ValidateReason(reason);
            return reasons.Contains(reason);
        }

        public void Clear()
        {
            reasons.Clear();
        }

        public IEnumerable<string> Reasons
        {
            get { return reasons; }
        }

        private static void ValidateReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("잠금 사유는 비어 있을 수 없다.", nameof(reason));
            }
        }
    }
}
