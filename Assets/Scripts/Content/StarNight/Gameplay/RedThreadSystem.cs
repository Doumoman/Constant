using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class RedThreadSystem : MonoBehaviour
    {
        [SerializeField] private int baseConnectionLimit = 3;
        [SerializeField] private float contractionRatio = 0.62f;
        [SerializeField] private float stiffness = 13f;
        [SerializeField] private float damping = 1.8f;
        [SerializeField] private float baseBreakTension = 46f;

        private readonly List<RedThreadConnection> connections = new();
        private FableObject pendingEndpoint;
        private Material threadMaterial;
        private float reinforcementMultiplier = 1f;

        public IReadOnlyList<RedThreadConnection> Connections => connections;
        public FableObject PendingEndpoint => pendingEndpoint;
        public int ConnectionLimit => baseConnectionLimit +
            (StarNightRunState.Instance != null ? StarNightRunState.Instance.GetCounter("thread.connection_bonus") : 0);
        public float ReinforcementMultiplier => reinforcementMultiplier;

        public event Action<FableObject> PendingChanged;
        public event Action<RedThreadConnection> ConnectionCreated;
        public event Action<RedThreadConnection> ConnectionRemoved;

        public FableToolResult Use(FableObject target, string actorId = "Player")
        {
            if (target == null)
            {
                return FableToolResult.Fail("붉은 실을 걸 대상을 바라보세요.");
            }
            if (!target.HasTrait(FableTraits.Linkable) || target.IsOverloaded)
            {
                return FableToolResult.Fail($"{target.DisplayName}에는 붉은 실을 걸 수 없다.");
            }

            StarNightRunState run = StarNightRunState.Ensure();
            if (pendingEndpoint == null)
            {
                pendingEndpoint = target;
                PendingChanged?.Invoke(target);
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.LinkEndpointSelected,
                    actorId = actorId,
                    targetId = target.ObjectId,
                    tool = FableVerb.Link,
                    detail = $"{target.DisplayName}에 붉은 실의 첫 끝을 걸었다"
                });
                return new FableToolResult
                {
                    success = true,
                    awaitingSecondTarget = true,
                    sentence = $"{target.DisplayName}에 첫 끝을 걸었다. 다른 대상을 고르자."
                };
            }

            if (pendingEndpoint == target)
            {
                pendingEndpoint = null;
                PendingChanged?.Invoke(null);
                return new FableToolResult
                {
                    success = true,
                    sentence = "붉은 실의 첫 끝을 거두었다."
                };
            }

            RedThreadConnection existing = FindConnection(pendingEndpoint, target);
            if (existing != null)
            {
                FableObject first = pendingEndpoint;
                pendingEndpoint = null;
                PendingChanged?.Invoke(null);
                BreakConnection(existing, "매듭을 풀었다", true);
                return new FableToolResult
                {
                    success = true,
                    connectionChanged = true,
                    sentence = $"{first.DisplayName}과 {target.DisplayName}의 붉은 실을 풀었다."
                };
            }

            if (connections.Count >= ConnectionLimit)
            {
                return FableToolResult.Fail($"붉은 실 매듭이 가득 찼다. 현재 {connections.Count}/{ConnectionLimit}");
            }

            FableObject a = pendingEndpoint;
            pendingEndpoint = null;
            PendingChanged?.Invoke(null);
            bool aWasLinked = a.IsLinked;
            FableToolResult aResult = a.ApplyLinkState(true);
            if (!aResult.success || aResult.overloaded)
            {
                RegisterLinkOverload(run, a, aResult, actorId);
                return aResult;
            }

            FableToolResult bResult = target.ApplyLinkState(true);
            if (!bResult.success || bResult.overloaded)
            {
                if (!aWasLinked)
                {
                    a.ApplyLinkState(false);
                }
                RegisterLinkOverload(run, target, bResult, actorId);
                return bResult;
            }

            RedThreadConnection connection = CreateConnection(a, target);
            float rawScent = 7f * Mathf.Max(0.5f, (a.ScentWeight + target.ScentWeight) * 0.5f);
            float scent = run.ConsequenceResolver.ModifyScent(rawScent);
            run.Chapter.AddScent(scent, "두 물건 사이에서 붉은 실이 팽팽해졌다", $"{a.ObjectId}:{target.ObjectId}");
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.LinkCreated,
                actorId = actorId,
                targetId = $"{a.ObjectId}<->{target.ObjectId}",
                tool = FableVerb.Link,
                detail = $"{a.DisplayName}과 {target.DisplayName}의 힘을 연결했다",
                scentDelta = scent,
                witnessed = true
            });
            ConnectionCreated?.Invoke(connection);

            return new FableToolResult
            {
                success = true,
                connectionChanged = true,
                sentence = $"{a.DisplayName} ↔ {target.DisplayName}. 움직임과 힘을 나눈다.",
                scentAdded = scent,
                secondaryEffects = new List<string> { "장력 공유", "힘 전달" }
            };
        }

        public string Preview(FableObject target)
        {
            if (target == null)
            {
                return "붉은 실의 끝점을 바라보세요";
            }
            if (pendingEndpoint == null)
            {
                return $"{target.DisplayName}에 첫 끝 걸기";
            }
            if (pendingEndpoint == target)
            {
                return "첫 끝 거두기";
            }
            if (FindConnection(pendingEndpoint, target) != null)
            {
                return $"{pendingEndpoint.DisplayName}과의 매듭 풀기";
            }
            return $"{pendingEndpoint.DisplayName} ↔ {target.DisplayName} 연결";
        }

        public RedThreadConnection FindConnection(FableObject a, FableObject b)
        {
            return connections.Find(connection => connection != null && connection.Connects(a, b));
        }

        public RedThreadConnection FindConnection(FableObject endpoint)
        {
            return connections.Find(connection => connection != null && connection.Contains(endpoint));
        }

        public bool HasConnection(FableObject endpoint) => FindConnection(endpoint) != null;

        public void BreakConnection(RedThreadConnection connection, string reason, bool intentional)
        {
            if (connection == null || !connections.Remove(connection))
            {
                return;
            }

            FableObject a = connection.EndpointA;
            FableObject b = connection.EndpointB;
            ConnectionRemoved?.Invoke(connection);
            DestroyRuntimeObject(connection.gameObject);

            if (a != null && !HasConnection(a)) a.ApplyLinkState(false);
            if (b != null && !HasConnection(b)) b.ApplyLinkState(false);

            StarNightRunState run = StarNightRunState.Instance;
            if (run != null)
            {
                float scent = intentional ? 1f : run.ConsequenceResolver.ModifyScent(9f);
                if (scent > 0f)
                {
                    run.Chapter.AddScent(scent, reason, a != null ? a.ObjectId : b?.ObjectId);
                }
                StarActionRecord record = run.Actions.Record(new StarActionContext
                {
                    actionType = intentional ? StarActionType.LinkCut : StarActionType.LinkSnapped,
                    actorId = intentional ? "Player" : "Tension",
                    targetId = $"{a?.ObjectId}<->{b?.ObjectId}",
                    tool = FableVerb.Link,
                    detail = intentional ? "붉은 실의 매듭을 풀었다" : $"붉은 실이 {reason}",
                    scentDelta = scent,
                    causedAccident = !intentional,
                    witnessed = true
                });
                if (!intentional)
                {
                    run.AccidentReport.Add("팽팽해진 붉은 실", "장력을 견디지 못해",
                        $"{a?.DisplayName}과 {b?.DisplayName} 사이에서 끊어졌다", record?.sequence ?? 0);
                    StarNightHUD.Instance?.Toast("팽— 붉은 실이 끊어졌다! 마루가 소리를 들었다.", 4f);
                }
            }
        }

        public void Reinforce(float multiplier)
        {
            reinforcementMultiplier = Mathf.Max(reinforcementMultiplier, multiplier);
        }

        public int AddConnectionCapacity(int amount = 1)
        {
            return StarNightRunState.Ensure().AddCounter("thread.connection_bonus", Mathf.Max(0, amount));
        }

        public void ResetForChapter()
        {
            pendingEndpoint = null;
            for (int i = connections.Count - 1; i >= 0; i--)
            {
                RedThreadConnection connection = connections[i];
                if (connection != null)
                {
                    if (connection.EndpointA != null) connection.EndpointA.ApplyLinkState(false);
                    if (connection.EndpointB != null) connection.EndpointB.ApplyLinkState(false);
                    DestroyRuntimeObject(connection.gameObject);
                }
            }
            connections.Clear();
            reinforcementMultiplier = 1f;
            PendingChanged?.Invoke(null);
        }

        private RedThreadConnection CreateConnection(FableObject a, FableObject b)
        {
            EnsureMaterial();
            GameObject linkObject = new($"RedThread · {a.ObjectId} ↔ {b.ObjectId}");
            linkObject.transform.SetParent(transform, false);
            RedThreadConnection connection = linkObject.AddComponent<RedThreadConnection>();
            float distance = Vector2.Distance(a.transform.position, b.transform.position);
            float massFactor = Mathf.Sqrt(Mathf.Min(a.EffectiveMass, 8f) + Mathf.Min(b.EffectiveMass, 8f));
            connection.Configure(this, a, b, threadMaterial, distance * contractionRatio, stiffness, damping,
                baseBreakTension * reinforcementMultiplier * Mathf.Max(0.8f, massFactor * 0.45f));
            connections.Add(connection);
            return connection;
        }

        private void EnsureMaterial()
        {
            if (threadMaterial != null)
            {
                return;
            }

            Shader shader = Shader.Find("Sprites/Default");
            threadMaterial = new Material(shader != null ? shader : Shader.Find("Unlit/Color"))
            {
                name = "Runtime Red Thread Material"
            };
        }

        private static void RegisterLinkOverload(StarNightRunState run, FableObject target,
            FableToolResult result, string actorId)
        {
            float scent = run.ConsequenceResolver.ModifyScent(Mathf.Max(18f, result.scentAdded));
            run.Chapter.AddScent(scent, result.sentence, target.ObjectId);
            StarActionRecord record = run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.ToolOverloaded,
                actorId = actorId,
                targetId = target.ObjectId,
                tool = FableVerb.Link,
                detail = result.sentence,
                scentDelta = scent,
                causedAccident = true
            });
            run.AccidentReport.Add(target.DisplayName, "네 번째 말로 연결되려다", "붉은 실을 튕겨 냈다", record?.sequence ?? 0);
        }

        private static void DestroyRuntimeObject(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private void OnDestroy()
        {
            if (threadMaterial != null)
            {
                DestroyRuntimeObject(threadMaterial);
            }
        }
    }
}
