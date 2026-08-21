#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Map
{
    [CreateAssetMenu(menuName = "NightFetch/Map/Map Element Definition")]
    public sealed class MapElementDefinition : ScriptableObject
    {
        public string ElementId;
        public string DisplayName;
        public ElementCategory Category;
        public RegionMask AllowedRegions = RegionMask.Common;

        public GameObject RuntimePrefab;
        public MapElementVisualProfileAsset BakedVisualProfile;
        public CellFootprint Footprint = new CellFootprint();
        public ElementVisualProfile VisualProfile = new ElementVisualProfile();
        public ElementCollisionProfile CollisionProfile = new ElementCollisionProfile();
        public ElementBehaviorProfile BehaviorProfile = new ElementBehaviorProfile();
        public ElementPlacementProfile PlacementProfile = new ElementPlacementProfile();
        public ElementBudgetProfile BudgetProfile = new ElementBudgetProfile();
        public CommonElementRuntimeProfile CommonProfile = new CommonElementRuntimeProfile();
        public MaruElementRuntimeProfile MaruProfile = new MaruElementRuntimeProfile();
        public MoonElementRuntimeProfile MoonProfile = new MoonElementRuntimeProfile();
        public BridgeElementRuntimeProfile BridgeProfile = new BridgeElementRuntimeProfile();
        public PalaceElementRuntimeProfile PalaceProfile = new PalaceElementRuntimeProfile();
        public PostElementRuntimeProfile PostProfile = new PostElementRuntimeProfile();
        public SunElementRuntimeProfile SunProfile = new SunElementRuntimeProfile();
        public PolarisElementRuntimeProfile PolarisProfile = new PolarisElementRuntimeProfile();
        public ToolReactionTable ToolReactions = new ToolReactionTable();
        public MaruReactionProfile MaruReaction = new MaruReactionProfile();
        public ElementBakeMetadata BakeMetadata = new ElementBakeMetadata();
    }
}

#endif
