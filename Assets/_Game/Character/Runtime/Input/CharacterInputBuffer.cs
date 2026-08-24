using System;
using System.Collections.Generic;

namespace StarNight.Character.Input
{
    /// <summary>
    /// 렌더 프레임에서 수집한 눌림을 물리 틱 소비 전까지 보존하는 입력 버퍼.
    /// action별 buffer window를 지원하고, 소비된 action은 같은 틱에서 중복 반환하지 않으며,
    /// 만료된 action은 반환하지 않는다. Down+Action으로 발생한 press는 SafeDrop으로만
    /// 기록되므로 같은 물리적 press가 단독 Action으로 중복 소비되지 않는다.
    /// </summary>
    public sealed class CharacterInputBuffer
    {
        private readonly Dictionary<CharacterActionId, double> pressTimes =
            new Dictionary<CharacterActionId, double>();

        private readonly Dictionary<CharacterActionId, double> windowOverrides =
            new Dictionary<CharacterActionId, double>();

        private readonly Dictionary<CharacterActionId, long> lastConsumedTick =
            new Dictionary<CharacterActionId, long>();

        private readonly double defaultWindowSeconds;

        public CharacterInputBuffer(double defaultWindowSeconds)
        {
            if (defaultWindowSeconds <= 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(defaultWindowSeconds),
                    "buffer window는 0보다 커야 한다.");
            }

            this.defaultWindowSeconds = defaultWindowSeconds;
        }

        public void SetBufferWindow(CharacterActionId actionId, double windowSeconds)
        {
            if (windowSeconds <= 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(windowSeconds),
                    "buffer window는 0보다 커야 한다.");
            }

            windowOverrides[actionId] = windowSeconds;
        }

        /// <summary>렌더 프레임에서 개별 press를 기록한다.</summary>
        public void RecordPress(CharacterActionId actionId, double time)
        {
            pressTimes[actionId] = time;
        }

        /// <summary>
        /// 스냅샷의 이번 프레임 press를 버퍼에 기록한다.
        /// Action press는 DownHeld 여부에 따라 SafeDrop 또는 Action 중 하나로만 기록된다.
        /// </summary>
        public void CaptureFrame(in CharacterInputSnapshot snapshot, double time)
        {
            if (snapshot.Jump.PressedThisFrame)
            {
                RecordPress(CharacterActionId.Jump, time);
            }

            if (snapshot.Bomb.PressedThisFrame)
            {
                RecordPress(CharacterActionId.Bomb, time);
            }

            if (snapshot.Rope.PressedThisFrame)
            {
                RecordPress(CharacterActionId.Rope, time);
            }

            if (snapshot.Action.PressedThisFrame)
            {
                RecordPress(
                    snapshot.DownHeld ? CharacterActionId.SafeDrop : CharacterActionId.Action,
                    time);
            }
        }

        public bool HasPending(CharacterActionId actionId, double time)
        {
            double pressTime;
            if (!pressTimes.TryGetValue(actionId, out pressTime))
            {
                return false;
            }

            if (time - pressTime > WindowFor(actionId))
            {
                pressTimes.Remove(actionId);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 물리 틱에서 버퍼된 press를 소비한다. 성공 시 같은 press는 다시 반환되지 않고
        /// 같은 틱에서는 같은 action이 두 번 반환되지 않는다.
        /// </summary>
        public bool TryConsume(CharacterActionId actionId, long physicsTick, double time)
        {
            long consumedTick;
            if (lastConsumedTick.TryGetValue(actionId, out consumedTick) && consumedTick == physicsTick)
            {
                return false;
            }

            if (!HasPending(actionId, time))
            {
                return false;
            }

            pressTimes.Remove(actionId);
            lastConsumedTick[actionId] = physicsTick;
            return true;
        }

        public void Clear()
        {
            pressTimes.Clear();
            lastConsumedTick.Clear();
        }

        private double WindowFor(CharacterActionId actionId)
        {
            double window;
            return windowOverrides.TryGetValue(actionId, out window) ? window : defaultWindowSeconds;
        }
    }
}
