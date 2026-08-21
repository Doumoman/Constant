#if LEGACY_DISABLED
using System;
using StarNight.Map;
using UnityEditor;

namespace StarNight.MapAuthoring.Editor
{
    public enum MapElementEditMode
    {
        Footprint,
        Visual,
        Collider,
        Path,
        Signal,
    }

    [InitializeOnLoad]
    public static class MapElementAuthoringSession
    {
        private static MapElementDefinition selectedDefinition;
        private static MapElementEditMode editMode;

        static MapElementAuthoringSession()
        {
            Selection.selectionChanged -= FollowProjectSelection;
            Selection.selectionChanged += FollowProjectSelection;
        }

        public static event Action Changed;

        public static MapElementDefinition SelectedDefinition
        {
            get => selectedDefinition;
            set
            {
                if (selectedDefinition == value)
                {
                    return;
                }

                selectedDefinition = value;
                if (value != null && Selection.activeObject != value)
                {
                    Selection.activeObject = value;
                }

                Changed?.Invoke();
                SceneView.RepaintAll();
            }
        }

        public static MapElementEditMode EditMode
        {
            get => editMode;
            set
            {
                if (editMode == value)
                {
                    return;
                }

                editMode = value;
                Changed?.Invoke();
                SceneView.RepaintAll();
            }
        }

        public static void NotifyDefinitionChanged()
        {
            Changed?.Invoke();
            SceneView.RepaintAll();
        }

        private static void FollowProjectSelection()
        {
            if (Selection.activeObject is MapElementDefinition definition &&
                selectedDefinition != definition)
            {
                selectedDefinition = definition;
                Changed?.Invoke();
                SceneView.RepaintAll();
            }
        }
    }
}

#endif
