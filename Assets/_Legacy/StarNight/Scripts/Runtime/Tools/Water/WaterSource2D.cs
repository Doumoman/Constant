#if LEGACY_DISABLED
using System;
using StarNight.Grid;
using UnityEngine;

namespace StarNight.Tools.Water
{
    [DisallowMultipleComponent]
    public sealed class WaterSource2D : MonoBehaviour
    {
        [SerializeField] private GridWorld gridWorld;
        [SerializeField] private bool useFixedCell;
        [SerializeField] private Vector2Int fixedCell;
        [SerializeField, Min(0)] private int refillRange = 1;
        [SerializeField] private bool available = true;

        public event Action<WateringCanTool2D> CanRefilled;

        public bool Available => available;
        public int RefillRange => refillRange;
        public GridPos Cell
        {
            get
            {
                if (useFixedCell)
                {
                    return new GridPos(fixedCell.x, fixedCell.y);
                }

                return gridWorld != null
                    ? gridWorld.WorldToCell(transform.position)
                    : new GridPos(
                        Mathf.FloorToInt(transform.position.x),
                        Mathf.FloorToInt(transform.position.y));
            }
        }

        private void Awake()
        {
            if (gridWorld == null)
            {
                gridWorld = FindFirstObjectByType<GridWorld>();
            }
        }

        public void Configure(
            GridWorld world,
            GridPos cell,
            int range = 1,
            bool isAvailable = true)
        {
            gridWorld = world;
            fixedCell = new Vector2Int(cell.X, cell.Y);
            useFixedCell = true;
            refillRange = Mathf.Max(0, range);
            available = isAvailable;
        }

        public bool CanRefillAt(GridPos actorCell)
        {
            if (!available)
            {
                return false;
            }

            GridPos sourceCell = Cell;
            int distance = Mathf.Abs(sourceCell.X - actorCell.X)
                + Mathf.Abs(sourceCell.Y - actorCell.Y);
            return distance <= refillRange;
        }

        public bool TryRefill(
            WateringCanTool2D wateringCan,
            GridPos actorCell)
        {
            if (wateringCan == null
                || !wateringCan.TryRecharge(this, actorCell))
            {
                return false;
            }

            CanRefilled?.Invoke(wateringCan);
            return true;
        }

        public void SetAvailable(bool value)
        {
            available = value;
        }
    }
}

#endif
