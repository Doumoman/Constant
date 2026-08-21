#if LEGACY_DISABLED
using System;
using StarNight.Core.Tools;
using StarNight.Interaction.HandSlot;
using StarNight.Player.Presentation;
using StarNight.Stage.Rooms;
using UnityEngine;

namespace StarNight.Stage.Secrets
{
    public enum SecretDetectionBand
    {
        None,
        Distant,
        Near,
        Close,
        Immediate,
    }

    [DisallowMultipleComponent]
    public sealed class SecretDetectorController : MonoBehaviour, ICompassFocusDetector
    {
        public const float DetectionRangeCells = CompassEquipmentContract.PassiveDetectionRangeCells;
        public const float DirectionalPulseIntervalSeconds = 0.45f;

        [SerializeField] private AudioClip directionalPulseClip;

        private IEquipmentInventoryBridge inventory;
        private RoomRuntime currentRoomOverride;
        private SecretAnchor hintedAnchor;
        private SecretAnchor focusedAnchor;
        private SpriteRenderer eyeIndicator;
        private SpriteRenderer needleIndicator;
        private AudioSource directionalAudioSource;
        private float focusEndsAt;
        private float nextDirectionalPulseAt;
        private static AudioClip generatedDirectionalPulseClip;

        public event Action<Vector2> DirectionalPulseRequested;

        public SecretDetectionBand Band { get; private set; }
        public Vector2 Direction { get; private set; }
        public float DistanceCells { get; private set; } = float.PositiveInfinity;
        public bool HasMoonEyeCompass { get; private set; }
        public bool SlowBlinkActive => Band == SecretDetectionBand.Distant;
        public bool FastBlinkActive => Band == SecretDetectionBand.Near;
        public bool NeedleVisible => Band == SecretDetectionBand.Close || Band == SecretDetectionBand.Immediate;
        public bool FocusActive => focusedAnchor != null && Time.unscaledTime < focusEndsAt;
        public float FocusRemainingSeconds => FocusActive ? Mathf.Max(0f, focusEndsAt - Time.unscaledTime) : 0f;
        public SecretAnchor FocusedAnchor => FocusActive ? focusedAnchor : null;
        public SecretGateToolFamily? FocusedToolFamily => FocusActive ? focusedAnchor.RequiredToolFamily : null;

        private void Update()
        {
            ResolveInventory();
            RefreshDetection();
            TickPresentation();
        }

        public void Configure(
            IEquipmentInventoryBridge configuredInventory,
            RoomRuntime configuredCurrentRoom = null)
        {
            inventory = configuredInventory;
            currentRoomOverride = configuredCurrentRoom;
            RefreshDetection();
            TickPresentation();
        }

        public void SetCurrentRoom(RoomRuntime room)
        {
            currentRoomOverride = room;
            RefreshDetection();
        }

        public void RefreshDetection()
        {
            ResolveInventory();
            HasMoonEyeCompass = false;
            var entries = inventory?.HudEntries;
            for (int index = 0; entries != null && index < entries.Count; index++)
            {
                if (entries[index].StableItemId == "ITEM_MOON_EYE_COMPASS")
                {
                    HasMoonEyeCompass = true;
                    break;
                }
            }

            SecretAnchor nearest = HasMoonEyeCompass
                ? FindNearestUnopenedSecret(DetectionRangeCells)
                : null;
            float nearestDistance = nearest != null
                ? Vector2.Distance(transform.position, nearest.transform.position)
                : float.PositiveInfinity;

            if (hintedAnchor != null && hintedAnchor != nearest)
            {
                hintedAnchor.SetDetectorHint(false);
            }
            hintedAnchor = nearest;
            DistanceCells = nearestDistance;
            Direction = nearest != null
                ? ((Vector2)nearest.transform.position - (Vector2)transform.position).normalized
                : Vector2.zero;
            Band = ResolveBand(nearestDistance);
            nearest?.SetDetectorHint(Band == SecretDetectionBand.Immediate);
            RefreshPresentationVisibility();
        }

