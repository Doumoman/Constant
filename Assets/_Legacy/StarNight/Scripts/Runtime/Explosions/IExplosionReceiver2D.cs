#if LEGACY_DISABLED
using StarNight.Grid;
using UnityEngine;

namespace StarNight.Explosions
{
    public readonly struct ExplosionHit2D
    {
        public ExplosionHit2D(
            GridPos centerCell,
            Vector2 worldCenter,
            Bomb2D source)
        {
            CenterCell = centerCell;
            WorldCenter = worldCenter;
            Source = source;
        }

        public GridPos CenterCell { get; }
        public Vector2 WorldCenter { get; }
        public Bomb2D Source { get; }
    }

    public interface IExplosionReceiver2D
    {
        void ReceiveExplosion(ExplosionHit2D hit);
    }
}

#endif
