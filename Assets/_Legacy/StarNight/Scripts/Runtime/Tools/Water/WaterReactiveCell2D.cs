#if LEGACY_DISABLED
using StarNight.Grid;
using UnityEngine;

namespace StarNight.Tools.Water
{
    public abstract class WaterReactiveCell2D : MonoBehaviour, IWaterReactive2D
    {
        [SerializeField] private WaterInteractionRegistry2D registry;
        [SerializeField] private GridWorld gridWorld;
        [SerializeField] private bool useFixedCell;
        [SerializeField] private Vector2Int fixedCell;
        [SerializeField] private int waterPriority;

        public GridPos WaterCell
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

        public int WaterPriority => waterPriority;
        public abstract bool CanReceiveWater { get; }
        public UnityEngine.Object WaterTargetObject => this;

        protected virtual void Awake()
        {
            if (gridWorld == null)
            {
                gridWorld = FindFirstObjectByType<GridWorld>();
            }

            if (registry == null)
            {
                registry = FindFirstObjectByType<WaterInteractionRegistry2D>();
            }
        }

        protected virtual void OnEnable()
        {
            registry?.Register(this);
        }

        protected virtual void OnDisable()
        {
            registry?.Unregister(this);
        }

        public void ConfigureCell(
            WaterInteractionRegistry2D targetRegistry,
            GridWorld world,
            GridPos cell,
            bool lockToCell = true,
            int priority = 0)
        {
            registry?.Unregister(this);
            registry = targetRegistry;
            gridWorld = world;
            fixedCell = new Vector2Int(cell.X, cell.Y);
            useFixedCell = lockToCell;
            waterPriority = priority;
            if (isActiveAndEnabled)
            {
                registry?.Register(this);
            }
        }

        public void SetCellForTests(GridPos cell)
        {
            fixedCell = new Vector2Int(cell.X, cell.Y);
            useFixedCell = true;
        }

        public abstract WaterReactionKind TryReceiveWater(
            WaterApplication application);
    }
}

#endif
