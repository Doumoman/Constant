#if LEGACY_DISABLED
using System;
using System.Collections;
using System.Collections.Generic;
using StarNight.Stage.Rooms;
using StarNight.Stage.Transitions;
using UnityEngine;

namespace StarNight.Stage.Streaming
{
    [DisallowMultipleComponent]
    public sealed class RoomStreamingManager : MonoBehaviour
    {
        public const int MaximumCellsAppliedPerFrame = 2048;
        public const float MaximumApplyMillisecondsPerFrame = 2f;

        private sealed class Record
        {
            public RoomStreamPlan Plan;
            public RoomRuntime Runtime;
            public RoomInstanceState State;
            public Coroutine BuildRoutine;
        }

        private readonly Dictionary<string, Record> records = new(StringComparer.Ordinal);
        private readonly HashSet<string> visitedRoomIds = new(StringComparer.Ordinal);

        public event Action<string, RoomInstanceState> StateChanged;
        public event Action<string, RoomRuntime> RoomInstantiated;

        public string CurrentRoomId { get; private set; } = string.Empty;
        public IReadOnlyCollection<string> VisitedRoomIds => visitedRoomIds;
        public int InstantiatedCount
        {
            get
            {
                int count = 0;
                foreach (Record record in records.Values)
                {
                    if (record.Runtime != null)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        public void ConfigurePlans(IEnumerable<RoomStreamPlan> plans)
        {
            StopAllCoroutines();
            records.Clear();
            visitedRoomIds.Clear();
            CurrentRoomId = string.Empty;
            if (plans == null)
            {
                return;
            }

            foreach (RoomStreamPlan plan in plans)
            {
                if (plan == null || string.IsNullOrWhiteSpace(plan.RoomId) || records.ContainsKey(plan.RoomId))
                {
                    continue;
                }
                records.Add(plan.RoomId, new Record
                {
                    Plan = plan,
                    State = RoomInstanceState.Uninstantiated,
                });
            }
        }

        public bool RegisterPlan(RoomStreamPlan plan, RoomRuntime existingRuntime = null)
        {
            if (plan == null || string.IsNullOrWhiteSpace(plan.RoomId) || records.ContainsKey(plan.RoomId))
            {
                return false;
            }

            records.Add(plan.RoomId, new Record
            {
                Plan = plan,
                Runtime = existingRuntime,
                State = RoomInstanceState.Uninstantiated,
            });
            existingRuntime?.SetSimulationState(RoomSimulationState.Dormant);
            RefreshPortalGates();
            return true;
        }

        public void ConfigureExistingRooms(IReadOnlyList<RoomRuntime> rooms)
        {
            var plans = new List<RoomStreamPlan>();
            if (rooms != null)
            {
                for (int index = 0; index < rooms.Count; index++)
                {
                    RoomRuntime room = rooms[index];
                    if (room == null)
                    {
                        continue;
                    }
                    var neighbors = new List<string>();
                    RoomPortal2D[] portals = room.GetComponentsInChildren<RoomPortal2D>(true);
                    for (int portalIndex = 0; portalIndex < portals.Length; portalIndex++)
                    {
                        string destinationId = portals[portalIndex].Destination?.RoomId;
                        if (!string.IsNullOrWhiteSpace(destinationId) && !neighbors.Contains(destinationId))
                        {
                            neighbors.Add(destinationId);
                        }
                    }
                    RoomRuntime captured = room;
                    plans.Add(new RoomStreamPlan(room.RoomId, StableSeed(room.RoomId), neighbors, () => captured));
                }
            }

            ConfigurePlans(plans);
            for (int index = 0; rooms != null && index < rooms.Count; index++)
            {
                RoomRuntime room = rooms[index];
                if (room != null && records.TryGetValue(room.RoomId, out Record record))
                {
                    record.Runtime = room;
                    room.SetSimulationState(RoomSimulationState.Dormant);
                }
            }
            RefreshPortalGates();
        }

        public bool Begin(string startRoomId)
        {
            if (!BuildNow(startRoomId) || !records.TryGetValue(startRoomId, out Record start))
            {
                return false;
            }

            CurrentRoomId = startRoomId;
            visitedRoomIds.Add(startRoomId);
            SetState(start, RoomInstanceState.Active);
            start.Runtime.SetSimulationState(RoomSimulationState.Active);
            WarmOneHop(start.Plan);
            RefreshPortalGates();
            return true;
        }

        public bool RequestWarmLoad(string roomId)
        {
            if (!records.TryGetValue(roomId ?? string.Empty, out Record record))
            {
                return false;
            }
            if (record.State != RoomInstanceState.Uninstantiated)
            {
                return true;
            }
            if (record.Plan.EstimatedCellCount <= 0)
            {
                return BuildNow(roomId);
            }

            SetState(record, RoomInstanceState.Building);
            record.BuildRoutine = StartCoroutine(BuildBudgeted(record));
            RefreshPortalGates();
            return true;
        }

        public bool Activate(string roomId)
        {
            if (!records.TryGetValue(roomId ?? string.Empty, out Record target)
                || target.Runtime == null
                || target.State != RoomInstanceState.WarmLoaded && target.State != RoomInstanceState.FrozenVisited)
            {
                return false;
            }

            if (records.TryGetValue(CurrentRoomId, out Record previous) && previous != target)
            {
                visitedRoomIds.Add(previous.Plan.RoomId);
                previous.Runtime?.SetSimulationState(RoomSimulationState.Frozen);
                SetState(previous, RoomInstanceState.FrozenVisited);
            }

            CurrentRoomId = roomId;
            visitedRoomIds.Add(roomId);
            target.Runtime.SetSimulationState(RoomSimulationState.Active);
            SetState(target, RoomInstanceState.Active);
            WarmOneHop(target.Plan);
            RefreshPortalGates();
            return true;
        }

        public RoomInstanceState GetState(string roomId)
        {
            return records.TryGetValue(roomId ?? string.Empty, out Record record)
                ? record.State
                : RoomInstanceState.Uninstantiated;
        }

        public bool IsWarmLoaded(string roomId)
        {
            RoomInstanceState state = GetState(roomId);
            return state == RoomInstanceState.WarmLoaded
                || state == RoomInstanceState.Active
                || state == RoomInstanceState.FrozenVisited;
        }

        public bool TryGetRuntime(string roomId, out RoomRuntime room)
        {
            room = records.TryGetValue(roomId ?? string.Empty, out Record record) ? record.Runtime : null;
            return room != null;
        }

        private bool BuildNow(string roomId)
        {
            if (!records.TryGetValue(roomId ?? string.Empty, out Record record))
            {
                return false;
            }
            if (record.Runtime != null && record.State != RoomInstanceState.Uninstantiated)
            {
                return true;
            }

            SetState(record, RoomInstanceState.Building);
            record.Runtime ??= record.Plan.Factory?.Invoke();
            if (record.Runtime == null)
            {
                SetState(record, RoomInstanceState.Uninstantiated);
                return false;
            }
            record.Runtime.SetSimulationState(RoomSimulationState.NeighborPreview);
            SetState(record, RoomInstanceState.WarmLoaded);
            RoomInstantiated?.Invoke(record.Plan.RoomId, record.Runtime);
            RefreshPortalGates();
            return true;
        }

        private IEnumerator BuildBudgeted(Record record)
        {
            record.Runtime ??= record.Plan.Factory?.Invoke();
            if (record.Runtime == null)
            {
                record.BuildRoutine = null;
                SetState(record, RoomInstanceState.Uninstantiated);
                yield break;
            }

            int applied = 0;
            while (applied < record.Plan.EstimatedCellCount)
            {
                float started = Time.realtimeSinceStartup;
                int frameCells = 0;
                while (applied < record.Plan.EstimatedCellCount
                    && frameCells < MaximumCellsAppliedPerFrame
                    && (Time.realtimeSinceStartup - started) * 1000f < MaximumApplyMillisecondsPerFrame)
                {
                    applied++;
                    frameCells++;
                }
                yield return null;
            }

            record.Runtime.SetSimulationState(RoomSimulationState.NeighborPreview);
            record.BuildRoutine = null;
            SetState(record, RoomInstanceState.WarmLoaded);
            RoomInstantiated?.Invoke(record.Plan.RoomId, record.Runtime);
            RefreshPortalGates();
        }

        private void WarmOneHop(RoomStreamPlan plan)
        {
            if (plan == null)
            {
                return;
            }
            for (int index = 0; index < plan.NeighborRoomIds.Count; index++)
            {
                RequestWarmLoad(plan.NeighborRoomIds[index]);
            }
        }

        private void RefreshPortalGates()
        {
            foreach (Record record in records.Values)
            {
                if (record.Runtime == null)
                {
                    continue;
                }
                RoomPortal2D[] portals = record.Runtime.GetComponentsInChildren<RoomPortal2D>(true);
                for (int index = 0; index < portals.Length; index++)
                {
                    RoomRuntime destination = portals[index].Destination;
                    portals[index].SetStreamingReady(destination != null && IsWarmLoaded(destination.RoomId));
                }
            }
        }

        private void SetState(Record record, RoomInstanceState state)
        {
            if (record.State == state)
            {
                return;
            }
            record.State = state;
            StateChanged?.Invoke(record.Plan.RoomId, state);
        }

        private static int StableSeed(string roomId)
        {
            unchecked
            {
                int hash = 17;
                for (int index = 0; index < (roomId?.Length ?? 0); index++)
                {
                    hash = hash * 31 + roomId[index];
                }
                return hash;
            }
        }
    }
}

#endif
