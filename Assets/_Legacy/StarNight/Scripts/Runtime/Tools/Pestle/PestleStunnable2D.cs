#if LEGACY_DISABLED
using System;
using StarNight.Grid;
using UnityEngine;

namespace StarNight.Tools.Pestle
{
    [DisallowMultipleComponent]
    public sealed class PestleStunnable2D : PestleTargetCell2D
    {
        public const float DefaultStunDuration = 1.25f;

        [SerializeField, Min(0.1f)] private float stunDuration =
            DefaultStunDuration;
        [SerializeField] private Behaviour[] disableWhileStunned;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color stunnedColor =
            new Color(1f, 0.92f, 0.35f, 1f);

        private float stunnedUntil;
        private bool stunnedState;

        public event Action<float> StunStarted;
        public event Action StunEnded;

        public float StunnedUntil => stunnedUntil;
        public float StunDuration => stunDuration;
        public bool IsStunned => IsStunnedAt(Time.time);
        public override bool CanReceivePestle => true;

        protected override void Awake()
        {
            base.Awake();
            if (visual == null)
            {
                visual = GetComponentInChildren<SpriteRenderer>();
            }

            stunDuration = Mathf.Max(0.1f, stunDuration);
            ApplyState(false);
        }

        private void Update()
        {
            TickAt(Time.time);
        }

        public void Configure(
            PestleInteractionRegistry2D registry,
            GridWorld world,
            GridPos cell,
            float duration = DefaultStunDuration,
            Behaviour[] behavioursToDisable = null,
            SpriteRenderer targetVisual = null)
        {
            ConfigureCell(registry, world, cell);
            stunDuration = Mathf.Max(0.1f, duration);
            disableWhileStunned = behavioursToDisable;
            visual = targetVisual;
            stunnedUntil = 0f;
            stunnedState = false;
            ApplyState(false);
        }

        public override PestleReactionKind TryReceivePestle(
            PestleStrikeContext context)
        {
            if (context.StrikeCell != PestleCell)
            {
                return PestleReactionKind.None;
            }

            bool wasStunned = IsStunnedAt(context.Timestamp);
            stunnedUntil = Mathf.Max(
                stunnedUntil,
                context.Timestamp + stunDuration);
            stunnedState = true;
            ApplyState(true);
            if (!wasStunned)
            {
                StunStarted?.Invoke(stunDuration);
            }

            return PestleReactionKind.EnemyStunned;
        }

        public bool IsStunnedAt(float timestamp)
        {
            return timestamp < stunnedUntil;
        }

        public bool TickAt(float timestamp)
        {
            bool shouldBeStunned = IsStunnedAt(timestamp);
            if (stunnedState == shouldBeStunned)
            {
                return false;
            }

            stunnedState = shouldBeStunned;
            ApplyState(stunnedState);
            if (!stunnedState)
            {
                StunEnded?.Invoke();
            }

            return true;
        }

        public void ClearStunForTests()
        {
            stunnedUntil = 0f;
            stunnedState = false;
            ApplyState(false);
        }

        private void ApplyState(bool stunned)
        {
            if (disableWhileStunned != null)
            {
                for (int index = 0;
                     index < disableWhileStunned.Length;
                     index++)
                {
                    Behaviour behaviour = disableWhileStunned[index];
                    if (behaviour != null && behaviour != this)
                    {
                        behaviour.enabled = !stunned;
                    }
                }
            }

            if (visual != null)
            {
                visual.color = stunned ? stunnedColor : normalColor;
            }
        }
    }
}

#endif
