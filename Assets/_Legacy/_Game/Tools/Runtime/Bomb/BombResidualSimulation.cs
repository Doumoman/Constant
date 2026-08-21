#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Tools.Bomb
{
    [Serializable]
    public sealed class BombSnapshot
    {
        public string RuntimeId;
        public Vector2 Position;
        public Vector2 Velocity;
        public float RemainingFuse;
        public bool Exploded;
        public bool Armed;
        public BombRuntimeState RuntimeState;
        public BombSimulationMode SimulationMode;
        public bool Active;
    }

    public enum ResidualSimulationState
    {
        Frozen,
        Running,
    }

    [DisallowMultipleComponent]
    public sealed class BombResidualSimulation : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float maximumSeconds = BombDefinition.ApprovedResidualSeconds;
        [SerializeField] private ResidualSimulationState state;
        [SerializeField] private float elapsedSeconds;
        [SerializeField] private List<BombRuntime> bombs = new List<BombRuntime>();
        [SerializeField] private List<Rigidbody2D> continuingBodies = new List<Rigidbody2D>();

        public event Action<IReadOnlyList<BombSnapshot>> SnapshotReady;

        public ResidualSimulationState State => state;
        public float ElapsedSeconds => elapsedSeconds;

        private void Update()
        {
            if (state == ResidualSimulationState.Running)
            {
                TickResidual(Time.deltaTime);
            }
        }

        public void Begin(IEnumerable<BombRuntime> activeBombs, IEnumerable<Rigidbody2D> activeBodies = null)
        {
            bombs.Clear();
            continuingBodies.Clear();
            if (activeBombs != null)
            {
                foreach (BombRuntime bomb in activeBombs)
                {
                    if (bomb == null || bombs.Contains(bomb))
                    {
                        continue;
                    }
                    bombs.Add(bomb);
                    bomb.SetSimulationMode(BombSimulationMode.Residual);
                }
            }
            if (activeBodies != null)
            {
                foreach (Rigidbody2D body in activeBodies)
                {
                    if (body != null && !continuingBodies.Contains(body))
                    {
                        continuingBodies.Add(body);
                    }
                }
            }

            elapsedSeconds = 0f;
            state = ResidualSimulationState.Running;
            if (!HasUnexplodedBomb())
            {
                FreezeAndSnapshot(false);
            }
        }

        public void TickResidual(float deltaSeconds)
        {
            if (state != ResidualSimulationState.Running || deltaSeconds <= 0f)
            {
                return;
            }

            elapsedSeconds += deltaSeconds;
            for (int index = 0; index < bombs.Count; index++)
            {
                BombRuntime bomb = bombs[index];
                if (bomb != null && !bomb.IsExploded)
                {
                    bomb.TickFuse(deltaSeconds);
                }
            }

            if (!HasUnexplodedBomb())
            {
                FreezeAndSnapshot(false);
            }
            else if (elapsedSeconds >= maximumSeconds)
            {
                FreezeAndSnapshot(true);
            }
        }

        public void ConfigureForTests(float configuredMaximumSeconds)
        {
            maximumSeconds = Mathf.Max(0.01f, configuredMaximumSeconds);
        }

        private bool HasUnexplodedBomb()
        {
            for (int index = 0; index < bombs.Count; index++)
            {
                if (bombs[index] != null && !bombs[index].IsExploded)
                {
                    return true;
                }
            }
            return false;
        }

        private void FreezeAndSnapshot(bool timedOut)
        {
            state = ResidualSimulationState.Frozen;
            var snapshots = new List<BombSnapshot>(bombs.Count);
            for (int index = 0; index < bombs.Count; index++)
            {
                BombRuntime bomb = bombs[index];
                if (bomb == null)
                {
                    continue;
                }
                bomb.SetSimulationMode(BombSimulationMode.Frozen);
                snapshots.Add(bomb.CaptureSnapshot());
            }

            if (timedOut)
            {
                for (int index = 0; index < continuingBodies.Count; index++)
                {
                    Rigidbody2D body = continuingBodies[index];
                    if (body != null)
                    {
                        body.linearVelocity = Vector2.zero;
                        body.angularVelocity = 0f;
                    }
                }
            }
            SnapshotReady?.Invoke(snapshots);
        }
    }
}

#endif