        public bool TryFocusNearestSecret(float rangeCells, float durationSeconds)
        {
            RefreshDetection();
            SecretAnchor nearest = HasMoonEyeCompass
                ? FindNearestUnopenedSecret(Mathf.Max(0f, rangeCells))
                : null;
            if (nearest == null)
            {
                return false;
            }

            if (focusedAnchor != null && focusedAnchor != nearest)
            {
                focusedAnchor.SetCompassFocused(false);
            }
            focusedAnchor = nearest;
            focusEndsAt = Time.unscaledTime + Mathf.Max(0f, durationSeconds);
            focusedAnchor.SetCompassFocused(true);
            return true;
        }

        public void ExpireFocusForTests()
        {
            focusEndsAt = 0f;
            TickPresentation();
        }

        private SecretAnchor FindNearestUnopenedSecret(float rangeCells)
        {
            RoomRuntime currentRoom = ResolveCurrentRoom();
            if (currentRoom == null)
            {
                return null;
            }

            SecretAnchor nearest = null;
            float nearestDistance = float.PositiveInfinity;
            SecretAnchor[] anchors = UnityEngine.Object.FindObjectsByType<SecretAnchor>(FindObjectsSortMode.None);
            for (int index = 0; index < anchors.Length; index++)
            {
                SecretAnchor anchor = anchors[index];
                if (anchor == null || anchor.IsRevealed || anchor.SourceRoom != currentRoom)
                {
                    continue;
                }
                float distance = Vector2.Distance(transform.position, anchor.transform.position);
                if (distance <= rangeCells && distance < nearestDistance)
                {
                    nearest = anchor;
                    nearestDistance = distance;
                }
            }
            return nearest;
        }

        private RoomRuntime ResolveCurrentRoom()
        {
            if (currentRoomOverride != null)
            {
                return currentRoomOverride;
            }

            Vector2 position = transform.position;
            RoomRuntime[] rooms = UnityEngine.Object.FindObjectsByType<RoomRuntime>(FindObjectsSortMode.None);
            for (int index = 0; index < rooms.Length; index++)
            {
                if (rooms[index] != null && rooms[index].WorldBounds.Contains(position))
                {
                    return rooms[index];
                }
            }
            return null;
        }

        private void TickPresentation()
        {
            if (focusedAnchor != null && Time.unscaledTime >= focusEndsAt)
            {
                focusedAnchor.SetCompassFocused(false);
                focusedAnchor = null;
            }

            EnsurePresentation();
            RefreshPresentationVisibility();
            if (eyeIndicator != null && eyeIndicator.enabled)
            {
                float frequency = FastBlinkActive ? 6f : 2f;
                Color color = eyeIndicator.color;
                color.a = 0.2f + (Mathf.Sin(Time.unscaledTime * frequency * Mathf.PI * 2f) + 1f) * 0.4f;
                eyeIndicator.color = color;
            }

            if (needleIndicator != null && needleIndicator.enabled)
            {
                float angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
                needleIndicator.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }

            if (FastBlinkActive && Time.unscaledTime >= nextDirectionalPulseAt)
            {
                nextDirectionalPulseAt = Time.unscaledTime + DirectionalPulseIntervalSeconds;
                DirectionalPulseRequested?.Invoke(Direction);
                if (directionalPulseClip != null)
                {
                    directionalAudioSource.panStereo = Mathf.Clamp(Direction.x, -1f, 1f);
                    directionalAudioSource.PlayOneShot(directionalPulseClip);
                }
            }
        }

