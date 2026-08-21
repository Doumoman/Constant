#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Explosions
{
    public enum BombState
    {
        Dormant = 0,
        Armed = 1,
        Queued = 2,
        Detonated = 3,
        SuppressedBySafetyCap = 4
    }

    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Bomb2D : MonoBehaviour
    {
        public const float DefaultFuseSeconds = ExplosionConstants.BombFuseSeconds;
        public const int DefaultStartCount = ExplosionConstants.DefaultStartingBombCount;

        [SerializeField] private ExplosionService2D explosionService;
        [SerializeField] private Rigidbody2D body;
        [SerializeField, Min(0f)] private float fuseSeconds = DefaultFuseSeconds;
        [SerializeField] private bool autoArmOnStart;
        [SerializeField] private bool destroyGameObjectOnDetonate = true;
        [SerializeField] private int deterministicOrder;
        [SerializeField] private BombState state;
        [SerializeField, Min(0f)] private float remainingFuseSeconds;

        public event Action<Bomb2D> Detonated;

        public ExplosionService2D Service => explosionService;
        public Rigidbody2D Body => body;
        public float FuseSeconds => fuseSeconds;
        public float RemainingFuseSeconds => remainingFuseSeconds;
        public bool AutoArmOnStart => autoArmOnStart;
        public bool DestroyGameObjectOnDetonate => destroyGameObjectOnDetonate;
        public int DeterministicOrder => deterministicOrder;
        public BombState State => state;

        internal long RegistrationOrder { get; set; }

        internal int ChainId
        {
            get
            {
                if (deterministicOrder != 0)
                {
                    return deterministicOrder;
                }

                if (RegistrationOrder <= int.MaxValue)
                {
                    return (int)RegistrationOrder;
                }

                return GetInstanceID();
            }
        }

        private void Reset()
        {
            body = GetComponent<Rigidbody2D>();
            EnsureContinuousCollision();
        }

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            EnsureContinuousCollision();
            ResolveService();
        }

        private void OnEnable()
        {
            ResolveService();
            if (explosionService != null)
            {
                explosionService.Register(this);
            }
        }

        private void Start()
        {
            if (autoArmOnStart)
            {
                Arm();
            }
        }

        private void FixedUpdate()
        {
            if (state != BombState.Armed)
            {
                return;
            }

            remainingFuseSeconds = Mathf.Max(0f, remainingFuseSeconds - Time.fixedDeltaTime);
            if (remainingFuseSeconds <= 0f)
            {
                RequestDetonation();
            }
        }

        private void OnDisable()
        {
            if (explosionService != null)
            {
                explosionService.Unregister(this);
            }
        }

        public void Configure(
            ExplosionService2D service,
            float configuredFuseSeconds = DefaultFuseSeconds,
            bool armOnStart = false,
            bool destroyOnDetonate = true,
            int stableOrder = 0)
        {
            bool wasRegistered = isActiveAndEnabled && explosionService != null;
            if (wasRegistered)
            {
                explosionService.Unregister(this);
            }

            explosionService = service;
            fuseSeconds = Mathf.Max(0f, configuredFuseSeconds);
            autoArmOnStart = armOnStart;
            destroyGameObjectOnDetonate = destroyOnDetonate;
            deterministicOrder = stableOrder;

            if (isActiveAndEnabled && explosionService != null)
            {
                explosionService.Register(this);
            }
        }

        public bool Arm()
        {
            return Arm(fuseSeconds);
        }

        public bool Arm(float delaySeconds)
        {
            if (state == BombState.Detonated || state == BombState.SuppressedBySafetyCap)
            {
                return false;
            }

            remainingFuseSeconds = Mathf.Max(0f, delaySeconds);
            state = BombState.Armed;
            return true;
        }

        public bool TriggerChain(ExplosionService2D sourceService = null)
        {
            if (sourceService != null && sourceService != explosionService)
            {
                Configure(
                    sourceService,
                    fuseSeconds,
                    autoArmOnStart,
                    destroyGameObjectOnDetonate,
                    deterministicOrder);
            }

            return RequestDetonation();
        }

        public ExplosionChainReport DetonateForTests()
        {
            if (!RequestDetonation() || explosionService == null)
            {
                return ExplosionChainReport.Empty;
            }

            return explosionService.ProcessPendingForTests();
        }

        internal bool TryMarkQueued()
        {
            if (state == BombState.Queued
                || state == BombState.Detonated
                || state == BombState.SuppressedBySafetyCap)
            {
                return false;
            }

            remainingFuseSeconds = 0f;
            state = BombState.Queued;
            return true;
        }

        internal void MarkDetonated()
        {
            if (state == BombState.Detonated)
            {
                return;
            }

            remainingFuseSeconds = 0f;
            state = BombState.Detonated;
            Detonated?.Invoke(this);

            if (destroyGameObjectOnDetonate && Application.isPlaying)
            {
                Destroy(gameObject);
            }
        }

        internal void MarkSuppressedBySafetyCap()
        {
            remainingFuseSeconds = 0f;
            state = BombState.SuppressedBySafetyCap;
        }

        private bool RequestDetonation()
        {
            ResolveService();
            return explosionService != null && explosionService.EnqueueDetonation(this);
        }

        private void ResolveService()
        {
            if (explosionService != null)
            {
                return;
            }

            explosionService = GetComponentInParent<ExplosionService2D>();
            if (explosionService == null)
            {
                explosionService = FindFirstObjectByType<ExplosionService2D>();
            }
        }

        private void EnsureContinuousCollision()
        {
            if (body != null)
            {
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
        }
    }
}

#endif
