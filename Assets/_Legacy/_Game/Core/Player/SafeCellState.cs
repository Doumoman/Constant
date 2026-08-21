#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Core.Player
{
    [Serializable]
    public struct SafeCellState
    {
        [SerializeField] private bool isValid;
        [SerializeField] private Vector2Int cell;
        [SerializeField] private Vector2 playerCenter;

        public bool IsValid => isValid;
        public Vector2Int Cell => cell;
        public Vector2 PlayerCenter => playerCenter;

        public static SafeCellState FromPlayerCenter(Vector2 playerCenter)
        {
            return new SafeCellState
            {
                isValid = true,
                cell = PlayerGridContract.PlayerCenterToCell(playerCenter),
                playerCenter = playerCenter
            };
        }

        public static SafeCellState Create(Vector2Int cell, Vector2 playerCenter)
        {
            return new SafeCellState
            {
                isValid = true,
                cell = cell,
                playerCenter = playerCenter
            };
        }
    }
}

#endif