        private void EnsurePresentation()
        {
            if (eyeIndicator != null && needleIndicator != null && directionalAudioSource != null)
            {
                return;
            }

            Transform eyeTransform = transform.Find("CompassEyeIndicator");
            GameObject eye = eyeTransform != null ? eyeTransform.gameObject : new GameObject("CompassEyeIndicator");
            if (eyeTransform == null)
            {
                eye.transform.SetParent(transform, false);
            }
            eye.transform.localPosition = new Vector3(0f, 1.18f, 0f);
            eye.transform.localScale = new Vector3(0.24f, 0.14f, 1f);
            eyeIndicator = eye.GetComponent<SpriteRenderer>();
            if (eyeIndicator == null)
            {
                eyeIndicator = eye.AddComponent<SpriteRenderer>();
            }
            eyeIndicator.sprite = PrototypeSpriteFactory.GetWhitePixel();
            eyeIndicator.color = new Color32(149, 218, 221, 0);
            eyeIndicator.sortingOrder = 40;

            Transform needleTransform = transform.Find("CompassNeedleIndicator");
            GameObject needle = needleTransform != null
                ? needleTransform.gameObject
                : new GameObject("CompassNeedleIndicator");
            if (needleTransform == null)
            {
                needle.transform.SetParent(transform, false);
            }
            needle.transform.localPosition = new Vector3(0f, 1.18f, 0f);
            needle.transform.localScale = new Vector3(0.58f, 0.06f, 1f);
            needleIndicator = needle.GetComponent<SpriteRenderer>();
            if (needleIndicator == null)
            {
                needleIndicator = needle.AddComponent<SpriteRenderer>();
            }
            needleIndicator.sprite = PrototypeSpriteFactory.GetWhitePixel();
            needleIndicator.color = new Color32(255, 211, 92, 230);
            needleIndicator.sortingOrder = 41;

            directionalAudioSource = GetComponent<AudioSource>();
            if (directionalAudioSource == null)
            {
                directionalAudioSource = gameObject.AddComponent<AudioSource>();
            }
            directionalAudioSource.playOnAwake = false;
            directionalAudioSource.spatialBlend = 0f;
            if (directionalPulseClip == null && Application.isPlaying)
            {
                directionalPulseClip = GetOrCreateDirectionalPulseClip();
            }
        }

        private void RefreshPresentationVisibility()
        {
            if (eyeIndicator != null)
            {
                eyeIndicator.enabled = HasMoonEyeCompass && (SlowBlinkActive || FastBlinkActive);
            }
            if (needleIndicator != null)
            {
                needleIndicator.enabled = HasMoonEyeCompass && NeedleVisible;
            }
        }

        private void ResolveInventory()
        {
            if (inventory != null)
            {
                return;
            }
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IEquipmentInventoryBridge bridge)
                {
                    inventory = bridge;
                    return;
                }
            }
        }

        private static SecretDetectionBand ResolveBand(float distance)
        {
            if (distance <= CompassEquipmentContract.ImmediateDistanceCells) return SecretDetectionBand.Immediate;
            if (distance <= CompassEquipmentContract.NeedleDistanceCells) return SecretDetectionBand.Close;
            if (distance <= CompassEquipmentContract.FastBlinkDistanceCells) return SecretDetectionBand.Near;
            if (distance <= DetectionRangeCells) return SecretDetectionBand.Distant;
            return SecretDetectionBand.None;
        }

        private static AudioClip GetOrCreateDirectionalPulseClip()
        {
            if (generatedDirectionalPulseClip != null)
            {
                return generatedDirectionalPulseClip;
            }

            const int sampleRate = 44100;
            const int sampleCount = 3528;
            float[] samples = new float[sampleCount];
            for (int index = 0; index < sampleCount; index++)
            {
                float normalized = (float)index / sampleCount;
                float envelope = 1f - normalized;
                samples[index] = Mathf.Sin(index * 2f * Mathf.PI * 880f / sampleRate)
                    * envelope * 0.12f;
            }
            generatedDirectionalPulseClip = AudioClip.Create(
                "CompassDirectionalPulse",
                sampleCount,
                1,
                sampleRate,
                false);
            generatedDirectionalPulseClip.SetData(samples, 0);
            return generatedDirectionalPulseClip;
        }
    }
}

#endif
