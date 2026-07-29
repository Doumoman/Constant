using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class MagpieBridgeAnchor : MonoBehaviour
    {
        [SerializeField] private string anchorId = "anchor";
        [SerializeField] private FableObject socket;
        [SerializeField] private FableObject requiredPiece;
        [SerializeField] private float settleDistance = 2.1f;
        [SerializeField] private float settleDuration = 0.55f;
        [SerializeField] private GateRouteObjective routeObjective;
        private RedThreadSystem thread;
        private RedThreadConnection connection;
        private float settledFor;
        private bool repaired;

        public bool Repaired => repaired;
        public FableObject RequiredPiece => requiredPiece;

        public void Configure(string id, FableObject anchorSocket, FableObject piece)
        {
            anchorId = id;
            socket = anchorSocket;
            requiredPiece = piece;
        }

        public void ConfigureRouteObjective(GateRouteObjective objective)
        {
            routeObjective = objective;
        }

        private void Start()
        {
            thread = StarNightRunState.Ensure().RedThread;
            thread.ConnectionCreated += OnConnectionCreated;
            thread.ConnectionRemoved += OnConnectionRemoved;
            connection = thread.FindConnection(socket, requiredPiece);
        }

        private void OnDestroy()
        {
            if (thread != null)
            {
                thread.ConnectionCreated -= OnConnectionCreated;
                thread.ConnectionRemoved -= OnConnectionRemoved;
            }
        }

        private void Update()
        {
            if (repaired || connection == null || socket == null || requiredPiece == null)
            {
                return;
            }

            float distance = Vector2.Distance(socket.transform.position, requiredPiece.transform.position);
            float requiredDistance = Mathf.Max(settleDistance, connection.RestLength + 0.28f);
            settledFor = distance <= requiredDistance ? settledFor + Time.deltaTime : 0f;
            if (settledFor >= settleDuration)
            {
                CompleteRepair();
            }
        }

        public void AssistPull(float force = 12f)
        {
            if (repaired || socket == null || requiredPiece == null || requiredPiece.Body == null)
            {
                return;
            }

            Vector2 direction = (socket.transform.position - requiredPiece.transform.position).normalized;
            requiredPiece.Body.AddForce(direction * force + Vector2.up * 1.5f, ForceMode2D.Impulse);
        }

        private void OnConnectionCreated(RedThreadConnection created)
        {
            if (created.Connects(socket, requiredPiece))
            {
                connection = created;
                StarNightHUD.Instance?.Toast($"{anchorId}의 실이 당기기 시작했다. 장력이 안정될 때까지 지켜보자.");
            }
        }

        private void OnConnectionRemoved(RedThreadConnection removed)
        {
            if (removed == connection && !repaired)
            {
                connection = null;
                settledFor = 0f;
            }
        }

        private void CompleteRepair()
        {
            repaired = true;
            connection.LockAsRepaired();
            if (requiredPiece.Body != null)
            {
                requiredPiece.Body.linearVelocity = Vector2.zero;
                requiredPiece.Body.bodyType = RigidbodyType2D.Kinematic;
            }
            requiredPiece.transform.position = Vector3.Lerp(requiredPiece.transform.position,
                socket.transform.position + Vector3.right * 0.85f, 0.65f);

            StarNightRunState run = StarNightRunState.Ensure();
            run.SetFlag($"CH2_ANCHOR_{anchorId.ToUpperInvariant()}");
            if (run.Chapter.GateLoopEnabled && routeObjective != null)
            {
                routeObjective.Complete();
            }
            else
            {
                run.Chapter.AddDepartureProgress(1, anchorId);
            }
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.BridgeAnchorRestored,
                actorId = "Player",
                targetId = anchorId,
                tool = FableVerb.Link,
                detail = $"{anchorId}을 붉은 실 장력으로 복구했다",
                helpedResident = true,
                witnessed = true
            });
            StarNightHUD.Instance?.Toast(run.Chapter.GateLoopEnabled
                ? $"{anchorId} 매듭이 안정됐다. 별문에 연결할 닻을 확보했다."
                : $"매듭이 안정됐다. 다리 닻 {run.Chapter.DepartureProgress}/{run.Chapter.RequiredDepartureProgress}", 4f);
        }
    }
}
