using UnityEngine;

namespace StarNight.Character.Movement
{
    /// <summary>
    /// Physics2D 기반 런타임 어댑터. 2D collision query만 수행하며
    /// MAP Tilemap, WorldCoordinateUtility, 생성 맵 내부는 직접 읽지 않는다.
    /// </summary>
    public sealed class UnityPhysics2DCharacterCollisionWorld : ICharacterCollisionWorld
    {
        private readonly LayerMask solidLayers;

        public UnityPhysics2DCharacterCollisionWorld(LayerMask solidLayers)
        {
            this.solidLayers = solidLayers;
        }

        public CharacterCollisionHit CapsuleCast(
            Vector2 origin,
            CharacterCapsuleGeometry capsule,
            Vector2 direction,
            float distance)
        {
            RaycastHit2D[] hits = Physics2D.CapsuleCastAll(
                origin,
                capsule.Size,
                CapsuleDirection2D.Vertical,
                0f,
                direction,
                distance,
                solidLayers);
            foreach (RaycastHit2D hit in hits)
            {
                // Triggers describe traversal/exit volumes; they never own a
                // physical support, wall, or ceiling response.
                if (hit.collider == null || hit.collider.isTrigger)
                {
                    continue;
                }

                return new CharacterCollisionHit(
                    true,
                    hit.point,
                    hit.normal,
                    hit.distance,
                    hit.collider.GetInstanceID());
            }

            return CharacterCollisionHit.None;
        }
    }
}
