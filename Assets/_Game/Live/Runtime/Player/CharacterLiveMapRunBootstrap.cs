using StarNight.Character.Live.Movement;
using UnityEngine;

namespace StarNight.Character.Live.Player
{
    /// <summary>
    /// Minimal local composition for a physical map-run fixture. It places the
    /// already-bound Player on one authored start point and starts the existing
    /// movement driver; it owns no generated-world session, save, route, or
    /// camera transition policy.
    /// </summary>
    public sealed class CharacterLiveMapRunBootstrap : MonoBehaviour
    {
        [SerializeField] private CharacterLivePlayerRig playerRig;
        [SerializeField] private CharacterLiveMovementDriver movementDriver;
        [SerializeField] private Vector2 spawnPosition = new Vector2(3f, 1f);

        public bool HasSpawned { get; private set; }

        public void Configure(
            CharacterLivePlayerRig rig,
            CharacterLiveMovementDriver driver,
            Vector2 spawn)
        {
            playerRig = rig;
            movementDriver = driver;
            spawnPosition = spawn;
        }

        private void Start()
        {
            if (playerRig == null || !playerRig.IsBound)
            {
                Debug.LogWarning("CharacterLiveMapRunBootstrap: Player rig is not bound.", this);
                return;
            }

            playerRig.Body.position = spawnPosition;
            playerRig.transform.position = new Vector3(spawnPosition.x, spawnPosition.y,
                playerRig.transform.position.z);
            Physics2D.SyncTransforms();
            if (movementDriver != null)
            {
                movementDriver.ResetMotion();
            }

            HasSpawned = movementDriver != null && movementDriver.IsDriving;
        }
    }
}
