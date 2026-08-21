#if LEGACY_DISABLED
using StarNight.Objects;
using StarNight.Tools;
using UnityEngine;

namespace StarNight.Stages.P5
{
    public enum P5ShopProductKind
    {
        RopeBundle3,
        BombBundle2,
        MoonCake
    }

    public enum P5MoonRabbitPestleState
    {
        WaitingForPestle,
        PestleDiscovered,
        Completed
    }

    public enum P5MaruBellPhase
    {
        Calm,
        FirstBell,
        SecondBell,
        MaruDue,
        Stopped
    }

    public enum P5BellSignal
    {
        Short,
        Long
    }

    public enum P5StageExitState
    {
        Unseen,
        Reached,
        Confirming,
        Departed
    }

    public enum P5CoreLoopState
    {
        Intro,
        Active,
        ExitReached,
        Departed
    }

    public readonly struct P5PlayerInteractionContext
    {
        public P5PlayerInteractionContext(
            Transform playerTransform,
            CarrySystem carrySystem,
            PlayerToolInventory2D toolInventory,
            PlayerConsumableTools2D consumableTools,
            P5RunState2D runState)
        {
            PlayerTransform = playerTransform;
            CarrySystem = carrySystem;
            ToolInventory = toolInventory;
            ConsumableTools = consumableTools;
            RunState = runState;
        }

        public Transform PlayerTransform { get; }
        public CarrySystem CarrySystem { get; }
        public PlayerToolInventory2D ToolInventory { get; }
        public PlayerConsumableTools2D ConsumableTools { get; }
        public P5RunState2D RunState { get; }

        public Vector2 PlayerPosition =>
            PlayerTransform != null
                ? (Vector2)PlayerTransform.position
                : Vector2.zero;
    }
}

#endif
