#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Map;
using StarNight.Stage.Rooms;
using UnityEngine;

namespace StarNight.Stage.Layout
{
    [DisallowMultipleComponent]
    public sealed class EmbeddedPocketRuntime : MonoBehaviour, IToolReactionReceiver, IRoomPersistentParticipant
    {
        [Serializable]
        private struct Snapshot
        {
            public bool revealed;
            public bool collected;
        }

        private readonly HashSet<int> processedActions = new HashSet<int>();
        private GeneratedHiddenContent definition;

        public event Action Revealed;
        public event Action Collected;

        public string PersistenceId => "EmbeddedPocket:" + (definition?.StableId ?? gameObject.name);
        public bool IsRevealed { get; private set; }
        public bool IsCollected { get; private set; }
        public HiddenPocketHint Hint => definition?.Hint ?? HiddenPocketHint.FineCrack;

        public void Configure(GeneratedHiddenContent configuredDefinition)
        {
            definition = configuredDefinition;
        }

        public ToolReactionResult TryReact(ToolReactionContext context)
        {
            if (!processedActions.Add(context.ActionId))
            {
                return ToolReactionResult.Rejected(FeedbackId.DuplicateAction);
            }
            ToolTag revealTool = context.Tags & (ToolTag.Bomb | ToolTag.Pickaxe | ToolTag.Shovel);
            if (IsRevealed || revealTool == ToolTag.None)
            {
                return ToolReactionResult.Rejected(FeedbackId.None);
            }

            IsRevealed = true;
            Revealed?.Invoke();
            return new ToolReactionResult
            {
                Accepted = true,
                ChangedState = true,
                ConsumeToolResource = (revealTool & ToolTag.Bomb) == 0,
                Feedback = FeedbackId.Hit,
            };
        }

        public bool TryCollect()
        {
            if (!IsRevealed || IsCollected) return false;
            IsCollected = true;
            Collected?.Invoke();
            return true;
        }

        public string CaptureRoomState()
        {
            return JsonUtility.ToJson(new Snapshot { revealed = IsRevealed, collected = IsCollected });
        }

        public void RestoreRoomState(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) return;
            Snapshot snapshot = JsonUtility.FromJson<Snapshot>(payload);
            IsRevealed = snapshot.revealed;
            IsCollected = snapshot.collected;
        }
    }

    public enum ToolEscapeOpenReason
    {
        None,
        ChallengeCleared,
        RewardAbandoned,
        ThirdBellEmergency,
    }

    [Serializable]
    public sealed class ToolEscapeRuntimeState
    {
        private GeneratedToolEscape definition;
        private float missingSince = -1f;
        private float abandonHeldSeconds;

        public GeneratedToolEscape Definition => definition;
        public bool RequiredToolAvailable { get; private set; }
        public bool RecoveryRackHasTool { get; private set; }
        public bool DoorOpen { get; private set; }
        public bool RewardForfeited { get; private set; }
        public bool RewardCollected { get; private set; }
        public ToolEscapeOpenReason OpenReason { get; private set; }
        public float AbandonProgress01 => definition == null || definition.AbandonHoldSeconds <= 0f
            ? 0f
            : Mathf.Clamp01(abandonHeldSeconds / definition.AbandonHoldSeconds);

        public void Configure(GeneratedToolEscape configuredDefinition)
        {
            definition = configuredDefinition;
            RequiredToolAvailable = configuredDefinition != null && configuredDefinition.RequiredTool != ToolTag.None;
            RecoveryRackHasTool = false;
            DoorOpen = false;
            RewardForfeited = false;
            RewardCollected = false;
            OpenReason = ToolEscapeOpenReason.None;
            missingSince = -1f;
            abandonHeldSeconds = 0f;
        }

        public void NotifyRequiredToolAvailable(bool available, float now)
        {
            if (definition == null || DoorOpen) return;
            RequiredToolAvailable = available;
            if (available)
            {
                RecoveryRackHasTool = false;
                missingSince = -1f;
            }
            else if (missingSince < 0f)
            {
                missingSince = Mathf.Max(0f, now);
            }
        }

        public void Tick(float now, bool thirdBellReached)
        {
            if (definition == null || DoorOpen) return;
            if (thirdBellReached && definition.EmergencyDoorAfterThirdBell)
            {
                Open(ToolEscapeOpenReason.ThirdBellEmergency, false);
                return;
            }
            if (!RequiredToolAvailable && !RecoveryRackHasTool && missingSince >= 0f &&
                now - missingSince >= definition.RecoveryDelaySeconds)
            {
                RecoveryRackHasTool = true;
            }
        }

        public bool TryTakeRecoveryTool()
        {
            if (!RecoveryRackHasTool || DoorOpen) return false;
            RecoveryRackHasTool = false;
            RequiredToolAvailable = true;
            missingSince = -1f;
            return true;
        }

        public void TickAbandonHold(bool held, float deltaSeconds)
        {
            if (definition == null || DoorOpen) return;
            if (!held)
            {
                abandonHeldSeconds = 0f;
                return;
            }
            abandonHeldSeconds += Mathf.Max(0f, deltaSeconds);
            if (abandonHeldSeconds >= definition.AbandonHoldSeconds)
            {
                Open(ToolEscapeOpenReason.RewardAbandoned, true);
            }
        }

        public bool CompleteChallenge()
        {
            if (definition == null || DoorOpen) return false;
            Open(ToolEscapeOpenReason.ChallengeCleared, false);
            return true;
        }

        public bool TryCollectReward()
        {
            if (!DoorOpen || RewardForfeited || RewardCollected) return false;
            RewardCollected = true;
            return true;
        }

        private void Open(ToolEscapeOpenReason reason, bool forfeitReward)
        {
            DoorOpen = true;
            OpenReason = reason;
            RewardForfeited = forfeitReward;
            abandonHeldSeconds = 0f;
        }
    }
}

#endif
