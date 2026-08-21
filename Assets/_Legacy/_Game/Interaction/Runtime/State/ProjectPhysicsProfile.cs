#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Interaction.State
{
    [CreateAssetMenu(menuName = "Game/Interaction/Project Physics Profile")]
    public sealed class ProjectPhysicsProfile : ScriptableObject
    {
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private LayerMask interactionMask;
        [SerializeField] private LayerMask toolTargetMask;
        [SerializeField] private LayerMask hookMask;
        [SerializeField] private LayerMask bombAffectMask;
        [SerializeField] private LayerMask dropBlockMask;
        [SerializeField] private LayerMask portalBoundaryMask;
        [SerializeField] private LayerMask voidRecoveryMask;

        public LayerMask GroundMask => groundMask;
        public LayerMask InteractionMask => interactionMask;
        public LayerMask ToolTargetMask => toolTargetMask;
        public LayerMask HookMask => hookMask;
        public LayerMask BombAffectMask => bombAffectMask;
        public LayerMask DropBlockMask => dropBlockMask;
        public LayerMask PortalBoundaryMask => portalBoundaryMask;
        public LayerMask VoidRecoveryMask => voidRecoveryMask;

        private void OnEnable()
        {
            if (groundMask.value == 0 || interactionMask.value == 0)
            {
                RebuildFromApprovedLayers();
            }
        }

        public void RebuildFromApprovedLayers()
        {
            groundMask = LayerMask.GetMask("TerrainSolid", "TerrainOneWay", "DynamicObject");
            interactionMask = LayerMask.GetMask("Interaction");
            toolTargetMask = LayerMask.GetMask("TerrainSolid", "DynamicObject", "Enemy", "Hazard");
            hookMask = toolTargetMask;
            bombAffectMask = LayerMask.GetMask(
                "TerrainSolid",
                "DynamicObject",
                "Enemy",
                "Hazard",
                "Rope");
            dropBlockMask = LayerMask.GetMask(
                "TerrainSolid",
                "UnbreakableBoundary",
                "PortalBoundary",
                "DynamicObject");
            portalBoundaryMask = LayerMask.GetMask("PortalBoundary");
            voidRecoveryMask = LayerMask.GetMask("VoidRecovery");
        }

        public void ConfigureForTests(LayerMask ground, LayerMask dropBlock, LayerMask portal, LayerMask voidMask)
        {
            groundMask = ground;
            dropBlockMask = dropBlock;
            portalBoundaryMask = portal;
            voidRecoveryMask = voidMask;
        }
    }
}

#endif
