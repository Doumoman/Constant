using System;
using StarNight.Rewrite.Core;
using UnityEngine;

namespace StarNight.Rewrite.Player
{
    [RequireComponent(typeof(PlayerInputReader))]
    [DisallowMultipleComponent]
    public sealed class ConsumableInventory : MonoBehaviour
    {
        [SerializeField]
        private RunContext runContext;

        private PlayerInputReader input;

        public event Action RopeUseRequested;
        public event Action BombUseRequested;

        public RunLoadout Loadout => runContext != null ? runContext.Loadout : null;

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
            input.UseRopePressed += RequestRopeUse;
            input.UseBombPressed += RequestBombUse;
        }

        private void OnDisable()
        {
            input.UseRopePressed -= RequestRopeUse;
            input.UseBombPressed -= RequestBombUse;
        }

        public int AddRopes(int amount)
        {
            return Loadout?.AddRopes(amount) ?? 0;
        }

        public int AddBombs(int amount)
        {
            return Loadout?.AddBombs(amount) ?? 0;
        }

        public bool CommitRopeUse()
        {
            return Loadout?.TryConsumeRope() ?? false;
        }

        public bool CommitBombUse()
        {
            return Loadout?.TryConsumeBomb() ?? false;
        }

        private void RequestRopeUse()
        {
            if ((Loadout?.Ropes ?? 0) > 0)
            {
                RopeUseRequested?.Invoke();
            }
        }

        private void RequestBombUse()
        {
            if ((Loadout?.Bombs ?? 0) > 0)
            {
                BombUseRequested?.Invoke();
            }
        }
    }
}
