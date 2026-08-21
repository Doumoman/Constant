#if LEGACY_DISABLED
using StarNight.Player.Motor;
using StarNight.Player.Presentation;
using UnityEngine;

namespace StarNight.Stage.Maru
{
    [DisallowMultipleComponent]
    public sealed class MaruRoomAgent : MonoBehaviour
    {
        public const float DefaultSpeed = 3.8f;
        public const float BiteDistance = 0.72f;
        public const float EscapeStunSeconds = 1.5f;

        private MaruDirector director;
        private MaruLane lane;
        private PlayerMotor2D player;
        private SpriteRenderer bodyRenderer;
        private float stunnedUntil;
        private bool biting;

        public string RoomId => lane?.Room?.RoomId ?? string.Empty;
        public bool IsStunned => Time.time < stunnedUntil;
        public bool IsBiting => biting;
        public float Speed { get; private set; } = DefaultSpeed;

        public void Configure(MaruDirector owner, MaruLane maruLane, PlayerMotor2D target, Vector2 spawnPosition)
        {
            director = owner;
            lane = maruLane;
            player = target;
            transform.position = lane != null ? lane.ClampToLane(spawnPosition) : spawnPosition;
            EnsureVisualAndTrigger();
        }

        public void SetBiting(bool value)
        {
            biting = value;
        }

        public void Stun(float seconds = EscapeStunSeconds)
        {
            biting = false;
            stunnedUntil = Time.time + Mathf.Max(0f, seconds);
        }

        private void Update()
        {
            if (director == null || lane == null || player == null || biting || IsStunned || Time.timeScale <= 0f)
            {
                UpdateColor();
                return;
            }

            Vector2 current = transform.position;
            Vector2 target = lane.ClampToLane(player.transform.position);
            Vector2 next = Vector2.MoveTowards(current, target, Speed * Time.deltaTime);
            transform.position = next;
            if (bodyRenderer != null && !Mathf.Approximately(target.x, current.x))
            {
                bodyRenderer.flipX = target.x < current.x;
            }
            if (Vector2.Distance(next, player.transform.position) <= BiteDistance)
            {
                director.TryBitePlayer(this);
            }
            UpdateColor();
        }

        private void UpdateColor()
        {
            if (bodyRenderer != null)
            {
                bodyRenderer.color = IsStunned
                    ? new Color32(99, 155, 170, 255)
                    : biting
                        ? new Color32(235, 126, 110, 255)
                        : new Color32(98, 70, 126, 255);
            }
        }

        private void EnsureVisualAndTrigger()
        {
            bodyRenderer = GetComponent<SpriteRenderer>();
            if (bodyRenderer == null)
            {
                bodyRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
            bodyRenderer.sprite = PrototypeSpriteFactory.GetWhitePixel();
            bodyRenderer.sortingOrder = 18;
            transform.localScale = new Vector3(1.15f, 1.05f, 1f);

            CircleCollider2D trigger = GetComponent<CircleCollider2D>();
            if (trigger == null)
            {
                trigger = gameObject.AddComponent<CircleCollider2D>();
            }
            trigger.isTrigger = true;
            trigger.radius = 0.48f;
            Rigidbody2D body = GetComponent<Rigidbody2D>();
            if (body == null)
            {
                body = gameObject.AddComponent<Rigidbody2D>();
            }
            body.bodyType = RigidbodyType2D.Kinematic;
            body.simulated = true;
            body.freezeRotation = true;
            UpdateColor();
        }
    }
}

#endif
