using System.Collections.Generic;

namespace StarNight.Character.Live.Tools
{
    /// <summary>인메모리 로프 명령 큐(FIFO) — 씬/자산 접촉 없음.</summary>
    public sealed class CharacterLiveRopeCommandQueue : ICharacterLiveRopeCommandSink
    {
        private readonly Queue<CharacterLiveRopeCommand> pending;

        public CharacterLiveRopeCommandQueue()
        {
            pending = new Queue<CharacterLiveRopeCommand>();
        }

        public int PendingCount
        {
            get { return pending.Count; }
        }

        public int TotalEnqueuedCount { get; private set; }

        public void Enqueue(in CharacterLiveRopeCommand command)
        {
            pending.Enqueue(command);
            TotalEnqueuedCount++;
        }

        public bool TryDequeue(out CharacterLiveRopeCommand command)
        {
            if (pending.Count == 0)
            {
                command = default(CharacterLiveRopeCommand);
                return false;
            }

            command = pending.Dequeue();
            return true;
        }
    }
}
