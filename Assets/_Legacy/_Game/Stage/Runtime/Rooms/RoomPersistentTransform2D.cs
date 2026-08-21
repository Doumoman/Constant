#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Stage.Rooms
{
    [DisallowMultipleComponent]
    public sealed class RoomPersistentTransform2D : MonoBehaviour, IRoomPersistentParticipant, IRoomSimulationParticipant
    {
        [Serializable]
        private struct Snapshot
        {
            public Vector2 position;
            public float rotation;
            public Vector2 velocity;
            public bool active;
        }

        [SerializeField] private string persistenceId;
        [SerializeField] private Rigidbody2D body;

        public string PersistenceId => string.IsNullOrWhiteSpace(persistenceId) ? gameObject.name : persistenceId;

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }
        }

        public void Configure(string id)
        {
            persistenceId = id;
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }
        }

        public string CaptureRoomState()
        {
            Snapshot snapshot = new Snapshot
            {
                position = body != null ? body.position : (Vector2)transform.position,
                rotation = body != null ? body.rotation : transform.eulerAngles.z,
                velocity = body != null ? body.linearVelocity : Vector2.zero,
                active = gameObject.activeSelf,
            };
            return JsonUtility.ToJson(snapshot);
        }

        public void RestoreRoomState(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return;
            }

            Snapshot snapshot = JsonUtility.FromJson<Snapshot>(payload);
            gameObject.SetActive(snapshot.active);
            if (body != null)
            {
                body.position = snapshot.position;
                body.rotation = snapshot.rotation;
                body.linearVelocity = snapshot.velocity;
            }
            else
            {
                transform.SetPositionAndRotation(snapshot.position, Quaternion.Euler(0f, 0f, snapshot.rotation));
            }
        }

        public void SetRoomSimulationState(RoomSimulationState state)
        {
            if (body != null)
            {
                body.simulated = state == RoomSimulationState.Active;
            }
        }
    }
}

#endif
