#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Map
{
    public enum FeedbackId
    {
        None,
        Accepted,
        Hit,
        Break,
        Dig,
        WetMud,
        Activate,
        Disable,
        Rotate,
        MetalFail,
        DuplicateAction,
        Busy,
    }

    [Serializable]
    public struct ToolReactionContext
    {
        public int ActionId;
        public ToolTag Tags;
        public GridCell OriginCell;
        public GridCell TargetCell;
        public Vector2Int Direction;
        public float Magnitude;
        public GameObject Source;
        public GameObject Instigator;
    }

    [Serializable]
    public struct ToolReactionResult
    {
        public bool Accepted;
        public bool ChangedState;
        public bool ConsumeToolResource;
        public FeedbackId Feedback;

        public static ToolReactionResult Rejected(FeedbackId feedback)
        {
            return new ToolReactionResult
            {
                Accepted = false,
                ChangedState = false,
                ConsumeToolResource = false,
                Feedback = feedback,
            };
        }
    }

    public interface IToolReactionReceiver
    {
        ToolReactionResult TryReact(ToolReactionContext context);
    }

    public interface IUmbrellaDeflectableProjectile
    {
        bool CanUmbrellaDeflect { get; }
        Vector2 Velocity { get; }
        bool TryDeflect(Vector2 direction, float maximumSpeed, GameObject deflector);
    }

    public readonly struct MapElementDamageEvent
    {
        public MapElementDamageEvent(int damage, Vector2 knockback, GameObject source, int activationId)
        {
            Damage = Mathf.Clamp(damage, 0, 1);
            Knockback = knockback;
            Source = source;
            ActivationId = activationId;
        }

        public int Damage { get; }
        public Vector2 Knockback { get; }
        public GameObject Source { get; }
        public int ActivationId { get; }
    }

    public interface IMapElementDamageReceiver
    {
        bool ReceiveMapElementDamage(MapElementDamageEvent damageEvent);
    }

    public interface IMapExplosionProtected { }

    public interface IMapElementWeightSource
    {
        int PressureWeight { get; }
    }

    public interface IMapElementEnvironmentalReceiver
    {
        void ReceiveWind(Vector2 velocityDelta);
        void ReceiveWater(Vector2 velocityDelta);
    }

    public interface IMapElementSignalReceiver
    {
        void ReceiveSignal(string channel, bool active);
    }

    public interface IMapElementInteractionReceiver
    {
        string InteractionPrompt { get; }
        bool TryInteract(GameObject instigator);
    }
}

#endif
