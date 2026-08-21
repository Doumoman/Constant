#if LEGACY_DISABLED
using System;
using StarNight.Map.Placement;
using UnityEngine;

namespace StarNight.Map
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ElementRuntimeId))]
    [RequireComponent(typeof(GridOccupier))]
    [RequireComponent(typeof(ElementStateMachine))]
    public sealed class MapElementInstance : MonoBehaviour,
        IMapElementSimulationParticipant,
        IMapElementPersistentParticipant
    {
        [SerializeField] private MapElementDefinition definition;
        [SerializeField] private MapElementState currentState = MapElementState.Dormant;
        [SerializeField] private MapElementState suspendedState = MapElementState.Idle;
        [SerializeField] private MapRoomState roomState = MapRoomState.Dormant;
        [SerializeField] private ElementRuntimeId runtimeId;
        [SerializeField] private GridOccupier gridOccupier;
        [SerializeField] private ElementStateMachine stateMachine;
        [SerializeField] private RoomElementRegistry roomRegistry;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform physicsRoot;
        [SerializeField] private Transform triggerRoot;

        private Animator[] animators = Array.Empty<Animator>();
        private float[] animatorSpeeds = Array.Empty<float>();
        private Rigidbody2D[] rigidbodies = Array.Empty<Rigidbody2D>();
        private bool[] rigidbodySimulatedDefaults = Array.Empty<bool>();
        private float suspendedElapsedSeconds;
        private bool initialized;
        private string lastRegistrationError = string.Empty;

        public event Action<MapElementState, MapElementState> StateChanged;

        public MapElementDefinition Definition => definition;
        public MapElementState CurrentState => currentState;
        public MapElementState SuspendedState => suspendedState;
        public MapRoomState RoomState => roomState;
        public ElementStateMachine StateMachine => stateMachine;
        public GridOccupier GridOccupier => gridOccupier;
        public string LastRegistrationError => lastRegistrationError;
        public string PersistenceId => runtimeId != null ? runtimeId.EnsureValue() : string.Empty;

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            if (roomRegistry != null && gridOccupier != null)
            {
                roomRegistry.Unregister(gridOccupier);
            }
        }

        public void Configure(
            MapElementDefinition elementDefinition,
            RoomElementRegistry registry = null,
            string persistentId = null)
        {
            Initialize();
            definition = elementDefinition;
            roomRegistry = registry != null ? registry : roomRegistry;

            if (!string.IsNullOrWhiteSpace(persistentId))
            {
                runtimeId.SetValue(persistentId);
            }

            suspendedState = GetInitialState();
            suspendedElapsedSeconds = 0f;
            currentState = MapElementState.Dormant;
            stateMachine.NotifyStateChanged();
            EnsureOccupancyRegistered();
            ApplyRoomState();
        }

        public void BindAuthoringRoots(
            Transform newVisualRoot,
            Transform newPhysicsRoot,
            Transform newTriggerRoot)
        {
            visualRoot = newVisualRoot;
            physicsRoot = newPhysicsRoot;
            triggerRoot = newTriggerRoot;
            RefreshRuntimeBindings();
            ApplyRoomState();
        }

        public void RefreshRuntimeBindings()
        {
            var previousAnimators = animators;
            var previousAnimatorSpeeds = animatorSpeeds;
            animators = GetComponentsInChildren<Animator>(true);
            animatorSpeeds = new float[animators.Length];
            for (var index = 0; index < animators.Length; index++)
            {
                var previousIndex = Array.IndexOf(previousAnimators, animators[index]);
                animatorSpeeds[index] = previousIndex >= 0 && previousIndex < previousAnimatorSpeeds.Length
                    ? previousAnimatorSpeeds[previousIndex]
                    : animators[index] != null ? animators[index].speed : 1f;
            }

            var previousRigidbodies = rigidbodies;
            var previousSimulatedDefaults = rigidbodySimulatedDefaults;
            rigidbodies = GetComponentsInChildren<Rigidbody2D>(true);
            rigidbodySimulatedDefaults = new bool[rigidbodies.Length];
            for (var index = 0; index < rigidbodies.Length; index++)
            {
                var previousIndex = Array.IndexOf(previousRigidbodies, rigidbodies[index]);
                rigidbodySimulatedDefaults[index] = previousIndex >= 0 && previousIndex < previousSimulatedDefaults.Length
                    ? previousSimulatedDefaults[previousIndex]
                    : rigidbodies[index] != null && rigidbodies[index].simulated;
            }
        }

        public bool TrySetState(MapElementState nextState)
        {
            Initialize();

            if (currentState == MapElementState.Dormant && roomState != MapRoomState.Active)
            {
                if (nextState == MapElementState.Dormant)
                {
                    return false;
                }

                var previousSuspended = suspendedState;
                suspendedState = nextState;
                suspendedElapsedSeconds = 0f;
                HandleOccupancyForLogicalState(nextState);
                StateChanged?.Invoke(previousSuspended, nextState);
                return true;
            }

            return SetStateInternal(nextState, true, false);
        }

        public void SetMapRoomState(MapRoomState nextRoomState)
        {
            Initialize();
            var previousRoomState = roomState;

            if (nextRoomState == MapRoomState.Frozen)
            {
                roomState = nextRoomState;
                ApplyRoomState();
                return;
            }

            if (nextRoomState == MapRoomState.Active)
            {
                roomState = nextRoomState;
                if (currentState == MapElementState.Dormant)
                {
                    var resumeState = ResolveResumeState(previousRoomState);
                    SetStateInternal(resumeState, false, true);
                    stateMachine.RestoreElapsedSeconds(suspendedElapsedSeconds);
                }

                ApplyRoomState();
                return;
            }

            if (previousRoomState == MapRoomState.Active && currentState != MapElementState.Dormant)
            {
                suspendedState = currentState;
                suspendedElapsedSeconds = stateMachine.ElapsedSeconds;
            }

            roomState = nextRoomState;
            if (currentState != MapElementState.Dormant)
            {
                SetStateInternal(MapElementState.Dormant, false, true);
            }

            ApplyRoomState();
        }

        public ElementSnapshot CaptureSnapshot()
        {
            Initialize();
            var logicalState = currentState == MapElementState.Dormant ? suspendedState : currentState;
            var elapsed = currentState == MapElementState.Dormant
                ? suspendedElapsedSeconds
                : stateMachine.ElapsedSeconds;

            if (logicalState == MapElementState.Broken &&
                definition != null &&
                definition.BehaviorProfile != null &&
                !definition.BehaviorProfile.PersistBrokenState)
            {
                logicalState = GetInitialState();
                elapsed = 0f;
            }

            var primaryBody = rigidbodies.Length > 0 ? rigidbodies[0] : null;
            return new ElementSnapshot
            {
                RuntimeId = PersistenceId,
                ElementId = definition != null ? definition.ElementId : string.Empty,
                State = logicalState,
                SuspendedState = suspendedState,
                LocalPosition = transform.localPosition,
                LocalRotation = transform.localRotation,
                LinearVelocity = primaryBody != null ? primaryBody.linearVelocity : Vector2.zero,
                AngularVelocity = primaryBody != null ? primaryBody.angularVelocity : 0f,
                StateElapsedSeconds = elapsed,
                OccupancyRegistered = roomRegistry != null && roomRegistry.IsRegistered(gridOccupier),
            };
        }

        public bool RestoreSnapshot(ElementSnapshot snapshot)
        {
            Initialize();
            if (snapshot == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(snapshot.RuntimeId) &&
                !string.Equals(PersistenceId, snapshot.RuntimeId, StringComparison.Ordinal))
            {
                return false;
            }

            transform.localPosition = snapshot.LocalPosition;
            transform.localRotation = snapshot.LocalRotation;
            suspendedState = snapshot.State;
            suspendedElapsedSeconds = Mathf.Max(0f, snapshot.StateElapsedSeconds);

            if (roomState == MapRoomState.Active || roomState == MapRoomState.Frozen)
            {
                SetStateInternal(snapshot.State, false, true);
                stateMachine.RestoreElapsedSeconds(snapshot.StateElapsedSeconds);
            }
            else
            {
                SetStateInternal(MapElementState.Dormant, false, true);
            }

            if (rigidbodies.Length > 0 && rigidbodies[0] != null)
            {
                rigidbodies[0].linearVelocity = snapshot.LinearVelocity;
                rigidbodies[0].angularVelocity = snapshot.AngularVelocity;
            }

            HandleOccupancyForLogicalState(snapshot.State);
            ApplyRoomState();
            return true;
        }

        public string CaptureMapElementState()
        {
            return JsonUtility.ToJson(CaptureSnapshot());
        }

        public void RestoreMapElementState(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return;
            }

            RestoreSnapshot(JsonUtility.FromJson<ElementSnapshot>(payload));
        }

        private void Initialize()
        {
            if (initialized)
            {
                return;
            }

            runtimeId = runtimeId != null ? runtimeId : GetComponent<ElementRuntimeId>();
            gridOccupier = gridOccupier != null ? gridOccupier : GetComponent<GridOccupier>();
            stateMachine = stateMachine != null ? stateMachine : GetComponent<ElementStateMachine>();
            roomRegistry = roomRegistry != null ? roomRegistry : GetComponentInParent<RoomElementRegistry>();
            visualRoot = visualRoot != null ? visualRoot : transform.Find("VisualRoot");
            physicsRoot = physicsRoot != null ? physicsRoot : transform.Find("PhysicsRoot");
            triggerRoot = triggerRoot != null ? triggerRoot : transform.Find("TriggerRoot");

            runtimeId?.EnsureValue();
            stateMachine?.Bind(this);
            suspendedState = definition != null ? GetInitialState() : suspendedState;
            RefreshRuntimeBindings();
            initialized = true;
            EnsureOccupancyRegistered();
            ApplyRoomState();
        }

        private MapElementState ResolveResumeState(MapRoomState previousRoomState)
        {
            var behavior = definition != null ? definition.BehaviorProfile : null;
            if (behavior != null &&
                behavior.ResetOnRoomReenter &&
                previousRoomState == MapRoomState.Dormant &&
                !(suspendedState == MapElementState.Broken && behavior.PersistBrokenState))
            {
                suspendedElapsedSeconds = 0f;
                return behavior.InitialState;
            }

            return suspendedState == MapElementState.Dormant ? GetInitialState() : suspendedState;
        }

        private MapElementState GetInitialState()
        {
            return definition != null && definition.BehaviorProfile != null
                ? definition.BehaviorProfile.InitialState
                : MapElementState.Idle;
        }

        private bool SetStateInternal(MapElementState nextState, bool resetTimer, bool allowBrokenExit)
        {
            if (currentState == nextState)
            {
                return false;
            }

            if (currentState == MapElementState.Broken &&
                nextState != MapElementState.Broken &&
                !allowBrokenExit)
            {
                return false;
            }

            var previousState = currentState;
            currentState = nextState;
            if (resetTimer)
            {
                stateMachine.NotifyStateChanged();
            }

            if (nextState != MapElementState.Dormant)
            {
                suspendedState = nextState;
            }

            HandleOccupancyForLogicalState(nextState == MapElementState.Dormant
                ? suspendedState
                : nextState);
            ApplyRoomState();
            StateChanged?.Invoke(previousState, nextState);
            return true;
        }

        private void HandleOccupancyForLogicalState(MapElementState logicalState)
        {
            if (logicalState == MapElementState.Broken)
            {
                if (roomRegistry != null && gridOccupier != null)
                {
                    roomRegistry.Unregister(gridOccupier);
                }
                return;
            }

            EnsureOccupancyRegistered();
        }

        private void EnsureOccupancyRegistered()
        {
            if (roomRegistry == null || gridOccupier == null || roomRegistry.IsRegistered(gridOccupier))
            {
                return;
            }

            if (!roomRegistry.TryRegister(gridOccupier, out var conflict))
            {
                lastRegistrationError = conflict.Reason;
                return;
            }

            lastRegistrationError = string.Empty;
        }

        private void ApplyRoomState()
        {
            if (!initialized || stateMachine == null)
            {
                return;
            }

            var operational = roomState == MapRoomState.Active &&
                              currentState != MapElementState.Dormant &&
                              currentState != MapElementState.Disabled &&
                              currentState != MapElementState.Broken;
            var visible = roomState != MapRoomState.Dormant;
            var collisionVisible = (roomState == MapRoomState.Active ||
                                    roomState == MapRoomState.TransitionTarget ||
                                    roomState == MapRoomState.Frozen) &&
                                   currentState != MapElementState.Broken;

            if (visualRoot != null)
            {
                visualRoot.gameObject.SetActive(visible);
            }

            if (physicsRoot != null)
            {
                physicsRoot.gameObject.SetActive(collisionVisible);
            }

            if (triggerRoot != null)
            {
                triggerRoot.gameObject.SetActive(operational);
            }

            stateMachine.SetTicking(operational);

            for (var index = 0; index < animators.Length; index++)
            {
                if (animators[index] != null)
                {
                    animators[index].speed = operational ? animatorSpeeds[index] : 0f;
                }
            }

            for (var index = 0; index < rigidbodies.Length; index++)
            {
                if (rigidbodies[index] == null)
                {
                    continue;
                }

                var transitionStaticBody = roomState == MapRoomState.TransitionTarget &&
                                           rigidbodies[index].bodyType == RigidbodyType2D.Static;
                rigidbodies[index].simulated = rigidbodySimulatedDefaults[index] &&
                                               (operational || transitionStaticBody);
            }
        }
    }
}

#endif
