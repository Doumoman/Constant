using System.Collections.Generic;

namespace StarNight.Character.Live.Tools
{
    /// <summary>인메모리 지형 명령 큐(FIFO) — 씬/자산 접촉 없음.</summary>
    public sealed class CharacterLiveTerrainCommandQueue
        : ICharacterLiveTerrainCommandSink
    {
        private readonly Queue<CharacterLiveTerrainCommand> pending;

        public CharacterLiveTerrainCommandQueue()
        {
            pending = new Queue<CharacterLiveTerrainCommand>();
        }

        public int PendingCount
        {
            get { return pending.Count; }
        }

        public int TotalEnqueuedCount { get; private set; }

        public void Enqueue(in CharacterLiveTerrainCommand command)
        {
            pending.Enqueue(command);
            TotalEnqueuedCount++;
        }

        public bool TryDequeue(out CharacterLiveTerrainCommand command)
        {
            if (pending.Count == 0)
            {
                command = default(CharacterLiveTerrainCommand);
                return false;
            }

            command = pending.Dequeue();
            return true;
        }
    }
}
