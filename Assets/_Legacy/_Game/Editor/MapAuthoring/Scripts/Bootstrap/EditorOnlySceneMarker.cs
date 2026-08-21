#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public sealed class EditorOnlySceneMarker : MonoBehaviour
    {
        [SerializeField] private string purpose = "Map authoring lab - never include in Build Settings";

        public string Purpose => purpose;
    }
}

#endif
