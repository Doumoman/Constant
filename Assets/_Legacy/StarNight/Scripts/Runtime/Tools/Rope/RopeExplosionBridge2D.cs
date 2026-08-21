#if LEGACY_DISABLED
using StarNight.Explosions;
using StarNight.Grid;
using UnityEngine;

namespace StarNight.Tools.Rope
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Bomb2D))]
    public sealed class RopeExplosionBridge2D : MonoBehaviour
    {
        [SerializeField] private Bomb2D bomb;
        [SerializeField] private GridWorld gridWorld;

        private bool subscribed;

        private void Reset()
        {
            bomb = GetComponent<Bomb2D>();
        }

        private void Awake()
        {
            ResolveDependencies();
        }

        private void OnEnable()
        {
            ResolveDependencies();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(Bomb2D configuredBomb, GridWorld world)
        {
            Unsubscribe();
            bomb = configuredBomb != null
                ? configuredBomb
                : GetComponent<Bomb2D>();
            gridWorld = world;
            Subscribe();
        }

        public int BreakRopesInMaskForTests(GridPos center)
        {
            int brokenCount = 0;
            for (int index =
                     RopeInstallation2D.ActiveInstallations.Count - 1;
                 index >= 0;
                 index--)
            {
                RopeInstallation2D installation =
                    RopeInstallation2D.ActiveInstallations[index];
                if (installation != null
                    && !installation.IsBroken
                    && installation.IntersectsExplosion(center)
                    && installation.Break(RopeDamageKind.Explosion, bomb))
                {
                    brokenCount++;
                }
            }

            return brokenCount;
        }

        private void HandleDetonated(Bomb2D detonatedBomb)
        {
            Vector2 position = detonatedBomb.transform.position;
            GridPos center = gridWorld != null
                ? gridWorld.WorldToCell(position)
                : new GridPos(
                    Mathf.FloorToInt(position.x),
                    Mathf.FloorToInt(position.y));
            BreakRopesInMaskForTests(center);
        }

        private void ResolveDependencies()
        {
            if (bomb == null)
            {
                bomb = GetComponent<Bomb2D>();
            }

            if (gridWorld == null && bomb != null && bomb.Service != null)
            {
                gridWorld = bomb.Service.GridWorld;
            }

            if (gridWorld == null)
            {
                gridWorld = GetComponentInParent<GridWorld>();
                if (gridWorld == null)
                {
                    gridWorld = FindFirstObjectByType<GridWorld>();
                }
            }
        }

        private void Subscribe()
        {
            if (subscribed || bomb == null)
            {
                return;
            }

            bomb.Detonated += HandleDetonated;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (bomb != null)
            {
                bomb.Detonated -= HandleDetonated;
            }

            subscribed = false;
        }
    }
}

#endif
