#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Map
{
    [CreateAssetMenu(menuName = "NightFetch/Map/Map Element Visual Profile")]
    public sealed class MapElementVisualProfileAsset : ScriptableObject
    {
        public string ElementId;
        public string SourceHash;
        public ElementVisualProfile Profile = new ElementVisualProfile();

        public void CopyFrom(string elementId, string sourceHash, ElementVisualProfile source)
        {
            ElementId = elementId;
            SourceHash = sourceHash;
            Profile = source == null
                ? new ElementVisualProfile()
                : JsonUtility.FromJson<ElementVisualProfile>(JsonUtility.ToJson(source));
        }
    }
}

#endif
