using StarNight.Character.State;
using StarNight.Character.Survival;
using UnityEngine;

namespace StarNight.Character.Live.Movement
{
    /// <summary>
    /// RMAP05의 Player-local 낙하 결과 상태. 기존 CharacterHealthState,
    /// CharacterHealthDamagePolicy 및 CharacterPlayerState를 그대로 소비하며
    /// 전투/저장 체력 시스템을 새로 소유하지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterLiveFallDamageState : MonoBehaviour
    {
        private CharacterHealthState health;
        private CharacterPlayerState playerState;
        private float stunEndsAt;

        public int CurrentHealth { get { return health.CurrentHealth; } }
        public int MaxHealth { get { return health.MaxHealth; } }
        public bool IsStunned { get { return playerState != null && playerState.IsStunned; } }
        public bool IsDead { get { return playerState != null && playerState.IsDead; } }
        public bool CanAcceptInput { get { return playerState != null && playerState.CanAcceptInput; } }
        public float LastProcessedFallDistance { get; private set; }
        public int LastAppliedDamage { get; private set; }
        public bool LastLandingWasOneWay { get; private set; }
        public string LastLandingSequence { get; private set; }
        public int LandingCount { get; private set; }
        public CharacterLiveFallResetKind LastTraversalResetKind { get; private set; }
        public float LastTraversalResetDistance { get; private set; }

        private void Awake()
        {
            EnsureInitialized();
        }

        public void ResetForSpawn(int actorId, CharacterLiveMovementSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            playerState = new CharacterPlayerState();
            health = CharacterHealthState.CreateFull(actorId,
                CharacterSurvivalTargetKind.Player, settings.FallMaxHealth);
            stunEndsAt = 0f;
            LastProcessedFallDistance = 0f;
            LastAppliedDamage = 0;
            LastLandingWasOneWay = false;
            LastLandingSequence = string.Empty;
            LandingCount = 0;
            LastTraversalResetKind = CharacterLiveFallResetKind.Spawn;
            LastTraversalResetDistance = 0f;
        }

        public void Tick(float physicsTime)
        {
            EnsureInitialized();
            if (!playerState.IsDead && playerState.IsStunned && physicsTime >= stunEndsAt)
            {
                playerState.SetStunned(false);
            }
        }

        public void ApplyLanding(
            float fallDistance,
            bool landedOnOneWay,
            float physicsTime,
            CharacterLiveMovementSettings settings)
        {
            EnsureInitialized();
            if (settings == null || playerState.IsDead)
            {
                return;
            }

            CharacterLiveFallLandingResult landing = settings.EvaluateFallLanding(fallDistance);
            LastProcessedFallDistance = Mathf.Max(0f, fallDistance);
            LastLandingWasOneWay = landedOnOneWay;
            LastAppliedDamage = 0;
            LandingCount++;

            if (landing.IsFatal || landing.Damage > 0)
            {
                int amount = landing.IsFatal ? health.CurrentHealth : landing.Damage;
                var request = new CharacterSurvivalDamageRequest(
                    CharacterDamageSourceKind.Fall,
                    gameObject.GetInstanceID(),
                    health.ActorId,
                    CharacterSurvivalTargetKind.Player,
                    amount,
                    Vector2.down,
                    bypassInvulnerability: true);
                var survivalSettings = new CharacterSurvivalSettings(settings.FallMaxHealth, 0f);
                CharacterDamageApplicationResult application =
                    CharacterHealthDamagePolicy.ApplyDamage(in health, in request, in survivalSettings);
                health = application.NewState;
                LastAppliedDamage = application.AppliedAmount;
                if (application.HasDeathRequest)
                {
                    playerState.SetDead(true);
                }
            }

            if (!playerState.IsDead && landing.AppliesStun)
            {
                playerState.SetStunned(true);
                stunEndsAt = physicsTime + settings.FallStunDuration;
            }

            LastLandingSequence = "fallDistance -> damage/stun/death";
        }

        public void CompleteLandingReset()
        {
            if (!string.IsNullOrEmpty(LastLandingSequence))
            {
                LastLandingSequence += " -> reset";
            }
        }

        public void RecordTraversalReset(
            CharacterLiveFallResetKind kind,
            float observedFallDistance)
        {
            LastTraversalResetKind = kind;
            LastTraversalResetDistance = Mathf.Max(0f, observedFallDistance);
        }

        private void EnsureInitialized()
        {
            if (playerState != null)
            {
                return;
            }

            playerState = new CharacterPlayerState();
            health = CharacterHealthState.CreateFull(gameObject.GetInstanceID(),
                CharacterSurvivalTargetKind.Player, 5);
            LastLandingSequence = string.Empty;
            LastTraversalResetKind = CharacterLiveFallResetKind.Spawn;
        }
    }

    public enum CharacterLiveFallResetKind
    {
        Spawn,
        Ground,
        Grab,
        Climb,
        Landing
    }
}
