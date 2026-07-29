using System;
using StarNight.Rewrite.Core;
using UnityEngine;

namespace StarNight.Rewrite.Player
{
    [RequireComponent(typeof(PlayerInputReader))]
    [DisallowMultipleComponent]
    public sealed class PlayerToolController : MonoBehaviour
    {
        [SerializeField]
        private RunContext runContext;

        private PlayerInputReader input;

        public event Action<HandToolId> ToolUseRequested;
        public event Action<HandToolId, HandToolId> ToolChanged;

        public HandToolId CurrentTool =>
            runContext != null ? runContext.Loadout.HandTool : HandToolId.None;

        private void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            if (runContext == null)
            {
                runContext = FindFirstObjectByType<RunContext>();
            }
        }

        private void OnEnable()
        {
            input.UseHandToolPressed += RequestToolUse;
        }

        private void OnDisable()
        {
            input.UseHandToolPressed -= RequestToolUse;
        }

        public HandToolId Equip(HandToolId nextTool)
        {
            if (runContext == null)
            {
                return HandToolId.None;
            }

            HandToolId previous = runContext.Loadout.EquipHandTool(nextTool);
            if (previous != nextTool)
            {
                ToolChanged?.Invoke(previous, nextTool);
            }

            return previous;
        }

        private void RequestToolUse()
        {
            if (CurrentTool != HandToolId.None)
            {
                ToolUseRequested?.Invoke(CurrentTool);
            }
        }
    }
}
