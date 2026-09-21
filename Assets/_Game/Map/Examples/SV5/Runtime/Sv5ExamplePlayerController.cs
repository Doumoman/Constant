using StarNight.Character.Live.Movement;
using StarNight.Character.Live.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StarNight.Map.SV5.Examples
{
    /// <summary>
    /// Example-scene lifecycle wrapper around the verified live Player prefab.
    /// Movement, one-way handling, and Grab all remain owned by the production
    /// CharacterLiveMovementDriver; this component only provides respawn.
    /// </summary>
    [RequireComponent(typeof(CharacterLivePlayerRig), typeof(CharacterLiveMovementDriver))]
    public sealed class Sv5ExamplePlayerController : MonoBehaviour
    {
        [SerializeField] private CharacterLivePlayerRig rig;
        [SerializeField] private CharacterLiveMovementDriver movement;
        [SerializeField] private Vector2 spawnPoint;
        [SerializeField] private Rect worldBounds = new Rect(0f, 0f, 624f, 416f);

        public CharacterLivePlayerRig Rig => rig;
        public CharacterLiveMovementDriver Movement => movement;
        public Rigidbody2D Body => rig == null ? null : rig.Body;
        public CapsuleCollider2D BodyCollider => rig == null ? null : rig.BodyCollider;
        public Vector2 SpawnPoint => spawnPoint;
        public Rect WorldBounds => worldBounds;
        public bool IsGroundedNow => movement != null && movement.IsGroundedNow;

        public void Configure(
            CharacterLivePlayerRig targetRig,
            CharacterLiveMovementDriver targetMovement,
            Vector2 spawn,
            Rect bounds)
        {
            rig = targetRig;
            movement = targetMovement;
            spawnPoint = spawn;
            worldBounds = bounds;
            transform.position = new Vector3(spawn.x, spawn.y, transform.position.z);
        }

        public void Respawn()
        {
            ResolveReferences();
            if (rig == null || rig.Body == null || movement == null)
            {
                return;
            }

            rig.Body.position = spawnPoint;
            rig.Body.linearVelocity = Vector2.zero;
            rig.Body.angularVelocity = 0f;
            transform.position = new Vector3(spawnPoint.x, spawnPoint.y, transform.position.z);
            movement.ResetMotion();
            Physics2D.SyncTransforms();
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            Respawn();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                Respawn();
            }
        }

        private void FixedUpdate()
        {
            if (Body == null)
            {
                return;
            }

            Vector2 position = Body.position;
            if (position.y < worldBounds.yMin - 8f ||
                position.x < worldBounds.xMin - 8f ||
                position.x > worldBounds.xMax + 8f)
            {
                Respawn();
            }
        }

        private void ResolveReferences()
        {
            if (rig == null)
            {
                rig = GetComponent<CharacterLivePlayerRig>();
            }

            if (movement == null)
            {
                movement = GetComponent<CharacterLiveMovementDriver>();
            }
        }
    }
}
