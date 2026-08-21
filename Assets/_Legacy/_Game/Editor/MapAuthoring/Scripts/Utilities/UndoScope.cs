#if LEGACY_DISABLED
using System;
using UnityEditor;

namespace StarNight.MapAuthoring.Editor
{
    public readonly struct UndoScope : IDisposable
    {
        private readonly int group;

        public UndoScope(string name)
        {
            Undo.IncrementCurrentGroup();
            group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(name);
        }

        public void Dispose()
        {
            Undo.CollapseUndoOperations(group);
        }
    }
}

#endif
